using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using GridBattle.Core;
using GridBattle.Core.Randomness;
using GridBattle.Gameplay.Entities;
using GridBattle.Gameplay.Meta;
using GridBattle.Gameplay.Run;
using GridBattle.Gameplay.Talents;
using GridBattle.Gameplay.Turns;
using GridBattle.Managers;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace GridBattle.Gameplay.Simulation
{
    /// <summary>
    /// Plays complete runs by itself, as fast as possible and with the game's real rules (<see cref="RunManager"/>,
    /// map and battle generation, <c>TurnManager</c>, <c>CombatResolver</c>, AI, skills, terrain, consumables, XP and
    /// talents), and records what happened (<see cref="SimulationCollector"/>, <see cref="SimulationReport"/>).
    /// It is a Play Mode component created by <see cref="Begin"/> (the editor tool and scripts call it; see
    /// <c>BalanceSimulation</c> in the editor assembly).
    /// <para>
    /// How it works: while <see cref="SimMode"/> is on, the game skips everything that only exists to be seen
    /// or heard, so a whole turn (the player's action and every enemy) resolves inside one call. Every frame the
    /// runner looks at the game state and does what a person would: the map bot picks a node, the talent bot
    /// answers offers, the battle bot plays the player's turn. It repeats until the frame's time budget is used or
    /// something has to wait for the next frame (an enemy turn held by a talent offer, the board being cleared).
    /// A run that makes no progress for a number of frames, passes the turn limit, takes too long or leaves the bot
    /// without a legal action is aborted and recorded as such.
    /// </para>
    /// <para>
    /// Safe by design: nothing on disk is changed (the profile is a temporary file, the run is never saved, the
    /// run seed is a runtime override, the metrics are off) and every setting touched in memory (time scale, frame
    /// rate, profile file) is restored when the simulation ends or the component is destroyed.
    /// </para>
    /// </summary>
    public sealed class SimulationRunner : MonoBehaviour
    {
        private const string TempProfileFile = "sim_profile.json";
        private const string BackupSuffix = ".simbak";
        private static readonly string[] ProtectedFiles = { RunManager.SaveFileName, MetricsRecorder.DefaultFileName };

        private readonly List<SimulationBatchResult> _results = new();
        private readonly List<(PlayerCharacterConfig Class, int RunIndex)> _jobs = new();
        private readonly Stopwatch _batchClock = new();
        private readonly Stopwatch _runClock = new();

        private IReadOnlyList<SimulationOptions> _batches;
        private Action<IReadOnlyList<SimulationBatchResult>> _onFinished;
        private int _batchIndex = -1;
        private int _jobIndex;
        private SimulationOptions _options;
        private SimulationBatchResult _batch;
        private SimulationCollector _collector;
        private BattleBot _battleBot;
        private MapPolicy _mapPolicy;
        private TalentPolicy _talentPolicy;

        private RunManager _runs;
        private GridController _grid;
        private TurnManager _turns;
        private BattleController _battleController;

        private bool _runActive;
        private int _runStartFrame;
        private int _failedActions;
        private int _abortAttempts;
        private int _signature;
        private int _lastProgressFrame;
        private int _consoleErrors;
        private bool _environmentPrepared;
        private bool _finished;
        private bool _cancelRequested;

        // Settings changed in memory and restored at the end.
        private float _savedTimeScale;
        private int _savedTargetFrameRate;
        private int _savedVSync;
        private bool _savedRunInBackground;

        /// <summary>The runner in progress, or null.</summary>
        public static SimulationRunner Current { get; private set; }

        /// <summary>
        /// Optional listener of the battle bot's decisions (one line each, plus a line per turn with the board),
        /// for debugging the bot from a script. Reset when Play Mode starts.
        /// </summary>
        public static Action<string> TraceHandler { get; set; }

        /// <summary>The batch results are ready (or the simulation was cancelled).</summary>
        public bool IsFinished => _finished;

        /// <summary>Finished batches so far.</summary>
        public IReadOnlyList<SimulationBatchResult> Results => _results;

        /// <summary>Table comparing the batches of the session (empty with a single batch).</summary>
        public string ComparisonText { get; private set; } = string.Empty;

        /// <summary>Human-readable progress ("batch 1/2 Warrior run 12/30").</summary>
        public string Progress { get; private set; } = "starting";

        /// <summary>Fraction of all the runs of all the batches already started, 0 to 1.</summary>
        public float Fraction
        {
            get
            {
                if (_batches == null || _batchIndex < 0) return 0f;

                var inBatch = _jobs.Count == 0 ? 1f : _jobIndex / (float)_jobs.Count;
                return Mathf.Clamp01((_batchIndex + inBatch) / _batches.Count);
            }
        }

        /// <summary>
        /// Starts the simulation of <paramref name="batches"/> one after the other (Play Mode only).
        /// <paramref name="onFinished"/> receives the results when the last batch ends, is cancelled or fails.
        /// </summary>
        public static SimulationRunner Begin(IReadOnlyList<SimulationOptions> batches,
            Action<IReadOnlyList<SimulationBatchResult>> onFinished = null)
        {
            if (!Application.isPlaying)
                throw new InvalidOperationException("The balance simulation only runs in Play Mode.");
            if (batches == null || batches.Count == 0)
                throw new ArgumentException("There is nothing to simulate.", nameof(batches));
            if (Current != null && !Current.IsFinished)
                throw new InvalidOperationException("A balance simulation is already running.");

            var host = new GameObject("BalanceSimulation");
            var runner = host.AddComponent<SimulationRunner>();
            runner._batches = batches;
            runner._onFinished = onFinished;
            Current = runner;
            return runner;
        }

        /// <summary>Stops the simulation after the current frame; the finished batches are still reported.</summary>
        public void Cancel()
        {
            _cancelRequested = true;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Current = null;
            TraceHandler = null;
        }

        // ------------------------------------------------------------------------------------ lifecycle

        private void Start()
        {
            if (!Prepare()) return;

            StartBatch(0);
        }

        private void OnDestroy()
        {
            Restore();
            if (Current == this)
                Current = null;
        }

        private void Update()
        {
            if (_finished) return;

            try
            {
                if (_cancelRequested)
                {
                    _batch.Status = "cancelled";
                    EndBatch(true);
                    return;
                }

                var clock = Stopwatch.StartNew();
                do
                {
                    if (!Step() || _finished) break;
                }
                while (clock.ElapsedMilliseconds < _options.ActionBudgetMs);

                if (!_finished)
                    CheckProgress();
            }
            catch (Exception exception)
            {
                OnException(exception);
            }
        }

        private void OnException(Exception exception)
        {
            Debug.LogException(exception);
            _batch.Exceptions++;
            if (_batch.Exceptions > 5)
            {
                _batch.Status = "too many exceptions";
                EndBatch(true);
                return;
            }

            if (_runActive)
                AbortRun("exception");
        }

        // ------------------------------------------------------------------------------------ environment

        private bool Prepare()
        {
            var manager = GameStateManager.Instance;
            _runs = RunManager.Instance;
            _grid = FindAnyObjectByType<GridController>();
            if (manager == null || _runs == null || _grid == null ||
                !_grid.TryGetComponent(out _turns) || !_grid.TryGetComponent(out _battleController))
            {
                Debug.LogError("Balance simulation: the scene needs the GameStateManager, RunManager, GridController, " +
                               "TurnManager and BattleController (open SampleScene).");
                _finished = true;
                _onFinished?.Invoke(_results);
                Destroy(gameObject);
                return false;
            }

            Application.logMessageReceived += OnLog;
            SimMode.Begin();
            _environmentPrepared = true;

            // Files the game could touch by itself: put back whatever was there when the simulation ends.
            foreach (var file in ProtectedFiles)
            {
                var path = SaveSystem.GetPath(file);
                if (File.Exists(path))
                    File.Copy(path, path + BackupSuffix, true);
            }

            // Temporary profile with every class unlocked: the real one is never touched.
            ProfileService.UseFile(TempProfileFile);
            var state = ProfileService.State;
            foreach (var config in manager.PlayableCharacters)
            {
                if (config != null && config.BattlesToUnlock > 0 && !state.UnlockedClassIds.Contains(config.Id))
                    state.UnlockedClassIds.Add(config.Id);
            }

            ProfileService.Save();

            _savedTimeScale = Time.timeScale;
            _savedTargetFrameRate = Application.targetFrameRate;
            _savedVSync = QualitySettings.vSyncCount;
            _savedRunInBackground = Application.runInBackground;
            Application.runInBackground = true;
            TurnBlockers.Clear();
            return true;
        }

        private void ApplySpeedSettings()
        {
            Time.timeScale = _options.TimeScale;
            if (_options.UncapFrameRate)
            {
                QualitySettings.vSyncCount = 0;
                Application.targetFrameRate = 1000;
            }
        }

        private void Restore()
        {
            if (!_environmentPrepared) return;

            _environmentPrepared = false;
            Application.logMessageReceived -= OnLog;
            _collector?.Dispose();
            _collector = null;
            DisposeBots();
            TurnBlockers.Clear();

            Time.timeScale = _savedTimeScale;
            Application.targetFrameRate = _savedTargetFrameRate;
            QualitySettings.vSyncCount = _savedVSync;
            Application.runInBackground = _savedRunInBackground;

            ProfileService.UseFile(null);
            SaveSystem.Delete(TempProfileFile);
            SaveSystem.Delete(TempProfileFile + ".tmp");

            foreach (var file in ProtectedFiles)
            {
                var path = SaveSystem.GetPath(file);
                var backup = path + BackupSuffix;
                if (File.Exists(backup))
                {
                    File.Copy(backup, path, true);
                    File.Delete(backup);
                }
                else if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }

            SimMode.End();
        }

        private void OnLog(string condition, string stackTrace, LogType type)
        {
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
                _consoleErrors++;
        }

        // ------------------------------------------------------------------------------------ batches

        private void StartBatch(int index)
        {
            _batchIndex = index;
            _options = _batches[index];
            _batch = new SimulationBatchResult { Options = _options };
            _jobs.Clear();
            _jobIndex = 0;
            _runActive = false;
            _consoleErrors = 0;
            ApplySpeedSettings();

            var manager = GameStateManager.Instance;
            var classes = new List<PlayerCharacterConfig>();
            if (_options.Classes.Count == 0)
            {
                foreach (var config in manager.PlayableCharacters)
                {
                    if (config != null)
                        classes.Add(config);
                }
            }
            else
            {
                foreach (var character in _options.Classes)
                {
                    var config = manager.FindPlayableCharacter(character);
                    if (config != null && !classes.Contains(config))
                        classes.Add(config);
                    else if (config == null)
                        Debug.LogWarning($"Balance simulation: no playable character for class {character}; skipped.");
                }
            }

            foreach (var config in classes)
            {
                for (var run = 0; run < _options.RunsPerClass; run++)
                    _jobs.Add((config, run));
            }

            _collector?.Dispose();
            _collector = new SimulationCollector();
            _batchClock.Restart();
            if (_jobs.Count == 0)
            {
                _batch.Status = "no classes to simulate";
                EndBatch(false);
            }
        }

        private void EndBatch(bool stop)
        {
            _batch.Runs.AddRange(_collector.Runs);
            _batch.Battles.AddRange(_collector.Battles);
            _batch.TotalMilliseconds = _batchClock.Elapsed.TotalMilliseconds;
            _batch.ConsoleErrors = _consoleErrors;
            SimulationReport.Write(_batch);
            if (_options.LogSummary)
                Debug.Log(_batch.SummaryText);
            _results.Add(_batch);

            _runActive = false;
            DisposeBots();
            _collector.Dispose();
            _collector = null;

            if (!stop && _batchIndex + 1 < _batches.Count)
            {
                StartBatch(_batchIndex + 1);
                return;
            }

            Finish();
        }

        private void Finish()
        {
            _finished = true;
            ComparisonText = SimulationReport.WriteComparison(_results);
            if (ComparisonText.Length > 0 && _options.LogSummary)
                Debug.Log(ComparisonText);
            Restore();
            _onFinished?.Invoke(_results);
            Destroy(gameObject);
        }

        // ------------------------------------------------------------------------------------ runs

        private void StartNextRun()
        {
            if (_jobIndex >= _jobs.Count)
            {
                EndBatch(false);
                return;
            }

            var job = _jobs[_jobIndex++];
            var seed = _options.StartSeed + job.RunIndex;
            var character = (long)job.Class.CharacterClass;
            var botSeed = (ulong)Math.Max(1L, _options.BotSeed);

            DisposeBots();
            _battleBot = new BattleBot(_options.BattleBot, Rng.FromKeys(botSeed, seed, character, 1))
            {
                Trace = TraceHandler,
            };
            _mapPolicy = new MapPolicy(_options.Map, Rng.FromKeys(botSeed, seed, character, 2));
            _talentPolicy = new TalentPolicy(_options.Talents, Rng.FromKeys(botSeed, seed, character, 3));

            Progress = $"batch {_batchIndex + 1}/{_batches.Count} '{_options.Label}' {job.Class.CharacterClass} " +
                       $"run {job.RunIndex + 1}/{_options.RunsPerClass} (job {_jobIndex}/{_jobs.Count})";
            _collector.BeginRun(job.Class.CharacterClass.ToString(), job.RunIndex, seed);
            TurnBlockers.Clear();
            SimMode.SeedOverride = (ulong)Math.Max(1L, seed);

            if (!_runs.StartNewRun(job.Class))
            {
                _batch.Status = $"StartNewRun refused {job.Class.name}";
                EndBatch(true);
                return;
            }

            _runActive = true;
            _failedActions = 0;
            _abortAttempts = 0;
            _runStartFrame = Time.frameCount;
            _runClock.Restart();
            _signature = int.MinValue;
            _lastProgressFrame = Time.frameCount;
        }

        /// <summary>
        /// One decision. Returns true when something was done that may allow another one right away, false when the
        /// next frame has to come first.
        /// </summary>
        private bool Step()
        {
            if (!_runActive)
            {
                StartNextRun();
                return true;
            }

            // Records of a battle or a run that just ended close here, after every handler of that moment ran.
            _collector.Flush();

            // The run ended (the collector closes its record on RunEndedEvent): leave a frame for the board clean-up.
            if (_collector.CurrentRun == null)
            {
                _runActive = false;
                var records = _collector.Runs;
                if (records.Count > 0)
                    records[records.Count - 1].Frames = Time.frameCount - _runStartFrame;
                return false;
            }

            if (_runClock.Elapsed.TotalSeconds > _options.MaxSecondsPerRun)
            {
                AbortRun("run_time_limit");
                return true;
            }

            if (TalentService.HasOffer)
                return _talentPolicy.Answer();

            switch (_runs.Phase)
            {
                case ERunPhase.Map:
                    return MapStep();
                case ERunPhase.Battle:
                    return BattleStep();
                case ERunPhase.ConsumableOffer:
                    return _mapPolicy.AnswerConsumableOffer(_runs);
                default:
                    // A talent node waits for its offer, a finished run for its event.
                    return false;
            }
        }

        private bool MapStep()
        {
            var available = _runs.GetAvailableNodes();
            var node = _mapPolicy.ChooseNode(available, _runs.PlayerHp, _runs.PlayerMaxHp);
            if (node == null)
            {
                AbortRun("no_nodes");
                return true;
            }

            // Diagnostic option: the battle starts at full HP.
            if (_options.RestoreHpBeforeBattle && (node.Type == EMapNodeType.Battle || node.Type == EMapNodeType.Boss))
            {
                var missing = _runs.PlayerMaxHp - _runs.PlayerHp;
                if (missing > 0)
                    _runs.ChangePlayerHp(missing);
            }

            if (!_runs.EnterNode(node.Id))
            {
                AbortRun("node_refused");
                return true;
            }

            return true;
        }

        private bool BattleStep()
        {
            if (_battleController.IsOver) return false;

            var player = FindAnyObjectByType<PlayerCharacter>();
            if (player == null || player.IsDead) return false;
            if (_turns.CurrentTurn != ETurnOwner.Player || TurnBlockers.IsBlocked) return false;

            if (_turns.GlobalTurn > _options.MaxTurnsPerBattle)
            {
                AbortRun("turn_limit");
                return true;
            }

            if (_battleBot.TakeTurn(_grid, player))
            {
                _failedActions = 0;
                return true;
            }

            if (++_failedActions >= _options.MaxFailedActions)
                AbortRun("no_legal_action");
            return true;
        }

        /// <summary>Stops the run in progress (recorded as aborted with <paramref name="reason"/>).</summary>
        private void AbortRun(string reason)
        {
            if (_collector.CurrentRun == null) return;

            if (++_abortAttempts > 3)
            {
                _batch.Status = $"could not abort a run ({reason})";
                EndBatch(true);
                return;
            }

            _collector.AbortRun(reason);
            if (_runs.IsRunActive)
                _runs.GiveUp();
        }

        /// <summary>Aborts the run when nothing changed for too many frames.</summary>
        private void CheckProgress()
        {
            if (!_runActive || _collector.CurrentRun == null) return;

            var run = _runs.CurrentRun;
            var signature = HashCode.Combine(
                (int)_runs.Phase,
                run != null ? run.Map.CurrentNodeId : -2,
                run != null ? run.Map.VisitedNodeIds.Count : -2,
                _turns.GlobalTurn,
                (int)_turns.CurrentTurn,
                _collector.CurrentBattle != null ? _collector.CurrentBattle.Turns : -1,
                TalentService.HasOffer ? TalentService.PendingCount + 100 : 0);
            if (signature != _signature)
            {
                _signature = signature;
                _lastProgressFrame = Time.frameCount;
                return;
            }

            if (Time.frameCount - _lastProgressFrame > _options.StuckFrames)
            {
                AbortRun($"stuck_{_runs.Phase}");
                _lastProgressFrame = Time.frameCount;
            }
        }

        private void DisposeBots()
        {
            _battleBot?.Dispose();
            _battleBot = null;
        }
    }
}
