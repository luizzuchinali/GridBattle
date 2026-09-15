using System.Collections.Generic;
using UnityEngine;

namespace GridBattle.UI.Controllers
{
    public class NavigationController : MonoBehaviour
    {
        private Stack<View> Pages { get; } = new();

        public Awaitable PushAsync<T>(T view) where T : View
        {
            Pages.Push(view);
            view.Show();
        }
    }
}