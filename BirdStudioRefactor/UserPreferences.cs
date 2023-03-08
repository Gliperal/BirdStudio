using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Input;
using Newtonsoft.Json;

namespace BirdStudioRefactor
{
    class Keybind
    {
        public int key { get; set; }
        public int modifiers { get; set; }
    }

    class PreferencesData
    {
        public string DarkMode { get; set; }
        public string ShowHelp { get; set; }
        public string AutosaveEnabled { get; set; }
        public IDictionary<string, Keybind> KeyBindings { get; set; }
    }

    class UserPreferences
    {
        private const string PREFERENCE_FILE = "./preferences.json";
        private static bool loaded = false;
        private static IDictionary<string, string> settings;
        private static IDictionary<string, Keybind> keyBindings;

        private UserPreferences() { }

        private static void loadFile()
        {
            if (loaded)
                return;
            settings = new Dictionary<string, string>();
            try
            {
                string json = File.ReadAllText(PREFERENCE_FILE);
                PreferencesData data = JsonConvert.DeserializeObject<PreferencesData>(json);
                settings["dark mode"] = data.DarkMode;
                settings["show help"] = data.ShowHelp;
                settings["autosave"] = data.AutosaveEnabled;
                keyBindings = data.KeyBindings;
            }
            catch (Exception e)
            {
                if (!(e is FileNotFoundException))
                    Util.logAndReportException(e);
                keyBindings = new Dictionary<string, Keybind>();
            }
            loaded = true;
        }

        private static void saveFile()
        {
            try
            {
                PreferencesData data = new PreferencesData
                {
                    DarkMode = settings.ContainsKey("dark mode") ? settings["dark mode"] : "false",
                    ShowHelp = settings.ContainsKey("show help") ? settings["show help"] : "false",
                    AutosaveEnabled = settings.ContainsKey("autosave") ? settings["autosave"] : "false",
                    KeyBindings = keyBindings
                };
                string json = JsonConvert.SerializeObject(data);
                File.WriteAllText(PREFERENCE_FILE, json);
            }
            catch (Exception e)
            {
                Util.logAndReportException(e);
            }
        }

        public static string get(string key, string fallback)
        {
            loadFile();
            if (settings.ContainsKey(key))
                return settings[key];
            else
                return fallback;
        }

        public static void set(string key, string value)
        {
            loadFile();
            if (settings.ContainsKey(key) && settings[key] == value)
                return;
            settings[key] = value;
            saveFile();
        }

        public static KeyGesture getKeyBinding(string action, KeyGesture fallback)
        {
            loadFile();
            try
            {
                Keybind binding = keyBindings[action];
                return new KeyGesture((Key)binding.key, (ModifierKeys)binding.modifiers);
            }
            catch (KeyNotFoundException)
            {
                return fallback;
            }
        }
    }
}
