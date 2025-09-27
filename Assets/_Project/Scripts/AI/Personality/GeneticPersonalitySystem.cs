using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Laboratory.Chimera.Genetics;

namespace Laboratory.AI.Personality
{
    /// <summary>
    /// AI personality system that generates unique behavioral patterns based on genetic traits.
    /// Creates emergent personalities through genetic influence on decision-making algorithms.
    /// </summary>
    [System.Serializable]
    public class GeneticPersonalitySystem
    {
        [Header("Personality Configuration")]
        [SerializeField] private bool enableLearning = true;
        [SerializeField] private float memoryDecayRate = 0.01f;
        [SerializeField] private float personalityStability = 0.8f;
        [SerializeField] private int maxMemoryEntries = 50;

        [Header("Genetic Influence")]
        [SerializeField] private float geneticInfluenceStrength = 0.7f;
        [SerializeField] private float environmentalInfluence = 0.3f;
        [SerializeField] private bool enableMoodSystem = true;

        // Core personality traits derived from genetics
        private PersonalityProfile personalityProfile;
        private Dictionary<string, float> baseTraits = new Dictionary<string, float>();
        private Dictionary<string, float> learnedBehaviors = new Dictionary<string, float>();
        private List<PersonalityMemory> memories = new List<PersonalityMemory>();

        // Dynamic state
        private MoodState currentMood = new MoodState();
        private Dictionary<string, float> temporaryTraitModifiers = new Dictionary<string, float>();
        private float lastPersonalityUpdate;

        // Decision making
        private DecisionMakingEngine decisionEngine;
        private SocialInteractionEngine socialEngine;
        private EmotionalResponseEngine emotionalEngine;

        public PersonalityProfile Profile => personalityProfile;
        public MoodState CurrentMood => currentMood;
        public bool IsLearningEnabled => enableLearning;
        public float PersonalityStability => personalityStability;

        // Events
        public System.Action<PersonalityChange> OnPersonalityChanged;
        public System.Action<MoodChange> OnMoodChanged;
        public System.Action<string, float> OnTraitLearned;
        public System.Action<SocialInteraction> OnSocialInteraction;

        public GeneticPersonalitySystem()
        {
            InitializePersonalitySystem();
        }

        private void InitializePersonalitySystem()
        {
            personalityProfile = new PersonalityProfile();
            decisionEngine = new DecisionMakingEngine();
            socialEngine = new SocialInteractionEngine();
            emotionalEngine = new EmotionalResponseEngine();

            InitializeBaseMood();
            UnityEngine.Debug.Log("Genetic Personality System initialized");
        }

        /// <summary>
        /// Generates personality from genetic traits
        /// </summary>
        public void GeneratePersonalityFromGenetics(GeneticProfile genome)
        {
            UnityEngine.Debug.Log($"Generating personality for creature {genome.ProfileId}");

            // Clear previous data
            baseTraits.Clear();
            personalityProfile = new PersonalityProfile
            {
                creatureId = (uint)genome.ProfileId.GetHashCode(), // Convert string ID to uint
                generation = (uint)genome.Generation,
                speciesName = "Unknown" // Species info not available in GeneticProfile
            };

            // Extract personality-relevant genetic traits
            ExtractPersonalityTraits(genome);

            // Generate core personality dimensions
            GenerateCorePersonalityDimensions();

            // Initialize behavioral tendencies
            InitializeBehavioralTendencies();

            // Set up decision-making parameters
            ConfigureDecisionMaking();

            // Initialize social preferences
            InitializeSocialPreferences(genome);

            UnityEngine.Debug.Log($"Personality generated: {GetPersonalityDescription()}");
        }

