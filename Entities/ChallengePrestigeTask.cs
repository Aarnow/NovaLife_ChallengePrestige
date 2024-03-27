using ChallengePrestige.Classes;
using ModKit.Internal;
using ModKit.Utils;
using Newtonsoft.Json;
using SQLite;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace ChallengePrestige.Entities
{
    public class ChallengePrestigeTask : ModKit.ORM.ModEntity<ChallengePrestigeTask>
    {
        [AutoIncrement][PrimaryKey] public int Id { get; set; }
        public int ItemId { get; set; }
        public int Quantity { get; set; }
        public int Date {  get; set; }
        public int Count { get; set; }
        public ChallengePrestigeTask() { }

        public static async Task<ChallengePrestigeTask> CreateTask()
        {
            ChallengePrestigeTask challengePrestigeTask = new ChallengePrestigeTask();
            string jsonContent = File.ReadAllText(Prestige.ConfigTaskFilePath);
            List<TaskItem> taskItems = JsonConvert.DeserializeObject<List<TaskItem>>(jsonContent);
            Random random = new Random();
            TaskItem item = taskItems[random.Next(taskItems.Count)];
            int qty = random.Next(item.Quantity[0], item.Quantity[1] + 1);

            challengePrestigeTask.ItemId = item.ItemId;
            challengePrestigeTask.Quantity = qty;
            challengePrestigeTask.Date = DateUtils.GetNumericalDateOfTheDay();
            challengePrestigeTask.Count = 0;

            if (await challengePrestigeTask.Save()) return challengePrestigeTask;
            else
            {
                Logger.LogError("ChallengePrestige - CreateTask", "Echec lors de la création de la quête quotidienne");
                return null;
            }
        }
    }
}
