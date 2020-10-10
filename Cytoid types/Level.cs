using System;
using System.IO;

namespace Cytoid
{   
    [Serializable]
    public class Level
    {
        public int version;
        public int schema_version;
        public string id;
        
        public string title;
        public string artist;
        public string artist_source;
        public string illustrator;
        public string illustrator_source;
        public string charter;

        public CytoidPath music;
        public CytoidPath music_preview;
        public CytoidPath background;

        public ChartInfo[] charts;
    }
}