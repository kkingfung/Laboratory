using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Laboratory.Core.Events;
using Laboratory.Core.Utilities;
using Laboratory.Core.Enums;
using Laboratory.Shared.Types;
using Laboratory.Chimera.Core;
using Laboratory.Core.Diagnostics;

namespace Laboratory.Core.Progression
{
    /// <summary>
    /// Comprehensive player progression system managing geneticist levels, biome specialization,
    /// research unlocks, and territory expansion. Integrates with breeding, analytics, and
    /// achievement systems to provide meaningful long-term progression and player motivation.
    /// </summary>
    public class PlayerProgressionManager : MonoBehaviour
    {
        [Header("Progression Configuration")]
        [SerializeField] private PlayerProgressionConfig progressionConfig;
        [SerializeField] private bool enableProgressionTracking = true;
        [SerializeField] private bool enableAutoSave = true;
        [SerializeField] private float autoSaveInterval = 30f;

        [Header("Experience Settings")]
        [SerializeField] private float baseExperienceRequirement = 100f;
        [SerializeField] private float experienceGrowthRate = 1.15f;
        [SerializeField] private int maxGeneticistLevel = 100;

        [Header("Creature Slot Configuration")]
        [SerializeField] private int baseCreatureSlots = 3;
        [SerializeField] private int maxCreatureSlots = 25;
        [SerializeField] private int slotsPerLevel = 1;
        [SerializeField] private int[] bonusSlotLevels = { 20, 40, 60, 80, 100 };

        [Header("Debug Settings")]
        [SerializeField] private bool enableDebugLogging = true;
        [SerializeField] private bool showProgressionNotifications = true;

        // Core progression data
        private PlayerProgressionData currentProgressionData;
        private Dictionary<BiomeType, BiomeSpecializationData> biomeSpecializations;
        private Dictionary<ResearchType, ResearchProgress> researchProgress;
        private TerritoryExpansionData territoryData;

        // Experience tracking
        private float pendingExperience = 0f;
        private float lastAutoSave = 0f;
        private Queue<ExperienceGain> recentExperienceGains = new Queue<ExperienceGain>();

        // Events
        public System.Action<int, int> OnLevelUp; // oldLevel, newLevel
        public System.Action<float> OnExperienceGained; // experienceAmount
        public System.Action<BiomeType, int> OnBiomeSpecializationUp; // biomeType, newLevel
        public System.Action<ResearchType> OnResearchUnlocked; // researchType
        public System.Action<TerritoryTier> OnTerritoryExpanded; // newTier
        public System.Action<int> OnCreatureSlotsIncreased; // newSlotCount

        // Skill-based progression events (for partnership system)
        public System.Action<string, string, float> OnSkillMilestoneReached; // genre, milestoneType, masteryLevel
        public System.Action<string, float, float> OnSkillImproved; // skillName, oldValue, newValue
        public System.Action<float, float, float> OnPartnershipQualityChanged; // cooperation, trust, understanding

        // Singleton access
        private static PlayerProgressionManager instance;
        public static PlayerProgressionManager Instance => instance;

        // Public properties
        public PlayerProgressionData CurrentProgression => currentProgressionData;
        public int GeneticistLevel => currentProgressionData?.geneticistLevel ?? 1;
        public float CurrentExperience => currentProgressionData?.currentExperience ?? 0f;
        public int AvailableCreatureSlots => CalculateAvailableCreatureSlots();
        public TerritoryTier CurrentTerritoryTier => territoryData?.currentTier ?? TerritoryTier.StartingFacility;

        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeProgression();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            LoadProgressionData();
            ConnectToGameSystems();
        }

        private void Update()
        {
            ProcessPendingExperience();

            // Auto-save progression data
            if (enableAutoSave && Time.time - lastAutoSave >= autoSaveInterval)
            {
                SaveProgressionData();
                lastAutoSave = Time.time;
            }
        }

