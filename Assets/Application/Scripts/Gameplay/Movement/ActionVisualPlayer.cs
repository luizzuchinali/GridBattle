using System.Collections.Generic;
using GridBattle.Gameplay.Entities;
using GridBattle.Gameplay.Simulation;
using LitMotion;
using UnityEngine;

namespace GridBattle.Gameplay.Movement
{
    /// <summary>
    /// Plays an <see cref="ActionVisualDefinition"/> on the board: the caster's gesture, the projectile and the
    /// effect on every cell of the area, in the order that draws the shape. It also tells each affected
    /// character's hit reaction (and so its damage number) to wait for the impact on its cell. Only visual and owned
    /// by the <see cref="GridController"/>, like <see cref="GridEffectsAnimator"/>: the rules have already resolved
    /// when it plays, and the balance simulation skips it.
    /// </summary>
    public sealed class ActionVisualPlayer
    {
        /// <summary>Above the characters (99), below the floating damage numbers (9999).</summary>
        private const int SortingOrder = 500;

        private const float GlowSeconds = 0.2f;
        private const float GlowLeadSeconds = 0.1f;
        private const float DropSeconds = 0.08f;

        private readonly GridController _grid;
        private readonly List<GameObject> _live = new();

        public ActionVisualPlayer(GridController grid)
        {
            _grid = grid;
        }

        /// <summary>Divides every duration (1 = normal speed).</summary>
        public float SpeedMultiplier { get; set; } = 1f;

        /// <summary>Whether an effect is still on the board (the turn flow waits for it).</summary>
        public bool IsAnimating
        {
            get
            {
                _live.RemoveAll(o => o == null);
                return _live.Count > 0;
            }
        }

        /// <summary>
        /// Plays <paramref name="visual"/> for an action of <paramref name="caster"/> aimed at
        /// <paramref name="targetPos"/>. Call it before the damage is applied, with the cells and characters the
        /// action is about to affect.
        /// </summary>
        public void Play(ActionVisualDefinition visual, GridEntity caster, Vector2Int targetPos,
            IReadOnlyList<Vector2Int> area, IEnumerable<Character> affected, GridMovementSettings settings)
        {
            if (visual == null || caster == null || !Application.isPlaying || SimMode.IsActive) return;

            var speed = Mathf.Max(0.01f, SpeedMultiplier);
            var casterPos = caster.CurrentGridPos;
            var direction = GetCardinal(casterPos, targetPos);
            var time = 0f;

            switch (visual.CasterGesture)
            {
                case ECasterGesture.Lunge when targetPos != casterPos && settings != null:
                    _grid.PlayAttackAnimation(caster, targetPos);
                    time += settings.LungeDuration * 0.5f / speed;
                    break;
                case ECasterGesture.Glow:
                    PlayGlow(caster, visual.GlowColor, GlowSeconds / speed);
                    time += GlowLeadSeconds / speed;
                    break;
            }

            if (visual.HasProjectile && targetPos != casterPos)
            {
                var distance = Vector2.Distance(casterPos, targetPos);
                var travel = distance / visual.ProjectileCellsPerSecond / speed;
                SpawnProjectile(visual, casterPos, targetPos, direction, time, travel);
                time += travel;
            }

            var steps = GetSteps(visual.Order, area, casterPos, targetPos, direction);
            var impacts = new Dictionary<Vector2Int, (float time, Vector2Int facing)>();
            foreach (var cell in area)
            {
                var start = time + steps[cell] * visual.StepSeconds / speed;
                var facing = GetFacing(visual.CellFacing, cell, targetPos, direction);
                impacts[cell] = (SpawnCellEffect(visual, cell, facing, start, speed), facing ?? direction);
            }

            foreach (var character in affected)
            {
                if (character != null && impacts.TryGetValue(character.CurrentGridPos, out var impact))
                    _grid.QueueImpactAnimation(character, impact.facing, impact.time);
            }
        }

