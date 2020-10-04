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
        public bool snapNotes = false;
        public bool convertChains = true;
        
        //HandColorHandler params
        public float strainDecayRate = 0.5f;
        public float historicalStrainWeight = 0.4f;
        public float timeStrainExponent = 2f;
        public float streamTimeThres = 120f;
        public float timeStrainWeight = 0.5f;
        public float movementStrainWeight = 5f;
        public float directionStrainWeight = 5f;
        public float crossoverStrainWeight = 25f;
        public float playspacePositionStrainWeight = 1f;
        public float streamStartStrainWeight = 50.0f;
    }

   
}

