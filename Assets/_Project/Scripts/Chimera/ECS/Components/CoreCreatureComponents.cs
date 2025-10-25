using Unity.Entities;
using Unity.Collections;
using Unity.Mathematics;

namespace Laboratory.Chimera.ECS
{
    /// <summary>
    /// UNIFIED creature identity - consolidates scattered identity data
    /// Performance: 128 bytes, cache-friendly
    /// </summary>
    public struct CreatureIdentityComponent : IComponentData
    {
        public FixedString64Bytes Species;
        public FixedString32Bytes CreatureName;
        public uint UniqueID;
        public int Generation;
        public float Age;
        public float MaxLifespan;
        public LifeStage CurrentLifeStage;
        public RarityLevel Rarity;
        public Entity OriginalParent1; // For lineage tracking
        public Entity OriginalParent2;
    }

    /// <summary>
    /// GENETICS 2.0 - Performance-optimized genetic data that DRIVES behavior
    /// Advanced genetics component for Chimera systems
    /// </summary>
    public struct ChimeraGeneticDataComponent : IComponentData
    {
        // Core Behavioral Traits (0-1 range, directly used by behavior systems)
        public float Aggression;      // Territorial behavior, combat willingness
        public float Sociability;    // Flocking, cooperation, group size preference
        public float Curiosity;      // Exploration radius, new area attraction
        public float Caution;        // Flee threshold, risk assessment
        public float Intelligence;   // Learning speed, problem-solving
        public float Metabolism;     // Energy consumption, activity level
        public float Fertility;      // Breeding success rate, offspring count
        public float Dominance;      // Leadership in groups, mating priority

        // Physical Traits
        public float Size;           // Affects territory size, combat, energy needs
        public float Speed;          // Movement speed multiplier
        public float Stamina;        // Energy capacity and recovery
        public float Camouflage;     // Predator avoidance, hunting success

        // Environmental Adaptation
        public float HeatTolerance;  // Desert/volcanic biome performance
        public float ColdTolerance;  // Mountain/arctic biome performance
        public float WaterAffinity;  // Ocean/swamp biome performance
        public float Adaptability;   // How quickly they adapt to new biomes

        // Derived Values (calculated from traits)
        public uint GeneticHash;     // Quick compatibility checking
        public float OverallFitness; // Cached fitness calculation
        public BiomeType NativeBiome;
        public float MutationRate;   // Individual mutation tendency
    }

    /// <summary>
    /// RAW GENETIC SEQUENCE - For complex breeding calculations and evolution
    /// Only loaded when needed for breeding/mutation calculations
    /// ENHANCES: Existing genetic sequence systems
    /// </summary>
    public struct GeneticSequenceComponent : IComponentData
    {
        public FixedList512Bytes<byte> ChromosomeA; // 128 genes
        public FixedList512Bytes<byte> ChromosomeB; // 128 genes
        public FixedList64Bytes<float> EpigeneticFactors; // Environmental switches
        public int ActiveGenes;        // Performance optimization
        public float GeneticStability; // Resistance to harmful mutations
    }

    /// <summary>
    /// BEHAVIOR STATE 2.0 - Unified behavior system
    /// Integrates with existing AI but adds genetics-driven decision making
    /// </summary>
    public struct BehaviorStateComponent : IComponentData
    {
        // Current State
        public CreatureBehaviorType CurrentBehavior;
        public CreatureBehaviorType QueuedBehavior; // Smooth transitions
        public float BehaviorTimer;
        public float BehaviorIntensity; // 0-1, how focused they are
        public Entity PrimaryTarget;   // What they're focused on
        public float3 TargetLocation;  // Where they're trying to go

        // Emotional State (affects all behaviors)
        public float Stress;          // 0-1, reduces performance
        public float Satisfaction;    // 0-1, from meeting needs
        public float SocialBond;      // Current social connection strength
        public float TerritorialPride; // Attachment to current area

        // Decision Making
        public float DecisionConfidence; // How sure they are of current choice
        public FixedList32Bytes<float> BehaviorWeights; // Cached weights for performance
        public float LastDecisionTime;
        public int ConsecutiveBehaviorChanges; // Prevents flip-flopping
    }

