using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using GridBattle.Managers.Audio;
using UnityEditor;
using UnityEngine;

namespace GridBattle.Editor.Tools
{
    /// <summary>
    /// Fills the <see cref="AudioLibrary"/> from the sound folders by name, so replacing a sound is only a matter of
    /// files: every clip in <c>Audio/Sfx</c> named <c>&lt;ESfx&gt;</c> or <c>&lt;ESfx&gt;_&lt;anything&gt;</c> (any
    /// audio format, the case does not matter) becomes a variation of that cue, and the same goes for
    /// <c>Audio/Music</c> and <see cref="EMusicContext"/>. Runs by itself whenever a file in those folders is added,
    /// removed, renamed or overwritten, and from the menu <c>GridBattle/Audio/Relink Sound Folders</c>. Clips assigned by
    /// hand from other folders are kept. New files get the import settings of the provisional ones (mono, ADPCM).
    /// </summary>
    public sealed class AudioFolderLinker : AssetPostprocessor
    {
        public const string SfxFolder = "Assets/Application/Audio/Sfx";
        public const string MusicFolder = "Assets/Application/Audio/Music";
        private const string LibraryPath = "Assets/Application/Settings/Audio/AudioLibrary.asset";

        private static bool _scheduled;

        [MenuItem("GridBattle/Audio/Relink Sound Folders")]
        public static void RelinkFromMenu()
        {
            Debug.Log(Relink());
        }

        [MenuItem("GridBattle/Audio/Open Sound Folder")]
        public static void OpenSfxFolder()
        {
            EditorUtility.RevealInFinder(SfxFolder);
        }

        [MenuItem("GridBattle/Audio/Select Audio Library")]
        public static void SelectLibrary()
        {
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<AudioLibrary>(LibraryPath);
        }

        /// <summary>Relinks every cue and track to the files of its name. Returns a short report.</summary>
        public static string Relink()
        {
            var library = AssetDatabase.LoadAssetAtPath<AudioLibrary>(LibraryPath);
            if (library == null) return "AudioFolderLinker: no AudioLibrary at " + LibraryPath;

            if (library.EnsureEntries())
                EditorUtility.SetDirty(library);

            var report = new StringBuilder();
            var sfx = ClipsByKey<ESfx>(SfxFolder, report);
            var music = ClipsByKey<EMusicContext>(MusicFolder, report);

            var so = new SerializedObject(library);
            var linked = Link(so.FindProperty("sfxCues"), "sfx", SfxFolder, sfx);
            linked += Link(so.FindProperty("musicTracks"), "context", MusicFolder, music);
            if (so.ApplyModifiedPropertiesWithoutUndo())
            {
                EditorUtility.SetDirty(library);
                AssetDatabase.SaveAssetIfDirty(library);
            }

            var silent = library.SfxCues.Where(cue => cue != null && !cue.HasClips).Select(cue => cue.Sfx).ToList();
            report.Insert(0, $"AudioFolderLinker: {linked} clips linked from the sound folders" +
                             (silent.Count > 0 ? $"; silent effects: {string.Join(", ", silent)}" : "") + "\n");
            return report.ToString();
        }

        private void OnPreprocessAudio()
        {
            // Only the first import of a new file: settings changed by hand later are kept.
            if (!assetImporter.importSettingsMissing || !IsInFolder(assetPath, SfxFolder)) return;

            var importer = (AudioImporter)assetImporter;
            importer.forceToMono = true;
            var settings = importer.defaultSampleSettings;
            settings.loadType = AudioClipLoadType.DecompressOnLoad;
            settings.compressionFormat = AudioCompressionFormat.ADPCM;
            importer.defaultSampleSettings = settings;
        }

        private static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved,
            string[] movedFrom)
        {
            if (_scheduled) return;
            if (!imported.Concat(deleted).Concat(moved).Concat(movedFrom).Any(IsSoundPath)) return;

            _scheduled = true;
            EditorApplication.delayCall += () =>
            {
                _scheduled = false;
                if (!EditorApplication.isPlayingOrWillChangePlaymode)
                    Debug.Log(Relink());
            };
        }

        private static bool IsSoundPath(string path)
        {
            return IsInFolder(path, SfxFolder) || IsInFolder(path, MusicFolder);
        }

        private static bool IsInFolder(string path, string folder)
        {
            return path.StartsWith(folder + "/", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>The clips of <paramref name="folder"/> grouped by the enum value their name starts with.</summary>
        private static Dictionary<int, List<AudioClip>> ClipsByKey<TEnum>(string folder, StringBuilder report)
            where TEnum : struct, Enum
        {
            var result = new Dictionary<int, List<AudioClip>>();
            if (!AssetDatabase.IsValidFolder(folder)) return result;

            foreach (var guid in AssetDatabase.FindAssets("t:AudioClip", new[] { folder }).OrderBy(g => AssetDatabase.GUIDToAssetPath(g)))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var name = Path.GetFileNameWithoutExtension(path);
                var separator = name.IndexOf('_');
                var key = separator >= 0 ? name[..separator] : name;
                if (!Enum.TryParse(key, true, out TEnum value) || !Enum.IsDefined(typeof(TEnum), value))
                {
                    report.AppendLine($"  ignored {path}: the name must start with one of {string.Join(", ", Enum.GetNames(typeof(TEnum)))}");
                    continue;
                }

                var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                if (clip == null) continue;

                var index = Convert.ToInt32(value);
                if (!result.TryGetValue(index, out var clips))
                    result[index] = clips = new List<AudioClip>();
                clips.Add(clip);
            }

            return result;
        }

        /// <summary>
        /// Rewrites the clips of every entry of <paramref name="entries"/>: the ones from other folders stay, the ones
        /// from <paramref name="folder"/> are replaced by the files that carry the entry's name.
        /// </summary>
        private static int Link(SerializedProperty entries, string keyField, string folder,
            Dictionary<int, List<AudioClip>> byKey)
        {
            var linked = 0;
            for (var i = 0; i < entries.arraySize; i++)
            {
                var entry = entries.GetArrayElementAtIndex(i);
                var key = entry.FindPropertyRelative(keyField).intValue;
                var clips = entry.FindPropertyRelative("clips");

                var kept = new List<AudioClip>();
                for (var c = 0; c < clips.arraySize; c++)
                {
                    var clip = clips.GetArrayElementAtIndex(c).objectReferenceValue as AudioClip;
                    if (clip != null && !IsInFolder(AssetDatabase.GetAssetPath(clip), folder))
                        kept.Add(clip);
                }

                if (byKey.TryGetValue(key, out var fromFolder))
                {
                    kept.AddRange(fromFolder);
                    linked += fromFolder.Count;
                }

                var same = clips.arraySize == kept.Count;
                for (var c = 0; same && c < kept.Count; c++)
                    same = clips.GetArrayElementAtIndex(c).objectReferenceValue == kept[c];
                if (same) continue;

                clips.ClearArray();
                for (var c = 0; c < kept.Count; c++)
                {
                    clips.InsertArrayElementAtIndex(c);
                    clips.GetArrayElementAtIndex(c).objectReferenceValue = kept[c];
                }
            }

            return linked;
        }
    }
}
