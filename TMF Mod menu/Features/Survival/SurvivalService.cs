using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using StudioForge.TotalMiner;
using StudioForge.TotalMiner.API;
using TMFModMenu.Menu;

namespace TMFModMenu.Features.Survival;

internal sealed class SurvivalService : ISurvivalMenuProvider
{
    private sealed class PlayerSurvivalState
    {
        public PlayerSurvivalState(
            ITMPlayer player,
            SurvivalAssistLease lease,
            MenuToggleItem menuItem)
        {
            Player = player;
            Lease = lease;
            MenuItem = menuItem;
        }

        public ITMPlayer Player { get; }

        public SurvivalAssistLease Lease { get; }

        public MenuToggleItem MenuItem { get; }

        public bool CleanupWarningSent { get; set; }
    }

    private sealed class SurvivalPlayerPort : ISurvivalAssistPort
    {
        private readonly ITMPlayer player;
        private readonly Func<bool> isCurrentLocalPlayer;
        private Action damageTaken;
        private bool bridgeAttached;

        public SurvivalPlayerPort(
            ITMPlayer player,
            Func<bool> isCurrentLocalPlayer)
        {
            this.player = player;
            this.isCurrentLocalPlayer = isCurrentLocalPlayer;
        }

        public event Action DamageTaken
        {
            add
            {
                if (value == null)
                    return;

                damageTaken += value;
                if (bridgeAttached)
                    return;

                // Mark first so a host add that mutates and then throws can
                // still be paired with a removal during cleanup.
                bridgeAttached = true;
                player.DamageTaken += ForwardDamageTaken;
            }
            remove
            {
                if (value != null)
                    damageTaken -= value;
                if (!bridgeAttached || damageTaken != null)
                    return;

                player.DamageTaken -= ForwardDamageTaken;
                bridgeAttached = false;
            }
        }

        public bool IsUnavailable =>
            !isCurrentLocalPlayer() ||
            player.IsDeadOrInactiveOrDisabled ||
            player.ActorState != ActorState.Alive ||
            player.Health <= 0f;

        public float Health
        {
            get => player.Health;
            set => player.Health = value;
        }

        public float MaxHealth => player.MaxHealth;

        public float Oxygen
        {
            get => player.Oxygen;
            set => player.Oxygen = value;
        }

        public float MaxOxygen => player.MaxOxygen;

        public float Stamina
        {
            get => player.Stamina;
            set => player.Stamina = value;
        }

        public float MaxStamina => player.MaxStamina;

        private void ForwardDamageTaken(object sender, ActorEventArgs e) =>
            damageTaken?.Invoke();
    }

    private readonly Dictionary<PlayerIndex, PlayerSurvivalState> states = new();
    private ITMGame game;

    public void Initialize(ITMGame game)
    {
        Clear();
        this.game = game;
    }

    public MenuItem CreateSurvivalMenuItem()
    {
        MenuToggleItem menuItem = null;
        menuItem = new MenuToggleItem(
            "Survival Assist",
            change: (context, requested) => Change(
                context.Player,
                requested,
                menuItem));
        return menuItem;
    }

    public void Update(ITMPlayer player)
    {
        try
        {
            UpdateCore(player);
        }
        catch
        {
            CleanupAfterCallbackFailure(player);
            Notify("TMF Survival Assist: update failed; cleanup was attempted.");
        }
    }

    public void PlayerLeft(ITMPlayer player)
    {
        if (player == null ||
            !states.TryGetValue(player.PlayerIndex, out var state) ||
            !ReferenceEquals(state.Player, player))
            return;

        if (TryDisableState(state))
            states.Remove(player.PlayerIndex);
        else
            ReportCleanupPending(state);
    }

    public void Clear()
    {
        var cleaned = new List<PlayerIndex>();
        foreach (var pair in states)
        {
            if (TryDisableState(pair.Value))
                cleaned.Add(pair.Key);
            else
                ReportCleanupPending(pair.Value);
        }
        foreach (var playerIndex in cleaned)
            states.Remove(playerIndex);
        game = null;
    }

    private void UpdateCore(ITMPlayer player)
    {
        if (player == null ||
            !states.TryGetValue(player.PlayerIndex, out var state))
            return;

        bool samePlayer = ReferenceEquals(state.Player, player);
        bool isLocalPlayer = IsLocalPlayer(player);
        if (!samePlayer && !isLocalPlayer)
            return;

        if (!samePlayer || !isLocalPlayer)
        {
            if (TryDisableState(state))
                states.Remove(player.PlayerIndex);
            else
                ReportCleanupPending(state);
            return;
        }

        if (!state.Lease.IsEnabled)
        {
            FinishStoppedState(player.PlayerIndex, state);
            return;
        }

        if (!state.Lease.Refresh())
            FinishStoppedState(player.PlayerIndex, state);
    }

