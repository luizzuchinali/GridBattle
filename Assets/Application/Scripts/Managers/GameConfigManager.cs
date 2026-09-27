using UnityEngine;

namespace GridBattle.Managers
{
    public static class GameConfigManager
    {
        public static readonly float Ppu = 100f;

        private const int MobileTargetFrameRate = 60;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
        private static void ApplyMobileTargetFrameRate()
        {
            if (Application.isMobilePlatform)
            {
                Application.targetFrameRate = MobileTargetFrameRate;
            }
        }
    }
}