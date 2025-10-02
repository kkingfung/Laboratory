using System;
using UnityEngine;

namespace Laboratory.Chimera.Consciousness
{
    /// <summary>
    /// Configuration ScriptableObject for the Creature Wisdom System.
    /// Allows designers to tune wisdom accumulation, sage progression, and mentorship mechanics.
    /// </summary>
    [CreateAssetMenu(fileName = "CreatureWisdomConfig", menuName = "Chimera/Consciousness/Wisdom Config")]
    public class CreatureWisdomConfig : ScriptableObject
    {
        [Header("Wisdom Accumulation")]
        [Tooltip("Base rate of wisdom gain from aging (per second)")]
        [Range(0.001f, 1f)]
        public float ageWisdomRate = 0.01f;

        [Tooltip("Multiplier for wisdom gained from interactions")]
        [Range(0.1f, 10f)]
        public float interactionWisdomMultiplier = 2f;

        [Tooltip("Wisdom bonus for overcoming challenges")]
        [Range(1f, 50f)]
        public float challengeWisdomBonus = 10f;

        [Tooltip("Wisdom bonus for teaching others")]
        [Range(1f, 20f)]
        public float teachingWisdomBonus = 5f;

        [Header("Age Thresholds")]
        [Tooltip("Age when creatures reach maturity (days)")]
        [Range(1f, 100f)]
        public float matureAgeThreshold = 30f;

        [Tooltip("Rate of wisdom gain during maturity")]
        [Range(0.001f, 0.1f)]
        public float maturityWisdomRate = 0.005f;

        [Tooltip("Age when legacy preparation begins (days)")]
        [Range(50f, 500f)]
        public float legacyPreparationAge = 200f;

        [Header("Sage Level Thresholds")]
        [Tooltip("Wisdom required to become a basic sage")]
        [Range(50f, 500f)]
        public float basicSageThreshold = 100f;

        [Tooltip("Wisdom required to become wise sage")]
        [Range(100f, 1000f)]
        public float wiseSageThreshold = 250f;

        [Tooltip("Wisdom required to become elder sage")]
        [Range(200f, 2000f)]
        public float elderSageThreshold = 500f;

        [Tooltip("Wisdom required to become ancient sage")]
        [Range(500f, 5000f)]
        public float ancientSageThreshold = 1000f;

        [Header("Age Requirements for Sage Levels")]
        [Tooltip("Minimum age to become sage (days)")]
        [Range(20f, 200f)]
        public float sageAgeRequirement = 50f;

        [Tooltip("Minimum age to become wise (days)")]
        [Range(40f, 300f)]
        public float wiseAgeRequirement = 100f;

        [Tooltip("Minimum age to become elder (days)")]
        [Range(80f, 500f)]
        public float elderAgeRequirement = 150f;

        [Tooltip("Minimum age to become ancient (days)")]
        [Range(150f, 1000f)]
        public float ancientAgeRequirement = 300f;

        [Header("Mentorship Settings")]
        [Tooltip("Maximum mentorships per sage level")]
        public int[] maxMentorshipsPerSage = new int[] { 0, 1, 2, 3, 5 }; // None, Sage, Wise, Elder, Ancient

        [Tooltip("Base duration for mentorships (seconds)")]
        [Range(3600f, 86400f)]
        public float baseMentorshipDuration = 14400f; // 4 hours

        [Tooltip("Maximum mentorship duration (seconds)")]
        [Range(7200f, 604800f)]
        public float maxMentorshipDuration = 86400f; // 24 hours

        [Tooltip("Rate of mentorship progress")]
        [Range(0.001f, 0.1f)]
        public float mentorshipProgressRate = 0.01f;

        [Tooltip("Wisdom bonus for completing mentorship")]
        [Range(5f, 100f)]
        public float mentorshipCompletionBonus = 25f;

        [Tooltip("Minimum learning capacity required for mentorship")]
        [Range(0.1f, 10f)]
        public float minLearningCapacityForMentorship = 1f;

