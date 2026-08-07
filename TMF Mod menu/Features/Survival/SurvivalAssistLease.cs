using System;

namespace TMFModMenu.Features.Survival;

internal interface ISurvivalAssistPort
{
    event Action DamageTaken;

    bool IsUnavailable { get; }

    float Health { get; set; }

    float MaxHealth { get; }

    float Oxygen { get; set; }

    float MaxOxygen { get; }

    float Stamina { get; set; }

    float MaxStamina { get; }
}

internal enum SurvivalAssistStopReason
{
    None,
    InactiveOrDead,
    CallbackFailure
}

internal sealed class SurvivalAssistLease
{
    private readonly Action<SurvivalAssistStopReason> stopped;
    private ISurvivalAssistPort actor;
    private bool damageSubscribed;
    private bool isRefreshing;

    public bool IsEnabled { get; private set; }

    public bool HasPendingCleanup => damageSubscribed;

    public SurvivalAssistStopReason StopReason { get; private set; }

    public SurvivalAssistLease(
        Action<SurvivalAssistStopReason> stopped = null)
    {
        this.stopped = stopped;
    }

    public bool Enable(ISurvivalAssistPort actor)
    {
        if (actor == null)
            throw new ArgumentNullException(nameof(actor));
        if (IsEnabled)
            return true;
        if (HasPendingCleanup)
            throw new InvalidOperationException(
                "The previous Survival Assist subscription is still pending cleanup.");

        this.actor = actor;
        StopReason = SurvivalAssistStopReason.None;
        damageSubscribed = true;
        try
        {
            actor.DamageTaken += OnDamageTaken;
            IsEnabled = true;
            return Refresh();
        }
        catch
        {
            StopReason = SurvivalAssistStopReason.CallbackFailure;
            IsEnabled = false;
            TryDetachWithoutThrowing();
            throw;
        }
    }

    public bool Refresh()
    {
        if (!IsEnabled)
            return false;
        if (isRefreshing)
            return true;

        isRefreshing = true;
        try
        {
            if (actor.IsUnavailable || actor.Health <= 0f)
            {
                Stop(SurvivalAssistStopReason.InactiveOrDead);
                return false;
            }

            RefillVitals(actor);
            return true;
        }
        finally
        {
            isRefreshing = false;
        }
    }

    public void Disable()
    {
        IsEnabled = false;
        if (!damageSubscribed)
        {
            actor = null;
            return;
        }

        actor.DamageTaken -= OnDamageTaken;
        damageSubscribed = false;
        actor = null;
    }

    private void OnDamageTaken()
    {
        try
        {
            Refresh();
        }
        catch
        {
            // This callback executes inside host damage processing. Never let
            // a mod setter/subscription failure escape into that pipeline.
            Stop(SurvivalAssistStopReason.CallbackFailure);
        }
    }

    private void Stop(SurvivalAssistStopReason reason)
    {
        StopReason = reason;
        IsEnabled = false;
        TryDetachWithoutThrowing();
        try
        {
            stopped?.Invoke(reason);
        }
        catch
        {
            // A UI observer is never allowed to escape into host damage logic.
        }
    }

    private void TryDetachWithoutThrowing()
    {
        try
        {
            Disable();
        }
        catch
        {
            // Keep the port and pending flag so the service can retry outside
            // the host's damage callback.
        }
    }

    private static void RefillVitals(ISurvivalAssistPort actor)
    {
        Exception firstError = null;
        TryRefill(
            () => actor.Health,
            value => actor.Health = value,
            () => actor.MaxHealth,
            "health",
            ref firstError);
        TryRefill(
            () => actor.Oxygen,
            value => actor.Oxygen = value,
            () => actor.MaxOxygen,
            "oxygen",
            ref firstError);
        TryRefill(
            () => actor.Stamina,
            value => actor.Stamina = value,
            () => actor.MaxStamina,
            "stamina",
            ref firstError);

        if (firstError != null)
            throw firstError;
    }

    private static void TryRefill(
        Func<float> readCurrent,
        Action<float> writeCurrent,
        Func<float> readMaximum,
        string name,
        ref Exception firstError)
    {
        try
        {
            float maximum = readMaximum();
            float current = readCurrent();
            if (!float.IsFinite(maximum) || maximum <= 0f ||
                (float.IsFinite(current) && current >= maximum))
                return;

            writeCurrent(maximum);
            float effective = readCurrent();
            if (!float.IsFinite(effective) || effective < maximum)
                throw new InvalidOperationException(
                    $"The host rejected the Survival Assist {name} refill.");
        }
        catch (Exception ex)
        {
            firstError ??= ex;
        }
    }
}