        // ------------------------------------------------------------------------------------ pieces

        private void PlayGlow(GridEntity caster, Color glow, float duration)
        {
            if (!caster.TryGetComponent(out SpriteRenderer sprite)) return;

            var baseColor = sprite.color;
            LMotion.Create(0f, 1f, duration)
                .WithOnComplete(() => sprite.color = baseColor)
                .Bind(t => sprite.color = Color.Lerp(baseColor, glow, 1f - Mathf.Abs(2f * t - 1f)))
                .AddTo(caster.gameObject);
        }

        private void SpawnProjectile(ActionVisualDefinition visual, Vector2Int from, Vector2Int to, Vector2Int direction,
            float start, float travel)
        {
            var renderer = CreateRenderer("Projectile", visual.ProjectileFrames[0], visual.Tint);
            if (visual.ProjectileFacesDirection)
                renderer.transform.rotation = GetRotation(direction);

            var a = GetCellPoint(from);
            var b = GetCellPoint(to);
            var arc = visual.ProjectileArc * GetCellHeight();
            var frames = visual.ProjectileFrames;
            var frameSeconds = visual.FrameSeconds;
            var total = start + travel;
            renderer.transform.position = a;

            LMotion.Create(0f, total, total)
                .WithOnComplete(() => Object.Destroy(renderer.gameObject))
                .Bind(elapsed =>
                {
                    var active = elapsed >= start;
                    renderer.enabled = active;
                    if (!active) return;

                    var p = Mathf.Clamp01((elapsed - start) / Mathf.Max(0.0001f, travel));
                    var position = Vector3.Lerp(a, b, p) + Vector3.up * (arc * 4f * p * (1f - p));
                    renderer.transform.position = Snap(position);
                    var frame = (int)((elapsed - start) / frameSeconds) % frames.Count;
                    if (frames[frame] != null)
                        renderer.sprite = frames[frame];
                })
                .AddTo(renderer.gameObject);
        }

        /// <summary>
        /// Plays the cell effect on <paramref name="cell"/> from <paramref name="start"/>, turned toward
        /// <paramref name="facing"/> (null = not turned); returns the impact time.
        /// </summary>
        private float SpawnCellEffect(ActionVisualDefinition visual, Vector2Int cell, Vector2Int? facing, float start,
            float speed)
        {
            var frames = visual.CellFrames;
            var drop = visual.DropPixels > 0 ? DropSeconds / speed : 0f;
            var frameSeconds = visual.FrameSeconds / speed;
            var impact = start + drop + visual.ImpactFrame * frameSeconds;
            if (frames.Count == 0 || frames[0] == null) return start;

            var renderer = CreateRenderer("CellEffect", frames[0], visual.Tint);
            if (facing.HasValue)
                renderer.transform.rotation = GetRotation(facing.Value);

            var center = GetCellPoint(cell);
            var dropHeight = visual.DropPixels / GridController.Ppu;
            var linger = visual.LingerSeconds / speed;
            var playEnd = start + drop + frames.Count * frameSeconds;
            var total = playEnd + linger;
            var baseColor = renderer.color;
            renderer.transform.position = center;

            LMotion.Create(0f, total, total)
                .WithOnComplete(() => Object.Destroy(renderer.gameObject))
                .Bind(elapsed =>
                {
                    var active = elapsed >= start;
                    renderer.enabled = active;
                    if (!active) return;

                    if (elapsed < start + drop)
                    {
                        var k = (elapsed - start) / drop;
                        renderer.transform.position = Snap(center + Vector3.up * (dropHeight * (1f - k * k)));
                        renderer.sprite = frames[0];
                        return;
                    }

                    renderer.transform.position = center;
                    var frame = Mathf.Min(frames.Count - 1, (int)((elapsed - start - drop) / frameSeconds));
                    if (frames[frame] != null)
                        renderer.sprite = frames[frame];

                    var color = baseColor;
                    if (linger > 0f && elapsed > playEnd)
                        color.a *= 1f - (elapsed - playEnd) / linger;
                    renderer.color = color;
                })
                .AddTo(renderer.gameObject);
            return impact;
        }

