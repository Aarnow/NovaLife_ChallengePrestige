using Life;
using ModKit.Helper;
using ModKit.Interfaces;
using ModKit.Internal;


namespace ChallengePrestige
{
    public class Prestige : ModKit.ModKit
    {

        public Prestige(IGameAPI api) : base(api)
        {
            PluginInformations = new PluginInformations(AssemblyHelper.GetName(), "1.0.0", "Aarnow");
        }

        public override void OnPluginInit()
        {
            base.OnPluginInit();


            Logger.LogSuccess($"{PluginInformations.SourceName} v{PluginInformations.Version}", "initialisé");
        }
    }
}
