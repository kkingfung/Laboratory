using UnityEngine;
using System.Collections.Generic;

namespace Laboratory.Core.Configuration
{
    /// <summary>
    /// Master configuration ScriptableObject that orchestrates all Project Chimera systems.
    /// Drop this into any scene to get full system integration with one click.
    /// </summary>
    [CreateAssetMenu(fileName = "Chimera Game Config", menuName = "Chimera/Master Game Configuration", order = 0)]
    public class ChimeraGameConfig : ScriptableObject
    {
        [Header("🎮 Core Game Settings")]
        [SerializeField] public string gameVersion = "1.0.0";
        [SerializeField] public bool enableDebugMode = true;
        [SerializeField] public bool enablePerformanceMode = false;
        [SerializeField] [Range(30, 144)] public int targetFramerate = 60;

        [Header("🧬 Species & Genetics")]
        [SerializeField] public ScriptableObject[] availableSpecies = new ScriptableObject[0];
        [SerializeField] public ScriptableObject[] availableBiomes = new ScriptableObject[0];
        [SerializeField] [Range(0.001f, 0.1f)] public float globalMutationRate = 0.02f;
        [SerializeField] [Range(1, 20)] public int maxGenerations = 10;

        [Header("🤖 AI Configuration")]
        [SerializeField] public ScriptableObject pathfindingConfig;
        [SerializeField] [Range(10, 1000)] public int maxSimultaneousCreatures = 500;
        [SerializeField] [Range(0.1f, 5f)] public float aiUpdateFrequency = 1f;

        [Header("🌍 World & Environment")]
        [SerializeField] [Range(100, 10000)] public float worldSize = 1000f;
        [SerializeField] [Range(1, 10)] public int biomeCount = 3;
        [SerializeField] public bool enableDynamicWeather = true;
        [SerializeField] public bool enableSeasonalChanges = true;

        [Header("⚡ Performance Settings")]
        [SerializeField] [Range(1, 100)] public int ecsJobBatchSize = 32;
        [SerializeField] public bool enableObjectPooling = true;
        [SerializeField] public bool enableLODSystem = true;
        [SerializeField] [Range(10f, 500f)] public float cullingDistance = 200f;

        [Header("🌐 Networking")]
        [SerializeField] public bool enableMultiplayer = false;
        [SerializeField] [Range(2, 100)] public int maxPlayersPerServer = 20;
        [SerializeField] [Range(10, 120)] public int networkTickRate = 30;

        // Runtime validation
        private void OnValidate()
        {
            if (availableSpecies.Length == 0)
            {
                Debug.LogWarning("ChimeraGameConfig: No species configured! Create some species ScriptableObject assets.");
            }

            if (maxSimultaneousCreatures > 1000 && !enablePerformanceMode)
            {
                Debug.LogWarning("ChimeraGameConfig: High creature count without performance mode may cause lag.");
            }
        }

        /// <summary>
        /// Get species config by name for runtime spawning
        /// </summary>
        public ScriptableObject GetSpeciesByName(string speciesName)
        {
            foreach (var species in availableSpecies)
            {
                if (species != null && species.name == speciesName)
                    return species;
            }
            return null;
        }

        /// <summary>
        /// Get biome config by index for environment spawning
        /// </summary>
        public ScriptableObject GetBiomeByIndex(int biomeIndex)
        {
            if (biomeIndex >= 0 && biomeIndex < availableBiomes.Length)
                return availableBiomes[biomeIndex];
            return null;
        }

        /// <summary>
        /// Calculate performance-optimized batch size based on creature count
        /// </summary>
        public int GetOptimalBatchSize()
        {
            if (enablePerformanceMode)
                return Mathf.Min(ecsJobBatchSize * 2, 64);
            return ecsJobBatchSize;
        }
    }
}