using System;
using UnityEngine;
using Laboratory.Shared.Types;

namespace Laboratory.Core.Enums
{
    /// <summary>
    /// Unified trait system: combines specific trait names with categorical organization
    /// High-performance enum for dictionary indexing: Dictionary<TraitType, T>
    /// </summary>
    public enum TraitType : byte
    {
        // Physical Traits (0-15)
        Size = 0,
        Speed = 1,
        Stamina = 2,
        Strength = 3,
        Agility = 4,
        Vitality = 5,

        // Behavioral Traits (16-31)
        Aggression = 16,
        Sociability = 17,
        Curiosity = 18,
        Caution = 19,
        Dominance = 20,
        Playfulness = 21,
        Courage = 22,
        Territoriality = 23,
        HuntingDrive = 24,
        ParentalCare = 25,

        // Mental Traits (32-47)
        Intelligence = 32,
        Memory = 33,
        Learning = 34,
        ProblemSolving = 35,

        // Combat Traits (48-63)
        CombatSkill = 48,
        DefensiveAbility = 49,
        AttackPower = 50,
        CriticalHit = 51,

        // Environmental Traits (64-79)
        HeatTolerance = 64,
        ColdTolerance = 65,
        WaterAffinity = 66,
        Adaptability = 67,
        Camouflage = 68,
        Environmental = 69,
        BiomeAdaptation = 70,
        FireAffinity = 71,
        EarthAffinity = 72,
        AirAffinity = 73,

        // Metabolic Traits (80-95)
        Metabolism = 80,
        Fertility = 81,
        HealingRate = 82,
        EnergyEfficiency = 83,

        // Sensory Traits (96-111)
        Vision = 96,
        Hearing = 97,
        Smell = 98,
        TactileSensitivity = 99,
        NightVision = 100,

        // Social Traits (112-127)
        PackBehavior = 112,
        Loyalty = 113,
        Communication = 114,
        Leadership = 115,

        // Special/Magical Traits (128-143)
        MagicalAffinity = 128,
        ElementalPower = 129,
        Telepathy = 130,
        Regeneration = 131,
        Elemental = 132,
        Magical = 133,
        Arcane = 134,
        Metallic = 135,
        Armor = 136,
        Hardness = 137,

        // Utility Traits (144-159)
        ToolUse = 144,
        EnvironmentManipulation = 145,
        ResourceGathering = 146,

        // Mutation Traits (160-175)
        MutationResistance = 160,
        MutationPotential = 161,
        GeneticStability = 162,
        Mutation = 163,

        // Hidden/Dormant Traits (176-191)
        HiddenPotential = 176,
        DormantAbility = 177,
        LatentPower = 178,

        // Cosmetic Traits (192-207)
        ColorPattern = 192,
        BodyMarkings = 193,
        EyeColor = 194,
        SkinTexture = 195,
        PrimaryColor = 196,
        SecondaryColor = 197,
        ColorComplexity = 198,
        Diversity = 199,
        Expression = 200,

        // General Category Traits (208-223)
        Physical = 208,
        Metabolic = 209,
        Utility = 210
    }

    /// <summary>
    /// Trait categories for organizational purposes
    /// Maps TraitType values to their categories
    /// </summary>
    public enum TraitCategory : byte
    {
        Physical = 0,
        Behavioral = 1,
        Mental = 2,
        Combat = 3,
        Environmental = 4,
        Metabolic = 5,
        Sensory = 6,
        Social = 7,
        Special = 8,
        Utility = 9,
        Mutation = 10,
        Hidden = 11,
        Cosmetic = 12
    }

    /// <summary>
    /// Action types for analytics tracking
    /// Replaces Dictionary<string, T> with Dictionary<ActionType, T>
    /// </summary>
    public enum ActionType : byte
    {
        Exploration = 0,
        Breeding = 1,
        Social = 2,
        Combat = 3,
        Research = 4,
        Quest = 5,
        UI = 6,
        Menu = 7
    }

    /// <summary>
    /// System identifiers for integration management
    /// Replaces Dictionary<string, T> with Dictionary<SystemType, T>
    /// </summary>
    public enum SystemType : byte
    {
        Analytics = 0,
        Breeding = 1,
        Combat = 2,
        Ecosystem = 3,
        Evolution = 4,
        Genetics = 5,
        Networking = 6,
        Quest = 7,
        Storytelling = 8,
        UI = 9,
        Audio = 10,
        Performance = 11,
        Physics = 12,
        Save = 13,
        AI = 14
    }

