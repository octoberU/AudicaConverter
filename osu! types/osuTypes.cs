using System;
using System.Collections.Generic;
using System.Text;

namespace OsuTypes
{
    class OsuTypes
    {

    }

    public class TimingPoint
    {
        public double beatTime;
        public float ms;
        public bool inherited;
        public float sliderVelocity;
        public int meter;
        public bool kiai;

        public float audicaTick;

        public TimingPoint(float ms, double beatTime, bool kiai, bool inherited)
        {
            this.beatTime = beatTime;
            this.ms = ms;
            this.kiai = kiai;
            this.inherited = inherited;

            if (inherited) sliderVelocity = 1f / (-(float)beatTime / 100f);
            else sliderVelocity = 1f;
        }
    }
}
