using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace HotLyric.Win32;

internal static class Fix
{
    private static readonly Lazy<string> CacheLazy = new(() =>
    {
        var dir = Path.Combine(Environment.CurrentDirectory, "cache");
        if (!Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }
        return dir;
    });
    public static string Cache => CacheLazy.Value;
    
    public static LocalSettingsImpl LocalSettings { get; } = new LocalSettingsImpl();
    public class LocalSettingsImpl
    {
        private string filePath = Path.Combine(Environment.CurrentDirectory, "HotLyric.LocalSettings.json");
        public LocalSettingsImpl()
        {
            dict = new();
            if (File.Exists(filePath))
            {
                try
                {
                    JsonConvert.PopulateObject(File.ReadAllText(filePath), dict);
                }
                catch (Exception e)
                {
                    File.WriteAllText(filePath + ".error.txt", e.ToString());
                }
            }
        }
        private void OnChanged()
        {
            File.WriteAllText(filePath, JsonConvert.SerializeObject(dict));
        }
        private Dictionary<string, string> dict;
        public bool TryGetValue(string key, out string result)
        {
            return dict.TryGetValue(key, out result);
        }
        public string this[string key]
        {
            //get { throw new System.NotImplementedException(); }
            set
            {
                dict[key] = value;
                OnChanged();
            }
        }
        public void Remove(string key)
        {
            dict.Remove(key);
            OnChanged();
        }
    }

}