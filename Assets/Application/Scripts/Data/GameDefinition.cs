using UnityEngine;

namespace GridBattle.Data
{
    /// <summary>
    /// Base class for every piece of game content that is referenced by saved
    /// data (characters, skills, states, talents, consumables, terrain, roles...).
    /// The <see cref="Id"/> is the asset GUID, filled in by the editor, so saves
    /// survive renames and moves. Every definition is registered in the
    /// <see cref="GameDatabase"/>.
    /// </summary>
    public abstract class GameDefinition : ScriptableObject
    {
        [SerializeField]
        [HideInInspector]
        private string id;

        /// <summary>Stable identifier (asset GUID) used by save data.</summary>
        public string Id => id;

#if UNITY_EDITOR
        /// <summary>
        /// Keeps the id in sync with the asset GUID (a duplicated asset gets a new
        /// GUID, and therefore a new id). Returns true if the id changed.
        /// </summary>
        public bool EnsureId()
        {
            var path = UnityEditor.AssetDatabase.GetAssetPath(this);
            if (string.IsNullOrEmpty(path)) return false;

            var guid = UnityEditor.AssetDatabase.AssetPathToGUID(path);
            if (id == guid) return false;

            id = guid;
            UnityEditor.EditorUtility.SetDirty(this);
            return true;
        }

        protected virtual void OnValidate()
        {
            EnsureId();
        }
#endif
    }
}
