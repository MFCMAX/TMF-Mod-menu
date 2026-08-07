using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using StudioForge.Engine.Core;
using StudioForge.TotalMiner;
using StudioForge.TotalMiner.API;
using TMFModMenu.Menu;

namespace TMFModMenu.Features.Flight;

internal sealed class FlightService : IFlightMenuProvider
{
    private enum FlightPhase
    {
        Active,
        LandingPending
    }

    private sealed class PlayerFlightState
    {
        public PlayerFlightState(
            ITMPlayer player,
            FlightActorPort actor,
            FlightLease lease,
            MenuToggleItem menuItem)
        {
            Player = player;
            Actor = actor;
            Lease = lease;
            MenuItem = menuItem;
        }

        public ITMPlayer Player { get; }

        public FlightActorPort Actor { get; }

        public FlightLease Lease { get; }

        public MenuToggleItem MenuItem { get; }

        public FlightControlGate ControlGate { get; } = new();

        public FlightPhase Phase { get; set; } = FlightPhase.Active;

        public bool CleanupWarningSent { get; set; }
    }

    private sealed class FlightActorPort : IFlightActorPort
    {
        private readonly ITMPlayer player;

        public FlightActorPort(ITMPlayer player)
        {
            this.player = player;
        }

        public float GravityOverride
        {
            get => player.GravityOverride;
            set => player.GravityOverride = value;
        }

        public float SpeedOverride
        {
            get => player.SpeedOverride;
            set => player.SpeedOverride = value;
        }

        public FlightVector Velocity
        {
            get => new(
                player.Velocity.X,
                player.Velocity.Y,
                player.Velocity.Z);
            set => player.Velocity = new Vector3(value.X, value.Y, value.Z);
        }

        public bool IsOnGround => player.IsOnGround;
    }

    private readonly Dictionary<PlayerIndex, PlayerFlightState> states = new();
    private ITMGame game;

    public void Initialize(ITMGame game)
    {
        Clear();
        this.game = game;
    }

    public MenuItem CreateFlightMenuItem()
    {
        MenuToggleItem menuItem = null;
        menuItem = new MenuToggleItem(
            "Flight",
            change: (context, requested) => Change(
                context.Player,
                requested,
                menuItem));
        return menuItem;
    }

