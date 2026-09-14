using UnityEngine;

namespace GridBattle.UI.Events
{
    public class StartScreenTapEvent
    {
        public StartScreenTapEvent(Vector2 tapPosition)
        {
            TapPosition = tapPosition;
        }

        public Vector2 TapPosition { get; }
    }
}