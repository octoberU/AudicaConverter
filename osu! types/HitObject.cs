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
        public int audicaBehavior;

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
        }
    }
}