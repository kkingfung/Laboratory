using Unity.Entities;
using Unity.Mathematics;
using System;
using Laboratory.Chimera.Creatures;
using Laboratory.Chimera.Core;

namespace Laboratory.Chimera.ECS
{
    /// <summary>
    /// Component containing basic creature definition data
    /// </summary>
    public struct CreatureDefinitionComponent : IComponentData
    {
        public int SpeciesId;
        public int MaxHealth;
        public int BaseAttack;
        public int BaseDefense;
        public int BaseSpeed;
    }
    
    /// <summary>
    /// Component for creature statistics
    /// </summary>
    public struct CreatureStatsComponent : IComponentData
    {
        public CreatureStats BaseStats;
        public int CurrentHealth;
        public int MaxHealth;
        public int Level;
        public int Experience;
        public float HealthRegenRate;

        // Additional properties for UI compatibility
        public int currentHealth => CurrentHealth;
        public int maxHealth => MaxHealth;
    }
    
    /// <summary>
    /// Component for creature movement data
    /// </summary>
    public struct CreatureMovementComponent : IComponentData
    {
        public float BaseSpeed;
        public float CurrentSpeed;
        public float RotationSpeed;
        public float3 TargetPosition;
        public bool IsMoving;
        public bool HasDestination;
    }
    
    /// <summary>
    /// Component for creature age and life stage
    /// </summary>
    public struct CreatureAgeComponent : IComponentData
    {
        public int AgeInDays;
        public long BirthTime; // DateTime.ToBinary()
        public bool IsAdult;
        public LifeStage LifeStage;
        public float AgingRate;
        public float MaturationProgress;

        // Property accessors for UI compatibility
        public int ageInDays => AgeInDays;
        public float maturationProgress => MaturationProgress;
        public LifeStage currentLifeStage => LifeStage;
    }
    
    /// <summary>
    /// Component for creature genetics data
    /// </summary>
    public struct CreatureGeneticsComponent : IComponentData
    {
        public int Generation;
        public float GeneticPurity;
        public bool IsShiny;
        public Guid ParentId1;
        public Guid ParentId2;
        public int ActiveGeneCount;

        // Individual trait values for UI compatibility
        public float StrengthTrait;
        public float VitalityTrait;
        public float AgilityTrait;
        public float ResilienceTrait;
        public float IntellectTrait;
        public float CharmTrait;
        public Guid LineageId;

        // Property accessors for case-insensitive access
        public float strengthTrait => StrengthTrait;
        public float vitalityTrait => VitalityTrait;
        public float agilityTrait => AgilityTrait;
        public float resilienceTrait => ResilienceTrait;
        public float intellectTrait => IntellectTrait;
        public float charmTrait => CharmTrait;
        public int generation => Generation;
        public Guid lineageId => LineageId;
    }
    
    /// <summary>
    /// Component for genetic modifiers that affect stats
    /// </summary>
    public struct GeneticModifiersComponent : IComponentData
    {
        public float StrengthModifier;
        public float AgilityModifier;
        public float IntelligenceModifier;
        public float SizeModifier;
        public float SpeedModifier;
        public float HealthModifier;
        public float ColorHue;
        public float PatternIntensity;
    }
    
    /// <summary>
    /// Component for creature AI behavior
    /// </summary>
    public struct CreatureAIComponent : IComponentData
    {
        public AIState CurrentState;
        public float StateTimer;
        public float DetectionRange;
        public float PatrolRadius;
        public float AggressionLevel;
        public float CuriosityLevel;
        public float LoyaltyLevel;
        public float3 PatrolCenter;
    }
    
    /// <summary>
    /// Component for AI target tracking
    /// </summary>
    public struct AITargetComponent : IComponentData
    {
        public Entity CurrentTarget;
        public float3 TargetPosition;
        public float3 LastKnownPosition;
        public bool HasTarget;
        public float LastTargetSeen;
        public float TargetDistance;
    }
    
    /// <summary>
    /// Component for breeding mechanics
    /// </summary>
    public struct BreedingComponent : IComponentData
    {
        public bool CanBreed;
        public float FertilityRate;
        public float LastBreedTime;
        public float BreedingCooldown;
        public bool IsPregnant;
        public float PregnancyTimer;
        public float PregnancyDuration;
        public Entity MateEntity;
    }
    
