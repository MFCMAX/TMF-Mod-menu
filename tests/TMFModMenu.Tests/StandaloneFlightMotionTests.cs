using TMFModMenu.Features.Flight;

namespace TMFModMenu.Tests;

public sealed class StandaloneFlightMotionTests
{
    [Fact]
    public void AxisCancelsOpposingInputs()
    {
        Assert.Equal(1, StandaloneFlightMotion.Axis(true, false));
        Assert.Equal(-1, StandaloneFlightMotion.Axis(false, true));
        Assert.Equal(0, StandaloneFlightMotion.Axis(true, true));
        Assert.Equal(0, StandaloneFlightMotion.Axis(false, false));
    }

    [Theory]
    [InlineData(1, StandaloneFlightMotion.VerticalSpeed)]
    [InlineData(-1, -StandaloneFlightMotion.VerticalSpeed)]
    [InlineData(0, 0f)]
    public void VerticalInputProducesAscendDescendOrHover(
        int vertical,
        float expected)
    {
        Assert.Equal(
            expected,
            StandaloneFlightMotion.DesiredVerticalVelocity(vertical),
            4);
    }

    [Fact]
    public void MenuSuppressionWaitsForAscendToReturnNeutral()
    {
        var gate = new FlightControlGate();

        Assert.Equal(0, gate.ResolveVertical(true, true, false));
        Assert.Equal(0, gate.ResolveVertical(false, true, false));
        Assert.Equal(0, gate.ResolveVertical(false, false, false));
        Assert.Equal(1, gate.ResolveVertical(false, true, false));
    }

    [Fact]
    public void MenuSuppressionWaitsForDescendToReturnNeutral()
    {
        var gate = new FlightControlGate();

        gate.SuppressUntilNeutral();

        Assert.Equal(0, gate.ResolveVertical(false, false, true));
        Assert.Equal(0, gate.ResolveVertical(false, false, false));
        Assert.Equal(-1, gate.ResolveVertical(false, false, true));
    }
}
