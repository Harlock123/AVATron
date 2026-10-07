using Silk.NET.SDL;
using Thread = System.Threading.Thread;
using ThreadPriority = System.Threading.ThreadPriority;
using AVATron.Infrastructure.Platform;

namespace AVATron.Infrastructure.Audio;

public interface IAudioOutput : IDisposable
{
    bool IsAvailable { get; }
    string Status { get; }
}

public sealed class NullAudioOutput(string reason) : IAudioOutput
{
    public bool IsAvailable => false;
    public string Status { get; } = reason;
    public void Dispose() { }
}

/// Pushes mixer output to an SDL2 audio device from a dedicated thread using SDL_QueueAudio.
/// Keeps ~40 ms queued: low latency, but tolerant of UI-thread hitches.
public sealed unsafe class SdlAudioOutput : IAudioOutput
{
    const int ChunkFrames = 512;
    const int TargetQueuedBytes = AudioMixer.SampleRate / 25 * sizeof(float);

    readonly Sdl _sdl;
    readonly uint _device;
    readonly AudioMixer _mixer;
    readonly Thread _thread;
    volatile bool _running = true;

    SdlAudioOutput(Sdl sdl, uint device, AudioMixer mixer)
    {
        _sdl = sdl; _device = device; _mixer = mixer;
        _thread = new Thread(Pump) { IsBackground = true, Name = "Audio", Priority = ThreadPriority.AboveNormal };
        _thread.Start();
        _sdl.PauseAudioDevice(_device, 0);
    }

    public bool IsAvailable => true;
    public string Status => "SDL2 audio";

    /// Never throws: any failure produces a NullAudioOutput describing why.
    public static IAudioOutput TryCreate(AudioMixer mixer)
    {
        try
        {
            var sdl = SdlHost.Instance.TryInitSubsystem(Sdl.InitAudio, out var err);
            if (sdl is null) return new NullAudioOutput($"Audio unavailable: {err}");
            var want = new AudioSpec { Freq = AudioMixer.SampleRate, Format = Sdl.AudioF32Sys, Channels = 1, Samples = ChunkFrames };
            AudioSpec have;
            uint dev = sdl.OpenAudioDevice((byte*)null, 0, &want, &have, 0);
            if (dev == 0) return new NullAudioOutput($"Audio unavailable: {sdl.GetErrorS()}");
            return new SdlAudioOutput(sdl, dev, mixer);
        }
        catch (Exception ex)
        {
            return new NullAudioOutput($"Audio unavailable: {ex.GetType().Name}: {ex.Message}");
        }
    }

    void Pump()
    {
        var buf = new float[ChunkFrames];
        while (_running)
        {
            try
            {
                while (_running && _sdl.GetQueuedAudioSize(_device) < TargetQueuedBytes)
                {
                    _mixer.Render(buf);
                    fixed (float* p = buf) _sdl.QueueAudio(_device, p, (uint)(buf.Length * sizeof(float)));
                }
            }
            catch (Exception) { _running = false; return; }
            Thread.Sleep(4);
        }
    }

    public void Dispose()
    {
        _running = false;
        _thread.Join(500);
        _sdl.CloseAudioDevice(_device);
    }
}
