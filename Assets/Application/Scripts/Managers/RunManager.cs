using System;
using System.Collections.Generic;
using GridBattle.Core;
using GridBattle.Core.Randomness;
using GridBattle.Data;
using GridBattle.Gameplay;
using GridBattle.Gameplay.Consumables;
using GridBattle.Gameplay.Entities;
using GridBattle.Gameplay.Events;
using GridBattle.Gameplay.Map;
using GridBattle.Gameplay.Meta;
using GridBattle.Gameplay.Run;
using GridBattle.Gameplay.Simulation;
using GridBattle.Gameplay.Terrain;
using GridBattle.Managers.Audio;
using JetBrains.Annotations;
using UnityEngine;

namespace GridBattle.Managers
{
    /// <summary>
    /// The run as a finite map of nodes (GDD Mechanic 2, 2.1 and 8.1): starts a run (seed, class, generated map),
    /// lets the player choose and enter nodes, resolves them (battle, heal, talent with a cost, consumable, final
    /// boss), keeps the player's HP, level, XP and items between nodes, ends the run (victory on the boss, defeat on
    /// death, give up) and saves automatically so a run resumes exactly where it stopped.
    /// <para>
    /// The manager lives on the GameStateManager GameObject (DontDestroyOnLoad with it) and has no UI: the UI
    /// reads <see cref="CurrentRun"/> and <see cref="GetAvailableNodes"/>, listens to the events
    /// (<see cref="RunStartedEvent"/>, <see cref="MapOpenedEvent"/>, <see cref="NodeSelectedEvent"/>,
    /// <see cref="NodeEnteredEvent"/>, <see cref="BattleStartedEvent"/>, <see cref="NodeCompletedEvent"/>,
    /// <see cref="TalentNodeEnteredEvent"/>, <see cref="ConsumableOfferEvent"/>, <see cref="RunEndedEvent"/> and
    /// <see cref="RunProgressChangedEvent"/>) and answers with <see cref="SelectNode"/>, <see cref="EnterNode"/>,
    /// <see cref="ChooseConsumableOffer"/>, <see cref="GiveUp"/> and the other methods.
    /// </para>
    /// <para>
    /// Saving (<c>run.json</c>): after starting a run, after every node resolution and at the start of every
    /// player turn in battle. The battle save is the state at the start of the turn, <b>before</b> that turn's
    /// start effects (so restoring replays them exactly once) and with the random streams at that point, so
    /// loading and replaying the turn gives the same results. Every random draw of the run goes through the run's
    /// <see cref="RunRandom"/>, exposed as <see cref="GameRandom.Active"/> while a run is active.
    /// </para>
    /// <para>
    /// The talent module plugs in through <see cref="RunPlayerHooks"/> (talent states on spawn, maximum HP) and
    /// <see cref="TalentNodeEnteredEvent"/> / <see cref="CompleteTalentNode"/>.
    /// </para>
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RunManager : MonoBehaviour
    {
        /// <summary>File of the run save in the persistent data path.</summary>
        public const string SaveFileName = "run.json";

        private static RunManager _instance;

        private GridController _grid;
        private RunState _run;
        private PlayerCharacterConfig _class;
        private MapNodeState _selectedNode;

        /// <summary>A node's battle is on the board (set before the board is built, cleared when it ends).</summary>
        private bool _battleActive;

        /// <summary>The XP events raised while the player is built must not overwrite the run's progress.</summary>
        private bool _suppressXpSync;

        /// <summary>The player's turn is open and no action was taken yet: XP that arrives now is part of the save.</summary>
        private bool _saveWindowOpen;

        /// <summary>The save of the battle in progress (state at the start of the current turn).</summary>
        private RunState _turnSave;

        /// <summary>The last state written (or meant to be written) to disk.</summary>
        private RunState _lastSaved;

        private int _boardVersion;
        private int _battleStartHp;
        private int _nodeStartHp;
        private int _consumableChoice = -1;

        // ------------------------------------------------------------------------------------ public state

        /// <summary>The run manager of the scene, or null before it woke up.</summary>
        public static RunManager Instance => _instance;

        /// <summary>Whether a run save exists on disk (<see cref="ContinueRun"/> would load it).</summary>
        public bool HasSavedRun => SaveSystem.Exists(SaveFileName);

        /// <summary>
        /// The run in progress, or the one that just ended (phase Finished) until the next run starts; null before
        /// the first run. Plain saved data: do not edit it from the UI.
        /// </summary>
        [CanBeNull]
        public RunState CurrentRun => _run;

        /// <summary>Whether a run is in progress (started or loaded and not finished).</summary>
        public bool IsRunActive => _run != null && _run.Phase != ERunPhase.Finished;

        /// <summary>Current phase (Map when no run is active).</summary>
        public ERunPhase Phase => _run != null ? _run.Phase : ERunPhase.Map;

        /// <summary>A node's battle is being played on the board.</summary>
        public bool IsBattleActive => _battleActive;

        /// <summary>Class of the run, or null.</summary>
        [CanBeNull]
        public PlayerCharacterConfig PlayerClass => _class;

        /// <summary>
        /// Depth of the node the player is at (GDD: position on the map from 1 up to the final boss); 0 before the
        /// first node or without a run.
        /// </summary>
        public int CurrentDepth
        {
            get
            {
                var node = CurrentNode;
                return node != null ? node.Depth : 0;
            }
        }

        /// <summary>Number of floors of the map (the depth of the final boss); 0 without a run.</summary>
        public int FloorCount => _run != null ? _run.Map.FloorCount : 0;

        /// <summary>The node the player is at (the one being resolved, or the last one visited), or null.</summary>
        [CanBeNull]
        public MapNodeState CurrentNode => _run != null ? MapRules.GetNode(_run.Map, _run.Map.CurrentNodeId) : null;

        /// <summary>The node chosen for the preview by <see cref="SelectNode"/>, or null.</summary>
        [CanBeNull]
        public MapNodeState SelectedNode => _selectedNode;

        /// <summary>The persistent HP of the player between nodes (0 without a run).</summary>
        public int PlayerHp => _run != null ? _run.Player.Hp : 0;

        /// <summary>
        /// Maximum HP of the player outside battles: the class's base through the talent hooks
        /// (<see cref="RunPlayerHooks.GetMaxHp"/>); during a battle, the live player's maximum.
        /// </summary>
        public int PlayerMaxHp
        {
            get
            {
                if (_run == null || _class == null) return 0;

                if (_battleActive)
                {
                    var player = FindAnyObjectByType<PlayerCharacter>();
                    if (player != null)
                        return player.MaxHp;
                }

                return RunPlayerHooks.GetMaxHp(_run.Player, _class);
            }
        }

        /// <summary>The consumables of a pending consumable-node offer, in the order offered (empty when none).</summary>
        public IReadOnlyList<ConsumableDefinition> PendingConsumableOptions => ResolveConsumableOptions();

        // ------------------------------------------------------------------------------------ lifecycle

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _instance = null;
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(this);
                return;
            }