        private void ExtractPersonalityTraits(GeneticProfile genome)
        {
            // Map genetic traits to personality traits
            var traitMappings = new Dictionary<string, string[]>
            {
                ["Aggression"] = new[] { "Dominance", "Territoriality", "Competitiveness" },
                ["Intelligence"] = new[] { "Curiosity", "Learning_Speed", "Problem_Solving" },
                ["Social"] = new[] { "Empathy", "Communication", "Pack_Bonding" },
                ["Fear"] = new[] { "Caution", "Risk_Aversion", "Stress_Response" },
                ["Energy"] = new[] { "Activity_Level", "Persistence", "Stamina" },
                ["Adaptability"] = new[] { "Flexibility", "Exploration", "Change_Tolerance" }
            };

            foreach (var kvp in traitMappings)
            {
                string geneticTrait = kvp.Key;
                string[] personalityTraits = kvp.Value;

                if (genome.TraitExpressions.TryGetValue(geneticTrait, out var geneValue))
                {
                    foreach (string personalityTrait in personalityTraits)
                    {
                        // Balance genetic influence with environmental factors
                        float geneticComponent = geneValue.Value * geneticInfluenceStrength;
                        float environmentalComponent = UnityEngine.Random.Range(0f, 1f) * environmentalInfluence;
                        float traitValue = Mathf.Clamp01(geneticComponent + environmentalComponent);
                        baseTraits[personalityTrait] = traitValue;
                    }
                }
            }

            // Add emergent traits based on combinations
            GenerateEmergentTraits(genome);
        }

        private void GenerateEmergentTraits(GeneticProfile genome)
        {
            // Emergent traits from genetic combinations
            if (genome.TraitExpressions.TryGetValue("Intelligence", out var intel) &&
                genome.TraitExpressions.TryGetValue("Curiosity", out var curiosity))
            {
                float creativity = (intel.Value + curiosity.Value) * 0.5f;
                float geneticCreativity = creativity * geneticInfluenceStrength;
                float environmentalCreativity = UnityEngine.Random.Range(0f, 0.5f) * environmentalInfluence;
                baseTraits["Creativity"] = Mathf.Clamp01(geneticCreativity + environmentalCreativity);
            }

            if (genome.TraitExpressions.TryGetValue("Aggression", out var aggr) &&
                genome.TraitExpressions.TryGetValue("Fear", out var fear))
            {
                float boldness = Mathf.Clamp01(aggr.Value - fear.Value + 0.5f);
                float geneticBoldness = boldness * geneticInfluenceStrength;
                float environmentalBoldness = UnityEngine.Random.Range(0f, 0.3f) * environmentalInfluence;
                baseTraits["Boldness"] = Mathf.Clamp01(geneticBoldness + environmentalBoldness);
            }

            if (genome.TraitExpressions.TryGetValue("Social", out var social) &&
                genome.TraitExpressions.TryGetValue("Intelligence", out var intel2))
            {
                float leadership = (social.Value + intel2.Value) * 0.4f;
                float geneticLeadership = leadership * geneticInfluenceStrength;
                float environmentalLeadership = UnityEngine.Random.Range(0f, 0.4f) * environmentalInfluence;
                baseTraits["Leadership"] = Mathf.Clamp01(geneticLeadership + environmentalLeadership);
            }
        }

        private void GenerateCorePersonalityDimensions()
        {
            // The Big Five personality model adapted for creatures
            personalityProfile.openness = CalculatePersonalityDimension("Curiosity", "Creativity", "Exploration");
            personalityProfile.conscientiousness = CalculatePersonalityDimension("Persistence", "Caution", "Learning_Speed");
            personalityProfile.extraversion = CalculatePersonalityDimension("Social", "Communication", "Leadership");
            personalityProfile.agreeableness = CalculatePersonalityDimension("Empathy", "Pack_Bonding", "Cooperation");
            personalityProfile.neuroticism = CalculatePersonalityDimension("Stress_Response", "Fear", "Anxiety");

            // Creature-specific dimensions
            personalityProfile.dominance = GetTraitValue("Dominance");
            personalityProfile.playfulness = CalculatePersonalityDimension("Curiosity", "Energy", "Creativity") * 0.8f;
            personalityProfile.territoriality = GetTraitValue("Territoriality");
            personalityProfile.parentalInstinct = CalculatePersonalityDimension("Empathy", "Protectiveness", "Nurturing");
        }

        private float CalculatePersonalityDimension(params string[] traitNames)
        {
            float sum = 0f;
            int count = 0;

            foreach (string traitName in traitNames)
            {
                if (baseTraits.TryGetValue(traitName, out float value))
                {
                    sum += value;
                    count++;
                }
            }

            return count > 0 ? sum / count : 0.5f; // Default to neutral if no traits found
        }

