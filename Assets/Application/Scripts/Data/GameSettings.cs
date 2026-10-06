using System;
using System.Collections.Generic;
using UnityEngine;

namespace GridBattle.Data
{
    /// <summary>
    /// Marker for the game's configuration assets (combat, run, map, talents...).
    /// Every asset implementing it is registered automatically in
    /// <see cref="GameSettings"/> by the editor.
    /// </summary>
    public interface IGameSettings
    {
    }

    /// <summary>
    /// Root of the game configuration: one asset per system (CombatSettings,
    /// RunSettings, MapGenerationSettings...), all editable in the Inspector.
    /// Lives in a Resources folder. Systems read their settings with
    /// <see cref="Get{T}"/>; a missing asset falls back to the type's default
    /// values (with a warning), so the game never breaks for lack of config.
    /// </summary>
    public sealed class GameSettings : ScriptableObject
    {
        public const string ResourcePath = "GameSettings";

        [SerializeField]
        [Tooltip("Configuration assets. Filled automatically by the editor with every IGameSettings asset.")]
        private List<ScriptableObject> settings = new();

        private static GameSettings _instance;
        private static readonly Dictionary<Type, ScriptableObject> Cache = new();

        public static GameSettings Instance
        {
            get
            {
                if (_instance == null)
                    _instance = Resources.Load<GameSettings>(ResourcePath);
                return _instance;
            }
        }

        public IReadOnlyList<ScriptableObject> All => settings;

        /// <summary>
        /// The settings asset of type <typeparamref name="T"/>, or an in-memory
        /// instance with default values if none is registered.
        /// </summary>
        public static T Get<T>() where T : ScriptableObject, IGameSettings
        {
            if (Cache.TryGetValue(typeof(T), out var cached) && cached != null)
                return (T)cached;

            T found = null;
            var root = Instance;
            if (root != null)
            {
                foreach (var asset in root.settings)
                {
                    if (asset is T typed)
                    {
                        found = typed;
                        break;
                    }
                }
            }

            if (found == null)
            {
                Debug.LogWarning($"No {typeof(T).Name} asset registered in GameSettings; using default values.");
                found = CreateInstance<T>();
                found.name = $"{typeof(T).Name} (defaults)";
            }

            Cache[typeof(T)] = found;
            return found;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetCache()
        {
            Cache.Clear();
        }

#if UNITY_EDITOR
        /// <summary>Editor only: replaces the registered settings assets.</summary>
        public void SetSettings(List<ScriptableObject> all)
        {
            settings = all;
            Cache.Clear();
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }
}
