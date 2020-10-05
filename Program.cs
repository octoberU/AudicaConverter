using AudicaTools;
using Newtonsoft.Json.Schema;
using osutoaudica;
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
            Config.Init();
            foreach (var item in args)
            {
                if(item.Contains(".osz")) ConversionProcess.ConvertToAudica(item);
            }
           //ConversionProcess.ConvertToAudica(@"C:\audica\netcoreapp3.1\532522 SakiZ - osu!memories.osz");
        }
    }

    public class ConversionProcess
    {
        public static bool snapNotes = false;
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
                tempList.Add(new TempoData((int)OsuUtility.MsToTick(timingPoint.ms, osz.osufiles[0].timingPoints, roundingPrecision: 1), TempoData.MicrosecondsPerQuarterNoteFromBPM(60000 / timingPoint.beatTime)));
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
            HitObject prevRightHitObject = null;
            HitObject prevLeftHitObject = null;

            if (Config.parameters.convertChains) RunChainPass(ref osufile.hitObjects);
            if (Config.parameters.convertSustains) RunSustainPass(ref osufile.hitObjects);
            ResetEndTimes(ref osufile.hitObjects);

            // do conversion stuff here
            for (int i = 0; i < osufile.hitObjects.Count; i++)
            {
                var hitObject = osufile.hitObjects[i];
                var audicaDataPos = OsuUtility.GetAudicaPosFromHitObject(hitObject);
                var gridOffset = ConversionProcess.snapNotes ? new Cue.GridOffset() : audicaDataPos.offset;
                int tickLength = 120;

                if (hitObject.audicaBehavior == 5)
                {
                    hitObject.audicaHandType = osufile.hitObjects[i - 1].audicaHandType;
                }
                else
                {
                    if (hitObject.audicaBehavior == 3)
                    {
                        tickLength = (int)hitObject.audicaEndTick - (int)hitObject.audicaTick;
                    }

                    HitObject nextHitObject = null; //Next non chain-link hitObject
                    for (int j = 1; nextHitObject==null && i+j < osufile.hitObjects.Count; j++)
                    {
                        if (osufile.hitObjects[i + j].audicaBehavior != 5) nextHitObject = osufile.hitObjects[i + j];
                    }
                    hitObject.audicaHandType = handColorHandler.GetHandType(hitObject, prevRightHitObject, prevLeftHitObject, nextHitObject);
                    if (hitObject.audicaHandType == 1) prevRightHitObject = hitObject;
                    else prevLeftHitObject = hitObject;
                }

                var cue = new Cue
                    (
                        hitObject.audicaTick,
                        tickLength,
                        audicaDataPos.pitch,
                        OsuUtility.GetVelocityForObject(hitObject),
                        gridOffset,
                        0f,
                        hitObject.audicaHandType,
                        hitObject.audicaBehavior
                    );
                diff.cues.Add(cue);
            }

            RunStackDistributionPass(ref diff.cues);

            if(Config.parameters.snapNotes) SnapNormalTargets(ref diff.cues);

            return diff;
        }

        private static void SnapNormalTargets(ref List<Cue> cues)
        {
            foreach (Cue cue in cues)
            {
                if(cue.behavior == 0)
                {
                    cue.gridOffset = new Cue.GridOffset();
                }
            }
        }

        private static void RunChainPass(ref List<HitObject> hitObjects)
        {
            float chainTimeThreshold = 120f;
            float chainSwitchFrequency = 480f;

            HitObject prevChainHeadHitObject = null;

            for (int i = 0; i < hitObjects.Count; i++)
            {
                HitObject prevHitObject = i > 0 ? hitObjects[i - 1] : null;
                HitObject nextHitObject = i + 1 < hitObjects.Count ? hitObjects[i + 1] : null;
                HitObject currentHitObject = hitObjects[i];

                if (Config.parameters.ignoreSlidersForChainConvert && (currentHitObject.type == 2 || currentHitObject.type == 6))
                    continue;

                if ((prevHitObject == null || currentHitObject.audicaTick - prevHitObject.audicaTick > chainTimeThreshold) && nextHitObject != null &&
                    nextHitObject.audicaTick - currentHitObject.audicaTick <= chainTimeThreshold && (!Config.parameters.ignoreSlidersForChainConvert || !(nextHitObject.type == 2 || nextHitObject.type == 6)))
                {
                    currentHitObject.audicaBehavior = 4;
                    prevChainHeadHitObject = currentHitObject;
                }
                else if (prevHitObject != null && currentHitObject.audicaTick - prevHitObject.audicaTick <= chainTimeThreshold)
                {
                    if (currentHitObject.audicaTick - prevChainHeadHitObject.audicaTick >= chainSwitchFrequency && nextHitObject != null && nextHitObject.audicaTick - currentHitObject.audicaTick <= chainTimeThreshold &&
                        (!Config.parameters.ignoreSlidersForChainConvert || !(nextHitObject.type == 2 || nextHitObject.type == 6)))
                    {
                        currentHitObject.audicaBehavior = 4;
                        prevChainHeadHitObject = currentHitObject;
                    }
                    else
                    {
                        currentHitObject.audicaBehavior = 5;
                        prevChainHeadHitObject.endTime = currentHitObject.time;
                        prevChainHeadHitObject.audicaEndTick = currentHitObject.audicaEndTick;
                        prevChainHeadHitObject.endX = currentHitObject.x;
                        prevChainHeadHitObject.endY = currentHitObject.y;
                    }
                }
            }
        }

        private static void RunSustainPass(ref List<HitObject> hitObjects)
        {
            foreach (HitObject hitObject in hitObjects)
            {
                int sliderTickDuration = (int)hitObject.audicaEndTick - (int)hitObject.audicaTick;
                if (sliderTickDuration >= Config.parameters.minSustainLength)
                {
                    hitObject.audicaBehavior = 3;
                }
            }
        }

        private static void ResetEndTimes(ref List<HitObject> hitObjects)
        {
            foreach (HitObject hitObject in hitObjects)
            {
                if (hitObject.audicaBehavior != 3 && hitObject.audicaBehavior != 4)
                {
                    hitObject.endTime = hitObject.time;
                    hitObject.audicaEndTick = hitObject.audicaTick;
                }
            }
        }

        private static void RunStackDistributionPass(ref List<Cue> cues)
        {
            float stackItemDistance = Config.parameters.stackItemDistance; //Offset for stack items. Time proportionate distancing is used through the stack based on getting this distance between first and second item in stack
            float stackResetTime = 960f;

            Cue stackStartCue = cues[0];
            float stackMovementSpeed = 0f;
            for (int i = 1; i < cues.Count; i++)
            {
                Cue currentCue = cues[i];
                Cue prevCue = cues[i - 1];

                if (OsuUtility.CuesPosEquals(currentCue, stackStartCue) && currentCue.tick - prevCue.tick < stackResetTime)
                {
                    if (stackMovementSpeed == 0f)
                    {
                        stackMovementSpeed = stackItemDistance / (currentCue.tick - stackStartCue.tick);
                    }
                    currentCue.gridOffset.y -= stackMovementSpeed * (currentCue.tick - stackStartCue.tick);
                }
                else
                {
                    stackStartCue = currentCue;
                    stackMovementSpeed = 0f;
                }
            }
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