        [Header("Wisdom Sharing")]
        [Tooltip("Interval between wisdom sharing attempts (seconds)")]
        [Range(60f, 3600f)]
        public float wisdomSharingInterval = 300f;

        [Tooltip("Efficiency of wisdom transfer")]
        [Range(0.1f, 1f)]
        public float wisdomTransferEfficiency = 0.7f;

        [Tooltip("Threshold wisdom for specialization")]
        [Range(50f, 500f)]
        public float specializationThreshold = 100f;

        [Header("Learning System")]
        [Tooltip("Base learning rate for students")]
        [Range(0.1f, 10f)]
        public float baseLearningRate = 1f;

        [Tooltip("Learning interval (seconds)")]
        [Range(30f, 600f)]
        public float learningInterval = 120f;

        [Tooltip("Base learning capacity")]
        [Range(1f, 20f)]
        public float baseLearningCapacity = 5f;

        [Tooltip("Wisdom multiplier for learning capacity")]
        [Range(0.01f, 0.5f)]
        public float wisdomLearningMultiplier = 0.1f;

        [Header("Legacy System")]
        [Tooltip("Interval for legacy creation checks (seconds)")]
        [Range(3600f, 86400f)]
        public float legacyCreationInterval = 21600f; // 6 hours

        [Tooltip("Maximum wisdom entries stored per creature")]
        [Range(50, 1000)]
        public int maxWisdomEntriesPerCreature = 200;

        [Header("Guidance System")]
        [Tooltip("Interval for guidance seeking (seconds)")]
        [Range(300f, 3600f)]
        public float guidanceSeekingInterval = 600f;

        [Tooltip("Wisdom threshold for providing guidance")]
        [Range(50f, 500f)]
        public float guidanceWisdomThreshold = 150f;

        [Tooltip("Base wisdom amount provided through guidance")]
        [Range(5f, 50f)]
        public float guidanceWisdomAmount = 15f;

        [Tooltip("Maximum wisdom level for guidance calculation")]
        [Range(500f, 5000f)]
        public float maxWisdomForGuidance = 1000f;

        [Header("Wisdom Milestones")]
        public WisdomMilestone[] wisdomMilestones = new WisdomMilestone[]
        {
            new WisdomMilestone
            {
                milestoneId = 1,
                name = "First Insights",
                wisdomThreshold = 25f,
                abilitiesGranted = new[] { "Basic Wisdom Sharing" },
                description = "Gained initial understanding of the world"
            },
            new WisdomMilestone
            {
                milestoneId = 2,
                name = "Growing Wisdom",
                wisdomThreshold = 75f,
                abilitiesGranted = new[] { "Enhanced Learning", "Pattern Recognition" },
                description = "Developed deeper understanding through experience"
            },
            new WisdomMilestone
            {
                milestoneId = 3,
                name = "Sage's Path",
                wisdomThreshold = 150f,
                abilitiesGranted = new[] { "Mentorship", "Wisdom Meditation" },
                description = "Embarked on the path of wisdom sharing"
            },
            new WisdomMilestone
            {
                milestoneId = 4,
                name = "Elder's Insight",
                wisdomThreshold = 300f,
                abilitiesGranted = new[] { "Community Guidance", "Legacy Preparation" },
                description = "Achieved elder status with profound insights"
            },
            new WisdomMilestone
            {
                milestoneId = 5,
                name = "Ancient Wisdom",
                wisdomThreshold = 600f,
                abilitiesGranted = new[] { "Transcendent Understanding", "Succession Planning" },
                description = "Reached the pinnacle of creature wisdom"
            }
        };

