using System;
using System.IO;

namespace VibranceGoggles
{
    /// <summary>Tiny append-only log in %APPDATA%\VibranceGoggles\log.txt, kept under 256 KB.</summary>
    internal static class Log
    {
        private static readonly object Gate = new object();

        public static string FilePath => Path.Combine(AppSettings.Folder, "log.txt");

        public static void Write(string message)
        {
            try
            {
                lock (Gate)
                {
                    Directory.CreateDirectory(AppSettings.Folder);
                    var info = new FileInfo(FilePath);
                    if (info.Exists && info.Length > 256 * 1024) info.Delete();
                    File.AppendAllText(FilePath, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss}  {message}{Environment.NewLine}");
                }
            }
            catch
            {
                // Logging must never break the app.
            }
        }
    }
}
