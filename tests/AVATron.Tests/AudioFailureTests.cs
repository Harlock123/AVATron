using AVATron.Infrastructure.Audio;
using AVATron.Infrastructure.Input;
using Xunit;

namespace AVATron.Tests;

public class AudioFailureTests
{
    [Fact]
    public void Null_output_reports_reason_and_disposes_cleanly()
    {
        using var o = new NullAudioOutput("Audio disabled");
        Assert.False(o.IsAvailable);
        Assert.Equal("Audio disabled", o.Status);
    }

    [Fact]
    public void Game_runs_with_no_audio_output_attached()
    {
        // The mixer is never rendered (no device): playing cues must not throw or block.
        var mixer = new AudioMixer();
        var bank = SoundBank.Build();
        for (int i = 0; i < 10_000; i++) mixer.Play(bank[(Cue)(i % 28)]);
        Assert.Equal(29, bank.All.Count);
        Assert.True(mixer.PendingCommands <= AudioMixer.MaxPendingCommands + 8);   // bounded: no leak without a consumer
    }

    [Fact]
    public void Missing_gamepad_support_is_a_harmless_stub()
    {
        using var pad = new NoGamepadSource("Gamepad disabled");
        Assert.False(pad.Poll().Connected);
    }
}
