using System;
using TMFModMenu.Features.Survival;

namespace TMFModMenu.Tests;

public sealed class SurvivalAssistLeaseTests
{
    [Fact]
    public void EnableSubscribesOnceAndRefillsAllBelowMaximumVitals()
    {
        var actor = new FakeSurvivalPort
        {
            Health = 4f,
            MaxHealth = 20f,
            Oxygen = 2f,
            MaxOxygen = 10f,
            Stamina = 1f,
            MaxStamina = 8f
        };
        var lease = new SurvivalAssistLease();
        actor.ResetWriteCounts();

        Assert.True(lease.Enable(actor));
        Assert.True(lease.Enable(actor));

        Assert.True(lease.IsEnabled);
        Assert.Equal(1, actor.AddCalls);
        Assert.Equal(20f, actor.Health);
        Assert.Equal(10f, actor.Oxygen);
        Assert.Equal(8f, actor.Stamina);
        Assert.Equal(1, actor.HealthWrites);
        Assert.Equal(1, actor.OxygenWrites);
        Assert.Equal(1, actor.StaminaWrites);
    }

    [Fact]
    public void DamageCallbackRefillsNonlethalVitalsImmediately()
    {
        var actor = FakeSurvivalPort.AtMaximum();
        var lease = new SurvivalAssistLease();
        lease.Enable(actor);
        actor.Health = 8f;
        actor.Oxygen = 3f;
        actor.Stamina = 2f;
        actor.ResetWriteCounts();

        actor.RaiseDamageTaken();

        Assert.Equal(actor.MaxHealth, actor.Health);
        Assert.Equal(actor.MaxOxygen, actor.Oxygen);
        Assert.Equal(actor.MaxStamina, actor.Stamina);
        Assert.True(lease.IsEnabled);
    }

    [Fact]
    public void RefillDoesNotLowerOverMaximumOrRewriteFullVitals()
    {
        var actor = FakeSurvivalPort.AtMaximum();
        actor.Health = actor.MaxHealth + 5f;
        actor.ResetWriteCounts();
        var lease = new SurvivalAssistLease();

        lease.Enable(actor);
        lease.Refresh();

        Assert.Equal(actor.MaxHealth + 5f, actor.Health);
        Assert.Equal(0, actor.HealthWrites);
        Assert.Equal(0, actor.OxygenWrites);
        Assert.Equal(0, actor.StaminaWrites);
    }

    [Fact]
    public void InvalidMaximumsDoNotCauseInvalidWrites()
    {
        var actor = new FakeSurvivalPort
        {
            Health = 1f,
            MaxHealth = float.NaN,
            Oxygen = 1f,
            MaxOxygen = 0f,
            Stamina = 1f,
            MaxStamina = -1f
        };
        var lease = new SurvivalAssistLease();
        actor.ResetWriteCounts();

        Assert.True(lease.Enable(actor));

        Assert.Equal(0, actor.HealthWrites);
        Assert.Equal(0, actor.OxygenWrites);
        Assert.Equal(0, actor.StaminaWrites);
    }

    [Fact]
    public void IgnoredHealthWriteIsDetectedAfterOtherVitalsAreAttempted()
    {
        var actor = new FakeSurvivalPort
        {
            Health = 1f,
            MaxHealth = 20f,
            Oxygen = 1f,
            MaxOxygen = 10f,
            Stamina = 1f,
            MaxStamina = 8f,
            IgnoreHealthWrite = true
        };
        var lease = new SurvivalAssistLease();

        Assert.Throws<InvalidOperationException>(() => lease.Enable(actor));

        Assert.False(lease.IsEnabled);
        Assert.False(lease.HasPendingCleanup);
        Assert.Equal(10f, actor.Oxygen);
        Assert.Equal(8f, actor.Stamina);
        Assert.Equal(1, actor.RemoveCalls);
    }

    [Fact]
    public void SetterFailureInsideDamageEventIsContainedAndStopsLease()
    {
        var actor = FakeSurvivalPort.AtMaximum();
        var lease = new SurvivalAssistLease();
        lease.Enable(actor);
        actor.Health = 1f;
        actor.Oxygen = 1f;
        actor.Stamina = 1f;
        actor.ThrowOnHealthWrite = true;

        var error = Record.Exception(actor.RaiseDamageTaken);

        Assert.Null(error);
        Assert.False(lease.IsEnabled);
        Assert.Equal(SurvivalAssistStopReason.CallbackFailure, lease.StopReason);
        Assert.Equal(1, actor.RemoveCalls);
        Assert.Equal(actor.MaxOxygen, actor.Oxygen);
        Assert.Equal(actor.MaxStamina, actor.Stamina);
    }

    [Fact]
    public void LethalDamageNeverAttemptsResurrectionAndUnsubscribes()
    {
        var actor = FakeSurvivalPort.AtMaximum();
        SurvivalAssistStopReason? observedReason = null;
        var lease = new SurvivalAssistLease(reason => observedReason = reason);
        lease.Enable(actor);
        actor.Health = 0f;
        actor.IsUnavailable = true;
        actor.ResetWriteCounts();

        actor.RaiseDamageTaken();

        Assert.Equal(0f, actor.Health);
        Assert.Equal(0, actor.HealthWrites);
        Assert.False(lease.IsEnabled);
        Assert.Equal(SurvivalAssistStopReason.InactiveOrDead, lease.StopReason);
        Assert.Equal(SurvivalAssistStopReason.InactiveOrDead, observedReason);
        Assert.Equal(1, actor.RemoveCalls);
    }

