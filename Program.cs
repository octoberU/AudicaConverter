using AudicaTools;
using Newtonsoft.Json.Schema;
using OsuTypes;
using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Runtime.CompilerServices;
using System.Text;

namespace AudicaConverter
{
    class Program
    {
        static void Main(string[] args)
        {
            ConversionProcess.ConvertToAudica(@"C:\Users\adamk\source\repos\AudicaConverter\testfile.osz");
        }
    }

    public class ConversionProcess
    {
        public static void ConvertToAudica(string filePath)
        {
            var osz = new OSZ(filePath);
            var audica = new Audica(@"C:\Users\adamk\source\repos\AudicaConverter\template.audica");
            Console.WriteLine($"{osz.osufiles[0].metadata.artist} - {osz.osufiles[0].metadata.title}" +
                $"\nMapped by {osz.osufiles[0].metadata.creator}" +
                $"\nFound {osz.osufiles.Count} difficulties");

            Console.WriteLine("\n\nSelect Expert difficulty:");
            Console.ForegroundColor = ConsoleColor.Yellow;
            for (int i = 0; i < osz.osufiles.Count; i++)
            {
                Console.WriteLine($"\n[{i}]{osz.osufiles[i].metadata.version}");
            }
            Console.ForegroundColor = ConsoleColor.Gray;
            int expert = int.Parse(Console.ReadLine());

            Console.Clear();
            Console.WriteLine("\n\nSelect Advanced difficulty:");
            Console.ForegroundColor = ConsoleColor.Yellow;
            for (int i = 0; i < osz.osufiles.Count; i++)
            {
                if (i == expert) continue;
                Console.WriteLine($"\n[{i}]{osz.osufiles[i].metadata.version}");
            }
            Console.ForegroundColor = ConsoleColor.Gray;
            int advanced = int.Parse(Console.ReadLine());

            Console.Clear();
            Console.WriteLine("\n\nSelect Standard difficulty:");
            Console.ForegroundColor = ConsoleColor.Yellow;
            for (int i = 0; i < osz.osufiles.Count; i++)
            {
                if (i == expert || i == advanced) continue;

                Console.WriteLine($"\n[{i}]{osz.osufiles[i].metadata.version}");
            }
            Console.ForegroundColor = ConsoleColor.Gray;
            int standard = int.Parse(Console.ReadLine());

            Console.Clear();
            Console.WriteLine("\n\nSelect Beginner difficulty:");
            Console.ForegroundColor = ConsoleColor.Yellow;
            for (int i = 0; i < osz.osufiles.Count; i++)
            {
                if (i == expert || i == advanced || i == standard) continue;

                Console.WriteLine($"\n[{i}]{osz.osufiles[i].metadata.version}");
            }
            Console.ForegroundColor = ConsoleColor.Gray;
            int beginner = int.Parse(Console.ReadLine());

            Console.Clear();
            Console.WriteLine("Converting...");
            audica.expert = ConvertToAudica(osz.osufiles[expert]);
            audica.advanced = ConvertToAudica(osz.osufiles[advanced]);
            audica.moderate = ConvertToAudica(osz.osufiles[standard]);
            audica.beginner = ConvertToAudica(osz.osufiles[beginner]);

            audica.desc.title = osz.osufiles[expert].metadata.title;
            audica.desc.artist = osz.osufiles[expert].metadata.artist;
            audica.desc.author = osz.osufiles[expert].metadata.creator;
            audica.desc.songID = RemoveSpecialCharacters(osz.osufiles[expert].metadata.title) + "-" + RemoveSpecialCharacters(osz.osufiles[expert].metadata.creator);
            Console.WriteLine(audica.desc.songID);

            //at the end
            audica.Export(@$"C:\Users\adamk\Documents\converterTests\{audica.desc.songID}.audica");
        }

        public static Difficulty ConvertToAudica(osufile osufile)
        {
            var diff = new Difficulty();
            diff.cues = new List<Cue>();
            // do conversion stuff here
            for (int i = 0; i < osufile.hitObjects.Count; i++)
            {
                var hitObject = osufile.hitObjects[i];
                var audicaDataPos = OsuUtility.GetAudicaPosFromHitObject(hitObject);
                var cue = new Cue
                    (
                        OsuUtility.MsToTick(hitObject.time, osufile.timingPoints),
                        OsuUtility.GetTickLengthForObject(hitObject, osufile.timingPoints),
                        audicaDataPos.pitch,
                        OsuUtility.GetVelocityForObject(hitObject),
                        audicaDataPos.offset,
                        0f,
                        0,
                        0
                    );
                diff.cues.Add(cue);
                Console.WriteLine(cue.tick);
            }
            

            return diff;
        }

        public static string RemoveSpecialCharacters(string str)
        {
            StringBuilder sb = new StringBuilder();
            foreach (char c in str)
            {
                if(sb.Length < 14)
                {
                    if ((c >= '0' && c <= '9') || (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z') || c == '.' || c == '_')
                    {
                        sb.Append(c);
                    }
                }
                
            }
            return sb.ToString().Replace(".", "");
        }
    }

    public class OSZ
    {
        public List<osufile> osufiles = new List<osufile>();
        public OSZ(string filePath)
        {
            ZipArchive zip = ZipFile.OpenRead(filePath);
            foreach (var entry in zip.Entries)
            {
                if (entry.Name.Contains(".osu"))
                {
                    osufiles.Add(new osufile(entry.Open()));   
                }
            }
        }
    }
}