    /// <summary>
    /// Component for creature personality traits
    /// </summary>
    public struct CreaturePersonalityComponent : IComponentData
    {
        public float Aggression;
        public float Curiosity;
        public float Sociability;
        public float Loyalty;
        public float Playfulness;
        public float Independence;
        public float Fearfulness;
        public float Bravery;
        public float SocialNeed;

        // Property accessors for UI compatibility
        public float bravery => Bravery;
        public float loyalty => Loyalty;
        public float curiosity => Curiosity;
        public float socialNeed => SocialNeed;
        public float playfulness => Playfulness;
    }
    
    /// <summary>
    /// Component for creature bonding and relationships
    /// </summary>
    public struct CreatureBondingComponent : IComponentData
    {
        public bool BondedToPlayer;
        public float BondStrength;
        public Entity BondedEntity;
        public float SocialNeed;
        public float LastInteraction;
        public int FriendCount;
        public float TrustLevel;
        public float Obedience;

        // Property accessors for UI compatibility
        public float bondStrength => BondStrength;
        public float trustLevel => TrustLevel;
        public float obedience => Obedience;
    }
    
    /// <summary>
    /// Component for creature needs and status
    /// </summary>
    public struct CreatureNeedsComponent : IComponentData
    {
        public float Hunger;
        public float Thirst;
        public float Energy;
        public float Social;
        public float Comfort;
        public float Happiness;
        public float LastFed;
        public float LastDrank;
        public float LastRested;
        public float Rest;
        public float Exercise;
        public float Mental;
        public float Stress;

        // Property accessors for UI compatibility
        public float hunger => Hunger;
        public float thirst => Thirst;
        public float rest => Rest;
        public float social => Social;
        public float exercise => Exercise;
        public float mental => Mental;
        public float happiness => Happiness;
        public float stress => Stress;
    }
    
    /// <summary>
    /// Component for creature health management
    /// </summary>
    public struct CreatureHealthComponent : IComponentData
    {
        public int MaxHealth;
        public int CurrentHealth;
        public float RegenerationRate;
        public bool IsAlive;
        public bool IsInvulnerable;
        public float LastDamageTime;
        public int DamageTaken;
    }
    
    /// <summary>
    /// Life stages for creatures
    /// </summary>
    public enum LifeStage : byte
    {
        Egg = 0,
        Juvenile = 1,
        Adult = 2,
        Elder = 3
    }
    
    /// <summary>
    /// AI states for creature behavior
    /// </summary>
    public enum AIState : byte
    {
        Idle = 0,
        Patrol = 1,
        Chase = 2,
        Attack = 3,
        Search = 4,
        Return = 5,
        Follow = 6,
        Flee = 7,
        Guard = 8,
        Rest = 9,
        Feed = 10,
        Socialize = 11,
        Mate = 12
    }

    /// <summary>
    /// Component for creature AI behavior management
    /// </summary>
    public struct CreatureBehaviorComponent : IComponentData
    {
        public AIState currentState;
        public int behaviorType; // AIBehaviorType as int
        public float stateChangeTime;
        public float lastActionTime;
        public Entity currentTarget;
        public float3 patrolCenter;
        public float patrolRadius;
        public bool isStateTransitioning;
    }
    
    /// <summary>
    /// Tag component for creatures that can breed
    /// </summary>
    public struct BreedableTag : IComponentData { }
    
    /// <summary>
    /// Tag component for shiny creatures
    /// </summary>
    public struct ShinyTag : IComponentData { }
    
    /// <summary>
    /// Tag component for wild creatures
    /// </summary>
    public struct WildCreatureTag : IComponentData { }
    
    /// <summary>
    /// Tag component for tamed creatures
    /// </summary>
    public struct TamedCreatureTag : IComponentData { }
    
    /// <summary>
    /// Tag component for creatures that are currently sleeping
    /// </summary>
    public struct SleepingTag : IComponentData { }
    
    /// <summary>
    /// Tag component for creatures that are currently hungry
    /// </summary>
    public struct HungryTag : IComponentData { }
    
    /// <summary>
    /// Tag component for creatures that are currently thirsty
    /// </summary>
    public struct ThirstyTag : IComponentData { }
    
    /// <summary>
    /// Tag component for creatures that are pregnant
    /// </summary>
    public struct PregnantTag : IComponentData { }
    
    /// <summary>
    /// Tag component for creatures that died this frame
    /// </summary>
    public struct DeadTag : IComponentData { }

    /// <summary>
    /// Component for creature lifecycle management
    /// </summary>
    public struct CreatureLifecycleComponent : IComponentData
    {
        public LifeStage CurrentStage;
        public float StageProgress;
        public float NextStageThreshold;
        public bool CanEvolve;
        public float EvolutionTimer;
        public float MetabolismRate;

