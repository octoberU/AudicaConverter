using osutoaudica.osu__types;
using OsuTypes;
using AudicaTools;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Linq;
using osutoaudica;

namespace OsuTypes
{
    public class Osufile
    {
        public General general = new General();
        public Metadata metadata = new Metadata();
        public OsuDifficulty difficulty = new OsuDifficulty();
        public List<TimingPoint> timingPoints = new List<TimingPoint>();
        public List<TimingPoint> inheritedTimingPoints = new List<TimingPoint>();
        public List<TimingPoint> mergedTimingPoints = null;
        public List<HitObject> hitObjects = new List<HitObject>();
        public List<HitObjectGroup> noteStreams = new List<HitObjectGroup>();
        public Difficulty audicaDifficulty;
        public float audicaDifficultyRating;

        public Osufile(Stream stream)
        {
            var ms = new MemoryStream();
            stream.CopyTo(ms);
            string[] osuString = Encoding.UTF8.GetString(ms.ToArray()).Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            ParseMode mode = ParseMode.None;
            foreach (var line in osuString)
            {
                if (mode != ParseMode.None)
                {
                    switch (mode)
                    {
                        case ParseMode.General:
                            ParseGeneral(line);
                            break;
                        case ParseMode.Metadata:
                            ParseMetadata(line);
                            break;
                        case ParseMode.Difficulty:
                            ParseDifficulty(line);
                            break;
                        case ParseMode.TimingPoints:
                            ParseTimingPoint(line);
                            break;
                        case ParseMode.HitObjects:
                            ParseHitObject(line);
                            break;

                        default:
                            break;
                    }
                }

                if (line.Contains("[General]")) mode = ParseMode.General;
                else if (line.Contains("[Metadata]")) mode = ParseMode.Metadata;
                else if (line.Contains("[Difficulty]")) mode = ParseMode.Difficulty;
                else if (line.Contains("[TimingPoints]")) mode = ParseMode.TimingPoints;
                else if (line.Contains("[HitObjects]")) mode = ParseMode.HitObjects;
                else if (line.Contains("[Colours]")) mode = ParseMode.None;
                else if (line.Contains("[Events]")) mode = ParseMode.None;
                else if (line.Contains("[Editor]")) mode = ParseMode.None;
                else if (line.Length < 0) mode = ParseMode.None;
            }

            TimingPoint initialTimingPoint = new TimingPoint(0f, timingPoints[0].beatTime, timingPoints[0].vol, false, false);
            initialTimingPoint.meter = timingPoints[0].meter;
            timingPoints.Insert(0, initialTimingPoint);

            FixFirstTimingPoint(timingPoints);
            MergeTimingPoints();
            CalculateSliderEndTimes();
            CalculateAudicaTicks();
            DetectStreams();
        }

        private void MergeTimingPoints()
        {
            mergedTimingPoints = timingPoints.Concat(inheritedTimingPoints).OrderBy(tp => tp.ms).ThenBy(tp => tp.inherited).ToList();
            TimingPoint prevUninheritedTimingPoint = null;
            for (int i = 0; i < mergedTimingPoints.Count; i++)
            {
                TimingPoint timingPoint = mergedTimingPoints[i];

                if (!timingPoint.inherited)
                {
                    prevUninheritedTimingPoint = timingPoint;
                }
                else if (timingPoint.ms == prevUninheritedTimingPoint.ms)
                {
                    prevUninheritedTimingPoint.sliderVelocity = timingPoint.sliderVelocity;
                    prevUninheritedTimingPoint.vol = timingPoint.vol;
                    prevUninheritedTimingPoint.kiai = prevUninheritedTimingPoint.kiai || timingPoint.kiai; //No clue if this is necessary...
                    mergedTimingPoints.RemoveAt(i--);
                }
                else
                {
                    timingPoint.beatTime = prevUninheritedTimingPoint.beatTime;
                }
            }
        }

        private void CalculateSliderEndTimes()
        {
            foreach(HitObject hitObject in hitObjects)
            {
                if (hitObject.type == 2 || hitObject.type == 6)
                {
                    hitObject.endTime = hitObject.time + OsuUtility.CalculateSliderDuration(hitObject, difficulty.sliderMultiplier, mergedTimingPoints);
                }
            }
        }

        private void CalculateAudicaTicks()
        {
            foreach (TimingPoint timingPoint in mergedTimingPoints)
            {
                timingPoint.audicaTick = OsuUtility.MsToTick(timingPoint.ms, timingPoints, roundingPrecision: 1);
            }

            foreach (HitObject hitObject in hitObjects)
            {
                hitObject.audicaTick = OsuUtility.MsToTick(hitObject.time, timingPoints, roundingPrecision: 10);
                hitObject.audicaEndTick = OsuUtility.MsToTick(hitObject.endTime, timingPoints, roundingPrecision: 10);
            }
        }