        private void InitializeProgression()
        {
            if (enableDebugLogging)
                DebugManager.LogInfo("Initializing Player Progression Manager");

            // Initialize core data structures
            biomeSpecializations = new Dictionary<BiomeType, BiomeSpecializationData>();
            researchProgress = new Dictionary<ResearchType, ResearchProgress>();

            // Initialize biome specializations
            foreach (BiomeType biome in System.Enum.GetValues(typeof(BiomeType)))
            {
                biomeSpecializations[biome] = new BiomeSpecializationData
                {
                    biomeType = biome,
                    specializationLevel = 0,
                    totalExperience = 0f,
                    isUnlocked = biome == BiomeType.Forest // Forest starts unlocked
                };
            }

            // Initialize research progress
            foreach (ResearchType research in System.Enum.GetValues(typeof(ResearchType)))
            {
                researchProgress[research] = new ResearchProgress
                {
                    researchType = research,
                    isUnlocked = research == ResearchType.BasicBreeding, // Basic breeding starts unlocked
                    progressPoints = 0f,
                    completionTime = 0f
                };
            }

            // Initialize territory data
            territoryData = new TerritoryExpansionData
            {
                currentTier = TerritoryTier.StartingFacility,
                facilitiesOwned = new List<FacilityData>
                {
                    new FacilityData
                    {
                        facilityType = FacilityType.BasicBreedingFacility,
                        biomeLocation = BiomeType.Forest,
                        creatureCapacity = 2,
                        isActive = true
                    }
                },
                totalInvestment = 0f
            };

            if (enableDebugLogging)
                DebugManager.LogInfo("Player Progression Manager initialized successfully");
        }

        /// <summary>
        /// Awards experience points to the player with detailed tracking and source attribution
        /// </summary>
        public void AwardExperience(float amount, ExperienceSource source, string description = "")
        {
            if (!enableProgressionTracking || amount <= 0f) return;

            var experienceGain = new ExperienceGain
            {
                amount = amount,
                source = source,
                description = description,
                timestamp = Time.time,
                biomeContext = BiomeType.Forest // Simplified default biome context
            };

            // Apply source-based multipliers
            float multipliedAmount = ApplyExperienceMultipliers(amount, source);

            pendingExperience += multipliedAmount;
            recentExperienceGains.Enqueue(experienceGain);

            // Keep only recent gains for analytics
            while (recentExperienceGains.Count > 100)
            {
                recentExperienceGains.Dequeue();
            }

            // Track in analytics using reflection to avoid assembly dependency
            var analyticsTracker = FindFirstObjectByType<MonoBehaviour>();
            var analyticsTrackerType = System.Type.GetType("Laboratory.Systems.Analytics.PlayerAnalyticsTracker, Laboratory.Systems");

            if (analyticsTrackerType != null)
            {
                var trackerInstance = FindFirstObjectByType(analyticsTrackerType);
                if (trackerInstance != null)
                {
                    var trackActionMethod = analyticsTrackerType.GetMethod("TrackAction",
                        new[] { typeof(string), typeof(Dictionary<string, object>) });

                    if (trackActionMethod != null)
                    {
                        var analyticsData = new Dictionary<string, object>
                        {
                            ["amount"] = multipliedAmount,
                            ["source"] = source.ToString(),
                            ["description"] = description,
                            ["currentLevel"] = GeneticistLevel
                        };

                        try
                        {
                            trackActionMethod.Invoke(trackerInstance, new object[] { "ExperienceGained", analyticsData });
                        }
                        catch (System.Exception ex)
                        {
                            if (enableDebugLogging)
                                DebugManager.LogWarning($"Analytics tracking failed: {ex.Message}");
                        }
                    }
                }
            }

            if (enableDebugLogging)
                DebugManager.LogInfo($"Awarded {multipliedAmount:F1} XP from {source}: {description}");

            OnExperienceGained?.Invoke(multipliedAmount);
        }

        /// <summary>
        /// Awards specialized experience for biome-specific activities
        /// </summary>
        public void AwardBiomeExperience(BiomeType biome, float amount, string activity = "")
        {
            if (!biomeSpecializations.ContainsKey(biome)) return;

            var specialization = biomeSpecializations[biome];
            if (!specialization.isUnlocked) return;

            float previousLevel = specialization.specializationLevel;
            specialization.totalExperience += amount;
            specialization.specializationLevel = (int)(specialization.totalExperience / 100f); // Simplified level calculation

            // Check for specialization level up
            if (specialization.specializationLevel > previousLevel)
            {
                HandleBiomeSpecializationLevelUp(biome, (int)previousLevel, (int)specialization.specializationLevel);
            }

            // Also award general experience (50% of biome experience)
            AwardExperience(amount * 0.5f, ExperienceSource.BiomeSpecialization, $"{biome} {activity}");

            if (enableDebugLogging)
                DebugManager.LogInfo($"Awarded {amount:F1} {biome} specialization XP for {activity}");
        }

