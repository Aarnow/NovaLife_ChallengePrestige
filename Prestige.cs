using Life;
using ModKit.Helper;
using ModKit.Interfaces;
using ModKit.Internal;
using System.IO;
using System.Collections.Generic;
using ChallengePrestige.Classes;
using Newtonsoft.Json;


namespace ChallengePrestige
{
    public class Prestige : ModKit.ModKit
    {
        public static string ConfigDirectoryPath;
        public static string ConfigFilePath;
        private readonly Events events;

        public Prestige(IGameAPI api) : base(api)
        {
            PluginInformations = new PluginInformations(AssemblyHelper.GetName(), "1.0.0", "Aarnow");
            events = new Events(api);
        }

        public override void OnPluginInit()
        {
            base.OnPluginInit();
            InitDirectory();
            events.Init(Nova.server);
            InitPoint();
            Logger.LogSuccess($"{PluginInformations.SourceName} v{PluginInformations.Version}", "initialisé");
        }

        public void InitPoint()
        {
            Orm.RegisterTable<ChallengePrestigeTask>();

            Orm.RegisterTable<ChallengePrestigeTasksPoint>();
            PointHelper.AddPattern("ChallengePrestigeTasksPoint", new ChallengePrestigeTasksPoint(false));
            AAMenu.AAMenu.menu.AddBuilder(PluginInformations, "ChallengePrestigeTasksPoint", new ChallengePrestigeTasksPoint(false), this);

            Orm.RegisterTable<ChallengePrestigeShopPoint>();
            PointHelper.AddPattern("ChallengePrestigeShopPoint", new ChallengePrestigeShopPoint(false));
            AAMenu.AAMenu.menu.AddBuilder(PluginInformations, "ChallengePrestigeShopPoint", new ChallengePrestigeShopPoint(false), this);
        }

        private void InitDirectory()
        {
            try
            {
                ConfigDirectoryPath = DirectoryPath + "/ChallengePrestige";
                ConfigFilePath = Path.Combine(ConfigDirectoryPath, "tasks.json");

                // Vérifiez si le répertoire parent existe, sinon créez-le
                if (!Directory.Exists(ConfigDirectoryPath))
                {
                    Directory.CreateDirectory(ConfigDirectoryPath);
                    InitFile(ConfigFilePath);
                } else InitFile(ConfigFilePath);
            }
            catch (IOException ex)
            {
                Logger.LogError("InitDirectory", ex.Message);
            }
        }

        private void InitFile(string filePath)
        {
            if (!File.Exists(filePath))
            {
                List<TaskItem> tasks = new List<TaskItem>
                {
                    new TaskItem(29, 5, 9),
                    new TaskItem(30, 2, 6),
                    new TaskItem(33, 5, 9),
                    new TaskItem(136, 2, 6)
                };
                string json = JsonConvert.SerializeObject(tasks, Formatting.Indented);
                File.WriteAllText(filePath, json);
            }
        }

    }
}
