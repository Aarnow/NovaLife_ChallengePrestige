using System.Collections.Generic;

namespace ChallengePrestige.Classes
{
    public class TaskItem
    {
        public int ItemId { get; set; }
        public List<int> Quantity { get; set; }

        public TaskItem(int itemId, int min, int max)
        {
            ItemId = itemId;
            Quantity = new List<int> {min, max};
        }
    }
}