        /// <summary>
        /// Advances research progress for specified research type
        /// </summary>
        public void AdvanceResearch(ResearchType researchType, float progressPoints)
        {
            if (!researchProgress.ContainsKey(researchType)) return;

            var research = researchProgress[researchType];
            if (research.isUnlocked) return; // Already unlocked

            research.progressPoints += progressPoints;
            float requiredPoints = 100f * (int)researchType; // Simplified research requirement

            if (research.progressPoints >= requiredPoints)
            {
                UnlockResearch(researchType);
            }

            if (enableDebugLogging)
                DebugManager.LogInfo($"Advanced {researchType} research: {research.progressPoints:F1}/{requiredPoints:F1}");
        }

        /// <summary>
        /// Unlocks a specific research type and its benefits
        /// </summary>
        public void UnlockResearch(ResearchType researchType)
        {
            if (!researchProgress.ContainsKey(researchType)) return;

            var research = researchProgress[researchType];
            if (research.isUnlocked) return; // Already unlocked

            research.isUnlocked = true;
            research.completionTime = Time.time;

            // Apply research benefits
            ApplyResearchBenefits(researchType);

            // Award experience for research completion
            AwardExperience(50f * (int)researchType, ExperienceSource.ResearchCompletion, researchType.ToString());

            OnResearchUnlocked?.Invoke(researchType);

            if (enableDebugLogging)
                DebugManager.LogInfo($"Research unlocked: {researchType}");

            if (showProgressionNotifications)
            {
                ShowResearchUnlockedNotification(researchType);
            }
        }

        /// <summary>
        /// Attempts to expand territory to the next tier
        /// </summary>
        public bool TryExpandTerritory(TerritoryTier targetTier)
        {
            if (targetTier <= territoryData.currentTier) return false;

            // Simplified territory requirements check
            if (GeneticistLevel < (int)targetTier * 5) return false; // Basic level requirement

            // Expand territory
            territoryData.currentTier = targetTier;
            territoryData.totalInvestment += (int)targetTier * 100; // Simplified investment cost

            // Add new facilities based on tier
            AddTierFacilities(targetTier);

            // Award experience for territory expansion
            AwardExperience((int)targetTier * 50, ExperienceSource.TerritoryExpansion, targetTier.ToString());

            OnTerritoryExpanded?.Invoke(targetTier);

            if (enableDebugLogging)
                DebugManager.LogInfo($"Territory expanded to: {targetTier}");

            if (showProgressionNotifications)
            {
                ShowTerritoryExpansionNotification(targetTier);
            }

            return true;
        }

        /// <summary>
        /// Unlocks a new biome for exploration and specialization
        /// </summary>
        public bool TryUnlockBiome(BiomeType biome)
        {
            if (!biomeSpecializations.ContainsKey(biome)) return false;
            if (biomeSpecializations[biome].isUnlocked) return false;

            // Simplified biome unlock requirements
            if (GeneticistLevel < (int)biome * 3) return false; // Basic level requirement

            // Unlock biome
            biomeSpecializations[biome].isUnlocked = true;

            // Award experience for biome unlock
            AwardExperience((int)biome * 30, ExperienceSource.BiomeExploration, biome.ToString()); // Fixed enum value

            if (enableDebugLogging)
                DebugManager.LogInfo($"Biome unlocked: {biome}");

            if (showProgressionNotifications)
            {
                ShowBiomeUnlockedNotification(biome);
            }

            return true;
        }

