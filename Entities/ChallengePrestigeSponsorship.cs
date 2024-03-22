using SQLite;

namespace ChallengePrestige.Entities
{
    public class ChallengePrestigeSponsorship : ModKit.ORM.ModEntity<ChallengePrestigeSponsorship>
    {
        [AutoIncrement][PrimaryKey] public int Id { get; set; }
        public int ReferralId { get; set; }
        public int ReferrerId { get; set; }
        public int Date { get; set; }
        public bool isClaimedByReferral { get; set; }
        public bool isClaimedByReferrer { get; set; }

        public ChallengePrestigeSponsorship() { }
    }
}
