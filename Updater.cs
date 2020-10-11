using AudicaConverter;
using System;
using System.Collections.Generic;
using System.IO;
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
                    var shouldUpdate = ConsolePrompt($"There's a new update available!\nWould you like update from {Program.version} to {newestVersionNumber}\n");
                    if (shouldUpdate)
                    {
                        string currentExecutable = Program.workingDirectory + @"/AudicaConverter.exe";
                        File.Move(currentExecutable, currentExecutable + ".old");
                        var data = web.DownloadData("https://github.com/octoberU/AudicaConverter/raw/main/Standalone%20Releases/AudicaConverter.exe");
                        File.WriteAllBytes(currentExecutable, data);
                        File.Delete(currentExecutable + ".old");
                        Console.WriteLine($"Successfully Updated!\nYou are currently running version {newestVersionNumber}");
                        Console.ReadLine();
                    }
                }
                else if(Program.version == newestVersionNumber)
                {
                    Console.WriteLine($"You are currently running the latest release of AudicaConverter\nVersion:{Program.version}");
                    Console.ReadLine();
                }
            }
        }

        public static void CheckVersion()
        {
            int newestVersionNumber;

            using (WebClient web = new WebClient())
            {
                int.TryParse(web.DownloadString("https://raw.githubusercontent.com/octoberU/AudicaConverter/main/version.txt"), out newestVersionNumber);
                if (Program.version < newestVersionNumber)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("There is a new version available! Launch AudicaConverter without any game files to update.");
                    Console.ForegroundColor = ConsoleColor.Gray;
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
