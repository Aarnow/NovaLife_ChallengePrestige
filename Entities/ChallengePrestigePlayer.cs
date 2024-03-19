using SQLite;
using System.Collections.Generic;

namespace ChallengePrestige.Entities
{
    public class ChallengePrestigePlayer : ModKit.ORM.ModEntity<ChallengePrestigePlayer>
    {
        [AutoIncrement][PrimaryKey] public int Id { get; set; }
        public int PlayerId { get; set; }
        public string CharacterFullName { get; set; }
        public string DiscordId { get; set; }
        public int Prestige { get; set; }
        public int LastTaskCompleted { get; set; }
        public string RewardsRecovered { get; set; }

        public ChallengePrestigePlayer()
        {
        }
    }
}