        private float GetTraitValue(string traitName)
        {
            return baseTraits.GetValueOrDefault(traitName, 0.5f);
        }

        private void InitializeBehavioralTendencies()
        {
            personalityProfile.behaviorTendencies = new Dictionary<string, float>
            {
                // Movement patterns
                ["Exploration_Tendency"] = personalityProfile.openness * 0.8f + GetTraitValue("Curiosity") * 0.2f,
                ["Territorial_Patrol"] = personalityProfile.territoriality,
                ["Social_Seeking"] = personalityProfile.extraversion,
                ["Hiding_Tendency"] = personalityProfile.neuroticism * 0.6f + GetTraitValue("Caution") * 0.4f,

                // Combat behaviors
                ["Aggression_Threshold"] = 1f - personalityProfile.agreeableness,
                ["Flight_Response"] = personalityProfile.neuroticism * 0.7f + GetTraitValue("Fear") * 0.3f,
                ["Pack_Defense"] = GetTraitValue("Pack_Bonding") * 0.8f + personalityProfile.agreeableness * 0.2f,

                // Social behaviors
                ["Cooperation_Willingness"] = personalityProfile.agreeableness,
                ["Leadership_Desire"] = GetTraitValue("Leadership"),
                ["Submission_Tendency"] = 1f - personalityProfile.dominance,

                // Learning behaviors
                ["Routine_Preference"] = personalityProfile.conscientiousness,
                ["Innovation_Seeking"] = personalityProfile.openness * 0.6f + GetTraitValue("Creativity") * 0.4f,
                ["Imitation_Learning"] = personalityProfile.extraversion * 0.5f + GetTraitValue("Learning_Speed") * 0.5f
            };
        }

        private void ConfigureDecisionMaking()
        {
            decisionEngine.Configure(new DecisionMakingConfig
            {
                impulsiveness = 1f - personalityProfile.conscientiousness,
                riskTolerance = GetTraitValue("Boldness"),
                socialInfluence = personalityProfile.extraversion,
                emotionalWeight = personalityProfile.neuroticism,
                logicalWeight = GetTraitValue("Problem_Solving"),
                memoryInfluence = GetTraitValue("Learning_Speed")
            });
        }

        private void InitializeSocialPreferences(GeneticProfile genome)
        {
            personalityProfile.socialPreferences = new SocialPreferences
            {
                preferredGroupSize = CalculatePreferredGroupSize(),
                leadershipDesire = GetTraitValue("Leadership"),
                submissionTendency = 1f - personalityProfile.dominance,
                trustBuilding = personalityProfile.agreeableness,
                conflictAvoidance = personalityProfile.agreeableness * 0.7f + GetTraitValue("Caution") * 0.3f,
                empathyLevel = GetTraitValue("Empathy"),
                communicationStyle = CalculateCommunicationStyle()
            };
        }

        private int CalculatePreferredGroupSize()
        {
            float socialNeed = personalityProfile.extraversion;
            float territorialNeed = personalityProfile.territoriality;

            // Highly social but not territorial = large groups
            // Highly territorial but social = medium groups
            // Low social = small groups or solitary

            if (socialNeed > 0.7f && territorialNeed < 0.4f)
                return UnityEngine.Random.Range(8, 15); // Large pack
            else if (socialNeed > 0.4f && territorialNeed < 0.7f)
                return UnityEngine.Random.Range(3, 8); // Medium group
            else if (socialNeed > 0.2f)
                return UnityEngine.Random.Range(2, 4); // Pair/small family
            else
                return 1; // Solitary
        }

        private CommunicationStyle CalculateCommunicationStyle()
        {
            float dominance = personalityProfile.dominance;
            float social = personalityProfile.extraversion;
            float aggressive = GetTraitValue("Aggression");

            if (dominance > 0.7f && aggressive > 0.6f)
                return CommunicationStyle.Dominant;
            else if (social > 0.7f && personalityProfile.agreeableness > 0.5f)
                return CommunicationStyle.Friendly;
            else if (personalityProfile.neuroticism > 0.6f)
                return CommunicationStyle.Cautious;
            else if (personalityProfile.openness > 0.6f)
                return CommunicationStyle.Playful;
            else
                return CommunicationStyle.Neutral;
        }

