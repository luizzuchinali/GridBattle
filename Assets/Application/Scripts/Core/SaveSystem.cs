using System;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;

namespace GridBattle.Core
{
    /// <summary>
    /// JSON files in Application.persistentDataPath. Writes are atomic (temp
    /// file + replace) so a save interrupted by the app closing never corrupts
    /// the previous one.
    /// </summary>
    public static class SaveSystem
    {
        private static readonly JsonSerializerSettings Settings = new()
        {
            Formatting = Formatting.Indented,
            NullValueHandling = NullValueHandling.Include,
            ObjectCreationHandling = ObjectCreationHandling.Replace,
        };

        public static string GetPath(string fileName) => Path.Combine(Application.persistentDataPath, fileName);

        public static bool Exists(string fileName) => File.Exists(GetPath(fileName));

        public static void Save<T>(string fileName, T data)
        {
            var path = GetPath(fileName);
            var temp = path + ".tmp";
            try
            {
                File.WriteAllText(temp, JsonConvert.SerializeObject(data, Settings));
                if (File.Exists(path))
                    File.Replace(temp, path, null);
                else
                    File.Move(temp, path);
            }
            catch (Exception exception)
            {
                Debug.LogError($"Failed to save '{fileName}': {exception}");
            }
        }

        public static bool TryLoad<T>(string fileName, out T data)
        {
            data = default;
            var path = GetPath(fileName);
            if (!File.Exists(path)) return false;

            try
            {
                data = JsonConvert.DeserializeObject<T>(File.ReadAllText(path), Settings);
                return data != null;
            }
            catch (Exception exception)
            {
                Debug.LogError($"Failed to load '{fileName}': {exception}");
                return false;
            }
        }

        public static void Delete(string fileName)
        {
            var path = GetPath(fileName);
            if (File.Exists(path))
                File.Delete(path);
        }

        /// <summary>Deep copy through JSON (used to snapshot save data).</summary>
        public static T Clone<T>(T data) =>
            JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(data, Settings), Settings);
    }
}
