using AudicaConverter;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace osutoaudica
{
    internal static class Config
    {
        public static string configDirectory = Program.workingDirectory + @"\config.json";

        public static ConfigParameters parameters;

        public static void Init()
        {
            if (File.Exists(configDirectory))
            {
                LoadConfig();
                SaveConfig();
            }
            else
            {
                parameters = new ConfigParameters();
                SaveConfig();
            }
        }

        public static void LoadConfig()
        {
            if (File.Exists(configDirectory))
            {
                string configString = File.ReadAllText(configDirectory);
                parameters = JsonConvert.DeserializeObject<ConfigParameters>(configString);
            }
        }

        public static void SaveConfig()
        {
            string configString = JsonConvert.SerializeObject(parameters, Formatting.Indented);
            File.WriteAllText(configDirectory, configString);
        }
    }

    [Serializable]
    internal struct AutoOptions
    {
        public bool useDifficultySlot;
        public float targetDifficultyRating;
        public float acceptedDifficultyRatingDifference;

        public AutoOptions(bool useDifficultySlot, float targetDifficultyRating, float acceptedDifficultyRatingDifference)
        {
            this.useDifficultySlot = useDifficultySlot;
            this.targetDifficultyRating = targetDifficultyRating;
            this.acceptedDifficultyRatingDifference = acceptedDifficultyRatingDifference;
        }
    }

    [Serializable]
    internal struct ScalingOptions
    {
        public float xScale;
        public float yScale;

        public ScalingOptions(float xScale, float yScale)
        {
            this.xScale = xScale;
            this.yScale = yScale;
        }
    }

    [Serializable]
    internal struct MeleeOptions
    {
        public bool convertMelees;
        public float normalFrequency;
        public float kiaiFrequency;
        public float preRestTime;
        public float postRestTime;
        public float prePositionTime;
        public float positionWindowMinDistance;
        public float positionWindowMaxDistance;

        public MeleeOptions(bool convertMelees, float normalFrequency, float kiaiFrequency, float preRestTime, float postRestTime, float prePositionTime,
            float positionWindowMinDistance, float positionWindowMaxDistance)
        {
            this.convertMelees = convertMelees;
            this.normalFrequency = normalFrequency;
            this.kiaiFrequency = kiaiFrequency;
            this.preRestTime = preRestTime;
            this.postRestTime = postRestTime;
            this.prePositionTime = prePositionTime;
            this.positionWindowMinDistance = positionWindowMinDistance;
            this.positionWindowMaxDistance = positionWindowMaxDistance;
        }
    }
    [Serializable]
    internal class SkipIntro
    {
        public bool enabled = true;
        public float threshold = 10000f;
        public float fadeTime = 5000f;
    }

    [Serializable]
    internal class ConfigParameters
    {
        public string customExportDirectory = "";
        public bool autoMode = false;
        public AutoOptions expertAutoOptions = new AutoOptions(true, 6f, 3f);
        public AutoOptions advancedAutoOptions = new AutoOptions(true, 4f, 2f);
        public AutoOptions standardAutoOptions = new AutoOptions(true, 3f, 2f);
        public AutoOptions beginnerAutoOptions = new AutoOptions(true, 2f, 2f);

        public bool allowOtherGameModes = false;
        public string customMapperName = "";
        public float introPadding = 2000f;
        public SkipIntro skipIntro = new SkipIntro();
        public bool snapNotes = false;
        public bool useStandardSounds = true;

        public ScalingOptions expertScalingOptions = new ScalingOptions(1.2f, 1f);
        public ScalingOptions advancedScalingOptions = new ScalingOptions(0.96f, 0.8f);
        public ScalingOptions standardScalingOptions = new ScalingOptions(0.78f, 0.65f);
        public ScalingOptions beginnerScalingOptions = new ScalingOptions(0.6f, 0.5f);

        public bool adaptiveScaling = true;
        public float fovRecenterTime = 2000f;
        public float scaleDistanceStartThres = 80f;
        public float scaleLogBase = 1.02f;

        public float streamTimeThres = 200f;
        public float streamDistanceThres = 80f;
        public int streamMinNoteCount = 5;
        public float streamMinAverageDistance = 25f;
        
        public bool convertSliderEnds = false;

        public bool convertSustains = true;
        public float minSustainLength = 960f;
        public float sustainExtension = 240f;

        public bool convertChains = true;
        public float chainTimeThres = 120f;
        public float chainSwitchFrequency = 480f;
        public bool ignoreSlidersForChainConvert = false;
        public bool ignoreSustainsForChainConvert = true;
        public int minChainLinks = 2;
        public float minChainSize = 1f;
        public bool reformChains = true;
        public float sharpChainAngle = 75f;
        public float chainMaxSpeedRatio = 1.5f;
        public float chainEndMinDistanceFromHead = 20f;

        public MeleeOptions expertMeleeOptions = new MeleeOptions(true, 0.5f, 1f, 400f, 400f, 800f, 0.5f, 3.5f);
        public MeleeOptions advancedMeleeOptions = new MeleeOptions(true, 0.5f, 1f, 600f, 600f, 1000f, 0.5f, 3.5f);
        public MeleeOptions standardMeleeOptions = new MeleeOptions(true, 0.25f, 0.5f, 800f, 800f, 1500f,  1f, 3f);
        public MeleeOptions beginnerMeleeOptions = new MeleeOptions(false, 0f, 0.5f, 1000f, 1000f, 2000f, 1f, 2.5f);

        public bool distributeStacks = true;
        public float stackInclusionRange = 0.333f;
        public float stackHandSeparation = 0.75f;
        public float stackItemDistance = 0.333f;
        public float stackMaxDistance = 1.5f;
        public float stackResetTime = 1000f;


        //HandColorHandler params
        public int exhaustiveSearchDepth = 8;
        public int greedySimulationDepth = 30;
        public float searchStrainExponent = 1.5f;

        public float strainDecayBase = 0.4f;
        public float historicalStrainWeight = 0.2f;

        public float timeStrainTransformExponent = 2.5f;
        public float timeStrainWeight = 0.5f;

        public float movementStrainWeight = 7f;

        public float directionStrainWeight = 3f;

        public float lookAheadTimeLowerLimit = 150f;
        public float lookAheadFixedStrain = 0.1f;
        public float lookAheadDirectionStrainWeight = 15f;

        public float crossoverStrainWeight = 10f;

        public float playspacePositionStrainWeight = 5f;

        public float holdRestTime = 400f;
        public float holdRestTransformExponent = 2.5f;
        public float holdRestStrainWeight = 200f;
        
        public string streamHandPreference = "right";
        public float streamStartStrainWeight = 50f;

        public float streamAlternationWeight = 50f;
    }

   
}