        private void InitializeBaseMood()
        {
            currentMood = new MoodState
            {
                happiness = 0.5f,
                excitement = 0.3f,
                stress = 0.2f,
                confidence = personalityProfile.dominance,
                curiosity = personalityProfile.openness,
                socialNeed = personalityProfile.extraversion,
                timestamp = Time.time
            };
        }

        /// <summary>
        /// Updates personality based on experiences and learning
        /// </summary>
        public void UpdatePersonality(float deltaTime)
        {
            if (Time.time - lastPersonalityUpdate < 1f) return; // Update once per second

            if (enableLearning)
            {
                ProcessLearning(deltaTime);
                ProcessMemoryDecay(deltaTime);
            }

            if (enableMoodSystem)
            {
                UpdateMoodSystem(deltaTime);
            }

            ApplyTemporaryModifiers(deltaTime);
            CheckPersonalityChanges();

            lastPersonalityUpdate = Time.time;
        }

        private void ProcessLearning(float deltaTime)
        {
            // Process recent experiences and adjust learned behaviors
            var recentMemories = memories.Where(m => Time.time - m.timestamp < 300f).ToList(); // Last 5 minutes

            foreach (var memory in recentMemories)
            {
                ProcessMemoryForLearning(memory, deltaTime);
            }
        }

        private void ProcessMemoryForLearning(PersonalityMemory memory, float deltaTime)
        {
            float learningRate = GetTraitValue("Learning_Speed") * deltaTime * 0.01f;

            // Adjust behaviors based on memory outcomes
            switch (memory.type)
            {
                case MemoryType.SuccessfulSocialInteraction:
                    ModifyLearnedBehavior("Social_Confidence", memory.emotionalIntensity * learningRate);
                    ModifyLearnedBehavior("Cooperation_Willingness", memory.emotionalIntensity * learningRate * 0.5f);
                    break;

                case MemoryType.DangerousEncounter:
                    ModifyLearnedBehavior("Caution_Level", memory.emotionalIntensity * learningRate);
                    ModifyLearnedBehavior("Risk_Assessment", memory.emotionalIntensity * learningRate * 0.3f);
                    break;

                case MemoryType.SuccessfulExploration:
                    ModifyLearnedBehavior("Exploration_Confidence", memory.emotionalIntensity * learningRate);
                    ModifyLearnedBehavior("Curiosity_Satisfaction", memory.emotionalIntensity * learningRate * 0.4f);
                    break;

                case MemoryType.ConflictResolution:
                    if (memory.emotionalIntensity > 0) // Positive outcome
                    {
                        ModifyLearnedBehavior("Conflict_Management", memory.emotionalIntensity * learningRate);
                    }
                    else // Negative outcome
                    {
                        ModifyLearnedBehavior("Conflict_Avoidance", -memory.emotionalIntensity * learningRate);
                    }
                    break;
            }
        }

        private void ModifyLearnedBehavior(string behaviorName, float change)
        {
            if (!learnedBehaviors.ContainsKey(behaviorName))
            {
                learnedBehaviors[behaviorName] = 0f;
            }

            float oldValue = learnedBehaviors[behaviorName];
            learnedBehaviors[behaviorName] = Mathf.Clamp(oldValue + change, -1f, 1f);

            if (Mathf.Abs(change) > 0.01f) // Significant change
            {
                OnTraitLearned?.Invoke(behaviorName, learnedBehaviors[behaviorName]);
                UnityEngine.Debug.Log($"Learned behavior updated: {behaviorName} = {learnedBehaviors[behaviorName]:F3}");
            }
        }

        private void ProcessMemoryDecay(float deltaTime)
        {
            // Decay old memories and learned behaviors
            for (int i = memories.Count - 1; i >= 0; i--)
            {
                memories[i].intensity *= (1f - memoryDecayRate * deltaTime);

                if (memories[i].intensity < 0.1f || memories.Count > maxMemoryEntries)
                {
                    memories.RemoveAt(i);
                }
            }

            // Decay learned behaviors toward genetic baseline
            var keys = learnedBehaviors.Keys.ToList();
            foreach (string key in keys)
            {
                float decayAmount = memoryDecayRate * deltaTime * personalityStability;
                learnedBehaviors[key] *= (1f - decayAmount);
            }
        }