        [Header("Learning Milestones")]
        public LearningMilestone[] learningMilestones = new LearningMilestone[]
        {
            new LearningMilestone
            {
                milestoneId = 1,
                name = "Eager Student",
                wisdomRequired = 10f,
                capacityBonus = 2f,
                learningBonus = 0.1f,
                description = "Showed exceptional eagerness to learn"
            },
            new LearningMilestone
            {
                milestoneId = 2,
                name = "Dedicated Learner",
                wisdomRequired = 30f,
                capacityBonus = 5f,
                learningBonus = 0.2f,
                description = "Demonstrated consistent dedication to growth"
            },
            new LearningMilestone
            {
                milestoneId = 3,
                name = "Wisdom Seeker",
                wisdomRequired = 60f,
                capacityBonus = 8f,
                learningBonus = 0.3f,
                description = "Actively sought wisdom from multiple sources"
            }
        };

        [Header("Sage Abilities by Level")]
        public SageAbilityConfig[] sageAbilities = new SageAbilityConfig[]
        {
            new SageAbilityConfig
            {
                sageLevel = SageLevel.Sage,
                abilities = new[] { "Wisdom Sharing", "Basic Mentorship", "Experience Reflection" }
            },
            new SageAbilityConfig
            {
                sageLevel = SageLevel.Wise,
                abilities = new[] { "Advanced Mentorship", "Conflict Resolution", "Group Guidance" }
            },
            new SageAbilityConfig
            {
                sageLevel = SageLevel.Elder,
                abilities = new[] { "Community Leadership", "Legacy Teaching", "Succession Planning" }
            },
            new SageAbilityConfig
            {
                sageLevel = SageLevel.Ancient,
                abilities = new[] { "Transcendent Wisdom", "Generational Guidance", "Eternal Legacy" }
            }
        };

        [Header("Mentorship Type Configurations")]
        public MentorshipTypeConfig[] mentorshipTypes = new MentorshipTypeConfig[]
        {
            new MentorshipTypeConfig
            {
                type = MentorshipType.General,
                durationMultiplier = 1f,
                wisdomBonus = 0.1f,
                description = "Well-rounded guidance covering all aspects of growth"
            },
            new MentorshipTypeConfig
            {
                type = MentorshipType.Survival,
                durationMultiplier = 0.8f,
                wisdomBonus = 0.15f,
                description = "Focused training on survival skills and adaptation"
            },
            new MentorshipTypeConfig
            {
                type = MentorshipType.Social,
                durationMultiplier = 1.2f,
                wisdomBonus = 0.12f,
                description = "Development of social skills and community bonds"
            },
            new MentorshipTypeConfig
            {
                type = MentorshipType.Spiritual,
                durationMultiplier = 1.5f,
                wisdomBonus = 0.2f,
                description = "Deep spiritual growth and inner wisdom development"
            },
            new MentorshipTypeConfig
            {
                type = MentorshipType.Leadership,
                durationMultiplier = 1.3f,
                wisdomBonus = 0.18f,
                description = "Preparation for future leadership and mentorship roles"
            },
            new MentorshipTypeConfig
            {
                type = MentorshipType.Specialized,
                durationMultiplier = 1.1f,
                wisdomBonus = 0.25f,
                description = "Highly specialized training in specific wisdom domains"
            }
        };

        /// <summary>
        /// Gets abilities for a specific sage level
        /// </summary>
        public string[] GetSageAbilities(SageLevel level)
        {
            foreach (var config in sageAbilities)
            {
                if (config.sageLevel == level)
                    return config.abilities;
            }
            return new string[0];
        }

        /// <summary>
        /// Gets duration multiplier for a mentorship type
        /// </summary>
        public float GetMentorshipDurationMultiplier(MentorshipType type)
        {
            foreach (var config in mentorshipTypes)
            {
                if (config.type == type)
                    return config.durationMultiplier;
            }
            return 1f;
        }

        /// <summary>
        /// Gets wisdom bonus for a mentorship type
        /// </summary>
        public float GetMentorshipBonus(MentorshipType type)
        {
            foreach (var config in mentorshipTypes)
            {
                if (config.type == type)
                    return config.wisdomBonus;
            }
            return 0.1f;
        }

        /// <summary>
        /// Gets description for a mentorship type
        /// </summary>
        public string GetMentorshipDescription(MentorshipType type)
        {
            foreach (var config in mentorshipTypes)
            {
                if (config.type == type)
                    return config.description;
            }
            return "General mentorship guidance";
        }

