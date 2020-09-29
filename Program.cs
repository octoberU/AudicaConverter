using System;
using System.Collections.Generic;

namespace AudicaConverter
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
        }
    }

    public class OsuToAudica
    {
        public struct TimingPoint
        {
            public double beatTime;
            public float ms;
        }

        public float MsToTick(float ms, List<TimingPoint> timingPoints)
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

    }
}