    [Fact]
    public void ReentrantDamageDuringRefillDoesNotDoubleWrite()
    {
        var actor = FakeSurvivalPort.AtMaximum();
        var lease = new SurvivalAssistLease();
        lease.Enable(actor);
        actor.Health = 1f;
        actor.ResetWriteCounts();
        actor.RaiseDamageOnHealthWrite = true;

        actor.RaiseDamageTaken();

        Assert.True(lease.IsEnabled);
        Assert.Equal(1, actor.HealthWrites);
        Assert.Equal(actor.MaxHealth, actor.Health);
    }

    [Fact]
    public void DisableLeavesCurrentVitalsAndStaleCallbackIsNoOp()
    {
        var actor = FakeSurvivalPort.AtMaximum();
        var lease = new SurvivalAssistLease();
        lease.Enable(actor);
        var staleHandler = actor.LastAddedHandler;

        lease.Disable();
        lease.Disable();
        actor.Health = 1f;
        staleHandler();

        Assert.False(lease.IsEnabled);
        Assert.Equal(1f, actor.Health);
        Assert.Equal(1, actor.RemoveCalls);
    }

    [Fact]
    public void FailedUnsubscribeRemainsPendingUntilRetrySucceeds()
    {
        var actor = FakeSurvivalPort.AtMaximum();
        var lease = new SurvivalAssistLease();
        lease.Enable(actor);
        actor.ThrowOnRemove = true;

        Assert.Throws<InvalidOperationException>(lease.Disable);
        Assert.False(lease.IsEnabled);
        Assert.True(lease.HasPendingCleanup);

        actor.ThrowOnRemove = false;
        lease.Disable();

        Assert.False(lease.HasPendingCleanup);
        Assert.Equal(2, actor.RemoveCalls);
    }

    [Fact]
    public void AddThatMutatesThenThrowsIsImmediatelyPairedWithRemoval()
    {
        var actor = FakeSurvivalPort.AtMaximum();
        actor.ThrowAfterAdd = true;
        var lease = new SurvivalAssistLease();

        Assert.Throws<InvalidOperationException>(() => lease.Enable(actor));

        Assert.False(lease.IsEnabled);
        Assert.False(lease.HasPendingCleanup);
        Assert.Equal(1, actor.AddCalls);
        Assert.Equal(1, actor.RemoveCalls);
    }

    [Fact]
    public void FailedCleanupAfterThrowingAddRemainsRetryable()
    {
        var actor = FakeSurvivalPort.AtMaximum();
        actor.ThrowAfterAdd = true;
        actor.ThrowOnRemove = true;
        var lease = new SurvivalAssistLease();

        Assert.Throws<InvalidOperationException>(() => lease.Enable(actor));
        Assert.True(lease.HasPendingCleanup);
        Assert.False(lease.IsEnabled);

        actor.ThrowOnRemove = false;
        lease.Disable();

        Assert.False(lease.HasPendingCleanup);
        Assert.Equal(2, actor.RemoveCalls);
    }

    private sealed class FakeSurvivalPort : ISurvivalAssistPort
    {
        private Action damageTaken;
        private float health;
        private float oxygen;
        private float stamina;

        public static FakeSurvivalPort AtMaximum()
        {
            var actor = new FakeSurvivalPort
            {
                Health = 20f,
                MaxHealth = 20f,
                Oxygen = 10f,
                MaxOxygen = 10f,
                Stamina = 8f,
                MaxStamina = 8f
            };
            actor.ResetWriteCounts();
            return actor;
        }

        public event Action DamageTaken
        {
            add
            {
                AddCalls++;
                LastAddedHandler = value;
                damageTaken += value;
                if (ThrowAfterAdd)
                    throw new InvalidOperationException("Add failed after mutation.");
            }
            remove
            {
                RemoveCalls++;
                if (ThrowOnRemove)
                    throw new InvalidOperationException("Remove failed.");
                damageTaken -= value;
            }
        }

        public int AddCalls { get; private set; }

        public int RemoveCalls { get; private set; }

        public Action LastAddedHandler { get; private set; }

        public bool IsUnavailable { get; set; }

        public bool ThrowOnHealthWrite { get; set; }

        public bool IgnoreHealthWrite { get; set; }

        public bool ThrowOnRemove { get; set; }

        public bool ThrowAfterAdd { get; set; }

        public bool RaiseDamageOnHealthWrite { get; set; }

        public int HealthWrites { get; private set; }

        public int OxygenWrites { get; private set; }

        public int StaminaWrites { get; private set; }

        public float Health
        {
            get => health;
            set
            {
                HealthWrites++;
                if (ThrowOnHealthWrite)
                    throw new InvalidOperationException("Health write failed.");
                if (!IgnoreHealthWrite)
                    health = value;
                if (RaiseDamageOnHealthWrite)
                    RaiseDamageTaken();
            }
        }

        public float MaxHealth { get; set; }

        public float Oxygen
        {
            get => oxygen;
            set
            {
                OxygenWrites++;
                oxygen = value;
            }
        }

        public float MaxOxygen { get; set; }

        public float Stamina
        {
            get => stamina;
            set
            {
                StaminaWrites++;
                stamina = value;
            }
        }

        public float MaxStamina { get; set; }

        public void RaiseDamageTaken() => damageTaken?.Invoke();

        public void ResetWriteCounts()
        {
            HealthWrites = 0;
            OxygenWrites = 0;
            StaminaWrites = 0;
        }
    }
}
