using Microsoft.Xna.Framework.Graphics;
using StudioForge.BlockWorld;
using StudioForge.TotalMiner.API;
using TMFModMenu.Menu;

namespace TMFModMenu
{
    public sealed class TMFPlugin : ITMPlugin
    {

        private ITMGame game;
        private MenuManager menu;


        public void Initialize(
      ITMPluginManager mgr,
      ITMMod mod)
        {
            menu = new MenuManager();

            System.IO.File.WriteAllText(
                "TMF_TEST.txt",
                "TMF Plugin Loaded"
            );
        }


        public void InitializeGame(ITMGame game)
        {
            this.game = game;

            game.AddNotification("TMF Mod Loaded");
        }


        public bool HandleInput(ITMPlayer player)
        {
            menu.HandleInput();

            return false;
        }


        public void Update()
        {

        }


        public void Update(ITMPlayer player)
        {

        }


        public void Draw(
            ITMPlayer player,
            ITMPlayer virtualPlayer,
            Viewport vp)
        {

        }


        public void UnloadMod()
        {

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

    }
}