using System;
using System.Runtime.CompilerServices;

namespace AudicaConverter
{
    internal class HandColorHandler
    {
        private float strain = 0f;
        private bool rightHand = true;
        private float powerExponent = 1.5f;
        private float strainThresholdBase = 4f;

        public int GetHandType(float msSinceLastTarget)
        {
            strain += (float)Math.Pow((msSinceLastTarget / 1000f), -powerExponent);
            if (strain >= Math.Pow(strainThresholdBase, powerExponent))
            {
                strain = 0f;
                rightHand = !rightHand;
            }
            if (rightHand) return 1;
            else return 2;
        }
    }
}