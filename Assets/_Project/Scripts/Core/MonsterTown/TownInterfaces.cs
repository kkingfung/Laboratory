using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Cysharp.Threading.Tasks;
using Laboratory.Chimera.Breeding;

namespace Laboratory.Core.MonsterTown
{
    /// <summary>
    /// Core interfaces for Monster Town system - integrates with existing Chimera patterns
    /// </summary>

    #region Core Manager Interfaces

    /// <summary>
    /// Main interface for town management - follows existing service pattern
    /// </summary>
    public interface ITownManager
    {
        UniTask InitializeTownAsync();
        bool AddMonsterToTown(MonsterInstance monster);
        UniTask<ActivityResult> SendMonsterToActivity(string monsterId, ActivityType activityType);
        UniTask<bool> ConstructBuilding(BuildingType buildingType, Vector3 position);
        TownResources GetCurrentResources();
        IReadOnlyDictionary<string, MonsterInstance> GetTownMonsters();
        IReadOnlyList<Entity> GetBuildingsOfType(BuildingType buildingType);

        event Action<TownResources> OnResourcesChanged;
        event Action<BuildingConstructedEvent> OnBuildingConstructed;
        event Action<MonsterInstance> OnMonsterAddedToTown;
        event Action<ActivityCompletedEvent> OnActivityCompleted;
    }

    /// <summary>
    /// Building system interface - ECS integrated
    /// </summary>
    public interface IBuildingSystem : IDisposable
    {
        void Initialize(Vector2 townBounds, float gridSize, bool useGrid);
        UniTask<Entity> ConstructBuilding(BuildingConfig config, Vector3 position);
        bool CanPlaceBuilding(BuildingConfig config, Vector3 position);
        void DestroyBuilding(Entity building);
        IReadOnlyList<Entity> GetAllBuildings();
    }

    /// <summary>
    /// Resource management interface
    /// </summary>
    public interface IResourceManager : IDisposable
    {
        void InitializeResources(TownResources startingResources);
        void UpdateResources(TownResources newResources);
        bool CanAfford(TownResources cost);
        void AddResources(TownResources resources);
        void DeductResources(TownResources cost);
        TownResources GetCurrentResources();

        event Action<TownResources> OnResourcesChanged;
    }

    /// <summary>
    /// Activity center management interface
    /// </summary>
    public interface IActivityCenterManager : IDisposable
    {
        UniTask InitializeActivityCenter(ActivityType activityType);
        UniTask<ActivityResult> RunActivity(MonsterInstance monster, ActivityType activityType, MonsterPerformance performance);
        void Update(float deltaTime);
        bool IsActivityAvailable(ActivityType activityType);
        ActivityCenterInfo GetActivityCenterInfo(ActivityType activityType);
    }

    #endregion

    #region Data Structures

    /// <summary>
    /// Town resource management - coins, gems, tokens, etc.
    /// </summary>
    [Serializable]
    public struct TownResources
    {
        public int coins;
        public int gems;
        public int activityTokens;
        public int geneticSamples;
        public int materials;
        public int energy;

        public static TownResources Zero => new TownResources();

        public static TownResources GetDefault() => new TownResources
        {
            coins = 1000,
            gems = 10,
            activityTokens = 50,
            geneticSamples = 5,
            materials = 100,
            energy = 100
        };

        public bool HasAnyResource() => coins > 0 || gems > 0 || activityTokens > 0 || geneticSamples > 0 || materials > 0 || energy > 0;

        public bool CanAfford(TownResources cost)
        {
            return coins >= cost.coins &&
                   gems >= cost.gems &&
                   activityTokens >= cost.activityTokens &&
                   geneticSamples >= cost.geneticSamples &&
                   materials >= cost.materials &&
                   energy >= cost.energy;
        }

        public static TownResources operator +(TownResources a, TownResources b)
        {
            return new TownResources
            {
                coins = a.coins + b.coins,
                gems = a.gems + b.gems,
                activityTokens = a.activityTokens + b.activityTokens,
                geneticSamples = a.geneticSamples + b.geneticSamples,
                materials = a.materials + b.materials,
                energy = a.energy + b.energy
            };
        }

