using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace VibranceGoggles
{
    public sealed class GameEntry
    {
        /// <summary>Executable name, e.g. "VALORANT-Win64-Shipping.exe".</summary>
        [XmlAttribute] public string Process { get; set; }

        /// <summary>Vibrance in percent while this game is active.</summary>
        [XmlAttribute] public int Vibrance { get; set; } = 150;

        /// <summary>Process name without extension, as reported by Windows.</summary>
        [XmlIgnore]
        public string ProcessName => Path.GetFileNameWithoutExtension(Process ?? "");
    }

    /// <summary>User settings, stored as XML in %APPDATA%\VibranceGoggles\settings.xml.</summary>
    public sealed class AppSettings
    {
        public int DesktopVibrance { get; set; } = 100;
        public bool Enabled { get; set; } = true;
        public bool OnlyWhenFocused { get; set; } = true;
        public bool StartWithWindows { get; set; }


        /// <summary>Toggle hotkey as a <see cref="Keys"/> value (key code + modifiers). 0 = none.</summary>
        public int Hotkey { get; set; } = (int)(Keys.Control | Keys.Alt | Keys.V);

        public bool TrayHintShown { get; set; }

        public List<GameEntry> Games { get; set; } = new List<GameEntry>();

        // ------------------------------------------------------------------------------------------

        public static string Folder =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "VibranceGoggles");

        private static string FilePath => Path.Combine(Folder, "settings.xml");

        public static AppSettings Load()
        {
            try
            {
                if (File.Exists(FilePath))
                {
                    using (var stream = File.OpenRead(FilePath))
                        return (AppSettings)new XmlSerializer(typeof(AppSettings)).Deserialize(stream);
                }
            }
            catch (Exception ex)
            {
                Log.Write("Could not read settings, using defaults: " + ex.Message);
            }

            var fresh = new AppSettings();
            fresh.ImportFromColorGoggles();
            fresh.Save();
            return fresh;
        }

        public void Save()
        {
            try
            {
                Directory.CreateDirectory(Folder);
                string temp = FilePath + ".tmp";
                using (var stream = File.Create(temp))
                    new XmlSerializer(typeof(AppSettings)).Serialize(stream, this);
                if (File.Exists(FilePath)) File.Replace(temp, FilePath, null);
                else File.Move(temp, FilePath);
            }
            catch (Exception ex)
            {
                Log.Write("Could not save settings: " + ex.Message);
            }
        }

        public GameEntry FindGame(string processName) =>
            Games.FirstOrDefault(g => string.Equals(g.ProcessName, processName, StringComparison.OrdinalIgnoreCase));

        /// <summary>
        /// First run only: if the user had Color-Goggles, bring over their game list and levels.
        /// Color-Goggles stores Intel offsets (0 = normal), so 35 becomes 135 %.
        /// </summary>
        private void ImportFromColorGoggles()
        {
            try
            {
                string root = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Daniele_S");
                if (!Directory.Exists(root)) return;

                string file = Directory.GetFiles(root, "user.config", SearchOption.AllDirectories)
                    .OrderByDescending(File.GetLastWriteTimeUtc).FirstOrDefault();
                if (file == null) return;

                XElement settings = XDocument.Load(file).Descendants("setting").FirstOrDefault()?.Parent;
                if (settings == null) return;

                string Value(string name) => settings.Elements("setting")
                    .FirstOrDefault(e => (string)e.Attribute("name") == name)?.Element("value")?.Value;

                int gameSat = int.TryParse(Value("GameSat"), out int g) ? g : 50;
                int winSat = int.TryParse(Value("WinSat"), out int w) ? w : 0;
                DesktopVibrance = 100 + winSat;

                XElement list = settings.Elements("setting")
                    .FirstOrDefault(e => (string)e.Attribute("name") == "Games")?.Element("value");
                foreach (string exe in list?.Descendants().Where(e => e.Name.LocalName == "string").Select(e => e.Value.Trim())
                         ?? Enumerable.Empty<string>())
                {
                    if (exe.Length > 0 && Regex.IsMatch(exe, @"^[^\\/:*?""<>|]+$") && FindGame(Path.GetFileNameWithoutExtension(exe)) == null)
                        Games.Add(new GameEntry { Process = exe, Vibrance = 100 + gameSat });
                }

                Log.Write($"Imported {Games.Count} games from Color-Goggles ({file}).");
            }
            catch (Exception ex)
            {
                Log.Write("Color-Goggles import skipped: " + ex.Message);
            }
        }
    }
}