    /// <summary>
    /// Cross-system event types for type-safe system integration
    /// </summary>
    public enum CrossSystemEventType : byte
    {
        // Adaptation and Evolution Events
        GeneticAdaptation = 0,
        EvolutionBoost = 1,
        EvolutionaryLeap = 2,
        EliteEmergence = 3,
        PopulationBoost = 4,

        // Environmental Events
        EnvironmentalImpact = 5,
        EnvironmentalChallenge = 6,
        EnvironmentalNarrative = 7,
        EcosystemCollapse = 8,

        // Social and Personality Events
        PersonalityQuestGeneration = 9,
        SocialRevolution = 10,

        // Engagement and Analytics Events
        EngagementStoryGeneration = 11,
        TechnologicalBreakthrough = 12,

        // Quest and Narrative Events
        QuestReward = 13,
        QuestNarrative = 14,
        QuestCompletion = 15
    }

    /// <summary>
    /// Compile-time safe keys for cross-system event data to prevent typos
    /// </summary>
    public static class EventDataKeys
    {
        // Creature/Genetic Data
        public const string CreatureId = "creatureId";
        public const string SpeciesId = "speciesId";
        public const string Trait = "trait";
        public const string TraitValue = "traitValue";
        public const string Intensity = "intensity";
        public const string GeneticProfile = "geneticProfile";

        // Environmental Data
        public const string Biome = "biome";
        public const string BiomeId = "biomeId";
        public const string Severity = "severity";
        public const string Duration = "duration";
        public const string Temperature = "temperature";
        public const string Humidity = "humidity";

        // Gameplay Data
        public const string PlayerId = "playerId";
        public const string QuestId = "questId";
        public const string RewardType = "rewardType";
        public const string RewardValue = "rewardValue";
        public const string Experience = "experience";
        public const string Level = "level";

        // AI/Personality Data
        public const string PersonalityType = "personalityType";
        public const string MoodState = "moodState";
        public const string SocialRank = "socialRank";
        public const string BehaviorPattern = "behaviorPattern";

        // System Data
        public const string Timestamp = "timestamp";
        public const string Priority = "priority";
        public const string Context = "context";
        public const string Metadata = "metadata";

        // Cross-System Event Specific Keys
        public const string AdaptationType = "adaptationType";
        public const string BoostType = "boostType";
        public const string Multiplier = "multiplier";
        public const string PlayerArchetype = "playerArchetype";
        public const string EngagementLevel = "engagementLevel";
        public const string EncourageBreeding = "encourageBreeding";
        public const string TargetPopulation = "targetPopulation";
        public const string Event = "event";
        public const string QuestType = "questType";
        public const string Difficulty = "difficulty";
        public const string PreferredType = "preferredType";
        public const string PersonalityDriven = "personality_driven";
        public const string Complexity = "complexity";
        public const string AvgFitness = "avgFitness";
        public const string Creature = "creature";
        public const string Quest = "quest";
    }

    // Note: BiomeType moved to Laboratory.Shared.Types to avoid cyclic dependencies

    /// <summary>
    /// Biome categories for organizational purposes
    /// Maps BiomeType values to their categories
    /// </summary>
    public enum BiomeCategory : byte
    {
        Terrestrial = 0,
        Aquatic = 1,
        Underground = 2,
        Aerial = 3,
        Extreme = 4,
        Magical = 5,
        Dimensional = 6,
        Corrupted = 7,
        Artificial = 8,
        Hybrid = 9,
        Celestial = 10
    }

    /// <summary>
    /// Research types for scientific systems
    /// Replaces Dictionary<string, T> with Dictionary<ResearchType, T>
    /// </summary>
    public enum ResearchType : byte
    {
        Genetics = 0,
        Behavior = 1,
        Evolution = 2,
        Ecology = 3,
        Breeding = 4,
        Adaptation = 5,
        Intelligence = 6,
        Social = 7,
        Technology = 8,
        Discovery = 9
    }

    /// <summary>
    /// Configuration keys for settings management
    /// Replaces Dictionary<string, T> with Dictionary<ConfigKey, T>
    /// </summary>
    public enum ConfigKey : byte
    {
        GraphicsQuality = 0,
        AudioVolume = 1,
        Difficulty = 2,
        LanguageCode = 3,
        AutoSave = 4,
        NetworkMode = 5,
        UIScale = 6,
        FrameRate = 7,
        Resolution = 8,
        VSync = 9
    }

    /// <summary>
    /// Asset types for resource management
    /// Replaces Dictionary<string, T> with Dictionary<AssetType, T>
    /// </summary>
    public enum AssetType : byte
    {
        Texture = 0,
        Model = 1,
        Audio = 2,
        Animation = 3,
        Material = 4,
        Shader = 5,
        Script = 6,
        Prefab = 7,
        Scene = 8,
        Config = 9
    }

