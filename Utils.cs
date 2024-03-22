
using Life.Network;
using System;

namespace ChallengePrestige
{
    public static class Utils
    {

        public static int GetNumericalDateOfTheDay()
        {
            return int.Parse(DateTime.Today.ToString("ddMMyyyy"));
        }

        public static string GetCodeByPlayer(Account account)
        {
            string codeString = account.steamId.ToString();
            int length = codeString.Length;
            return codeString.Substring(Math.Max(0, length - 5)) + account.id;
        }
    }
}
