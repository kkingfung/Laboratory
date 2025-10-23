using System;
using UnityEngine;

namespace Laboratory.Core.MonsterTown
{
    /// <summary>
    /// Main configuration ScriptableObject for Monster Town system.
    /// Designer-friendly configuration that follows Chimera patterns.
    /// </summary>
    [CreateAssetMenu(fileName = "MonsterTownConfig", menuName = "Chimera/Monster Town/Town Configuration")]
    public class MonsterTownConfig : ScriptableObject
    {
        [Header("Town Identity")]
        [SerializeField] public string townName = "New Monster Town";
        [SerializeField] public string townDescription = "A thriving monster breeding community";
        [SerializeField] public Sprite townIcon;
        [SerializeField] public Color townColor = Color.cyan;

        [Header("Town Layout")]
        [SerializeField] public Vector2 townBounds = new Vector2(100f, 100f);
        [SerializeField] public float gridSize = 5f;
        [SerializeField] public bool useGridBasedPlacement = true;
        [SerializeField] public LayerMask buildingLayerMask = 1;

        [Header("Starting Resources")]
        [SerializeField] public TownResourcesConfig startingResources;

        [Header("Initial Buildings")]
        [SerializeField] public InitialBuildingConfig[] initialBuildings;

        [Header("Population Settings")]
        [SerializeField] public int maxPopulation = 50;
        [SerializeField] public int startingPopulation = 5;
        [SerializeField] public bool allowBreeding = true;
        [SerializeField] public bool allowTrading = true;

        [Header("Activity Settings")]
        [SerializeField] public bool enableActivityCenters = true;
        [SerializeField] public ActivityType[] unlockedActivities = { ActivityType.Racing, ActivityType.Puzzle, ActivityType.Adventure };
        [SerializeField] public float activityCooldownMultiplier = 1f;

        [Header("Resource Generation")]
        [SerializeField] public bool enableResourceGeneration = true;
        [SerializeField] public float resourceGenerationMultiplier = 1f;
        [SerializeField] public float happinessResourceBonus = 0.5f;

        [Header("Integration Settings")]
        [SerializeField] public bool integrateWithExistingChimera = true;
        [SerializeField] public bool enableBreedingFacilities = true;
        [SerializeField] public bool enableECSPerformanceMode = true;

        [Header("Difficulty & Balance")]
        [SerializeField] public float difficultyMultiplier = 1f;
        [SerializeField] public TownResourcesConfig resourceLimits;
        [SerializeField] public bool enableDebugMode = false;

        #region Validation