    /// <summary>
    /// NEEDS & DRIVES - What motivates creatures moment to moment
    /// Directly influences behavior decisions
    /// </summary>
    public struct CreatureNeedsComponent : IComponentData
    {
        // Basic Survival Needs (0-1, 1 = fully satisfied)
        public float Hunger;          // Drives foraging behavior
        public float Thirst;          // Drives water-seeking
        public float Energy;          // Affects activity level
        public float Comfort;         // Temperature, shelter needs
        public float Safety;          // Influences caution behaviors

        // Social/Reproductive Drives
        public float SocialConnection; // Drives flocking/pack behavior
        public float BreedingUrge;     // Increases with age and season
        public float Territorial;      // Drives territory establishment
        public float Parental;        // Care for offspring

        // Growth/Learning Drives
        public float Exploration;      // Drives curiosity behaviors
        public float Play;            // Learning and social bonding
        public float Dominance;       // Status-seeking behaviors

        // Need Satisfaction Rates (genetics influence these)
        public float HungerDecayRate;
        public float EnergyRecoveryRate;
        public float SocialDecayRate;
    }

    /// <summary>
    /// TERRITORY & SOCIAL RELATIONSHIPS - Who owns what and who knows who
    /// </summary>
    public struct SocialTerritoryComponent : IComponentData
    {
        // Territory Data
        public float3 TerritoryCenter;
        public float TerritoryRadius;
        public float TerritoryQuality;    // Resource richness
        public float DefenseCommitment;   // How hard they'll fight for it
        public bool HasTerritory;
        public Entity TerritoryRival;     // Current challenger

        // Social Relationships (dynamic lists)
        public FixedList128Bytes<Entity> KnownCreatures;     // Who they've met
        public FixedList128Bytes<float> RelationshipStrength; // -1 to 1 for each known creature
        public FixedList128Bytes<byte> RelationshipType;      // Mate, Rival, Pack member, etc.

        // Pack/Group Data
        public Entity PackLeader;
        public Entity PreferredMate;
        public int PackSize;
        public int PreferredPackSize;     // Genetics-influenced
        public float PackLoyalty;

        // Social Memory
        public Entity LastInteracted;
        public float LastInteractionTime;
        public float SocialReputation;    // How others generally view this creature
    }

    /// <summary>
    /// ENVIRONMENTAL ADAPTATION - How creatures interact with their world
    /// ENHANCES: Existing environmental systems
    /// </summary>
    public struct EnvironmentalComponent : IComponentData
    {
        // Current Environment
        public BiomeType CurrentBiome;
        public float3 CurrentPosition;
        public float LocalTemperature;
        public float LocalHumidity;
        public float LocalResourceDensity;
        public float BiomeComfortLevel;   // 0-1, affects all behaviors

        // Adaptation Progress
        public float BiomeAdaptation;     // How well adapted to current biome
        public float AdaptationRate;      // How quickly they adapt (genetics)
        public FixedList32Bytes<float> BiomeExperience; // Familiarity with each biome type

        // Migration & Movement
        public float3 PreferredLocation;  // Where they want to be
        public float MigrationUrge;       // Drive to move to new areas
        public float HomeRangeRadius;     // How far they're willing to travel from territory
        public bool IsMigrating;
        public Entity MigrationTarget;    // Following another creature?

        // Resource Interaction
        public ResourceType PreferredResource;
        public ResourceType SecondaryResource;
        public float ForagingEfficiency;  // Success rate at finding resources
        public float ResourceConsumptionRate;
        public Entity NearestResource;
    }

    /// <summary>
    /// BREEDING & REPRODUCTION 2.0 - Integrated with behavior and territory systems
    /// ENHANCES: Existing breeding systems with behavior integration
    /// </summary>
    public struct BreedingComponent : IComponentData
    {
        // Breeding Status
        public BreedingStatus Status;
        public Entity Partner;
        public Entity PreferredPartner;   // Genetic compatibility preference
        public float BreedingReadiness;   // 0-1, affected by age, health, territory
        public float BreedingCooldown;    // Time until can breed again