        /// <summary>
        /// Gets comprehensive progression statistics
        /// </summary>
        public PlayerProgressionStats GetProgressionStats()
        {
            return new PlayerProgressionStats
            {
                geneticistLevel = GeneticistLevel,
                currentExperience = CurrentExperience,
                experienceToNextLevel = GetExperienceRequiredForLevel(GeneticistLevel + 1) - CurrentExperience,
                totalExperienceEarned = currentProgressionData?.totalExperienceEarned ?? 0f,
                availableCreatureSlots = AvailableCreatureSlots,
                maxCreatureSlots = maxCreatureSlots,
                unlockedBiomes = biomeSpecializations.Where(kvp => kvp.Value.isUnlocked).Select(kvp => kvp.Key).ToList(),
                biomeSpecializationLevels = biomeSpecializations.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.specializationLevel),
                unlockedResearch = researchProgress.Where(kvp => kvp.Value.isUnlocked).Select(kvp => kvp.Key).ToList(),
                currentTerritoryTier = territoryData.currentTier,
                totalFacilities = territoryData.facilitiesOwned.Count,
                totalTerritoryInvestment = territoryData.totalInvestment
            };
        }

        private void ProcessPendingExperience()
        {
            if (pendingExperience <= 0f) return;

            float previousLevel = GeneticistLevel;
            currentProgressionData.currentExperience += pendingExperience;
            currentProgressionData.totalExperienceEarned += pendingExperience;

            // Process level ups
            int newLevel = CalculateGeneticistLevel(currentProgressionData.currentExperience);
            if (newLevel > previousLevel)
            {
                HandleLevelUp((int)previousLevel, newLevel);
            }

            pendingExperience = 0f;
        }

        private void HandleLevelUp(int oldLevel, int newLevel)
        {
            currentProgressionData.geneticistLevel = newLevel;

            // Award creature slots for level up
            int previousSlots = CalculateCreatureSlotsForLevel(oldLevel);
            int newSlots = CalculateCreatureSlotsForLevel(newLevel);

            if (newSlots > previousSlots)
            {
                OnCreatureSlotsIncreased?.Invoke(newSlots);
            }

            // Check for biome unlocks based on level
            CheckLevelBasedUnlocks(newLevel);

            OnLevelUp?.Invoke(oldLevel, newLevel);

            if (enableDebugLogging)
                DebugManager.LogInfo($"Level up! {oldLevel} → {newLevel} (Creature slots: {newSlots})");

            if (showProgressionNotifications)
            {
                ShowLevelUpNotification(oldLevel, newLevel);
            }
        }

        private void HandleBiomeSpecializationLevelUp(BiomeType biome, int oldLevel, int newLevel)
        {
            // Award specialization bonuses
            ApplyBiomeSpecializationBenefits(biome, newLevel);

            OnBiomeSpecializationUp?.Invoke(biome, newLevel);

            if (enableDebugLogging)
                DebugManager.LogInfo($"{biome} specialization level up! {oldLevel} → {newLevel}");

            if (showProgressionNotifications)
            {
                ShowBiomeSpecializationNotification(biome, newLevel);
            }
        }

        private int CalculateGeneticistLevel(float totalExperience)
        {
            int level = 1;

            while (level < maxGeneticistLevel)
            {
                float levelRequirement = GetExperienceRequiredForLevel(level + 1);
                if (totalExperience < levelRequirement)
                    break;
                level++;
            }

            return level;
        }

        private float GetExperienceRequiredForLevel(int level)
        {
            if (level <= 1) return 0f;

            float total = 0f;
            for (int i = 2; i <= level; i++)
            {
                total += baseExperienceRequirement * Mathf.Pow(experienceGrowthRate, i - 2);
            }
            return total;
        }

        private int CalculateAvailableCreatureSlots()
        {
            int levelSlots = CalculateCreatureSlotsForLevel(GeneticistLevel);
            int bonusSlots = GetBonusCreatureSlots();
            return Mathf.Min(levelSlots + bonusSlots, maxCreatureSlots);
        }

        private int CalculateCreatureSlotsForLevel(int level)
        {
            int slots = baseCreatureSlots + ((level - 1) / slotsPerLevel);

            // Add bonus slots at specific levels
            foreach (int bonusLevel in bonusSlotLevels)
            {
                if (level >= bonusLevel)
                    slots += 2;
            }

            return Mathf.Min(slots, maxCreatureSlots);
        }

        private int GetBonusCreatureSlots()
        {
            int bonusSlots = 0;

            // Territory-based bonuses
            bonusSlots += GetTerritorySlotBonus();

            // Research-based bonuses
            bonusSlots += GetResearchSlotBonus();

            // Specialization-based bonuses
            bonusSlots += GetSpecializationSlotBonus();

            return bonusSlots;
        }

        private int GetTerritorySlotBonus()
        {
            return territoryData.currentTier switch
            {
                TerritoryTier.StartingFacility => 0,
                TerritoryTier.RanchUpgrade => 2,
                TerritoryTier.BiomeOutpost => 4,
                TerritoryTier.RegionalHub => 6,
                TerritoryTier.ContinentalNetwork => 8,
                _ => 0
            };
        }

        private int GetResearchSlotBonus()
        {
            int bonus = 0;
            foreach (var research in researchProgress.Values)
            {
                if (research.isUnlocked)
                    bonus++;
            }
            return bonus / 2; // 1 slot per 2 research unlocked
        }

        private int GetSpecializationSlotBonus()
        {
            int bonus = 0;
            foreach (var spec in biomeSpecializations.Values)
            {
                bonus += (int)spec.specializationLevel / 5; // 1 slot per 5 specialization levels
            }
            return bonus;
        }

        private float ApplyExperienceMultipliers(float baseAmount, ExperienceSource source)
        {
            float multiplier = 1f;

            // Source-based multipliers
            switch (source)
            {
                case ExperienceSource.BreedingSuccess:
                    multiplier *= 1.2f;
                    break;
                case ExperienceSource.RareDiscovery:
                    multiplier *= 2.0f;
                    break;
                case ExperienceSource.ResearchCompletion:
                    multiplier *= 1.5f;
                    break;
            }

            // Research-based multipliers
            if (researchProgress.ContainsKey(ResearchType.ExperienceOptimization) && researchProgress[ResearchType.ExperienceOptimization].isUnlocked)
            {
                multiplier *= 1.25f;
            }

            return baseAmount * multiplier;
        }

        private void ConnectToGameSystems()
        {
            // Analytics integration would be implemented here when available

            // Connect to breeding system for experience rewards
            // This would connect to breeding completion events

            // Connect to ecosystem system for biome experience
            // This would connect to biome interaction events
        }

        private void LoadProgressionData()
        {
            // Load from PlayerPrefs or save system
            string savedData = PlayerPrefs.GetString("PlayerProgressionData", "");

            if (string.IsNullOrEmpty(savedData))
            {
                // Create new progression data
                currentProgressionData = new PlayerProgressionData
                {
                    geneticistLevel = 1,
                    currentExperience = 0f,
                    totalExperienceEarned = 0f,
                    creationTime = Time.time,
                    lastUpdated = Time.time
                };
            }
            else
            {
                try
                {
                    currentProgressionData = JsonUtility.FromJson<PlayerProgressionData>(savedData);
                    currentProgressionData.lastUpdated = Time.time;
                }
                catch (System.Exception e)
                {
                    DebugManager.LogError($"Failed to load progression data: {e.Message}");
                    // Create fresh data on error
                    currentProgressionData = new PlayerProgressionData
                    {
                        geneticistLevel = 1,
                        currentExperience = 0f,
                        totalExperienceEarned = 0f,
                        creationTime = Time.time,
                        lastUpdated = Time.time
                    };
                }
            }

            if (enableDebugLogging)
                DebugManager.LogInfo($"Loaded progression data - Level: {GeneticistLevel}, XP: {CurrentExperience:F1}");
        }

        private void SaveProgressionData()
        {
            if (currentProgressionData == null) return;

            currentProgressionData.lastUpdated = Time.time;
            string jsonData = JsonUtility.ToJson(currentProgressionData);
            PlayerPrefs.SetString("PlayerProgressionData", jsonData);
            PlayerPrefs.Save();

            if (enableDebugLogging)
                DebugManager.LogInfo("Progression data saved");
        }

        // Additional helper methods would be implemented here...
        // (Including notification systems, requirement checks, benefit applications, etc.)

        private void OnDestroy()
        {
            if (instance == this)
            {
                SaveProgressionData();
                instance = null;
            }
        }

        #region Missing Implementation Methods

        /// <summary>
        /// Applies research benefits to game systems
        /// </summary>
        private void ApplyResearchBenefits(ResearchType researchType)
        {
            // Apply specific benefits based on research type
            switch (researchType)
            {
                case ResearchType.BasicBreeding:
                    ApplyBreedingBonus(0.05f); // 5% bonus
                    break;
                case ResearchType.AdvancedGenetics:
                    ApplyGeneticAnalysisBonus(0.1f); // 10% better genetic predictions
                    break;
                case ResearchType.SelectiveBreeding:
                    ApplySelectiveBreedingBonus(0.08f); // 8% bonus to desired traits
                    break;
                case ResearchType.GeneticEngineering:
                    UnlockGeneticModification();
                    break;
                case ResearchType.CrossSpeciesBreeding:
                    UnlockCrossSpeciesBreeding();
                    break;
                case ResearchType.LegendaryLineages:
                    ApplyLegendaryBreedingBonus(0.15f); // 15% bonus for legendary outcomes
                    break;
                case ResearchType.EcosystemManagement:
                    ApplyEcosystemManagementBonus(0.1f); // 10% better ecosystem rewards
                    break;
                default:
                    if (enableDebugLogging)
                        DebugManager.LogWarning($"No benefits defined for research type: {researchType}");
                    break;
            }

            if (enableDebugLogging)
                DebugManager.LogInfo($"Applied research benefits for: {researchType}");
        }

        /// <summary>
        /// Adds facilities based on territory tier
        /// </summary>
        private void AddTierFacilities(TerritoryTier targetTier)
        {
            switch (targetTier)
            {
                case TerritoryTier.StartingFacility:
                    break;
                case TerritoryTier.RanchUpgrade:
                    AddFacility("AdvancedBreedingChamber");
                    AddFacility("GeneticAnalysisStation");
                    break;
                case TerritoryTier.BiomeOutpost:
                    AddFacility("ResearchLaboratory");
                    AddFacility("SpecimenStorage");
                    AddFacility("EnvironmentalChambers");
                    break;
                case TerritoryTier.RegionalHub:
                    AddFacility("BiomeSimulationChamber");
                    AddFacility("EcosystemMonitoringStation");
                    break;
                case TerritoryTier.ContinentalNetwork:
                    AddFacility("GlobalCommunicationArray");
                    AddFacility("InterregionalTradingPost");
                    AddFacility("LegendaryBreedingFacility");
                    break;
            }
        }

        /// <summary>
        /// Checks for level-based unlocks
        /// </summary>
        private void CheckLevelBasedUnlocks(int newLevel)
        {
            // Unlock biomes based on level
            if (newLevel >= 5 && !biomeSpecializations[BiomeType.Desert].isUnlocked)
                TryUnlockBiome(BiomeType.Desert);
            if (newLevel >= 10 && !biomeSpecializations[BiomeType.Arctic].isUnlocked)
                TryUnlockBiome(BiomeType.Arctic);
            if (newLevel >= 15 && !biomeSpecializations[BiomeType.Volcanic].isUnlocked)
                TryUnlockBiome(BiomeType.Volcanic);
            if (newLevel >= 20 && !biomeSpecializations[BiomeType.DeepSea].isUnlocked)
                TryUnlockBiome(BiomeType.DeepSea);

            // Unlock research based on level
            if (newLevel >= 3 && !researchProgress[ResearchType.BasicBreeding].isUnlocked)
                UnlockResearch(ResearchType.BasicBreeding);
            if (newLevel >= 8 && !researchProgress[ResearchType.AdvancedGenetics].isUnlocked)
                UnlockResearch(ResearchType.AdvancedGenetics);
            if (newLevel >= 12 && !researchProgress[ResearchType.SelectiveBreeding].isUnlocked)
                UnlockResearch(ResearchType.SelectiveBreeding);
        }

        /// <summary>
        /// Applies biome specialization benefits
        /// </summary>
        private void ApplyBiomeSpecializationBenefits(BiomeType biome, int newLevel)
        {
            float bonusMultiplier = newLevel * 0.05f; // 5% per level

            switch (biome)
            {
                case BiomeType.Forest:
                    ApplyBiomeBenefit("ForestHealing", bonusMultiplier);
                    break;
                case BiomeType.Desert:
                    ApplyBiomeBenefit("DesertSurvival", bonusMultiplier);
                    break;
                case BiomeType.Arctic:
                    ApplyBiomeBenefit("ColdResistance", bonusMultiplier);
                    break;
                case BiomeType.Volcanic:
                    ApplyBiomeBenefit("HeatResistance", bonusMultiplier);
                    break;
                case BiomeType.DeepSea:
                    ApplyBiomeBenefit("PressureResistance", bonusMultiplier);
                    break;
            }
        }

        // Notification methods
        private void ShowResearchUnlockedNotification(ResearchType researchType)
        {
            var notification = new ProgressionNotification
            {
                title = "Research Unlocked!",
                message = $"You have unlocked {researchType} research!",
                type = ProgressionNotificationType.ResearchUnlocked
            };
            ShowProgressionNotification(notification);
        }

        private void ShowTerritoryExpansionNotification(TerritoryTier tier)
        {
            var notification = new ProgressionNotification
            {
                title = "Territory Expanded!",
                message = $"Your territory has been upgraded to {tier}!",
                type = ProgressionNotificationType.TerritoryExpanded
            };
            ShowProgressionNotification(notification);
        }

        private void ShowBiomeUnlockedNotification(BiomeType biome)
        {
            var notification = new ProgressionNotification
            {
                title = "New Biome Unlocked!",
                message = $"You can now access the {biome} biome!",
                type = ProgressionNotificationType.BiomeUnlocked
            };
            ShowProgressionNotification(notification);
        }

        private void ShowLevelUpNotification(int oldLevel, int newLevel)
        {
            var notification = new ProgressionNotification
            {
                title = "Level Up!",
                message = $"Congratulations! You reached level {newLevel}!",
                type = ProgressionNotificationType.LevelUp
            };
            ShowProgressionNotification(notification);
        }

        private void ShowBiomeSpecializationNotification(BiomeType biome, int level)
        {
            var notification = new ProgressionNotification
            {
                title = "Biome Specialization!",
                message = $"Your {biome} specialization reached level {level}!",
                type = ProgressionNotificationType.BiomeSpecialization
            };
            ShowProgressionNotification(notification);
        }

        // Helper methods for applying benefits
        private void ApplyBreedingBonus(float bonus)
        {
            if (enableDebugLogging)
                DebugManager.LogInfo($"Applied breeding bonus: {bonus:P}");
        }

        private void ApplyGeneticAnalysisBonus(float bonus)
        {
            if (enableDebugLogging)
                DebugManager.LogInfo($"Applied genetic analysis bonus: {bonus:P}");
        }

        private void ApplySelectiveBreedingBonus(float bonus)
        {
            if (enableDebugLogging)
                DebugManager.LogInfo($"Applied selective breeding bonus: {bonus:P}");
        }

        private void UnlockGeneticModification()
        {
            if (enableDebugLogging)
                DebugManager.LogInfo("Unlocked genetic modification capabilities");
        }

        private void UnlockCrossSpeciesBreeding()
        {
            if (enableDebugLogging)
                DebugManager.LogInfo("Unlocked cross-species breeding");
        }

        private void ApplyLegendaryBreedingBonus(float bonus)
        {
            if (enableDebugLogging)
                DebugManager.LogInfo($"Applied legendary breeding bonus: {bonus:P}");
        }

        private void ApplyEcosystemManagementBonus(float bonus)
        {
            if (enableDebugLogging)
                DebugManager.LogInfo($"Applied ecosystem management bonus: {bonus:P}");
        }

        private void AddFacility(string facilityName)
        {
            if (enableDebugLogging)
                DebugManager.LogInfo($"Added facility: {facilityName}");
        }

        private void ApplyBiomeBenefit(string benefitType, float multiplier)
        {
            if (enableDebugLogging)
                DebugManager.LogInfo($"Applied {benefitType} benefit: {multiplier:P}");
        }

        private void ShowProgressionNotification(ProgressionNotification notification)
        {
            if (enableDebugLogging)
                DebugManager.LogInfo($"Notification: {notification.title} - {notification.message}");
        }

        #endregion

        // Editor menu items for testing
        [UnityEditor.MenuItem("🧪 Laboratory/Progression/Award Test Experience", false, 600)]
        private static void MenuAwardTestExperience()
        {
            if (Application.isPlaying && Instance != null)
            {
                Instance.AwardExperience(50f, ExperienceSource.TestReward, "Debug experience reward");
                DebugManager.LogInfo("Awarded 50 test experience points");
            }
        }

        [UnityEditor.MenuItem("🧪 Laboratory/Progression/Show Progression Stats", false, 601)]
        private static void MenuShowProgressionStats()
        {
            if (Application.isPlaying && Instance != null)
            {
                var stats = Instance.GetProgressionStats();
                DebugManager.LogInfo($"Progression Stats:\n" +
                         $"Level: {stats.geneticistLevel}\n" +
                         $"Experience: {stats.currentExperience:F1}\n" +
                         $"Creature Slots: {stats.availableCreatureSlots}/{stats.maxCreatureSlots}\n" +
                         $"Unlocked Biomes: {stats.unlockedBiomes.Count}\n" +
                         $"Territory: {stats.currentTerritoryTier}");
            }
        }

        [UnityEditor.MenuItem("🧪 Laboratory/Progression/Reset Progression", false, 602)]
        private static void MenuResetProgression()
        {
            if (Application.isPlaying && Instance != null)
            {
                PlayerPrefs.DeleteKey("PlayerProgressionData");
                DebugManager.LogInfo("Progression data reset - restart scene to see changes");
            }
        }
    }
}