        public static CreatureLifecycleComponent Create(CreatureAgeComponent age, CreatureGeneticsComponent genetics)
        {
            return new CreatureLifecycleComponent
            {
                CurrentStage = age.LifeStage,
                StageProgress = age.AgeInDays / 100f,
                NextStageThreshold = 50f,
                CanEvolve = age.IsAdult && genetics.Generation > 0,
                EvolutionTimer = 0f,
                MetabolismRate = 1f
            };
        }
    }

    /// <summary>
    /// Component for creature breeding mechanics
    /// </summary>
    public struct CreatureBreedingComponent : IComponentData
    {
        public bool IsReadyToBreed;
        public float BreedingCooldown;
        public float FertilityScore;
        public int OffspringCount;
        public float LastBreedTime;
        public Entity PreferredMate;
        public bool IsPregnant;
        public float PregnancyProgress;

        public static CreatureBreedingComponent Create(CreatureAgeComponent age, CreatureGeneticsComponent genetics, CreaturePersonalityComponent personality)
        {
            return new CreatureBreedingComponent
            {
                IsReadyToBreed = age.IsAdult,
                BreedingCooldown = 0f,
                FertilityScore = genetics.GeneticPurity * 0.8f + personality.Sociability * 0.2f,
                OffspringCount = 0,
                LastBreedTime = 0f,
                PreferredMate = Entity.Null,
                IsPregnant = false,
                PregnancyProgress = 0f
            };
        }
    }

    /// <summary>
    /// Component for creature environmental adaptation
    /// </summary>
    public struct CreatureEnvironmentalComponent : IComponentData
    {
        public Laboratory.Chimera.Core.BiomeType CurrentBiome;
        public Laboratory.Chimera.Core.BiomeType PreferredBiome;
        public float BiomeAdaptation;
        public float EnvironmentalStress;
        public float TemperatureTolerance;
        public float HumidityTolerance;
        public float AltitudeTolerance;

        // Property accessors for UI compatibility
        public float environmentalStress => EnvironmentalStress;

        public static CreatureEnvironmentalComponent Create(object environment, CreatureGeneticsComponent genetics)
        {
            return new CreatureEnvironmentalComponent
            {
                CurrentBiome = Laboratory.Chimera.Core.BiomeType.Forest,
                PreferredBiome = Laboratory.Chimera.Core.BiomeType.Forest,
                BiomeAdaptation = genetics.GeneticPurity,
                EnvironmentalStress = 0f,
                TemperatureTolerance = 0.5f,
                HumidityTolerance = 0.5f,
                AltitudeTolerance = 0.5f
            };
        }
    }

    /// <summary>
    /// Component for linking ECS entities to GameObjects
    /// </summary>
    public struct GameObjectLinkComponent : IComponentData
    {
        public int InstanceID;
        public bool IsActive;

        public static GameObjectLinkComponent Create(UnityEngine.GameObject gameObject)
        {
            return new GameObjectLinkComponent
            {
                InstanceID = gameObject.GetInstanceID(),
                IsActive = gameObject.activeInHierarchy
            };
        }
    }

    /// <summary>
    /// Alternative name for GameObjectLinkComponent for UI compatibility
    /// </summary>
    public struct EntityLinkComponent : IComponentData
    {
        public int InstanceID;
        public bool IsActive;
        public Entity LinkedEntity;

        public static EntityLinkComponent Create(UnityEngine.GameObject gameObject, Entity entity)
        {
            return new EntityLinkComponent
            {
                InstanceID = gameObject.GetInstanceID(),
                IsActive = gameObject.activeInHierarchy,
                LinkedEntity = entity
            };
        }
    }

    /// <summary>
    /// Component for creature biome preferences and interactions
    /// </summary>
    public struct CreatureBiomeComponent : IComponentData
    {
        public Laboratory.Chimera.Core.BiomeType HomeBiome;
        public Laboratory.Chimera.Core.BiomeType CurrentBiome;
        public float BiomeComfort;
        public float BiomeExploration;
        public bool IsNativeToBiome;
        public float BiomeComfortLevel;
        public float AdaptationLevel;

        // Property accessors for UI compatibility
        public Laboratory.Chimera.Core.BiomeType currentBiome => CurrentBiome;
        public float biomeComfortLevel => BiomeComfortLevel;
        public float adaptationLevel => AdaptationLevel;
    }
}
