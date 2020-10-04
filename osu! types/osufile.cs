using osutoaudica.osu__types;
using OsuTypes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Linq;

namespace OsuTypes
{
    public class osufile
    {
        public General general = new General();
        public Metadata metadata = new Metadata();
        public OsuDifficulty difficulty = new OsuDifficulty();
        public List<TimingPoint> timingPoints = new List<TimingPoint>();
        public List<TimingPoint> inheritedTimingPoints = new List<TimingPoint>();
        public List<TimingPoint> mergedTimingPoints = null;
        public List<HitObject> hitObjects = new List<HitObject>();

        public osufile(Stream stream)
        {
            timingPoints.Add(new TimingPoint(0f, 480d, false));
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

            FixFirstTimingPoint(timingPoints);
            MergeTimingPoints();
            CalculateSliderEndTimes();
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

        private void ParseDifficulty(string line)
        {
            if (line.Contains("SliderMultiplier:")) difficulty.sliderMultiplier = float.Parse(line.Split(":")[1]);
        }

        private void FixFirstTimingPoint(List<TimingPoint> timingPoints)
        {
            while (timingPoints[1].ms < 0) timingPoints[1].ms += (float)timingPoints[1].beatTime;
            
            timingPoints[1].ms = timingPoints[1].ms % (float)(timingPoints[1].beatTime * 4);
            
        }

        private void ParseHitObject(string line)
        {
            var split = line.Split(",");
            if (split.Length < 2) return;
            int type = int.Parse(split[3]);
            if(type == 1 || type == 5)
            {
                this.hitObjects.Add(new HitObject(float.Parse(split[0]), float.Parse(split[1]), float.Parse(split[2]), int.Parse(split[3]), int.Parse(split[4]), 0f, 0));
            }
            else if(type == 2 || type == 6)
            {
                this.hitObjects.Add(new HitObject(float.Parse(split[0]), float.Parse(split[1]), float.Parse(split[2]), int.Parse(split[3]), int.Parse(split[4]), float.Parse(split[7]), int.Parse(split[6])));
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
            if (split.Length > 2)
            {
                var timingPoint = new TimingPoint(int.Parse(split[0]), float.Parse(split[1]), !Convert.ToBoolean(int.Parse(split[6])));
                if (!timingPoint.inherited) timingPoints.Add(timingPoint);
                else inheritedTimingPoints.Add(timingPoint);
            }
            
        }

        private void ParseGeneral(string line)
        {
            if (line.Contains("AudioFilename:")) general.audioFileName = line.Split(": ")[1];
            else if (line.Contains("PreviewTime:")) int.TryParse(line.Split(": ")[1], out general.previewTime);
            else if (line.Contains("StackLeniency:")) float.TryParse(line.Split(": ")[1], out general.stackLeniency);
            else if (line.Contains("Mode:")) int.TryParse(line.Split(": ")[1], out general.mode);
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
