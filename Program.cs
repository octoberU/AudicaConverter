using AudicaTools;
using NAudio.Wave.SampleProviders;
using osutoaudica;
using osutoaudica.osu__types;
using OsuTypes;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace AudicaConverter
{
    class Program
    {
        public static int version = 3; //https://github.com/octoberU/AudicaConverter/wiki/Creating-a-new-release-for-the-updater
        public static string FFMPEGNAME = @"\ffmpeg.exe";
        public static string OGG2MOGGNAME = @"\ogg2mogg.exe";
        public static string workingDirectory = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);

        static void Main(string[] args)
        {
            Config.Init();
            try
            {
                if (args.Length < 1) Updater.UpdateClient();
                else Updater.CheckVersion();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Updater has failed, make sure you're connected to the internet or update manually.\nhttps://github.com/octoberU/AudicaConverter/releases");
            }

            List<string> oszFileNames = new List<string>();
            foreach (var item in args)
            {
                if (Directory.Exists(item))
                {
                    foreach (string dirItem in Directory.GetFiles(item))
                    {
                        if (dirItem.Contains(".osz")) oszFileNames.Add(dirItem);
                    }
                }
                else if (item.Contains(".osz"))
                    oszFileNames.Add(item);
            }

            for (int i = 0; i < oszFileNames.Count; i++)
            {
                string oszFileName = oszFileNames[i];
                string[] pathElements = oszFileName.Split("\\");
                string oszName = pathElements[pathElements.Length-1];
                if (Config.parameters.autoMode)
                {
                    Console.Clear();
                    Console.WriteLine(String.Format("({0}/{1}) Converting {2}...", i+1, oszFileNames.Count, oszName));
                }
                ConversionProcess.ConvertToAudica(oszFileName, Config.parameters.autoMode ? "auto" : "manual");
            }
            //ConversionProcess.ConvertToAudica(@"C:\Users\adamk\source\repos\AudicaConverter\1173192 Ricky Montgomery - This December (3).osz", "manual");
        }
    }

    public class ConversionProcess
    {
        public static bool snapNotes = false;
        public static void ConvertToAudica(string filePath, string mode)
        {
            var osz = new OSZ(filePath);
            var audica = new Audica(@$"{Program.workingDirectory}\template.audica");
            ConvertTempos(osz, ref audica);

            int convertMode = 1;
            if (mode == "manual")
            {
                Console.WriteLine($"{osz.osufiles[0].metadata.artist} - {osz.osufiles[0].metadata.title}" +
                $"\nMapped by {osz.osufiles[0].metadata.creator}" +
                $"\nFound {osz.osufiles.Count} difficulties");
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("\n\nSelect Conversion Mode: \n[1] Convert whole map.\n[2] Convert audio/timing only.");
                Console.ForegroundColor = ConsoleColor.Gray;
                convertMode = int.Parse(Console.ReadLine());
            }

            if (convertMode == 1)
            {
                if (mode == "manual")
                {
                    Console.Clear();
                    Console.WriteLine("Converting song to OGG");
                }
                ConvertSongToOGG(ref osz, audica);

                if (mode == "manual")
                {
                    Console.Clear();
                    Console.WriteLine("Converting beatmaps...");
                }
                foreach (osufile file in osz.osufiles)
                {
                    file.audicaDifficulty = ConvertToAudica(file);
                    file.audicaDifficultyRating = audica.GetRatingForDifficulty(file.audicaDifficulty);
                }
                SortOSZ(osz);

                audica.expert = AskDifficulty(osz, audica, "Expert", mode);
                audica.advanced = AskDifficulty(osz, audica, "Advanced", mode);
                audica.moderate = AskDifficulty(osz, audica, "Standard", mode);
                audica.beginner = AskDifficulty(osz, audica, "Beginner", mode);

                if (audica.expert == null && audica.advanced == null && audica.moderate == null && audica.beginner == null) return; //Abort conversion if all slots are empty
            }
            else
            {
                audica.expert = null;
                audica.advanced = null;
                audica.moderate = null;
                audica.beginner = null;
                ConvertSongToOGG(ref osz, audica);
            }

            ConvertMetadata(osz, audica);

            string audicaFileName = $"{audica.desc.songID}.audica";

            //at the end
            ExportConvert(audica, audicaFileName);
        }

        private static void ExportConvert(Audica audica, string audicaFileName)
        {
            if (Config.parameters.customExportDirectory == "")
            {
                ExportToDefaultDirectory(audica, audicaFileName);
            }
            else
            {
                if (Directory.Exists(Config.parameters.customExportDirectory))
                {
                    audica.Export($"{Config.parameters.customExportDirectory}\\{audicaFileName}");
                }
                else
                {
                    Console.WriteLine("Custom directory doesn't exist, exporting to default /audicaFiles");
                    ExportToDefaultDirectory(audica, audicaFileName);
                }
            }

            static void ExportToDefaultDirectory(Audica audica, string audicaFileName)
            {
                if (!Directory.Exists(@$"{Program.workingDirectory}\audicaFiles")) Directory.CreateDirectory(@$"{Program.workingDirectory}\audicaFiles");
                audica.Export(@$"{Program.workingDirectory}\audicaFiles\{audicaFileName}");
            }
        }

        private static void ConvertTempos(OSZ osz, ref Audica audica)
        {
            var tempList = new List<TempoData>();
            foreach (var timingPoint in osz.osufiles[0].timingPoints)
            {
                tempList.Add(new TempoData((int)timingPoint.audicaTick, TempoData.MicrosecondsPerQuarterNoteFromBPM(60000 / timingPoint.beatTime)));
            }
            audica.tempoData = tempList;
        }

        private static void SortOSZ(OSZ osz)
        {
            var templist = osz.osufiles.OrderByDescending(o => o.audicaDifficultyRating).ToList();
            osz.osufiles = templist;
        }

        private static void ConvertMetadata(OSZ osz, Audica audica)
        {
            string mapperName = Config.parameters.customMapperName == "" ? RemoveSpecialCharacters(osz.osufiles[0].metadata.creator) : RemoveSpecialCharacters(Config.parameters.customMapperName);
            audica.desc.title = osz.osufiles[0].metadata.title;
            audica.desc.artist = osz.osufiles[0].metadata.artist;
            audica.desc.author = mapperName;
            audica.desc.songID = RemoveSpecialCharacters(osz.osufiles[0].metadata.title) + "-" + mapperName;
            audica.desc.previewStartSeconds = (float)osz.osufiles[0].general.previewTime / 1000f;
            audica.desc.fxSong = "";
        }

        private static Difficulty AskDifficulty(OSZ osz, Audica audica, string difficultyName, string mode)
        {
            ScalingOptions scalingOptions = new ScalingOptions();
            AutoOptions autoOptions = new AutoOptions();
            switch (difficultyName.ToLower())
            {
                case ("expert"):
                    scalingOptions = Config.parameters.expertScalingOptions;
                    autoOptions = Config.parameters.expertAutoOptions;
                    break;
                case ("advanced"):
                    scalingOptions = Config.parameters.advancedScalingOptions;
                    autoOptions = Config.parameters.advancedAutoOptions;
                    break;
                case ("standard"):
                    scalingOptions = Config.parameters.standardScalingOptions;
                    autoOptions = Config.parameters.standardAutoOptions;
                    break;
                case ("beginner"):
                    scalingOptions = Config.parameters.beginnerScalingOptions;
                    autoOptions = Config.parameters.beginnerAutoOptions;
                    break;
            }

            if (mode == "auto" && !autoOptions.useDifficultySlot) return null; //Difficulty slot not in use

            if (mode == "manual")
            {
                Console.Clear();
                Console.WriteLine($"\n\nSelect {difficultyName} difficulty[Leave empty for none]:");
                Console.ForegroundColor = ConsoleColor.Yellow;
            }

            //Conversion steps (Scaling and melee pass) here is not very clean and should probably be refactored in the future
            var scaledDifficulties = new List<(Difficulty difficulty, float difficultyRating)>();
            for (int i = 0; i < osz.osufiles.Count; i++)
            {
                Difficulty scaledDifficulty = ScaleDifficulty(osz.osufiles[i].audicaDifficulty, scalingOptions.xScale, scalingOptions.yScale);
                RunMeleePass(scaledDifficulty.cues, osz.osufiles[i].timingPoints, osz.osufiles[i].mergedTimingPoints, difficultyName);
                if (Config.parameters.useStandardSounds) RunHitsoundPass(scaledDifficulty.cues);
                float difficultyRating = audica.GetRatingForDifficulty(scaledDifficulty);
                scaledDifficulties.Add((scaledDifficulty, difficultyRating));

                if (mode == "manual") Console.WriteLine($"\n[{i+1}]{osz.osufiles[i].metadata.version} [{difficultyRating.ToString("n2")} Audica difficulty]");
            }

            int difficultyIdx=0;
            if (mode == "manual")
            {
                Console.ForegroundColor = ConsoleColor.Gray;
                string userInput = Console.ReadLine();
                Console.Clear();
                if (userInput == "") return null;// User hasn't picked a difficulty
                else difficultyIdx = int.Parse(userInput)-1;
            }
            else
            {
                float bestDifficultyRatingDeviation = float.PositiveInfinity;
                for (int i = 0; i < scaledDifficulties.Count; i++)
                {
                    float difficultyRatingDeviation = Math.Abs(autoOptions.targetDifficultyRating - scaledDifficulties[i].difficultyRating);
                    if (difficultyRatingDeviation < bestDifficultyRatingDeviation)
                    {
                        bestDifficultyRatingDeviation = difficultyRatingDeviation;
                        difficultyIdx = i;
                    }
                }

                if (bestDifficultyRatingDeviation > autoOptions.acceptedDifficultyRatingDifference) return null; //No difficulty meets difficulty range requirement
            }
                
            if (Config.parameters.customMapperName == "")
            {
                switch (difficultyName.ToLower())
                {
                    case ("expert"):
                        audica.desc.customExpert = osz.osufiles[difficultyIdx].metadata.version;
                        break;
                    case ("advanced"):
                        audica.desc.customAdvanced = osz.osufiles[difficultyIdx].metadata.version;
                        break;
                    case ("standard"):
                        audica.desc.customModerate = osz.osufiles[difficultyIdx].metadata.version;
                        break;
                    case ("beginner"):
                        audica.desc.customBeginner = osz.osufiles[difficultyIdx].metadata.version;
                        break;
                }
            }

            return scaledDifficulties[difficultyIdx].difficulty;
        }

        private static void ConvertSongToOGG(ref OSZ osz, Audica audica)
        {
            string audioFileName = osz.osufiles[0].general.audioFileName;
            string tempDirectory = Program.workingDirectory + @"\AudicaConverterTemp\";
            string tempAudioPath = tempDirectory + @"audio.mp3";
            string tempOggPath = tempDirectory + @"tempogg.ogg";
            string tempMoggPath = tempDirectory + @"tempMogg.mogg";

            float firstHitObjectTime = float.PositiveInfinity;
            foreach (var osufile in osz.osufiles)
            {
                if (osufile.hitObjects.Count > 0 && osufile.hitObjects[0].time < firstHitObjectTime)
                {
                    firstHitObjectTime = osufile.hitObjects[0].time;
                } 
            }
            float paddingTime = 0f;
            if (firstHitObjectTime < Config.parameters.introPadding)
            {
                paddingTime = Config.parameters.introPadding - firstHitObjectTime;
            }
            else if (Config.parameters.skipIntro.enabled && firstHitObjectTime > Config.parameters.skipIntro.threshold)
            {
                //Checks if the first hitobject is after the threshold, if it is, we cut it.
                paddingTime = (firstHitObjectTime - Config.parameters.skipIntro.fadeTime) * -1; //We need a negative value to not mess wih padding
                
            }

            if (paddingTime > 0f)
            {
                ShiftEverythingByMs(osz, paddingTime);
                ConvertTempos(osz, ref audica);
            }

            string pruneString = "-ss 0.025"; //Default 25ms to deal with compression block latency
            if (paddingTime < 0f)
            {
                ShiftEverythingByMs(osz, paddingTime);
                ConvertTempos(osz, ref audica);
                float pruneValue = (paddingTime / 1000f) * -1; //Convert ms to seconds and invert again
                float fadeTime = Config.parameters.skipIntro.fadeTime / 1000f; //Convert ms to seconds
                pruneString = $"-af \"afade=t=in:st=0:d={fadeTime.ToString("n1")}\" -ss {(0.025 + pruneValue).ToString("n3")}";
            }

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

            ogg2mogg.StartInfo.Arguments = $"\"{tempOggPath}\" \"{tempMoggPath}\"";

            string paddingString = paddingTime > 0 ? $"-af \"adelay = {paddingTime} | {paddingTime}\"" : "";


            ffmpeg.StartInfo.Arguments = $"-y -i \"{tempAudioPath}\" -hide_banner -loglevel panic -ab 256k {pruneString} {paddingString} -map 0:a \"{tempOggPath}\"";
            ffmpeg.Start();
            ffmpeg.WaitForExit();

            ogg2mogg.Start();
            ogg2mogg.WaitForExit();

            var ms = new MemoryStream(File.ReadAllBytes(tempMoggPath));
            audica.song = new Mogg(ms);

            Directory.Delete(tempDirectory, true);
        }

        private static void ShiftEverythingByMs(OSZ osz, float paddingTime)
        {
            foreach (var osuDifficulty in osz.osufiles)
            {
                foreach (var timingPoint in osuDifficulty.mergedTimingPoints)
                {
                    if (timingPoint.ms > 0f)
                    {
                        timingPoint.ms += paddingTime;
                    }
                }

                foreach (var hitObject in osuDifficulty.hitObjects)
                {
                    hitObject.time += paddingTime;
                    hitObject.endTime += paddingTime;
                }
                osuDifficulty.general.previewTime += (int)paddingTime;

                List<TimingPoint> negativeMergedTimingPoints = osuDifficulty.mergedTimingPoints.Where(tp => tp.ms < 0).ToList();
                List<TimingPoint> negativeTimingPoints = osuDifficulty.timingPoints.Where(tp => tp.ms < 0).ToList();

                if (negativeTimingPoints.Count > 0)
                {
                    //Find and push forward the last negative normal timing point
                    TimingPoint lastNegativeTimingPoint = negativeTimingPoints[negativeTimingPoints.Count - 1];
                    float newMs = lastNegativeTimingPoint.ms - ((float)lastNegativeTimingPoint.beatTime * 4) * (float)Math.Floor(lastNegativeTimingPoint.ms/((float)lastNegativeTimingPoint.beatTime * 4));
                    //Only keep and shift this timing point if it becomes the first timing point
                    if (!osuDifficulty.timingPoints.Exists(tp => tp.ms > 0 && tp.ms < newMs))
                    {
                        lastNegativeTimingPoint.ms = newMs;
                        //Inherit the slider velocity of the most recent merged timing point
                        int prevMergedTimingPointIdx = osuDifficulty.mergedTimingPoints.FindIndex(tp => tp.ms > newMs) - 1;
                        if (prevMergedTimingPointIdx == -2) prevMergedTimingPointIdx = osuDifficulty.mergedTimingPoints.Count - 1;
                        if (prevMergedTimingPointIdx != -2)
                        {
                            TimingPoint prevMergedTimingPoint = osuDifficulty.mergedTimingPoints[prevMergedTimingPointIdx];
                            lastNegativeTimingPoint.sliderVelocity = prevMergedTimingPoint.sliderVelocity;
                            lastNegativeTimingPoint.kiai = prevMergedTimingPoint.kiai;
                        }
                        negativeMergedTimingPoints.Remove(lastNegativeTimingPoint);
                    }
                }

                //Remove all negative timing points except the most recent one
                for (int i = 0; i < negativeMergedTimingPoints.Count; i++)
                {
                    TimingPoint timingPoint = negativeMergedTimingPoints[i];

                    osuDifficulty.mergedTimingPoints.Remove(timingPoint);
                    if (!timingPoint.inherited)
                    {
                        osuDifficulty.timingPoints.Remove(timingPoint);
                    }
                }

                //Resort timing points
                osuDifficulty.mergedTimingPoints.Sort((tp1, tp2) => tp1.ms.CompareTo(tp2.ms));
                osuDifficulty.timingPoints.Sort((tp1, tp2) => tp1.ms.CompareTo(tp2.ms));

                //Update initial 0ms timing point to the new first timing point bpm
                if (osuDifficulty.timingPoints.Count > 1)
                    osuDifficulty.timingPoints[0].beatTime = osuDifficulty.timingPoints[1].beatTime;

                //recalculate all the audica tick timings on every timing point and hitObject...
                foreach (TimingPoint timingPoint in osuDifficulty.mergedTimingPoints)
                    timingPoint.audicaTick = OsuUtility.MsToTick(timingPoint.ms, osuDifficulty.timingPoints, roundingPrecision: 1);
                foreach (HitObject hitObject in osuDifficulty.hitObjects)
                {
                    hitObject.audicaTick = OsuUtility.MsToTick(hitObject.time, osuDifficulty.timingPoints, roundingPrecision: 10);
                    hitObject.audicaEndTick = OsuUtility.MsToTick(hitObject.endTime, osuDifficulty.timingPoints, roundingPrecision: 10);
                }
            }
        }

        public static Difficulty ConvertToAudica(osufile osufile)
        {
            var diff = new Difficulty();
            diff.cues = new List<Cue>();
            var handColorHandler = new HandColorHandler();


            if (Config.parameters.convertSliderEnds) RunSliderSplitPass(osufile.hitObjects, osufile.timingPoints);
            if (Config.parameters.streamMinAverageDistance > 0f) RunStreamScalePass(osufile.noteStreams);
            if (Config.parameters.adaptiveScaling) RunFovScalePass(osufile.hitObjects);
            if (Config.parameters.convertSustains) RunSustainPass(osufile.hitObjects, osufile.timingPoints);
            if (Config.parameters.convertChains) RunChainPass(osufile.hitObjects, osufile.timingPoints);
            ResetEndTimesAndPos(osufile.hitObjects);
            RemoveUnusedHitObjects(osufile.hitObjects);

            handColorHandler.AssignHandTypes(osufile.hitObjects);

            // do conversion stuff here
            for (int i = 0; i < osufile.hitObjects.Count; i++)
            {
                var hitObject = osufile.hitObjects[i];

                var audicaDataPos = OsuUtility.GetAudicaPosFromHitObject(hitObject);
                var gridOffset = ConversionProcess.snapNotes ? new Cue.GridOffset() : audicaDataPos.offset;
                int tickLength = 120;
                if (hitObject.audicaBehavior == 3)
                {
                    tickLength = (int)hitObject.audicaEndTick - (int)hitObject.audicaTick;
                }

                var cue = new Cue
                    (
                        (int)hitObject.audicaTick,
                        tickLength,
                        audicaDataPos.pitch,
                        OsuUtility.GetVelocityForObject(hitObject),
                        gridOffset,
                        0f,
                        hitObject.audicaHandType,
                        hitObject.audicaBehavior
                    );
                hitObject.audicaCue = cue;
                diff.cues.Add(cue);
            }


            if (Config.parameters.snapNotes) SnapNormalTargets(diff.cues);
            if (Config.parameters.distributeStacks) RunStackDistributionPass(osufile.hitObjects);
            if (Config.parameters.minChainSize > 0f) RunChainResizePass(diff.cues);

            return diff;
        }

        private static void RunSliderSplitPass(List<HitObject> hitObjects, List<TimingPoint> timingPoints)
        {
            int hitObjectsOrgCount = hitObjects.Count;
            for (int i = 0; i < hitObjectsOrgCount - 1; i++)
            {
                HitObject hitObject = hitObjects[i];
                HitObject nextHitObject = hitObjects[i + 1];

                //Add a hitObject for sliders if the end is on beat and the next target is within 1/12th of the slider end.
                if ((hitObject.type == 2 || hitObject.type == 6) && OsuUtility.ticksSincePrevTimingPoint(hitObject.audicaEndTick, timingPoints) % 240f == 0f &&
                    nextHitObject.audicaTick - hitObject.audicaEndTick <= 160f)
                {
                    HitObject newHitObject = new HitObject
                    (
                        hitObject.repeats % 2 == 1 ? hitObject.endX : hitObject.x,
                        hitObject.repeats % 2 == 1 ? hitObject.endY : hitObject.y,
                        hitObject.endTime,
                        1,
                        hitObject.endHitsound,
                        0f,
                        0
                    );

                    newHitObject.endTime = newHitObject.time;
                    newHitObject.endX = newHitObject.x;
                    newHitObject.endY = newHitObject.y;
                    newHitObject.audicaTick = newHitObject.audicaEndTick = hitObject.audicaEndTick;
                    hitObjects.Add(newHitObject);
                }
            }
            hitObjects.Sort((ho1, ho2) => ho1.time.CompareTo(ho2.time));
        }

        private static void RunStreamScalePass(List<HitObjectGroup> noteStreams)
        {
            foreach (HitObjectGroup noteStream in noteStreams)
            {
                float averageStreamDistance = noteStream.length / (noteStream.hitObjects.Count - 1);
                if (averageStreamDistance > 0f && averageStreamDistance < Config.parameters.streamMinAverageDistance)
                {
                    noteStream.BoundScale(Config.parameters.streamMinAverageDistance / averageStreamDistance);
                }
            }
        }

        private static void RunFovScalePass(List<HitObject> hitObjects)
        {
            float fovRecenterTime = Config.parameters.fovRecenterTime;
            float scaleDistanceStartThres = Config.parameters.scaleDistanceStartThres;
            float scaleLogBase = Config.parameters.scaleLogBase;
            float scaleTimeThres = Config.parameters.convertChains ? Config.parameters.chainTimeThres : 0f;

            float fovX = 256f;
            float fovY = 192;

            for (int i = 0; i < hitObjects.Count; i++)
            {
                HitObject currentHitObject = hitObjects[i];
                HitObject prevHitObject = i > 0 ? hitObjects[i - 1] : null;

                if (prevHitObject != null && currentHitObject.time - prevHitObject.time >= fovRecenterTime)
                {
                    fovX = 256f;
                    fovY = 192f;
                }

                float diffX = currentHitObject.x - fovX;
                float diffY = currentHitObject.y - fovY;
                float diffLength = (float)Math.Sqrt(diffX * diffX + diffY * diffY);
                float scaledDiffLength;
                if (diffLength <= scaleDistanceStartThres)
                {
                    scaledDiffLength = diffLength;
                }
                else
                {
                    scaledDiffLength = scaleDistanceStartThres;
                    float logTranslation = 1f / (float)Math.Log(scaleLogBase);
                    scaledDiffLength += (float)Math.Log(diffLength - scaleDistanceStartThres + logTranslation, scaleLogBase) - (float)Math.Log(logTranslation, scaleLogBase);
                }
                float scaledDiffX = diffLength != 0  ? diffX / diffLength * scaledDiffLength : 0;
                float scaledDiffY = diffLength != 0 ?  diffY / diffLength * scaledDiffLength : 0;
                float newPosX = fovX + scaledDiffX;
                float newPosY = fovY + scaledDiffY;
                float translationX = newPosX - currentHitObject.x;
                float translationY = newPosY - currentHitObject.y;


                List<HitObject> syncTranslateHitObjects = new List<HitObject>();
                syncTranslateHitObjects.Add(currentHitObject);
                //If the object is in a stream, add all other stream objects to the group
                if (currentHitObject.noteStream != null)
                {
                    for (int j = 1; j < currentHitObject.noteStream.hitObjects.Count; j++)
                    {
                        syncTranslateHitObjects.Add(currentHitObject.noteStream.hitObjects[j]);
                        i++;
                    }
                }
                //Add more objects to the group as long as the time difference is less than scaleTimeThres
                while (i + 1 < hitObjects.Count && hitObjects[i+1].time - hitObjects[i].time < scaleTimeThres)
                {
                    syncTranslateHitObjects.Add(hitObjects[i + 1]);
                    i++;
                }

                HitObjectGroup hitObjectGroup = new HitObjectGroup(syncTranslateHitObjects);
                hitObjectGroup.BoundTranslate(translationX, translationY);

                fovX = hitObjectGroup.endX;
                fovY = hitObjectGroup.endY;
            }
        }

        private static void RunHitsoundPass(List<Cue> cues)
        {
            foreach (var cue in cues)
            {
                switch ((int)cue.behavior)
                {
                    case 4:
                        cue.velocity = 1;
                        break;
                    case 5:
                        cue.velocity = 2;
                        break;
                    case 6:
                        cue.velocity = 3; //melee for future purposes
                        break;

                    default:
                        break;
                }
            }
        }

        private static void SnapNormalTargets(List<Cue> cues)
        {
            foreach (Cue cue in cues)
            {
                if(cue.behavior == 0)
                {
                    cue.gridOffset = new Cue.GridOffset();
                }
            }
        }

        private static void RunChainPass(List<HitObject> hitObjects, List<TimingPoint> timingPoints)
        {

            HitObject prevChainHeadHitObject = null;

            for (int i = 0; i < hitObjects.Count; i++)
            {
                HitObject prevHitObject = i > 0 ? hitObjects[i - 1] : null;
                HitObject nextHitObject = i + 1 < hitObjects.Count ? hitObjects[i + 1] : null;
                HitObject nextNextHitObject = i + 2 < hitObjects.Count ? hitObjects[i + 2] : null;
                HitObject currentHitObject = hitObjects[i];

                bool isIgnoredChainEnd = Config.parameters.ignoreSlidersForChainConvert && (currentHitObject.type == 2 || currentHitObject.type == 6) &&
                    (nextHitObject == null || nextHitObject.time - currentHitObject.time > Config.parameters.chainTimeThres) || Config.parameters.ignoreSustainsForChainConvert && currentHitObject.audicaBehavior == 3;
                bool nextIsIgnoredChainEnd = nextHitObject == null || Config.parameters.ignoreSlidersForChainConvert && (nextHitObject.type == 2 || nextHitObject.type == 6) &&
                    (nextNextHitObject == null || nextNextHitObject.time - nextHitObject.time > Config.parameters.chainTimeThres) || Config.parameters.ignoreSustainsForChainConvert && nextHitObject.audicaBehavior == 3;

                if (isIgnoredChainEnd)
                    continue;

                if ((prevHitObject == null || currentHitObject.time - prevHitObject.time > Config.parameters.chainTimeThres) && nextHitObject != null &&
                    nextHitObject.time - currentHitObject.time <= Config.parameters.chainTimeThres && !nextIsIgnoredChainEnd)
                {
                    currentHitObject.audicaBehavior = 4;
                    prevChainHeadHitObject = currentHitObject;
                }
                else if (prevHitObject != null && currentHitObject.time - prevHitObject.time <= Config.parameters.chainTimeThres)
                {   
                    if (currentHitObject.time - prevChainHeadHitObject.time > Config.parameters.chainTimeThres && currentHitObject.audicaTick - prevChainHeadHitObject.audicaTick >= Config.parameters.chainSwitchFrequency && nextHitObject != null &&
                        nextHitObject.time - currentHitObject.time <= Config.parameters.chainTimeThres && OsuUtility.ticksSincePrevTimingPoint(currentHitObject.audicaTick, timingPoints) % Config.parameters.chainSwitchFrequency == 0 && !nextIsIgnoredChainEnd)
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

            //Find and prune too short chains
            List<HitObject> chainHitObjects = new List<HitObject>();
            foreach (HitObject hitObject in hitObjects)
            {
                if (hitObject.audicaBehavior == 4)
                {
                    if (chainHitObjects.Count != 0) CheckAndPruneChain(chainHitObjects, timingPoints);
                    chainHitObjects = new List<HitObject>();
                    chainHitObjects.Add(hitObject);
                }
                else if (hitObject.audicaBehavior == 5)
                {
                    chainHitObjects.Add(hitObject);
                }
            }
            if (chainHitObjects.Count != 0) CheckAndPruneChain(chainHitObjects, timingPoints);
        }

        private static void CheckAndPruneChain(List<HitObject> chainHitObjects, List<TimingPoint> timingPoints)
        {
            if (chainHitObjects.Count <= Config.parameters.minChainLinks)
            {
                //Choose the chain hit object with highest GCD with a measure 
                int bestGcd = 0;
                HitObject bestGcdHitObject = null;
                foreach (HitObject chainHitObject in chainHitObjects)
                {
                    chainHitObject.audicaBehavior = -1;
                    int gcd = OsuUtility.GCD((int)OsuUtility.ticksSincePrevTimingPoint(chainHitObject.audicaTick, timingPoints), 1920);
                    if (gcd > bestGcd)
                    {
                        bestGcd = gcd;
                        bestGcdHitObject = chainHitObject;
                    }
                }
                bestGcdHitObject.audicaBehavior = 0;
            }
        }

        private static void RunSustainPass(List<HitObject> hitObjects, List<TimingPoint> timingPoints)
        {
            for (int i = 0; i < hitObjects.Count; i++)
            {
                HitObject currentHitObject = hitObjects[i];
                HitObject nextHitObject = i + 1 < hitObjects.Count ? hitObjects[i + 1] : null;
                HitObject nextNextHitObject = i + 2 < hitObjects.Count ? hitObjects[i + 2] : null;

                if (currentHitObject.audicaBehavior == 4) continue;

                //Extend duration to next target if next target is on beat and within extension time
                if (nextHitObject != null && nextHitObject.audicaTick - currentHitObject.audicaEndTick <= Config.parameters.sustainExtension)
                {
                    if (OsuUtility.ticksSincePrevTimingPoint(nextHitObject.audicaTick, timingPoints) % 480f == 0f)
                    {
                        currentHitObject.endTime = nextHitObject.time;
                        currentHitObject.audicaEndTick = nextHitObject.audicaTick;
                    }
                }

                //Shorten duration if there are two targets within too short time after the sustain
                if (nextNextHitObject != null && nextNextHitObject.time - currentHitObject.endTime < Config.parameters.holdRestTime)
                {
                    currentHitObject.audicaEndTick -= 240f;
                    currentHitObject.endTime = OsuUtility.TickToMs(currentHitObject.audicaEndTick, timingPoints);
                }

                if (currentHitObject.audicaEndTick - currentHitObject.audicaTick >= Config.parameters.minSustainLength)
                {
                    currentHitObject.audicaBehavior = 3;
                }
            }
        }

        private static void ResetEndTimesAndPos(List<HitObject> hitObjects)
        {
            foreach (HitObject hitObject in hitObjects)
            {
                if (hitObject.audicaBehavior != 3 && hitObject.audicaBehavior != 4)
                {
                    hitObject.endTime = hitObject.time;
                    hitObject.audicaEndTick = hitObject.audicaTick;
                }

                if (hitObject.audicaBehavior != 4)
                {
                    hitObject.endX = hitObject.x;
                    hitObject.endY = hitObject.y;
                }
            }
        }

        private static void RemoveUnusedHitObjects(List<HitObject> hitObjects)
        {
            for (int i = hitObjects.Count-1; i >= 0; i--)
            {
                if (hitObjects[i].audicaBehavior < 0) hitObjects.RemoveAt(i);
            }
        }

        private class TargetStack
        {
            public Cue stackStartCue;
            public List<Cue> tailCues;
            public float stackMovementSpeed;
            public float lastStackTick;
            public OsuUtility.Coordinate2D lastPos;
            public OsuUtility.Coordinate2D direction;

            public TargetStack()
            {
                tailCues = new List<Cue>();
            }
        }

        private static void RunStackDistributionPass(List<HitObject> hitObjects)
        {
            float stackInclusionRange = Config.parameters.stackInclusionRange;
            float stackItemDistance = Config.parameters.stackItemDistance; //Offset for stack items. Time proportionate distancing is used through the stack based on getting this distance between first and second item in stack
            float stackMaxDistance = Config.parameters.stackMaxDistance;
            float stackResetTime = Config.parameters.stackResetTime;

            List<TargetStack> allStacks = new List<TargetStack>();
            List<TargetStack> activeStacks = new List<TargetStack>();

            for (int i = 0; i < hitObjects.Count; i++)
            {
                HitObject currentHitObject = hitObjects[i];
                HitObject prevHitObject = i - 1 >= 0 ? hitObjects[i - 1] : null;
                HitObject nextHitObject = i + 1 < hitObjects.Count ? hitObjects[i + 1] : null;
                Cue currentCue = currentHitObject.audicaCue;
                Cue prevCue = prevHitObject?.audicaCue;
                Cue nextCue = nextHitObject?.audicaCue;

                bool isInStream = currentHitObject.noteStream != null;
                bool isStreamStart = isInStream && currentHitObject == currentHitObject.noteStream.hitObjects[0];
                bool isStreamEnd = isInStream && currentHitObject == currentHitObject.noteStream.hitObjects[currentHitObject.noteStream.hitObjects.Count - 1];

                if (isStreamStart) activeStacks = new List<TargetStack>();

                bool prevPosDifferent = prevHitObject == null || prevHitObject.x != currentHitObject.x || prevHitObject.y != currentHitObject.y;
                bool nextPosDifferent = nextHitObject == null || nextHitObject.x != currentHitObject.x || nextHitObject.y != currentHitObject.y;


                //Ignore moving targets in streams other than stream head and tail
                if (isInStream && !isStreamStart && !isStreamEnd && prevPosDifferent && nextPosDifferent ) continue;

                //Remove unactive stacks
                for (int j = activeStacks.Count - 1; j >= 0; j--)
                {
                    if (currentCue.tick - activeStacks[j].lastStackTick >= stackResetTime)
                        activeStacks.RemoveAt(j);
                }

                OsuUtility.Coordinate2D currentCuePos = OsuUtility.GetPosFromCue(currentCue);

                //Check if target fits in a currently active stack, if not create a stack for this target
                TargetStack stack = activeStacks.Find(s => OsuUtility.EuclideanDistance(currentCuePos.x, currentCuePos.y, s.lastPos.x, s.lastPos.y) <= stackInclusionRange);
                if (stack == null)
                {
                    TargetStack newStack = new TargetStack();
                    newStack.stackStartCue = currentCue;
                    newStack.lastStackTick = currentCue.tick;
                    newStack.lastPos = OsuUtility.GetPosFromCue(currentCue);
                    allStacks.Add(newStack);
                    activeStacks.Add(newStack);
                    continue;
                }

                if (stack.direction.x == 0f && stack.direction.y == 0f)
                {

                    OsuUtility.Coordinate2D? dirToNextCue = null;
                    for (int j = 1; j + i < hitObjects.Count; j++)
                    {
                        Cue otherCue = hitObjects[i + j].audicaCue;
                        if (!(OsuUtility.DistanceBetweenCues(currentCue, otherCue) <= stackInclusionRange))
                        {
                            OsuUtility.Coordinate2D otherCuePos = OsuUtility.GetPosFromCue(otherCue);
                            dirToNextCue = new OsuUtility.Coordinate2D(otherCuePos.x - currentCuePos.x, otherCuePos.y - currentCuePos.y);
                            break;
                        }
                    }

                    OsuUtility.Coordinate2D? dirFromPrevCue = null;
                    for (int j = 1; j <= i; j++)
                    {
                        Cue otherCue = hitObjects[i - j].audicaCue;
                        if (!(OsuUtility.DistanceBetweenCues(currentCue, otherCue) <= stackInclusionRange) &&
                            otherCue.tick < stack.stackStartCue.tick)
                        {
                            OsuUtility.Coordinate2D otherCuePos = OsuUtility.GetPosFromCue(otherCue);
                            dirFromPrevCue = new OsuUtility.Coordinate2D(currentCuePos.x - otherCuePos.x, currentCuePos.y - otherCuePos.y);
                            break;
                        }
                    }

                    OsuUtility.Coordinate2D direction = new OsuUtility.Coordinate2D(0f, -1f);
                    if (dirFromPrevCue != null) direction = (OsuUtility.Coordinate2D)dirFromPrevCue;
                    else if (dirToNextCue != null) direction = (OsuUtility.Coordinate2D)dirToNextCue;

                    //normalize
                    float length = (float)Math.Sqrt(direction.x * direction.x + direction.y * direction.y);
                    direction.x /= length;
                    direction.y /= length;

                    stack.direction = direction;
                }
                stack.tailCues.Add(currentCue);
                stack.lastPos = currentCuePos;
                stack.lastStackTick = currentCue.tick;
            }

            foreach (TargetStack stack in allStacks)
            {
                if (stack.tailCues.Count == 0)
                    continue;
                stack.stackMovementSpeed = Math.Min(stackItemDistance / (stack.tailCues[0].tick - stack.stackStartCue.tick), stackMaxDistance / (stack.tailCues[stack.tailCues.Count - 1].tick - stack.stackStartCue.tick));
                OsuUtility.Coordinate2D stackHeadPos = OsuUtility.GetPosFromCue(stack.stackStartCue);

                //Direction to move the right hand for hand separation.
                float handSepDirectionX = -stack.direction.y;
                float handSepDirectionY = stack.direction.x;
                if (stack.direction.y > 0)
                {
                    handSepDirectionX = -handSepDirectionX;
                    handSepDirectionY = -handSepDirectionY;
                }

                Cue cue = stack.stackStartCue;
                for (int i = -1; i < stack.tailCues.Count; i++)
                {
                    if (i >= 0) cue = stack.tailCues[i];
                    float distributionX = stack.direction.x * stack.stackMovementSpeed * (cue.tick - stack.stackStartCue.tick);
                    float distributionY = stack.direction.y * stack.stackMovementSpeed * (cue.tick - stack.stackStartCue.tick);
                    float handSepX = 0f;
                    float handSepY = 0f;
                    if (cue.behavior != Cue.Behavior.ChainStart || cue.behavior != Cue.Behavior.Chain)
                    {
                        handSepX = (cue.handType == Cue.HandType.Right ? handSepDirectionX : -handSepDirectionX) * Config.parameters.stackHandSeparation / 2;
                        handSepY = (cue.handType == Cue.HandType.Right ? handSepDirectionY : -handSepDirectionY) * Config.parameters.stackHandSeparation / 2;
                    }
                    OsuUtility.Coordinate2D newPos = new OsuUtility.Coordinate2D(stackHeadPos.x + distributionX + handSepX,
                        stackHeadPos.y + distributionY + handSepY);
                    OsuUtility.SetCuePos(cue, newPos);
                }
            }
        }

        private static void RunChainResizePass(List<Cue> cues)
        {
            Cue chainHead = null;
            List<Cue> chainLinks = new List<Cue>();
            foreach (Cue cue in cues)
            {
                if (cue.behavior == Cue.Behavior.ChainStart)
                {
                    if (chainHead != null) ResizeChain(chainHead, chainLinks);
                    chainHead = cue;
                    chainLinks = new List<Cue>();
                }
                else if (cue.behavior == Cue.Behavior.Chain)
                {
                    chainLinks.Add(cue);
                }
            }
            if (chainHead != null) ResizeChain(chainHead, chainLinks);
        }

        private static void ResizeChain(Cue chainHead, List<Cue> chainLinks)
        {
            float minChainSize = Config.parameters.minChainSize;
            float chainSize = 0f;
            foreach (Cue chainLink in chainLinks)
            {
                chainSize = Math.Max(chainSize, OsuUtility.DistanceBetweenCues(chainLink, chainHead));
            }

            if (chainSize > 0f && chainSize < minChainSize)
            {
                float scaleFactor = minChainSize / chainSize;
                OsuUtility.Coordinate2D chainHeadPos = OsuUtility.GetPosFromCue(chainHead);
                foreach (Cue chainLink in chainLinks)
                {
                    OsuUtility.Coordinate2D chainLinkPos = OsuUtility.GetPosFromCue(chainLink);
                    chainLinkPos = OsuUtility.ScalePos(chainLinkPos, chainHeadPos, scaleFactor);
                    OsuUtility.SetCuePos(chainLink, chainLinkPos);
                }
            }
        }

        private static void RunMeleePass(List<Cue> cues, List<TimingPoint> timingPoints, List<TimingPoint> mergedTimingPoints, string difficultyName)
        {
            MeleeOptions meleeOptions = new MeleeOptions();
            switch (difficultyName.ToLower())
            {
                case ("expert"):
                    meleeOptions = Config.parameters.expertMeleeOptions;
                    break;
                case ("advanced"):
                    meleeOptions = Config.parameters.advancedMeleeOptions;
                    break;
                case ("standard"):
                    meleeOptions = Config.parameters.standardMeleeOptions;
                    break;
                case ("beginner"):
                    meleeOptions = Config.parameters.beginnerMeleeOptions;
                    break;
            }
            float fovRecenterTime = Config.parameters.fovRecenterTime;

            if (!meleeOptions.convertMelees) return;

            bool prevMeleeRight = false;
            for (int i = 0; i < cues.Count; i++)
            {
                Cue currentCue = cues[i];
                Cue prevCue = i > 0 ? cues[i - 1] : null;
                Cue nextCue = i + 1 < cues.Count ? cues[i + 1] : null;
                float currentCueMsTime = OsuUtility.TickToMs(currentCue.tick, timingPoints);
                float prevCueMsTime = prevCue != null ? OsuUtility.TickToMs(prevCue.tick, timingPoints) : 0f;
                float nextCueMsTime = nextCue != null ? OsuUtility.TickToMs(nextCue.tick, timingPoints) : 0f;
                TimingPoint prevNormalTimingPoint = OsuUtility.getPrevTimingPoint(currentCue.tick, timingPoints);
                TimingPoint prevEitherTimingPoints = OsuUtility.getPrevTimingPoint(currentCue.tick, mergedTimingPoints);

                float timeSinceTimingPoint = currentCue.tick - prevNormalTimingPoint.audicaTick;
                float frequency = prevEitherTimingPoints.kiai ? meleeOptions.kiaiFrequency : meleeOptions.normalFrequency;
                if (frequency == 0) continue;
                bool onMeleeConvertTime = timeSinceTimingPoint > 0 && timeSinceTimingPoint % (480f * prevNormalTimingPoint.meter / frequency) == 0;
                if (onMeleeConvertTime && currentCue.behavior != Cue.Behavior.Hold && currentCue.behavior != Cue.Behavior.ChainStart && currentCue.behavior != Cue.Behavior.Chain)
                {
                    //Check melee conversion conditions for each target
                    bool rightMeleeOk = true;
                    bool leftMeleeOk = true;

                    foreach (Cue otherCue in cues.Where(
                        otherCue =>
                        {
                            float otherCueMsTime = OsuUtility.TickToMs(otherCue.tick, timingPoints);
                            return otherCue.behavior != Cue.Behavior.Melee && otherCueMsTime >= currentCueMsTime - meleeOptions.preRestTime && otherCueMsTime <= currentCueMsTime + meleeOptions.postRestTime;
                        }
                    ))
                    {
                        if (otherCue == currentCue) continue;


                        //Don't convert to melee if other targets for the same hand are within rest window
                        if (otherCue.handType == Cue.HandType.Right) rightMeleeOk = false;
                        if (otherCue.handType == Cue.HandType.Left) leftMeleeOk = false;

                        //Don't convert to melee if any targets within the rest window are outside the corresponding melee target position window
                        OsuUtility.Coordinate2D otherCuePos = OsuUtility.GetPosFromCue(otherCue);
                        if (otherCuePos.x > 7.5f - meleeOptions.positionWindowMinDistance || otherCuePos.x < 7.5f - meleeOptions.positionWindowMaxDistance) rightMeleeOk = false;
                        if (otherCuePos.x < 3.5f + meleeOptions.positionWindowMinDistance || otherCuePos.x > 3.5f + meleeOptions.positionWindowMaxDistance) leftMeleeOk = false;

                    }

                    //Require previous target to either be within the melee target position window or sufficiently long ago that fov has recentered
                    if (prevCue!= null && prevCue.behavior != Cue.Behavior.Melee && currentCueMsTime - prevCueMsTime < fovRecenterTime)
                    {
                        OsuUtility.Coordinate2D prevHitObjectPos = OsuUtility.GetPosFromCue(prevCue);
                        if (prevHitObjectPos.x > 7.5f - meleeOptions.positionWindowMinDistance || prevHitObjectPos.x < 7.5f - meleeOptions.positionWindowMaxDistance) rightMeleeOk = false;
                        if (prevHitObjectPos.x < 3.5f + meleeOptions.positionWindowMinDistance || prevHitObjectPos.x > 3.5f + meleeOptions.positionWindowMaxDistance) leftMeleeOk = false;
                    }

                    //Require next target to be on the inside of the side of the position window far away from the melee, or be sufficiently long ago that the fov has recentered
                    if (nextCue != null && currentCueMsTime - nextCueMsTime < fovRecenterTime)
                    {
                        OsuUtility.Coordinate2D nextHitObjectPos = OsuUtility.GetPosFromCue(nextCue);
                        if (nextHitObjectPos.x < 7.5f - meleeOptions.positionWindowMaxDistance) rightMeleeOk = false;
                        if (nextHitObjectPos.x > 3.5f + meleeOptions.positionWindowMaxDistance) leftMeleeOk = false;
                    }
                    
                    //Convert to melee
                    if (rightMeleeOk || leftMeleeOk)
                    {
                        //Prioritize opposite hand of previous melee if both are ok
                        if (rightMeleeOk && leftMeleeOk)
                        {
                            rightMeleeOk = !prevMeleeRight;
                        }

                        currentCue.handType = Cue.HandType.Either;
                        currentCue.behavior = Cue.Behavior.Melee;
                        currentCue.pitch = rightMeleeOk ? 101 : 100;
                        currentCue.gridOffset = new Cue.GridOffset();

                        prevMeleeRight = rightMeleeOk;
                    }
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

        public static Difficulty ScaleDifficulty(Difficulty unscaledDifficulty, float scaleX, float scaleY)
        {
            Difficulty scaledDifficulty = OsuUtility.DeepClone(unscaledDifficulty);
            foreach (Cue cue in scaledDifficulty.cues)
            {
                if (cue.behavior == Cue.Behavior.Melee) continue;
                OsuUtility.Coordinate2D cuePos = OsuUtility.GetPosFromCue(cue);
                cuePos.x = (cuePos.x - 5.5f) * scaleX + 5.5f;
                cuePos.y = (cuePos.y - 3f) * scaleY + 3f;
                OsuUtility.SetCuePos(cue, cuePos);
            }

            return scaledDifficulty;
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


