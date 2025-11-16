using Unity.Entities;
using Unity.Mathematics;
using System;
using Laboratory.Chimera.Core;
using Laboratory.Chimera.Creatures;
using Laboratory.Shared.Types;

namespace Laboratory.Chimera.ECS
{
    /// <summary>
    /// ECS component containing immutable creature definition data.
    /// Defines the basic species characteristics that don't change during the creature's lifetime.
    /// Used by combat, breeding, and AI systems to determine creature capabilities.
    /// </summary>
    public struct CreatureDefinitionComponent : IComponentData
    {
        /// <summary>Unique identifier for the creature's species (references CreatureSpeciesConfig)</summary>
        public int SpeciesId;

        /// <summary>Maximum health points this creature can have at full vitality</summary>
        public int MaxHealth;

        /// <summary>Base attack power before modifiers and genetic traits are applied</summary>
        public int BaseAttack;

        /// <summary>Base defensive capability before armor and genetic modifiers</summary>
        public int BaseDefense;

        /// <summary>Base movement speed in units per second before agility modifiers</summary>
        public int BaseSpeed;
    }
    
    /// <summary>
    /// ECS component for dynamic creature statistics that change during gameplay.
    /// Tracks current state, progression, and regeneration rates.
    /// Modified by combat, aging, breeding, and genetic expression systems.
    /// </summary>
    public struct CreatureStatsComponent : IComponentData
    {
        /// <summary>Base statistical values before genetic and temporary modifiers</summary>
        public CreatureStats BaseStats;

        /// <summary>Current health points (0 = dead, MaxHealth = full health)</summary>
        public int CurrentHealth;

        /// <summary>Maximum health points this creature can have (affected by genetics and level)</summary>
        public int MaxHealth;

        /// <summary>Experience level affecting stat multipliers and breeding eligibility</summary>
        public int Level;

        /// <summary>Experience points accumulated through activities and survival</summary>
        public int Experience;

        /// <summary>Health points regenerated per second when not in combat</summary>
        public float HealthRegenRate;
    }
    
    /// <summary>
    /// ECS component controlling creature movement and pathfinding state.
    /// Integrates with AI systems, pathfinding, and animation controllers.
    /// Updated by movement systems based on AI decisions and player input.
    /// </summary>
    public struct CreatureMovementComponent : IComponentData
    {
        /// <summary>Base movement speed without genetic or temporary modifiers (units/second)</summary>
        public float BaseSpeed;

        /// <summary>Current effective movement speed including all modifiers and conditions</summary>
        public float CurrentSpeed;

        /// <summary>How fast the creature can rotate to face new directions (degrees/second)</summary>
        public float RotationSpeed;

        /// <summary>World position the creature is currently moving toward</summary>
        public float3 TargetPosition;

        /// <summary>True if the creature is actively moving, false if stationary</summary>
        public bool IsMoving;

        /// <summary>True if the creature has a valid movement destination set</summary>
        public bool HasDestination;
    }
    
    /// <summary>
    /// ECS component tracking creature aging, life stage progression, and maturation.
    /// Affects breeding eligibility, stat modifiers, and behavioral patterns.
    /// Updated by aging systems and influences creature appearance and capabilities.
    /// </summary>
    public struct CreatureAgeComponent : IComponentData
    {
        /// <summary>Total age of the creature in game days since birth</summary>
        public int AgeInDays;

        /// <summary>Birth timestamp stored as DateTime.ToBinary() for persistence</summary>
        public long BirthTime;

        /// <summary>Whether the creature has reached adult life stage and can breed</summary>
        public bool IsAdult;

        /// <summary>Current life stage affecting stats, behavior, and breeding ability</summary>
        public LifeStage LifeStage;

        /// <summary>Rate at which this creature ages (1.0 = normal, higher = faster aging)</summary>
        public float AgingRate;

        /// <summary>Progress toward next life stage (0.0 to 1.0)</summary>
        public float MaturationProgress;
    }
    
    /// <summary>
    /// ECS component containing creature genetic data and inherited traits.
    /// Determines stat bonuses, appearance variations, and breeding compatibility.
    /// Core data for the genetic breeding system and evolutionary mechanics.
    /// </summary>
    public struct CreatureGeneticsComponent : IComponentData
    {
        /// <summary>Breeding generation number (0 = wild, higher = more bred)</summary>
        public int Generation;

        /// <summary>Genetic purity percentage (0.0-1.0, affects breeding outcomes)</summary>
        public float GeneticPurity;

        /// <summary>Rare coloration variant with enhanced stats (approximately 1/8192 chance)</summary>
        public bool IsShiny;

        /// <summary>Unique ID of first parent for lineage tracking</summary>
        public Guid ParentId1;

        /// <summary>Unique ID of second parent for lineage tracking</summary>
        public Guid ParentId2;

        /// <summary>Number of actively expressed genes (affects stat calculations)</summary>
        public int ActiveGeneCount;

        /// <summary>Unique identifier for this creature's family lineage</summary>
        public Guid LineageId;

        // Core genetic traits that influence creature capabilities
        /// <summary>Physical power trait affecting attack damage and carrying capacity (0.0-1.0)</summary>
        public float StrengthTrait;

        /// <summary>Health and endurance trait affecting max HP and regeneration (0.0-1.0)</summary>
        public float VitalityTrait;

        /// <summary>Speed and dexterity trait affecting movement and dodge chance (0.0-1.0)</summary>
        public float AgilityTrait;

        /// <summary>Environmental resistance trait affecting survival in harsh biomes (0.0-1.0)</summary>
        public float ResilienceTrait;

        /// <summary>Learning and problem-solving trait affecting AI behavior complexity (0.0-1.0)</summary>
        public float IntellectTrait;

        /// <summary>Social interaction trait affecting pack behavior and breeding success (0.0-1.0)</summary>
        public float CharmTrait;
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
        public BiomeType CurrentBiome;
        public BiomeType PreferredBiome;
        public float BiomeAdaptation;
        public float EnvironmentalStress;
        public float TemperatureTolerance;
        public float HumidityTolerance;
        public float AltitudeTolerance;

        public static CreatureEnvironmentalComponent Create(object environment, CreatureGeneticsComponent genetics)
        {
            return new CreatureEnvironmentalComponent
            {
                CurrentBiome = BiomeType.Forest,
                PreferredBiome = BiomeType.Forest,
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
        public BiomeType HomeBiome;
        public BiomeType CurrentBiome;
        public float BiomeComfort;
        public float BiomeExploration;
        public bool IsNativeToBiome;
        public float BiomeComfortLevel;
        public float AdaptationLevel;
    }
}
