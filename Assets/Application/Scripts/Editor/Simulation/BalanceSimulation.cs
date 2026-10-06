using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using GridBattle.Gameplay.Simulation;
using UnityEditor;
using UnityEngine;

namespace GridBattle.Editor.Simulation
{
    /// <summary>
    /// Editor side of the balance simulator (see <see cref="SimulationRunner"/> for what it does): the menu, the
    /// settings asset and the Play Mode choreography. The simulation runs in Play Mode with the open scene
    /// (SampleScene); this class enters Play Mode, starts the runner when the game is up, shows a progress bar,
    /// writes the result and leaves Play Mode again.
    /// <list type="bullet">
    /// <item>Menu <c>GridBattle/Simulation/Run Balance Simulation</c> runs the batch described by the
    /// <see cref="BalanceSimulationSettings"/> asset (<c>Settings/Simulation</c>, created on first use).</item>
    /// <item>From a script (<c>unity command run_script</c>, or <c>runplay.sh</c> in the project's tooling):
    /// <c>var o = BalanceSimulation.LoadOptions(); o.RunsPerClass = 40;
    /// BalanceSimulation.Run(o, "my_result.txt");</c> or <see cref="RunBatches"/> for several batches in one Play
    /// Mode session. The optional result file (a name in the temp folder) receives the summaries and the output
    /// folders when everything is done; its appearance tells a script that the simulation ended.</item>
    /// </list>
    /// The request survives a domain reload while Play Mode starts (it is kept in <see cref="SessionState"/>).
    /// </summary>
    [InitializeOnLoad]
    public static class BalanceSimulation
    {
        public const string SettingsPath = "Assets/Application/Settings/Simulation/BalanceSimulationSettings.asset";

        private const string PendingKey = "GridBattle.Simulation.Pending";
        private const string ScenePath = "Assets/Scenes/SampleScene.unity";

        [Serializable]
        private sealed class Request
        {
            public List<string> batches = new();
            public string resultFile;
            public bool exitPlayMode;
            public bool enteredByTool;
        }

        private static bool _progressShown;

        static BalanceSimulation()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorApplication.update += UpdateProgress;
        }

        // ------------------------------------------------------------------------------------ menu

        [MenuItem("GridBattle/Simulation/Run Balance Simulation")]
        private static void RunFromMenu()
        {
            var settings = GetOrCreateSettings();
            Run(settings.Options.Clone());
        }

        [MenuItem("GridBattle/Simulation/Select Settings Asset")]
        private static void SelectSettings()
        {
            var settings = GetOrCreateSettings();
            Selection.activeObject = settings;
            EditorGUIUtility.PingObject(settings);
        }

        [MenuItem("GridBattle/Simulation/Open Output Folder")]
        private static void OpenOutputFolder()
        {
            var folder = Path.Combine(Application.persistentDataPath, GetOrCreateSettings().Options.OutputFolder);
            Directory.CreateDirectory(folder);
            EditorUtility.RevealInFinder(folder);
        }

        // ------------------------------------------------------------------------------------ settings

