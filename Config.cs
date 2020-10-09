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
        public float scaleX = 0.8f;
        public float scaleY = 0.8f;
        public bool snapNotes = false;
        public bool convertSliderEnds = false;
        public bool convertChains = true;
        public float chainTimeThres = 120f;
        public float chainSwitchFrequency = 480f;
        public bool ignoreSlidersForChainConvert = false;
        public bool ignoreSustainsForChainConvert = true;
        public bool convertSustains = true;
        public float minSustainLength = 960f;
        public float sustainExtension = 160f;
        public float introPadding = 2000f;
        public bool useChainSounds = true;
        public string customMapperName = "";

        public float stackItemDistance = 0.333f;
        public float stackMaxDistance = 1.5f;
        public float stackResetTime = 960f;

        //HandColorHandler params
        public float strainDecayRate = 0.4f;
        public float historicalStrainWeight = 0.2f;
        public float timeStrainExponent = 2f;
        public float streamTimeThres = 120f;
        public float streamDistanceThres = 0.1f;
        public float lookAheadTimeCap = 150f;
        public float lookAheadFixedStrain = 0.1f;
        public float holdRestTime = 400f;
        public float holdRestTransformExponent = 2.5f;
        public float timeStrainWeight = 1.2f;
        public float movementStrainWeight = 7f;
        public float directionStrainWeight = 3f;
        public float lookAheadDirectionStrainWeight = 15.0f;
        public float crossoverStrainWeight = 10f;
        public float playspacePositionStrainWeight = 3f;
        public float holdRestStrainWeight = 200f;
        public float streamStartStrainWeight = 50.0f;
    }

   
}