        private void OnValidate()
        {
            // Ensure valid town bounds
            townBounds.x = Mathf.Max(10f, townBounds.x);
            townBounds.y = Mathf.Max(10f, townBounds.y);

            // Ensure valid grid size
            gridSize = Mathf.Max(1f, gridSize);

            // Ensure valid population limits
            maxPopulation = Mathf.Max(1, maxPopulation);
            startingPopulation = Mathf.Clamp(startingPopulation, 0, maxPopulation);

            // Ensure valid multipliers
            activityCooldownMultiplier = Mathf.Max(0.1f, activityCooldownMultiplier);
            resourceGenerationMultiplier = Mathf.Max(0f, resourceGenerationMultiplier);
            difficultyMultiplier = Mathf.Max(0.1f, difficultyMultiplier);
            happinessResourceBonus = Mathf.Max(0f, happinessResourceBonus);

            // Create default starting resources if null
            if (startingResources == null)
            {
                startingResources = CreateDefaultStartingResources();
            }

            // Create default resource limits if null
            if (resourceLimits == null)
            {
                resourceLimits = CreateDefaultResourceLimits();
            }

            // Ensure we have some initial buildings
            if (initialBuildings == null || initialBuildings.Length == 0)
            {
                initialBuildings = CreateDefaultInitialBuildings();
            }

            // Ensure we have some unlocked activities
            if (unlockedActivities == null || unlockedActivities.Length == 0)
            {
                unlockedActivities = new ActivityType[] { ActivityType.Racing, ActivityType.Puzzle };
            }
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Get town bounds as Unity Bounds object
        /// </summary>
        public Bounds GetTownBounds(Vector3 center)
        {
            return new Bounds(center, new Vector3(townBounds.x, 10f, townBounds.y));
        }

        /// <summary>
        /// Check if this town configuration is valid
        /// </summary>
        public bool IsValid()
        {
            if (string.IsNullOrEmpty(townName)) return false;
            if (townBounds.x <= 0 || townBounds.y <= 0) return false;
            if (gridSize <= 0) return false;
            if (maxPopulation <= 0) return false;
            if (startingResources == null) return false;
            if (initialBuildings == null) return false;

            return true;
        }

        /// <summary>
        /// Get starting resources as TownResources struct
        /// </summary>
        public TownResources GetStartingResources()
        {
            return startingResources?.ToTownResources() ?? TownResources.GetDefault();
        }

        /// <summary>
        /// Get resource limits as TownResources struct
        /// </summary>
        public TownResources GetResourceLimits()
        {
            return resourceLimits?.ToTownResources() ?? new TownResources
            {
                coins = 999999,
                gems = 99999,
                activityTokens = 9999,
                geneticSamples = 999,
                materials = 9999,
                energy = 999
            };
        }

        /// <summary>
        /// Check if an activity is initially unlocked
        /// </summary>
        public bool IsActivityUnlocked(ActivityType activityType)
        {
            if (unlockedActivities == null) return false;

            foreach (var activity in unlockedActivities)
            {
                if (activity == activityType) return true;
            }

            return false;
        }

        #endregion

        #region Default Configuration Creation

        private TownResourcesConfig CreateDefaultStartingResources()
        {
            var config = CreateInstance<TownResourcesConfig>();
            config.coins = 1000;
            config.gems = 10;
            config.activityTokens = 50;
            config.geneticSamples = 5;
            config.materials = 100;
            config.energy = 100;
            return config;
        }

        private TownResourcesConfig CreateDefaultResourceLimits()
        {
            var config = CreateInstance<TownResourcesConfig>();
            config.coins = 999999;
            config.gems = 99999;
            config.activityTokens = 9999;
            config.geneticSamples = 999;
            config.materials = 9999;
            config.energy = 999;
            return config;
        }

        private InitialBuildingConfig[] CreateDefaultInitialBuildings()
        {
            return new InitialBuildingConfig[]
            {
                new InitialBuildingConfig { buildingType = BuildingType.BreedingCenter, position = new Vector3(-10f, 0f, 0f) },
                new InitialBuildingConfig { buildingType = BuildingType.MonsterHabitat, position = new Vector3(10f, 0f, 0f) },
                new InitialBuildingConfig { buildingType = BuildingType.ActivityCenter, position = new Vector3(0f, 0f, -10f) }
            };
        }

        #endregion

        #region Editor Utilities

#if UNITY_EDITOR
        [ContextMenu("Validate Configuration")]
        private void ValidateConfigurationMenu()
        {
            OnValidate();
            if (IsValid())
            {
                Debug.Log($"✅ {name} configuration is valid!");
            }
            else
            {
                Debug.LogError($"❌ {name} configuration has errors!");
            }
        }

        [ContextMenu("Create Default Initial Buildings")]
        private void CreateDefaultInitialBuildingsMenu()
        {
            initialBuildings = CreateDefaultInitialBuildings();
            UnityEditor.EditorUtility.SetDirty(this);
        }

        [ContextMenu("Reset to Defaults")]
        private void ResetToDefaults()
        {
            townName = "New Monster Town";
            townDescription = "A thriving monster breeding community";
            townBounds = new Vector2(100f, 100f);
            gridSize = 5f;
            useGridBasedPlacement = true;
            maxPopulation = 50;
            startingPopulation = 5;
            enableActivityCenters = true;
            unlockedActivities = new ActivityType[] { ActivityType.Racing, ActivityType.Puzzle, ActivityType.Adventure };
            enableResourceGeneration = true;
            resourceGenerationMultiplier = 1f;
            happinessResourceBonus = 0.5f;
            integrateWithExistingChimera = true;
            enableBreedingFacilities = true;
            enableECSPerformanceMode = true;
            difficultyMultiplier = 1f;

            OnValidate();
            UnityEditor.EditorUtility.SetDirty(this);
            Debug.Log($"🔄 {name} reset to default values");
        }
#endif

        #endregion
    }

