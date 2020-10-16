using osutoaudica;
using OsuTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;

namespace AudicaConverter
{
    internal class HandColorHandler
    {
        private int exhaustiveSearchDepth => Config.parameters.exhaustiveSearchDepth;

        private int greedySimulationDepth => Config.parameters.greedySimulationDepth;

        private float searchStrainExponent => Config.parameters.searchStrainExponent;

        //The base for exponential decay of accumulated strain on each hand. e.g. a value of means accumulated strain on each hand is halved every second.
        private float strainDecayBase => Config.parameters.strainDecayBase;

        // Weights the impact of accumulated historical strain compared to the immediate strain of the new target when choosing hand.
        private float historicalStrainWeight => Config.parameters.historicalStrainWeight;

        //The exponent for which inversed time since last target will be power transformed by. Adjusting this allows adjusting relative strain of different time spacings.
        private float timeStrainTransformExponent => Config.parameters.timeStrainTransformExponent;

        //The hand streams are prefered to start on
        private string streamStartHandPreference => Config.parameters.streamHandPreference;

        //A limit on how small the time difference between notes for look ahead strain can be. Prevents overweighting in chain hand-overs.
        private float lookAheadTimeLowerLimit => Config.parameters.lookAheadTimeLowerLimit;

        //The fixed, distance-independent strain factor of look-ahead strain
        private float lookAheadFixedStrain => Config.parameters.lookAheadFixedStrain;

        //The time window after a hold where post-hold rest is encouraged
        private float holdRestTime => Config.parameters.holdRestTime;

        //The exponent of the power transform of the hold rest strain
        private float holdRestTransformExponent => Config.parameters.holdRestTransformExponent;

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

        //The weight for post-hold refire strain
        private float holdRestStrainWeight => Config.parameters.holdRestStrainWeight;

        //The weight for starting streams on left hand
        private float streamStartStrainWeight => Config.parameters.streamStartStrainWeight;

        //The weight for alternating hands on streams
        private float streamAlternationStrainWeight => Config.parameters.streamAlternationWeight;

        public void AssignHandTypes(List<HitObject> hitObjects)
        {
            List<HitObject> assignableHitObjects = hitObjects.Where(ho => ho.audicaBehavior != 5).ToList();
            HitObject prevRightHitObject = null;
            HitObject prevLeftHitObject = null;
            float rightHistoricStrain = 0f;
            float leftHistoricStrain = 0f;

            for (int i = 0; i < assignableHitObjects.Count; i++)
            {
                HitObject currentHitObject = assignableHitObjects[i];
                HitObject prevHitObject = i > 0 ? assignableHitObjects[i - 1] : null;

                var searchResult = ExhaustiveSearchStrain(assignableHitObjects, i, prevRightHitObject, prevLeftHitObject, rightHistoricStrain, leftHistoricStrain, exhaustiveSearchDepth);

                currentHitObject.audicaHandType = searchResult.handType;

                //Decay strain
                if (prevHitObject != null)
                {
                    rightHistoricStrain *= (float)Math.Pow(strainDecayBase, (currentHitObject.time - prevHitObject.time) / 1000f);
                    leftHistoricStrain *= (float)Math.Pow(strainDecayBase, (currentHitObject.time - prevHitObject.time) / 1000f);
                }

                if (searchResult.handType == 1)
                {
                    rightHistoricStrain += searchResult.targetStrain;
                    prevRightHitObject = currentHitObject;
                }
                else
                {
                    leftHistoricStrain += searchResult.targetStrain;
                    prevLeftHitObject = currentHitObject;
                } 
            }

            for (int i = 0; i < hitObjects.Count; i++)
            {
                if (hitObjects[i].audicaBehavior == 5)
                    hitObjects[i].audicaHandType = hitObjects[i - 1].audicaHandType;
            }
        }