        private void UpdateMoodSystem(float deltaTime)
        {
            var oldMood = new MoodState
            {
                happiness = currentMood.happiness,
                excitement = currentMood.excitement,
                stress = currentMood.stress,
                confidence = currentMood.confidence,
                curiosity = currentMood.curiosity,
                socialNeed = currentMood.socialNeed
            };

            // Mood naturally returns to personality baseline
            float moodStabilization = deltaTime * 0.1f;

            currentMood.happiness = Mathf.Lerp(currentMood.happiness, personalityProfile.agreeableness, moodStabilization);
            currentMood.excitement = Mathf.Lerp(currentMood.excitement, personalityProfile.openness * 0.5f, moodStabilization);
            currentMood.stress = Mathf.Lerp(currentMood.stress, personalityProfile.neuroticism * 0.3f, moodStabilization);
            currentMood.confidence = Mathf.Lerp(currentMood.confidence, personalityProfile.dominance, moodStabilization);
            currentMood.curiosity = Mathf.Lerp(currentMood.curiosity, personalityProfile.openness, moodStabilization);
            currentMood.socialNeed = Mathf.Lerp(currentMood.socialNeed, personalityProfile.extraversion, moodStabilization);

            // Check for mood changes
            CheckMoodChanges(oldMood);

            currentMood.timestamp = Time.time;
        }

        private void CheckMoodChanges(MoodState oldMood)
        {
            float totalChange = 0f;
            totalChange += Mathf.Abs(currentMood.happiness - oldMood.happiness);
            totalChange += Mathf.Abs(currentMood.excitement - oldMood.excitement);
            totalChange += Mathf.Abs(currentMood.stress - oldMood.stress);

            if (totalChange > 0.1f) // Significant mood change
            {
                OnMoodChanged?.Invoke(new MoodChange
                {
                    oldMood = oldMood,
                    newMood = currentMood,
                    changeIntensity = totalChange
                });
            }
        }

        private void ApplyTemporaryModifiers(float deltaTime)
        {
            // Apply and decay temporary trait modifiers (from items, events, etc.)
            var keys = temporaryTraitModifiers.Keys.ToList();
            foreach (string key in keys)
            {
                temporaryTraitModifiers[key] *= (1f - deltaTime * 0.1f); // 10% decay per second

                if (Mathf.Abs(temporaryTraitModifiers[key]) < 0.01f)
                {
                    temporaryTraitModifiers.Remove(key);
                }
            }
        }

        private void CheckPersonalityChanges()
        {
            // Check if learned behaviors have significantly altered the personality
            // This could trigger personality change events for dramatic character development
        }

        /// <summary>
        /// Records a memory that can influence future behavior
        /// </summary>
        public void RecordMemory(MemoryType type, string description, float emotionalIntensity, Vector3? location = null)
        {
            var memory = new PersonalityMemory
            {
                type = type,
                description = description,
                emotionalIntensity = emotionalIntensity,
                intensity = Mathf.Abs(emotionalIntensity),
                location = location ?? Vector3.zero,
                timestamp = Time.time
            };

            memories.Add(memory);

            // Affect current mood immediately
            AffectMoodFromMemory(memory);

            UnityEngine.Debug.Log($"Memory recorded: {type} - {description} (Intensity: {emotionalIntensity:F2})");
        }

        private void AffectMoodFromMemory(PersonalityMemory memory)
        {
            float moodImpact = memory.emotionalIntensity * 0.1f; // Scale for mood impact

            switch (memory.type)
            {
                case MemoryType.SuccessfulSocialInteraction:
                    currentMood.happiness += moodImpact;
                    currentMood.socialNeed -= moodImpact * 0.5f; // Partially satisfied
                    break;

                case MemoryType.DangerousEncounter:
                    currentMood.stress += Mathf.Abs(moodImpact);
                    currentMood.confidence -= moodImpact * 0.3f;
                    break;

                case MemoryType.SuccessfulExploration:
                    currentMood.excitement += moodImpact;
                    currentMood.curiosity -= moodImpact * 0.3f; // Partially satisfied
                    break;

                case MemoryType.Achievement:
                    currentMood.confidence += moodImpact;
                    currentMood.happiness += moodImpact * 0.7f;
                    break;
            }

            // Clamp mood values
            currentMood.happiness = Mathf.Clamp01(currentMood.happiness);
            currentMood.excitement = Mathf.Clamp01(currentMood.excitement);
            currentMood.stress = Mathf.Clamp01(currentMood.stress);
            currentMood.confidence = Mathf.Clamp01(currentMood.confidence);
            currentMood.curiosity = Mathf.Clamp01(currentMood.curiosity);
            currentMood.socialNeed = Mathf.Clamp01(currentMood.socialNeed);
        }