    /// <summary>
    /// Parameter keys for analytics data
    /// Replaces Dictionary<string, object> with Dictionary<ParamKey, object>
    /// </summary>
    public enum ParamKey : byte
    {
        ElementName = 0,
        InteractionType = 1,
        InteractionTime = 2,
        ChoiceCategory = 3,
        ChoiceValue = 4,
        DecisionContext = 5,
        EmotionalState = 6,
        Intensity = 7,
        Trigger = 8,
        BreedingType = 9,
        ParentSpecies1 = 10,
        ParentSpecies2 = 11,
        Success = 12,
        Species = 13,
        Biome = 14,
        FirstDiscovery = 15,
        TimeSpent = 16,
        CreaturesDiscovered = 17,
        ResourcesGathered = 18,
        ResearchType = 19,
        TargetTrait = 20,
        Breakthrough = 21,
        ResearchTime = 22,
        QuestType = 23,
        QuestId = 24,
        Progress = 25,
        Completed = 26,
        SessionTime = 27,
        TotalActions = 28,
        FrameRate = 29,
        ActiveCreatures = 30,
        CurrentBiome = 31,
        ActiveQuests = 32,
        QuestProgress = 33,
        NewValue = 34,
        Content = 35,
        PreviousValue = 36,
        Category = 37,
        Choice = 38,
        MomentType = 39,
        Context = 40,
        ParentAFitness = 41,
        ParentBFitness = 42,
        OffspringFitness = 43,
        BreedingTime = 44,
        CreatureId = 45,
        Generation = 46,
        EventDescription = 47,
        EventType = 48,
        QuestDifficulty = 49,
        QuestGeneratedTime = 50
    }

    /// <summary>
    /// Choice categories for gameplay tracking
    /// Replaces string-based choice categories
    /// </summary>
    public enum ChoiceCategory : byte
    {
        QuestCompletion = 0,
        BreedingChoice = 1,
        OffspringDecision = 2,
        BiomeExploration = 3,
        ResearchFocus = 4,
        SocialInteraction = 5
    }

    /// <summary>
    /// Breeding types for breeding system
    /// Replaces string-based breeding types
    /// </summary>
    public enum BreedingType : byte
    {
        Natural = 0,
        Assisted = 1,
        Experimental = 2,
        Hybrid = 3,
        Enhanced = 4
    }

    /// <summary>
    /// Environmental condition types for ecosystem simulation
    /// Replaces Dictionary<string, float> for environmental conditions
    /// </summary>
    public enum EnvironmentalCondition : byte
    {
        Temperature = 0,
        Humidity = 1,
        ResourceAvailability = 2,
        PopulationDensity = 3,
        PredatorPresence = 4,
        DiseaseLevel = 5,
        ToxicityLevel = 6,
        OxygenLevel = 7
    }

    /// <summary>
    /// Player narrative preference types for storytelling system
    /// Replaces Dictionary<string, float> for player preferences
    /// </summary>
    public enum NarrativePreference : byte
    {
        DramaticIntensity = 0,
        HumorLevel = 1,
        TechnicalDetail = 2,
        CharacterFocus = 3,
        ActionPaced = 4,
        MysteryElements = 5,
        ScienceElements = 6,
        SocialElements = 7
    }

    /// <summary>
    /// Narrative theme types for storytelling system
    /// Replaces Dictionary<string, float> for theme popularity
    /// </summary>
    public enum NarrativeTheme : byte
    {
        Discovery = 0,
        Evolution = 1,
        Social = 2,
        Survival = 3,
        Mystery = 4,
        Adventure = 5,
        Scientific = 6,
        Dramatic = 7,
        Romantic = 8,
        Horror = 9
    }

    /// <summary>
    /// Performance metric types for breeding analysis
    /// Replaces Dictionary<string, float> for performance metrics
    /// </summary>
    public enum PerformanceMetric : byte
    {
        FitnessAccuracy = 0,
        TraitDiversity = 1,
        GenerationImprovement = 2,
        PredictionAccuracy = 3,
        BreedingSuccess = 4,
        MutationRate = 5,
        AdaptationSpeed = 6,
        SurvivalRate = 7
    }

    /// <summary>
    /// Ecosystem stress factors for analysis and recommendations
    /// Replaces List<string> stressFactors and recommendations
    /// </summary>
    public enum EcosystemStressFactor : byte
    {
        OverpopulationStress = 0,
        ResourceDepletion = 1,
        BiomeDegradation = 2,
        TemperatureExtreme = 3,
        PredatorImbalance = 4,
        DiseaseOutbreak = 5,
        HabitatFragmentation = 6,
        PollutionLevel = 7,
        ClimateInstability = 8,
        FoodChainDisruption = 9
    }

