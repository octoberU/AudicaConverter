using System;
using System.Collections.Generic;
using System.Text;

namespace OsuTypes
{
    public class TimingPoint
    {
        public float ms;
        public double beatTime;
        public int vol;
        public bool inherited;
        public float sliderVelocity;
        public int meter;
        public bool kiai;

        public float audicaTick;

        public TimingPoint(float ms, double beatTime, int vol, bool kiai, bool inherited)
        {
            this.ms = ms;
            this.beatTime = beatTime;
            this.vol = vol;
            this.kiai = kiai;
            this.inherited = inherited;

            if (inherited) sliderVelocity = 1f / (-(float)beatTime / 100f);
            else sliderVelocity = 1f;
        }
    }
}
