using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;

namespace GridBattle.Data
{
    /// <summary>
    /// Registry of every <see cref="GameDefinition"/> in the project, used to
    /// resolve the ids stored in save data and to list content (glossary, pools).
    /// Lives in a Resources folder and is rebuilt automatically by the editor
    /// whenever a definition is created, moved or deleted
    /// (menu: GridBattle/Rebuild Game Database).
    /// </summary>
    public sealed class GameDatabase : ScriptableObject
    {
        public const string ResourcePath = "GameDatabase";

        [SerializeField]
        private List<GameDefinition> definitions = new();

        private Dictionary<string, GameDefinition> _byId;
        private static GameDatabase _instance;

        public static GameDatabase Instance
        {
            get
            {
                if (_instance == null)
                    _instance = Resources.Load<GameDatabase>(ResourcePath);
                return _instance;
            }
        }

        public IReadOnlyList<GameDefinition> Definitions => definitions;

        /// <summary>Resolves an id from save data. Returns null for unknown ids.</summary>
        [CanBeNull]
        public T Get<T>(string id) where T : GameDefinition
        {
            if (string.IsNullOrEmpty(id)) return null;

            if (_byId == null)
            {
                _byId = new Dictionary<string, GameDefinition>();
                foreach (var definition in definitions)
                {
                    if (definition != null && !string.IsNullOrEmpty(definition.Id))
                        _byId[definition.Id] = definition;
                }
            }

            return _byId.TryGetValue(id, out var found) ? found as T : null;
        }

        /// <summary>Every definition of type <typeparamref name="T"/>, in database order.</summary>
        public List<T> GetAll<T>() where T : GameDefinition
        {
            var result = new List<T>();
            foreach (var definition in definitions)
            {
                if (definition is T typed)
                    result.Add(typed);
            }

            return result;
        }

#if UNITY_EDITOR
        /// <summary>Editor only: replaces the registered definitions.</summary>
        public void SetDefinitions(List<GameDefinition> all)
        {
            definitions = all;
            _byId = null;
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }
}