    /// <summary>
    /// Resource configuration as ScriptableObject
    /// </summary>
    [CreateAssetMenu(fileName = "TownResourcesConfig", menuName = "Chimera/Monster Town/Resource Configuration")]
    public class TownResourcesConfig : ScriptableObject
    {
        [Header("Currency Resources")]
        public int coins = 1000;
        public int gems = 10;

        [Header("Activity Resources")]
        public int activityTokens = 50;
        public int energy = 100;

        [Header("Breeding Resources")]
        public int geneticSamples = 5;
        public int materials = 100;

        /// <summary>
        /// Convert to TownResources struct
        /// </summary>
        public TownResources ToTownResources()
        {
            return new TownResources
            {
                coins = coins,
                gems = gems,
                activityTokens = activityTokens,
                geneticSamples = geneticSamples,
                materials = materials,
                energy = energy
            };
        }

        /// <summary>
        /// Update from TownResources struct
        /// </summary>
        public void FromTownResources(TownResources resources)
        {
            coins = resources.coins;
            gems = resources.gems;
            activityTokens = resources.activityTokens;
            geneticSamples = resources.geneticSamples;
            materials = resources.materials;
            energy = resources.energy;
        }

        /// <summary>
        /// Check if this config has any non-zero resources
        /// </summary>
        public bool HasAnyResource()
        {
            return coins > 0 || gems > 0 || activityTokens > 0 ||
                   geneticSamples > 0 || materials > 0 || energy > 0;
        }

        private void OnValidate()
        {
            // Ensure non-negative values
            coins = Mathf.Max(0, coins);
            gems = Mathf.Max(0, gems);
            activityTokens = Mathf.Max(0, activityTokens);
            geneticSamples = Mathf.Max(0, geneticSamples);
            materials = Mathf.Max(0, materials);
            energy = Mathf.Max(0, energy);
        }
    }

    /// <summary>
    /// Initial building placement configuration
    /// </summary>
    [Serializable]
    public struct InitialBuildingConfig
    {
        public BuildingType buildingType;
        public Vector3 position;
        public int level;

        public InitialBuildingConfig(BuildingType type, Vector3 pos, int lvl = 1)
        {
            buildingType = type;
            position = pos;
            level = Mathf.Max(1, lvl);
        }
    }

    /// <summary>
    /// Town development stages configuration
    /// </summary>
    [CreateAssetMenu(fileName = "TownDevelopmentConfig", menuName = "Chimera/Monster Town/Development Configuration")]
    public class TownDevelopmentConfig : ScriptableObject
    {
        [Header("Development Stages")]
        public TownStage[] developmentStages;

        [Header("Unlock Requirements")]
        public int[] populationRequirements = { 5, 15, 30, 50 };
        public int[] buildingRequirements = { 3, 7, 12, 20 };
        public TownResourcesConfig[] resourceRequirements;

        private void OnValidate()
        {
            // Ensure arrays are properly sized
            if (developmentStages == null)
            {
                developmentStages = CreateDefaultStages();
            }

            // Ensure requirements arrays match stages
            var stageCount = developmentStages?.Length ?? 4;

            if (populationRequirements == null || populationRequirements.Length != stageCount)
            {
                populationRequirements = new int[stageCount];
                for (int i = 0; i < stageCount; i++)
                {
                    populationRequirements[i] = (i + 1) * 10;
                }
            }

            if (buildingRequirements == null || buildingRequirements.Length != stageCount)
            {
                buildingRequirements = new int[stageCount];
                for (int i = 0; i < stageCount; i++)
                {
                    buildingRequirements[i] = (i + 1) * 5;
                }
            }
        }

        private TownStage[] CreateDefaultStages()
        {
            return new TownStage[]
            {
                new TownStage { stageName = "Settlement", description = "A small monster settlement", unlockLevel = 1 },
                new TownStage { stageName = "Village", description = "A growing monster village", unlockLevel = 2 },
                new TownStage { stageName = "Town", description = "A bustling monster town", unlockLevel = 3 },
                new TownStage { stageName = "City", description = "A thriving monster city", unlockLevel = 4 }
            };
        }
    }

    /// <summary>
    /// Town development stage
    /// </summary>
    [Serializable]
    public struct TownStage
    {
        public string stageName;
        public string description;
        public int unlockLevel;
        public Sprite stageIcon;
        public Color stageColor;
        public ActivityType[] unlockedActivities;
        public BuildingType[] unlockedBuildings;
    }
}