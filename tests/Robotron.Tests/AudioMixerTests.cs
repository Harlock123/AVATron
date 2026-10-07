using Robotron.Infrastructure.Audio;
using Xunit;

namespace Robotron.Tests;

public class AudioMixerTests
{
    static SoundClip Clip(string n, int prio, float level = 0.9f, int len = 4000) => new(n, Enumerable.Repeat(level, len).ToArray(), prio);

    [Fact]
    public void Rapid_triggering_does_not_throw_or_exceed_full_scale()
    {
        var m = new AudioMixer { MasterVolume = 1, EffectsVolume = 1 };
        var clips = Enumerable.Range(0, 30).Select(i => Clip("c" + i, i % 5)).ToArray();
        var buf = new float[512];
        for (int round = 0; round < 200; round++)
        {
            foreach (var c in clips) m.Play(c);
            m.Render(buf);
            Assert.All(buf, s => Assert.InRange(s, -1f, 1f));
        }
        Assert.InRange(m.ActiveVoices, 1, 12);
    }

    [Fact]
    public void Single_channel_mode_lets_only_higher_or_equal_priority_interrupt()
    {
        var m = new AudioMixer { Mode = MixMode.SingleChannelPriority, MasterVolume = 1, EffectsVolume = 1 };
        var high = Clip("high", 5, 0.5f);
        var low = Clip("low", 1, 0.1f);
        var buf = new float[64];
        m.Play(high); m.Play(low);
        m.Render(buf);
        Assert.Equal(1, m.ActiveVoices);
        Assert.True(buf[0] > 0.3f);   // the high-priority clip is what is sounding
    }

    [Fact]
    public void Null_and_empty_clips_are_ignored()
    {
        var m = new AudioMixer();
        m.Play(null); m.Play(new SoundClip("e", [], 1));
        m.Render(new float[16]);
        Assert.Equal(0, m.ActiveVoices);
    }

    [Fact]
    public void Muted_mixer_outputs_silence()
    {
        var m = new AudioMixer { Muted = true };
        m.Play(Clip("a", 1));
        var buf = new float[32]; m.Render(buf);
        Assert.All(buf, s => Assert.Equal(0f, s));
    }
}
