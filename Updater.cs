using AudicaConverter;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace osutoaudica
{
    public class Updater
    {
        public static void UpdateClient()
        {
            int newestVersionNumber;

            using (WebClient web = new WebClient())
            {
                int.TryParse(web.DownloadString("https://raw.githubusercontent.com/octoberU/AudicaConverter/main/version.txt"), out newestVersionNumber);
                if(Program.version < newestVersionNumber)
                {
                    var shouldUpdate = ConsolePrompt("There's a new update available!\nWould you like to download it now?\n");
                    Console.WriteLine(shouldUpdate);
                }
            }
        }

        private static bool ConsolePrompt(string promptString)
        {
            Console.WriteLine(promptString);
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("[Y,N]");
            var keyInfo = Console.ReadKey();
            Console.Clear();
            if (keyInfo.Key == ConsoleKey.Y) return true;
            else if (keyInfo.Key == ConsoleKey.N) return false;
            else return false;
        }
    }
}