        private SpriteRenderer CreateRenderer(string name, Sprite sprite, Color tint)
        {
            var go = new GameObject("Vfx " + name);
            go.transform.SetParent(_grid.transform, false);
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = tint;
            renderer.sortingOrder = SortingOrder;
            renderer.enabled = false;
            _live.Add(go);
            return renderer;
        }

        // ------------------------------------------------------------------------------------ geometry

        /// <summary>Order step of every cell: 0 plays first; equal steps play together.</summary>
        private static Dictionary<Vector2Int, int> GetSteps(EVisualCellOrder order, IReadOnlyList<Vector2Int> area,
            Vector2Int casterPos, Vector2Int targetPos, Vector2Int direction)
        {
            var steps = new Dictionary<Vector2Int, int>();
            var min = int.MaxValue;
            foreach (var cell in area)
            {
                var step = order switch
                {
                    EVisualCellOrder.RippleFromTarget =>
                        Mathf.Max(Mathf.Abs(cell.x - targetPos.x), Mathf.Abs(cell.y - targetPos.y)),
                    EVisualCellOrder.AwayFromCaster =>
                        Mathf.Abs(cell.x - casterPos.x) + Mathf.Abs(cell.y - casterPos.y),
                    EVisualCellOrder.Sweep => direction.x != 0 ? cell.y : cell.x,
                    _ => 0,
                };
                steps[cell] = step;
                min = Mathf.Min(min, step);
            }

            foreach (var cell in area)
                steps[cell] -= min;
            return steps;
        }

        /// <summary>Direction the art of <paramref name="cell"/> turns to (null = it does not turn).</summary>
        private static Vector2Int? GetFacing(EVisualFacing facing, Vector2Int cell, Vector2Int targetPos,
            Vector2Int direction)
        {
            return facing switch
            {
                EVisualFacing.CasterToTarget => direction,
                EVisualFacing.AwayFromTarget => cell == targetPos ? direction : GetCardinal(targetPos, cell),
                _ => null,
            };
        }

        /// <summary>
        /// The dominant grid direction from <paramref name="from"/> to <paramref name="to"/> (horizontal when equal,
        /// right when the same cell).
        /// </summary>
        private static Vector2Int GetCardinal(Vector2Int from, Vector2Int to)
        {
            var delta = to - from;
            if (delta == Vector2Int.zero) return Vector2Int.right;
            return Mathf.Abs(delta.x) >= Mathf.Abs(delta.y)
                ? new Vector2Int(delta.x > 0 ? 1 : -1, 0)
                : new Vector2Int(0, delta.y > 0 ? 1 : -1);
        }

        /// <summary>Rotation that turns art drawn facing right toward a grid direction (grid y grows downward).</summary>
        private static Quaternion GetRotation(Vector2Int direction)
        {
            var angle = direction.x > 0 ? 0f : direction.x < 0 ? 180f : direction.y > 0 ? -90f : 90f;
            return Quaternion.Euler(0f, 0f, angle);
        }

        private Vector3 GetCellPoint(Vector2Int cell)
        {
            var c = _grid.GetCell(cell);
            return c != null ? Snap(c.transform.position) : Vector3.zero;
        }

        private float GetCellHeight()
        {
            var a = _grid.GetCell(Vector2Int.zero);
            var b = _grid.GetCell(Vector2Int.up);
            return a != null && b != null ? Mathf.Abs(a.transform.position.y - b.transform.position.y) : 0.5f;
        }

        private static Vector3 Snap(Vector3 position)
        {
            var ppu = GridController.Ppu;
            return new Vector3(Mathf.Round(position.x * ppu) / ppu, Mathf.Round(position.y * ppu) / ppu, position.z);
        }
    }
}