    /// <summary>
    /// Ecosystem management recommendations
    /// Replaces List<string> recommendations
    /// </summary>
    public enum EcosystemRecommendation : byte
    {
        ReducePopulationDensity = 0,
        IncreaseResourceGeneration = 1,
        RestoreBiomeHealth = 2,
        RegulateTemperature = 3,
        RebalancePredators = 4,
        ImplementDiseaseControl = 5,
        ConnectHabitats = 6,
        ReducePollution = 7,
        StabilizeClimate = 8,
        RestoreFoodChain = 9,
        MonitorResourceLevels = 10,
        DiversifySpecies = 11
    }

    /// <summary>
    /// Player archetype types for behavioral classification
    /// Replaces string-based archetype identification
    /// </summary>
    public enum ArchetypeType : byte
    {
        Explorer = 0,
        Breeder = 1,
        Achiever = 2,
        Socializer = 3,
        Competitor = 4,
        Creator = 5,
        Unknown = 6
    }

    /// <summary>
    /// Gameplay style preferences for player analytics
    /// Replaces string-based gameplay style tracking
    /// </summary>
    public enum GameplayStyle : byte
    {
        Casual = 0,
        Hardcore = 1,
        Strategic = 2,
        Creative = 3,
        Competitive = 4,
        Collaborative = 5,
        Story = 6,
        Experimental = 7
    }

    /// <summary>
    /// Analytics metric types for performance tracking
    /// Replaces Dictionary<string, float> for metrics
    /// </summary>
    public enum MetricType : byte
    {
        RecentSuccessRate = 0,
        SessionDuration = 1,
        ActionsPerMinute = 2,
        FocusScore = 3,
        SatisfactionLevel = 4,
        OverallEngagement = 5,
        PerformanceConsistency = 6,
        LearningCurveProgression = 7,
        AdaptabilityScore = 8,
        RiskTolerance = 9,
        PatienceLevel = 10,
        RepetitiveScore = 11,
        EngagementLevel = 12,
        BehaviorVariety = 13,
        AverageSessionLength = 14
    }

    /// <summary>
    /// Input pattern types for input analytics
    /// Replaces Dictionary<string, float> for input patterns
    /// </summary>
    public enum InputType : byte
    {
        MouseSensitivity = 0,
        ClickFrequency = 1,
        KeyboardSpeed = 2,
        MovementPrecision = 3,
        ReactionTime = 4,
        InputAccuracy = 5,
        TimingConsistency = 6,
        GestureComplexity = 7,
        Mouse = 8,
        Keyboard = 9,
        Gamepad = 10,
        Touch = 11,
        Click = 12,
        Drag = 13,
        Scroll = 14,
        Hover = 15,
        KeyPress = 16,
        KeyCombo = 17,
        AverageClickTime = 18,
        AverageDragDistance = 19,
        AverageHoverTime = 20,
        InputFrequency = 21
    }

    /// <summary>
    /// Engagement factor types for player engagement analysis
    /// Replaces Dictionary<string, float> for engagement factors
    /// </summary>
    public enum EngagementType : byte
    {
        ContentEngagement = 0,
        SocialEngagement = 1,
        ProgressEngagement = 2,
        ExplorationEngagement = 3,
        CreativeEngagement = 4,
        CompetitiveEngagement = 5,
        LearningEngagement = 6,
        EmotionalEngagement = 7
    }

    /// <summary>
    /// Preference types for player preference tracking
    /// Replaces Dictionary<string, float> preferences
    /// </summary>
    public enum PreferenceType : byte
    {
        Exploration = 0,
        Breeding = 1,
        Social = 2,
        Combat = 3,
        Research = 4,
        Creative = 5,
        Achievement = 6,
        Story = 7,
        Competition = 8,
        Relaxation = 9,
        Challenge = 10,
        Discovery = 11
    }

    /// <summary>
    /// Personality types for player psychological profiling
    /// Replaces string-based personality identification
    /// </summary>
    public enum PersonalityType : byte
    {
        Balanced = 0,
        Analytical = 1,
        Creative = 2,
        Social = 3,
        Competitive = 4,
        Casual = 5,
        Perfectionist = 6,
        Explorer = 7,
        Achiever = 8
    }

    /// <summary>
    /// Insight types for behavioral analysis categorization
    /// Replaces string-based insight classification
    /// </summary>
    public enum InsightType : byte
    {
        HighTrait = 0,
        LowTrait = 1,
        LongSession = 2,
        HighVariety = 3,
        Focused = 4,
        Achievement = 5,
        Performance = 6,
        Social = 7,
        Learning = 8,
        Adaptation = 9
    }

