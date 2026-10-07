using AVATron.Core.Input;
using Xunit;

namespace AVATron.Tests;

public class InputQuantizationTests
{
    [Theory]
    [InlineData(1f, 0f, 127, 0)]
    [InlineData(0.92f, 0.38f, 127, 0)]      // 22.4 deg: still "right"
    [InlineData(0.9f, 0.43f, 127, 127)]     // 25.5 deg: now diagonal
    [InlineData(-0.7f, -0.7f, -127, -127)]
    [InlineData(0f, -1f, 0, -127)]
    public void EightWay_snaps_to_45_degree_sectors(float x, float y, int ex, int ey)
    {
        var s = StickQuantizer.EightWay(x, y, 0.2f);
        Assert.Equal((ex, ey), ((int)s.X, (int)s.Y));
    }

    [Fact]
    public void Deadzone_yields_neutral()
    {
        Assert.True(StickQuantizer.EightWay(0.1f, 0.1f, 0.2f).IsNeutral);
        Assert.True(StickQuantizer.Analog(0.1f, -0.1f, 0.2f).IsNeutral);
    }

    [Fact]
    public void Analog_preserves_direction_and_rescales_past_deadzone()
    {
        var s = StickQuantizer.Analog(0.6f, 0.8f, 0.2f);   // magnitude 1
        Assert.Equal(Math.Atan2(0.8, 0.6), Math.Atan2(s.Y, s.X), 2);
        Assert.InRange(Math.Sqrt(s.X * s.X + s.Y * s.Y), 125, 128);
    }

    [Fact]
    public void Opposing_digital_switches_cancel()
    {
        Assert.True(StickQuantizer.Digital(left: true, right: true, up: false, down: false).IsNeutral);
        var s = StickQuantizer.Digital(left: true, right: true, up: true, down: false);
        Assert.Equal((0, -127), ((int)s.X, (int)s.Y));
    }

    [Fact]
    public void Classic_collapse_of_analog_stick_is_eight_way()
    {
        var s = StickQuantizer.ToEightWay(new StickInput(100, 30));
        Assert.Equal((127, 0), ((int)s.X, (int)s.Y));
    }
}
