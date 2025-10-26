using System;

namespace Laboratory.Core.Enums
{
    /// <summary>
    /// Core trait enumeration for high-performance dictionary indexing
    /// Replaces Dictionary<string, T> with Dictionary<TraitType, T>
    /// </summary>
    public enum TraitType : byte
    {
        Aggression = 0,
        Sociability = 1,
        Curiosity = 2,
        Caution = 3,
        Intelligence = 4,
        Metabolism = 5,
        Fertility = 6,
        Dominance = 7,
        Size = 8,
        Speed = 9,
        Stamina = 10,
        Camouflage = 11,
        HeatTolerance = 12,
        ColdTolerance = 13,
        WaterAffinity = 14,
        Adaptability = 15
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
    /// Biome types for ecosystem management
    /// Replaces Dictionary<string, T> with Dictionary<BiomeType, T>
    /// </summary>
    public enum BiomeType : byte
    {
        Forest = 0,
        Desert = 1,
        Ocean = 2,
        Mountain = 3,
        Swamp = 4,
        Arctic = 5,
        Grassland = 6,
        Volcano = 7,
        Cave = 8,
        Sky = 9,
        Underground = 10,
        Magical = 11,
        Urban = 12,
        Hybrid = 13,
        Corrupted = 14,
        Celestial = 15,
        Unknown = 16
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
        QuestProgress = 33
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
}