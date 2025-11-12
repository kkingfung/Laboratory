using UnityEngine;
using Laboratory.Core.Diagnostics;

namespace Laboratory.Core.Progression
{
    /// <summary>
    /// Test class to verify Laboratory.Systems types are accessible
    /// </summary>
    public class TestSystemsAccess : MonoBehaviour
    {
        public void TestAccess()
        {
            // Try to use Laboratory.Systems types
            // Test with reflection to avoid hard dependencies
            var analyticsType = System.Type.GetType("Laboratory.Systems.Analytics.PlayerAnalyticsTracker, Laboratory.Systems");
            var breedingType = System.Type.GetType("Laboratory.Systems.Breeding.AdvancedBreedingSimulator, Laboratory.Systems");

            bool analyticsExists = analyticsType != null;
            bool breedingExists = breedingType != null;

            DebugManager.LogInfo($"Systems accessible: Analytics={analyticsExists}, Breeding={breedingExists}");
        }
    }
}