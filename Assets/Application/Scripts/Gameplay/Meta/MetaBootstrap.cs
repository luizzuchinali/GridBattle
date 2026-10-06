using System.Collections.Generic;
using GridBattle.Gameplay.Entities;
using GridBattle.Gameplay.Events;
using UnityEngine;

namespace GridBattle.Gameplay.Meta
{
    /// <summary>
    /// Starts the profile, tutorial and metrics services without any scene object (domain reload is
    /// off in Play Mode, so statics are reset on SubsystemRegistration):
    /// <list type="bullet">
    /// <item>Loads the profile and applies the saved language.</item>
    /// <item>Battle won (<see cref="BattleEndedEvent"/> with victory) counts toward class unlocking and
    /// requests the "first battle won" tip.</item>
    /// <item>Battle start (<see cref="GridInitializedEvent"/>) registers the enemies on the board in the
    /// glossary and requests the "first battle" tip.</item>
    /// <item>Hooks the design metrics (<see cref="MetricsHooks"/>) and flushes them when the app loses focus or quits.</item>
    /// </list>
    /// Run end, talents and summoned enemies are reported by their own systems through
    /// <see cref="ProfileService"/>. The hooks ignore events outside Play Mode (the grid also builds itself in
    /// the editor).
    /// </summary>
    public static class MetaBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Unhook();
            MetricsRecorder.Reset();
            TutorialService.ResetSession();
            ProfileService.Unload();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            ProfileService.Load();
            Hook();

            if (ProfileService.LocaleCode != null)
                ProfileService.ApplyLocale();

            MetricsRecorder.Record("session_started", new
            {
                appVersion = Application.version,
                platform = Application.platform.ToString(),
                locale = ProfileService.LocaleCode ?? "auto",
            });
        }

        private static void Hook()
        {
            Unhook();
            EventBus.Subscribe<GridInitializedEvent>(OnGridInitialized);
            EventBus.Subscribe<BattleEndedEvent>(OnBattleEnded);
            MetricsHooks.Subscribe();
            Application.quitting += OnQuitting;
            Application.focusChanged += MetricsRecorder.OnFocusChanged;
        }

        private static void Unhook()
        {
            EventBus.Unsubscribe<GridInitializedEvent>(OnGridInitialized);
            EventBus.Unsubscribe<BattleEndedEvent>(OnBattleEnded);
            MetricsHooks.Unsubscribe();
            Application.quitting -= OnQuitting;
            Application.focusChanged -= MetricsRecorder.OnFocusChanged;
        }

        private static void OnQuitting()
        {
            MetricsRecorder.Flush();
        }

        private static void OnGridInitialized(GridInitializedEvent e)
        {
            if (!Application.isPlaying) return;

            var enemies = new List<EnemyConfig>();
            foreach (var enemy in Object.FindObjectsByType<Enemy>(FindObjectsInactive.Exclude))
            {
                if (enemy.EnemyConfig != null)
                    enemies.Add(enemy.EnemyConfig);
            }

            ProfileService.RegisterEnemiesFaced(enemies);
            TutorialService.Notify(ETutorialTrigger.FirstBattle);
        }

        private static void OnBattleEnded(BattleEndedEvent e)
        {
            if (!Application.isPlaying || !e.Victory) return;

            ProfileService.RegisterBattleWon();
            TutorialService.Notify(ETutorialTrigger.FirstBattleWon);
        }
    }
}