    /// <summary>
    /// Emotional moment types for player experience tracking
    /// Replaces string-based moment categorization
    /// </summary>
    public enum MomentType : byte
    {
        Breakthrough = 0,
        Discovery = 1,
        Mastery = 2,
        EngagementSpike = 3,
        Frustration = 4,
        Flow = 5,
        Success = 6,
        Challenge = 7,
        Social = 8,
        Creative = 9
    }

    /// <summary>
    /// Behavior pattern types for player analysis
    /// Replaces List<string> patterns in behavior metrics
    /// </summary>
    public enum PatternType : byte
    {
        Repetitive = 0,
        Exploratory = 1,
        Social = 2,
        Competitive = 3,
        Creative = 4,
        Achievement = 5,
        Learning = 6,
        Adaptive = 7,
        Focused = 8,
        Diverse = 9,
        Methodical = 10,
        Impulsive = 11,
        Strategic = 12,
        Casual = 13
    }

    /// <summary>
    /// Context categories for data organization
    /// Replaces Dictionary<string, object> for contextual data
    /// </summary>
    public enum ContextType : byte
    {
        Location = 0,
        Activity = 1,
        Performance = 2,
        Social = 3,
        Timing = 4,
        Environment = 5,
        Emotion = 6,
        Progress = 7,
        Challenge = 8,
        Resource = 9
    }

    /// <summary>
    /// Milestone types for session tracking
    /// Replaces List<string> sessionMilestones
    /// </summary>
    public enum MilestoneType : byte
    {
        FirstSession = 0,
        HighActivity = 1,
        LongSession = 2,
        Achievement = 3,
        Discovery = 4,
        Social = 5,
        Mastery = 6,
        Breakthrough = 7,
        Completion = 8,
        Progress = 9
    }

    /// <summary>
    /// Risk factor types for behavioral analysis
    /// Replaces List<string> riskFactors
    /// </summary>
    public enum RiskFactorType : byte
    {
        Frustration = 0,
        Disengagement = 1,
        Repetitive = 2,
        Isolation = 3,
        Impatience = 4,
        Burnout = 5,
        Confusion = 6,
        Difficulty = 7,
        Boredom = 8,
        Anxiety = 9
    }

    /// <summary>
    /// Strength area types for positive behavioral patterns
    /// Replaces List<string> strengthAreas
    /// </summary>
    public enum StrengthAreaType : byte
    {
        Flow = 0,
        Performance = 1,
        Adaptation = 2,
        Social = 3,
        Creative = 4,
        Strategic = 5,
        Learning = 6,
        Persistence = 7,
        Leadership = 8,
        Problem = 9
    }

    /// <summary>
    /// Adaptation data keys for game adaptation engine
    /// Replaces Dictionary<string, object> for adaptation data
    /// </summary>
    public enum AdaptationKey : byte
    {
        Type = 0,
        Intensity = 1,
        Trigger = 2,
        PlayerArchetype = 3,
        Timestamp = 4,
        Context = 5,
        Success = 6,
        Performance = 7,
        Difficulty = 8,
        Response = 9
    }

    /// <summary>
    /// Quest parameter keys for quest data tracking
    /// Replaces Dictionary<string, object> questParameters
    /// </summary>
    public enum QuestKey : byte
    {
        Objective = 0,
        Reward = 1,
        Progress = 2,
        Requirements = 3,
        Status = 4,
        Priority = 5,
        Category = 6,
        Target = 7,
        Condition = 8,
        Timeout = 9
    }

    /// <summary>
    /// Event data keys for ecosystem event tracking
    /// Replaces Dictionary<string, object> eventData
    /// </summary>
    public enum EventKey : byte
    {
        Source = 0,
        Target = 1,
        Magnitude = 2,
        Duration = 3,
        Cause = 4,
        Effect = 5,
        Participants = 6,
        Environment = 7,
        Outcome = 8,
        Category = 9
    }

    /// <summary>
    /// Quest types for quest classification and tracking
    /// Replaces string-based quest type identification
    /// </summary>
    public enum QuestType : byte
    {
        Exploration = 0,
        Collection = 1,
        Breeding = 2,
        Research = 3,
        Combat = 4,
        Discovery = 5,
        Social = 6,
        Achievement = 7,
        Story = 8,
        Challenge = 9,
        Tutorial = 10,
        Daily = 11,
        Weekly = 12,
        Seasonal = 13,
        Epic = 14,
        Hidden = 15
    }

