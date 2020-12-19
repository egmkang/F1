using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Sample.Client
{
    public class Account
    {
        private static string Content = "01234567890QWERTYUIOPASDFGHJKLZXCVBNMqwertyuiopasdfghjklzxcvbnm";
        private static ThreadLocal<Random> random = new ThreadLocal<Random>(() => new Random());
        public static int NextInt => random.Value.Next();

        public string AccountID { get; private set; }
        private readonly List<string> playerList = new List<string>();
        public int PlayerCount { get; private set; }

        public bool Logined { get; set; } = false;

        public Account(string accountId) 
        {
            this.AccountID = accountId;
        }

        public void AddPlayers(IEnumerable<string> players) 
        {
            this.playerList.Clear();
            this.playerList.AddRange(players);
        }

        public string RandomPlayer() 
        {
            var index = random.Value.Next(0, playerList.Count - 1);
            return playerList[index];
        }

        public string RandomEchoString() 
        {
            return Content.Substring(0, (NextInt % Content.Length) + 1);
        }

        public int EchoCount { get; set; }
    }
}
