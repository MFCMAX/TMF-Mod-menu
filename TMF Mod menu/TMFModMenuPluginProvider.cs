using StudioForge.TotalMiner.API;

namespace TMFModMenu
{
    public sealed class PluginProvider : ITMPluginProvider
    {

        public ITMPlugin GetPlugin()
        {
            return new TMFPlugin();
        }


        public ITMPluginArcade GetPluginArcade() => null;
        public ITMPluginBlocks GetPluginBlocks() => null;
        public ITMPluginGUI GetPluginGUI() => null;
        public ITMPluginNet GetPluginNet() => null;
        public ITMPluginBiome GetPluginBiome() => null;
        public ITMPluginConfig GetPluginConfig() => null;

    }
}