        // Courtship & Selection
        public float CourtshipProgress;   // 0-1, how far along courtship is
        public float PartnerCompatibility; // Genetic + personality compatibility
        public int CourtshipAttempts;     // How many times they've tried
        public float Selectiveness;       // How picky they are (genetics + experience)

        // Pregnancy & Offspring
        public float PregnancyProgress;   // 0-1 if pregnant
        public int ExpectedOffspring;     // How many babies expected
        public float OffspringViability;  // Health prediction of babies
        public FixedList64Bytes<uint> OffspringGeneticHashes; // Pre-calculated genetics

        // Breeding Behavior Modifiers
        public bool RequiresTerritory;    // Won't breed without established territory
        public bool SeasonalBreeder;      // Only breeds at certain times
        public float ParentalInvestment;  // How much energy they put into offspring
        public int LifetimeOffspring;     // Total offspring produced
    }

    // BEHAVIOR TAGS - For ultra-fast ECS queries
    public struct IdleBehaviorTag : IComponentData { public float Restfulness; }
    public struct ForagingBehaviorTag : IComponentData { public ResourceType TargetResource; }
    public struct ExploringBehaviorTag : IComponentData { public float3 ExplorationCenter; }
    public struct SocialBehaviorTag : IComponentData { public Entity SocialTarget; }
    public struct TerritorialBehaviorTag : IComponentData { public float AggressionLevel; }
    public struct BreedingBehaviorTag : IComponentData { public Entity BreedingTarget; }
    public struct MigratingBehaviorTag : IComponentData { public float3 Destination; }
    public struct FleeingBehaviorTag : IComponentData { public Entity ThreatSource; }
    public struct ParentingBehaviorTag : IComponentData { public Entity Offspring; }

    // WORLD MANAGEMENT COMPONENTS
    public struct WorldDataComponent : IComponentData
    {
        public float Size;
        public int CreatureCount;
        public int MaxCreatures;
        public float SimulationSpeed;
        public float WorldAge;
        public float Season; // 0-4 for seasonal cycles
    }

    public struct BiomeComponent : IComponentData
    {
        public BiomeType BiomeType;
        public float3 Center;
        public float Radius;
        public float Temperature;
        public float Humidity;
        public float ResourceDensity;
        public float CarryingCapacity; // Max creatures this biome can support
    }

    public struct ResourceComponent : IComponentData
    {
        public ResourceType ResourceType;
        public float Amount;
        public float MaxAmount;
        public float RegenerationRate;
        public float QualityLevel; // How nutritious/valuable this resource is
    }

    // ENUMS
    public enum CreatureBehaviorType : byte
    {
        Idle,
        Foraging,
        Exploring,
        Social,
        Territorial,
        Breeding,
        Migrating,
        Fleeing,
        Parenting,
        Playing,
        Sleeping,
        Hunting
    }

    public enum BreedingStatus : byte
    {
        NotReady,
        Seeking,
        Courting,
        Mating,
        Pregnant,
        Caring,
        Cooldown,
        TooOld
    }

    public enum RelationshipType : byte
    {
        Unknown,
        Neutral,
        Friend,
        Rival,
        Mate,
        Child,
        Parent,
        PackMember,
        PackLeader,
        Enemy,
        Prey,
        Predator
    }

    public enum LifeStage : byte
    {
        Embryo,
        Juvenile,
        Adolescent,
        Adult,
        Elder,
        Ancient
    }

    public enum RarityLevel : byte
    {
        Common,
        Uncommon,
        Rare,
        Epic,
        Legendary,
        Mythic
    }

    public enum BiomeType : byte
    {
        Grassland,
        Forest,
        Desert,
        Mountain,
        Ocean,
        Swamp,
        Tundra,
        Volcanic,
        Cave,
        Sky
    }

    public enum ResourceType : byte
    {
        Plants,
        Fruits,
        SmallAnimals,
        Fish,
        Insects,
        Minerals,
        Water,
        Shelter,
        Energy,
        Medicine
    }
}