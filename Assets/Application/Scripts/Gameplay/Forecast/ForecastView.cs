using System.Collections.Generic;
using GridBattle.Gameplay.Controllers;
using GridBattle.Gameplay.Entities;
using GridBattle.Gameplay.Events;
using GridBattle.Gameplay.Simulation;
using GridBattle.Managers;
using JetBrains.Annotations;
using TMPro;
using UnityEngine;

namespace GridBattle.Gameplay.Forecast
{
    /// <summary>
    /// Draws the battle forecast on the board (Into the Breach style clarity), so every consequence is visible before
    /// the player commits:
    /// <list type="bullet">
    /// <item>the cells of every prepared enemy strike (<see cref="Cell.Warned"/>, striped), red where a character
    /// would be hit (<see cref="Cell.WarnHit"/>); while the player inspects an enemy
    /// (<see cref="PlayerCharacterController.InspectedEnemy"/>) the other strikes fade (<see cref="Cell.WarnDimmed"/>),
    /// so each area reads as whose without numbers on the cells;</item>
    /// <item>the acting order on every enemy (<see cref="EnemyIconsView.SetActingOrder"/>) and, on the inspected one, the
    /// icon of its prepared strike (<see cref="EnemyIconsView.SetFocused"/>);</item>
    /// <item>the damage each character would take, blinking on its health bar with the amount (red when lethal);</item>
    /// <item>while a skill or an item is aimed (<see cref="PlayerCharacterController.AimedCell"/>): the action's own
    /// damage and kills, arrows for its pushes and pulls, ghosts where characters would end, and the enemy strikes as
    /// they would be after it (moved with a displaced caster, gone with a killed one).</item>
    /// </list>
    /// Added by the grid in Play Mode; recomputed once per frame after anything that can change it. Visual only:
    /// nothing reads it, and it draws nothing in the balance simulation.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ForecastView : MonoBehaviour
    {
        private const string FontResource = "Fonts & Materials/ThaleahFat_TTF SDF";
        private const int OverlaySortingOrder = 101;
        private const int GhostSortingOrder = 98;

        private GridController _grid;
        private TurnManager _turns;
        private bool _dirty = true;
        private bool _cleared = true;
        private TMP_FontAsset _font;

        private readonly List<TextMeshPro> _orderTags = new();
        private readonly List<SpriteRenderer> _arrows = new();
        private readonly List<SpriteRenderer> _ghosts = new();
        private readonly List<SpriteRenderer> _ghostSources = new();
        private readonly HashSet<Enemy> _badged = new();
        private int _usedTags;
        private int _usedArrows;
        private int _usedGhosts;
        private Enemy _lastInspected;

        /// <summary>The enemy strikes as last drawn (after the aimed action, when there is one).</summary>
        [CanBeNull]
        public ThreatForecast Threat { get; private set; }

        /// <summary>The aimed action as last drawn, or null when nothing is aimed.</summary>
        [CanBeNull]
        public ActionForecast Action { get; private set; }

        private void Awake()
        {
            _grid = GetComponent<GridController>();
            _turns = GetComponent<TurnManager>();
        }

        private void OnEnable()
        {
            EventBus.Subscribe<SkillTelegraphChangedEvent>(MarkDirty);
            EventBus.Subscribe<EntityEnteredCellEvent>(MarkDirty);
            EventBus.Subscribe<EntityMoveUndoneEvent>(MarkDirty);
            EventBus.Subscribe<CharacterDisplacedEvent>(MarkDirty);
            EventBus.Subscribe<CharactersSwappedEvent>(MarkDirty);
            EventBus.Subscribe<CharacterDiedEvent>(MarkDirty);
            EventBus.Subscribe<EnemySummonedEvent>(MarkDirty);
            EventBus.Subscribe<StateAppliedEvent>(MarkDirty);
            EventBus.Subscribe<StateRemovedEvent>(MarkDirty);
            EventBus.Subscribe<DamageDealtEvent>(MarkDirty);
            EventBus.Subscribe<HealedEvent>(MarkDirty);
            EventBus.Subscribe<TerrainTriggeredEvent>(MarkDirty);
            EventBus.Subscribe<TurnChangedEvent>(MarkDirty);
            EventBus.Subscribe<GridInitializedEvent>(MarkDirty);
            EventBus.Subscribe<PlayerTurnStateChangedEvent>(MarkDirty);
            EventBus.Subscribe<ActionAimChangedEvent>(MarkDirty);
            EventBus.Subscribe<SkillSelectionChangedEvent>(MarkDirty);
            EventBus.Subscribe<ConsumableSelectionChangedEvent>(MarkDirty);
            EventBus.Subscribe<BattleDecidedEvent>(MarkDirty);
            _dirty = true;
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<SkillTelegraphChangedEvent>(MarkDirty);
            EventBus.Unsubscribe<EntityEnteredCellEvent>(MarkDirty);
            EventBus.Unsubscribe<EntityMoveUndoneEvent>(MarkDirty);
            EventBus.Unsubscribe<CharacterDisplacedEvent>(MarkDirty);
            EventBus.Unsubscribe<CharactersSwappedEvent>(MarkDirty);
            EventBus.Unsubscribe<CharacterDiedEvent>(MarkDirty);
            EventBus.Unsubscribe<EnemySummonedEvent>(MarkDirty);
            EventBus.Unsubscribe<StateAppliedEvent>(MarkDirty);
            EventBus.Unsubscribe<StateRemovedEvent>(MarkDirty);
            EventBus.Unsubscribe<DamageDealtEvent>(MarkDirty);
            EventBus.Unsubscribe<HealedEvent>(MarkDirty);
            EventBus.Unsubscribe<TerrainTriggeredEvent>(MarkDirty);
            EventBus.Unsubscribe<TurnChangedEvent>(MarkDirty);
            EventBus.Unsubscribe<GridInitializedEvent>(MarkDirty);
            EventBus.Unsubscribe<PlayerTurnStateChangedEvent>(MarkDirty);
            EventBus.Unsubscribe<ActionAimChangedEvent>(MarkDirty);
            EventBus.Unsubscribe<SkillSelectionChangedEvent>(MarkDirty);
            EventBus.Unsubscribe<ConsumableSelectionChangedEvent>(MarkDirty);
            EventBus.Unsubscribe<BattleDecidedEvent>(MarkDirty);
            Clear();
        }

        private void MarkDirty<T>(T e) => _dirty = true;

        /// <summary>Recomputes the forecast at the end of this frame (after every change of the frame).</summary>
        public void MarkDirty() => _dirty = true;

        private void LateUpdate()
        {
            if (SimMode.IsActive)
            {
                Clear();
                return;
            }

            // Tapping an enemy changes no game state (no event): follow it here.
            var inspected = GetInspectedEnemy();
            if (inspected != _lastInspected)
            {
                _lastInspected = inspected;
                _dirty = true;
            }

            if (_dirty)
            {
                _dirty = false;
                Refresh();
            }

            SyncGhostFrames();
        }

        /// <summary>Recomputes and redraws the forecast now.</summary>
        public void Refresh()
        {
            if (_grid == null || SimMode.IsActive || IsBattleDecided())
            {
                Clear();
                return;
            }

            _cleared = false;
            Action = GetAimedAction();
            Threat = ThreatForecast.Compute(_grid, Action);

            var settings = ForecastSettings.Current;
            DrawCells(settings);
            DrawBadges(settings);

            _usedTags = 0;
            _usedArrows = 0;
            _usedGhosts = 0;
            if (settings.ShowOrderOnStrikes)
                DrawStrikeOrders(settings);
            if (Action != null)
                DrawAction(settings);
            HideUnused();
        }

        private bool IsBattleDecided() => _turns != null && _turns.CurrentTurn == ETurnOwner.None;

        [CanBeNull]
        private static Enemy GetInspectedEnemy()
        {
            var player = PlayerCharacter.Active;
            return player != null && player.TryGetComponent(out PlayerCharacterController controller)
                ? controller.InspectedEnemy
                : null;
        }

        [CanBeNull]
        private ActionForecast GetAimedAction()
        {
            var player = PlayerCharacter.Active;
            if (player == null || player.IsDead ||
                !player.TryGetComponent(out PlayerCharacterController controller) || !controller.AimedCell.HasValue)
                return null;

            var target = controller.AimedCell.Value;
            if (controller.SelectedSkill != null)
                return ActionForecast.ForSkill(_grid, player, controller.SelectedSkill, target);
            if (controller.SelectedConsumable != null)
                return ActionForecast.ForConsumable(_grid, player, controller.SelectedConsumable, target);
            return null;
        }

        /// <summary>The strike marks and the damage each character would take (on its health bar).</summary>
        private void DrawCells(ForecastSettings settings)
        {
            var totals = new Dictionary<Character, int>();
            if (settings.ShowPredictedDamage)
            {
                if (Action != null)
                    AddAll(totals, Action.Damage);
                AddAll(totals, Threat.Damage);
            }

            // With an enemy inspected, only its own strike stays at full strength (none when it prepares nothing).
            HashSet<Vector2Int> focus = null;
            var inspected = GetInspectedEnemy();
            if (inspected != null)
            {
                focus = new HashSet<Vector2Int>();
                foreach (var strike in Threat.Telegraphs)
                {
                    if (strike.Caster == inspected)
                        focus.UnionWith(strike.Cells);
                }
            }

            var size = _grid.Size;
            for (var x = 0; x < size.x; x++)
            {
                for (var y = 0; y < size.y; y++)
                {
                    var position = new Vector2Int(x, y);
                    var cell = _grid.GetCell(position);
                    if (cell == null) continue;

                    cell.Warned = Threat.MarkedCells.Contains(position);
                    cell.WarnHit = Threat.HitCells.Contains(position);
                    cell.WarnDimmed = focus != null && !focus.Contains(position);

                    var content = cell.GetContent() as Character;
                    if (content == null || content.IsDead || !totals.TryGetValue(content, out var damage))
                    {
                        cell.SetPredictedDamage(0, false);
                        continue;
                    }

                    var lethal = Threat.Killed.Contains(content) || (Action != null && Action.Killed.Contains(content));
                    cell.SetPredictedDamage(damage, lethal);
                }
            }
        }

        private static void AddAll(Dictionary<Character, int> totals, Dictionary<Character, int> damage)
        {
            foreach (var (character, amount) in damage)
            {
                if (character == null) continue;

                totals.TryGetValue(character, out var total);
                totals[character] = total + amount;
            }
        }

        /// <summary>
        /// The acting order on each enemy (hidden on the ones that died or left) and the strike icon of the inspected one.
        /// </summary>
        private void DrawBadges(ForecastSettings settings)
        {
            var inspected = GetInspectedEnemy();
            var stale = new List<Enemy>(_badged);
            _badged.Clear();
            foreach (var (enemy, order) in Threat.Order)
            {
                if (enemy == null || !enemy.TryGetComponent(out EnemyIconsView icons)) continue;

                icons.SetActingOrder(settings.ShowActingOrder ? order : 0);
                icons.SetFocused(enemy == inspected);
                _badged.Add(enemy);
            }

            foreach (var enemy in stale)
            {
                if (enemy == null || _badged.Contains(enemy) || !enemy.TryGetComponent(out EnemyIconsView icons)) continue;

                icons.SetActingOrder(0);
                icons.SetFocused(false);
            }
        }

        /// <summary>
        /// The caster's number on the aimed cell of each strike. Several strikes on one cell sit side by side in acting
        /// order, <see cref="ForecastSettings.StrikeOrderSpacing"/> apart, so "1 3" never reads as "13".
        /// </summary>
        private void DrawStrikeOrders(ForecastSettings settings)
        {
            var perCell = new Dictionary<Vector2Int, int>();
            var ppu = GameConfigManager.Ppu;
            var strikes = new List<TelegraphForecast>(Threat.Telegraphs);
            strikes.Sort((a, b) => a.Order.CompareTo(b.Order));
            foreach (var strike in strikes)
            {
                var cell = _grid.GetCell(strike.TargetPos);
                if (cell == null) continue;

                perCell.TryGetValue(strike.TargetPos, out var index);
                perCell[strike.TargetPos] = index + 1;

                var tag = NextTag(settings);
                var offset = settings.StrikeOrderOffset + new Vector2(index * settings.StrikeOrderSpacing, 0f);
                tag.transform.position = cell.transform.position + new Vector3(offset.x / ppu, offset.y / ppu, 0f);
                tag.text = strike.Order.ToString();
                tag.color = strike.FiresNext ? settings.OrderColor : settings.LaterStrikeColor;
            }
        }

        /// <summary>Arrows for the pushes and pulls of the aimed action, ghosts where characters would end.</summary>
        private void DrawAction(ForecastSettings settings)
        {
            var ppu = GameConfigManager.Ppu;
            foreach (var move in Action.Moves)
            {
                var result = move.Result;
                if (result == null || result.Target == null || result.Direction == Vector2Int.zero) continue;

                var cell = _grid.GetCell(result.Start);
                var sprite = ForecastSprites.GetArrow(result.Direction);
                if (cell == null || sprite == null) continue;

                var arrow = NextArrow();
                arrow.sprite = sprite;
                arrow.color = settings.ArrowColor;
                // Grid y grows downward, the world y upward.
                var direction = new Vector3(result.Direction.x, -result.Direction.y, 0f).normalized;
                arrow.transform.position = cell.transform.position + direction * (settings.ArrowOffset / ppu);
            }

            foreach (var (character, final) in Action.Finals)
            {
                if (character == null || character.IsDead || Action.Killed.Contains(character) ||
                    final == character.CurrentGridPos) continue;

                var cell = _grid.GetCell(final);
                if (cell == null || !character.TryGetComponent(out SpriteRenderer source)) continue;

                var ghost = NextGhost(source);
                ghost.transform.position = cell.ContentRestPosition;
                ghost.transform.localScale = character.transform.lossyScale;
                var color = source.color;
                color.a *= settings.GhostAlpha;
                ghost.color = color;
            }
        }

        /// <summary>Ghosts follow the animation frame of the character they copy.</summary>
        private void SyncGhostFrames()
        {
            for (var i = 0; i < _usedGhosts; i++)
            {
                var source = _ghostSources[i];
                if (source == null) continue;

                _ghosts[i].sprite = source.sprite;
                _ghosts[i].flipX = source.flipX;
            }
        }

        private TextMeshPro NextTag(ForecastSettings settings)
        {
            if (_usedTags == _orderTags.Count)
            {
                var go = new GameObject("StrikeOrder");
                go.transform.SetParent(transform, false);
                var text = go.AddComponent<TextMeshPro>();
                if (_font == null)
                    _font = Resources.Load<TMP_FontAsset>(FontResource);
                if (_font != null)
                    text.font = _font;
                text.alignment = TextAlignmentOptions.Center;
                text.textWrappingMode = TextWrappingModes.NoWrap;
                text.rectTransform.sizeDelta = new Vector2(0.3f, 0.3f);
                text.sortingOrder = OverlaySortingOrder;
                _orderTags.Add(text);
            }

            var tag = _orderTags[_usedTags++];
            tag.fontSize = settings.OrderFontSize;
            tag.gameObject.SetActive(true);
            return tag;
        }

        private SpriteRenderer NextArrow()
        {
            if (_usedArrows == _arrows.Count)
            {
                var go = new GameObject("PushArrow");
                go.transform.SetParent(transform, false);
                var renderer = go.AddComponent<SpriteRenderer>();
                renderer.sortingOrder = OverlaySortingOrder;
                _arrows.Add(renderer);
            }

            var arrow = _arrows[_usedArrows++];
            arrow.enabled = true;
            return arrow;
        }

        private SpriteRenderer NextGhost(SpriteRenderer source)
        {
            if (_usedGhosts == _ghosts.Count)
            {
                var go = new GameObject("Ghost");
                go.transform.SetParent(transform, false);
                var renderer = go.AddComponent<SpriteRenderer>();
                _ghosts.Add(renderer);
                _ghostSources.Add(null);
            }

            var ghost = _ghosts[_usedGhosts];
            _ghostSources[_usedGhosts] = source;
            _usedGhosts++;
            ghost.sprite = source.sprite;
            ghost.flipX = source.flipX;
            ghost.sharedMaterial = source.sharedMaterial;
            ghost.sortingLayerID = source.sortingLayerID;
            ghost.sortingOrder = GhostSortingOrder;
            ghost.enabled = true;
            return ghost;
        }

        private void HideUnused()
        {
            for (var i = _usedTags; i < _orderTags.Count; i++)
            {
                if (_orderTags[i] != null)
                    _orderTags[i].gameObject.SetActive(false);
            }

            for (var i = _usedArrows; i < _arrows.Count; i++)
            {
                if (_arrows[i] != null)
                    _arrows[i].enabled = false;
            }

            for (var i = _usedGhosts; i < _ghosts.Count; i++)
            {
                if (_ghosts[i] != null)
                    _ghosts[i].enabled = false;
                _ghostSources[i] = null;
            }
        }

        /// <summary>Removes everything the forecast drew (battle decided, simulation, disabled).</summary>
        private void Clear()
        {
            Threat = null;
            Action = null;
            if (_cleared) return;

            _cleared = true;
            if (_grid != null)
            {
                var size = _grid.Size;
                for (var x = 0; x < size.x; x++)
                {
                    for (var y = 0; y < size.y; y++)
                    {
                        var cell = _grid.GetCell(new Vector2Int(x, y));
                        if (cell == null) continue;

                        cell.Warned = false;
                        cell.WarnHit = false;
                        cell.WarnDimmed = false;
                        cell.SetPredictedDamage(0, false);
                    }
                }
            }

            foreach (var enemy in _badged)
            {
                if (enemy == null || !enemy.TryGetComponent(out EnemyIconsView icons)) continue;

                icons.SetActingOrder(0);
                icons.SetFocused(false);
            }

            _badged.Clear();
            _usedTags = 0;
            _usedArrows = 0;
            _usedGhosts = 0;
            HideUnused();
        }
    }
}
