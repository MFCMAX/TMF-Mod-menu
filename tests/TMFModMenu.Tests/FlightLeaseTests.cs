using System;
using System.Collections.Generic;
using TMFModMenu.Features.Flight;

namespace TMFModMenu.Tests;

public sealed class FlightLeaseTests
{
    [Fact]
    public void EnableSnapshotsOverridesAndPreservesHorizontalVelocity()
    {
        var actor = new FakeFlightActor(
            gravityOverride: 0.35f,
            velocity: new FlightVector(1f, -2f, 3f));
        var lease = new FlightLease();

        Assert.True(lease.Enable(actor));

        Assert.True(lease.IsEnabled);
        Assert.True(lease.HasPendingRestore);
        Assert.Equal(0.35f, lease.OriginalGravityOverride);
        Assert.Equal(0f, lease.OriginalSpeedOverride);
        Assert.Equal(FlightLease.ActiveGravityOverride, actor.GravityOverride);
        Assert.Equal(FlightLease.ActiveSpeedOverride, actor.SpeedOverride);
        Assert.Equal(new FlightVector(1f, 0f, 3f), actor.Velocity);
        Assert.Equal(
            new[] { "gravity:0.0001", "speed:4", "velocity:1,0,3" },
            actor.Events);
    }

    [Fact]
    public void ArmVerticalMotionCompensatesGravityAndPreservesHorizontalVelocity()
    {
        var actor = new FakeFlightActor(
            0f,
            new FlightVector(0.4f, 0f, -0.3f));
        var lease = new FlightLease();
        lease.Enable(actor);
        actor.Events.Clear();

        lease.ArmVerticalMotion(actor, 0.16f);

        Assert.Equal(
            new FlightVector(0.4f, 0.1599f, -0.3f),
            actor.Velocity);
        Assert.Equal(new[] { "velocity:0.4,0.1599,-0.3" }, actor.Events);
    }

    [Theory]
    [InlineData(0f, -0.0001f)]
    [InlineData(-0.16f, -0.1601f)]
    public void ArmVerticalMotionCompensatesHoverAndControlledLanding(
        float desired,
        float expectedArmed)
    {
        var actor = new FakeFlightActor(
            0f,
            new FlightVector(0.4f, 0.2f, -0.3f));
        var lease = new FlightLease();
        lease.Enable(actor);

        lease.ArmVerticalMotion(actor, desired);

        Assert.Equal(expectedArmed, actor.Velocity.Y, 4);
        Assert.Equal(0.4f, actor.Velocity.X);
        Assert.Equal(-0.3f, actor.Velocity.Z);
    }

    [Fact]
    public void DisableStopsMotionThenRestoresExactGravityOverride()
    {
        var actor = new FakeFlightActor(0.42f, default);
        var lease = new FlightLease();
        lease.Enable(actor);
        actor.Events.Clear();

        lease.Disable(actor);

        Assert.False(lease.IsEnabled);
        Assert.False(lease.HasPendingRestore);
        Assert.Equal(default, actor.Velocity);
        Assert.Equal(0.42f, actor.GravityOverride);
        Assert.Equal(0f, actor.SpeedOverride);
        Assert.Equal(
            new[] { "velocity:0,0,0", "speed:0", "gravity:0.42" },
            actor.Events);
    }

    [Fact]
    public void RepeatedDisableIsIdempotent()
    {
        var actor = new FakeFlightActor(0f, default);
        var lease = new FlightLease();
        lease.Enable(actor);
        lease.Disable(actor);
        actor.Events.Clear();

        lease.Disable(actor);

        Assert.Empty(actor.Events);
    }

    [Fact]
    public void IgnoredEnableOverrideIsDetectedAndRolledBack()
    {
        var actor = new FakeFlightActor(0.5f, default)
        {
            IgnoreActiveGravity = true
        };
        var lease = new FlightLease();

        Assert.Throws<InvalidOperationException>(() => lease.Enable(actor));

        Assert.False(lease.IsEnabled);
        Assert.False(lease.HasPendingRestore);
        Assert.Equal(0.5f, actor.GravityOverride);
    }

    [Fact]
    public void ThrowAfterEnableMutationStillRestoresOriginalGravity()
    {
        var actor = new FakeFlightActor(0.7f, default)
        {
            ThrowAfterApplyingActiveGravity = true
        };
        var lease = new FlightLease();

        Assert.Throws<InvalidOperationException>(() => lease.Enable(actor));

        Assert.False(lease.IsEnabled);
        Assert.False(lease.HasPendingRestore);
        Assert.Equal(0.7f, actor.GravityOverride);
    }

