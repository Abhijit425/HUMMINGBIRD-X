using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace HummingbirdX
{
    public enum GraphicsPreset { Performance = 0, High = 1, Ultra = 2 }
    public enum AudioCategory { Master, Nature, Birds, Water, Ambient, UI }

    /// <summary>Player preferences (PlayerPrefs backed).</summary>
    public static class Settings
    {
        public static event Action Changed;

        public static float Volume(AudioCategory c) => PlayerPrefs.GetFloat("vol_" + c, c == AudioCategory.Master ? 0.9f : 0.8f);
        public static void SetVolume(AudioCategory c, float v) { PlayerPrefs.SetFloat("vol_" + c, Mathf.Clamp01(v)); Fire(); }

        public static bool Muted { get => PlayerPrefs.GetInt("muted", 0) == 1; set { PlayerPrefs.SetInt("muted", value ? 1 : 0); Fire(); } }

        /// <summary>-1 = automatic (detected at start).</summary>
        public static int PresetChoice { get => PlayerPrefs.GetInt("preset", -1); set { PlayerPrefs.SetInt("preset", value); Fire(); } }

        public static float LookSensitivity { get => PlayerPrefs.GetFloat("look", 1f); set { PlayerPrefs.SetFloat("look", value); Fire(); } }
        public static bool InvertY { get => PlayerPrefs.GetInt("invertY", 0) == 1; set { PlayerPrefs.SetInt("invertY", value ? 1 : 0); Fire(); } }
        public static bool ShowFps { get => PlayerPrefs.GetInt("fps", 0) == 1; set { PlayerPrefs.SetInt("fps", value ? 1 : 0); Fire(); } }
        public static float DayLengthMinutes { get => PlayerPrefs.GetFloat("dayLen", 24f); set { PlayerPrefs.SetFloat("dayLen", value); Fire(); } }
        public static bool DynamicWeather { get => PlayerPrefs.GetInt("dynWeather", 1) == 1; set { PlayerPrefs.SetInt("dynWeather", value ? 1 : 0); Fire(); } }
        public static bool AdaptiveQuality { get => PlayerPrefs.GetInt("adaptive", 1) == 1; set { PlayerPrefs.SetInt("adaptive", value ? 1 : 0); Fire(); } }
        public static bool FlightSchoolDone { get => PlayerPrefs.GetInt("schoolDone", 0) == 1; set { PlayerPrefs.SetInt("schoolDone", value ? 1 : 0); } }

        static void Fire() { PlayerPrefs.Save(); Changed?.Invoke(); }
    }

    [Serializable]
    public class PhotoEntry
    {
        public string file;
        public string subject;
        public string region;
        public string time;
    }

    /// <summary>Exploration progress: discoveries, nectar, regions, photos. Stored as JSON.</summary>
    [Serializable]
    public class SaveData
    {
        public List<string> discovered = new List<string>();
        public List<string> unlockedRegions = new List<string>();
        public List<string> secrets = new List<string>();
        public List<PhotoEntry> photos = new List<PhotoEntry>();
        public float nectar;
        public int flowersVisited;
        public float distanceFlown;
        public float secondsFlown;

        static string PathFile => Path.Combine(Application.persistentDataPath, "hummingbirdx_save.json");
        public static string PhotoDir => Path.Combine(Application.persistentDataPath, "Photos");

        public static SaveData Load()
        {
            try
            {
                if (File.Exists(PathFile))
                {
                    var d = JsonUtility.FromJson<SaveData>(File.ReadAllText(PathFile));
                    if (d != null) return d;
                }
            }
            catch (Exception e) { Debug.LogWarning("[HBX] save load failed: " + e.Message); }
            return new SaveData();
        }

        public void Save()
        {
            try { File.WriteAllText(PathFile, JsonUtility.ToJson(this)); }
            catch (Exception e) { Debug.LogWarning("[HBX] save failed: " + e.Message); }
        }

        public bool IsDiscovered(string id) => discovered.Contains(id);
    }
}
