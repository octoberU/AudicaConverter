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
        public int pixelLength;
        public int repeats;

        public HitObject(float x, float y, float time, int type, int hitsound, int pixelLength, int repeats)
        {
            this.x = x;
            this.y = y;
            this.time = time;
            this.type = type;
            this.hitsound = hitsound;
            this.pixelLength = pixelLength;
            this.repeats = repeats;
        }
    }
}