        //Recursive method for exhaustive search for sequence of hand assignments that gives the least combined, transformed future strain.
        public (int handType, float targetStrain, float pathSumStrain) ExhaustiveSearchStrain(List<HitObject> hitObjects, int currentHitObjectIdx, HitObject prevRightHitObject, HitObject prevLeftHitObject, float rightHistoricStrain, float leftHistoricStrain, int depth,
            float sumStrain = 0f, float bestPathSumStrain = float.PositiveInfinity)
        {
            if (depth == 0) //Base case, switch to greedy simulation search
            {
                return GreedySimulationStrain(hitObjects, currentHitObjectIdx, prevRightHitObject, prevLeftHitObject, rightHistoricStrain, leftHistoricStrain, greedySimulationDepth, sumStrain, bestPathSumStrain);
            }

            HitObject currentHitObject = hitObjects[currentHitObjectIdx];
            HitObject nextHitObject = currentHitObjectIdx + 1 < hitObjects.Count ? hitObjects[currentHitObjectIdx + 1] : null;
            HitObject prevHitObject;
            if (prevRightHitObject != null && prevLeftHitObject != null) prevHitObject = prevRightHitObject.time > prevLeftHitObject.time ? prevRightHitObject : prevLeftHitObject;
            else if (prevRightHitObject != null) prevHitObject = prevRightHitObject;
            else prevHitObject = prevLeftHitObject;

            //Decay previous strain
            if (prevHitObject != null)
            {
                rightHistoricStrain *= (float)Math.Pow(strainDecayBase, (currentHitObject.time - prevHitObject.time) / 1000f);
                leftHistoricStrain *= (float)Math.Pow(strainDecayBase, (currentHitObject.time - prevHitObject.time) / 1000f);
            }

            float rightTargetStrain = GetRightTargetStrain(currentHitObject, prevHitObject, prevRightHitObject, nextHitObject);
            float leftTargetStrain = GetLeftTargetStrain(currentHitObject, prevHitObject, prevLeftHitObject, nextHitObject);

            float rightCombStrain = historicalStrainWeight * rightHistoricStrain + rightTargetStrain;
            float leftCombStrain = historicalStrainWeight * leftHistoricStrain + leftTargetStrain;

            float rightBranchSumStrain = sumStrain + (float)Math.Pow(rightCombStrain, searchStrainExponent);
            float leftBranchSumStrain = sumStrain + (float)Math.Pow(leftCombStrain, searchStrainExponent);

            float leftPathSumStrain = float.PositiveInfinity;
            float rightPathSumStrain = float.PositiveInfinity;
            
            //Only keep searching if there are targets remaining
            if (currentHitObjectIdx + 1 < hitObjects.Count)
            {
                //Search down the opposite path of the previous target hand first. Allows more efficient pruning since alternation is expected to be a relatively low-strain solution.
                if ((prevRightHitObject != null ? prevRightHitObject.time : 0f) >= (prevLeftHitObject != null ? prevLeftHitObject.time : 0f))
                {   
                    //Search left branch if it could be better than the previously best found full path
                    if (leftBranchSumStrain < bestPathSumStrain)
                    {
                        leftPathSumStrain = ExhaustiveSearchStrain(hitObjects, currentHitObjectIdx + 1, prevRightHitObject, currentHitObject, rightHistoricStrain, leftHistoricStrain + leftTargetStrain, depth - 1, leftBranchSumStrain, bestPathSumStrain).pathSumStrain;
                        bestPathSumStrain = Math.Min(bestPathSumStrain, leftPathSumStrain);
                    }
                    //Search right branch if it could be better than the previously best found full path
                    if (rightBranchSumStrain < bestPathSumStrain)
                    {
                        rightPathSumStrain = ExhaustiveSearchStrain(hitObjects, currentHitObjectIdx + 1, currentHitObject, prevLeftHitObject, rightHistoricStrain + rightTargetStrain, leftHistoricStrain, depth - 1, rightBranchSumStrain, bestPathSumStrain).pathSumStrain;
                    }
                }
                else
                {
                    //Search right branch if it could be better than the previously best found full path
                    if (rightBranchSumStrain < bestPathSumStrain)
                    {
                        rightPathSumStrain = ExhaustiveSearchStrain(hitObjects, currentHitObjectIdx + 1, currentHitObject, prevLeftHitObject, rightHistoricStrain + rightTargetStrain, leftHistoricStrain, depth - 1, rightBranchSumStrain, bestPathSumStrain).pathSumStrain;
                        bestPathSumStrain = Math.Min(bestPathSumStrain, rightPathSumStrain);
                    }
                    //Search left branch if it could be better than the previously best found full path
                    if (leftBranchSumStrain < bestPathSumStrain)
                    {
                        leftPathSumStrain = ExhaustiveSearchStrain(hitObjects, currentHitObjectIdx + 1, prevRightHitObject, currentHitObject, rightHistoricStrain, leftHistoricStrain + leftTargetStrain, depth - 1, leftBranchSumStrain, bestPathSumStrain).pathSumStrain;
                    }
                }
            }
            else
            {
                rightPathSumStrain = rightBranchSumStrain;
                leftPathSumStrain = leftBranchSumStrain;
            }
            

            if (rightPathSumStrain <= leftPathSumStrain) return (1, rightTargetStrain, rightPathSumStrain);
            else return (2, leftTargetStrain, leftPathSumStrain);
        }

