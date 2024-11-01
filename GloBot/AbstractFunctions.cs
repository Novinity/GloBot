using System;

namespace GloBot {
    internal class AbstractFunctions {
        public static string FormatSeconds(float dur) {
            int duration = (int)Math.Round(dur);
            int minutes = (int)Math.Floor((double)(duration / 60));
            int seconds = duration - (minutes * 60);

            return $"{minutes}:{seconds.ToString("00")}";
        }
    }
}
