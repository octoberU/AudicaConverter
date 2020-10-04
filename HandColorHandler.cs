using osutoaudica;
using OsuTypes;
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;

namespace AudicaConverter
{
    internal class HandColorHandler
    {
        //The base for exponential decay of accumulated strain on each hand. e.g. strainDecayRate of 0.5 means accumulated strain on each hand is halved every second.
        private float strainDecayRate => Config.parameters.strainDecayRate;

        // Weights the impact of accumulated historical strain compared to the immediate strain of the new target when choosing hand.
        private float historicalStrainWeight => Config.parameters.historicalStrainWeight;

        //The exponent for which inversed time since last target will be power transformed by. Adjusting this allows adjusting relative strain of different time spacings.
        private float timeStrainExponent => Config.parameters.timeStrainExponent;

        //The amount of time (ms) between notes to be counted as a stream in terms of biasing in favour of right-hand start.
        private float streamTimeThres => Config.parameters.streamTimeThres;

        //The fixed, distance-independent strain factor of look-ahead strain
        private float lookAheadFixedStrain => Config.parameters.lookAheadFixedStrain;

        //The weight for timing strains impact on total strain
        private float timeStrainWeight => Config.parameters.timeStrainWeight;

        //The weight for movement speed strain
        private float movementStrainWeight => Config.parameters.movementStrainWeight;

        //The weight for movement direction strain
        private float directionStrainWeight => Config.parameters.directionStrainWeight;

        //The weight for look-ahead movement direction strain
        private float lookAheadDirectionStrainWeight => Config.parameters.lookAheadDirectionStrainWeight;

        //The weight for crossover strain
        private float crossoverStrainWeight => Config.parameters.crossoverStrainWeight;

        //The weight of playspace position strain, favouring left hand for left side of playspace and vice versa.
        private float playspacePositionStrainWeight => Config.parameters.playspacePositionStrainWeight;

        //The weight for starting streams on left hand
        private float streamStartStrainWeight => Config.parameters.streamStartStrainWeight;

        private bool convertChains => Config.parameters.convertChains;

        private float rightStrain = 0f;
        private float leftStrain = 0f;