        private void DetectStreams()
        {
            float streamTimeThres = Config.parameters.streamOptions.streamTimeThres;
            float streamDistanceThres = Config.parameters.streamOptions.streamDistanceThres;

            List<HitObject> streamHitObjects = new List<HitObject>();
            if (hitObjects.Count > 0) streamHitObjects.Add(hitObjects[0]);

            for (int i = 1; i < hitObjects.Count; i++)
            {
                HitObject currentHitObject = hitObjects[i];
                HitObject prevHitObject = hitObjects[i - 1];

                if (currentHitObject.time - prevHitObject.time <= streamTimeThres &&
                    OsuUtility.EuclideanDistance(prevHitObject.x, prevHitObject.y, currentHitObject.x, currentHitObject.y) <= streamDistanceThres)
                {
                    streamHitObjects.Add(currentHitObject);
                }
                else
                {
                    ConsiderStream(streamHitObjects);

                    streamHitObjects = new List<HitObject>();
                    streamHitObjects.Add(currentHitObject);
                }
            }
            ConsiderStream(streamHitObjects);
        }

        private void ConsiderStream(List<HitObject> streamHitObjects)
        {
            float streamMinNoteCount = Config.parameters.streamOptions.streamMinNoteCount;
            if (streamHitObjects.Count < streamMinNoteCount) return;

            HitObjectGroup noteStream = new HitObjectGroup(streamHitObjects);
            foreach (HitObject hitObject in streamHitObjects)
            {
                hitObject.noteStream = noteStream;
            }
            this.noteStreams.Add(noteStream);
        }

        private void ParseDifficulty(string line)
        {
            if (line.Contains("SliderMultiplier:")) difficulty.sliderMultiplier = float.Parse(line.Split(":")[1]);
            if (line.Contains("ApproachRate:")) difficulty.approachRate = float.Parse(line.Split(":")[1]);
        }

        private void FixFirstTimingPoint(List<TimingPoint> timingPoints)
        {
            while (timingPoints[1].ms < 0) timingPoints[1].ms += (float)timingPoints[1].beatTime * 4;
            
            timingPoints[1].ms = timingPoints[1].ms % (float)(timingPoints[1].beatTime * 4);
            
        }

        private void ParseHitObject(string line)
        {
            var split = line.Split(",");
            if (split.Length < 2) return;
            int type = int.Parse(split[3]) % 16;

            if(type == 1 || type == 5)
            {
                hitObjects.Add(new HitObject(float.Parse(split[0]), float.Parse(split[1]), float.Parse(split[2]), int.Parse(split[3]), int.Parse(split[4]), 0f, 0));
            }
            else if(type == 2 || type == 6)
            {
                int hitsound;
                int endHitsound;
                if (split.Length >= 9)
                {
                    var hitsounds = split[8].Split("|");
                    hitsound = int.Parse(hitsounds[0]);
                    endHitsound = int.Parse(hitsounds[1]);
                }
                else
                {
                    hitsound = endHitsound = int.Parse(split[4]);
                }
                var sliderPoints = split[5].Split("|");
                var endCoords = sliderPoints[sliderPoints.Length - 1].Split(":");
                HitObject hitObject = new HitObject(float.Parse(split[0]), float.Parse(split[1]), float.Parse(split[2]), int.Parse(split[3]), hitsound, float.Parse(split[7]), int.Parse(split[6]));
                hitObject.endHitsound = endHitsound;
                hitObject.endX = int.Parse(endCoords[0]);
                hitObject.endY = int.Parse(endCoords[1]);

                hitObjects.Add(hitObject);
                
            }
        }

        private void ParseMetadata(string line)
        {
            if (line.Contains("Title:")) metadata.title = line.Split(":")[1];
            else if (line.Contains("Artist:")) metadata.artist = line.Split(":")[1];
            else if (line.Contains("Creator:")) metadata.creator = line.Split(":")[1];
            else if (line.Contains("Version:")) metadata.version = line.Split(":")[1];
        }

        private void ParseTimingPoint(string line)
        {
            var split = line.Split(",");
            if (split.Length >= 2)
            {
                int effects = split.Length > 7 ? int.Parse(split[7]) : 0;
                bool kiai = effects == 1 || effects == 5;
                var timingPoint = new TimingPoint((int)float.Parse(split[0]), float.Parse(split[1]), split.Length > 5 ? int.Parse(split[5]) : 100, kiai, split.Length > 6 ? !Convert.ToBoolean(int.Parse(split[6])): false);
                if (!timingPoint.inherited)
                {
                    timingPoint.meter = split.Length > 2 ? int.Parse(split[2]) : 4;
                    timingPoints.Add(timingPoint);
                }
                else inheritedTimingPoints.Add(timingPoint);
            }
            
        }

        private void ParseGeneral(string line)
        {
            if (line.Contains("AudioFilename:")) general.audioFileName = line.Split(":")[1].Trim();
            else if (line.Contains("PreviewTime:")) int.TryParse(line.Split(":")[1].Trim(), out general.previewTime);
            else if (line.Contains("StackLeniency:")) float.TryParse(line.Split(":")[1].Trim(), out general.stackLeniency);
            else if (line.Contains("Mode:")) int.TryParse(line.Split(":")[1].Trim(), out general.mode);
        }

        enum ParseMode
        {
            None,
            General,
            Editor,
            Metadata,
            Difficulty,
            Events,
            TimingPoints,
            Colours,
            HitObjects
        }
    }
}
