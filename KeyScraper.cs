using System;
using System.Collections.Generic;
using System.Net;
using System.Text.RegularExpressions;
using System.Web;

namespace osutoaudica
{
    static class KeyScraper
    {

        public static Dictionary<string, string> pitchEventDict = new Dictionary<string, string>
        {
            {"A", "A" },
            {"A#", "A#" },
            {"B\u266D", "A#" },
            {"B", "B" },
            {"C", "C" },
            {"C#", "C#" },
            {"D\u266D", "C#" },
            {"D", "D" },
            {"D#", "D#" },
            {"E\u266D", "D#" },
            {"E", "E" },
            {"F", "F" },
            {"F#", "F#" },
            {"G\u266D", "F#" },
            {"G", "G" },
            {"G#", "G#" },
            {"A\u266D", "G#" },
        };

        static int scrapeCount = 0;

    public static string GetSongEndEvent(string artist, string songName)
        {
            string key = GetKey($@"{artist} {songName}");

            if (key == "" && Config.parameters.artistlessSearchFallback)
            {
                key = GetKey(songName);
            }

            if (key == "") return Config.parameters.defaultEndEvent;

            string pitch = key.Split(" ")[0];

            if (pitchEventDict.ContainsKey(pitch)) return "event:/song_end/song_end_" + pitchEventDict[pitch];
            else return Config.parameters.defaultEndEvent;
        }

        public static string GetKey(string search)
        {
            if (scrapeCount >= Config.parameters.scrapeLimit) return "";
            string content = "";
            try
            {
                WebClient client = new WebClient();
                string query = HttpUtility.UrlEncode(search);

                scrapeCount++;
                content = client.DownloadString(@$"https://tunebat.com/Search?q={query}");
            }
            catch (Exception ex)
            {
                return "";
            }

            Match match = Regex.Match(content, ">.{1,2} (minor|major)", RegexOptions.IgnoreCase);
            if (!match.Success) return "";
            return match.Value.Replace(">", "");
        }
    }
}
