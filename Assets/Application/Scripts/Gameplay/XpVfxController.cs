using System.Collections.Generic;
using GridBattle.Gameplay.Entities;
using GridBattle.Gameplay.Events;
using LitMotion;
using UnityEngine;
using UnityEngine.UIElements;

namespace GridBattle.Gameplay
{
    //TODO: Melhorar a implementação desse cara. Deve ser transformado em um cara apartado do grid.
    /// <summary>
    /// VFX de ganho de XP: quando um inimigo morre, spawna "células" de XP
    /// (sprite XpVFX) que voam em arco até a barra de XP da UI. Cada célula
    /// que chega concede sua fração de XP ao PlayerCharacter — a barra cresce
    /// conforme as células chegam.
    /// </summary>
    public class XpVfxController : MonoBehaviour
    {
        [SerializeField]
        private Sprite xpCellSprite;

        [SerializeField]
        private float flightDuration = 0.6f;

        [SerializeField]
        private float staggerDelay = 0.08f;

        [SerializeField]
        private float arcHeight = 60f;

        [SerializeField]
        private float cellSize = 16f;

        [SerializeField]
        private int xpPerCell = 5;

        private const float XpBarFrameWidth = 112f;
        private const float XpBarMaxWidth = 110f;

        private VisualElement _root;
        private IPanel _panel;
        private PlayerCharacter _player;
        private Camera _camera;
        private readonly List<PanelRenderer> _registeredPanels = new();

        private void Awake()
        {
            EventBus.Subscribe<CharacterDiedEvent>(OnCharacterDied);

            foreach (var panelRenderer in FindObjectsByType<PanelRenderer>())
            {
                panelRenderer.RegisterUIReloadCallback(OnUIReload);
                _registeredPanels.Add(panelRenderer);
            }
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<CharacterDiedEvent>(OnCharacterDied);

            foreach (var panelRenderer in _registeredPanels)
                panelRenderer.UnregisterUIReloadCallback(OnUIReload);
            _registeredPanels.Clear();
        }

        private void OnUIReload(PanelRenderer panelRenderer, VisualElement rootElement, int version)
        {
            // Só interessa o painel da GameScreen (aquele que contém a barra de XP).
            if (rootElement.Q<VisualElement>("xp-bar-progress") == null) return;

            _root = rootElement;
            _panel = rootElement.panel;
        }

        private void OnCharacterDied(CharacterDiedEvent e)
        {
            if (e.Character is not Enemy enemy) return;

            var enemyConfig = enemy.Config as EnemyConfig;
            if (enemyConfig == null) return;

            SpawnXpCells(enemy.transform.position, enemyConfig.XpReward);
        }

        private void SpawnXpCells(Vector3 worldPos, int xpReward)
        {
            if (_root == null || _panel == null) return;

            _camera ??= Camera.main;
            if (_camera == null) return;

            _player ??= FindAnyObjectByType<PlayerCharacter>();
            if (_player == null) return;

            var toNext = _player.XpToNextLevel;
            if (toNext <= 0) return;

            var frame = _root.Q<VisualElement>("xp-bar-detail-2");
            if (frame == null) return;

            var start = RuntimePanelUtils.CameraTransformWorldToPanel(_panel, worldPos, _camera);

            // Track da barra derivado do frame (112px) por proporção: o
            // progresso ocupa 110px com 1px de moldura de cada lado. Isso é
            // imune ao scale do painel, pois usamos razões, não px absolutos.
            var frameRect = frame.worldBound;
            var trackLeft = frameRect.xMin + frameRect.width * (1f / XpBarFrameWidth);
            var trackMaxWidth = frameRect.width * (XpBarMaxWidth / XpBarFrameWidth);
            var centerY = frameRect.center.y;

            var playerXp = _player.CurrentXp;
            var cellCount = Mathf.Max(1, Mathf.CeilToInt((float)xpReward / xpPerCell));
            var perCell = xpReward / cellCount;
            var remainder = xpReward % cellCount;

            var accumulated = 0;
            for (var i = 0; i < cellCount; i++)
            {
                var amount = perCell + (i < remainder ? 1 : 0);
                accumulated += amount;

                // O alvo é exatamente onde a borda direita da barra estará
                // quando ESTA célula chegar (XP do player + células anteriores).
                var progress = Mathf.Clamp01((float)(playerXp + accumulated) / toNext);
                var end = new Vector2(trackLeft + progress * trackMaxWidth, centerY);
                var control = (start + end) / 2f + new Vector2(0f, -arcHeight);

                SpawnCell(start, control, end, i * staggerDelay, amount);
            }
        }

        private void SpawnCell(Vector2 start, Vector2 control, Vector2 end, float delay, int xpAmount)
        {
            var cell = new VisualElement();
            cell.pickingMode = PickingMode.Ignore;
            cell.style.position = Position.Absolute;
            cell.style.width = cellSize;
            cell.style.height = cellSize;
            cell.style.backgroundImage = new StyleBackground(xpCellSprite);
            SetPosition(cell, start);
            _root.Add(cell);

            LMotion.Create(0f, 1f, flightDuration)
                .WithDelay(delay)
                .WithEase(Ease.InQuad)
                .WithOnComplete(() =>
                {
                    cell.RemoveFromHierarchy();
                    _player?.GainXp(xpAmount);
                })
                .Bind(t => SetPosition(cell, EvaluateQuadraticBezier(start, control, end, t)));
        }

        private static void SetPosition(VisualElement element, Vector2 position)
        {
            element.style.left = position.x;
            element.style.top = position.y;
        }

        private static Vector2 EvaluateQuadraticBezier(Vector2 start, Vector2 control, Vector2 end, float t)
        {
            var u = 1f - t;
            return u * u * start + 2f * u * t * control + t * t * end;
        }
    }
}