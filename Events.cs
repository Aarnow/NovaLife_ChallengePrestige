using Life;
using System;

namespace ChallengePrestige
{
    public class Events : ModKit.Helper.Events
    {
        private int date;
        public Events(IGameAPI api) : base(api)
        {
        }

        public async override void OnMinutePassed()
        {
            var today = int.Parse(DateTime.Today.ToString("ddMMyyyy"));

            if (date == default)
            {
                var query = await ChallengePrestigeTask.Query(c => c.Date == today);
                if (query.Count > 0)
                {
                    date = query[0].Date;
                }
                else
                {
                    var task = await ChallengePrestigeTask.CreateTask();
                    if (task != null) date = task.Date;
                }
            }
            else
            {
                if (date != today)
                {
                    var task = await ChallengePrestigeTask.CreateTask();
                    if (task != null) date = task.Date;
                }
            }
        }
    }
}
