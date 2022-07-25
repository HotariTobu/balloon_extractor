using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dataset_generator
{
    internal static class Logger
    {
        private static readonly string LogPath = "log.txt";

        public static async Task Log(this string text)
        {
            string content = $"[{DateTime.Now.ToString("G")}] {text}{Environment.NewLine}";
            await File.AppendAllTextAsync(LogPath, content, Encoding.UTF8);
        }

        public static async Task Log(this IEnumerable<string> lines)
        {
            await string.Join(Environment.NewLine, lines).Log();
        }

        public static async void Log(this Exception exception)
        {
            await new string[]
            {
                exception.Message,
                exception.StackTrace ?? "",
            }.Log();
        }
    }
}
