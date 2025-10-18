using System;
using UnityEngine;

namespace Laboratory.Core.MonsterTown
{
    /// <summary>
    /// Building configuration ScriptableObject for Monster Town buildings.
    /// Designer-friendly configuration for all building types.
    /// </summary>
    [CreateAssetMenu(fileName = "BuildingConfig", menuName = "Chimera/Monster Town/Building Configuration")]
    public class BuildingConfig : ScriptableObject
    {
        [Header("Building Identity")]
        [SerializeField] public BuildingType buildingType = BuildingType.BreedingCenter;
        [SerializeField] public string buildingName = "Building";
        [SerializeField] public string description = "A building for your monster town";
        [SerializeField] public Sprite buildingIcon;
        [SerializeField] public Color buildingColor = Color.white;

        [Header("Physical Properties")]
        [SerializeField] public Vector3 size = Vector3.one * 3f;
        [SerializeField] public float height = 3f;
        [SerializeField] public GameObject buildingPrefab;
        [SerializeField] public Material[] buildingMaterials;

        [Header("Construction")]
        [SerializeField] public TownResourcesConfig constructionCost;
        [SerializeField] public float constructionTime = 10f; // seconds
        [SerializeField] public bool requiresFoundation = true;
        [SerializeField] public BuildingType[] requiredBuildings;

        [Header("Building Stats")]
        [SerializeField] public float maxHealth = 100f;
        [SerializeField] public int maxLevel = 5;
        [SerializeField] public int capacity = 1;
        [SerializeField] public float efficiency = 1f;

        [Header("Resource Generation")]
        [SerializeField] public TownResourcesConfig resourceGeneration;
        [SerializeField] public float generationInterval = 60f; // seconds
        [SerializeField] public bool requiresHappyMonsters = false;

        [Header("Activity Center Settings")]
        [SerializeField] public bool isActivityCenter = false;
        [SerializeField] public ActivityType[] supportedActivities;
        [SerializeField] public int activityCapacity = 1;
        [SerializeField] public float activityBonus = 0f;

        [Header("Upgrade Paths")]
        [SerializeField] public BuildingUpgrade[] upgrades;

        [Header("Special Features")]
        [SerializeField] public bool canHouseMonsters = false;
        [SerializeField] public int monsterCapacity = 0;
        [SerializeField] public bool providesHappinessBonus = false;
        [SerializeField] public float happinessBonus = 0.1f;

        [Header("Placement Rules")]
        [SerializeField] public bool canPlaceOnWater = false;
        [SerializeField] public bool requiresSpecialTerrain = false;
        [SerializeField] public LayerMask validTerrainLayers = -1;
        [SerializeField] public float minDistanceFromOtherBuildings = 0f;
        [SerializeField] public BuildingType[] cannotBeNear;

        [Header("Visual & Audio")]
        [SerializeField] public ParticleSystem constructionEffect;
        [SerializeField] public AudioClip constructionSound;
        [SerializeField] public AudioClip ambientSound;
        [SerializeField] public float ambientVolume = 0.5f;

        #region Validation