            _instance = this;
            EventBus.Subscribe<BattleEndedEvent>(OnBattleEnded);
            EventBus.Subscribe<GlobalTurnStartedEvent>(OnGlobalTurnStarted);
            EventBus.Subscribe<TurnChangedEvent>(OnTurnChanged);
            EventBus.Subscribe<PlayerActionEvent>(OnPlayerAction);
            EventBus.Subscribe<PlayerXpChangedEvent>(OnPlayerXpChanged);
            EventBus.Subscribe<DamageDealtEvent>(OnDamageDealt);
            EventBus.Subscribe<CharacterDiedEvent>(OnCharacterDied);
        }

        private void OnDestroy()
        {
            if (_instance != this) return;

            EventBus.Unsubscribe<BattleEndedEvent>(OnBattleEnded);
            EventBus.Unsubscribe<GlobalTurnStartedEvent>(OnGlobalTurnStarted);
            EventBus.Unsubscribe<TurnChangedEvent>(OnTurnChanged);
            EventBus.Unsubscribe<PlayerActionEvent>(OnPlayerAction);
            EventBus.Unsubscribe<PlayerXpChangedEvent>(OnPlayerXpChanged);
            EventBus.Unsubscribe<DamageDealtEvent>(OnDamageDealt);
            EventBus.Unsubscribe<CharacterDiedEvent>(OnCharacterDied);
            _instance = null;
            if (_run != null && GameRandom.Active == _run.Random)
                GameRandom.Active = null;
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused)
                FlushSave();
        }

        private void OnApplicationQuit()
        {
            FlushSave();
        }

        // ------------------------------------------------------------------------------------ run start / end

        /// <summary>
        /// Starts a new run with <paramref name="playerClass"/>: draws the seed (<see cref="RunSettings.DebugSeed"/>
        /// or a random one), generates the map and the battles, resets the player (full HP, the class's skills, no
        /// consumables), saves and opens the map. A run in progress (or a saved one) is discarded without being
        /// recorded in the profile: use <see cref="GiveUp"/> first to record it. Returns false (nothing changes)
        /// for a null or locked class (<see cref="ProfileService.IsClassUnlocked"/>) or without a board in the scene.
        /// </summary>
        public bool StartNewRun(PlayerCharacterConfig playerClass)
        {
            if (playerClass == null)
            {
                Debug.LogError("StartNewRun needs a class.", this);
                return false;
            }

            if (!ProfileService.IsClassUnlocked(playerClass))
            {
                Debug.LogWarning($"Class {playerClass.name} is not unlocked yet; the run was not started.", this);
                return false;
            }

            if (GetGrid() == null)
            {
                Debug.LogError("There is no GridController in the scene; the run cannot start.", this);
                return false;
            }

            var settings = RunSettings.Current;
            var seed = SimMode.IsActive && SimMode.SeedOverride != 0UL
                ? SimMode.SeedOverride
                : settings.DebugSeed != 0UL ? settings.DebugSeed : RunRandom.CreateSeed();
            var random = new RunRandom(seed);
            var run = new RunState
            {
                Random = random,
                ClassId = playerClass.Id,
                Phase = ERunPhase.Map,
                Map = MapGenerator.Generate(random, GameSettings.Get<MapGenerationSettings>(),
                    GameSettings.Get<BattleGenerationSettings>(), GameSettings.Get<TerrainGenerationSettings>()),
            };

            foreach (var skill in playerClass.Skills)
            {
                if (skill != null && !string.IsNullOrEmpty(skill.Id) && !run.Player.SkillIds.Contains(skill.Id))
                    run.Player.SkillIds.Add(skill.Id);
            }

            run.Player.Hp = RunPlayerHooks.GetMaxHp(run.Player, playerClass);

            // The previous run is only dropped once the new one exists.
            DiscardRun(true);
            BeginRun(run, playerClass, false);
            return true;
        }

        /// <summary>
        /// Loads the saved run and resumes it where it stopped: the map, the open talent node or consumable offer, or
        /// the battle at the start of the turn it was saved at (same board, HP, states, cooldowns, AI memory and
        /// random streams). Returns false (and keeps the file) when there is no readable save or its class is
        /// unknown; an unusable or finished save is deleted.
        /// </summary>
        public bool ContinueRun()
        {
            if (!SaveSystem.TryLoad(SaveFileName, out RunState run))
                return false;

            if (run.Phase == ERunPhase.Finished || run.Map == null || run.Map.Nodes.Count == 0 ||
                run.Random == null || run.Player == null)
            {
                Debug.LogWarning("The saved run is finished or unusable; it was deleted.", this);
                SaveSystem.Delete(SaveFileName);
                return false;
            }

            var database = GameDatabase.Instance;
            var playerClass = database != null ? database.Get<PlayerCharacterConfig>(run.ClassId) : null;
            if (playerClass == null)
            {
                Debug.LogError($"The saved run's class '{run.ClassId}' was not found in the GameDatabase.", this);
                return false;
            }

            if (GetGrid() == null)
            {
                Debug.LogError("There is no GridController in the scene; the run cannot resume.", this);
                return false;
            }

            DiscardRun(false);
            BeginRun(run, playerClass, true);
            return true;
        }

        /// <summary>
        /// The player gives up the run in progress: recorded in the profile as the settings say
        /// (<see cref="MetaSettings.GiveUpCountsAsDefeat"/>), the save deleted, the board cleared and
        /// <see cref="RunEndedEvent"/> raised with reason GivenUp.
        /// </summary>
        public void GiveUp()
        {
            if (!IsRunActive) return;

            ProfileService.RegisterRunGivenUp(_class, _run.Player.Level, Mathf.Max(1, _run.Statistics.MaxDepthReached));
            MetricsRecorder.Record("run_given_up", new
            {
                depth = _run.Statistics.MaxDepthReached,
                level = _run.Player.Level,
                battlesWon = _run.Statistics.BattlesWon,
            });
            FinishRun(ERunEndReason.GivenUp, true);
        }

        /// <summary>
        /// Throws away the saved run (and the run in progress, if any) without recording anything: for an unusable
        /// save or when the player starts over from the menu.
        /// </summary>
        public void AbandonSavedRun()
        {
            DiscardRun(true);
        }

        /// <summary>
        /// Writes the run to disk now. In a battle it rewrites the save of the current turn (the state at its
        /// start); elsewhere it saves the run as it is. Call it after changing <see cref="RunState"/> outside the
        /// normal flow (e.g. the talent module after adding a talent or a pending offer on the map).
        /// </summary>
        public void SaveNow()
        {
            if (!IsRunActive) return;

            if (_run.Phase == ERunPhase.Battle)
            {
                if (_turnSave != null)
                    WriteSave(_turnSave);
            }
            else
            {
                SaveCurrent();
            }
        }

        // ------------------------------------------------------------------------------------ map queries

        /// <summary>
        /// The nodes the player can enter now: the first floor before the first node, otherwise the nodes the
        /// current node leads to. Empty outside the map phase.
        /// </summary>
        public List<MapNodeState> GetAvailableNodes()
        {
            if (_run == null || _run.Phase != ERunPhase.Map)
                return new List<MapNodeState>();

            return MapRules.GetAvailableNodes(_run.Map);
        }

        /// <summary>The node with the id, or null.</summary>
        [CanBeNull]
        public MapNodeState GetNode(int id) => _run != null ? MapRules.GetNode(_run.Map, id) : null;

        // ------------------------------------------------------------------------------------ nodes

        /// <summary>
        /// Previews a node: remembers it (<see cref="SelectedNode"/>), plays the select sound and raises
        /// <see cref="NodeSelectedEvent"/>. Nothing is committed. Returns false if the node is not available.
        /// </summary>
        public bool SelectNode(int id)
        {
            if (_run == null || _run.Phase != ERunPhase.Map) return false;

            var node = FindAvailableNode(id);
            if (node == null) return false;

            _selectedNode = node;
            AudioManager.Play(ESfx.NodeSelect);
            EventBus.Raise(new NodeSelectedEvent(node));
            return true;
        }

        /// <summary>
        /// Enters a node reachable from the current one: plays the confirm sound, raises
        /// <see cref="NodeEnteredEvent"/> and resolves it. Battle and boss: builds the board and starts the fight
        /// (<see cref="BattleStartedEvent"/>). Heal: heals the persistent HP and returns to the map. Talent: pays the
        /// HP cost and raises <see cref="TalentNodeEnteredEvent"/> (the node stays open until
        /// <see cref="CompleteTalentNode"/>). Consumable: draws the item(s) from the Consumables stream and gives it
        /// or raises <see cref="ConsumableOfferEvent"/>. Returns false if the node is not available.
        /// </summary>
        public bool EnterNode(int id)
        {
            if (_run == null || _run.Phase != ERunPhase.Map) return false;

            var node = FindAvailableNode(id);
            if (node == null) return false;

            if ((node.Type == EMapNodeType.Battle || node.Type == EMapNodeType.Boss) && node.Battle == null)
            {
                Debug.LogError($"Battle node {node.Id} has no battle spec.", this);
                return false;
            }

            _selectedNode = null;
            _run.Map.CurrentNodeId = node.Id;
            _run.Statistics.MaxDepthReached = Mathf.Max(_run.Statistics.MaxDepthReached, node.Depth);
            _nodeStartHp = _run.Player.Hp;
            AudioManager.Play(ESfx.NodeConfirm);

            MetricsRecorder.SetContext("depth", node.Depth);
            MetricsRecorder.Record("node_entered", new
            {
                nodeId = node.Id,
                type = node.Type.ToString(),
                difficulty = node.Difficulty.ToString(),
                depth = node.Depth,
                hp = _run.Player.Hp,
                maxHp = PlayerMaxHp,
                level = _run.Player.Level,
                totalXp = node.Battle != null ? node.Battle.TotalXp : 0,
            });

            EventBus.Raise(new NodeEnteredEvent(node));
            RaiseProgress();

            switch (node.Type)
            {
                case EMapNodeType.Battle:
                case EMapNodeType.Boss:
                    StartBattle(node);
                    break;
                case EMapNodeType.Heal:
                    ResolveHeal(node);
                    break;
                case EMapNodeType.Talent:
                    ResolveTalent(node);
                    break;
                default:
                    ResolveConsumable(node);
                    break;
            }

            return true;
        }

        /// <summary>
        /// The talent module is done with the talent node (the player chose or skipped): marks it visited, saves and
        /// returns to the map. Ignored outside the TalentNode phase.
        /// </summary>
        public void CompleteTalentNode()
        {
            if (_run == null || _run.Phase != ERunPhase.TalentNode) return;

            var node = CurrentNode;
            if (node == null) return;

            CompleteNode(node, _run.Player.Hp - _nodeStartHp);
        }

        /// <summary>
        /// Adds (or removes, if negative) HP to the persistent player outside battles, within 1 and the maximum.
        /// For modules that change the run's HP between nodes (e.g. a talent that heals when chosen). Returns the
        /// change applied.
        /// </summary>
        public int ChangePlayerHp(int delta)
        {
            if (_run == null || _battleActive) return 0;

            var before = _run.Player.Hp;
            _run.Player.Hp = Mathf.Clamp(before + delta, 1, Mathf.Max(1, PlayerMaxHp));
            RaiseProgress();
            return _run.Player.Hp - before;
        }

        // ------------------------------------------------------------------------------------ consumable offers

        /// <summary>
        /// The player takes option <paramref name="index"/> of the pending consumable offer. With a free slot (or
        /// with the DiscardNew / ReplaceOldest policies) the item is received and the node completes; with a full
        /// inventory under AskPlayer the choice is remembered (<see cref="EConsumableOfferResult.NeedsSlotChoice"/>)
        /// and the UI follows with <see cref="ReplaceConsumable"/> or <see cref="DeclineConsumableOffer"/>.
        /// </summary>
        public EConsumableOfferResult ChooseConsumableOffer(int index)
        {
            if (_run == null || _run.Phase != ERunPhase.ConsumableOffer) return EConsumableOfferResult.Invalid;

            var options = ResolveConsumableOptions();
            if (index < 0 || index >= options.Count) return EConsumableOfferResult.Invalid;

            var inventory = new ConsumableInventory(_run.Player.ConsumableIds);
            if (inventory.IsFull && ConsumableSettings.Current.FullPolicy == EFullInventoryPolicy.AskPlayer)
            {
                _consumableChoice = index;
                return EConsumableOfferResult.NeedsSlotChoice;
            }

            var result = inventory.Receive(options[index]);
            var node = CurrentNode;
            switch (result)
            {
                case EConsumableReceiveResult.Added:
                    FinishConsumableNode(node, true);
                    return EConsumableOfferResult.Added;
                case EConsumableReceiveResult.ReplacedOldest:
                    FinishConsumableNode(node, true);
                    return EConsumableOfferResult.ReplacedOldest;
                case EConsumableReceiveResult.DiscardedNew:
                    FinishConsumableNode(node, false);
                    return EConsumableOfferResult.DiscardedNew;
                default:
                    _consumableChoice = index;
                    return EConsumableOfferResult.NeedsSlotChoice;
            }
        }

        /// <summary>
        /// Full inventory: the chosen option (the only one, if the offer had a single option) takes the place of the
        /// item in <paramref name="slot"/>, which is discarded; the node completes. Returns false if there is no
        /// option chosen, or the slot is invalid.
        /// </summary>
        public bool ReplaceConsumable(int slot)
        {
            if (_run == null || _run.Phase != ERunPhase.ConsumableOffer) return false;

            var options = ResolveConsumableOptions();
            var index = _consumableChoice >= 0 ? _consumableChoice : (options.Count == 1 ? 0 : -1);
            if (index < 0 || index >= options.Count) return false;

            var inventory = new ConsumableInventory(_run.Player.ConsumableIds);
            if (!inventory.Replace(slot, options[index])) return false;

            FinishConsumableNode(CurrentNode, true);
            return true;
        }

        /// <summary>The player takes nothing: the consumable node completes without an item.</summary>
        public bool DeclineConsumableOffer()
        {
            if (_run == null || _run.Phase != ERunPhase.ConsumableOffer) return false;

            FinishConsumableNode(CurrentNode, false);
            return true;
        }

        // ------------------------------------------------------------------------------------ run flow (private)

        private void BeginRun(RunState run, PlayerCharacterConfig playerClass, bool resumed)
        {
            _run = run;
            _class = playerClass;
            _selectedNode = null;
            _battleActive = false;
            _saveWindowOpen = false;
            _turnSave = null;
            _consumableChoice = -1;
            _boardVersion++;
            GameRandom.Active = run.Random;

            MetricsRecorder.SetContext("class", playerClass.name);
            MetricsRecorder.SetContext("depth", CurrentDepth);
            MetricsRecorder.Record(resumed ? "run_resumed" : "run_started", new
            {
                @class = new { id = playerClass.Id, name = playerClass.name },
                seed = run.Random.Seed,
                floors = run.Map.FloorCount,
                nodes = run.Map.Nodes.Count,
                phase = run.Phase.ToString(),
            });

            EventBus.Raise(new RunStartedEvent(run, playerClass, resumed));
            RaiseProgress();

            switch (run.Phase)
            {
                case ERunPhase.Battle:
                    if (run.Battle != null && MapRules.GetNode(run.Map, run.Battle.NodeId) != null)
                        RestoreBattle(run);
                    else
                        OpenMap(true);
                    break;
                case ERunPhase.TalentNode:
                    _nodeStartHp = run.Player.Hp;
                    ClearBoard();
                    AudioManager.SetMusicContext(EMusicContext.Map);
                    EventBus.Raise(new TalentNodeEnteredEvent(CurrentNode, 0, true));
                    break;
                case ERunPhase.ConsumableOffer:
                    ClearBoard();
                    AudioManager.SetMusicContext(EMusicContext.Map);
                    RaiseConsumableOffer();
                    break;
                default:
                    ClearBoard();
                    OpenMap(resumed);
                    break;
            }
        }

        /// <summary>Shows the map: back from a node, or at the start / resume of a run.</summary>
        private void OpenMap(bool resumed)
        {
            _run.Phase = ERunPhase.Map;
            _run.Battle = null;
            _run.PendingConsumableOptions.Clear();
            _turnSave = null;
            _battleActive = false;
            _saveWindowOpen = false;
            _selectedNode = null;
            _consumableChoice = -1;
            var version = ++_boardVersion;

            // The world is emptied one frame later: other listeners of the event that ended the battle still see it.
            if (resumed)
                ClearBoard();
            else
                _ = ClearBoardNextFrame(version);

            AudioManager.SetMusicContext(EMusicContext.Map);
            SaveCurrent();
            RaiseProgress();
            EventBus.Raise(new MapOpenedEvent(resumed));
            TutorialService.Notify(ETutorialTrigger.FirstMap);

            if (RunSettings.Current.AutoEnterFirstNode)
                _ = AutoEnterFirstNodeNextFrame(version);
        }

        private void StartBattle(MapNodeState node)
        {
            _run.Phase = ERunPhase.Battle;
            _run.Battle = null;
            _battleStartHp = _run.Player.Hp;
            _boardVersion++;

            if (node.Type == EMapNodeType.Boss)
                AudioManager.SetMusicContext(EMusicContext.Boss);

            _battleActive = true;
            _suppressXpSync = true;
            try
            {
                GetGrid().InitializeBattle(node.Battle, _class, _run.Player);
            }
            finally
            {
                _suppressXpSync = false;
            }

            EventBus.Raise(new BattleStartedEvent(node, node.Battle, false));
        }

        private void RestoreBattle(RunState run)
        {
            var node = MapRules.GetNode(run.Map, run.Battle.NodeId);
            var snapshot = run.Battle;
            run.Map.CurrentNodeId = node.Id;
            run.Battle = null;
            _battleStartHp = snapshot.Player != null ? snapshot.Player.Hp : run.Player.Hp;
            _boardVersion++;

            if (node.Type == EMapNodeType.Boss)
                AudioManager.SetMusicContext(EMusicContext.Boss);

            _battleActive = true;
            _suppressXpSync = true;
            try
            {
                GetGrid().RestoreBattle(snapshot, _class, run.Player);
            }
            finally
            {
                _suppressXpSync = false;
            }

            EventBus.Raise(new BattleStartedEvent(node, node.Battle ?? snapshot.Spec, true));
        }

        private void ResolveHeal(MapNodeState node)
        {
            var settings = RunSettings.Current;
            var maxHp = PlayerMaxHp;
            var before = _run.Player.Hp;
            _run.Player.Hp = Mathf.Min(Mathf.Max(maxHp, before), before + settings.GetHealAmount(maxHp));
            TutorialService.Notify(ETutorialTrigger.FirstHealNode);
            AudioManager.Play(ESfx.Heal);
            CompleteNode(node, _run.Player.Hp - before);
        }

        private void ResolveTalent(MapNodeState node)
        {
            var settings = RunSettings.Current;
            var before = _run.Player.Hp;
            var cost = settings.GetTalentNodeCost(before, PlayerMaxHp);
            _run.Player.Hp = before - cost;

            _run.Phase = ERunPhase.TalentNode;
            SaveCurrent();
            RaiseProgress();
            TutorialService.Notify(ETutorialTrigger.FirstTalentNode);
            EventBus.Raise(new TalentNodeEnteredEvent(node, cost, false));
        }

        private void ResolveConsumable(MapNodeState node)
        {
            TutorialService.Notify(ETutorialTrigger.FirstConsumableNode);

            var options = ConsumableGrant.Roll(GameRandom.Stream(ERandomStream.Consumables), ConsumableSettings.Current);
            if (options.Count == 0)
            {
                Debug.LogWarning("The consumable pool is empty; the node granted nothing.", this);
                CompleteNode(node, 0);
                return;
            }

            _run.PendingConsumableOptions.Clear();
            foreach (var option in options)
                _run.PendingConsumableOptions.Add(option.Id);
            _consumableChoice = -1;

            if (options.Count == 1)
            {
                var inventory = new ConsumableInventory(_run.Player.ConsumableIds);
                switch (inventory.Receive(options[0]))
                {
                    case EConsumableReceiveResult.Added:
                    case EConsumableReceiveResult.ReplacedOldest:
                        FinishConsumableNode(node, true);
                        return;
                    case EConsumableReceiveResult.DiscardedNew:
                        FinishConsumableNode(node, false);
                        return;
                }
            }

            _run.Phase = ERunPhase.ConsumableOffer;
            SaveCurrent();
            RaiseConsumableOffer();
        }

        private void RaiseConsumableOffer()
        {
            var inventory = new ConsumableInventory(_run.Player.ConsumableIds);
            EventBus.Raise(new ConsumableOfferEvent(CurrentNode, ResolveConsumableOptions(), inventory.IsFull));
        }

        private void FinishConsumableNode(MapNodeState node, bool gained)
        {
            if (gained)
                AudioManager.Play(ESfx.ConsumableGained);
            if (node != null)
                CompleteNode(node, 0);
        }

        /// <summary>Marks the node visited, saves and goes back to the map.</summary>
        private void CompleteNode(MapNodeState node, int hpDelta)
        {
            if (!_run.Map.VisitedNodeIds.Contains(node.Id))
                _run.Map.VisitedNodeIds.Add(node.Id);

            EventBus.Raise(new NodeCompletedEvent(node, hpDelta));
            OpenMap(false);
        }

        // ------------------------------------------------------------------------------------ battle end

        private void OnBattleEnded(BattleEndedEvent e)
        {
            if (_run == null || _run.Phase != ERunPhase.Battle || !_battleActive) return;

            _battleActive = false;
            _saveWindowOpen = false;
            var node = CurrentNode;
            if (node == null) return;

            if (!e.Victory)
            {
                MetricsRecorder.Record("battle_result", BattleResult(node, false, _battleStartHp, 0));
                FinishRun(ERunEndReason.Defeat, false);
                return;
            }

            var player = FindAnyObjectByType<PlayerCharacter>();
            if (player != null)
                PlayerRunStateApplier.Capture(player, _run.Player, RunSettings.Current);

            _run.Statistics.BattlesWon++;
            MetricsRecorder.Record("battle_result", BattleResult(node, true, _battleStartHp, _run.Player.Hp));
            RaiseProgress();

            if (node.Type == EMapNodeType.Boss)
                FinishRun(ERunEndReason.Victory, false);
            else
                CompleteNode(node, _run.Player.Hp - _battleStartHp);
        }

        private object BattleResult(MapNodeState node, bool victory, int hpBefore, int hpAfter)
        {
            return new
            {
                nodeId = node.Id,
                victory,
                type = node.Type.ToString(),
                difficulty = node.Difficulty.ToString(),
                depth = node.Depth,
                hpBefore,
                hpAfter,
                level = _run.Player.Level,
                totalXp = node.Battle != null ? node.Battle.TotalXp : 0,
            };
        }

        /// <summary>Ends the run: summary, profile, save deleted, board cleared, <see cref="RunEndedEvent"/>.</summary>
        private void FinishRun(ERunEndReason reason, bool clearBoardNow)
        {
            var run = _run;
            run.Phase = ERunPhase.Finished;
            run.Victory = reason == ERunEndReason.Victory;
            run.Battle = null;
            _battleActive = false;
            _saveWindowOpen = false;
            _turnSave = null;
            _lastSaved = null;
            _selectedNode = null;
            var version = ++_boardVersion;

            var summary = BuildSummary(reason);
            SaveSystem.Delete(SaveFileName);

            if (reason != ERunEndReason.GivenUp)
                ProfileService.RegisterRunEnded(_class, run.Victory, summary.Level, summary.Depth);

            switch (reason)
            {
                case ERunEndReason.Victory:
                    AudioManager.Play(ESfx.RunVictory);
                    break;
                case ERunEndReason.Defeat:
                    AudioManager.Play(ESfx.RunDefeat);
                    break;
            }

            if (GameRandom.Active == run.Random)
                GameRandom.Active = null;
            MetricsRecorder.SetContext("class", null);
            MetricsRecorder.SetContext("depth", null);

            if (clearBoardNow)
                ClearBoard();
            else
                _ = ClearBoardNextFrame(version);

            EventBus.Raise(new RunProgressChangedEvent(0, 0, 0, 0));
            EventBus.Raise(new RunEndedEvent(summary));
        }

        private RunSummary BuildSummary(ERunEndReason reason)
        {
            var stats = _run.Statistics;
            var summary = new RunSummary
            {
                Reason = reason,
                PlayerClass = _class,
                ClassId = _run.ClassId,
                Level = _run.Player.Level,
                Depth = Mathf.Max(1, stats.MaxDepthReached),
                FloorCount = _run.Map.FloorCount,
                BattlesWon = stats.BattlesWon,
                EnemiesKilled = stats.EnemiesKilled,
                DamageDealt = stats.DamageDealt,
                DamageTaken = stats.DamageTaken,
                TurnsPlayed = stats.TurnsPlayed,
                Statistics = SaveSystem.Clone(stats),
                SkillIds = new List<string>(_run.Player.SkillIds),
            };

            foreach (var talent in _run.Player.Talents)
            {
                summary.Talents.Add(new TalentRankState { TalentId = talent.TalentId, Rank = talent.Rank });
                summary.TalentCount += Mathf.Max(1, talent.Rank);
            }

            return summary;
        }

        /// <summary>Drops the run in memory (and optionally the save) without recording anything.</summary>
        private void DiscardRun(bool deleteSave)
        {
            if (_run != null && GameRandom.Active == _run.Random)
                GameRandom.Active = null;

            var hadBoard = _battleActive;
            _run = null;
            _class = null;
            _selectedNode = null;
            _battleActive = false;
            _saveWindowOpen = false;
            _turnSave = null;
            _lastSaved = null;
            _consumableChoice = -1;
            _boardVersion++;

            if (deleteSave)
                SaveSystem.Delete(SaveFileName);
            if (hadBoard)
                ClearBoard();
        }

        // ------------------------------------------------------------------------------------ saving

        private void OnGlobalTurnStarted(GlobalTurnStartedEvent e)
        {
            // The simulation never saves (no turn snapshot, no disk writes).
            if (SimMode.IsActive) return;
            if (!_battleActive || _run == null || _run.Phase != ERunPhase.Battle) return;

            var grid = GetGrid();
            if (grid == null) return;

            // The turn starts here and its start effects have not run yet: this is the state a load replays.
            var snapshot = grid.CaptureBattle();
            snapshot.NodeId = _run.Map.CurrentNodeId;
            snapshot.ConsumableUsedThisTurn = false;

            var copy = SaveSystem.Clone(_run);
            copy.Battle = snapshot;
            if (snapshot.Player != null)
                copy.Player.Hp = snapshot.Player.Hp;
            _turnSave = copy;
            _saveWindowOpen = false;
        }

        private void OnTurnChanged(TurnChangedEvent e)
        {
            if (!_battleActive || _run == null || _run.Phase != ERunPhase.Battle) return;

            if (!e.IsPlayerTurn)
            {
                _saveWindowOpen = false;
                return;
            }

            if (_turnSave == null) return;

            // Level-ups and talent choices of the last enemy phase are resolved by now: keep the progress as it
            // stands. The battle itself stays as it was at the start of the turn.
            var hp = _turnSave.Battle != null && _turnSave.Battle.Player != null
                ? _turnSave.Battle.Player.Hp
                : _run.Player.Hp;
            _turnSave.Player = SaveSystem.Clone(_run.Player);
            _turnSave.Player.Hp = hp;
            _turnSave.PendingOffers = SaveSystem.Clone(_run.PendingOffers);
            _saveWindowOpen = true;
            WriteSave(_turnSave);
        }

        private void OnPlayerAction(PlayerActionEvent e)
        {
            if (!_battleActive || _run == null) return;

            _saveWindowOpen = false;
            _run.Statistics.TurnsPlayed++;
        }

        private void OnPlayerXpChanged(PlayerXpChangedEvent e)
        {
            if (_suppressXpSync || !_battleActive || _run == null || _run.Phase != ERunPhase.Battle) return;

            _run.Player.Level = e.Level;
            _run.Player.Xp = e.CurrentXp;

            // XP still flying from kills of the last turn lands before the player acts: it belongs in the save.
            if (_saveWindowOpen && _turnSave != null)
            {
                _turnSave.Player.Level = e.Level;
                _turnSave.Player.Xp = e.CurrentXp;
                WriteSave(_turnSave);
            }
        }

        private void OnDamageDealt(DamageDealtEvent e)
        {
            if (!_battleActive || _run == null) return;

            var hit = e.Hit;
            if (hit.Target is PlayerCharacter)
                _run.Statistics.DamageTaken += hit.HpDamage;
            else if (hit.Target is Enemy && hit.Attacker is PlayerCharacter)
                _run.Statistics.DamageDealt += hit.Damage;
        }

        private void OnCharacterDied(CharacterDiedEvent e)
        {
            if (!_battleActive || _run == null) return;

            if (e.Character is Enemy)
                _run.Statistics.EnemiesKilled++;
        }

        private void SaveCurrent()
        {
            if (_run == null || _run.Phase == ERunPhase.Finished || SimMode.IsActive) return;

            WriteSave(SaveSystem.Clone(_run));
        }

        private void WriteSave(RunState state)
        {
            _lastSaved = state;
            if (!RunSettings.Current.Autosave || SimMode.IsActive) return;

            SaveSystem.Save(SaveFileName, state);
        }

        /// <summary>Writes the last saved state again (the app is pausing or quitting).</summary>
        private void FlushSave()
        {
            if (_lastSaved == null || !IsRunActive || !RunSettings.Current.Autosave) return;

            SaveSystem.Save(SaveFileName, _lastSaved);
        }

        // ------------------------------------------------------------------------------------ helpers

        [CanBeNull]
        private GridController GetGrid()
        {
            if (_grid == null)
                _grid = FindAnyObjectByType<GridController>();
            return _grid;
        }

        [CanBeNull]
        private MapNodeState FindAvailableNode(int id)
        {
            foreach (var node in MapRules.GetAvailableNodes(_run.Map))
            {
                if (node.Id == id)
                    return node;
            }

            return null;
        }

        private List<ConsumableDefinition> ResolveConsumableOptions()
        {
            var result = new List<ConsumableDefinition>();
            var database = GameDatabase.Instance;
            if (_run == null || database == null) return result;

            foreach (var id in _run.PendingConsumableOptions)
            {
                var definition = database.Get<ConsumableDefinition>(id);
                if (definition != null)
                    result.Add(definition);
            }

            return result;
        }

        private void RaiseProgress()
        {
            if (!IsRunActive)
            {
                EventBus.Raise(new RunProgressChangedEvent(0, 0, 0, 0));
                return;
            }

            EventBus.Raise(new RunProgressChangedEvent(CurrentDepth, FloorCount, _run.Player.Hp, PlayerMaxHp));
        }

        private void ClearBoard()
        {
            var grid = GetGrid();
            if (grid != null)
                grid.ClearBoard();
        }

        private async Awaitable ClearBoardNextFrame(int version)
        {
            try
            {
                await Awaitable.NextFrameAsync(destroyCancellationToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            if (version == _boardVersion)
                ClearBoard();
        }

        private async Awaitable AutoEnterFirstNodeNextFrame(int version)
        {
            try
            {
                await Awaitable.NextFrameAsync(destroyCancellationToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            if (version != _boardVersion || _run == null || _run.Phase != ERunPhase.Map) return;

            var nodes = GetAvailableNodes();
            if (nodes.Count > 0)
                EnterNode(nodes[0].Id);
        }
    }
}
