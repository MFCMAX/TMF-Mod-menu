namespace TMFModMenu.Features.Flight;

internal readonly record struct FlightVector(float X, float Y, float Z);

internal sealed class FlightControlGate
{
    private bool suppressedUntilNeutral;

    public int ResolveVertical(
        bool suppressForMenu,
        bool ascendPressed,
        bool descendPressed)
    {
        if (suppressForMenu)
        {
            suppressedUntilNeutral = true;
            return 0;
        }

        // The menu toggle gestures share the default ascend/descend inputs.
        // Do not let a partially released chord bleed into flight controls.
        if (suppressedUntilNeutral)
        {
            if (!ascendPressed && !descendPressed)
                suppressedUntilNeutral = false;
            return 0;
        }

        return StandaloneFlightMotion.Axis(ascendPressed, descendPressed);
    }

    public void SuppressUntilNeutral() => suppressedUntilNeutral = true;
}

internal static class StandaloneFlightMotion
{
    public const float VerticalSpeed = 0.16f;

    public static int Axis(bool positive, bool negative) =>
        positive == negative ? 0 : positive ? 1 : -1;

    public static float DesiredVerticalVelocity(int vertical) =>
        System.Math.Clamp(vertical, -1, 1) * VerticalSpeed;
}
