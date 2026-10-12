using GridBattle.Gameplay.Monetization;
using UnityEditor;
using UnityEngine;

namespace GridBattle.Editor.Tools
{
    /// <summary>
    /// Editor shortcuts for testing monetization (GDD 7) without a store: mark the remove ads purchase as made or
    /// not in the player's profile, and forget the forced ad counters (the next ads follow the ramp of a new
    /// player). They change <c>profile.json</c>, the real profile of whoever plays in the Editor.
    /// </summary>
    public static class MonetizationMenu
    {
        [MenuItem("GridBattle/Monetization/Select Settings", priority = 40)]
        private static void SelectSettings()
        {
            Selection.activeObject = MonetizationSettings.Current;
        }

        [MenuItem("GridBattle/Monetization/Testing/Mark Ads Removed (profile)", priority = 41)]
        private static void MarkAdsRemoved()
        {
            MonetizationService.SetAdsRemovedForTesting(true);
            Debug.Log("Monetization: the profile now has the remove ads purchase.");
        }

        [MenuItem("GridBattle/Monetization/Testing/Show Ads Again (profile)", priority = 42)]
        private static void ShowAdsAgain()
        {
            MonetizationService.SetAdsRemovedForTesting(false);
            Debug.Log("Monetization: the profile no longer has the remove ads purchase.");
        }

        [MenuItem("GridBattle/Monetization/Testing/Reset Forced Ad Counters (profile)", priority = 43)]
        private static void ResetCounters()
        {
            MonetizationService.ResetAdCountersForTesting();
            Debug.Log("Monetization: the forced ad counters were reset.");
        }
    }
}
