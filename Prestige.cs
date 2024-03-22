using Life;
using ModKit.Helper;
using ModKit.Interfaces;
using ModKit.Internal;
using System.IO;
using System.Collections.Generic;
using ChallengePrestige.Classes;
using ChallengePrestige.Entities;
using Newtonsoft.Json;
using ChallengePrestige.Points;
using Life.Network;


namespace ChallengePrestige
{
    public class Prestige : ModKit.ModKit
    {
        public static string ConfigDirectoryPath;
        public static string ConfigTaskFilePath;
        public static string ConfigRegisterFilePath;
        public static Register ConfigRegister { get; set; }

        private readonly Events events;

        public Prestige(IGameAPI api) : base(api)
        {
            PluginInformations = new PluginInformations(AssemblyHelper.GetName(), "1.0.0", "Aarnow");
            events = new Events(api);
        }

        public override void OnPluginInit()
        {
            base.OnPluginInit();
            InitDirectoryAndFiles();
            InitConfig();
            events.Init(Nova.server);
            InitPoint();

            new SChatCommand("/prestige", "Permet d'éditer le prestige d'un joueur", "/prestige playerId value", async (player, arg) =>
            {
                if (player.IsAdmin && player.account.adminLevel >= 5)
                {
                    if (arg[0] != null && uint.TryParse(arg[0], out uint playerId))
                    {
                        if (arg[1] != null && int.TryParse(arg[1], out int value))
                        {
                            var currentPlayer = await ChallengePrestigePlayer.Query(p => p.PlayerId == playerId);
                            if(currentPlayer.Count > 0)
                            {
                                currentPlayer[0].Prestige += value;
                                await currentPlayer[0].Save();
                                player.Notify("Succès", $"Le joueur ID:{playerId} est désormais Prestige {currentPlayer[0].Prestige}", NotificationManager.Type.Success);
                            }
                        }
                        else player.Notify("Erreur", "argument \"value\" incorrect", NotificationManager.Type.Error);
                    } else player.Notify("Erreur", "argument \"playerId\" incorrect", NotificationManager.Type.Error);
                }
                else player.Notify("Erreur", "Absence d'arguments", NotificationManager.Type.Error);
            }).Register();

            Logger.LogSuccess($"{PluginInformations.SourceName} v{PluginInformations.Version}", "initialisé");
        }

        public void InitPoint()
        {
            Orm.RegisterTable<ChallengePrestigeTask>();
            Orm.RegisterTable<ChallengePrestigePlayer>();
            Orm.RegisterTable<ChallengePrestigeReward>();
            Orm.RegisterTable<ChallengePrestigeSponsorship>();

            Orm.RegisterTable<ChallengePrestigeTasksPoint>();
            PointHelper.AddPattern("ChallengePrestigeTasksPoint", new ChallengePrestigeTasksPoint(false));
            AAMenu.AAMenu.menu.AddBuilder(PluginInformations, "ChallengePrestigeTasksPoint", new ChallengePrestigeTasksPoint(false), this);

            Orm.RegisterTable<ChallengePrestigeRewardPoint>();
            PointHelper.AddPattern("ChallengePrestigeRewardPoint", new ChallengePrestigeRewardPoint(false));
            AAMenu.AAMenu.menu.AddBuilder(PluginInformations, "ChallengePrestigeRewardPoint", new ChallengePrestigeRewardPoint(false), this);

            Orm.RegisterTable<ChallengePrestigeSponsorshipPoint>();
            PointHelper.AddPattern("ChallengePrestigeSponsorshipPoint", new ChallengePrestigeSponsorshipPoint(false));
            AAMenu.AAMenu.menu.AddBuilder(PluginInformations, "ChallengePrestigeSponsorshipPoint", new ChallengePrestigeSponsorshipPoint(false), this);
        }

        private void InitDirectoryAndFiles()
        {
            try
            {
                ConfigDirectoryPath = DirectoryPath + "/ChallengePrestige";
                ConfigTaskFilePath = Path.Combine(ConfigDirectoryPath, "tasks.json");
                ConfigRegisterFilePath = Path.Combine(ConfigDirectoryPath, "register.json");

                if (!Directory.Exists(ConfigDirectoryPath)) Directory.CreateDirectory(ConfigDirectoryPath);
                InitTaskFile();
                InitRegisterFile();
                
            }
            catch (IOException ex)
            {
                Logger.LogError("InitDirectory", ex.Message);
            }
        }
        private void InitTaskFile()
        {
            if (!File.Exists(ConfigTaskFilePath))
            {
                List<TaskItem> tasks = new List<TaskItem>
                {
                    new TaskItem(29, 5, 9),
                    new TaskItem(30, 2, 6),
                    new TaskItem(33, 5, 9),
                    new TaskItem(136, 2, 6)
                };
                string json = JsonConvert.SerializeObject(tasks, Formatting.Indented);
                File.WriteAllText(ConfigTaskFilePath, json);
            }
        }
        private void InitRegisterFile()
        {
            if (!File.Exists(ConfigRegisterFilePath))
            {
                Register register = new Register()
                {
                    CityHallId = 999,
                    MinPrestige = 3
                };
                string json = JsonConvert.SerializeObject(register, Formatting.Indented);
                File.WriteAllText(ConfigRegisterFilePath, json);
            }
        }
        private void InitConfig()
        {
            string jsonContent = File.ReadAllText(ConfigRegisterFilePath);
            ConfigRegister = JsonConvert.DeserializeObject<Register>(jsonContent);
        }
    }
}