    /// <summary>
    /// Event types for ecosystem and game event classification
    /// Replaces string-based event type identification
    /// </summary>
    public enum EventType : byte
    {
        PopulationChange = 0,
        ResourceChange = 1,
        BiomeShift = 2,
        ClimateEvent = 3,
        SpeciesEvent = 4,
        PlayerAction = 5,
        SystemEvent = 6,
        EnvironmentalEvent = 7,
        SocialEvent = 8,
        BreedingEvent = 9,
        DiscoveryEvent = 10,
        QuestEvent = 11,
        AchievementEvent = 12,
        ErrorEvent = 13,
        DebugEvent = 14,
        Unknown = 15
    }

    /// <summary>
    /// Season types for ecosystem seasonal changes
    /// Replaces string-based season identification
    /// </summary>
    public enum Season : byte
    {
        Spring = 0,
        Summer = 1,
        Autumn = 2,
        Winter = 3
    }

    /// <summary>
    /// Adaptation types for environmental adaptation events
    /// Replaces string-based adaptation type identification
    /// </summary>
    public enum AdaptationType : byte
    {
        Temperature = 0,
        Humidity = 1,
        Altitude = 2,
        Depth = 3,
        Salinity = 4,
        Pressure = 5,
        Light = 6,
        Nutrition = 7,
        Toxicity = 8,
        Radiation = 9,
        Gravity = 10,
        Magnetism = 11,
        Biome = 12,
        Social = 13,
        Predator = 14,
        Resource = 15
    }

    /// <summary>
    /// Food types for creature dietary preferences
    /// Replaces string-based food type identification
    /// </summary>
    public enum FoodType : byte
    {
        // Plant-based foods (0-31)
        Grass = 0,
        Leaves = 1,
        Berries = 2,
        Fruits = 3,
        Nuts = 4,
        Seeds = 5,
        Flowers = 6,
        Roots = 7,
        Bark = 8,
        Algae = 9,
        Moss = 10,
        Fungi = 11,
        Nectar = 12,
        Pollen = 13,
        Sap = 14,
        Grain = 15,

        // Animal-based foods (32-63)
        Insects = 32,
        SmallFish = 33,
        LargeFish = 34,
        Birds = 35,
        SmallMammals = 36,
        LargeMammals = 37,
        Eggs = 38,
        Larvae = 39,
        Worms = 40,
        Mollusks = 41,
        Crustaceans = 42,
        Amphibians = 43,
        Reptiles = 44,
        Carrion = 45,
        Blood = 46,
        Milk = 47,

        // Mineral/Other foods (64-95)
        Minerals = 64,
        Salt = 65,
        Clay = 66,
        Bone = 67,
        Crystal = 68,
        Energy = 69,
        Magic = 70,
        Essence = 71,
        Plankton = 72,
        Honey = 73,
        Resin = 74,
        Spores = 75,

        // Special/Synthetic foods (96-127)
        Synthetic = 96,
        Processed = 97,
        Enhanced = 98,
        Manufactured = 99,
        Supplement = 100,
        Medicine = 101,
        Poison = 102,
        Unknown = 103
    }

    /// <summary>
    /// Predator types for creature threat identification
    /// Replaces string-based predator type identification
    /// </summary>
    public enum PredatorType : byte
    {
        // Small predators (0-15)
        SmallBird = 0,
        SmallMammal = 1,
        SmallReptile = 2,
        Insect = 3,
        Spider = 4,
        Rodent = 5,
        SmallFish = 6,
        Weasel = 7,

        // Medium predators (16-31)
        MediumBird = 16,
        MediumMammal = 17,
        MediumReptile = 18,
        Wolf = 19,
        Wildcat = 20,
        Hawk = 21,
        Snake = 22,
        MediumFish = 23,
        Coyote = 24,
        Fox = 25,
        Badger = 26,
        Otter = 27,

        // Large predators (32-47)
        LargeBird = 32,
        LargeMammal = 33,
        LargeReptile = 34,
        Bear = 35,
        BigCat = 36,
        Eagle = 37,
        Crocodile = 38,
        Shark = 39,
        Tiger = 40,
        Lion = 41,
        Leopard = 42,
        Jaguar = 43,

        // Apex predators (48-63)
        ApexMammal = 48,
        ApexReptile = 49,
        ApexBird = 50,
        ApexFish = 51,
        Dragon = 52,
        Griffin = 53,
        Phoenix = 54,
        Kraken = 55,

        // Environmental threats (64-79)
        Human = 64,
        Disease = 65,
        Parasite = 66,
        Pollution = 67,
        Climate = 68,
        Starvation = 69,
        Habitat = 70,
        Competition = 71,

        // Magical/Mythical threats (80-95)
        MagicalBeast = 80,
        Demon = 81,
        Spirit = 82,
        Elemental = 83,
        Undead = 84,
        Construct = 85,
        Aberration = 86,
        Celestial = 87,

