using System;
using System.Collections.Generic;
using UnityEngine;
using Laboratory.Core.ECS.Components;

namespace Laboratory.Core.Discovery.Data
{
    /// <summary>
    /// Journal configuration
    /// </summary>
    [Serializable]
    public class JournalConfig
    {
        [Header("Documentation Settings")]
        public bool autoDocumentBreeding = true;
        public bool autoDocumentDiscoveries = true;
        public bool enablePlayerNotes = true;

        [Header("Sharing Settings")]
        public bool enableCommunitySharing = true;
        public bool enableFriendSharing = true;
    }

    /// <summary>
    /// Player journal data
    /// </summary>
    [Serializable]
    public class PlayerJournal
    {
        public string PlayerId;
        public string JournalName;
        public DateTime CreatedDate;
        public DateTime LastEntryDate;
        public int TotalEntries;
        public JournalSettings Settings;
    }

    /// <summary>
    /// Journal settings
    /// </summary>
    [Serializable]
    public class JournalSettings
    {
        public bool AutoDocumentBreeding = true;
        public bool AutoDocumentDiscoveries = true;
        public bool AllowPlayerNotes = true;
        public bool ShareWithFriends = false;
        public bool ShareWithCommunity = false;
    }

    /// <summary>
    /// Journal entry
    /// </summary>
    [Serializable]
    public class JournalEntry
    {
        public string EntryId;
        public string PlayerId;
        public JournalEntryType EntryType;
        public string Title;
        [TextArea(5, 10)]
        public string Content;
        public DateTime CreatedDate;
        public List<string> Tags = new();
        public object AssociatedData;
    }

    /// <summary>
    /// Achievement data
    /// </summary>
    [Serializable]
    public class Achievement
    {
        public string AchievementId;
        public string Title;
        public string Description;
        public AchievementType Type;
        public int RequiredValue;
        public DateTime UnlockedDate;
        public TownResources Rewards;
    }

    /// <summary>
    /// Research project
    /// </summary>
    [Serializable]
    public class ResearchProject
    {
        public string ProjectId;
        public string Title;
        public string Description;
        public ResearchObjectiveType ObjectiveType;
        public ResearchStatus Status;
        public DateTime StartDate;
        public DateTime EndDate;
        public DateTime CompletionDate;
        public int CurrentProgress;
        public int RequiredBreedings;
        public int RequiredSamples;
        public int RequiredTests;
        public List<string> Findings = new();
    }

    /// <summary>
    /// Genetic discovery
    /// </summary>
    [Serializable]
    public class GeneticDiscovery
    {
        public string DiscoveryId;
        public string DiscoveryName;
        public string Description;
        public DiscoveryType DiscoveryType;
        public DiscoverySignificance Significance;
        public DateTime DiscoveryDate;
        public string RelatedJournalEntry;
    }

    /// <summary>
    /// Breeding analysis results
    /// </summary>
    [Serializable]
    public class BreedingAnalysis
    {
        public Dictionary<string, InheritanceType> InheritancePatterns = new();
        public Dictionary<string, TraitComparison> TraitComparisons = new();
        public List<string> NotableObservations = new();
        public float GeneticNovelty;
    }

    /// <summary>
    /// Trait comparison data
    /// </summary>
    [Serializable]
    public struct TraitComparison
    {
        public float Parent1Value;
        public float Parent2Value;
        public float OffspringValue;
        public InheritanceType InheritanceType;
    }

    /// <summary>
    /// Breeding success tracking
    /// </summary>
    [Serializable]
    public class BreedingSuccess
    {
        public string SuccessId;
        public string Parent1Species;
        public string Parent2Species;
        public float OffspringQualities;
        public float GeneticNovelty;
        public DateTime SuccessDate;
    }

    /// <summary>
    /// Discovery statistics
    /// </summary>
    [Serializable]
    public class DiscoveryStatistics
    {
        public int TotalDiscoveries;
        public int TotalJournalEntries;
        public int CompletedResearchProjects;
        public DateTime LastDiscoveryDate;
        public DateTime LastJournalEntry;
    }

    /// <summary>
    /// Community discovery for sharing
    /// </summary>
    [Serializable]
    public class CommunityDiscovery
    {
        public string DiscoveryId;
        public string DiscovererId;
        public GeneticDiscovery Discovery;
        public DateTime SharedDate;
        public int CommunityRating;
        public List<string> Comments = new();
    }

    /// <summary>
    /// Breeding data for journal entries
    /// </summary>
    [Serializable]
    public class BreedingData
    {
        public Monster Parent1;
        public Monster Parent2;
        public Monster Offspring;
    }
}