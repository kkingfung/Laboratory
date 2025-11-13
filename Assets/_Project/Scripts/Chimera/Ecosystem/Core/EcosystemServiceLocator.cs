using UnityEngine;
using Laboratory.Chimera.Ecosystem.Systems;

namespace Laboratory.Chimera.Ecosystem.Core
{
    /// <summary>
    /// Service locator pattern for ecosystem systems.
    /// Provides O(1) access to ecosystem subsystems without FindObjectOfType overhead.
    /// Replaces expensive FindObjectOfType calls with efficient static references.
    /// </summary>
    public static class EcosystemServiceLocator
    {
        private static ClimateEvolutionSystem _climateSystem;
        private static BiomeTransitionSystem _biomeSystem;
        private static ResourceFlowSystem _resourceSystem;
        private static SpeciesInteractionSystem _speciesSystem;
        private static EcosystemHealthMonitor _healthMonitor;
        private static CatastropheSystem _catastropheSystem;

        // Climate System
        public static void RegisterClimate(ClimateEvolutionSystem system)
        {
            if (_climateSystem != null && _climateSystem != system)
                Debug.LogWarning($"[EcosystemServiceLocator] Replacing existing ClimateEvolutionSystem");
            _climateSystem = system;
        }

        public static ClimateEvolutionSystem Climate
        {
            get
            {
                if (_climateSystem == null)
                    Debug.LogError("[EcosystemServiceLocator] ClimateEvolutionSystem not registered. Ensure it's initialized in Awake()");
                return _climateSystem;
            }
        }

        // Biome System
        public static void RegisterBiome(BiomeTransitionSystem system)
        {
            if (_biomeSystem != null && _biomeSystem != system)
                Debug.LogWarning($"[EcosystemServiceLocator] Replacing existing BiomeTransitionSystem");
            _biomeSystem = system;
        }

        public static BiomeTransitionSystem Biome
        {
            get
            {
                if (_biomeSystem == null)
                    Debug.LogError("[EcosystemServiceLocator] BiomeTransitionSystem not registered. Ensure it's initialized in Awake()");
                return _biomeSystem;
            }
        }

        // Resource System
        public static void RegisterResource(ResourceFlowSystem system)
        {
            if (_resourceSystem != null && _resourceSystem != system)
                Debug.LogWarning($"[EcosystemServiceLocator] Replacing existing ResourceFlowSystem");
            _resourceSystem = system;
        }

        public static ResourceFlowSystem Resource
        {
            get
            {
                if (_resourceSystem == null)
                    Debug.LogError("[EcosystemServiceLocator] ResourceFlowSystem not registered. Ensure it's initialized in Awake()");
                return _resourceSystem;
            }
        }

        // Species System
        public static void RegisterSpecies(SpeciesInteractionSystem system)
        {
            if (_speciesSystem != null && _speciesSystem != system)
                Debug.LogWarning($"[EcosystemServiceLocator] Replacing existing SpeciesInteractionSystem");
            _speciesSystem = system;
        }

        public static SpeciesInteractionSystem Species
        {
            get
            {
                if (_speciesSystem == null)
                    Debug.LogError("[EcosystemServiceLocator] SpeciesInteractionSystem not registered. Ensure it's initialized in Awake()");
                return _speciesSystem;
            }
        }

        // Health Monitor
        public static void RegisterHealthMonitor(EcosystemHealthMonitor monitor)
        {
            if (_healthMonitor != null && _healthMonitor != monitor)
                Debug.LogWarning($"[EcosystemServiceLocator] Replacing existing EcosystemHealthMonitor");
            _healthMonitor = monitor;
        }

        public static EcosystemHealthMonitor HealthMonitor
        {
            get
            {
                if (_healthMonitor == null)
                    Debug.LogError("[EcosystemServiceLocator] EcosystemHealthMonitor not registered. Ensure it's initialized in Awake()");
                return _healthMonitor;
            }
        }

        // Catastrophe System
        public static void RegisterCatastrophe(CatastropheSystem system)
        {
            if (_catastropheSystem != null && _catastropheSystem != system)
                Debug.LogWarning($"[EcosystemServiceLocator] Replacing existing CatastropheSystem");
            _catastropheSystem = system;
        }

        public static CatastropheSystem Catastrophe
        {
            get
            {
                if (_catastropheSystem == null)
                    Debug.LogError("[EcosystemServiceLocator] CatastropheSystem not registered. Ensure it's initialized in Awake()");
                return _catastropheSystem;
            }
        }

        /// <summary>
        /// Check if all essential systems are registered
        /// </summary>
        public static bool AreEssentialSystemsRegistered()
        {
            return _climateSystem != null &&
                   _biomeSystem != null &&
                   _resourceSystem != null &&
                   _speciesSystem != null;
        }

        /// <summary>
        /// Clear all registrations (useful for scene unload/cleanup)
        /// </summary>
        public static void Clear()
        {
            _climateSystem = null;
            _biomeSystem = null;
            _resourceSystem = null;
            _speciesSystem = null;
            _healthMonitor = null;
            _catastropheSystem = null;
        }

        /// <summary>
        /// Get status of all registered systems for debugging
        /// </summary>
        public static string GetRegistrationStatus()
        {
            return $"Ecosystem Service Locator Status:\n" +
                   $"  Climate: {(_climateSystem != null ? "✓" : "✗")}\n" +
                   $"  Biome: {(_biomeSystem != null ? "✓" : "✗")}\n" +
                   $"  Resource: {(_resourceSystem != null ? "✓" : "✗")}\n" +
                   $"  Species: {(_speciesSystem != null ? "✓" : "✗")}\n" +
                   $"  Health Monitor: {(_healthMonitor != null ? "✓" : "✗")}\n" +
                   $"  Catastrophe: {(_catastropheSystem != null ? "✓" : "✗")}";
        }
    }
}
