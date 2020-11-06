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
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace AudicaConverter
{
    class Program
    {
        public static string version = "1.0.3"; //https://github.com/octoberU/AudicaConverter/wiki/Creating-a-new-release-for-the-updater
        public static string workingDirectory = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);

        public static string FfmpegName {
            get 
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    return "ffmpegOSX";
                }
                else return "ffmpeg.exe";
            }
            private set { } 
        }
        public static string Ogg2moggName
        {
            get
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    return "ogg2moggOSX";
                }
                else return "ogg2mogg.exe";
            }
            private set { }
        }

        static void Main(string[] args)
        {
            Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture; //Ensures locale independent parsing.
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

            var argsList = args.ToList();
            if (argsList.Count == 0 && !RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                Console.WriteLine("\nIn order to convert maps, drag-and-drop one or more .osz files, or folders of .osz files onto the AudicaConverter exe within file explorer, or onto this window and press enter.");
                string input = Console.ReadLine();
                input = input.Replace("\"", "");
                argsList.Add(input);
            }

            List<string> oszFileNames = new List<string>();
            foreach (var item in argsList)
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
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                string osxInputDirectory = Path.Join(Program.workingDirectory, "Input");
                if (Directory.Exists(osxInputDirectory))
                {
                    foreach (var fileName in Directory.GetFiles(osxInputDirectory))
                    {
                        if (fileName.Contains(".osz")) oszFileNames.Add(fileName);
                    }
                }
                else Directory.CreateDirectory(osxInputDirectory);
            }

            for (int i = 0; i < oszFileNames.Count; i++)
            {
                string oszFileName = oszFileNames[i];
                string[] pathElements = oszFileName.Split("\\");
                string oszName = pathElements[pathElements.Length-1];
                if (Config.parameters.converterOperationOptions.autoMode)
                {
                    Console.Clear();
                    Console.WriteLine(@$"({i + 1}/{oszFileNames.Count}) Converting {oszName}...");
                }

                try
                {
                    ConversionProcess.ConvertToAudica(oszFileName, Config.parameters.converterOperationOptions.autoMode ? "auto" : "manual");
                }
                catch (Exception e)
                {
                    Console.Clear();
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine(@$"({i + 1}/{oszFileNames.Count}) {oszName} Failed to convert with the following error:");
                    Console.WriteLine();
                    Console.WriteLine(e.ToString());
                    Console.ForegroundColor = ConsoleColor.Gray;

                    if (Config.parameters.converterOperationOptions.autoMode)
                    {
                        Thread.Sleep(5000);
                    }
                    else
                    {
                        Console.WriteLine("[Press enter to continue]");
                        Console.ReadLine();
                    }
                }
            }
            //ConversionProcess.ConvertToAudica(@"C:\audica\Repos\AudicaConverter\bin\Release\netcoreapp3.1\393663 UNDEAD CORPORATION - Flowering Night Fever.osz", "manual");
        }
    }

    public class ConversionProcess
    {
        public static bool snapNotes = false;
        public static void ConvertToAudica(string filePath, string mode)
        {
            var osz = new OSZ(filePath);

            var audica = new Audica(Path.Join(Program.workingDirectory, "template.audica"));

            ConvertTempos(osz, ref audica);

            //Find number of osu!standard difficulties.
            int standardDiffCount = osz.osufiles.Count(of => of.general.mode == 0);

            int convertMode = 1;
            if (mode == "manual")
            {
                Console.Clear();
                Console.WriteLine($"{osz.osufiles[0].metadata.artist} - {osz.osufiles[0].metadata.title}" +
                $"\nMapped by {osz.osufiles[0].metadata.creator}" +
                $"\nFound {standardDiffCount} osu!standard difficulties ({osz.osufiles.Count} difficulties across all modes)");
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("\n\nSelect Conversion Mode: \n[1] Convert whole map.\n[2] Convert audio/timing only.");
                Console.ForegroundColor = ConsoleColor.Gray;
                convertMode = int.Parse(Console.ReadLine());
            }

            if (convertMode == 1)
            {
                //Remove difficulties without any hitobjects
                for (int i = osz.osufiles.Count - 1; i >= 0; i--)
                {
                    if (osz.osufiles[i].hitObjects.Count == 0)
                    {
                        if (osz.osufiles[i].general.mode != 0) standardDiffCount--;
                        osz.osufiles.RemoveAt(i);
                    }
                }

                if (!Config.parameters.generalOptions.allowOtherGameModes && standardDiffCount == 0)
                {
                    if (mode == "manual")
                    {
                        Console.WriteLine("\nThis song has no osu!standard difficulties. While full map conversion of other modes are possible, they generally make for unplayable maps and " +
                            "are disabled by default. You can enable conversion of other modes in the config.json, but this is not recommended if you intend to play the maps unedited. " +
                            "[Press enter to continue]");
                        Console.ReadLine();
                    }
                    return;
                }

                if (osz.osufiles.Count == 0)
                {
                    if (mode == "manual")
                    {
                        Console.WriteLine("\nThis song has no non-empty difficulties, and can therefor not be converted. " +
                            "[Press enter to continue]");
                        Console.ReadLine();
                    }
                    return;
                }

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
                foreach (Osufile file in osz.osufiles)
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

            if (Config.parameters.endPitchKeyOptions.scrapeKey)
            {
                if (mode == "manual")
                {
                    Console.Clear();
                    Console.WriteLine("Scraping key");
                }
                audica.desc.songEndEvent = KeyScraper.GetSongEndEvent(osz.osufiles[0].metadata.artist, osz.osufiles[0].metadata.title);
            }
            else audica.desc.songEndEvent = Config.parameters.endPitchKeyOptions.defaultEndEvent;

            ConvertMetadata(osz, audica);

            string audicaFileName = $"{audica.desc.songID}.audica";

            //at the end
            ExportConvert(audica, audicaFileName);
        }

        private static void ExportConvert(Audica audica, string audicaFileName)
        {
            if (Config.parameters.converterOperationOptions.customExportDirectory == "")
            {
                ExportToDefaultDirectory(audica, audicaFileName);
            }
            else
            {
                if (Directory.Exists(Config.parameters.converterOperationOptions.customExportDirectory))
                {
                    audica.Export(Path.Join(Config.parameters.converterOperationOptions.customExportDirectory, audicaFileName));
                }
                else
                {
                    Console.WriteLine("Custom directory doesn't exist, exporting to default /audicaFiles");
                    ExportToDefaultDirectory(audica, audicaFileName);
                }
            }

            static void ExportToDefaultDirectory(Audica audica, string audicaFileName)
            {
                string audicaExportPath = Path.Join(Program.workingDirectory, "audicaFiles");
                if (!Directory.Exists(audicaExportPath)) Directory.CreateDirectory(audicaExportPath);
                audica.Export(Path.Join(audicaExportPath, audicaFileName));
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
            string mapperName = Config.parameters.generalOptions.customMapperName == "" ? RemoveSpecialCharacters(osz.osufiles[0].metadata.creator) : RemoveSpecialCharacters(Config.parameters.generalOptions.customMapperName);
            audica.desc.title = osz.osufiles[0].metadata.title;
            audica.desc.artist = osz.osufiles[0].metadata.artist;
            audica.desc.author = mapperName;
            audica.desc.songID = RemoveSpecialCharacters(osz.osufiles[0].metadata.title) + "-" + mapperName;
            audica.desc.previewStartSeconds = (float)osz.osufiles[0].general.previewTime / 1000f;
            audica.desc.fxSong = "";
        }

        private static Difficulty AskDifficulty(OSZ osz, Audica audica, string difficultyName, string mode)
        {
            MapScaleOptions scalingOptions = null;
            AutoOptions autoOptions = null;
            switch (difficultyName.ToLower())
            {
                case ("expert"):
                    scalingOptions = Config.parameters.scalingOptions.expertMapScaleOptions;
                    autoOptions = Config.parameters.converterOperationOptions.expertAutoOptions;
                    break;
                case ("advanced"):
                    scalingOptions = Config.parameters.scalingOptions.advancedMapScaleOptions;
                    autoOptions = Config.parameters.converterOperationOptions.advancedAutoOptions;
                    break;
                case ("standard"):
                    scalingOptions = Config.parameters.scalingOptions.standardMapScaleOptions;
                    autoOptions = Config.parameters.converterOperationOptions.standardAutoOptions;
                    break;
                case ("beginner"):
                    scalingOptions = Config.parameters.scalingOptions.beginnerMapScaleOptions;
                    autoOptions = Config.parameters.converterOperationOptions.beginnerAutoOptions;
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
            var scaledDifficulties = new List<(Difficulty difficulty, Osufile osufile, float difficultyRating)>();
            int count = 1;
            for (int i = 0; i < osz.osufiles.Count; i++)
            {
                if (!Config.parameters.generalOptions.allowOtherGameModes && osz.osufiles[i].general.mode != 0) continue; //Don't allow full conversion of other modes than osu!standard
                Difficulty scaledDifficulty = ScaleDifficulty(osz.osufiles[i].audicaDifficulty, scalingOptions.xScale, scalingOptions.yScale, scalingOptions.zOffset);
                RunMeleePass(scaledDifficulty.cues, osz.osufiles[i].timingPoints, osz.osufiles[i].mergedTimingPoints, difficultyName);
                if (Config.parameters.generalOptions.useStandardSounds) RunHitsoundPass(scaledDifficulty.cues);
                float difficultyRating = audica.GetRatingForDifficulty(scaledDifficulty);
                scaledDifficulties.Add((scaledDifficulty, osz.osufiles[i], difficultyRating));

                if (mode == "manual") Console.WriteLine($"\n[{count}]{osz.osufiles[i].metadata.version} [{difficultyRating.ToString("n2")} Audica difficulty]");
                count++;
            }

            if (scaledDifficulties.Count == 0)
            {
                return null;
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
                
            if (Config.parameters.generalOptions.customMapperName == "")
            {
                switch (difficultyName.ToLower())
                {
                    case ("expert"):
                        audica.desc.customExpert = scaledDifficulties[difficultyIdx].osufile.metadata.version;
                        break;
                    case ("advanced"):
                        audica.desc.customAdvanced = scaledDifficulties[difficultyIdx].osufile.metadata.version;
                        break;
                    case ("standard"):
                        audica.desc.customModerate = scaledDifficulties[difficultyIdx].osufile.metadata.version;
                        break;
                    case ("beginner"):
                        audica.desc.customBeginner = scaledDifficulties[difficultyIdx].osufile.metadata.version;
                        break;
                }
            }

            return scaledDifficulties[difficultyIdx].difficulty;
        }

        private static void ConvertSongToOGG(ref OSZ osz, Audica audica)
        {
            string audioFileName = osz.osufiles[0].general.audioFileName;
            string tempDirectory = Path.Join(Program.workingDirectory, "AudicaConverterTemp");
            string tempAudioPath = Path.Join(tempDirectory, "audio.mp3");
            string tempAudioPath2 = Path.Join(tempDirectory, "audio2.mp3");
            string tempOggPath = Path.Join(tempDirectory, "tempogg.ogg");
            string tempMoggPath = Path.Join(tempDirectory, "tempMogg.mogg");

            float firstHitObjectTime = float.PositiveInfinity;
            foreach (var osufile in osz.osufiles)
            {
                if (osufile.hitObjects.Count > 0 && osufile.hitObjects[0].time < firstHitObjectTime)
                {
                    firstHitObjectTime = osufile.hitObjects[0].time;
                } 
            }
            float paddingTime = 0f;
            if (firstHitObjectTime < Config.parameters.generalOptions.introPadding)
            {
                paddingTime = Config.parameters.generalOptions.introPadding - firstHitObjectTime;
            }
            else if (Config.parameters.generalOptions.skipIntroOptions.enabled && firstHitObjectTime > Config.parameters.generalOptions.skipIntroOptions.threshold)
            {
                //Checks if the first hitobject is after the threshold, if it is, we cut it.
                paddingTime = (firstHitObjectTime - Config.parameters.generalOptions.skipIntroOptions.cutIntroTime) * -1; //We need a negative value to not mess wih padding
                
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
                pruneString = $"-ss {(0.025 + pruneValue).ToString("n3")}";
            }

            if (Directory.Exists(tempDirectory)) Directory.Delete(tempDirectory, true);

            Directory.CreateDirectory(tempDirectory);

            ZipArchive zip = ZipFile.OpenRead(osz.oszFilePath);
            foreach (ZipArchiveEntry entry in zip.Entries) {
                // Check for the name while ignoring case, as osu ignores case as well.
                if (entry.FullName.Equals(audioFileName, StringComparison.OrdinalIgnoreCase)) {
                    zip.GetEntry(entry.FullName).ExtractToFile(tempAudioPath);
                }
            }

            Process ffmpeg = new Process();
            ffmpeg.StartInfo.FileName = Path.Join(Program.workingDirectory, Program.FfmpegName);
            ffmpeg.StartInfo.WindowStyle = ProcessWindowStyle.Normal;
            ffmpeg.StartInfo.UseShellExecute = false;
            ffmpeg.StartInfo.RedirectStandardOutput = false;

            Process ogg2mogg = new Process();
            ogg2mogg.StartInfo.FileName = Path.Join(Program.workingDirectory, Program.Ogg2moggName);
            ogg2mogg.StartInfo.WindowStyle = ProcessWindowStyle.Normal;
            ogg2mogg.StartInfo.UseShellExecute = false;
            ogg2mogg.StartInfo.RedirectStandardOutput = true;

            ogg2mogg.StartInfo.Arguments = $"\"{tempOggPath}\" \"{tempMoggPath}\"";

            string paddingString = paddingTime > 0 ? $"-af \"adelay = {paddingTime} | {paddingTime}\"" : "";

            string outputPath = paddingTime < 0f ? tempAudioPath2 : tempOggPath;
            ffmpeg.StartInfo.Arguments = $"-y -i \"{tempAudioPath}\" -hide_banner -loglevel panic -ac 2 -ar 44100 -ab 256k {pruneString} {paddingString} -map 0:a \"{outputPath}\"";
            ffmpeg.Start();
            ffmpeg.WaitForExit();

            if (paddingTime < 0f)
            {
                //Reprocess the ogg file with fade, this might need "-ss 0.025"
                float fadeTime = Config.parameters.generalOptions.skipIntroOptions.cutIntroTime / 1000f / 2;
                ffmpeg.StartInfo.Arguments = $"-y -i \"{tempAudioPath2}\" -hide_banner -loglevel panic -ac 2 -ab 256k -af \"afade=t=in:st=0:d={fadeTime.ToString("n1")}\" -map 0:a \"{tempOggPath}\"";
                ffmpeg.Start();
                ffmpeg.WaitForExit();
            }

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

        public static Difficulty ConvertToAudica(Osufile osufile)
        {
            var diff = new Difficulty();
            diff.cues = new List<Cue>();
            var handColorHandler = new HandColorHandler();


            if (Config.parameters.sliderConversionOptions.sliderEndStreamStartConvert) RunSliderSplitPass(osufile.hitObjects, osufile.timingPoints);
            if (Config.parameters.streamOptions.streamMinAverageDistance > 0f) RunStreamScalePass(osufile.noteStreams);
            if (Config.parameters.sustainConversionOptions.convertSustains) RunSustainPass(osufile.hitObjects, osufile.timingPoints);
            if (Config.parameters.chainConversionOptions.convertChains) RunChainPass(osufile.hitObjects, osufile.timingPoints);
            ResetEndTimesAndPos(osufile.hitObjects);
            RemoveUnusedHitObjects(osufile.hitObjects, osufile.noteStreams);
            if (Config.parameters.scalingOptions.adaptiveScalingOptions.useAdaptiveScaling) RunFovScalePass(osufile.hitObjects);

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


            if (Config.parameters.generalOptions.snapNotes) SnapNormalTargets(diff.cues);
            if (Config.parameters.stackDistributionOptions.distributeStacks) RunStackDistributionPass(osufile.hitObjects, diff.cues);
            if (Config.parameters.chainConversionOptions.minSize > 0f) RunChainEnlargePass(diff.cues);

            return diff;
        }

        private static void RunSliderSplitPass(List<HitObject> hitObjects, List<TimingPoint> timingPoints)
        {
            SliderConversionOptions sliderConversionOptions = Config.parameters.sliderConversionOptions;

            int hitObjectsOrgCount = hitObjects.Count;
            for (int i = 0; i < hitObjectsOrgCount; i++)
            {
                HitObject hitObject = hitObjects[i];

                if ((hitObject.type == 2 || hitObject.type == 6) && hitObject.repeats > 1)
                {
                    float repeatTime = (hitObject.endTime - hitObject.time) / hitObject.repeats;
                    float repeatTicks = (hitObject.audicaEndTick - hitObject.audicaTick) / hitObject.repeats;

                    bool fastRepeat = repeatTime < sliderConversionOptions.fastRepeatTimeThres;

                    //Find slider to target conversion step frequency.
                    int meter = OsuUtility.getPrevTimingPoint(hitObject.audicaTick, timingPoints).meter;
                    int meterSmallestFactor = meter;
                    for (int j = 2; j <= 100; j++)
                    {
                        if (meter % j == 0)
                        {
                            meterSmallestFactor = j;
                            break;
                        }
                    }
                    int repeatStep = 1;
                    if (!(Config.parameters.chainConversionOptions.convertChains && repeatTime <= Config.parameters.chainConversionOptions.timeThres))
                    {
                        while (repeatStep * repeatTime < sliderConversionOptions.targetMinTime) repeatStep *= meterSmallestFactor;
                    }

                    if (!fastRepeat && sliderConversionOptions.slowRepeatEndsConvert || fastRepeat && sliderConversionOptions.fastRepeatStackConvert)
                    {
                        for (int j = repeatStep; j <= hitObject.repeats; j += repeatStep)
                        {
                            bool finalEnd = j == hitObject.repeats;
                            HitObject newHitObject = new HitObject
                            (
                                fastRepeat || j % 2 == 0 ? hitObject.x : hitObject.endX,
                                fastRepeat || j % 2 == 0 ? hitObject.y : hitObject.endY,
                                hitObject.time + j * repeatTime,
                                finalEnd ? 0 : 2,
                                hitObject.endHitsound,
                                finalEnd ? 0 : hitObject.pixelLength / (hitObject.repeats),
                                0
                            );
                            newHitObject.audicaTick = (float)Math.Round(hitObject.audicaTick + j * repeatTicks);
                            newHitObject.endTime = newHitObject.time + (finalEnd ? 0f : repeatTime);
                            newHitObject.audicaEndTick = finalEnd ? newHitObject.audicaTick : (float)Math.Round(hitObject.audicaTick + (j + 1) * repeatTicks);
                            newHitObject.endX = finalEnd ? newHitObject.x : (fastRepeat || j % 2 == 1 ? hitObject.x : hitObject.endX);
                            newHitObject.endY = finalEnd ? newHitObject.y : (fastRepeat || j % 2 == 1 ? hitObject.y : hitObject.endY);
                            hitObjects.Add(newHitObject);
                        }

                        //Set head end to first repeat
                        hitObject.endTime = hitObject.time + repeatTime;
                        hitObject.audicaEndTick = (float)Math.Round(hitObject.audicaTick + repeatTicks);
                        hitObject.repeats = 1;
                    }
                }
            }
            hitObjects.Sort((ho1, ho2) => ho1.time.CompareTo(ho2.time));

            if (sliderConversionOptions.sliderEndStreamStartConvert)
            {
                hitObjectsOrgCount = hitObjects.Count;
                for (int i = 0; i < hitObjectsOrgCount - 1; i++)
                {
                    HitObject hitObject = hitObjects[i];
                    HitObject nextHitObject = hitObjects[i + 1];

                    //Add a hitObject for sliders if the end is on half-beat and the next target is within 1/12th of the slider end.
                    if ((hitObject.type == 2 || hitObject.type == 6) && OsuUtility.ticksSincePrevTimingPoint(hitObject.audicaEndTick, timingPoints) % 240f == 0f &&
                        nextHitObject.audicaTick - hitObject.audicaEndTick <= 160f && !hitObjects.Any(ho => ho.time == hitObject.endTime))
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
        }

        private static void RunStreamScalePass(List<HitObjectGroup> noteStreams)
        {
            foreach (HitObjectGroup noteStream in noteStreams)
            {
                float averageStreamDistance = noteStream.length / (noteStream.hitObjects.Count - 1);
                if (averageStreamDistance > 0f && averageStreamDistance < Config.parameters.streamOptions.streamMinAverageDistance)
                {
                    noteStream.BoundScale(Config.parameters.streamOptions.streamMinAverageDistance / averageStreamDistance);
                }
            }
        }

        private static void RunFovScalePass(List<HitObject> hitObjects)
        {
            float fovMotionFactor = Config.parameters.scalingOptions.adaptiveScalingOptions.fovMotionFactor;
            float fovRecenterTime = Config.parameters.scalingOptions.adaptiveScalingOptions.fovRecenterTime;
            float scaleDistanceStartThres = Config.parameters.scalingOptions.adaptiveScalingOptions.scaleDistanceStartThres;
            float scaleLogBase = Config.parameters.scalingOptions.adaptiveScalingOptions.scaleLogBase;

            float fovX = 256f;
            float fovY = 192;
            float prevFovUpdateTime = 0f;

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
                //Extend the group to include the rest of any started chains
                while (i + 1 < hitObjects.Count && hitObjects[i+1].audicaBehavior == 5)
                {
                    syncTranslateHitObjects.Add(hitObjects[i + 1]);
                    i++;
                }

                HitObjectGroup hitObjectGroup = new HitObjectGroup(syncTranslateHitObjects);
                hitObjectGroup.Translate(translationX, translationY);
                hitObjectGroup.BoundScale(1f);

                //Iterate over hitObjects and update FOV position, simulating FOV lag

                foreach (HitObject hitObject in hitObjectGroup.hitObjects)
                {
                    float fovMovementFraction = 1f - 1f / (fovMotionFactor * (hitObject.time - prevFovUpdateTime) / 1000f + 1f);
                    fovX += fovMovementFraction * (hitObject.x - fovX);
                    fovY += fovMovementFraction * (hitObject.y - fovY);
                    prevFovUpdateTime = hitObject.time;
                }
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
            float chainTimeThres = Config.parameters.chainConversionOptions.timeThres;

            HitObject prevChainHeadHitObject = null;

            for (int i = 0; i < hitObjects.Count; i++)
            {
                HitObject prevHitObject = i > 0 ? hitObjects[i - 1] : null;
                HitObject nextHitObject = i + 1 < hitObjects.Count ? hitObjects[i + 1] : null;
                HitObject nextNextHitObject = i + 2 < hitObjects.Count ? hitObjects[i + 2] : null;
                HitObject currentHitObject = hitObjects[i];

                bool isIgnoredChainEnd = Config.parameters.chainConversionOptions.ignoreSlidersForChainConvert && (currentHitObject.type == 2 || currentHitObject.type == 6) &&
                    (nextHitObject == null || nextHitObject.time - currentHitObject.time > chainTimeThres) || Config.parameters.chainConversionOptions.ignoreSustainsForChainConvert && currentHitObject.audicaBehavior == 3;
                bool nextIsIgnoredChainEnd = nextHitObject == null || Config.parameters.chainConversionOptions.ignoreSlidersForChainConvert && (nextHitObject.type == 2 || nextHitObject.type == 6) &&
                    (nextNextHitObject == null || nextNextHitObject.time - nextHitObject.time > chainTimeThres) || Config.parameters.chainConversionOptions.ignoreSustainsForChainConvert && nextHitObject.audicaBehavior == 3;

                if (isIgnoredChainEnd)
                    continue;

                if ((prevHitObject == null || currentHitObject.time - prevHitObject.time > chainTimeThres) && nextHitObject != null &&
                    nextHitObject.time - currentHitObject.time <= chainTimeThres && !nextIsIgnoredChainEnd)
                {
                    currentHitObject.audicaBehavior = 4;
                    prevChainHeadHitObject = currentHitObject;
                }
                else if (prevHitObject != null && currentHitObject.time - prevHitObject.time <= chainTimeThres)
                {   
                    if (currentHitObject.time - prevChainHeadHitObject.time > chainTimeThres && currentHitObject.audicaTick - prevChainHeadHitObject.audicaTick >= Config.parameters.chainConversionOptions.switchFrequency && nextHitObject != null &&
                        nextHitObject.time - currentHitObject.time <= chainTimeThres && OsuUtility.ticksSincePrevTimingPoint(currentHitObject.audicaTick, timingPoints) % Config.parameters.chainConversionOptions.switchFrequency == 0 && !nextIsIgnoredChainEnd)
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

            if (Config.parameters.chainConversionOptions.reformChains)
            {
                chainHitObjects = new List<HitObject>();
                foreach (HitObject hitObject in hitObjects)
                {
                    if (hitObject.audicaBehavior == 4)
                    {
                        if (chainHitObjects.Count != 0) CheckAndReformChain(chainHitObjects);
                        chainHitObjects = new List<HitObject>();
                        chainHitObjects.Add(hitObject);
                    }
                    else if (hitObject.audicaBehavior == 5)
                    {
                        chainHitObjects.Add(hitObject);
                    }
                }
                if (chainHitObjects.Count != 0) CheckAndReformChain(chainHitObjects);
            }

            //Shrink chains with too high average distance per 
            chainHitObjects = new List<HitObject>();
            foreach (HitObject hitObject in hitObjects)
            {
                if (hitObject.audicaBehavior == 4)
                {
                    if (chainHitObjects.Count != 0) CheckAndShrinkChain(chainHitObjects);
                    chainHitObjects = new List<HitObject>();
                    chainHitObjects.Add(hitObject);
                }
                else if (hitObject.audicaBehavior == 5)
                {
                    chainHitObjects.Add(hitObject);
                }
            }
            if (chainHitObjects.Count != 0) CheckAndShrinkChain(chainHitObjects);
        }

        private static void CheckAndPruneChain(List<HitObject> chainHitObjects, List<TimingPoint> timingPoints)
        {
            if (chainHitObjects.Count <= Config.parameters.chainConversionOptions.minChainLinks)
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
                bestGcdHitObject.ResetEndVals();
            }
        }

        private static void CheckAndReformChain(List<HitObject> chainHitObjects)
        {
            bool reformChain = false;

            //Reform chain if they contain too sharp angles
            for (int i = 1; i < chainHitObjects.Count - 1; i++)
            {
                HitObject currentHitObject = chainHitObjects[i];
                HitObject prevHitObject = chainHitObjects[i - 1];
                HitObject nextHitObject = chainHitObjects[i + 1];

                float inVecX = currentHitObject.x - prevHitObject.x;
                float inVecY = currentHitObject.y - prevHitObject.y;
                float outVecX = nextHitObject.x - currentHitObject.x;
                float outVecY = nextHitObject.y - currentHitObject.y;

                //Calculate angle from dot product
                float angle = (float)Math.Acos((inVecX * outVecX + inVecY * outVecY) / (Math.Sqrt(inVecX * inVecX + inVecY * inVecY) * Math.Sqrt(outVecX * outVecX + outVecY * outVecY)));
                angle *= 180f / (float)Math.PI;

                if (angle > Config.parameters.chainConversionOptions.sharpChainAngle)
                {
                    reformChain = true;
                    break;
                }
            }

            //Reform chains if the chain link movement speed vary too much
            float minSpeed = float.PositiveInfinity;
            float maxSpeed = 0f;
            for (int i = 1; i < chainHitObjects.Count; i++)
            {
                HitObject currentHitObject = chainHitObjects[i];
                HitObject prevHitObject = chainHitObjects[i - 1];
                float speed = OsuUtility.EuclideanDistance(currentHitObject.x, currentHitObject.y, prevHitObject.x, prevHitObject.y) / (currentHitObject.time - prevHitObject.time);
                minSpeed = Math.Min(minSpeed, speed);
                maxSpeed = Math.Max(maxSpeed, speed);
            }
            if (maxSpeed / minSpeed > Config.parameters.chainConversionOptions.maxSpeedRatio) reformChain = true;

            //Reform chains if the last link is too close to the chain head
            HitObject chainStart = chainHitObjects[0];
            HitObject chainEnd = chainHitObjects[chainHitObjects.Count - 1];
            if (OsuUtility.EuclideanDistance(chainStart.x, chainStart.y, chainEnd.x, chainEnd.y) < Config.parameters.chainConversionOptions.endMinDistanceFromHead) reformChain = true;

            if (reformChain)
            {
                //Reform the chain as a time proportionally spaced chain from first to last link.
                HitObject chainHead = chainHitObjects[0];
                HitObject lastLink = chainHitObjects[chainHitObjects.Count - 1];

                float posDiffX = lastLink.x - chainHead.x;
                float posDiffY = lastLink.y - chainHead.y;
                float timeDiff = lastLink.time - chainHead.time;

                for (int i = 1; i < chainHitObjects.Count - 1; i++)
                {
                    HitObject chainLink = chainHitObjects[i];

                    float chainTimeProgress = (chainLink.time - chainHead.time) / timeDiff;
                    chainLink.x = chainHead.x + posDiffX * chainTimeProgress;
                    chainLink.y = chainHead.y + posDiffY * chainTimeProgress;
                }
            }
        }

        private static void CheckAndShrinkChain(List<HitObject> chainHitObjects)
        {
            HitObjectGroup chainHitObjectGroup = new HitObjectGroup(chainHitObjects);

            float avgLinkDistance = chainHitObjectGroup.length / (chainHitObjects.Count - 1);
            if (avgLinkDistance > Config.parameters.chainConversionOptions.maxAvgLinkDistance)
            {
                chainHitObjectGroup.Scale(Config.parameters.chainConversionOptions.maxAvgLinkDistance / avgLinkDistance);
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
                if (nextHitObject != null && nextHitObject.audicaTick - currentHitObject.audicaEndTick <= Config.parameters.sustainConversionOptions.sustainExtension)
                {
                    if (OsuUtility.ticksSincePrevTimingPoint(nextHitObject.audicaTick, timingPoints) % 480f == 0f)
                    {
                        currentHitObject.endTime = nextHitObject.time;
                        currentHitObject.audicaEndTick = nextHitObject.audicaTick;
                    }
                }

                //Shorten duration if there are two targets within too short time after the sustain
                if (nextNextHitObject != null && nextNextHitObject.time - currentHitObject.endTime < Config.parameters.handAssignmentAlgorithmParameters.holdRestStrain.time)
                {
                    currentHitObject.audicaEndTick -= 240f;
                    currentHitObject.endTime = OsuUtility.TickToMs(currentHitObject.audicaEndTick, timingPoints);
                }
            }


            //Extend minSustainLength 1 beat at a time until less than maxSustainFraction is met
            float minSustainLength = Config.parameters.sustainConversionOptions.minSustainLength - 480f;
            float sustainFraction;
            do
            {
                minSustainLength += 480f;
                sustainFraction = (float)hitObjects.Count(ho => ho.audicaEndTick - ho.audicaTick >= minSustainLength) / hitObjects.Count();
            }
            while (sustainFraction > Config.parameters.sustainConversionOptions.maxSustainFraction);

            foreach (HitObject hitObject in hitObjects)
            {
                if (hitObject.audicaEndTick - hitObject.audicaTick >= minSustainLength)
                {
                    hitObject.audicaBehavior = 3;
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

        private static void RemoveUnusedHitObjects(List<HitObject> hitObjects, List<HitObjectGroup> noteStreams)
        {
            for (int i = hitObjects.Count-1; i >= 0; i--)
            {
                if (hitObjects[i].audicaBehavior < 0) hitObjects.RemoveAt(i);
            }

            foreach (HitObjectGroup noteStream in noteStreams)
            {
                noteStream.FormGroup(noteStream.hitObjects.Where(ho => ho.audicaBehavior >= 0).ToList());
            }
        }

        private class TargetStack
        {
            public Cue stackStartCue;
            public List<Cue> tailCues;
            public float stackMovementSpeed;
            public float lastStackTime;
            public OsuUtility.Coordinate2D lastPos;
            public OsuUtility.Coordinate2D direction;

            public TargetStack()
            {
                tailCues = new List<Cue>();
            }
        }

        private static void RunStackDistributionPass(List<HitObject> hitObjects, List<Cue> cues)
        {
            float stackInclusionRange = Config.parameters.stackDistributionOptions.inclusionRange;
            float stackItemDistance = Config.parameters.stackDistributionOptions.itemDistance; //Offset for stack items. Time proportionate distancing is used through the stack based on getting this distance between first and second item in stack
            float stackMaxDistance = Config.parameters.stackDistributionOptions.maxDistance;
            float stackResetTime = Config.parameters.stackDistributionOptions.resetTime;

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


                //Ignore moving targets in chains and in streams other than stream head and tail
                if ((currentHitObject.audicaBehavior == 5 || isInStream && !isStreamStart && !isStreamEnd) && prevPosDifferent && nextPosDifferent) continue;

                //Remove unactive stacks
                for (int j = activeStacks.Count - 1; j >= 0; j--)
                {
                    if (currentHitObject.time - activeStacks[j].lastStackTime >= stackResetTime)
                        activeStacks.RemoveAt(j);
                }

                OsuUtility.Coordinate2D currentCuePos = OsuUtility.GetPosFromCue(currentCue);

                //Check if target fits in a currently active stack, if not create a stack for this target
                TargetStack stack = activeStacks.Find(s => OsuUtility.EuclideanDistance(currentCuePos.x, currentCuePos.y, s.lastPos.x, s.lastPos.y) <= stackInclusionRange);
                if (stack == null)
                {
                    TargetStack newStack = new TargetStack();
                    newStack.stackStartCue = currentCue;
                    newStack.lastStackTime = currentHitObject.time;
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
                stack.lastStackTime = currentHitObject.time;
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
                    if (cue.behavior != Cue.Behavior.ChainStart && cue.behavior != Cue.Behavior.Chain)
                    {
                        handSepX = (cue.handType == Cue.HandType.Right ? handSepDirectionX : -handSepDirectionX) * Config.parameters.stackDistributionOptions.handSeparation / 2;
                        handSepY = (cue.handType == Cue.HandType.Right ? handSepDirectionY : -handSepDirectionY) * Config.parameters.stackDistributionOptions.handSeparation / 2;
                    }

                    OsuUtility.Coordinate2D newPos = new OsuUtility.Coordinate2D(stackHeadPos.x + distributionX + handSepX,
                        stackHeadPos.y + distributionY + handSepY);
                    OsuUtility.SetCuePos(cue, newPos);

                    //If the cue is a chain head, also translate subsequent chain links
                    if (cue.behavior == Cue.Behavior.ChainStart)
                    {
                        int cueIdx = cues.IndexOf(cue);
                        for (int j = 1; j < cues.Count - cueIdx; j++)
                        {
                            Cue otherCue = cues[cueIdx + j];
                            if (otherCue.handType != cue.handType) continue;

                            if (otherCue.behavior != Cue.Behavior.Chain) break;

                            OsuUtility.Coordinate2D otherCuePos = OsuUtility.GetPosFromCue(otherCue);
                            OsuUtility.Coordinate2D newOtherCuePos = new OsuUtility.Coordinate2D(otherCuePos.x + distributionX + handSepX,
                                otherCuePos.y + distributionY + handSepY);
                            OsuUtility.SetCuePos(otherCue, newOtherCuePos);
                        }
                    }
                }
            }
        }

        private static void RunChainEnlargePass(List<Cue> cues)
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
            float minChainSize = Config.parameters.chainConversionOptions.minSize;
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
            DifficultyMeleeOptions meleeOptions = null;
            switch (difficultyName.ToLower())
            {
                case ("expert"):
                    meleeOptions = Config.parameters.meleeOptions.expertMeleeOptions;
                    break;
                case ("advanced"):
                    meleeOptions = Config.parameters.meleeOptions.advancedMeleeOptions;
                    break;
                case ("standard"):
                    meleeOptions = Config.parameters.meleeOptions.standardMeleeOptions;
                    break;
                case ("beginner"):
                    meleeOptions = Config.parameters.meleeOptions.beginnerMeleeOptions;
                    break;
            }
            float fovRecenterTime = Config.parameters.scalingOptions.adaptiveScalingOptions.fovRecenterTime;

            if (!meleeOptions.convertMelees) return;

            bool prevMeleeRight = false;
            float prevMeleeTick = float.NegativeInfinity;

            var cueTimes = new List<(Cue cue, float time, float endTime)>();
            foreach (Cue cue in cues)
            {
                float time = OsuUtility.TickToMs(cue.tick, timingPoints);
                float endTime = cue.behavior == Cue.Behavior.Hold ? OsuUtility.TickToMs(cue.tick + cue.tickLength, timingPoints) : time;
                cueTimes.Add((cue, time, endTime));
            }

            for (int i = 0; i < cues.Count; i++)
            {
                Cue currentCue = cueTimes[i].cue;
                float currentCueMsTime = cueTimes[i].time;

                //Previous cue, excluding cues that would be removed from the no-target window if no-target window target removal is on
                Cue prevCue = null;
                float prevCueMsTime = 0f;
                float prevCueMsEndTime = 0f;
                for (int j = 1; j <= i && prevCue == null; j++)
                {
                    var cueTime = cueTimes[i - j];
                    if (!meleeOptions.removeNoTargetWindowTargets || cueTime.time < currentCueMsTime - meleeOptions.preNoTargetTime)
                    {
                        prevCue = cueTime.cue;
                        prevCueMsTime = cueTime.time;
                        prevCueMsEndTime = cueTime.endTime;
                    }
                }

                //Next cue, excluding the cues that would be removed from the no-target window if no-target window target removal is on
                Cue nextCue = null;
                float nextCueMsTime = 0f;
                for (int j = 1; i + j < cueTimes.Count && nextCue == null; j++)
                {
                    var cueTime = cueTimes[i + j];
                    if (!meleeOptions.removeNoTargetWindowTargets || cueTime.time > currentCueMsTime + meleeOptions.postNoTargetTime)
                    {
                        nextCue = cueTime.cue;
                        nextCueMsTime = cueTime.time;
                    }
                }


                TimingPoint prevNormalTimingPoint = OsuUtility.getPrevTimingPoint(currentCue.tick, timingPoints);
                TimingPoint prevEitherTimingPoints = OsuUtility.getPrevTimingPoint(currentCue.tick, mergedTimingPoints);

                float timeSinceTimingPoint = currentCue.tick - prevNormalTimingPoint.audicaTick;
                float frequency = prevEitherTimingPoints.kiai ? meleeOptions.kiaiAttemptFrequency : meleeOptions.normalAttemptFrequency;
                if (frequency == 0) continue;
                bool onMeleeConvertTime = timeSinceTimingPoint > 0 && timeSinceTimingPoint % (480f * prevNormalTimingPoint.meter / frequency) == 0;

                float cooldown = prevEitherTimingPoints.kiai ? meleeOptions.kiaiCooldown : meleeOptions.normalCooldown;
                bool offCooldown = (currentCue.tick - prevMeleeTick) / (480f * prevNormalTimingPoint.meter) >= cooldown;

                if (onMeleeConvertTime && offCooldown && currentCue.behavior != Cue.Behavior.Hold && currentCue.behavior != Cue.Behavior.ChainStart && currentCue.behavior != Cue.Behavior.Chain)
                {
                    //Check melee conversion conditions for each target
                    bool rightMeleeOk = true;
                    bool leftMeleeOk = true;

                    //Don't convert to melee if next or previous targets are within the no-target window.
                    if (prevCue != null && prevCueMsTime >= currentCueMsTime - meleeOptions.preNoTargetTime || nextCue != null && nextCueMsTime <= currentCueMsTime + meleeOptions.postNoTargetTime) rightMeleeOk = leftMeleeOk = false;

                    foreach (var otherCueTime in cueTimes.Where(oct => oct.cue.behavior != Cue.Behavior.Melee && oct.time >= currentCueMsTime - meleeOptions.preRestTime && oct.time <= currentCueMsTime + meleeOptions.postRestTime &&
                    (!meleeOptions.removeNoTargetWindowTargets || oct.time < currentCueMsTime - meleeOptions.preNoTargetTime || oct.time > currentCueMsTime + meleeOptions.postNoTargetTime)))
                    {
                        Cue otherCue = otherCueTime.cue;
                        if (otherCue == currentCue) continue;

                        //Don't convert to melee if other targets for the same hand are within rest window
                        if (otherCue.handType == Cue.HandType.Right) rightMeleeOk = false;
                        if (otherCue.handType == Cue.HandType.Left) leftMeleeOk = false;

                        //Don't convert to melee if any targets within the rest window are outside the corresponding melee target position window
                        OsuUtility.Coordinate2D otherCuePos = OsuUtility.GetPosFromCue(otherCue);
                        if (otherCuePos.x > 7.5f - meleeOptions.positionWindowMinDistance || otherCuePos.x < 7.5f - meleeOptions.positionWindowMaxDistance) rightMeleeOk = false;
                        if (otherCuePos.x < 3.5f + meleeOptions.positionWindowMinDistance || otherCuePos.x > 3.5f + meleeOptions.positionWindowMaxDistance) leftMeleeOk = false;
                    }

                    foreach (var otherCueTime in cueTimes.Where(oct => oct.cue.behavior != Cue.Behavior.Melee && oct.time >= currentCueMsTime - meleeOptions.prePositionTime && oct.time <= currentCueMsTime &&
                    (!meleeOptions.removeNoTargetWindowTargets || oct.time < currentCueMsTime - meleeOptions.preNoTargetTime)))
                    {
                        if (otherCueTime.cue == currentCue) continue;
                        //Require all targets between the position time window start and melee to be on the inside of the edge of the position window opposite of the melee.
                        OsuUtility.Coordinate2D otherCuePos = OsuUtility.GetPosFromCue(otherCueTime.cue);
                        if (otherCuePos.x < 7.5f - meleeOptions.positionWindowMaxDistance) rightMeleeOk = false;
                        if (otherCuePos.x > 3.5f + meleeOptions.positionWindowMaxDistance) leftMeleeOk = false;

                        //Don't allow targets for the melee hand within the prePositionTime window to be on the same height and same half of the playfield as the melee. Prevents some occlusion and hand collisions
                        if (otherCuePos.y > 3 && otherCuePos.y < 5)
                        {
                            if (otherCueTime.cue.handType == Cue.HandType.Right && otherCuePos.x > 5.5f) rightMeleeOk = false;
                            else if (otherCueTime.cue.handType == Cue.HandType.Left && otherCuePos.x < 5.5f) leftMeleeOk = false;
                        }
                    }

                    //Require previous target to either be within the melee target position window or sufficiently long ago that fov has recentered
                    if (prevCue!= null && prevCue.behavior != Cue.Behavior.Melee && currentCueMsTime - prevCueMsEndTime < fovRecenterTime)
                    {
                        OsuUtility.Coordinate2D prevHitObjectPos = OsuUtility.GetPosFromCue(prevCue);
                        if (prevHitObjectPos.x > 7.5f - meleeOptions.positionWindowMinDistance || prevHitObjectPos.x < 7.5f - meleeOptions.positionWindowMaxDistance) rightMeleeOk = false;
                        if (prevHitObjectPos.x < 3.5f + meleeOptions.positionWindowMinDistance || prevHitObjectPos.x > 3.5f + meleeOptions.positionWindowMaxDistance) leftMeleeOk = false;
                    }

                    //Require next target to be on the inside of the side of the position window far away from the melee, or be sufficiently long to that the fov will have recentered
                    if (nextCue != null && nextCueMsTime - currentCueMsTime < fovRecenterTime)
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
                        currentCue.zOffset = 0f;

                        prevMeleeRight = rightMeleeOk;
                        prevMeleeTick = currentCue.tick;
                    }
                }
            }

            //Remove any cues within the no-target window of a melee
            if (meleeOptions.removeNoTargetWindowTargets)
            {
                for (int i = cueTimes.Count - 1; i >= 0; i--)
                {
                    bool inMeleeNoTargetWindow = false;
                    var cueTime = cueTimes[i];

                    //Check for melees before current cue
                    for (int j = 1; j <= i; j++)
                    {
                        var otherCueTime = cueTimes[i - j];
                        if (otherCueTime.time < cueTime.time - meleeOptions.preNoTargetTime) break;
                        if (otherCueTime.cue.behavior == Cue.Behavior.Melee) inMeleeNoTargetWindow = true;
                    }

                    //Check for melees after current cue
                    for (int j = 1; i + j < cueTimes.Count; j++)
                    {
                        var otherCueTime = cueTimes[i + j];
                        if (otherCueTime.time > cueTime.time + meleeOptions.postNoTargetTime) break;
                        if (otherCueTime.cue.behavior == Cue.Behavior.Melee) inMeleeNoTargetWindow = true;
                    }

                    //Remove current target if in melee no target window
                    if (inMeleeNoTargetWindow)
                    {
                        cueTimes.RemoveAt(i);
                        cues.RemoveAt(i);
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

        public static Difficulty ScaleDifficulty(Difficulty unscaledDifficulty, float scaleX, float scaleY, float zOffset)
        {
            Difficulty scaledDifficulty = OsuUtility.DeepClone(unscaledDifficulty);
            foreach (Cue cue in scaledDifficulty.cues)
            {
                if (cue.behavior == Cue.Behavior.Melee) continue;
                OsuUtility.Coordinate2D cuePos = OsuUtility.GetPosFromCue(cue);
                cuePos.x = (cuePos.x - 5.5f) * scaleX + 5.5f;
                cuePos.y = (cuePos.y - 3f) * scaleY + 3f;
                OsuUtility.SetCuePos(cue, cuePos);
                if (cue.behavior != Cue.Behavior.Melee) cue.zOffset += zOffset;
            }

            return scaledDifficulty;
        }
    }

    public class OSZ
    {
        public string oszFilePath;
        
        public List<Osufile> osufiles = new List<Osufile>();
        public OSZ(string filePath)
        {
            oszFilePath = filePath;
            ZipArchive zip = ZipFile.OpenRead(filePath);
            foreach (var entry in zip.Entries)
            {
                if (entry.Name.Contains(".osu"))
                {
                    osufiles.Add(new Osufile(entry.Open()));   
                }
            }
        }
    }
}


