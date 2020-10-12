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
    internal class ConfigParameters
    {
        public string customMapperName = "";
        public float introPadding = 2000f;
        public bool snapNotes = false;
        public bool useChainSounds = true;

        public float expertScaleX = 1.375f;
        public float expertScaleY = 1f;
        public float advancedScaleX = 1.1f;
        public float advancedScaleY = 0.8f;
        public float standardScaleX = 0.825f;
        public float standardScaleY = 0.6f;
        public float beginnerScaleX = 0.6875f;
        public float beginnerScaleY = 0.5f;

        public bool adaptiveScaling = true;
        public float fovRecenterTime = 2000f;
        public float scaleDistanceStartThres = 100f;
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
        public float minChainSize = 0.6f;

        public bool distributeStacks = true;
        public float stackInclusionRange = 0.333f;
        public float stackItemDistance = 0.333f;
        public float stackMaxDistance = 1.5f;
        public float stackResetTime = 960f;

        
        //HandColorHandler params
        public float strainDecayBase = 0.4f;
        public float historicalStrainWeight = 0.2f;


        public float timeStrainTransformExponent = 2f;
        public float timeStrainWeight = 1.2f;

        public float movementStrainWeight = 7f;

        public float directionStrainWeight = 3f;

        public float lookAheadTimeLowerLimit = 150f;
        public float lookAheadFixedStrain = 0.1f;
        public float lookAheadDirectionStrainWeight = 15f;

        public float crossoverStrainWeight = 10f;

        public float playspacePositionStrainWeight = 3f;

        public float holdRestTime = 400f;
        public float holdRestTransformExponent = 2.5f;
        public float holdRestStrainWeight = 200f;
        
        public string streamHandPreference = "right";
        public float streamStartStrainWeight = 50f;

        public float streamAlternationWeight = 50f;
    }

   
}