        // Unknown/Other (96-127)
        Unknown = 96,
        Multiple = 97,
        Seasonal = 98,
        Territorial = 99,
        Migratory = 100,
        Extinct = 101,
        Artificial = 102,
        Experimental = 103
    }

    /// <summary>
    /// Food categories for organizational purposes
    /// Maps FoodType values to their categories
    /// </summary>
    public enum FoodCategory : byte
    {
        Plant = 0,
        Animal = 1,
        Mineral = 2,
        Special = 3
    }

    /// <summary>
    /// Predator categories for organizational purposes
    /// Maps PredatorType values to their categories
    /// </summary>
    public enum PredatorCategory : byte
    {
        Small = 0,
        Medium = 1,
        Large = 2,
        Apex = 3,
        Environmental = 4,
        Magical = 5,
        Unknown = 6
    }

    /// <summary>
    /// Game state data keys for quest generation context
    /// Replaces Dictionary<string, object> gameData with Dictionary<GameStateKey, object>
    /// Provides type-safe access to game state information
    /// </summary>
    public enum GameStateKey : byte
    {
        ActivePersonalities = 0,
        UnexploredAreaRatio = 1,
        ResourceAbundance = 2,
        EnvironmentalFactors = 3,
        SessionTime = 4,
        PlayerArchetype = 5,
        EcosystemHealth = 6,
        CreaturePopulation = 7,
        AverageFitness = 8,
        BiomeStability = 9,
        QuestProgress = 10,
        PlayerEngagement = 11
    }

    /// <summary>
    /// Session data keys for player analytics tracking
    /// Replaces Dictionary<string, object> sessionData with Dictionary<SessionDataKey, object>
    /// Provides type-safe access to session analytics data
    /// </summary>
    public enum SessionDataKey : byte
    {
        // Choice tracking patterns
        ChoicePatterns = 0,
        ChoiceFrequency = 1,
        ChoicePreferences = 2,

        // UI interaction patterns
        UIPatterns = 10,
        UIFrequency = 11,
        UIElementUsage = 12,
        UIInteractionTypes = 13,

        // Session metrics
        SessionDuration = 20,
        ActionCount = 21,
        EngagementLevel = 22,

        // Behavioral patterns
        BehaviorPatterns = 30,
        DecisionSpeed = 31,
        ErrorRate = 32,

        // Performance metrics
        ResponseTime = 40,
        AccuracyMetrics = 41,
        EfficiencyScore = 42
    }

    /// <summary>
    /// Extension methods for unified enums
    /// Provides utility functions and category mappings
    /// </summary>
    public static class ChimeraEnumExtensions
    {
        /// <summary>
        /// Gets the category for a trait type
        /// </summary>
        public static TraitCategory GetCategory(this TraitType trait)
        {
            return (TraitCategory)((byte)trait / 16);
        }

        /// <summary>
        /// Gets the category for a biome type
        /// </summary>
        public static BiomeCategory GetCategory(this BiomeType biome)
        {
            return (BiomeCategory)((byte)biome / 16);
        }

        /// <summary>
        /// Checks if this trait type can be inherited normally
        /// </summary>
        public static bool IsInheritable(this TraitType trait)
        {
            var category = trait.GetCategory();
            return category switch
            {
                TraitCategory.Mutation => false, // Mutations are special
                TraitCategory.Hidden => false,   // Hidden traits have special rules
                _ => true
            };
        }

        /// <summary>
        /// Gets the base mutation rate for this trait type
        /// </summary>
        public static float GetBaseMutationRate(this TraitType trait)
        {
            var category = trait.GetCategory();
            return category switch
            {
                TraitCategory.Physical => 0.02f,
                TraitCategory.Behavioral => 0.03f,
                TraitCategory.Combat => 0.01f,
                TraitCategory.Mental => 0.015f,
                TraitCategory.Environmental => 0.03f,
                TraitCategory.Social => 0.025f,
                TraitCategory.Mutation => 0.1f,  // Mutations beget mutations
                TraitCategory.Hidden => 0.001f,  // Very rare to activate
                TraitCategory.Sensory => 0.02f,
                TraitCategory.Metabolic => 0.015f,
                TraitCategory.Utility => 0.025f,
                TraitCategory.Special => 0.005f,
                TraitCategory.Cosmetic => 0.05f,
                _ => 0.02f
            };
        }

        /// <summary>
        /// Gets the display name for a trait type
        /// </summary>
        public static string GetDisplayName(this TraitType trait)
        {
            return trait.ToString();
        }

        /// <summary>
        /// Gets the display name for a biome type
        /// </summary>
        public static string GetDisplayName(this BiomeType biome)
        {
            return biome.ToString();
        }

