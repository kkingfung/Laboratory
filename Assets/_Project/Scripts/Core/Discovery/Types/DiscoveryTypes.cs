namespace Laboratory.Core.Discovery.Types
{
    /// <summary>
    /// Enums for discovery system
    /// </summary>
    public enum JournalEntryType
    {
        BreedingResult,
        GeneticDiscovery,
        PlayerObservation,
        ResearchCompletion,
        Achievement
    }

    public enum AchievementType
    {
        JournalEntries,
        BreedingDocumentation,
        GeneticDiscoveries,
        ResearchProjects,
        ScientificMethod
    }

    public enum DiscoveryType
    {
        InheritancePattern,
        TraitExpression,
        MutationEvent,
        PerformanceCorrelation
    }

    public enum DiscoverySignificance
    {
        Minor,
        Notable,
        Significant,
        Major,
        Groundbreaking
    }

    public enum ResearchObjectiveType
    {
        BreedingAnalysis,
        TraitAnalysis,
        PerformanceStudy,
        PopulationStudy
    }

    public enum ResearchStatus
    {
        Available,
        InProgress,
        Completed,
        Cancelled
    }

    public enum InheritanceType
    {
        Blended,
        DominantFromParent1,
        DominantFromParent2,
        Novel
    }
}