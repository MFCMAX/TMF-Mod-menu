using System;

namespace TMFModMenu.Features.Flight;

internal interface IFlightActorPort
{
    float GravityOverride { get; set; }

    float SpeedOverride { get; set; }

    FlightVector Velocity { get; set; }

    bool IsOnGround { get; }
}

internal sealed class FlightLease
{
    // Total Miner treats exactly zero as "use normal gravity". A small,
    // non-zero public override produces effectively neutral gravity while
    // keeping collision and normal actor physics active.
    public const float ActiveGravityOverride = 0.0001f;
    public const float ActiveSpeedOverride = 4f;

    private bool needsGravityRestore;
    private bool needsSpeedRestore;
    private bool needsVelocityStop;

    public bool IsEnabled { get; private set; }

    public bool HasPendingRestore =>
        needsGravityRestore || needsSpeedRestore || needsVelocityStop;

    public float OriginalGravityOverride { get; private set; }

    public float OriginalSpeedOverride { get; private set; }

    public bool Enable(IFlightActorPort actor)
    {
        if (actor == null)
            throw new ArgumentNullException(nameof(actor));
        if (IsEnabled)
            return true;

        OriginalGravityOverride = actor.GravityOverride;
        OriginalSpeedOverride = actor.SpeedOverride;
        needsGravityRestore = true;
        needsSpeedRestore = true;
        needsVelocityStop = true;
        try
        {
            actor.GravityOverride = ActiveGravityOverride;
            if (actor.GravityOverride != ActiveGravityOverride)
                throw new InvalidOperationException(
                    "The host rejected the custom gravity override.");

            actor.SpeedOverride = ActiveSpeedOverride;
            if (actor.SpeedOverride != ActiveSpeedOverride)
                throw new InvalidOperationException(
                    "The host rejected the custom speed override.");

            SetVerticalVelocity(actor, 0f);
            IsEnabled = true;
            return true;
        }
        catch
        {
            try
            {
                Disable(actor);
            }
            catch
            {
                // The service retains the lease while restoration is pending.
            }
            throw;
        }
    }

    public void ArmVerticalMotion(
        IFlightActorPort actor,
        float desiredPhysicsVelocity)
    {
        if (actor == null)
            throw new ArgumentNullException(nameof(actor));
        if (!IsEnabled)
            return;

        // Physics adds GravityOverride after the plugin input callback. Arm
        // the pre-physics value that resolves to the requested Y velocity.
        SetVerticalVelocity(
            actor,
            desiredPhysicsVelocity - ActiveGravityOverride);
    }

    public void Disable(IFlightActorPort actor)
    {
        if (actor == null)
            throw new ArgumentNullException(nameof(actor));
        if (!HasPendingRestore)
            return;

        IsEnabled = false;
        if (needsVelocityStop)
        {
            // Stop only the vertical component owned by this feature. Do not
            // erase host-authored knockback, current, or platform momentum.
            SetVerticalVelocity(actor, 0f);
            needsVelocityStop = false;
        }

        Exception restoreError = null;
        if (needsSpeedRestore)
        {
            try
            {
                actor.SpeedOverride = OriginalSpeedOverride;
                if (actor.SpeedOverride != OriginalSpeedOverride)
                    throw new InvalidOperationException(
                        "The host rejected the original speed restore.");
                needsSpeedRestore = false;
            }
            catch (Exception ex)
            {
                restoreError = ex;
            }
        }

        if (needsGravityRestore)
        {
            try
            {
                actor.GravityOverride = OriginalGravityOverride;
                if (actor.GravityOverride != OriginalGravityOverride)
                    throw new InvalidOperationException(
                        "The host rejected the original gravity restore.");
                needsGravityRestore = false;
            }
            catch (Exception ex)
            {
                restoreError ??= ex;
            }
        }

        if (restoreError != null)
            throw restoreError;
    }

    private static void SetVerticalVelocity(
        IFlightActorPort actor,
        float value)
    {
        var current = actor.Velocity;
        actor.Velocity = new FlightVector(current.X, value, current.Z);
        if (actor.Velocity.Y != value)
            throw new InvalidOperationException(
                "The host rejected the custom vertical velocity.");
    }
}
