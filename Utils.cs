
using System;

namespace ChallengePrestige
{
    public static class Utils
    {

        public static int GetNumericalDateOfTheDay()
        {
            return int.Parse(DateTime.Today.ToString("ddMMyyyy"));
        }
    }
}
