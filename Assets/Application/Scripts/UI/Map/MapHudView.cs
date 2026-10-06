using GridBattle.Gameplay.Consumables;
using GridBattle.Gameplay.Progression;
using GridBattle.Managers;
using GridBattle.UI.Hud;
using GridBattle.UI.Screens;
using UnityEngine;
using UnityEngine.UIElements;
using ZS.UI;

namespace GridBattle.UI.Map
{
    /// <summary>
    /// HUD of the map screen (interface 4.2): depth, HP bar, level and XP bar, and the consumables carried
    /// (read only). The only place that knows these elements in the UXML; reads the run through the
    /// <see cref="RunManager"/>.
    /// </summary>
    public sealed class MapHudView
    {
        private readonly Label _depthCaption;
        private readonly Label _depthValue;
        private readonly VisualElement _hpFill;
        private readonly Label _hpLabel;
        private readonly VisualElement _xpFill;
        private readonly Label _xpLabel;
        private readonly VisualElement _items;

        private int _depth;
        private int _floorCount;
        private int _hp;
        private int _maxHp;
        private int _level = 1;
        private int _xp;
        private int _xpToNext;

        public MapHudView(VisualElement root)
        {
            _depthCaption = root.Q<Label>("map-depth-caption");
            _depthValue = root.Q<Label>("map-depth-value");
            _hpFill = root.Q<VisualElement>("map-hp-fill");
            _hpLabel = root.Q<Label>("map-hp-label");
            _xpFill = root.Q<VisualElement>("map-xp-fill");
            _xpLabel = root.Q<Label>("map-xp-label");
            _items = root.Q<VisualElement>("map-items");
        }

        /// <summary>Text shown in the depth tile ("3/11").</summary>
        public string DepthText => _depthValue != null ? _depthValue.text : string.Empty;

        /// <summary>Text shown over the HP bar ("HP 18/24").</summary>
        public string HpText => _hpLabel != null ? _hpLabel.text : string.Empty;

        /// <summary>Text shown over the XP bar ("Lv 3  12/50").</summary>
        public string XpText => _xpLabel != null ? _xpLabel.text : string.Empty;

        /// <summary>Number of consumable slots drawn.</summary>
        public int ItemSlotCount => _items != null ? _items.childCount : 0;

        /// <summary>Reads everything from the run manager (the run in progress, or nothing).</summary>
        public void Refresh()
        {
            var runs = RunManager.Instance;
            var run = runs != null ? runs.CurrentRun : null;
            if (run == null)
            {
                SetProgress(0, 0, 0, 0);
                SetXp(1, 0, 0);
                RefreshItems();
                return;
            }

            SetProgress(runs.CurrentDepth, runs.FloorCount, runs.PlayerHp, runs.PlayerMaxHp);
            SetXp(run.Player.Level, run.Player.Xp, GetXpToNextLevel(runs, run.Player.Level));
            RefreshItems();
        }

        /// <summary>Depth and HP (RunProgressChangedEvent).</summary>
        public void SetProgress(int depth, int floorCount, int hp, int maxHp)
        {
            _depth = depth;
            _floorCount = floorCount;
            _hp = hp;
            _maxHp = maxHp;
            Render();
        }

        /// <summary>Level and XP (PlayerXpChangedEvent).</summary>
        public void SetXp(int level, int xp, int xpToNext)
        {
            _level = level;
            _xp = xp;
            _xpToNext = xpToNext;
            Render();
        }

        /// <summary>Redraws the consumables the player carries (read only).</summary>
        public void RefreshItems()
        {
            if (_items == null) return;

            _items.Clear();
            var show = ScreensSettings.Current.ShowConsumablesOnMap;
            _items.SetDisplayed(show);
            if (!show) return;

            var runs = RunManager.Instance;
            var run = runs != null ? runs.CurrentRun : null;
            if (run == null) return;

            var inventory = new ConsumableInventory(run.Player.ConsumableIds);
            for (var slot = 0; slot < inventory.Slots; slot++)
            {
                var item = inventory.Get(slot);
                var box = new VisualElement { pickingMode = PickingMode.Ignore };
                box.AddToClassList("mini-slot");
                box.EnableInClassList("mini-slot--empty", item == null);
                if (item != null && item.Icon != null)
                    box.Add(new Image { sprite = item.Icon, pickingMode = PickingMode.Ignore });
                _items.Add(box);
            }
        }

        /// <summary>Applies the current language.</summary>
        public void RefreshTexts()
        {
            Render();
        }

        private void Render()
        {
            if (_depthValue == null) return;

            _depthCaption.text = HudText.Get("hud.depth");
            _depthValue.text = _floorCount > 0 ? HudText.Format("map.depth_value", _depth, _floorCount) : "-";

            var hpRatio = _maxHp > 0 ? Mathf.Clamp01((float)_hp / _maxHp) : 0f;
            _hpFill.style.width = Length.Percent(hpRatio * 100f);
            _hpLabel.text = HudText.Format("map.hp", _hp, _maxHp);

            var xpRatio = _xpToNext > 0 ? Mathf.Clamp01((float)_xp / _xpToNext) : 0f;
            _xpFill.style.width = Length.Percent(xpRatio * 100f);
            _xpLabel.text = _xpToNext > 0
                ? HudText.Format("map.xp", _level, _xp, _xpToNext)
                : HudText.Format("map.level", _level);
        }

        /// <summary>
        /// XP needed to leave <paramref name="level"/> (the progression settings' curve), or 0 at the maximum
        /// level (the bar then shows only the level). Kept in one place so a change of the XP curve only touches
        /// this method.
        /// </summary>
        public static int GetXpToNextLevel(RunManager runs, int level)
        {
            var progression = ProgressionSettings.Current;
            return progression.IsMaxLevel(level) ? 0 : progression.GetXpToNextLevel(level);
        }
    }
}
