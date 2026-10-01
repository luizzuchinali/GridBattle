using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using ZS.UI.Navigation;
using ZS.UI.Navigation.Transitions;

namespace ZS.UI.Tests
{
    public class NavigatorTests
    {
        private readonly List<Object> _assets = new();
        private FakeHost _host;

        private Navigator Navigator => _host.Navigator;
        private static List<string> Log => LogController.Log;

        [SetUp]
        public void SetUp()
        {
            Log.Clear();
            _host = new FakeHost();
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var asset in _assets)
                Object.DestroyImmediate(asset);
            _assets.Clear();
        }

        [Test]
        public void Push_ShowsNewView_HidesAndKeepsPrevious()
        {
            var a = Define<LogController>("A");
            var b = Define<LogController>("B");

            Navigator.Push(a);
            Navigator.Push(b);

            Assert.AreEqual(2, Navigator.Depth);
            Assert.AreEqual("B", Navigator.Current.Definition.name);
            Assert.IsFalse(Navigator.Stack[0].IsVisible);
            Assert.IsTrue(Navigator.Stack[1].IsVisible);
            Assert.AreEqual(DisplayStyle.None, Navigator.Stack[0].Root.style.display.value);
        }

        [Test]
        public void Push_RunsLifecycleInOrder()
        {
            var a = Define<LogController>("A");
            var b = Define<LogController>("B");
            Navigator.Push(a);
            Log.Clear();

            Navigator.Push(b, "args");

            CollectionAssert.AreEqual(
                new[] { "B.Create", "B.Bind", "B.Enter(args)", "A.Exit", "A.Exited", "B.Entered" }, Log);
        }

        [Test]
        public void Pop_ReturnsToPrevious_AndDestroysPoppedView()
        {
            var a = Define<LogController>("A");
            var b = Define<LogController>("B");
            Navigator.Push(a);
            Navigator.Push(b);
            var bRoot = Navigator.Current.Root;
            Log.Clear();

            Navigator.Pop();

            Assert.AreEqual("A", Navigator.Current.Definition.name);
            Assert.IsTrue(Navigator.Current.IsVisible);
            Assert.IsNull(bRoot.parent, "Popped view elements must be removed.");
            CollectionAssert.AreEqual(
                new[] { "A.Enter()", "B.Exit", "B.Exited", "A.Entered", "B.Destroy" }, Log);
        }

        [Test]
        public void Replace_DestroysReplacedView()
        {
            var a = Define<LogController>("A");
            var b = Define<LogController>("B");
            Navigator.Push(a);

            Navigator.Replace(b);

            Assert.AreEqual(1, Navigator.Depth);
            Assert.AreEqual("B", Navigator.Current.Definition.name);
            Assert.Contains("A.Destroy", Log);
            Assert.AreEqual(1, _host.Live.Count);
        }

        [Test]
        public void PopTo_RemovesEverythingAboveTarget()
        {
            var a = Define<LogController>("A");
            Navigator.Push(a);
            Navigator.Push(Define<LogController>("B"));
            Navigator.Push(Define<LogController>("C"));

            Navigator.PopTo(a);

            Assert.AreEqual(1, Navigator.Depth);
            Assert.AreEqual("A", Navigator.Current.Definition.name);
            Assert.Contains("B.Destroy", Log);
            Assert.Contains("C.Destroy", Log);
        }

        [UnityTest]
        public IEnumerator IgnorePolicy_DropsRequestsDuringTransition()
        {
            var transition = Asset(ScriptableObject.CreateInstance<ManualTransition>());
            var a = Define<LogController>("A");
            var b = Define<LogController>("B", transition);
            Navigator.Push(a);

            Navigator.Push(b);
            Assert.IsTrue(Navigator.IsTransitioning);
            Navigator.Push(Define<LogController>("C"));

            transition.Finish();
            yield return null;

            Assert.IsFalse(Navigator.IsTransitioning);
            Assert.AreEqual(2, Navigator.Depth);
            Assert.AreEqual("B", Navigator.Current.Definition.name);
            Assert.IsFalse(Log.Any(entry => entry.StartsWith("C.")));
        }

        [UnityTest]
        public IEnumerator QueuePolicy_RunsRequestAfterTransition()
        {
            _host = new FakeHost(ConcurrentNavigationPolicy.Queue);
            var transition = Asset(ScriptableObject.CreateInstance<ManualTransition>());
            Navigator.Push(Define<LogController>("A"));
            Navigator.Push(Define<LogController>("B", transition));
            Navigator.Push(Define<LogController>("C"));

            Assert.AreEqual(2, Navigator.Depth);
            transition.Finish();
            yield return null;

            Assert.AreEqual(3, Navigator.Depth);
            Assert.AreEqual("C", Navigator.Current.Definition.name);
        }

