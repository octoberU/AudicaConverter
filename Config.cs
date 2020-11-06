using AudicaConverter;
using Newtonsoft.Json;
using System;
using System.IO;

namespace osutoaudica
{
    internal static class Config
    {
        public static string configDirectory = Path.Join(Program.workingDirectory, "config.json");

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
            parameters.metadata.version = Program.version;
            string configString = JsonConvert.SerializeObject(parameters, Formatting.Indented);
            File.WriteAllText(configDirectory, configString);
        }
    }

    [Serializable]
    internal class Metadata
    {
        public string version = Program.version;
    }

    [Serializable]
    internal class ConverterOperationOptions
    {
        public string customExportDirectory = "";
        public bool autoMode = false;
        public AutoOptions expertAutoOptions = new AutoOptions(true, 6f, 2f);
        public AutoOptions advancedAutoOptions = new AutoOptions(true, 4f, 1f);
        public AutoOptions standardAutoOptions = new AutoOptions(true, 3f, 1f);
        public AutoOptions beginnerAutoOptions = new AutoOptions(true, 2f, 1f);
    }

    [Serializable]
    internal class AutoOptions
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
    internal class GeneralOptions
    {
        public bool allowOtherGameModes = false;
        public string customMapperName = "";
        public float introPadding = 2000f;
        public SkipIntroOptions skipIntroOptions = new SkipIntroOptions();
        public bool snapNotes = false;
        public bool useStandardSounds = true;
    }

    [Serializable]
    internal class SkipIntroOptions
    {
        public bool enabled = true;
        public float threshold = 10000f;
        public float cutIntroTime = 5000f;
    }

    [Serializable]
    internal class EndPitchKeyOptions
    {
        public bool scrapeKey = true;
        public int scrapeLimit = 14;
        public string defaultEndEvent = "event:/song_end/song_end_C";
    }

    [Serializable]
    internal class ScalingOptions
    {
        public MapScaleOptions expertMapScaleOptions = new MapScaleOptions(1.2f, 1f, 0f);
        public MapScaleOptions advancedMapScaleOptions = new MapScaleOptions(1.08f, 0.9f, 0f);
        public MapScaleOptions standardMapScaleOptions = new MapScaleOptions(0.96f, 0.8f, 0f);
        public MapScaleOptions beginnerMapScaleOptions = new MapScaleOptions(0.84f, 0.7f, 0f);
        public AdaptiveScalingOptions adaptiveScalingOptions = new AdaptiveScalingOptions();
    }

    [Serializable]
    internal class MapScaleOptions
    {
        public float xScale;
        public float yScale;
        public float zOffset;

        public MapScaleOptions(float xScale, float yScale, float zOffset)
        {
            this.xScale = xScale;
            this.yScale = yScale;
            this.zOffset = zOffset;
        }
    }

    [Serializable]
    internal class AdaptiveScalingOptions
    {
        public bool useAdaptiveScaling = true;
        public float fovMotionFactor = 10f;
        public float fovRecenterTime = 3000f;
        public float scaleDistanceStartThres = 80f;
        public float scaleLogBase = 1.02f;
    }

    [Serializable]
    internal class StreamOptions
    {
        public float streamTimeThres = 200f;
        public float streamDistanceThres = 80f;
        public int streamMinNoteCount = 5;
        public float streamMinAverageDistance = 25f;
    }

    [Serializable]
    internal class SliderConversionOptions
    {
        public bool sliderEndStreamStartConvert = true;
        public bool slowRepeatEndsConvert = true;
        public bool fastRepeatStackConvert = true;
        public float fastRepeatTimeThres = 150f;
        public float targetMinTime = 300;
    }

    [Serializable]
    internal class SustainConversionOptions
    {
        public bool convertSustains = true;
        public float minSustainLength = 960f;
        public float maxSustainFraction = 0.15f;
        public float sustainExtension = 240f;
    }

    [Serializable]
    internal class ChainConversionOptions
    {
        public bool convertChains = true;
        public float timeThres = 120f;
        public float switchFrequency = 480f;
        public bool ignoreSlidersForChainConvert = false;
        public bool ignoreSustainsForChainConvert = true;
        public int minChainLinks = 2;
        public float minSize = 1f;
        public float maxAvgLinkDistance = 80f;
        public bool reformChains = true;
        public float sharpChainAngle = 60f;
        public float maxSpeedRatio = 1.5f;
        public float endMinDistanceFromHead = 20f;
    }

    [Serializable]
    internal class MeleeOptions
    {
        public DifficultyMeleeOptions expertMeleeOptions = new DifficultyMeleeOptions(true, 1f, 2f, 2f, 1f, 400f, 400f, 800f, 0.5f, 3.5f, 0f, 0f, true);
        public DifficultyMeleeOptions advancedMeleeOptions = new DifficultyMeleeOptions(true, 1f, 2f, 2f, 1f, 600f, 600f, 1000f, 0.5f, 3.5f, 300f, 300f, true);
        public DifficultyMeleeOptions standardMeleeOptions = new DifficultyMeleeOptions(true, 0.5f, 4f, 1f, 2f, 800f, 800f, 1500f, 1f, 3f, 800f, 800f, true);
        public DifficultyMeleeOptions beginnerMeleeOptions = new DifficultyMeleeOptions(false, 0f, 4f, 0.5f, 4f, 1000f, 1000f, 2000f, 1f, 2.5f, 1000f, 1000f, true);
    }

    [Serializable]
    internal class DifficultyMeleeOptions
    {
        public bool convertMelees;
        public float normalAttemptFrequency;
        public float normalCooldown;
        public float kiaiAttemptFrequency;
        public float kiaiCooldown;
        public float preRestTime;
        public float postRestTime;
        public float prePositionTime;
        public float positionWindowMinDistance;
        public float positionWindowMaxDistance;
        public float preNoTargetTime;
        public float postNoTargetTime;
        public bool removeNoTargetWindowTargets;

        public DifficultyMeleeOptions(bool convertMelees, float normalAttemptFrequency, float normalCooldown, float kiaiAttemptFrequency, float kiaiCooldown,
            float preRestTime, float postRestTime, float prePositionTime, float positionWindowMinDistance, float positionWindowMaxDistance, float preNoTargetTime,
            float postNoTargetTime, bool removeNoTargetWindowTargets)
        {
            this.convertMelees = convertMelees;
            this.normalAttemptFrequency = normalAttemptFrequency;
            this.normalCooldown = normalCooldown;
            this.kiaiAttemptFrequency = kiaiAttemptFrequency;
            this.kiaiCooldown = kiaiCooldown;
            this.preRestTime = preRestTime;
            this.postRestTime = postRestTime;
            this.prePositionTime = prePositionTime;
            this.positionWindowMinDistance = positionWindowMinDistance;
            this.positionWindowMaxDistance = positionWindowMaxDistance;
            this.preNoTargetTime = preNoTargetTime;
            this.postNoTargetTime = postNoTargetTime;
            this.removeNoTargetWindowTargets = removeNoTargetWindowTargets;
        }
    }

    [Serializable]
    internal class StackDistributionOptions
    {
        public bool distributeStacks = true;
        public float inclusionRange = 0.333f;
        public float handSeparation = 0.75f;
        public float itemDistance = 0.333f;
        public float maxDistance = 1.5f;
        public float resetTime = 1000f;
    }
    
    [Serializable]
    internal class HandAssignmentAlgorithmParameters
    {
        public searchParameters searchParameters = new searchParameters();
        public AccumulatedStrain accumulatedStrain = new AccumulatedStrain();
        public TimeStrain timeStrain = new TimeStrain();
        public MovementStrain movementStrain = new MovementStrain();
        public DirectionStrain directionStrain = new DirectionStrain();
        public LookAheadDirectionStrain lookAheadDirectionStrain = new LookAheadDirectionStrain();
        public CrossoverStrain crossoverStrain = new CrossoverStrain();
        public PlayspacePositionStrain playspacePositionStran = new PlayspacePositionStrain();
        public HoldRestStrain holdRestStrain = new HoldRestStrain();
        public StreamStartStrain streamStartStrain = new StreamStartStrain();
        public StreamAlternationStrain streamAlternationStrain = new StreamAlternationStrain();
    }

    [Serializable]
    internal class searchParameters
    {
        public int exhaustiveSearchDepth = 8;
        public int greedySimulationDepth = 30;
        public float searchStrainExponent = 1.5f;
    }

    [Serializable]
    internal class AccumulatedStrain
    {
        public float strainDecayBase = 0.4f;
        public float historicalStrainWeight = 0.2f;
    }

    [Serializable]
    internal class TimeStrain
    {
        public float transformExponent = 2.5f;
        public float weight = 0.5f;
    }

    [Serializable]
    internal class MovementStrain
    {
        public float weight = 7f;
    }

    [Serializable]
    internal class DirectionStrain
    {
        public float weight = 3f;
    }

    [Serializable]
    internal class LookAheadDirectionStrain
    {
        public float timeLowerLimit = 150f;
        public float fixedStrain = 0.1f;
        public float weight = 15f;
    }

    [Serializable]
    internal class CrossoverStrain
    {
        public float weight = 10f;
    }

    [Serializable]
    internal class PlayspacePositionStrain
    {
        public float weight = 5f;
    }

    [Serializable]
    internal class HoldRestStrain
    {
        public float time = 400f;
        public float transformExponent = 2.5f;
        public float weight = 200f;
    }

    [Serializable]
    internal class StreamStartStrain
    {
        public string startHandPreference = "right";
        public float weight = 100f;
    }

    [Serializable]
    internal class StreamAlternationStrain
    {
        public float weight = 50f;
    }

    [Serializable]
    internal class ConfigParameters
    {
        public Metadata metadata = new Metadata();

        public ConverterOperationOptions converterOperationOptions = new ConverterOperationOptions();

        public GeneralOptions generalOptions = new GeneralOptions();

        public EndPitchKeyOptions endPitchKeyOptions = new EndPitchKeyOptions();

        public ScalingOptions scalingOptions = new ScalingOptions();

        public StreamOptions streamOptions = new StreamOptions();

        public SliderConversionOptions sliderConversionOptions = new SliderConversionOptions();

        public SustainConversionOptions sustainConversionOptions = new SustainConversionOptions();

        public ChainConversionOptions chainConversionOptions = new ChainConversionOptions();

        public MeleeOptions meleeOptions = new MeleeOptions();

        public StackDistributionOptions stackDistributionOptions = new StackDistributionOptions();

        public HandAssignmentAlgorithmParameters handAssignmentAlgorithmParameters = new HandAssignmentAlgorithmParameters();
    }
}

