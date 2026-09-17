using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UIElements;

namespace GridBattle.UI
{
    [RequireComponent(typeof(PixelPerfectCamera))]
    [ExecuteAlways]
    public class PixelPerfectPanelScale : MonoBehaviour
    {
        [SerializeField] private PanelSettings panelSettings;

        private PixelPerfectCamera _pixelPerfectCamera;
        private int _lastRatio = -1;

        private void OnEnable()
        {
            _pixelPerfectCamera = GetComponent<PixelPerfectCamera>();
            if (panelSettings == null) return;
            panelSettings.scaleMode = PanelScaleMode.ConstantPixelSize;
            SyncScale();
        }

        private void LateUpdate()
        {
            SyncScale();
        }

        private void SyncScale()
        {
            if (panelSettings == null || _pixelPerfectCamera == null) return;

            int ratio;
            if (Application.isPlaying)
            {
                ratio = _pixelPerfectCamera.pixelRatio;
            }
            else
            {
                ratio = Mathf.Max(1, Mathf.Min(Screen.width / _pixelPerfectCamera.refResolutionX, Screen.height / _pixelPerfectCamera.refResolutionY));
            }

            if (ratio == _lastRatio) return;
            panelSettings.scale = ratio;
            _lastRatio = ratio;
        }
    }
}