        /// <summary>The settings asset (created with the defaults on first use).</summary>
        public static BalanceSimulationSettings GetOrCreateSettings()
        {
            var settings = AssetDatabase.LoadAssetAtPath<BalanceSimulationSettings>(SettingsPath);
            if (settings != null) return settings;

            Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath) ?? "Assets");
            settings = ScriptableObject.CreateInstance<BalanceSimulationSettings>();
            AssetDatabase.CreateAsset(settings, SettingsPath);
            AssetDatabase.SaveAssets();
            return settings;
        }

        /// <summary>A copy of the asset's options: tweak it freely, the asset is not touched.</summary>
        public static SimulationOptions LoadOptions() => GetOrCreateSettings().Options.Clone();

        // ------------------------------------------------------------------------------------ running

        /// <summary>
        /// Runs one batch (entering Play Mode if needed). <paramref name="resultFile"/>, when given, is a file name
        /// in the temp folder written when the simulation ended. <paramref name="exitPlayMode"/> leaves Play Mode
        /// afterwards when this tool entered it (a caller that was already playing keeps its Play Mode).
        /// </summary>
        public static void Run(SimulationOptions options, string resultFile = null, bool exitPlayMode = true)
        {
            RunBatches(new[] { options }, resultFile, exitPlayMode);
        }

        /// <summary>Runs several batches, one after the other, in one Play Mode session.</summary>
        public static void RunBatches(IEnumerable<SimulationOptions> batches, string resultFile = null,
            bool exitPlayMode = true)
        {
            var request = new Request { resultFile = resultFile, exitPlayMode = exitPlayMode };
            foreach (var batch in batches)
                request.batches.Add(JsonUtility.ToJson(batch));
            if (request.batches.Count == 0)
            {
                Debug.LogWarning("Balance simulation: there is nothing to run.");
                return;
            }

            if (SimulationRunner.Current != null && !SimulationRunner.Current.IsFinished)
            {
                Debug.LogWarning("Balance simulation: one is already running.");
                return;
            }

            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().path != ScenePath && !EditorApplication.isPlaying)
                Debug.LogWarning($"Balance simulation: the open scene is not {ScenePath}; the runner needs the game's manager objects.");

            if (EditorApplication.isPlaying && !EditorApplication.isPaused)
            {
                Start(request);
                return;
            }

            request.enteredByTool = true;
            SessionState.SetString(PendingKey, JsonUtility.ToJson(request));
            EditorApplication.EnterPlaymode();
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange change)
        {
            switch (change)
            {
                case PlayModeStateChange.EnteredPlayMode:
                    var json = SessionState.GetString(PendingKey, string.Empty);
                    if (string.IsNullOrEmpty(json)) return;

                    SessionState.EraseString(PendingKey);
                    var request = JsonUtility.FromJson<Request>(json);
                    EditorApplication.delayCall += () => Start(request);
                    break;
                case PlayModeStateChange.EnteredEditMode:
                    SessionState.EraseString(PendingKey);
                    ClearProgress();
                    break;
            }
        }

        private static void Start(Request request)
        {
            if (!EditorApplication.isPlaying) return;

            var batches = new List<SimulationOptions>();
            foreach (var json in request.batches)
                batches.Add(JsonUtility.FromJson<SimulationOptions>(json));

            try
            {
                SimulationRunner.Begin(batches, results => OnFinished(request, results));
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                if (request.exitPlayMode && request.enteredByTool)
                    EditorApplication.ExitPlaymode();
            }
        }

        private static void OnFinished(Request request, IReadOnlyList<SimulationBatchResult> results)
        {
            ClearProgress();
            if (!string.IsNullOrEmpty(request.resultFile))
                WriteResultFile(request.resultFile, results);

            if (request.exitPlayMode && request.enteredByTool)
                EditorApplication.delayCall += EditorApplication.ExitPlaymode;
        }

        private static void WriteResultFile(string name, IReadOnlyList<SimulationBatchResult> results)
        {
            var text = new StringBuilder();
            var comparison = SimulationRunner.Current != null ? SimulationRunner.Current.ComparisonText : string.Empty;
            if (comparison.Length > 0)
                text.AppendLine(comparison);
            foreach (var result in results)
            {
                text.AppendLine($"=== batch '{result.Options.Label}' [{result.Status}] output: {result.OutputFolder}");
                text.AppendLine(result.SummaryText);
            }

            text.AppendLine("DONE");

            var path = Path.Combine(Path.GetTempPath(), name);
            var temp = path + ".part";
            File.WriteAllText(temp, text.ToString());
            if (File.Exists(path))
                File.Delete(path);
            File.Move(temp, path);
        }

        // ------------------------------------------------------------------------------------ progress bar

        private static void UpdateProgress()
        {
            var runner = SimulationRunner.Current;
            if (runner == null || runner.IsFinished || !EditorApplication.isPlaying)
            {
                if (_progressShown) ClearProgress();
                return;
            }

            _progressShown = true;
            if (EditorUtility.DisplayCancelableProgressBar("Balance simulation", runner.Progress, runner.Fraction))
                runner.Cancel();
        }

        private static void ClearProgress()
        {
            if (!_progressShown) return;

            _progressShown = false;
            EditorUtility.ClearProgressBar();
        }
    }

    /// <summary>Inspector of <see cref="BalanceSimulationSettings"/>: the fields and a button that runs the batch.</summary>
    [CustomEditor(typeof(BalanceSimulationSettings))]
    public sealed class BalanceSimulationSettingsEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            GUILayout.Space(10);
            using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode))
            {
                if (GUILayout.Button("Run balance simulation"))
                    BalanceSimulation.Run(((BalanceSimulationSettings)target).Options.Clone());
            }

            if (GUILayout.Button("Open output folder"))
            {
                var folder = Path.Combine(Application.persistentDataPath,
                    ((BalanceSimulationSettings)target).Options.OutputFolder);
                Directory.CreateDirectory(folder);
                EditorUtility.RevealInFinder(folder);
            }
        }
    }
}