        public int GetHandType(HitObject hitObject, HitObject prevRightHitObject, HitObject prevLeftHitObject, HitObject nextHitObject) {
            HitObject prevHitObject;
            if (prevRightHitObject != null && prevLeftHitObject != null) prevHitObject = prevRightHitObject.time > prevLeftHitObject.time ? prevRightHitObject : prevLeftHitObject;
            else if (prevRightHitObject != null) prevHitObject = prevRightHitObject;
            else prevHitObject = prevLeftHitObject;

            //Decay previous strain
            if (prevHitObject != null)
            {
                rightStrain *= (float)Math.Pow(strainDecayRate, (hitObject.time - prevHitObject.time) / 1000f);
                leftStrain *= (float)Math.Pow(strainDecayRate, (hitObject.time - prevHitObject.time) / 1000f);
            }

            //Time strain
            float rightTimeStrain = 0f;
            float leftTimeStrain = 0f;
            if (prevRightHitObject != null) rightTimeStrain = (float)Math.Pow((hitObject.time - prevRightHitObject.time) / 1000f, -timeStrainExponent);
            if (prevLeftHitObject != null) leftTimeStrain = (float)Math.Pow((hitObject.time - prevLeftHitObject.time) / 1000f, -timeStrainExponent);

            //Movement speed strain
            float rightMovementStrain = 0f;
            float leftMovementStrain = 0f;
            if (prevRightHitObject != null) rightMovementStrain = OsuUtility.EuclideanDistance(prevRightHitObject.x, prevRightHitObject.y, hitObject.x, hitObject.y) / 512f / ((hitObject.time - prevRightHitObject.time)/1000f);
            if (prevLeftHitObject != null) leftMovementStrain = OsuUtility.EuclideanDistance(prevLeftHitObject.x, prevLeftHitObject.y, hitObject.x, hitObject.y) / 512f / ((hitObject.time - prevLeftHitObject.time) / 1000f);

            //Direction strain. Adds strain based on horizontal movement from the previous target
            float rightDirectionStrain = 0f;
            float leftDirectionStrain = 0f;
            if (prevHitObject != null)
            {
                rightDirectionStrain = Math.Max(-(hitObject.x - prevHitObject.x) / 512f, 0f) / ((hitObject.time - prevHitObject.time) / 1000f);
                leftDirectionStrain = Math.Max((hitObject.x - prevHitObject.x) / 512f, 0f) / ((hitObject.time - prevHitObject.time) / 1000f);
            }

            //Look-ahead direction strains, sets up better future handing with less crossover, and centers the players view better on upcoming notes
            float rightLookAheadDirectionStrain = 0f;
            float leftLookAheadDirectionStrain = 0f;
            if (nextHitObject != null)
            {
                if (nextHitObject.x - hitObject.x > 0) rightLookAheadDirectionStrain = ((nextHitObject.x - hitObject.x) / 512f + lookAheadFixedStrain) / ((nextHitObject.time - hitObject.time) / 1000f);
                if (nextHitObject.x - hitObject.x < 0) leftLookAheadDirectionStrain = (-(nextHitObject.x - hitObject.x) / 512f + lookAheadFixedStrain) / ((nextHitObject.time - hitObject.time) / 1000f);
            }

            //Crossover strain. attempts to identify sustained crossover positions based of both previous and next target
            float rightCrossoverStrain = 0f;
            float leftCrossoverStrain = 0f;
            if (prevHitObject != null && nextHitObject != null)
            {
                rightCrossoverStrain = Math.Max(Math.Min(-(hitObject.x - prevHitObject.x), -(hitObject.x - nextHitObject.x)) / 512f, 0f) / ((nextHitObject.time - prevHitObject.time) / 2000f);
                leftCrossoverStrain = Math.Max(Math.Min((hitObject.x - prevHitObject.x), (hitObject.x - nextHitObject.x)) / 512f, 0f) / ((nextHitObject.time - prevHitObject.time) / 2000f);
            }

            //Playfield position strain
            float rightPlayspacePositionStrain = Math.Max(1f - hitObject.x / 206f, 0f);
            float leftPlayspacePositionStrain = Math.Max(hitObject.x / 206f - 1f, 0f);

            //Stream start strain
            float leftStreamStartStrain = 0f;
            if (!convertChains && nextHitObject != null && nextHitObject.time - hitObject.time <= streamTimeThres && (prevHitObject == null || hitObject.time - prevHitObject.time > streamTimeThres))
                leftStreamStartStrain = 1f;

            
            //Strain combination through weighted sum
            float rightStrainIncrease = timeStrainWeight * rightTimeStrain + movementStrainWeight * rightMovementStrain + directionStrainWeight * rightDirectionStrain + lookAheadDirectionStrainWeight * rightLookAheadDirectionStrain +
                crossoverStrainWeight * rightCrossoverStrain + playspacePositionStrainWeight * rightPlayspacePositionStrain;
            float leftStrainIncrease = timeStrainWeight * leftTimeStrain + movementStrainWeight * leftMovementStrain + directionStrainWeight * leftDirectionStrain + lookAheadDirectionStrainWeight * leftLookAheadDirectionStrain +
                crossoverStrainWeight * leftCrossoverStrain + playspacePositionStrainWeight * leftPlayspacePositionStrain + streamStartStrainWeight * leftStreamStartStrain;

            //Strain based hand selection and accumulated strain increase
            if (historicalStrainWeight * rightStrain + rightStrainIncrease <= historicalStrainWeight * leftStrain + leftStrainIncrease)
            {
                rightStrain += rightStrainIncrease;
                return 1;
            }
            else
            {
                leftStrain += leftStrainIncrease;
                return 2;
            }
        }
    }
}