    public bool HandleInput(ITMPlayer player, bool suppressForMenu)
    {
        try
        {
            if (!IsLocalPlayer(player) ||
                !states.TryGetValue(player.PlayerIndex, out var state) ||
                !ReferenceEquals(state.Player, player) ||
                !state.Lease.IsEnabled)
                return false;

            if (state.Phase == FlightPhase.LandingPending)
            {
                ArmLanding(state);
                return false;
            }

            bool ascendPressed = InputManager1.IsInputPressed(
                player.PlayerIndex,
                PlayerInput.FlyAscend);
            bool descendPressed = InputManager1.IsInputPressed(
                player.PlayerIndex,
                PlayerInput.FlyDescend);
            int vertical = state.ControlGate.ResolveVertical(
                suppressForMenu,
                ascendPressed,
                descendPressed);
            state.Lease.ArmVerticalMotion(
                state.Actor,
                StandaloneFlightMotion.DesiredVerticalVelocity(vertical));
            return vertical != 0;
        }
        catch
        {
            CleanupAfterCallbackFailure(player);
            Notify("TMF Flight: input failed; cleanup was attempted.");
            return false;
        }
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
            Notify("TMF Flight: update failed; cleanup was attempted.");
        }
    }

    public void PlayerLeft(ITMPlayer player)
    {
        if (player == null ||
            !states.TryGetValue(player.PlayerIndex, out var state) ||
            !ReferenceEquals(state.Player, player))
            return;

        TryDisableState(state);
        states.Remove(player.PlayerIndex);
    }

    public void Clear()
    {
        foreach (var state in states.Values)
            TryDisableState(state);
        states.Clear();
        game = null;
    }

    private void UpdateCore(ITMPlayer player)
    {
        if (!IsLocalPlayer(player) ||
            !states.TryGetValue(player.PlayerIndex, out var state) ||
            !ReferenceEquals(state.Player, player))
            return;

        if (state.Lease.HasPendingRestore && !state.Lease.IsEnabled)
        {
            if (TryDisableState(state))
            {
                states.Remove(player.PlayerIndex);
                Notify("TMF Flight: pending cleanup completed.");
            }
            else
                ReportCleanupPending(state);
            return;
        }

        if (player.IsDeadOrInactiveOrDisabled)
        {
            if (TryDisableState(state))
            {
                states.Remove(player.PlayerIndex);
                Notify("TMF Flight: disabled because the player became inactive.");
            }
            else
                ReportCleanupPending(state);
            return;
        }

        if (state.Phase == FlightPhase.LandingPending)
        {
            if (state.Actor.IsOnGround)
            {
                if (TryDisableState(state))
                {
                    states.Remove(player.PlayerIndex);
                    Notify("TMF Flight: landing complete; normal movement restored.");
                }
                else
                    ReportCleanupPending(state);
            }
            else
                ArmLanding(state);
            return;
        }

        if (!player.IsInputEnabled)
            state.ControlGate.SuppressUntilNeutral();

        // Update runs after physics. Arm neutral Y only for the next frame so
        // a skipped input callback cannot repeat stale ascent/descent. Never
        // restore user motion or X/Z after collision has resolved.
        state.Lease.ArmVerticalMotion(state.Actor, 0f);
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
            Notify("TMF Flight: request failed; cleanup was attempted.");
            return false;
        }
    }

    private bool ChangeCore(
        object playerObject,
        bool requested,
        MenuToggleItem menuItem)
    {
        if (playerObject is not ITMPlayer player)
        {
            Notify("TMF Flight: request rejected; local player unavailable.");
            return false;
        }

        if (!IsLocalPlayer(player))
        {
            if (!requested &&
                states.TryGetValue(player.PlayerIndex, out var unavailable) &&
                ReferenceEquals(unavailable.Player, player))
            {
                if (TryDisableState(unavailable))
                    states.Remove(player.PlayerIndex);
                else
                    ReportCleanupPending(unavailable);
            }
            Notify("TMF Flight: request rejected; local player unavailable.");
            return false;
        }

        if (!requested)
        {
            if (states.TryGetValue(player.PlayerIndex, out var existing) &&
                ReferenceEquals(existing.Player, player))
            {
                existing.MenuItem.SetState(false);
                existing.ControlGate.SuppressUntilNeutral();
                if (!existing.Lease.IsEnabled || existing.Actor.IsOnGround)
                {
                    if (TryDisableState(existing))
                    {
                        states.Remove(player.PlayerIndex);
                        Notify("TMF Flight: normal movement restored.");
                    }
                    else
                        ReportCleanupPending(existing);
                }
                else
                {
                    existing.Phase = FlightPhase.LandingPending;
                    ArmLanding(existing);
                    Notify("TMF Flight: landing safely before restoring normal gravity.");
                }
            }
            return false;
        }

        if (player.IsDeadOrInactiveOrDisabled)
        {
            Notify("TMF Flight: unavailable while the player is inactive.");
            return false;
        }

        if (states.TryGetValue(player.PlayerIndex, out var stale))
        {
            if (ReferenceEquals(stale.Player, player) && stale.Lease.IsEnabled)
            {
                bool cancelledLanding = stale.Phase == FlightPhase.LandingPending;
                stale.Phase = FlightPhase.Active;
                stale.ControlGate.SuppressUntilNeutral();
                if (cancelledLanding)
                    Notify("TMF Flight: controlled landing cancelled.");
                return true;
            }

            if (!TryDisableState(stale))
            {
                ReportCleanupPending(stale);
                return false;
            }
            states.Remove(player.PlayerIndex);
        }

        var actor = new FlightActorPort(player);
        var lease = new FlightLease();
        var state = new PlayerFlightState(player, actor, lease, menuItem);
        try
        {
            lease.Enable(actor);
            state.ControlGate.SuppressUntilNeutral();
            states.Add(player.PlayerIndex, state);
            Notify("TMF Flight: standalone custom flight enabled; no permission or item required.");
            return true;
        }
        catch
        {
            if (TryDisableState(state))
                Notify("TMF Flight: enable failed; original gravity restored.");
            else
            {
                states[player.PlayerIndex] = state;
                ReportCleanupPending(state);
            }
            return false;
        }
    }

    private static void ArmLanding(PlayerFlightState state) =>
        state.Lease.ArmVerticalMotion(
            state.Actor,
            -StandaloneFlightMotion.VerticalSpeed);

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

    private static bool TryDisableState(PlayerFlightState state)
    {
        state.MenuItem.SetState(false);
        state.ControlGate.SuppressUntilNeutral();
        for (int attempt = 0; attempt < 2; attempt++)
        {
            try
            {
                state.Lease.Disable(state.Actor);
                return !state.Lease.HasPendingRestore;
            }
            catch
            {
                // Retry once while the actor is still valid. The lease keeps
                // the original gravity snapshot until readback succeeds.
            }
        }
        return !state.Lease.HasPendingRestore;
    }

    private void ReportCleanupPending(PlayerFlightState state)
    {
        if (state.CleanupWarningSent)
            return;

        state.CleanupWarningSent = true;
        Notify("TMF Flight: movement cleanup is pending and will be retried.");
    }

    private void Notify(string message)
    {
        try
        {
            game?.AddNotification(message);
        }
        catch
        {
            // Notifications are never allowed to break feature cleanup.
        }
    }
}
