using SQLite;

namespace ChallengePrestige.Entities
{
    public class ChallengePrestigeReward : ModKit.ORM.ModEntity<ChallengePrestigeReward>
    {
        [AutoIncrement][PrimaryKey] public int Id { get; set; }
        public int Money { get; set; }
        public int ItemId { get; set; }
        public int ItemQuantity { get; set; }
        public int PrestigeRequired { get; set; }

        public ChallengePrestigeReward() { }
    }
}
