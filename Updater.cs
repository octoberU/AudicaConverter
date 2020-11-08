﻿using AudicaConverter;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Text;

namespace osutoaudica
{
    public class Updater
    {
        public static void UpdateClient()
        {
            if (Program.beta)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("This is a beta version of the AudicaConverter, and cannot be updated through the auto-updater. Please visit the GitHub release page for newer stable and/or beta releases" +
                    "(https://github.com/octoberU/AudicaConverter/releases).");
                Console.WriteLine($"Current beta version: {Program.version}");
                Console.ForegroundColor = ConsoleColor.Gray;
                return;
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) return;
            using (WebClient web = new WebClient())
            {
                string newestVersionNumber = web.DownloadString("https://raw.githubusercontent.com/octoberU/AudicaConverter/main/version.txt");
                if (VersionLessThan(Program.version, newestVersionNumber))
                {
                    var shouldUpdate = ConsolePrompt($"There's a new update available!\nWould you like update from {Program.version} to {newestVersionNumber}\n");
                    if (shouldUpdate)
                    {
                        string currentExecutable = Program.workingDirectory + @"/AudicaConverter.exe";
                        File.Move(currentExecutable, currentExecutable + ".old");
                        Console.Clear();
                        Console.WriteLine("Updating...");
                        var data = web.DownloadData("https://github.com/octoberU/AudicaConverter/raw/main/Standalone%20Releases/AudicaConverter.exe");
                        File.WriteAllBytes(currentExecutable, data);
                        Console.Clear();
                        Console.WriteLine($"Update successful!");
                        System.Threading.Thread.Sleep(2000); //Wait 2 seconds
                        Process process = Process.Start(new ProcessStartInfo() //Start process to delete old executable in 1 second
                        {
                            Arguments = "/C choice /C Y /N /D Y /T 1 & Del \"AudicaConverter.exe.old\"",
                            WindowStyle = ProcessWindowStyle.Hidden,
                            CreateNoWindow = true,
                            FileName = "cmd.exe"
                        });
                        Environment.Exit(0); //Terminate program
                    }
                }
                else if(VersionEquals(Program.version, newestVersionNumber))
                {
                    Console.WriteLine($"You are currently running the latest version of AudicaConverter\nVersion: {Program.version}");
                }
            }
        }

        public static void CheckVersion()
        {
            if (Program.beta)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"NB! This is a beta version of the AudicaConverter ({Program.version}). This version might have experimental features or incomplete balancing that can result in poorly " +
                    $"converted maps. See the GitHub release page for stable versions of the converter (https://github.com/octoberU/AudicaConverter/releases)." +
                    $"\nPlease report any technical issues or problems with convert quality in the #converter-feedback channel in the Audica Modding Group Discord or through creating an issue " +
                    $"on the AudicaConverter GitHub. Thank you for testing!");
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.WriteLine("[Press enter to continue]");
                Console.ReadLine();
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) return;
            using (WebClient web = new WebClient())
            {
                string newestVersionNumber = web.DownloadString("https://raw.githubusercontent.com/octoberU/AudicaConverter/main/version.txt");
                if (VersionLessThan(Program.version, newestVersionNumber))
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Currently using an older version of the AudicaConverter ({Program.version}). " +
                        $"There is a newer version ({newestVersionNumber}) available! Launch AudicaConverter without any game files to update. [Press enter to continue]");
                    Console.ForegroundColor = ConsoleColor.Gray;
                    Console.ReadLine();
                }
            }
        }

        public static bool VersionLessThan(string v1, string v2)
        {
            string[] v1Nums = v1.Split(".");
            string[] v2Nums = v2.Split(".");

            for (int i = 0; i < v1Nums.Length; i++)
            {
                int v1Num = int.Parse(v1Nums[i]);
                int v2Num = int.Parse(v2Nums[i]);

                if (v1Num < v2Num) return true;
                else if (v1Num > v2Num) return false;
            }
            return false;
        }

        public static bool VersionEquals(string v1, string v2)
        {
            string[] v1Nums = v1.Split(".");
            string[] v2Nums = v2.Split(".");

            for (int i = 0; i < v1Nums.Length; i++)
            {
                int v1Num = int.Parse(v1Nums[i]);
                int v2Num = int.Parse(v2Nums[i]);

                if (v1Num != v2Num) return false;
            }
            return true;
        }

        private static bool ConsolePrompt(string promptString)
        {
            Console.WriteLine(promptString);
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("[Y,N]");
            Console.ForegroundColor = ConsoleColor.Gray;
            var keyInfo = Console.ReadKey();
            Console.Clear();
            if (keyInfo.Key == ConsoleKey.Y) return true;
            else if (keyInfo.Key == ConsoleKey.N) return false;
            else return false;
        }
    }
}