        public static TownResources operator -(TownResources a, TownResources b)
        {
            return new TownResources
            {
                coins = Mathf.Max(0, a.coins - b.coins),
                gems = Mathf.Max(0, a.gems - b.gems),
                activityTokens = Mathf.Max(0, a.activityTokens - b.activityTokens),
                geneticSamples = Mathf.Max(0, a.geneticSamples - b.geneticSamples),
                materials = Mathf.Max(0, a.materials - b.materials),
                energy = Mathf.Max(0, a.energy - b.energy)
            };
        }

        public static TownResources operator *(TownResources resources, float multiplier)
        {
            return new TownResources
            {
                coins = Mathf.RoundToInt(resources.coins * multiplier),
                gems = Mathf.RoundToInt(resources.gems * multiplier),
                activityTokens = Mathf.RoundToInt(resources.activityTokens * multiplier),
                geneticSamples = Mathf.RoundToInt(resources.geneticSamples * multiplier),
                materials = Mathf.RoundToInt(resources.materials * multiplier),
                energy = Mathf.RoundToInt(resources.energy * multiplier)
            };
        }

        public override string ToString()
        {
            return $"Coins: {coins}, Gems: {gems}, Tokens: {activityTokens}, Samples: {geneticSamples}, Materials: {materials}, Energy: {energy}";
        }
    }

    /// <summary>
    /// Monster instance for town management - extends existing creature system
    /// </summary>
    [Serializable]
    public class MonsterInstance
    {
        public string UniqueId { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = "Monster";
        public GeneticProfile GeneticProfile { get; set; }
        public MonsterStats Stats { get; set; } = new MonsterStats();
        public Dictionary<ActivityType, float> ActivityExperience { get; set; } = new();
        public List<string> Equipment { get; set; } = new();
        public float Happiness { get; set; } = 0.8f;
        public bool IsInTown { get; set; } = false;
        public TownLocation CurrentLocation { get; set; } = TownLocation.TownCenter;
        public DateTime LastActivityTime { get; set; } = DateTime.UtcNow;

        public float GetActivityExperience(ActivityType activityType)
        {
            return ActivityExperience.TryGetValue(activityType, out var exp) ? exp : 0f;
        }

        public void AddActivityExperience(ActivityType activityType, float amount)
        {
            if (!ActivityExperience.ContainsKey(activityType))
                ActivityExperience[activityType] = 0f;

            ActivityExperience[activityType] += amount;
        }

        public void ImproveStatsFromActivity(ActivityType activityType, float performanceRating)
        {
            var improvement = performanceRating * 0.1f; // Small stat improvements

            switch (activityType)
            {
                case ActivityType.Racing:
                    Stats.speed += improvement;
                    Stats.agility += improvement * 0.5f;
                    break;
                case ActivityType.Combat:
                    Stats.strength += improvement;
                    Stats.vitality += improvement * 0.5f;
                    break;
                case ActivityType.Puzzle:
                    Stats.intelligence += improvement;
                    Stats.adaptability += improvement * 0.5f;
                    break;
            }
        }

        public void AdjustHappiness(float change)
        {
            Happiness = Mathf.Clamp01(Happiness + change);
        }
    }

    /// <summary>
    /// Monster statistics for activities
    /// </summary>
    [Serializable]
    public struct MonsterStats
    {
        public float strength;
        public float agility;
        public float vitality;
        public float intelligence;
        public float social;
        public float adaptability;
        public float speed;
        public float charisma;

        public static MonsterStats GetDefault() => new MonsterStats
        {
            strength = 50f,
            agility = 50f,
            vitality = 50f,
            intelligence = 50f,
            social = 50f,
            adaptability = 50f,
            speed = 50f,
            charisma = 50f
        };
    }

    /// <summary>
    /// Monster performance calculation for activities
    /// </summary>
    [Serializable]
    public struct MonsterPerformance
    {
        public float basePerformance;
        public float geneticBonus;
        public float equipmentBonus;
        public float experienceBonus;
        public float happinessModifier;

        public float CalculateTotal()
        {
            return Mathf.Clamp01(basePerformance + geneticBonus + equipmentBonus + experienceBonus + happinessModifier);
        }
    }

    /// <summary>
    /// Activity result data
    /// </summary>
    [Serializable]
    public struct ActivityResult
    {
        public bool IsSuccess { get; set; }
        public ActivityType ActivityType { get; set; }
        public float PerformanceRating { get; set; }
        public TownResources ResourcesEarned { get; set; }
        public float ExperienceGained { get; set; }
        public float HappinessChange { get; set; }
        public string ResultMessage { get; set; }
        public string FailureReason { get; set; }