        /// <summary>
        /// Gets the color associated with this trait category for UI display
        /// </summary>
        public static Color GetCategoryColor(this TraitCategory category)
        {
            return category switch
            {
                TraitCategory.Physical => Color.cyan,
                TraitCategory.Behavioral => Color.yellow,
                TraitCategory.Combat => Color.red,
                TraitCategory.Mental => Color.blue,
                TraitCategory.Environmental => new Color(0.5f, 0.8f, 0.3f), // Olive
                TraitCategory.Social => new Color(0.8f, 0.4f, 0.8f), // Pink
                TraitCategory.Mutation => new Color(0.2f, 0.2f, 0.2f), // Dark gray
                TraitCategory.Hidden => new Color(0.5f, 0.5f, 0.5f, 0.5f), // Translucent gray
                TraitCategory.Sensory => new Color(0.9f, 0.8f, 0.2f), // Bright yellow
                TraitCategory.Metabolic => new Color(0.3f, 0.9f, 0.6f), // Light green
                TraitCategory.Utility => new Color(0.6f, 0.6f, 0.9f), // Light blue
                TraitCategory.Special => Color.magenta,
                TraitCategory.Cosmetic => new Color(1f, 0.8f, 0.9f), // Light pink
                _ => Color.gray
            };
        }

        /// <summary>
        /// Gets the seasonal multiplier for ecosystem calculations
        /// </summary>
        public static float GetSeasonalMultiplier(this Season season)
        {
            return season switch
            {
                Season.Spring => 1.2f, // Growth season
                Season.Summer => 1.1f, // Abundance
                Season.Autumn => 0.9f, // Preparation
                Season.Winter => 0.7f, // Harsh conditions
                _ => 1f
            };
        }

        /// <summary>
        /// Gets the display name for a season
        /// </summary>
        public static string GetDisplayName(this Season season)
        {
            return season.ToString();
        }

        /// <summary>
        /// Gets the display name for an adaptation type
        /// </summary>
        public static string GetDisplayName(this AdaptationType adaptationType)
        {
            return adaptationType.ToString();
        }

        /// <summary>
        /// Gets the display name for a food type
        /// </summary>
        public static string GetDisplayName(this FoodType foodType)
        {
            return foodType.ToString();
        }

        /// <summary>
        /// Gets the display name for a predator type
        /// </summary>
        public static string GetDisplayName(this PredatorType predatorType)
        {
            return predatorType.ToString();
        }

        /// <summary>
        /// Gets the food category for dietary analysis
        /// </summary>
        public static FoodCategory GetCategory(this FoodType foodType)
        {
            return (FoodCategory)((byte)foodType / 32);
        }

        /// <summary>
        /// Gets the predator category for threat analysis
        /// </summary>
        public static PredatorCategory GetCategory(this PredatorType predatorType)
        {
            return (PredatorCategory)((byte)predatorType / 16);
        }

        /// <summary>
        /// Gets the nutritional value modifier for this food type
        /// </summary>
        public static float GetNutritionalValue(this FoodType foodType)
        {
            return foodType.GetCategory() switch
            {
                FoodCategory.Plant => 0.7f,
                FoodCategory.Animal => 1.2f,
                FoodCategory.Mineral => 0.3f,
                FoodCategory.Special => 1.5f,
                _ => 1f
            };
        }

        /// <summary>
        /// Gets the threat level for this predator type
        /// </summary>
        public static float GetThreatLevel(this PredatorType predatorType)
        {
            return predatorType.GetCategory() switch
            {
                PredatorCategory.Small => 0.2f,
                PredatorCategory.Medium => 0.5f,
                PredatorCategory.Large => 0.8f,
                PredatorCategory.Apex => 1.0f,
                PredatorCategory.Environmental => 0.6f,
                PredatorCategory.Magical => 0.9f,
                PredatorCategory.Unknown => 0.4f,
                _ => 0.5f
            };
        }
    }

    /// <summary>
    /// Equipment slot types for creature equipment system
    /// Replaces Laboratory.Core.Equipment.Types.EquipmentSlot
    /// </summary>
    public enum EquipmentSlot : byte
    {
        Head = 0,
        Body = 1,
        Weapon = 2,
        Accessory = 3,
        Special = 4
    }

    /// <summary>
    /// Equipment rarity levels for item classification
    /// Replaces Laboratory.Core.Equipment.Types.EquipmentRarity
    /// </summary>
    public enum EquipmentRarity : byte
    {
        Common = 0,
        Uncommon = 1,
        Rare = 2,
        Epic = 3,
        Legendary = 4,
        Mythic = 5
    }
}