        /// <summary>
        /// Gets the effective value of a trait including learned modifications
        /// </summary>
        public float GetEffectiveTrait(string traitName)
        {
            float baseValue = GetTraitValue(traitName);
            float learnedModifier = learnedBehaviors.GetValueOrDefault(traitName, 0f);
            float temporaryModifier = temporaryTraitModifiers.GetValueOrDefault(traitName, 0f);

            return Mathf.Clamp01(baseValue + learnedModifier + temporaryModifier);
        }

        /// <summary>
        /// Makes a personality-influenced decision
        /// </summary>
        public DecisionResult MakeDecision(DecisionContext context)
        {
            return decisionEngine.MakeDecision(context, this);
        }

        /// <summary>
        /// Processes a social interaction and records the outcome
        /// </summary>
        public SocialInteractionResult ProcessSocialInteraction(SocialInteractionContext context)
        {
            var result = socialEngine.ProcessInteraction(context, this);
            OnSocialInteraction?.Invoke(context.ToInteraction());

            // Record memory of the interaction
            RecordMemory(
                MemoryType.SuccessfulSocialInteraction,
                $"Interacted with {context.otherCreatureId}",
                result.satisfaction,
                context.location
            );

            return result;
        }

        /// <summary>
        /// Gets a human-readable personality description
        /// </summary>
        public string GetPersonalityDescription()
        {
            var traits = new List<string>();

            // Describe dominant traits
            if (personalityProfile.dominance > 0.7f) traits.Add("Dominant");
            else if (personalityProfile.dominance < 0.3f) traits.Add("Submissive");

            if (personalityProfile.extraversion > 0.7f) traits.Add("Social");
            else if (personalityProfile.extraversion < 0.3f) traits.Add("Solitary");

            if (personalityProfile.agreeableness > 0.7f) traits.Add("Cooperative");
            else if (personalityProfile.agreeableness < 0.3f) traits.Add("Aggressive");

            if (personalityProfile.openness > 0.7f) traits.Add("Curious");
            else if (personalityProfile.openness < 0.3f) traits.Add("Cautious");

            if (personalityProfile.neuroticism > 0.7f) traits.Add("Anxious");
            else if (personalityProfile.neuroticism < 0.3f) traits.Add("Calm");

            return traits.Count > 0 ? string.Join(", ", traits) : "Balanced";
        }

        /// <summary>
        /// Applies a temporary trait modifier (from items, spells, etc.)
        /// </summary>
        public void ApplyTemporaryTraitModifier(string traitName, float modifier)
        {
            temporaryTraitModifiers[traitName] = modifier;
            UnityEngine.Debug.Log($"Temporary trait modifier applied: {traitName} += {modifier:F2}");
        }
    }

    // Supporting data structures
    [System.Serializable]
    public class PersonalityProfile
    {
        public uint creatureId;
        public uint generation;
        public string speciesName;

        // Big Five personality traits
        public float openness;
        public float conscientiousness;
        public float extraversion;
        public float agreeableness;
        public float neuroticism;

        // Creature-specific traits
        public float dominance;
        public float playfulness;
        public float territoriality;
        public float parentalInstinct;

        // Behavioral tendencies
        public Dictionary<string, float> behaviorTendencies;
        public SocialPreferences socialPreferences;
    }

    [System.Serializable]
    public class SocialPreferences
    {
        public int preferredGroupSize;
        public float leadershipDesire;
        public float submissionTendency;
        public float trustBuilding;
        public float conflictAvoidance;
        public float empathyLevel;
        public CommunicationStyle communicationStyle;
    }