        [Test]
        public void Transition_SwapsViewsWhenItEnds()
        {
            var transition = Asset(ScriptableObject.CreateInstance<ManualTransition>());
            Navigator.Push(Define<LogController>("A"));
            Navigator.Push(Define<LogController>("B", transition));

            Assert.IsTrue(Navigator.Stack[0].IsVisible, "Previous view stays until the transition swaps.");
            Assert.IsFalse(Navigator.Stack[1].IsVisible);

            transition.Finish();

            Assert.IsFalse(Navigator.Stack[0].IsVisible);
            Assert.IsTrue(Navigator.Stack[1].IsVisible);
        }

        [Test]
        public void Modal_ReturnsResult_AndRemovesBackdrop()
        {
            Navigator.Push(Define<LogController>("Screen"));
            var confirm = Define<ConfirmController>("Confirm");

            var pending = Navigator.ShowModal<bool>(confirm);
            var modal = (ConfirmController)Navigator.Modals.Single();
            Assert.IsNotNull(modal.Context.Navigator);
            Assert.AreEqual(1, _host.Container.Query(className: "zs-modal-backdrop").ToList().Count);

            modal.Answer(true);

            var awaiter = pending.GetAwaiter();
            Assert.IsTrue(awaiter.IsCompleted);
            Assert.IsTrue(awaiter.GetResult());
            Assert.IsFalse(Navigator.HasModal);
            Assert.AreEqual(0, _host.Container.Query(className: "zs-modal-backdrop").ToList().Count);
        }

        [Test]
        public void Modal_ClosedWithoutResult_ReturnsDefault()
        {
            var confirm = Define<ConfirmController>("Confirm");
            var pending = Navigator.ShowModal<bool>(confirm);

            Navigator.CloseModal(Navigator.Modals.Single());

            Assert.IsFalse(pending.GetAwaiter().GetResult());
        }

        [Test]
        public void HandleBack_ClosesModal_ThenPops_ThenReportsRoot()
        {
            Navigator.Push(Define<LogController>("A"));
            Navigator.Push(Define<LogController>("B"));
            Navigator.ShowModal(Define<LogController>("M"));

            Assert.IsTrue(Navigator.HandleBack());
            Assert.IsFalse(Navigator.HasModal);

            Assert.IsTrue(Navigator.HandleBack());
            Assert.AreEqual("A", Navigator.Current.Definition.name);

            Assert.IsFalse(Navigator.HandleBack());
        }

        [Test]
        public void HandleBack_ControllerCanConsumeIt()
        {
            Navigator.Push(Define<LogController>("A"));
            Navigator.Push(Define<BackConsumingController>("B"));

            Assert.IsTrue(Navigator.HandleBack());

            Assert.AreEqual(2, Navigator.Depth);
            Assert.AreEqual(1, ((BackConsumingController)Navigator.Current).BackCount);
        }

        [Test]
        public void Pages_AreNestedInPageHost_AndPoppedByBackFirst()
        {
            Navigator.Push(Define<LogController>("Root"));
            Navigator.Push(Define<PagesController>("Settings"));
            var pages = Navigator.Current.Context.GetPageNavigator("pages");

            pages.Push(Define<LogController>("General"));
            pages.Push(Define<LogController>("Audio"));

            Assert.AreEqual(2, pages.HostElement.childCount);
            Assert.IsTrue(pages.Current.IsVisible);

            Assert.IsTrue(Navigator.HandleBack());
            Assert.AreEqual("General", pages.Current.Definition.name);
            Assert.AreEqual("Settings", Navigator.Current.Definition.name);

            Assert.IsTrue(Navigator.HandleBack());
            Assert.AreEqual("Root", Navigator.Current.Definition.name);
            Assert.Contains("General.Destroy", Log, "Pages are destroyed with their view.");
        }

        [Test]
        public void ViewWithoutUxml_GetsEmptyRootElement()
        {
            Navigator.Push(Define<LogController>("Code"));

            Assert.IsNotNull(Navigator.Current.Root);
            Assert.AreEqual(_host.Container, Navigator.Current.Root.parent);
        }

        private ViewDefinition Define<TController>(string name, ViewTransition transition = null)
            where TController : ViewController
        {
            var definition = Asset(ScriptableObject.CreateInstance<ViewDefinition>());
            definition.name = name;
            definition.SetForTests(typeof(TController), null, ViewMountMode.Instantiate, transition);
            return definition;
        }

        private T Asset<T>(T asset) where T : Object
        {
            _assets.Add(asset);
            return asset;
        }
    }
}
