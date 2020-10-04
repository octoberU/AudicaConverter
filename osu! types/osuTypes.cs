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

        public TimingPoint(float ms, double beatTime, bool inherited)
        {
            this.beatTime = beatTime;
            this.ms = ms;
            this.inherited = inherited;
        }
    }
}
