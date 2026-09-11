using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace GridBattle
{
    [RequireComponent(typeof(PanelRenderer))]
    public class MainMenuScreen : MonoBehaviour
    {
        private PanelRenderer _panelRenderer;

        private void Awake()
        {
            _panelRenderer = GetComponent<PanelRenderer>();
        }

        private void OnEnable()
        {
            
        }

        private void OnDisable()
        {
            
        }
    }
}