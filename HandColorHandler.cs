using System.Runtime.CompilerServices;

namespace AudicaConverter
{
    internal class HandColorHandler
    {
        public float msCounter = 0f;
        public int GetHandType(float msDistanceToLastTarget)
        {
            msCounter += msDistanceToLastTarget;

            // do logic stuff here

            return 1; //return some kind of handtype here based on formula above
        }
    }
}