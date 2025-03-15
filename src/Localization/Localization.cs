using System;
using System.Collections.Generic;
using System.Text;
using I2.Loc;
using Newtonsoft.Json;
using System.IO;
using System.Reflection;
using System.Linq;

namespace BoomerangFoo.Localization
{
    public class Localization
    {
        public static void LoadLocalizationFromJSON(string jsonPath)
        {
            string jsonText = File.ReadAllText(jsonPath);
            var translations = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, Dictionary<string, string>>>>(jsonText);

            var source = LocalizationManager.Sources[0]; // Main source
            if (source == null) return;

            foreach (var entry in translations)
            {
                string key = entry.Key;
                var languageMap = entry.Value["translations"];

                if (!source.ContainsTerm(key))
                {
                    var termData = source.AddTerm(key);
                    foreach (var langEntry in languageMap)
                    {
                        int langIndex = source.GetLanguageIndex(langEntry.Key);
                        if (langIndex >= 0)
                            termData.Languages[langIndex] = langEntry.Value;
                    }
                }
            }
        }
    }

    public class LocalizationEntry
    {
        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("contributions")]
        public string Contributions { get; set; }

        [JsonProperty("translations")]
        public Dictionary<string, string> Translations { get; set; } = new();
    }

    public class LocalizationData
    {
        [JsonProperty]
        public Dictionary<string, LocalizationEntry> Entries { get; set; } = new();

        public static string EmbeddedPath = "BoomerangFoo.Localization.localization.json";

        public static LocalizationData LoadFromFile(string filePath)
        {
            if (!File.Exists(filePath))
            {
                Console.WriteLine($"Localization file {filePath} not found.");
                return new LocalizationData();
            }

            string jsonText = File.ReadAllText(filePath);
            var localizationData = JsonConvert.DeserializeObject<Dictionary<string, LocalizationEntry>>(jsonText);
            return new LocalizationData { Entries = localizationData };
        }

        public static LocalizationData LoadEmbeddedResource(string resourceName)
        {
            var assembly = Assembly.GetExecutingAssembly();
            string resourcePath = assembly.GetManifestResourceNames()
                .FirstOrDefault(name => name.EndsWith(resourceName, StringComparison.OrdinalIgnoreCase));

            if (resourcePath == null)
                throw new FileNotFoundException($"Embedded resource {resourceName} not found.");

            using (Stream stream = assembly.GetManifestResourceStream(resourcePath))
            using (StreamReader reader = new StreamReader(stream))
            {
                string jsonText = reader.ReadToEnd();
                var localizationData = JsonConvert.DeserializeObject<Dictionary<string, LocalizationEntry>>(jsonText);
                return new LocalizationData { Entries = localizationData };
            }
        }

        public static LocalizationData LoadLocalizations()
        {
            // Get the general plugins directory
            string pluginsDir = BepInEx.Paths.PluginPath;

            // Define your plugin's specific directory
            string pluginDir = Path.Combine(pluginsDir, "BoomerangFoo");

            // Define the expected file path
            string filePath = Path.Combine(pluginDir, "localization.json");

            LocalizationData localizationData = null;
            // Check if the directory exists
            if (Directory.Exists(pluginDir) && File.Exists(filePath))
            {
                try
                {
                    localizationData = LoadFromFile(filePath);
                } catch {
                    BoomerangFoo.Logger.LogWarning($"There was an error parsing localizations at {filePath}");
                }
            }
            if (localizationData == null || localizationData.Entries.Count == 0)
            {
                localizationData = LoadEmbeddedResource(EmbeddedPath);

                if (localizationData == null)
                {
                    localizationData = new LocalizationData();
                    BoomerangFoo.Logger.LogInfo($"No custom localizations found.");
                } else
                {
                    BoomerangFoo.Logger.LogInfo($"Loaded embedded localizations.");
                }
            } else
            {
                BoomerangFoo.Logger.LogInfo($"Loaded localizations from {filePath}");
            }
            
            return localizationData;
        }

        public static void RegisterCustomLocalizations(LocalizationData localizationData)
        {
            if ( LocalizationManager.Sources == null || LocalizationManager.Sources.Count == 0)
            {
                throw new Exception("LocalizationManager was not initialized yet with a source.");
            }
            var source = LocalizationManager.Sources[0]; // Main source
            if (source == null) return;

            foreach (var entry in localizationData.Entries)
            {
                string key = entry.Key;
                var languageMap = entry.Value.Translations;
                if (languageMap == null) continue;

                if (!source.ContainsTerm(key))
                {
                    var termData = source.AddTerm(key);
                    foreach (string language in LocalizationManager.GetAllLanguages()) 
                    {
                        string translation = null;
                        if (languageMap.ContainsKey(language))
                        {
                            translation = languageMap[language];
                        } else
                        {
                            translation = languageMap["English"];
                        }
                        int langIndex = source.GetLanguageIndex(language);
                        if (langIndex >= 0)
                            termData.Languages[langIndex] = translation;
                    }
                }
            }
        }
    }
}
