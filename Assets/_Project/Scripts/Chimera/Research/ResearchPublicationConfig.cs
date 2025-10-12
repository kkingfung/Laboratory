using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Laboratory.Chimera.Research
{
    /// <summary>
    /// Configuration ScriptableObject for the Research Publication System.
    /// Allows designers to tune peer review requirements, reputation systems, and publication standards.
    /// </summary>
    [CreateAssetMenu(fileName = "ResearchPublicationConfig", menuName = "Chimera/Research/Publication Config")]
    public class ResearchPublicationConfig : ScriptableObject
    {
        [Header("Paper Requirements")]
        [Tooltip("Minimum title length for paper submission")]
        [Range(5, 50)]
        public int minTitleLength = 10;

        [Tooltip("Minimum abstract length for paper submission")]
        [Range(50, 500)]
        public int minAbstractLength = 100;

        [Tooltip("Minimum number of evidence samples required")]
        [Range(1, 20)]
        public int minEvidenceSamples = 5;

        [Tooltip("Minimum average data quality for submission")]
        [Range(0.1f, 1f)]
        public float minDataQuality = 0.6f;

        [Tooltip("Plagiarism similarity threshold")]
        [Range(0.3f, 0.9f)]
        public float plagiarismThreshold = 0.8f;

        [Header("Peer Review Settings")]
        [Tooltip("Number of reviewers assigned to each paper")]
        [Range(1, 10)]
        public int reviewersPerPaper = 3;

        [Tooltip("Days reviewers have to complete reviews")]
        [Range(1f, 30f)]
        public float reviewTimeoutDays = 7f;

        [Tooltip("Minimum reputation required to review papers")]
        [Range(10f, 500f)]
        public float minReputationToReview = 50f;

        [Tooltip("Percentage of accepting reviews needed for publication")]
        [Range(0.3f, 1f)]
        public float acceptanceThreshold = 0.6f;

        [Tooltip("Minimum average quality score for publication")]
        [Range(0.3f, 1f)]
        public float minQualityForPublication = 0.7f;

        [Header("Reputation System")]
        [Tooltip("Starting reputation for new researchers")]
        [Range(0f, 100f)]
        public float startingReputation = 10f;

        [Tooltip("Base reputation gain for publishing a paper")]
        [Range(5f, 100f)]
        public float basePublicationReputation = 20f;

        [Tooltip("Base reputation gain for completing a review")]
        [Range(1f, 20f)]
        public float baseReviewReputation = 5f;

        [Tooltip("Reputation multiplier for high-quality publications")]
        [Range(1f, 5f)]
        public float qualityReputationMultiplier = 2f;

        [Tooltip("Reputation bonus for timely reviews")]
        [Range(0f, 10f)]
        public float timelyReviewBonus = 2f;

        [Header("Publication Tiers")]
        public PublicationTierConfig[] publicationTiers = new PublicationTierConfig[]
        {
            new PublicationTierConfig
            {
                tierName = "Student Research",
                minReputation = 0f,
                maxReputation = 49f,
                reputationMultiplier = 0.8f,
                maxActiveReviews = 1,
                description = "Entry-level research contributions"
            },
            new PublicationTierConfig
            {
                tierName = "Junior Researcher",
                minReputation = 50f,
                maxReputation = 99f,
                reputationMultiplier = 1f,
                maxActiveReviews = 2,
                description = "Developing research expertise"
            },
            new PublicationTierConfig
            {
                tierName = "Research Associate",
                minReputation = 100f,
                maxReputation = 199f,
                reputationMultiplier = 1.2f,
                maxActiveReviews = 3,
                description = "Established research contributor"
            },
            new PublicationTierConfig
            {
                tierName = "Senior Researcher",
                minReputation = 200f,
                maxReputation = 499f,
                reputationMultiplier = 1.5f,
                maxActiveReviews = 4,
                description = "Expert-level research leadership"
            },
            new PublicationTierConfig
            {
                tierName = "Distinguished Fellow",
                minReputation = 500f,
                maxReputation = float.MaxValue,
                reputationMultiplier = 2f,
                maxActiveReviews = 5,
                description = "World-renowned research authority"
            }
        };

        [Header("Research Topics")]
        public ResearchTopicConfig[] topicConfigs = new ResearchTopicConfig[]
        {
            new ResearchTopicConfig
            {
                topic = ResearchTopic.GeneticCombinations,
                displayName = "Genetic Combinations",
                reputationMultiplier = 1.2f,
                difficultyLevel = 2,
                description = "Study of unique genetic trait combinations",
                requiredEvidence = new[] { EvidenceType.GeneticData, EvidenceType.BreedingOutcome }
            },
            new ResearchTopicConfig
            {
                topic = ResearchTopic.EcosystemImpact,
                displayName = "Ecosystem Impact",
                reputationMultiplier = 1.5f,
                difficultyLevel = 3,
                description = "Effects of breeding on wild populations",
                requiredEvidence = new[] { EvidenceType.PopulationData, EvidenceType.EnvironmentalMeasurement }
            },
            new ResearchTopicConfig
            {
                topic = ResearchTopic.ConservationBiology,
                displayName = "Conservation Biology",
                reputationMultiplier = 1.8f,
                difficultyLevel = 4,
                description = "Species preservation and recovery strategies",
                requiredEvidence = new[] { EvidenceType.PopulationData, EvidenceType.GeneticData }
            },
            new ResearchTopicConfig
            {
                topic = ResearchTopic.BehavioralGenetics,
                displayName = "Behavioral Genetics",
                reputationMultiplier = 1.3f,
                difficultyLevel = 3,
                description = "Genetic basis of creature behavior",
                requiredEvidence = new[] { EvidenceType.BehavioralObservation, EvidenceType.GeneticData }
            }
        };

        [Header("Grant System")]
        [Tooltip("Enable research grant system")]
        public bool enableGrants = true;

        [Tooltip("Minimum reputation to apply for grants")]
        [Range(50f, 500f)]
        public float minReputationForGrants = 100f;

        [Tooltip("Maximum number of active grants per researcher")]
        [Range(1, 5)]
        public int maxActiveGrantsPerResearcher = 2;

        [Tooltip("Base grant funding amount")]
        [Range(1000, 50000)]
        public int baseGrantFunding = 10000;

        [Tooltip("Grant duration in days")]
        [Range(30, 365)]
        public int grantDurationDays = 90;

        [Header("Collaboration Settings")]
        [Tooltip("Enable collaborative research")]
        public bool enableCollaboration = true;

        [Tooltip("Maximum authors per paper")]
        [Range(1, 10)]
        public int maxAuthorsPerPaper = 5;

        [Tooltip("Reputation bonus for collaboration")]
        [Range(0f, 10f)]
        public float collaborationBonus = 5f;

        [Tooltip("Cross-expertise collaboration bonus")]
        [Range(1f, 3f)]
        public float crossExpertiseBonus = 1.5f;

        [Header("Quality Metrics")]
        [Tooltip("Weight of evidence quality in overall paper score")]
        [Range(0f, 1f)]
        public float evidenceQualityWeight = 0.4f;

        [Tooltip("Weight of peer review scores in overall paper score")]
        [Range(0f, 1f)]
        public float peerReviewWeight = 0.6f;

        [Tooltip("Minimum citations for high-impact classification")]
        [Range(1, 50)]
        public int highImpactCitationThreshold = 10;

        [Header("Publication Archive")]
        [Tooltip("Enable public research archive")]
        public bool enablePublicArchive = true;

        [Tooltip("Maximum search results to display")]
        [Range(10, 100)]
        public int maxSearchResults = 25;

        [Tooltip("Enable paper citation tracking")]
        public bool enableCitationTracking = true;

        [Header("Performance Settings")]
        [Tooltip("Maximum papers processed per frame")]
        [Range(1, 20)]
        public int maxPapersPerFrame = 5;

        [Tooltip("Review processing frequency (seconds)")]
        [Range(60f, 3600f)]
        public float reviewProcessingInterval = 3600f;

        [Tooltip("Grant processing frequency (seconds)")]
        [Range(3600f, 86400f)]
        public float grantProcessingInterval = 86400f;

        /// <summary>
        /// Gets the publication tier for a given reputation level
        /// </summary>
        public PublicationTierConfig GetPublicationTier(float reputation)
        {
            foreach (var tier in publicationTiers)
            {
                if (reputation >= tier.minReputation && reputation <= tier.maxReputation)
                    return tier;
            }

            return publicationTiers[0]; // Default to lowest tier
        }

        /// <summary>
        /// Gets configuration for a specific research topic
        /// </summary>
        public ResearchTopicConfig GetTopicConfig(ResearchTopic topic)
        {
            foreach (var config in topicConfigs)
            {
                if (config.topic == topic)
                    return config;
            }

            // Return default if not found
            return new ResearchTopicConfig
            {
                topic = topic,
                displayName = topic.ToString(),
                reputationMultiplier = 1f,
                difficultyLevel = 1,
                description = "Research topic",
                requiredEvidence = new[] { EvidenceType.GeneticData }
            };
        }

        /// <summary>
        /// Calculates reputation gain for a publication
        /// </summary>
        public float CalculatePublicationReputation(ResearchTopic topic, float qualityScore, int authorCount, bool isCollaboration)
        {
            var topicConfig = GetTopicConfig(topic);
            float reputation = basePublicationReputation;

            // Apply topic multiplier
            reputation *= topicConfig.reputationMultiplier;

            // Apply quality multiplier
            if (qualityScore > 0.8f)
                reputation *= qualityReputationMultiplier;

            // Apply collaboration bonus
            if (isCollaboration)
                reputation += collaborationBonus;

            // Distribute among authors
            reputation /= authorCount;

            return reputation;
        }

        /// <summary>
        /// Calculates reputation gain for completing a review
        /// </summary>
        public float CalculateReviewReputation(float reviewQuality, bool isTimely, bool hasDetailedFeedback)
        {
            float reputation = baseReviewReputation;

            // Quality bonus
            reputation *= (0.5f + reviewQuality);

            // Timely bonus
            if (isTimely)
                reputation += timelyReviewBonus;

            // Detailed feedback bonus
            if (hasDetailedFeedback)
                reputation += 1f;

            return reputation;
        }

        /// <summary>
        /// Determines if a researcher can review papers
        /// </summary>
        public bool CanReviewPapers(float reputation, int activeReviews)
        {
            if (reputation < minReputationToReview)
                return false;

            var tier = GetPublicationTier(reputation);
            return activeReviews < tier.maxActiveReviews;
        }

        /// <summary>
        /// Validates paper submission requirements
        /// </summary>
        public ValidationResult ValidatePaperSubmission(ResearchPaper paper)
        {
            var result = new ValidationResult { isValid = true, issues = new List<string>() };

            // Check title length
            if (string.IsNullOrEmpty(paper.title) || paper.title.Length < minTitleLength)
            {
                result.isValid = false;
                result.issues.Add($"Title must be at least {minTitleLength} characters");
            }

            // Check abstract length
            if (string.IsNullOrEmpty(paper.abstractText) || paper.abstractText.Length < minAbstractLength)
            {
                result.isValid = false;
                result.issues.Add($"Abstract must be at least {minAbstractLength} characters");
            }

            // Check evidence count
            if (paper.results.Count < minEvidenceSamples)
            {
                result.isValid = false;
                result.issues.Add($"Minimum {minEvidenceSamples} evidence samples required");
            }

            // Check data quality
            if (paper.results.Count > 0)
            {
                var avgQuality = paper.results.Average(r => r.quality);
                if (avgQuality < minDataQuality)
                {
                    result.isValid = false;
                    result.issues.Add($"Data quality too low: {avgQuality:F2} < {minDataQuality:F2}");
                }
            }

            // Check topic-specific requirements
            var topicConfig = GetTopicConfig(paper.topic);
            var presentEvidenceTypes = paper.results.Select(r => r.type).Distinct().ToList();
            var missingTypes = topicConfig.requiredEvidence.Where(req => !presentEvidenceTypes.Contains(req)).ToList();

            if (missingTypes.Any())
            {
                result.isValid = false;
                result.issues.Add($"Missing required evidence types: {string.Join(", ", missingTypes)}");
            }

            return result;
        }

        void OnValidate()
        {
            // Ensure reasonable values
            minTitleLength = Mathf.Clamp(minTitleLength, 5, 50);
            minAbstractLength = Mathf.Clamp(minAbstractLength, 50, 500);
            minEvidenceSamples = Mathf.Clamp(minEvidenceSamples, 1, 20);
            reviewersPerPaper = Mathf.Clamp(reviewersPerPaper, 1, 10);
            reviewTimeoutDays = Mathf.Clamp(reviewTimeoutDays, 1f, 30f);

            // Validate tier progression
            for (int i = 1; i < publicationTiers.Length; i++)
            {
                if (publicationTiers[i].minReputation <= publicationTiers[i - 1].maxReputation)
                {
                    publicationTiers[i].minReputation = publicationTiers[i - 1].maxReputation + 1f;
                }
            }

            // Ensure topic configs have required evidence
            for (int i = 0; i < topicConfigs.Length; i++)
            {
                if (topicConfigs[i].requiredEvidence == null || topicConfigs[i].requiredEvidence.Length == 0)
                {
                    topicConfigs[i].requiredEvidence = new[] { EvidenceType.GeneticData };
                }
            }
        }
    }

    #region Configuration Data Structures

    /// <summary>
    /// Configuration for publication reputation tiers
    /// </summary>
    [Serializable]
    public struct PublicationTierConfig
    {
        [Tooltip("Display name for this tier")]
        public string tierName;

        [Tooltip("Minimum reputation for this tier")]
        public float minReputation;

        [Tooltip("Maximum reputation for this tier")]
        public float maxReputation;

        [Tooltip("Reputation gain multiplier for this tier")]
        [Range(0.5f, 3f)]
        public float reputationMultiplier;

        [Tooltip("Maximum concurrent reviews for this tier")]
        [Range(1, 10)]
        public int maxActiveReviews;

        [Tooltip("Description of this tier")]
        [TextArea(1, 3)]
        public string description;
    }

    /// <summary>
    /// Configuration for research topics
    /// </summary>
    [Serializable]
    public struct ResearchTopicConfig
    {
        [Tooltip("The research topic this configures")]
        public ResearchTopic topic;

        [Tooltip("Display name for this topic")]
        public string displayName;

        [Tooltip("Reputation multiplier for this topic")]
        [Range(0.5f, 3f)]
        public float reputationMultiplier;

        [Tooltip("Difficulty level (affects grant requirements)")]
        [Range(1, 5)]
        public int difficultyLevel;

        [Tooltip("Description of this research area")]
        [TextArea(2, 4)]
        public string description;

        [Tooltip("Types of evidence required for this topic")]
        public EvidenceType[] requiredEvidence;

        [Tooltip("Enable special recognition for this topic")]
        public bool enableSpecialRecognition;

        [Tooltip("Color associated with this topic")]
        public Color topicColor;
    }

    /// <summary>
    /// Result of paper validation
    /// </summary>
    [Serializable]
    public struct ValidationResult
    {
        public bool isValid;
        public List<string> issues;
    }

    #endregion
}