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
    /// Presents XP dropped by enemies as a burst (Vampire Survivors style): every
    /// packet becomes an orb (UI Toolkit element); all of a kill's orbs pop out of
    /// the enemy together, scatter in a small explosion, hold for a moment and fly
    /// together in an arc to the point on the XP bar where the progress will be when
    /// each one arrives. On arrival, the orb is removed and its packet is credited.
    /// The whole delivery takes the same short time whatever the amount of XP. How
    /// much XP and to whom is decided by XpRewardSystem; this is only presentation
    /// and delivery timing.
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

            var count = e.Packets != null ? e.Packets.Count : 0;
            if (count == 0) return;

            var start = Snap(RuntimePanelUtils.CameraTransformWorldToPanel(layer.panel, e.Origin, _camera));
            var angleStep = 2f * Mathf.PI / count;
            var firstAngle = UnityEngine.Random.Range(0f, 2f * Mathf.PI);

            var bursts = new Action[count];
            for (var i = 0; i < count; i++)
            {
                var packet = e.Packets[i];

                // Evenly spread directions with some jitter, so the orbs never clump on one side.
                var jitter = UnityEngine.Random.Range(-0.5f, 0.5f) * settings.BurstAngleJitter * angleStep;
                var angle = firstAngle + i * angleStep + jitter;
                var radius = Mathf.Max(0f, settings.BurstRadius +
                                           UnityEngine.Random.Range(-1f, 1f) * settings.BurstRadiusVariance);
                var scattered = Snap(start + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius);

                var end = track.GetPoint(packet.ProgressAfter);
                var control = (scattered + end) / 2f + new Vector2(0f, -settings.ArcHeight);
                var launchDelay = settings.HoldDuration + UnityEngine.Random.Range(0f, settings.LaunchJitter);

                var orb = CreateOrb(layer, start, hiddenFor > 0f);
                bursts[i] = () => Burst(orb, start, scattered, control, end, launchDelay, () => e.Collect(packet));
            }

            e.MarkPresented();

            // All of the kill's orbs burst at the same moment.
            if (hiddenFor <= 0f)
            {
                foreach (var launch in bursts)
                    launch();
                return;
            }

            LMotion.Create(0f, 1f, hiddenFor)
                .WithOnComplete(() =>
                {
                    foreach (var launch in bursts)
                        launch();
                })
                .Bind(_ => { });
        }

        /// <summary>
        /// Adds an orb centered on <paramref name="position"/>, hidden while <paramref name="hidden"/>.
        /// </summary>
        private VisualElement CreateOrb(VisualElement layer, Vector2 position, bool hidden)
        {
            var orb = new VisualElement();
            orb.pickingMode = PickingMode.Ignore;
            orb.style.position = Position.Absolute;
            orb.style.width = settings.OrbSize;
            orb.style.height = settings.OrbSize;
            orb.style.backgroundImage = new StyleBackground(settings.OrbSprite);
            orb.visible = !hidden;
            SetPosition(orb, position);
            layer.Add(orb);
            return orb;
        }

        /// <summary>
        /// Shows the orb and scatters it from <paramref name="start"/> to <paramref name="scattered"/>; after
        /// <paramref name="launchDelay"/> more seconds it flies to <paramref name="end"/>. Each stage starts from the
        /// previous one's completion, so no motion writes the position while another one runs.
        /// </summary>
        private void Burst(VisualElement orb, Vector2 start, Vector2 scattered, Vector2 control, Vector2 end,
            float launchDelay, Action onArrived)
        {
            orb.visible = true;
            LMotion.Create(0f, 1f, settings.BurstDuration)
                .WithEase(settings.BurstEase)
                .WithOnComplete(() =>
                {
                    SetPosition(orb, scattered);
                    LMotion.Create(0f, 1f, settings.FlightDuration)
                        .WithDelay(launchDelay)
                        .WithEase(settings.FlightEase)
                        .WithOnComplete(() =>
                        {
                            orb.RemoveFromHierarchy();
                            onArrived();
                        })
                        .Bind(t => SetPosition(orb, EvaluateQuadraticBezier(scattered, control, end, t)));
                })
                .Bind(t => SetPosition(orb, Vector2.LerpUnclamped(start, scattered, t)));
        }

        /// <summary>
        /// Places the orb's center on <paramref name="position"/>, snapped to whole panel px (pixel perfect).
        /// </summary>
        private void SetPosition(VisualElement element, Vector2 position)
        {
            var half = settings.OrbSize / 2f;
            element.style.left = Mathf.Round(position.x - half);
            element.style.top = Mathf.Round(position.y - half);
        }

        private static Vector2 Snap(Vector2 position) => new(Mathf.Round(position.x), Mathf.Round(position.y));

        private static Vector2 EvaluateQuadraticBezier(Vector2 start, Vector2 control, Vector2 end, float t)
        {
            var u = 1f - t;
            return u * u * start + 2f * u * t * control + t * t * end;
        }
    }
}