    [System.Serializable]
    public class MoodState
    {
        public float happiness;
        public float excitement;
        public float stress;
        public float confidence;
        public float curiosity;
        public float socialNeed;
        public float timestamp;
    }

    [System.Serializable]
    public class PersonalityMemory
    {
        public MemoryType type;
        public string description;
        public float emotionalIntensity;
        public float intensity;
        public Vector3 location;
        public float timestamp;
    }

    // Decision making systems
    public class DecisionMakingEngine
    {
        private DecisionMakingConfig config;

        public void Configure(DecisionMakingConfig newConfig)
        {
            config = newConfig;
        }

        public DecisionResult MakeDecision(DecisionContext context, GeneticPersonalitySystem personality)
        {
            // Implement decision making logic based on personality
            var result = new DecisionResult
            {
                chosenOption = 0, // Placeholder
                confidence = personality.CurrentMood.confidence,
                reasoningPath = "Personality-based decision"
            };

            return result;
        }
    }

    public class SocialInteractionEngine
    {
        public SocialInteractionResult ProcessInteraction(SocialInteractionContext context, GeneticPersonalitySystem personality)
        {
            // Process social interaction based on personality
            float satisfaction = personality.Profile.extraversion * 0.5f + personality.Profile.agreeableness * 0.3f;

            return new SocialInteractionResult
            {
                success = satisfaction > 0.3f,
                satisfaction = satisfaction,
                relationshipChange = satisfaction - 0.5f
            };
        }
    }

    public class EmotionalResponseEngine
    {
        public EmotionalResponse ProcessEmotion(EmotionalStimulus stimulus, GeneticPersonalitySystem personality)
        {
            // Process emotional response based on personality
            return new EmotionalResponse
            {
                intensity = stimulus.intensity * personality.Profile.neuroticism,
                duration = stimulus.intensity * (2f - personality.Profile.conscientiousness),
                type = stimulus.type
            };
        }
    }

    // Enums and data structures
    public enum MemoryType
    {
        SuccessfulSocialInteraction,
        DangerousEncounter,
        SuccessfulExploration,
        ConflictResolution,
        Achievement,
        TraumaticEvent,
        PositiveReinforcement,
        NegativeReinforcement
    }

    public enum CommunicationStyle
    {
        Dominant,
        Friendly,
        Cautious,
        Playful,
        Neutral,
        Aggressive,
        Submissive
    }

    [System.Serializable]
    public struct DecisionMakingConfig
    {
        public float impulsiveness;
        public float riskTolerance;
        public float socialInfluence;
        public float emotionalWeight;
        public float logicalWeight;
        public float memoryInfluence;
    }

    [System.Serializable]
    public struct PersonalityChange
    {
        public string traitName;
        public float oldValue;
        public float newValue;
        public string reason;
    }

    [System.Serializable]
    public struct MoodChange
    {
        public MoodState oldMood;
        public MoodState newMood;
        public float changeIntensity;
    }

    // Context and result structures for decisions and interactions
    public struct DecisionContext
    {
        public string decisionType;
        public float[] options;
        public float timeConstraint;
        public float stressLevel;
        public bool hasGroupInfluence;
    }

    public struct DecisionResult
    {
        public int chosenOption;
        public float confidence;
        public string reasoningPath;
    }

    public struct SocialInteractionContext
    {
        public uint otherCreatureId;
        public string interactionType;
        public Vector3 location;
        public float duration;
        public bool isGroupInteraction;

        public SocialInteraction ToInteraction()
        {
            return new SocialInteraction
            {
                otherCreatureId = otherCreatureId,
                type = interactionType,
                location = location,
                timestamp = Time.time
            };
        }
    }

    public struct SocialInteractionResult
    {
        public bool success;
        public float satisfaction;
        public float relationshipChange;
    }

    public struct SocialInteraction
    {
        public uint otherCreatureId;
        public string type;
        public Vector3 location;
        public float timestamp;
    }

    public struct EmotionalStimulus
    {
        public string type;
        public float intensity;
        public Vector3 source;
    }

    public struct EmotionalResponse
    {
        public string type;
        public float intensity;
        public float duration;
    }
}