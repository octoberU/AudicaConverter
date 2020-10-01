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

        public TimingPoint(float ms, double beatTime)
        {
            this.beatTime = beatTime;
            this.ms = ms;
        }
    }
}