        /// <summary>
        /// Calculates expected wisdom gain for a given time period
        /// </summary>
        public float CalculateExpectedWisdom(float timeInSeconds, float currentAge, int interactions, int challenges)
        {
            float ageWisdom = timeInSeconds * ageWisdomRate;
            float interactionWisdom = interactions * interactionWisdomMultiplier;
            float challengeWisdom = challenges * challengeWisdomBonus;

            // Apply maturity bonus if applicable
            if (currentAge > matureAgeThreshold)
            {
                float maturityBonus = (currentAge - matureAgeThreshold) * maturityWisdomRate * timeInSeconds;
                ageWisdom += maturityBonus;
            }

            return ageWisdom + interactionWisdom + challengeWisdom;
        }

        /// <summary>
        /// Determines if a creature can become a sage at given age and wisdom
        /// </summary>
        public bool CanBecomeSage(float age, float wisdom, SageLevel targetLevel)
        {
            switch (targetLevel)
            {
                case SageLevel.Sage:
                    return age >= sageAgeRequirement && wisdom >= basicSageThreshold;
                case SageLevel.Wise:
                    return age >= wiseAgeRequirement && wisdom >= wiseSageThreshold;
                case SageLevel.Elder:
                    return age >= elderAgeRequirement && wisdom >= elderSageThreshold;
                case SageLevel.Ancient:
                    return age >= ancientAgeRequirement && wisdom >= ancientSageThreshold;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Gets the maximum number of mentorships for a sage level
        /// </summary>
        public int GetMaxMentorships(SageLevel level)
        {
            int index = (int)level;
            if (index >= 0 && index < maxMentorshipsPerSage.Length)
                return maxMentorshipsPerSage[index];
            return 0;
        }

        /// <summary>
        /// Calculates learning efficiency based on mentor-student compatibility
        /// </summary>
        public float CalculateLearningEfficiency(float mentorWisdom, float studentWisdom, MentorshipType type)
        {
            float wisdomRatio = mentorWisdom / Mathf.Max(studentWisdom, 1f);
            float optimalRatio = 3f; // Ideal mentor to student wisdom ratio

            // Calculate efficiency based on how close to optimal ratio
            float efficiency = 1f - Mathf.Abs(wisdomRatio - optimalRatio) / optimalRatio;
            efficiency = Mathf.Clamp01(efficiency);

            // Apply mentorship type bonus
            efficiency += GetMentorshipBonus(type);

            return Mathf.Clamp01(efficiency);
        }

        void OnValidate()
        {
            // Ensure reasonable wisdom accumulation rates
            ageWisdomRate = Mathf.Clamp(ageWisdomRate, 0.001f, 1f);
            interactionWisdomMultiplier = Mathf.Clamp(interactionWisdomMultiplier, 0.1f, 50f);
            challengeWisdomBonus = Mathf.Clamp(challengeWisdomBonus, 1f, 200f);
            teachingWisdomBonus = Mathf.Clamp(teachingWisdomBonus, 1f, 100f);

            // Validate age thresholds
            matureAgeThreshold = Mathf.Clamp(matureAgeThreshold, 1f, 200f);
            legacyPreparationAge = Mathf.Clamp(legacyPreparationAge, matureAgeThreshold + 10f, 1000f);

            // Ensure sage thresholds are progressive
            basicSageThreshold = Mathf.Clamp(basicSageThreshold, 10f, 1000f);
            wiseSageThreshold = Mathf.Max(wiseSageThreshold, basicSageThreshold + 50f);
            elderSageThreshold = Mathf.Max(elderSageThreshold, wiseSageThreshold + 100f);
            ancientSageThreshold = Mathf.Max(ancientSageThreshold, elderSageThreshold + 200f);

            // Ensure age requirements are progressive
            sageAgeRequirement = Mathf.Clamp(sageAgeRequirement, matureAgeThreshold, 500f);
            wiseAgeRequirement = Mathf.Max(wiseAgeRequirement, sageAgeRequirement + 20f);
            elderAgeRequirement = Mathf.Max(elderAgeRequirement, wiseAgeRequirement + 30f);
            ancientAgeRequirement = Mathf.Max(ancientAgeRequirement, elderAgeRequirement + 50f);

            // Validate mentorship settings
            baseMentorshipDuration = Mathf.Clamp(baseMentorshipDuration, 1800f, 172800f);
            maxMentorshipDuration = Mathf.Max(maxMentorshipDuration, baseMentorshipDuration);
            mentorshipProgressRate = Mathf.Clamp(mentorshipProgressRate, 0.001f, 1f);
            mentorshipCompletionBonus = Mathf.Clamp(mentorshipCompletionBonus, 1f, 500f);

            // Validate learning settings
            baseLearningRate = Mathf.Clamp(baseLearningRate, 0.1f, 50f);
            learningInterval = Mathf.Clamp(learningInterval, 30f, 3600f);
            baseLearningCapacity = Mathf.Clamp(baseLearningCapacity, 1f, 100f);
            wisdomLearningMultiplier = Mathf.Clamp(wisdomLearningMultiplier, 0.01f, 2f);

            // Validate wisdom sharing settings
            wisdomSharingInterval = Mathf.Clamp(wisdomSharingInterval, 60f, 7200f);
            wisdomTransferEfficiency = Mathf.Clamp01(wisdomTransferEfficiency);
            specializationThreshold = Mathf.Clamp(specializationThreshold, 10f, 1000f);

            // Validate guidance settings
            guidanceSeekingInterval = Mathf.Clamp(guidanceSeekingInterval, 60f, 7200f);
            guidanceWisdomThreshold = Mathf.Clamp(guidanceWisdomThreshold, 10f, 1000f);
            guidanceWisdomAmount = Mathf.Clamp(guidanceWisdomAmount, 1f, 200f);
            maxWisdomForGuidance = Mathf.Clamp(maxWisdomForGuidance, 100f, 10000f);

            // Validate legacy settings
            legacyCreationInterval = Mathf.Clamp(legacyCreationInterval, 1800f, 172800f);
            maxWisdomEntriesPerCreature = Mathf.Clamp(maxWisdomEntriesPerCreature, 10, 5000);

            // Ensure maximum mentorships array has correct length
            if (maxMentorshipsPerSage.Length != 5)
            {
                maxMentorshipsPerSage = new int[] { 0, 1, 2, 3, 5 };
            }

            // Validate mentorship limits
            for (int i = 0; i < maxMentorshipsPerSage.Length; i++)
            {
                maxMentorshipsPerSage[i] = Mathf.Clamp(maxMentorshipsPerSage[i], 0, 10);
            }

            // Validate wisdom milestones
            for (int i = 0; i < wisdomMilestones.Length; i++)
            {
                wisdomMilestones[i].wisdomThreshold = Mathf.Clamp(wisdomMilestones[i].wisdomThreshold, 1f, 10000f);

                if (string.IsNullOrEmpty(wisdomMilestones[i].name))
                    wisdomMilestones[i].name = $"Milestone {i + 1}";

                if (wisdomMilestones[i].abilitiesGranted == null || wisdomMilestones[i].abilitiesGranted.Length == 0)
                    wisdomMilestones[i].abilitiesGranted = new[] { "Basic Ability" };
            }

            // Validate learning milestones
            for (int i = 0; i < learningMilestones.Length; i++)
            {
                learningMilestones[i].wisdomRequired = Mathf.Clamp(learningMilestones[i].wisdomRequired, 1f, 1000f);
                learningMilestones[i].capacityBonus = Mathf.Clamp(learningMilestones[i].capacityBonus, 0f, 50f);
                learningMilestones[i].learningBonus = Mathf.Clamp(learningMilestones[i].learningBonus, 0f, 2f);

                if (string.IsNullOrEmpty(learningMilestones[i].name))
                    learningMilestones[i].name = $"Learning Milestone {i + 1}";
            }

            // Validate mentorship type configurations
            for (int i = 0; i < mentorshipTypes.Length; i++)
            {
                mentorshipTypes[i].durationMultiplier = Mathf.Clamp(mentorshipTypes[i].durationMultiplier, 0.1f, 5f);
                mentorshipTypes[i].wisdomBonus = Mathf.Clamp(mentorshipTypes[i].wisdomBonus, 0f, 1f);

                if (string.IsNullOrEmpty(mentorshipTypes[i].description))
                    mentorshipTypes[i].description = "Mentorship guidance";
            }
        }
    }

    #region Configuration Data Structures

    /// <summary>
    /// Configuration for wisdom milestones
    /// </summary>
    [Serializable]
    public struct WisdomMilestone
    {
        [Tooltip("Unique identifier for this milestone")]
        public int milestoneId;

        [Tooltip("Display name for the milestone")]
        public string name;

        [Tooltip("Wisdom threshold required to achieve this milestone")]
        [Range(1f, 10000f)]
        public float wisdomThreshold;

        [Tooltip("Abilities granted when milestone is reached")]
        public string[] abilitiesGranted;

        [Tooltip("Description of the milestone achievement")]
        [TextArea(2, 4)]
        public string description;

        [Tooltip("Visual effect intensity for milestone achievement")]
        [Range(0f, 2f)]
        public float celebrationIntensity;

        [Tooltip("Whether this milestone grants special recognition")]
        public bool grantsSpecialRecognition;
    }

    /// <summary>
    /// Configuration for learning milestones
    /// </summary>
    [Serializable]
    public struct LearningMilestone
    {
        [Tooltip("Unique identifier for this milestone")]
        public int milestoneId;

        [Tooltip("Display name for the milestone")]
        public string name;

        [Tooltip("Wisdom required to achieve this milestone")]
        [Range(1f, 1000f)]
        public float wisdomRequired;

        [Tooltip("Learning capacity bonus granted")]
        [Range(0f, 50f)]
        public float capacityBonus;

        [Tooltip("Learning rate bonus granted")]
        [Range(0f, 2f)]
        public float learningBonus;

        [Tooltip("Description of the learning achievement")]
        [TextArea(2, 4)]
        public string description;

        [Tooltip("Whether this milestone qualifies for advanced mentorship")]
        public bool qualifiesForAdvancedMentorship;
    }

    /// <summary>
    /// Configuration for sage abilities by level
    /// </summary>
    [Serializable]
    public struct SageAbilityConfig
    {
        [Tooltip("Sage level this configuration applies to")]
        public SageLevel sageLevel;

        [Tooltip("Abilities granted at this sage level")]
        public string[] abilities;

        [Tooltip("Special privileges granted")]
        public string[] privileges;

        [Tooltip("Responsibility level")]
        [Range(1, 10)]
        public int responsibilityLevel;

        [Tooltip("Community influence level")]
        [Range(1, 10)]
        public int influenceLevel;
    }

    /// <summary>
    /// Configuration for mentorship types
    /// </summary>
    [Serializable]
    public struct MentorshipTypeConfig
    {
        [Tooltip("Type of mentorship")]
        public MentorshipType type;

        [Tooltip("Duration multiplier for this mentorship type")]
        [Range(0.1f, 5f)]
        public float durationMultiplier;

        [Tooltip("Wisdom bonus multiplier for this type")]
        [Range(0f, 1f)]
        public float wisdomBonus;

        [Tooltip("Description of this mentorship type")]
        [TextArea(2, 4)]
        public string description;

        [Tooltip("Prerequisites for this mentorship type")]
        public string[] prerequisites;

        [Tooltip("Outcomes expected from this mentorship")]
        public string[] expectedOutcomes;

        [Tooltip("Difficulty level of this mentorship")]
        [Range(1, 10)]
        public int difficultyLevel;
    }

    #endregion
}