        private void OnValidate()
        {
            // Ensure valid building name
            if (string.IsNullOrEmpty(buildingName))
            {
                buildingName = buildingType.ToString();
            }

            // Ensure valid size
            size.x = Mathf.Max(1f, size.x);
            size.y = Mathf.Max(1f, size.y);
            size.z = Mathf.Max(1f, size.z);
            height = Mathf.Max(1f, height);

            // Ensure valid stats
            maxHealth = Mathf.Max(1f, maxHealth);
            maxLevel = Mathf.Max(1, maxLevel);
            capacity = Mathf.Max(1, capacity);
            efficiency = Mathf.Max(0.1f, efficiency);
            constructionTime = Mathf.Max(1f, constructionTime);
            generationInterval = Mathf.Max(1f, generationInterval);

            // Activity center validation
            if (isActivityCenter)
            {
                activityCapacity = Mathf.Max(1, activityCapacity);
                if (supportedActivities == null || supportedActivities.Length == 0)
                {
                    // Auto-assign activity based on building type
                    supportedActivities = GetDefaultActivitiesForBuilding();
                }
            }
            else
            {
                activityCapacity = 0;
                activityBonus = 0f;
            }

            // Monster housing validation
            if (canHouseMonsters)
            {
                monsterCapacity = Mathf.Max(1, monsterCapacity);
            }
            else
            {
                monsterCapacity = 0;
            }

            // Happiness validation
            if (providesHappinessBonus)
            {
                happinessBonus = Mathf.Max(0f, happinessBonus);
            }
            else
            {
                happinessBonus = 0f;
            }

            // Create default construction cost if null
            if (constructionCost == null)
            {
                constructionCost = CreateDefaultConstructionCost();
            }

            // Create default resource generation if null
            if (resourceGeneration == null)
            {
                resourceGeneration = CreateDefaultResourceGeneration();
            }

            // Volume validation
            ambientVolume = Mathf.Clamp01(ambientVolume);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Get construction cost as TownResources struct
        /// </summary>
        public TownResources GetConstructionCost()
        {
            return constructionCost?.ToTownResources() ?? TownResources.Zero;
        }

        /// <summary>
        /// Get resource generation as TownResources struct
        /// </summary>
        public TownResources GetResourceGeneration()
        {
            return resourceGeneration?.ToTownResources() ?? TownResources.Zero;
        }

        /// <summary>
        /// Check if this building can be placed at the given position
        /// </summary>
        public bool CanPlaceAt(Vector3 position, BuildingType[] nearbyBuildings)
        {
            // Check terrain requirements
            if (requiresSpecialTerrain)
            {
                // In a real implementation, this would check terrain layers
                // For now, we'll return true
            }

            // Check distance from other buildings
            if (minDistanceFromOtherBuildings > 0f && nearbyBuildings != null)
            {
                // Implementation would check actual distances
                // For now, we'll return true
            }

            // Check conflicting building types
            if (cannotBeNear != null && nearbyBuildings != null)
            {
                foreach (var nearbyType in nearbyBuildings)
                {
                    foreach (var forbiddenType in cannotBeNear)
                    {
                        if (nearbyType == forbiddenType)
                        {
                            return false;
                        }
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// Check if this building supports a specific activity
        /// </summary>
        public bool SupportsActivity(ActivityType activityType)
        {
            if (!isActivityCenter || supportedActivities == null)
                return false;

            foreach (var activity in supportedActivities)
            {
                if (activity == activityType)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Get upgrade configuration for a specific level
        /// </summary>
        public BuildingUpgrade GetUpgrade(int level)
        {
            if (upgrades == null || level < 1 || level > upgrades.Length)
                return default;

            return upgrades[level - 1];
        }

        /// <summary>
        /// Check if this building type is valid
        /// </summary>
        public bool IsValid()
        {
            if (string.IsNullOrEmpty(buildingName)) return false;
            if (size.magnitude <= 0) return false;
            if (maxHealth <= 0) return false;
            if (constructionCost == null) return false;

            return true;
        }

        #endregion

        #region Default Configuration Creation

        private TownResourcesConfig CreateDefaultConstructionCost()
        {
            var cost = CreateInstance<TownResourcesConfig>();

            // Set costs based on building type
            switch (buildingType)
            {
                case BuildingType.BreedingCenter:
                    cost.coins = 500;
                    cost.materials = 50;
                    cost.geneticSamples = 2;
                    break;

                case BuildingType.TrainingGrounds:
                    cost.coins = 300;
                    cost.materials = 30;
                    cost.energy = 20;
                    break;

                case BuildingType.ResearchLab:
                    cost.coins = 800;
                    cost.materials = 60;
                    cost.gems = 5;
                    break;

                case BuildingType.MonsterHabitat:
                    cost.coins = 200;
                    cost.materials = 25;
                    break;

                case BuildingType.ActivityCenter:
                    cost.coins = 600;
                    cost.materials = 40;
                    cost.activityTokens = 10;
                    break;

                default:
                    cost.coins = 250;
                    cost.materials = 20;
                    break;
            }

            return cost;
        }

        private TownResourcesConfig CreateDefaultResourceGeneration()
        {
            var generation = CreateInstance<TownResourcesConfig>();

            // Set generation based on building type
            switch (buildingType)
            {
                case BuildingType.BreedingCenter:
                    generation.coins = 5;
                    generation.geneticSamples = 1;
                    break;

                case BuildingType.TrainingGrounds:
                    generation.activityTokens = 2;
                    break;

                case BuildingType.ResearchLab:
                    generation.gems = 1;
                    generation.materials = 2;
                    break;

                case BuildingType.ResourceGenerator:
                    generation.coins = 10;
                    generation.materials = 5;
                    generation.energy = 5;
                    break;

                default:
                    generation.coins = 2;
                    break;
            }

            return generation;
        }

        private ActivityType[] GetDefaultActivitiesForBuilding()
        {
            return buildingType switch
            {
                BuildingType.RacingTrack => new[] { ActivityType.Racing },
                BuildingType.CombatArena => new[] { ActivityType.Combat },
                BuildingType.PuzzleAcademy => new[] { ActivityType.Puzzle },
                BuildingType.StrategyCommand => new[] { ActivityType.Strategy },
                BuildingType.MusicStudio => new[] { ActivityType.Music },
                BuildingType.AdventureGuild => new[] { ActivityType.Adventure },
                BuildingType.CraftingWorkshop => new[] { ActivityType.Crafting },
                BuildingType.ActivityCenter => new[] { ActivityType.Racing, ActivityType.Puzzle, ActivityType.Adventure },
                _ => new ActivityType[0]
            };
        }

        #endregion

        #region Static Methods

        /// <summary>
        /// Create a default building configuration for a specific building type
        /// </summary>
        public static BuildingConfig CreateDefault(BuildingType buildingType)
        {
            var config = CreateInstance<BuildingConfig>();
            config.buildingType = buildingType;
            config.buildingName = buildingType.ToString();
            config.description = GetDefaultDescription(buildingType);
            config.isActivityCenter = IsActivityBuilding(buildingType);
            config.canHouseMonsters = IsHousingBuilding(buildingType);
            config.providesHappinessBonus = IsHappinessBuilding(buildingType);

            config.OnValidate();
            return config;
        }

        private static string GetDefaultDescription(BuildingType buildingType)
        {
            return buildingType switch
            {
                BuildingType.BreedingCenter => "Specialized environment for monster breeding and genetic research",
                BuildingType.TrainingGrounds => "Training facility for preparing monsters for various activities",
                BuildingType.ResearchLab => "Advanced laboratory for genetic research and technology development",
                BuildingType.MonsterHabitat => "Comfortable living space for monsters to rest and recover",
                BuildingType.EquipmentShop => "Shop for purchasing and upgrading monster equipment",
                BuildingType.ActivityCenter => "Multi-purpose facility hosting various monster activities",
                BuildingType.RacingTrack => "High-speed racing circuit for speed competitions",
                BuildingType.CombatArena => "Combat facility for battle training and tournaments",
                BuildingType.PuzzleAcademy => "Learning center focused on puzzle-solving and intelligence",
                BuildingType.StrategyCommand => "Command center for strategic planning and tactical training",
                BuildingType.MusicStudio => "Creative space for musical expression and rhythm training",
                BuildingType.AdventureGuild => "Hub for organizing adventures and exploration missions",
                BuildingType.CraftingWorkshop => "Workshop for creating items and equipment",
                BuildingType.ResourceGenerator => "Facility that generates basic resources for the town",
                BuildingType.StorageWarehouse => "Large storage facility for town resources and items",
                BuildingType.SocialHub => "Community gathering place for social interactions",
                BuildingType.MedicalCenter => "Health facility for monster care and recovery",
                BuildingType.Library => "Knowledge repository for research and learning",
                _ => $"A {buildingType.ToString().ToLower()} building for your monster town"
            };
        }

        private static bool IsActivityBuilding(BuildingType buildingType)
        {
            return buildingType switch
            {
                BuildingType.ActivityCenter or
                BuildingType.RacingTrack or
                BuildingType.CombatArena or
                BuildingType.PuzzleAcademy or
                BuildingType.StrategyCommand or
                BuildingType.MusicStudio or
                BuildingType.AdventureGuild or
                BuildingType.CraftingWorkshop => true,
                _ => false
            };
        }

        private static bool IsHousingBuilding(BuildingType buildingType)
        {
            return buildingType switch
            {
                BuildingType.MonsterHabitat or
                BuildingType.BreedingCenter => true,
                _ => false
            };
        }

        private static bool IsHappinessBuilding(BuildingType buildingType)
        {
            return buildingType switch
            {
                BuildingType.MonsterHabitat or
                BuildingType.SocialHub or
                BuildingType.MedicalCenter or
                BuildingType.Library => true,
                _ => false
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
                Debug.Log($"✅ {buildingName} building configuration is valid!");
            }
            else
            {
                Debug.LogError($"❌ {buildingName} building configuration has errors!");
            }
        }

        [ContextMenu("Reset to Defaults")]
        private void ResetToDefaults()
        {
            var tempType = buildingType;
            var defaultConfig = CreateDefault(tempType);

            buildingName = defaultConfig.buildingName;
            description = defaultConfig.description;
            isActivityCenter = defaultConfig.isActivityCenter;
            canHouseMonsters = defaultConfig.canHouseMonsters;
            providesHappinessBonus = defaultConfig.providesHappinessBonus;

            OnValidate();
            UnityEditor.EditorUtility.SetDirty(this);
            Debug.Log($"🔄 {buildingName} reset to default values");
        }

        [ContextMenu("Auto-Configure Activity Center")]
        private void AutoConfigureActivityCenter()
        {
            if (IsActivityBuilding(buildingType))
            {
                isActivityCenter = true;
                supportedActivities = GetDefaultActivitiesForBuilding();
                activityCapacity = supportedActivities.Length > 1 ? 3 : 2;
                activityBonus = 0.1f;

                OnValidate();
                UnityEditor.EditorUtility.SetDirty(this);
                Debug.Log($"🎮 {buildingName} configured as activity center");
            }
            else
            {
                Debug.LogWarning($"{buildingName} is not an activity building type");
            }
        }
#endif

        #endregion
    }

    /// <summary>
    /// Building upgrade configuration
    /// </summary>
    [Serializable]
    public struct BuildingUpgrade
    {
        [Header("Upgrade Identity")]
        public int level;
        public string upgradeName;
        public string description;

        [Header("Upgrade Costs")]
        public TownResourcesConfig upgradeCost;
        public float upgradeTime;

        [Header("Stat Improvements")]
        public float healthMultiplier;
        public float efficiencyMultiplier;
        public int capacityIncrease;
        public float happinessBonus;

        [Header("New Features")]
        public ActivityType[] unlockedActivities;
        public bool unlocksNewFeatures;
        public string[] newFeatureDescriptions;

        public static BuildingUpgrade CreateDefault(int level)
        {
            return new BuildingUpgrade
            {
                level = level,
                upgradeName = $"Level {level} Upgrade",
                description = $"Improves building to level {level}",
                healthMultiplier = 1.2f,
                efficiencyMultiplier = 1.1f,
                capacityIncrease = 1,
                happinessBonus = 0.05f,
                unlocksNewFeatures = false
            };
        }
    }
}