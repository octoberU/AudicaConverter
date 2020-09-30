using System;
using System.Collections.Generic;
using System.Text;

namespace OsuTypes
{
    class OsuTypes
    {

    }

    public struct TimingPoint
    {
        public double beatTime;
        public float ms;

        public TimingPoint(double beatTime, float ms)
        {
            this.beatTime = beatTime;
            this.ms = ms;
        }
    }
}
