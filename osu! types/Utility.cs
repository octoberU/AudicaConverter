using AudicaTools;
using System;
using System.Collections.Generic;
using System.Text;

namespace OsuTypes
{
    internal static class OsuUtility
    {
        public static float MsToTick(float ms, List<TimingPoint> timingPoints)
        {
            //Timing points is assumed to be a list of timing points sorted in chronological order.
            float tickTime = 0f;
            int i = 0;

            while (i + 1 < timingPoints.Count && ms > timingPoints[i + 1].ms)
            {
                tickTime += (timingPoints[i + 1].ms - timingPoints[i].ms) * 480f / (float)timingPoints[i].beatTime;
                i++;
            }
            tickTime += (ms - timingPoints[i].ms) * 480f / (float)timingPoints[i].beatTime;
            return tickTime;
        }

        public static int GetTickLengthForObject(HitObject hitObject, List<TimingPoint> timingPoints)
        {
            if (hitObject.type == 1 || hitObject.type == 5) return 20;
            else return 480; // This is a placeholder for later, calculate slider pixel length here
        }

        public static int GetVelocityForObject(HitObject hitObject)
        {
            return 20;
        }

        public struct AudicaDataPos
        {
            public Cue.GridOffset offset;
            public int pitch;

            public AudicaDataPos(float offsetX, float offsetY, int pitch)
            {
                this.offset.x = offsetX;
                this.offset.y = offsetY;
                this.pitch = pitch;
            }
        }

        public static AudicaDataPos GetAudicaPosFromHitObject(HitObject hitObject)
        {
            float tempPosx = ((hitObject.x - 256f) / 512f) * 12f;
            float tempPosy = ((hitObject.y - 192f) / 512) * 7f;

            var x = Math.Clamp((int)(tempPosx), 0, 11);
            var y = Math.Clamp((int)(tempPosy), 0, 6);
            int pitch = x + 12 * y;

            float offsetX = (tempPosx - x);
            float offsetY = (tempPosy - y);

            return new AudicaDataPos(offsetX, offsetY, pitch);
        }

        private static float GetZOffsetForX(float x)
        {
            if (x < 0f) x *= -1f;

            if (x < 5.5f) return 0f;

            var zOffset = Math.Clamp(Math.Abs(x) - 5.5f, 0f, 2.5f) / 5f;

            return zOffset;
        }
    }
}