        public static ActivityResult Success(ActivityType activityType, float performance, TownResources rewards, float experience)
        {
            return new ActivityResult
            {
                IsSuccess = true,
                ActivityType = activityType,
                PerformanceRating = performance,
                ResourcesEarned = rewards,
                ExperienceGained = experience,
                HappinessChange = performance * 0.1f,
                ResultMessage = $"Great performance in {activityType}!"
            };
        }

        public static ActivityResult Failed(string reason)
        {
            return new ActivityResult
            {
                IsSuccess = false,
                FailureReason = reason,
                HappinessChange = -0.05f
            };
        }
    }

    /// <summary>
    /// Activity center information
    /// </summary>
    [Serializable]
    public struct ActivityCenterInfo
    {
        public ActivityType activityType;
        public string name;
        public string description;
        public bool isUnlocked;
        public TownResources entryCost;
        public float difficultyLevel;
        public TownResources baseRewards;
    }

    #endregion

    #region Enums

    /// <summary>
    /// Available activity types - covers all major game genres
    /// </summary>
    public enum ActivityType
    {
        // Core Activity Types
        Racing,
        Combat,
        Puzzle,
        Strategy,
        Platforming,
        Adventure,
        Crafting,
        Music,

        // Extended Activity Types
        Sports,
        Stealth,
        Exploration,
        Rhythm,
        CardGame,
        BoardGame,
        Simulation,
        Detective
    }

    /// <summary>
    /// Building types for town construction
    /// </summary>
    public enum BuildingType
    {
        // Essential Facilities
        BreedingCenter,
        TrainingGrounds,
        ResearchLab,
        MonsterHabitat,
        EquipmentShop,

        // Activity Centers
        ActivityCenter,
        RacingTrack,
        CombatArena,
        PuzzleAcademy,
        StrategyCommand,
        MusicStudio,
        AdventureGuild,
        CraftingWorkshop,

        // Support Buildings
        ResourceGenerator,
        StorageWarehouse,
        SocialHub,
        MedicalCenter,
        Library
    }

    /// <summary>
    /// Monster locations within town
    /// </summary>
    public enum TownLocation
    {
        TownCenter,
        BreedingCenter,
        TrainingGrounds,
        ActivityCenter,
        Habitat,
        Hospital,
        Adventure
    }

    #endregion

    #region Events

    /// <summary>
    /// Town-specific events that integrate with existing event system
    /// </summary>
    public class TownInitializedEvent
    {
        public MonsterTownConfig TownConfig { get; private set; }
        public TownResources InitialResources { get; private set; }

        public TownInitializedEvent(MonsterTownConfig config, TownResources resources)
        {
            TownConfig = config;
            InitialResources = resources;
        }
    }

    public class BuildingConstructedEvent
    {
        public BuildingType BuildingType { get; private set; }
        public Entity BuildingEntity { get; private set; }
        public Vector3 Position { get; private set; }

        public BuildingConstructedEvent(BuildingType buildingType, Entity entity, Vector3 position)
        {
            BuildingType = buildingType;
            BuildingEntity = entity;
            Position = position;
        }
    }

    public class MonsterAddedToTownEvent
    {
        public MonsterInstance Monster { get; private set; }

        public MonsterAddedToTownEvent(MonsterInstance monster)
        {
            Monster = monster;
        }
    }

    public class ActivityCompletedEvent
    {
        public MonsterInstance Monster { get; private set; }
        public ActivityType ActivityType { get; private set; }
        public ActivityResult Result { get; private set; }

        public ActivityCompletedEvent(MonsterInstance monster, ActivityType activityType, ActivityResult result)
        {
            Monster = monster;
            ActivityType = activityType;
            Result = result;
        }
    }

    public class ResourcesChangedEvent
    {
        public TownResources OldResources { get; private set; }
        public TownResources NewResources { get; private set; }
        public TownResources Change { get; private set; }

        public ResourcesChangedEvent(TownResources oldResources, TownResources newResources)
        {
            OldResources = oldResources;
            NewResources = newResources;
            Change = newResources - oldResources;
        }
    }

    #endregion
}