        //Recursive method for greedy simulation to find future strain.
        public (int handType, float targetStrain, float pathSumStrain) GreedySimulationStrain(List<HitObject> hitObjects, int currentHitObjectIdx, HitObject prevRightHitObject, HitObject prevLeftHitObject, float rightHistoricStrain, float leftHistoricStrain, int depth,
            float sumStrain, float bestPathSumStrain)
        {
            HitObject currentHitObject = hitObjects[currentHitObjectIdx];
            HitObject nextHitObject = currentHitObjectIdx + 1 < hitObjects.Count ? hitObjects[currentHitObjectIdx + 1] : null;
            HitObject prevHitObject;
            if (prevRightHitObject != null && prevLeftHitObject != null) prevHitObject = prevRightHitObject.time > prevLeftHitObject.time ? prevRightHitObject : prevLeftHitObject;
            else if (prevRightHitObject != null) prevHitObject = prevRightHitObject;
            else prevHitObject = prevLeftHitObject;

            //Decay previous strain
            if (prevHitObject != null)
            {
                rightHistoricStrain *= (float)Math.Pow(strainDecayBase, (currentHitObject.time - prevHitObject.time) / 1000f);
                leftHistoricStrain *= (float)Math.Pow(strainDecayBase, (currentHitObject.time - prevHitObject.time) / 1000f);
            }

            float rightTargetStrain = GetRightTargetStrain(currentHitObject, prevHitObject, prevRightHitObject, nextHitObject);
            float leftTargetStrain = GetLeftTargetStrain(currentHitObject, prevHitObject, prevLeftHitObject, nextHitObject);

            float rightCombStrain = historicalStrainWeight * rightHistoricStrain + rightTargetStrain;
            float leftCombStrain = historicalStrainWeight * leftHistoricStrain + leftTargetStrain;

            float rightBranchSumStrain = sumStrain + (float)Math.Pow(rightCombStrain, searchStrainExponent);
            float leftBranchSumStrain = sumStrain + (float)Math.Pow(leftCombStrain, searchStrainExponent);

            float pathSumStrain = float.PositiveInfinity;


            //Only keep simulating if there is still depth left to search, and if there are more targets
            if (depth > 1 && currentHitObjectIdx + 1 < hitObjects.Count)
            {
                if (rightCombStrain <= leftCombStrain && rightBranchSumStrain < bestPathSumStrain)
                {
                    pathSumStrain = GreedySimulationStrain(hitObjects, currentHitObjectIdx + 1, currentHitObject, prevLeftHitObject, rightHistoricStrain + rightTargetStrain, leftHistoricStrain, depth - 1, rightBranchSumStrain, bestPathSumStrain).pathSumStrain;
                }
                else if (leftCombStrain < rightCombStrain && leftBranchSumStrain < bestPathSumStrain)
                {
                    pathSumStrain = GreedySimulationStrain(hitObjects, currentHitObjectIdx + 1, prevRightHitObject, currentHitObject, rightHistoricStrain, leftHistoricStrain + leftTargetStrain, depth - 1, leftBranchSumStrain, bestPathSumStrain).pathSumStrain;
                }
            }
            else
            {
                pathSumStrain = (float)Math.Min(rightBranchSumStrain, leftBranchSumStrain);
            }

            if (rightCombStrain <= leftCombStrain) return (1, rightTargetStrain, pathSumStrain);
            else return (2, leftTargetStrain, pathSumStrain);
        }

        public float GetRightTargetStrain(HitObject hitObject, HitObject prevHitObject, HitObject prevRightHitObject, HitObject nextHitObject)
        {
            float timeStrain = 0f;
            if (prevRightHitObject != null) timeStrain = (float)Math.Pow(Math.Max(hitObject.time - prevRightHitObject.endTime, 50f) / 1000f, -timeStrainTransformExponent);

            float movementStrain = 0f;
            if (prevRightHitObject != null) movementStrain = OsuUtility.EuclideanDistance(prevRightHitObject.endX, prevRightHitObject.endY, hitObject.x, hitObject.y) / 512f / (Math.Max(hitObject.time - prevRightHitObject.endTime, 50f) / 1000f);

            float directionStrain = 0f;
            if (prevHitObject != null) directionStrain = Math.Max(-(hitObject.x - prevHitObject.endX) / 512f, 0f) / (Math.Max(hitObject.time - prevHitObject.endTime, 100f) / 1000f);

            float lookAheadDirectionStrain = 0f;
            if (nextHitObject != null && nextHitObject.x - hitObject.endX > 0)
                lookAheadDirectionStrain = ((nextHitObject.x - hitObject.endX) / 512f + lookAheadFixedStrain) / (Math.Max((nextHitObject.time - hitObject.endTime), lookAheadTimeLowerLimit) / 1000f);

            float crossoverStrain = 0f;
            if (prevHitObject != null && nextHitObject != null)
                crossoverStrain = Math.Max(Math.Min(-(hitObject.x - prevHitObject.endX), (nextHitObject.x - hitObject.endX)) / 512f, 0f) / (Math.Max(nextHitObject.time - hitObject.endTime + hitObject.time - prevHitObject.endTime, 100f) / 2000f);

            float playspacePositionStrain = Math.Max(1f - hitObject.x / 256f, 0f);

            float holdStrain = 0f;
            if (prevRightHitObject != null && (prevRightHitObject.audicaBehavior == 3 || prevRightHitObject.audicaBehavior == 4) && hitObject.time - prevRightHitObject.endTime < holdRestTime)
                holdStrain = (float)Math.Pow(1 - (hitObject.time - prevRightHitObject.endTime) / holdRestTime, holdRestTransformExponent);

            float streamStartStrain = 0f;
            float streamAlternationStrain = 0f;
            if (hitObject.noteStream != null && hitObject.audicaBehavior != 4)
            {
                if (hitObject == hitObject.noteStream.hitObjects[0])
                {
                    if (streamStartHandPreference.ToLower() == "left") streamStartStrain = 1f;
                }
                else if (prevHitObject.audicaBehavior == 1) streamAlternationStrain = 1f;
            }

            float targetStrain = timeStrainWeight * timeStrain + movementStrainWeight * movementStrain + directionStrainWeight * directionStrain + lookAheadDirectionStrainWeight * lookAheadDirectionStrain +
                crossoverStrainWeight * crossoverStrain + playspacePositionStrainWeight * playspacePositionStrain + holdRestStrainWeight * holdStrain +
                streamStartStrainWeight * streamStartStrain + streamAlternationStrainWeight * streamAlternationStrain;

            return targetStrain;
        }

