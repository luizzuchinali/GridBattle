using JetBrains.Annotations;
using UnityEngine;

namespace GridBattle.Gameplay.Entities
{
    /// <summary>
    /// Single entry point for creating characters: instantiates the prefab template
    /// referenced by the config and applies the config to it. Returns null (and logs
    /// an error pointing at the asset) if the config has no prefab template.
    /// </summary>
    public static class CharacterFactory
    {
        [CanBeNull]
        public static PlayerCharacter Spawn(PlayerCharacterConfig config) => Spawn(config, config.Prefab);

        [CanBeNull]
        public static Enemy Spawn(EnemyConfig config) => Spawn(config, config.Prefab);

        private static TCharacter Spawn<TCharacter>(CharacterConfig config, TCharacter prefab)
            where TCharacter : Character
        {
            if (prefab == null)
            {
                Debug.LogError($"Config '{config.name}' has no prefab template assigned.", config);
                return null;
            }

            var instance = InstantiatePrefabLinked(prefab);
            instance.Initialize(config);
            RecordEditModeChanges(instance);
            return instance;
        }

        /// <summary>
        /// In the editor (outside play mode), instantiates linked to the prefab so that
        /// clones reflect changes to the template.
        /// </summary>
        private static T InstantiatePrefabLinked<T>(T prefab) where T : Component
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
                return (T)UnityEditor.PrefabUtility.InstantiatePrefab(prefab);
#endif
            return Object.Instantiate(prefab);
        }

        /// <summary>
        /// Outside play mode, script changes to prefab instances must be recorded as
        /// overrides to persist in the scene.
        /// </summary>
        private static void RecordEditModeChanges(Component instance)
        {
#if UNITY_EDITOR
            if (Application.isPlaying) return;

            UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(instance.gameObject);
            foreach (var component in instance.GetComponents<Component>())
                UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(component);
#endif
        }
    }
}
