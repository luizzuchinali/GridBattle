using System;
using GridBattle.Gameplay;
using GridBattle.Gameplay.Events;
using GridBattle.Gameplay.Simulation;
using GridBattle.UI.Screens;
using LitMotion;
using UnityEngine;
using UnityEngine.UIElements;
using ZS.UI.Navigation;

namespace GridBattle.UI.Vfx
{
    /// <summary>
    /// Presents XP dropped by enemies: each packet becomes an orb (UI Toolkit
    /// element) that flies in an arc from the enemy position to the point on the
    /// XP bar where the progress will be when it arrives. On arrival, the orb is
    /// removed and the packet is credited. How much XP and to whom is decided by
    /// XpRewardSystem; this is only presentation and delivery timing.
    /// </summary>
    [RequireComponent(typeof(UILayer))]
    public class XpOrbsVfx : MonoBehaviour
    {
        [SerializeField]
        private XpOrbsVfxSettings settings;

        private UIRoot _uiRoot;
        private Camera _camera;

        private void Awake()
        {
            _uiRoot = GetComponentInParent<UIRoot>();
        }

        private void OnEnable()
        {
            EventBus.Subscribe<XpRewardDroppedEvent>(OnXpRewardDropped);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<XpRewardDroppedEvent>(OnXpRewardDropped);
        }

        private void OnXpRewardDropped(XpRewardDroppedEvent e)
        {
            if (e.IsPresented || settings == null || SimMode.IsActive) return;
            if (_uiRoot == null || !_uiRoot.TryGetController(out GameScreenController gameScreen)) return;

            var layer = gameScreen.EffectsLayer;
            var xpBar = gameScreen.XpBar;
            if (layer?.panel == null || xpBar == null || !xpBar.TryGetTrack(out var track)) return;

            if (_camera == null)
                _camera = Camera.main;
            if (_camera == null) return;

            // The orbs appear when the killing blow lands on screen: a projectile or an area effect may still be on
            // its way to the enemy's cell (the same wait as its damage number and death effect).
            var grid = GridController.Instance;
            var hiddenFor = e.Source != null && grid != null ? grid.GetPendingImpactDelay(e.Source) : 0f;

            var start = RuntimePanelUtils.CameraTransformWorldToPanel(layer.panel, e.Origin, _camera);
            for (var i = 0; i < e.Packets.Count; i++)
            {
                var packet = e.Packets[i];
                var end = track.GetPoint(packet.ProgressAfter);
                var control = (start + end) / 2f + new Vector2(0f, -settings.ArcHeight);

                LaunchOrb(layer, start, control, end, hiddenFor, i * settings.StaggerDelay, () => e.Collect(packet));
            }

            e.MarkPresented();
        }

        /// <summary>
        /// Adds an orb at <paramref name="start"/>, hidden for <paramref name="hiddenFor"/> seconds; it waits there
        /// <paramref name="delay"/> more seconds and then flies to <paramref name="end"/>.
        /// </summary>
        private void LaunchOrb(VisualElement layer, Vector2 start, Vector2 control, Vector2 end, float hiddenFor,
            float delay, Action onArrived)
        {
            var orb = new VisualElement();
            orb.pickingMode = PickingMode.Ignore;
            orb.style.position = Position.Absolute;
            orb.style.width = settings.OrbSize;
            orb.style.height = settings.OrbSize;
            orb.style.backgroundImage = new StyleBackground(settings.OrbSprite);
            SetPosition(orb, start);
            layer.Add(orb);
            if (hiddenFor > 0f)
            {
                orb.visible = false;
                LMotion.Create(0f, 1f, hiddenFor)
                    .WithOnComplete(() => orb.visible = true)
                    .Bind(_ => { });
            }

            LMotion.Create(0f, 1f, settings.FlightDuration)
                .WithDelay(hiddenFor + delay)
                .WithEase(settings.FlightEase)
                .WithOnComplete(() =>
                {
                    orb.RemoveFromHierarchy();
                    onArrived();
                })
                .Bind(t => SetPosition(orb, EvaluateQuadraticBezier(start, control, end, t)));
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