        public float GetLeftTargetStrain(HitObject hitObject, HitObject prevHitObject, HitObject prevLeftHitObject, HitObject nextHitObject)
        {
            float timeStrain = 0f;
            if (prevLeftHitObject != null) timeStrain = (float)Math.Pow(Math.Max((hitObject.time - prevLeftHitObject.endTime), 50f) / 1000f, -timeStrainTransformExponent);

            float movementStrain = 0f;
            if (prevLeftHitObject != null) movementStrain = OsuUtility.EuclideanDistance(prevLeftHitObject.endX, prevLeftHitObject.endY, hitObject.x, hitObject.y) / 512f / (Math.Max(hitObject.time - prevLeftHitObject.endTime, 50f) / 1000f);

            float directionStrain = 0f;
            if (prevHitObject != null) directionStrain = Math.Max((hitObject.x - prevHitObject.endY) / 512f, 0f) / (Math.Max(hitObject.time - prevHitObject.endTime, 100f) / 1000f);

            float lookAheadDirectionStrain = 0f;
            if (nextHitObject != null && nextHitObject.x - hitObject.endX < 0)
                lookAheadDirectionStrain = (-(nextHitObject.x - hitObject.endX) / 512f + lookAheadFixedStrain) / (Math.Max((nextHitObject.time - hitObject.endTime), lookAheadTimeLowerLimit) / 1000f);

            float crossoverStrain = 0f;
            if (prevHitObject != null && nextHitObject != null)
                crossoverStrain = Math.Max(Math.Min((hitObject.x - prevHitObject.endX), -(nextHitObject.x - hitObject.endX)) / 512f, 0f) / (Math.Max(nextHitObject.time - hitObject.endTime + hitObject.time - prevHitObject.endTime, 100f) / 2000f);

            float playspacePositionStrain = Math.Max(hitObject.x / 256f - 1f, 0f); ;

            float holdStrain = 0f;
            if (prevLeftHitObject != null && (prevLeftHitObject.audicaBehavior == 3 || prevLeftHitObject.audicaBehavior == 4) && hitObject.time - prevLeftHitObject.endTime < holdRestTime)
                holdStrain = (float)Math.Pow(1 - (hitObject.time - prevLeftHitObject.endTime) / holdRestTime, holdRestTransformExponent);

            float streamStartStrain = 0f;
            float streamAlternationStrain = 0f;
            if (hitObject.noteStream != null && hitObject.audicaBehavior != 4)
            {
                if (hitObject == hitObject.noteStream.hitObjects[0])
                {
                    if (streamStartHandPreference.ToLower() == "right") streamStartStrain = 1f;
                }
                else if (prevHitObject.audicaBehavior == 2) streamAlternationStrain = 1f;
            }

            float targetStrain = timeStrainWeight * timeStrain + movementStrainWeight * movementStrain + directionStrainWeight * directionStrain + lookAheadDirectionStrainWeight * lookAheadDirectionStrain +
                crossoverStrainWeight * crossoverStrain + playspacePositionStrainWeight * playspacePositionStrain + holdRestStrainWeight * holdStrain +
                streamStartStrainWeight * streamStartStrain + streamAlternationStrainWeight * streamAlternationStrain;


            return targetStrain;
        }
    }
}