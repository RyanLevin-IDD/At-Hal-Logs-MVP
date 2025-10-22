using System;
using System.Collections.Generic;
using System.IO;

public static class Config
{
    private static Dictionary<string, string> _settings;
    private static readonly string ConfigFilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "AutomationHoistinger",
        "settings.txt"
    );

    // Static constructor runs once automatically
    static Config()
    {
        LoadConfig();
    }

    private static void LoadConfig()
    {
        _settings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        // Ensure folder exists
        Directory.CreateDirectory(Path.GetDirectoryName(ConfigFilePath)!);

        if (!File.Exists(ConfigFilePath))
        {
            // Optional: create default settings
            File.WriteAllLines(ConfigFilePath, new string[]
            {
                "Email=",
                "Password=",
                "TimeFilter=",
                "ChromeProfile=",
                "SheetAPI=",
                "TimerEnabled=false"
            });
        }

        foreach (var line in File.ReadAllLines(ConfigFilePath))
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            if (line.StartsWith("#")) continue; // allow comments

            var parts = line.Split('=', 2);
            if (parts.Length != 2) continue;

            _settings[parts[0].Trim()] = parts[1].Trim();
        }
    }

    public static string Get(string key, string defaultValue = "")
    {
        if (_settings.TryGetValue(key, out var value))
            return value;
        return defaultValue;
    }

    public static int GetInt(string key, int defaultValue = 0)
    {
        if (_settings.TryGetValue(key, out var value) && int.TryParse(value, out var intVal))
            return intVal;
        return defaultValue;
    }

    public static bool GetBool(string key, bool defaultValue = false)
    {
        if (_settings.TryGetValue(key, out var value) && bool.TryParse(value, out var boolVal))
            return boolVal;
        return defaultValue;
    }

    public static void Set(string key, string value)
    {
        _settings[key] = value;

        // Save back to settings.txt
        Directory.CreateDirectory(Path.GetDirectoryName(ConfigFilePath)!);

        var lines = File.Exists(ConfigFilePath) ? new List<string>(File.ReadAllLines(ConfigFilePath)) : new List<string>();
        bool found = false;

        for (int i = 0; i < lines.Count; i++)
        {
            if (lines[i].StartsWith(key + "=", StringComparison.OrdinalIgnoreCase))
            {
                lines[i] = key + "=" + value;
                found = true;
                break;
            }
        }
        if (!found) lines.Add(key + "=" + value);

        File.WriteAllLines(ConfigFilePath, lines);
    }

    public static void SetBool(string key, bool value) => Set(key, value.ToString().ToLower());
}
