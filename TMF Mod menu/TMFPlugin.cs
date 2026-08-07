using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using StudioForge.BlockWorld;
using StudioForge.Engine;
using StudioForge.TotalMiner.API;
using TMFModMenu.Features.Flight;
using TMFModMenu.Features.Inventory;
using TMFModMenu.Features.Survival;
using TMFModMenu.Menu;

namespace TMFModMenu
{
    public sealed class TMFPlugin : ITMPlugin
    {

        private ITMGame game;
        private Dictionary<PlayerIndex, MenuSession> sessions = new();
        private FlightService flight = new();
        private InventoryService inventory = new();
        private SurvivalService survival = new();
        private MenuRenderer renderer = new();


        public void Initialize(
      ITMPluginManager mgr,
      ITMMod mod)
        {
            sessions = new Dictionary<PlayerIndex, MenuSession>();
            flight = new FlightService();
            inventory = new InventoryService();
            survival = new SurvivalService();
            renderer = new MenuRenderer();
        }


        public void InitializeGame(ITMGame game)
        {
            this.game = game;
            CloseAllSessions();
            sessions.Clear();
            flight.Initialize(game);
            inventory.Initialize(game);
            survival.Initialize(game);

            game.AddNotification("TMF Mod Loaded");
        }


        public bool HandleInput(ITMPlayer player)
        {
            if (game == null || player == null)
                return false;

            if (!player.IsInputEnabled || player.IsDeadOrInactiveOrDisabled)
            {
                if (sessions.TryGetValue(player.PlayerIndex, out var inactiveSession))
                    inactiveSession.Close();
                return false;
            }

            if (!sessions.TryGetValue(player.PlayerIndex, out var session))
            {
                session = new MenuSession(inventory, flight, survival);
                sessions.Add(player.PlayerIndex, session);
            }

            var inputResult = session.Input.Poll(player, session.Menu.IsOpen);
            if (inputResult.Command != MenuCommand.None)
                session.Handle(player, inputResult.Command);

            bool flightConsumed = flight.HandleInput(
                player,
                session.Menu.IsOpen || inputResult.IsConsumed);
            return inputResult.IsConsumed || flightConsumed;
        }


        public void Update()
        {

        }


        public void Update(ITMPlayer player)
        {
            flight.Update(player);
            survival.Update(player);
        }


        public void Draw(
            ITMPlayer player,
            ITMPlayer virtualPlayer,
            Viewport vp)
        {
            if (game == null || renderer == null || player == null ||
                virtualPlayer == null || !virtualPlayer.GamerID.IsGamer ||
                virtualPlayer.GamerID != player.GamerID ||
                !player.IsInputEnabled || player.IsDeadOrInactiveOrDisabled ||
                !sessions.TryGetValue(player.PlayerIndex, out var session) ||
                !session.Menu.IsOpen)
                return;

            var titleFont = CoreGlobals.GameFont20 ?? CoreGlobals.GameFont16;
            var rowFont = CoreGlobals.GameFont16 ?? titleFont;
            var smallFont = CoreGlobals.GameFont12 ?? rowFont;
            renderer.Draw(
                game.SpriteBatch,
                CoreGlobals.BlankTexture,
                titleFont,
                rowFont,
                smallFont,
                vp,
                session.Snapshot);
        }


        public void UnloadMod()
        {
            CloseAllSessions();
            sessions.Clear();
            flight.Clear();
            inventory.Clear();
            survival.Clear();
            game = null;
        }


        public object[] RegisterLuaFunctions(
            ITMScriptInstance si)
        {
            return System.Array.Empty<object>();
        }


        public void PlayerJoined(
            ITMPlayer player)
        {

        }


        public void PlayerLeft(
            ITMPlayer player)
        {
            flight.PlayerLeft(player);
            survival.PlayerLeft(player);
            if (player != null && sessions.Remove(player.PlayerIndex, out var session))
                session.Close();
        }


        public void WorldSaved(int version)
        {

        }


        public void Callback(
            string data,
            GlobalPoint3D? p,
            ITMActor actor,
            ITMActor contextActor)
        {

        }


        private void CloseAllSessions()
        {
            foreach (var session in sessions.Values)
            {
                try
                {
                    session.Close();
                }
                catch
                {
                    // One failed control-lease restoration must not prevent
                    // feature cleanup for other local players or F8 unload.
                }
            }
        }

    }
}
