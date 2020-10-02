using AudicaTools;
using Newtonsoft.Json.Schema;
using OsuTypes;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Runtime.CompilerServices;
using System.Text;

namespace AudicaConverter
{
    class Program
    {
        public static string FFMPEGNAME = @"\ffmpeg.exe";
        public static string OGG2MOGGNAME = @"\ogg2mogg.exe";
        public static string workingDirectory = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);

        static void Main(string[] args)
        {
            foreach (var item in args)
            {
                if(item.Contains(".osz")) ConversionProcess.ConvertToAudica(item);
            }
           //ConversionProcess.ConvertToAudica(@"C:\Users\adamk\source\repos\AudicaConverter\testfile.osz");
        }
    }

    public class ConversionProcess
    {
        public static void ConvertToAudica(string filePath)
        {
            Console.Clear();
            var osz = new OSZ(filePath);
            var audica = new Audica(@$"{Program.workingDirectory}\template.audica");
            Console.WriteLine($"{osz.osufiles[0].metadata.artist} - {osz.osufiles[0].metadata.title}" +
                $"\nMapped by {osz.osufiles[0].metadata.creator}" +
                $"\nFound {osz.osufiles.Count} difficulties");

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("\n\nSelect Conversion Mode: \n[1] Convert audio/timing only.\n[2] Convert everything");
            Console.ForegroundColor = ConsoleColor.Gray;
            int convertMode = int.Parse(Console.ReadLine());

            if (convertMode == 2)
            {

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
            }
            else
            {
                audica.expert = new Difficulty();
                audica.advanced = new Difficulty();
                audica.moderate = new Difficulty();
                audica.beginner = new Difficulty();
            }

            audica.desc.title = osz.osufiles[0].metadata.title;
            audica.desc.artist = osz.osufiles[0].metadata.artist;
            audica.desc.author = osz.osufiles[0].metadata.creator;
            audica.desc.songID = RemoveSpecialCharacters(osz.osufiles[0].metadata.title) + "-" + RemoveSpecialCharacters(osz.osufiles[0].metadata.creator);
            Console.WriteLine(audica.desc.songID);

            ConvertSongToOGG(osz, audica, 0);

            //Convert tempos
            var tempList = new List<TempoData>();
            foreach (var timingPoint in osz.osufiles[0].timingPoints)
            {
                tempList.Add(new TempoData((int)OsuUtility.MsToTick(timingPoint.ms, osz.osufiles[0].timingPoints), TempoData.MicrosecondsPerQuarterNoteFromBPM(60000 / timingPoint.beatTime)));
            }
            audica.tempoData = tempList;

            //at the end
            if (!Directory.Exists(@$"{Program.workingDirectory}\audicaFiles")) Directory.CreateDirectory(@$"{Program.workingDirectory}\audicaFiles");
            audica.Export(@$"{Program.workingDirectory}\audicaFiles\{audica.desc.songID}.audica");
        }

        private static void ConvertSongToOGG(OSZ osz, Audica audica, int lastDiffIndex)
        {
            string audioFileName = osz.osufiles[lastDiffIndex].general.audioFileName;
            string tempDirectory = Program.workingDirectory + @"\AudicaConverterTemp\";
            string tempAudioPath = tempDirectory + @"audio.mp3";
            string tempOggPath = tempDirectory + @"tempogg.ogg";
            string tempMoggPath = tempDirectory + @"tempMogg.mogg";

            if (Directory.Exists(tempDirectory)) Directory.Delete(tempDirectory, true);

            Directory.CreateDirectory(tempDirectory);

            ZipArchive zip = ZipFile.OpenRead(osz.oszFilePath);
            zip.GetEntry(audioFileName).ExtractToFile(tempAudioPath);

            Process ffmpeg = new Process();
            ffmpeg.StartInfo.FileName = Program.workingDirectory + Program.FFMPEGNAME;
            ffmpeg.StartInfo.WindowStyle = ProcessWindowStyle.Normal;
            ffmpeg.StartInfo.UseShellExecute = false;
            ffmpeg.StartInfo.RedirectStandardOutput = true;

            Process ogg2mogg = new Process();
            ogg2mogg.StartInfo.FileName = Program.workingDirectory + Program.OGG2MOGGNAME;
            ogg2mogg.StartInfo.WindowStyle = ProcessWindowStyle.Normal;
            ogg2mogg.StartInfo.UseShellExecute = false;
            ogg2mogg.StartInfo.RedirectStandardOutput = true;

            ogg2mogg.StartInfo.Arguments = $"{tempOggPath} {tempMoggPath}";
            
            ffmpeg.StartInfo.Arguments = String.Format("-y -i \"{0}\" -ab 256k -ss 0.025 -map 0:a \"{1}\"", tempAudioPath, tempOggPath);
            ffmpeg.Start();
            ffmpeg.WaitForExit();

            ogg2mogg.Start();
            ogg2mogg.WaitForExit();

            audica.song.bytes = File.ReadAllBytes(tempMoggPath);

            Directory.Delete(tempDirectory, true);
        }

        public static Difficulty ConvertToAudica(osufile osufile)
        {
            var diff = new Difficulty();
            diff.cues = new List<Cue>();
            var handColorHandler = new HandColorHandler();
            // do conversion stuff here
            for (int i = 0; i < osufile.hitObjects.Count; i++)
            {
                var lastHitObject = i > 0 ? osufile.hitObjects[i - 1] : null;
                var hitObject = osufile.hitObjects[i];
                float timeSinceLastObject = lastHitObject == null ? 0f : hitObject.time - lastHitObject.time;
                var audicaDataPos = OsuUtility.GetAudicaPosFromHitObject(hitObject);
                var cue = new Cue
                    (
                        OsuUtility.MsToTick(hitObject.time, osufile.timingPoints),
                        OsuUtility.GetTickLengthForObject(hitObject, osufile.timingPoints),
                        audicaDataPos.pitch,
                        OsuUtility.GetVelocityForObject(hitObject),
                        audicaDataPos.offset,
                        0f,
                        handColorHandler.GetHandType(timeSinceLastObject),
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
        public string oszFilePath;
        
        public List<osufile> osufiles = new List<osufile>();
        public OSZ(string filePath)
        {
            oszFilePath = filePath;
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