    private bool Change(
        object playerObject,
        bool requested,
        MenuToggleItem menuItem)
    {
        try
        {
            return ChangeCore(playerObject, requested, menuItem);
        }
        catch
        {
            if (playerObject is ITMPlayer player)
                CleanupAfterCallbackFailure(player);
            Notify("TMF Survival Assist: request failed; cleanup was attempted.");
            return false;
        }
    }

    private bool ChangeCore(
        object playerObject,
        bool requested,
        MenuToggleItem menuItem)
    {
        if (playerObject is not ITMPlayer player || !IsLocalPlayer(player))
        {
            Notify("TMF Survival Assist: local player unavailable.");
            return false;
        }

        if (!requested)
        {
            if (states.TryGetValue(player.PlayerIndex, out var existing))
            {
                if (TryDisableState(existing))
                {
                    states.Remove(player.PlayerIndex);
                    Notify("TMF Survival Assist: disabled; current vitals were left unchanged.");
                }
                else
                    ReportCleanupPending(existing);
            }
            return false;
        }

        if (player.IsDeadOrInactiveOrDisabled ||
            player.ActorState != ActorState.Alive ||
            player.Health <= 0f)
        {
            Notify("TMF Survival Assist: unavailable while the player is inactive.");
            return false;
        }

        if (states.TryGetValue(player.PlayerIndex, out var stale))
        {
            if (ReferenceEquals(stale.Player, player) && stale.Lease.IsEnabled)
                return true;

            if (!TryDisableState(stale))
            {
                ReportCleanupPending(stale);
                return false;
            }
            states.Remove(player.PlayerIndex);
        }

        var lease = new SurvivalAssistLease(_ => menuItem.SetState(false));
        var state = new PlayerSurvivalState(player, lease, menuItem);
        try
        {
            bool enabled = lease.Enable(new SurvivalPlayerPort(
                player,
                () => IsLocalPlayer(player)));
            if (!enabled)
            {
                if (!TryDisableState(state))
                {
                    states[player.PlayerIndex] = state;
                    ReportCleanupPending(state);
                }
                Notify("TMF Survival Assist: player became unavailable during enable.");
                return false;
            }

            states.Add(player.PlayerIndex, state);
            Notify(
                "TMF Survival Assist: nonlethal vitals refill enabled; one-hit lethal damage can still kill.");
            return true;
        }
        catch
        {
            if (TryDisableState(state))
                Notify("TMF Survival Assist: enable failed; event cleanup completed.");
            else
            {
                states[player.PlayerIndex] = state;
                ReportCleanupPending(state);
            }
            return false;
        }
    }

    private void FinishStoppedState(
        PlayerIndex playerIndex,
        PlayerSurvivalState state)
    {
        var reason = state.Lease.StopReason;
        if (!TryDisableState(state))
        {
            ReportCleanupPending(state);
            return;
        }

        states.Remove(playerIndex);
        if (reason == SurvivalAssistStopReason.InactiveOrDead)
        {
            Notify(
                "TMF Survival Assist: stopped after death/inactivity; lethal damage is not prevented.");
        }
        else if (reason == SurvivalAssistStopReason.CallbackFailure)
        {
            Notify("TMF Survival Assist: stopped after a host callback failure.");
        }
    }

    private bool IsLocalPlayer(ITMPlayer player) =>
        game != null &&
        player != null &&
        ReferenceEquals(game.GetLocalPlayer(player.PlayerIndex), player);

    private void CleanupAfterCallbackFailure(ITMPlayer player)
    {
        if (player == null ||
            !states.TryGetValue(player.PlayerIndex, out var state) ||
            !ReferenceEquals(state.Player, player))
            return;

        if (TryDisableState(state))
            states.Remove(player.PlayerIndex);
        else
            ReportCleanupPending(state);
    }

    private static bool TryDisableState(PlayerSurvivalState state)
    {
        state.MenuItem.SetState(false);
        for (int attempt = 0; attempt < 2; attempt++)
        {
            try
            {
                state.Lease.Disable();
                return !state.Lease.HasPendingCleanup;
            }
            catch
            {
                // Retry once while the player/event source remains valid.
            }
        }
        return !state.Lease.HasPendingCleanup;
    }

    private void ReportCleanupPending(PlayerSurvivalState state)
    {
        if (state.CleanupWarningSent)
            return;

        state.CleanupWarningSent = true;
        Notify("TMF Survival Assist: event cleanup is pending and will be retried.");
    }

    private void Notify(string message)
    {
        try
        {
            game?.AddNotification(message);
        }
        catch
        {
            // Notifications never participate in feature state changes.
        }
    }
}
