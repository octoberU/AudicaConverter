using Microsoft.VisualBasic.CompilerServices;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace Cytoid
{
    class CytoidLevel
    {
        public Level level;
        public string cytoidLevelPath;
        public List<Chart> charts;

        public CytoidLevel(string filePath)
        {
            cytoidLevelPath = filePath;
            ZipArchive zip = ZipFile.OpenRead(filePath);
            this.level = ReadJsonEntry<Level>(zip, "level.json");

            foreach (var chartInfo in level.charts)
            {
                charts.Add(ReadJsonEntry<Chart>(zip, chartInfo.path));
            }
        }
        private static T ReadJsonEntry<T>(ZipArchive zip, string entryName)
        {
            if (zip.GetEntry(entryName) == null) return default(T);
            var descStream = zip.GetEntry(entryName)
                .Open();
            using (var reader = new StreamReader(descStream))
            {
                string text = reader.ReadToEnd();
                return JsonConvert.DeserializeObject<T>(text);
            }
        }
    }
}