    [Fact]
    public void IgnoredRestoreRemainsPendingAndCanBeRetried()
    {
        var actor = new FakeFlightActor(0.25f, default);
        var lease = new FlightLease();
        lease.Enable(actor);
        actor.IgnoreOriginalGravity = true;

        Assert.Throws<InvalidOperationException>(() => lease.Disable(actor));
        Assert.True(lease.HasPendingRestore);
        Assert.Equal(FlightLease.ActiveGravityOverride, actor.GravityOverride);

        actor.IgnoreOriginalGravity = false;
        lease.Disable(actor);

        Assert.False(lease.HasPendingRestore);
        Assert.Equal(0.25f, actor.GravityOverride);
    }

    [Fact]
    public void IgnoredSpeedRestoreIsRetriedWithoutBlockingGravityRestore()
    {
        var actor = new FakeFlightActor(0.2f, default, speedOverride: 0.75f);
        var lease = new FlightLease();
        lease.Enable(actor);
        actor.IgnoreOriginalSpeed = true;

        Assert.Throws<InvalidOperationException>(() => lease.Disable(actor));
        Assert.True(lease.HasPendingRestore);
        Assert.Equal(FlightLease.ActiveSpeedOverride, actor.SpeedOverride);
        Assert.Equal(0.2f, actor.GravityOverride);

        actor.IgnoreOriginalSpeed = false;
        lease.Disable(actor);

        Assert.False(lease.HasPendingRestore);
        Assert.Equal(0.75f, actor.SpeedOverride);
        Assert.Equal(0.2f, actor.GravityOverride);
    }

    [Fact]
    public void RejectedVelocityStopKeepsOverridesUntilRetrySucceeds()
    {
        var actor = new FakeFlightActor(
            0.2f,
            new FlightVector(0.4f, 0.3f, -0.2f),
            speedOverride: 0.75f);
        var lease = new FlightLease();
        lease.Enable(actor);
        lease.ArmVerticalMotion(actor, 0.16f);
        actor.IgnoreZeroVertical = true;

        Assert.Throws<InvalidOperationException>(() => lease.Disable(actor));

        Assert.True(lease.HasPendingRestore);
        Assert.Equal(FlightLease.ActiveGravityOverride, actor.GravityOverride);
        Assert.Equal(FlightLease.ActiveSpeedOverride, actor.SpeedOverride);

        actor.IgnoreZeroVertical = false;
        lease.Disable(actor);

        Assert.False(lease.HasPendingRestore);
        Assert.Equal(0f, actor.Velocity.Y);
        Assert.Equal(0.2f, actor.GravityOverride);
        Assert.Equal(0.75f, actor.SpeedOverride);
    }

    private sealed class FakeFlightActor : IFlightActorPort
    {
        private float gravityOverride;
        private float speedOverride;
        private FlightVector velocity;

        public FakeFlightActor(
            float gravityOverride,
            FlightVector velocity,
            float speedOverride = 0f)
        {
            this.gravityOverride = gravityOverride;
            this.velocity = velocity;
            this.speedOverride = speedOverride;
        }

        public List<string> Events { get; } = new();

        public bool IgnoreActiveGravity { get; init; }

        public bool ThrowAfterApplyingActiveGravity { get; init; }

        public bool IgnoreOriginalGravity { get; set; }

        public bool IgnoreOriginalSpeed { get; set; }

        public bool IgnoreZeroVertical { get; set; }

        public float GravityOverride
        {
            get => gravityOverride;
            set
            {
                Events.Add($"gravity:{value:0.####}");
                if (value == FlightLease.ActiveGravityOverride)
                {
                    if (!IgnoreActiveGravity)
                        gravityOverride = value;
                    if (ThrowAfterApplyingActiveGravity)
                        throw new InvalidOperationException(
                            "Gravity write failed after mutation.");
                    return;
                }

                if (!IgnoreOriginalGravity)
                    gravityOverride = value;
            }
        }

        public FlightVector Velocity
        {
            get => velocity;
            set
            {
                Events.Add($"velocity:{value.X:0.####},{value.Y:0.####},{value.Z:0.####}");
                if (!IgnoreZeroVertical || value.Y != 0f)
                    velocity = value;
            }
        }

        public float SpeedOverride
        {
            get => speedOverride;
            set
            {
                Events.Add($"speed:{value:0.####}");
                if (value == FlightLease.ActiveSpeedOverride ||
                    !IgnoreOriginalSpeed)
                    speedOverride = value;
            }
        }

        public bool IsOnGround => true;
    }
}
