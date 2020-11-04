using AudicaTools;
using System.Security.Cryptography.X509Certificates;

namespace OsuTypes
{
    public class HitObject
    {
        public float x;
        public float y;
        public float time;
        public int type;
        public int hitsound;
        public float pixelLength;
        public int repeats;
        public float endTime;
        public float endX;
        public float endY;
        public int endHitsound;
        public HitObjectGroup noteStream;
        public HitObjectGroup chain;

        public float audicaTick;
        public float audicaEndTick;
        public int audicaBehavior;
        public int audicaHandType;
        public Cue audicaCue;

        public HitObject(float x, float y, float time, int type, int hitsound, float pixelLength, int repeats)
        {
            this.x = x;
            this.y = y;
            this.time = time;
            this.type = type % 16;
            this.hitsound = hitsound;
            this.pixelLength = pixelLength;
            this.repeats = repeats;

            endTime = time;
            endX = x;
            endY = y;
            audicaBehavior = 0;
        }

        public void ResetEndVals()
        {
            this.endTime = this.time;
            this.audicaEndTick = this.audicaTick;
            this.endX = this.x;
            this.endY = this.y;
        }
    }
}