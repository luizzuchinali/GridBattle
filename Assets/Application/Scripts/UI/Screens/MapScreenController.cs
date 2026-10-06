using GridBattle.Core;
using GridBattle.Gameplay.Events;
using GridBattle.Gameplay.Run;
using GridBattle.Managers;
using GridBattle.Managers.Audio;
using GridBattle.UI.Events;
using GridBattle.UI.Map;
using UnityEngine.Scripting;
using UnityEngine.UIElements;
using ZS.UI.Navigation;

namespace GridBattle.UI.Screens
{
    /// <summary>
    /// The run's node map (mapa_e_nos, interface 4.1 / 4.2): the HUD with HP, level, XP and depth, the scrollable
    /// map, and the preview panel of the node picked. Tapping an available node previews it
    /// (<c>RunManager.SelectNode</c>), the confirm button enters it (<c>RunManager.EnterNode</c>). The screen
    /// reads the run through the <see cref="RunManager"/> and refreshes itself from the run events; it does not
    /// navigate (the <c>GameFlowController</c> does).
    /// </summary>
    [Preserve]
    public sealed class MapScreenController : ViewController
    {
        private MapHudView _hud;
        private MapGraphView _graph;
        private NodePreviewView _preview;
        private bool _entering;

        /// <summary>The HUD of the map (null until the UI loads).</summary>
        public MapHudView Hud => _hud;

        /// <summary>The node map (null until the UI loads).</summary>
        public MapGraphView Graph => _graph;

        /// <summary>The preview panel of the picked node (null until the UI loads).</summary>
        public NodePreviewView Preview => _preview;

        protected override void OnCreate()
        {
            EventBus.Subscribe<MapOpenedEvent>(OnMapOpened);
            EventBus.Subscribe<NodeSelectedEvent>(OnNodeSelected);
            EventBus.Subscribe<NodeEnteredEvent>(OnNodeEntered);
            EventBus.Subscribe<RunProgressChangedEvent>(OnRunProgressChanged);
            EventBus.Subscribe<PlayerXpChangedEvent>(OnXpChanged);
            EventBus.Subscribe<ConsumableInventoryChangedEvent>(OnConsumablesChanged);
            Loc.LocaleChanged += OnLocaleChanged;
        }

        protected override void OnDestroy()
        {
            EventBus.Unsubscribe<MapOpenedEvent>(OnMapOpened);
            EventBus.Unsubscribe<NodeSelectedEvent>(OnNodeSelected);
            EventBus.Unsubscribe<NodeEnteredEvent>(OnNodeEntered);
            EventBus.Unsubscribe<RunProgressChangedEvent>(OnRunProgressChanged);
            EventBus.Unsubscribe<PlayerXpChangedEvent>(OnXpChanged);
            EventBus.Unsubscribe<ConsumableInventoryChangedEvent>(OnConsumablesChanged);
            Loc.LocaleChanged -= OnLocaleChanged;
        }

        protected override void OnBind(VisualElement root)
        {
            _hud = new MapHudView(root);
            _graph = new MapGraphView(root);
            _graph.NodeClicked += OnNodeClicked;
            _preview = new NodePreviewView(root);

            _preview.ConfirmButton.OnClick(OnConfirmClicked);
            root.Q<Button>("map-menu-button").OnClick(() =>
            {
                AudioManager.Play(ESfx.ButtonTap);
                EventBus.Raise(new MenuOpenedEvent());
            });
            root.Q<Button>("map-glossary-button").OnClick(() =>
            {
                AudioManager.Play(ESfx.ButtonTap);
                EventBus.Raise(new GlossaryRequestedEvent());
            });

            Refresh(true);
        }

        protected override void OnEnter(object args)
        {
            _entering = false;
            Refresh(true);
        }

        /// <summary>Rebuilds the HUD, the map and the preview from the run in progress.</summary>
        public void Refresh(bool focus)
        {
            if (_graph == null) return;

            var runs = RunManager.Instance;
            var run = runs != null ? runs.CurrentRun : null;

            _hud.Refresh();
            _preview.RefreshTexts();

            if (run == null)
            {
                _graph.Rebuild(new MapState(), new MapNodeState[0], -1, false);
                _preview.Clear();
                return;
            }

            var selected = runs.SelectedNode;
            _graph.Rebuild(run.Map, runs.GetAvailableNodes(), selected != null ? selected.Id : -1, focus);
            _preview.Show(selected);
        }

        private void OnMapOpened(MapOpenedEvent e)
        {
            _entering = false;
            if (IsVisible)
                Refresh(true);
        }

        private void OnNodeSelected(NodeSelectedEvent e)
        {
            if (_graph == null) return;

            _graph.SetSelected(e.Node.Id);
            _preview.Show(e.Node);
        }

        /// <summary>The node was entered: the preview stays but cannot be confirmed again.</summary>
        private void OnNodeEntered(NodeEnteredEvent e)
        {
            _entering = true;
            if (_preview != null)
                _preview.ConfirmButton.SetEnabled(false);
        }

        private void OnRunProgressChanged(RunProgressChangedEvent e)
        {
            _hud?.SetProgress(e.Depth, e.FloorCount, e.Hp, e.MaxHp);
        }

        private void OnXpChanged(PlayerXpChangedEvent e)
        {
            _hud?.SetXp(e.Level, e.CurrentXp, e.XpToNextLevel);
        }

        private void OnConsumablesChanged(ConsumableInventoryChangedEvent e)
        {
            _hud?.RefreshItems();
        }

        private void OnLocaleChanged()
        {
            _hud?.RefreshTexts();
            _preview?.RefreshTexts();
        }

        private void OnNodeClicked(int nodeId)
        {
            if (_entering) return;

            var runs = RunManager.Instance;
            if (runs != null)
                runs.SelectNode(nodeId);
        }

        private void OnConfirmClicked()
        {
            if (_entering) return;

            var runs = RunManager.Instance;
            var node = _preview.Node;
            if (runs == null || node == null) return;

            // Entering a node plays its own sound; the flow follows the events it raises.
            runs.EnterNode(node.Id);
        }
    }
}
