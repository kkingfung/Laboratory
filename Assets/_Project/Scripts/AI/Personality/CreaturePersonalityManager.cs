using UnityEngine;
using System.Collections.Generic;
using Laboratory.Chimera.Genetics;
using Laboratory.Chimera.Genetics.Advanced;
using Laboratory.Chimera.AI;
using Laboratory.Core;
using Laboratory.Core.Events;
using Laboratory.Core.Diagnostics;

namespace Laboratory.AI.Personality
{
    /// <summary>
    /// Unity manager that integrates genetic personality system with creature AI.
    /// Handles personality-driven behavior modifications and social interactions.
    /// </summary>
    public class CreaturePersonalityManager : MonoBehaviour
    {
        [Header("Personality Configuration")]
        [SerializeField] private bool enablePersonalitySystem = true;
        [SerializeField] private float personalityUpdateInterval = 2f;
        [SerializeField] private float moodDecayRate = 0.1f;
        [SerializeField] private float socialInteractionRange = 10f;

        [Header("Behavior Modifiers")]
        [SerializeField, Range(0f, 2f)] private float aggressionMultiplier = 1f;
        [SerializeField, Range(0f, 2f)] private float explorationMultiplier = 1f;
        [SerializeField, Range(0f, 2f)] private float socialityMultiplier = 1f;

        [Header("Debug Settings")]
        [SerializeField] private bool showPersonalityGizmos = false;
        [SerializeField] private bool logPersonalityEvents = false;

        // Core system
        private GeneticPersonalitySystem personalitySystem;

        // Active creature personalities
        private Dictionary<uint, CreaturePersonalityProfile> activePersonalities = new Dictionary<uint, CreaturePersonalityProfile>();

        // Social interaction tracking
        private Dictionary<uint, List<SocialInteraction>> socialHistory = new Dictionary<uint, List<SocialInteraction>>();

        // Timers
        private float lastPersonalityUpdate;

        // Events
        public System.Action<uint, PersonalityTrait> OnPersonalityTraitChanged;
        public System.Action<uint, uint, SocialInteractionType> OnSocialInteraction;
        public System.Action<uint, MoodState> OnMoodChanged;

        // Singleton access
        private static CreaturePersonalityManager instance;
        public static CreaturePersonalityManager Instance => instance;

        public bool IsSystemEnabled => enablePersonalitySystem;
        public int ActivePersonalityCount => activePersonalities.Count;

        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
                InitializePersonalitySystem();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void InitializePersonalitySystem()
        {
            personalitySystem = new GeneticPersonalitySystem();
            DebugManager.LogInfo("Creature Personality Manager initialized");

            // Subscribe to genetic system events if available
            if (GeneticEvolutionManager.Instance != null)
            {
                GeneticEvolutionManager.Instance.OnEliteCreatureEmerged += HandleEliteCreature;
            }
        }

        private void Update()
        {
            if (!enablePersonalitySystem) return;

            // Update personalities periodically
            if (Time.time - lastPersonalityUpdate >= personalityUpdateInterval)
            {
                UpdateActivePersonalities();
                ProcessSocialInteractions();
                lastPersonalityUpdate = Time.time;
            }
        }

        /// <summary>
        /// Registers a creature with the personality system
        /// </summary>
        public CreaturePersonalityProfile RegisterCreature(uint creatureId, CreatureGenome genome)
        {
            if (!enablePersonalitySystem) return null;

            // Generate personality from genetics
            var geneticProfile = ConvertGenomeToGeneticProfile(genome);
            personalitySystem.GeneratePersonalityFromGenetics(geneticProfile);

            var profile = new CreaturePersonalityProfile
            {
                creatureId = creatureId,
                genome = genome,
                personalityTraits = ConvertProfileToPersonalityTrait(personalitySystem.Profile),
                currentMood = personalitySystem.CurrentMood,
                behavioralTendencies = personalitySystem.Profile.behaviorTendencies,
                registrationTime = Time.time
            };

            activePersonalities[creatureId] = profile;
            socialHistory[creatureId] = new List<SocialInteraction>();

            if (logPersonalityEvents)
            {
                DebugManager.LogInfo($"Creature {creatureId} registered with personality: {profile.GetPersonalitySummary()}");
            }

            return profile;
        }

        /// <summary>
        /// Unregisters a creature from the personality system
        /// </summary>
        public void UnregisterCreature(uint creatureId)
        {
            if (activePersonalities.ContainsKey(creatureId))
            {
                activePersonalities.Remove(creatureId);
                socialHistory.Remove(creatureId);

                if (logPersonalityEvents)
                {
                    DebugManager.LogInfo($"Creature {creatureId} unregistered from personality system");
                }
            }
        }

        /// <summary>
        /// Gets personality-modified behavior values for a creature
        /// </summary>
        public BehaviorModifiers GetBehaviorModifiers(uint creatureId)
        {
            if (!activePersonalities.TryGetValue(creatureId, out var profile))
                return BehaviorModifiers.Default;

            var modifiers = new BehaviorModifiers();
            var traits = profile.personalityTraits;

            // Apply personality-based modifications
            modifiers.aggressionBoost = (traits.aggressiveness - 0.5f) * 2f * aggressionMultiplier;
            modifiers.explorationBoost = (traits.openness - 0.5f) * 2f * explorationMultiplier;
            modifiers.socialityBoost = (traits.extroversion - 0.5f) * 2f * socialityMultiplier;

            // Apply mood modifications
            var mood = profile.currentMood;
            modifiers.speedMultiplier = 1f + (mood.excitement - 0.5f) * 0.5f;
            modifiers.decisionSpeed = 1f + (mood.confidence - 0.5f) * 0.3f;
            modifiers.riskTolerance = traits.openness * mood.confidence;

            // Apply stress effects
            if (mood.stress > 0.7f)
            {
                modifiers.aggressionBoost += 0.3f;
                modifiers.decisionSpeed *= 0.7f;
            }

            return modifiers;
        }

        /// <summary>
        /// Records a social interaction between two creatures
        /// </summary>
        public void RecordSocialInteraction(uint creatureA, uint creatureB, SocialInteractionType interactionType)
        {
            if (!enablePersonalitySystem) return;

            var interaction = new SocialInteraction
            {
                participantA = creatureA,
                participantB = creatureB,
                interactionType = interactionType,
                timestamp = Time.time,
                success = Random.Range(0f, 1f) > 0.3f // Basic success rate
            };

            // Record for both creatures
            if (socialHistory.ContainsKey(creatureA))
            {
                socialHistory[creatureA].Add(interaction);
                var contextA = CreateSocialInteractionContext(interaction, creatureB);
                personalitySystem.ProcessSocialInteraction(contextA);
            }

            if (socialHistory.ContainsKey(creatureB))
            {
                socialHistory[creatureB].Add(interaction);
                var contextB = CreateSocialInteractionContext(interaction, creatureA);
                personalitySystem.ProcessSocialInteraction(contextB);
            }

            // Update mood based on interaction
            UpdateMoodFromInteraction(creatureA, interactionType, interaction.success);
            UpdateMoodFromInteraction(creatureB, interactionType, interaction.success);

            OnSocialInteraction?.Invoke(creatureA, creatureB, interactionType);

            if (logPersonalityEvents)
            {
                DebugManager.LogInfo($"Social interaction: {creatureA} {interactionType} {creatureB} (Success: {interaction.success})");
            }
        }

        /// <summary>
        /// Applies environmental stimulus to a creature's personality
        /// </summary>
        public void ApplyEnvironmentalStimulus(uint creatureId, EnvironmentalStimulus stimulus)
        {
            if (!activePersonalities.TryGetValue(creatureId, out var profile)) return;

            personalitySystem.SetCurrentCreature(creatureId, profile.personalityTraits, profile.currentMood);
            personalitySystem.ApplyEnvironmentalStimulus(stimulus);

            // Update profile with new mood
            profile.currentMood = personalitySystem.CurrentMood;

            OnMoodChanged?.Invoke(creatureId, profile.currentMood);
        }

        /// <summary>
        /// Gets detailed personality analysis for a creature
        /// </summary>
        public PersonalityAnalysis AnalyzeCreaturePersonality(uint creatureId)
        {
            if (!activePersonalities.TryGetValue(creatureId, out var profile))
                return null;

            var analysis = new PersonalityAnalysis
            {
                creatureId = creatureId,
                personalityType = DeterminePersonalityType(profile.personalityTraits),
                dominantTraits = GetDominantTraits(profile.personalityTraits),
                currentMoodState = profile.currentMood,
                behavioralPredictions = PredictBehaviors(profile),
                socialCompatibility = CalculateSocialCompatibility(creatureId),
                stressFactors = IdentifyStressFactors(profile),
                recommendations = GenerateRecommendations(profile)
            };

            return analysis;
        }

        private void UpdateActivePersonalities()
        {
            foreach (var kvp in activePersonalities)
            {
                uint creatureId = kvp.Key;
                var profile = kvp.Value;

                // Update mood decay
                personalitySystem.SetCurrentCreature(creatureId, profile.personalityTraits, profile.currentMood);
                personalitySystem.UpdateMood(moodDecayRate);

                profile.currentMood = personalitySystem.CurrentMood;

                // Check for significant mood changes
                CheckForMoodChanges(creatureId, profile);
            }
        }

        private void ProcessSocialInteractions()
        {
            var creatures = new List<uint>(activePersonalities.Keys);

            for (int i = 0; i < creatures.Count; i++)
            {
                for (int j = i + 1; j < creatures.Count; j++)
                {
                    uint creatureA = creatures[i];
                    uint creatureB = creatures[j];

                    // Check if creatures are in interaction range (simplified)
                    if (AreCreaturesNearby(creatureA, creatureB))
                    {
                        ProcessPotentialInteraction(creatureA, creatureB);
                    }
                }
            }
        }

        private void ProcessPotentialInteraction(uint creatureA, uint creatureB)
        {
            var profileA = activePersonalities[creatureA];
            var profileB = activePersonalities[creatureB];

            // Calculate interaction probability based on personalities
            float interactionChance = CalculateInteractionProbability(profileA, profileB);

            if (Random.Range(0f, 1f) < interactionChance)
            {
                var interactionType = DetermineInteractionType(profileA, profileB);
                RecordSocialInteraction(creatureA, creatureB, interactionType);
            }
        }

        private float CalculateInteractionProbability(CreaturePersonalityProfile profileA, CreaturePersonalityProfile profileB)
        {
            float baseChance = 0.1f;

            // Higher chance for more extroverted creatures
            float extroversionFactor = (profileA.personalityTraits.extroversion + profileB.personalityTraits.extroversion) / 2f;

            // Lower chance if either creature is stressed
            float stressPenalty = Mathf.Max(profileA.currentMood.stress, profileB.currentMood.stress) * 0.5f;

            return Mathf.Clamp01(baseChance + extroversionFactor * 0.3f - stressPenalty);
        }

        private SocialInteractionType DetermineInteractionType(CreaturePersonalityProfile profileA, CreaturePersonalityProfile profileB)
        {
            float aggressionLevel = (profileA.personalityTraits.aggressiveness + profileB.personalityTraits.aggressiveness) / 2f;
            float friendliness = (profileA.personalityTraits.agreeableness + profileB.personalityTraits.agreeableness) / 2f;

            if (aggressionLevel > 0.7f && friendliness < 0.3f)
                return SocialInteractionType.Conflict;
            else if (friendliness > 0.6f)
                return SocialInteractionType.Cooperation;
            else if (Random.Range(0f, 1f) < 0.3f)
                return SocialInteractionType.Play;
            else
                return SocialInteractionType.Neutral;
        }

        private void UpdateMoodFromInteraction(uint creatureId, SocialInteractionType interactionType, bool success)
        {
            if (!activePersonalities.TryGetValue(creatureId, out var profile)) return;

            var mood = profile.currentMood;

            switch (interactionType)
            {
                case SocialInteractionType.Cooperation:
                    if (success)
                    {
                        mood.happiness += 0.1f;
                        mood.confidence += 0.05f;
                    }
                    break;

                case SocialInteractionType.Conflict:
                    mood.stress += success ? -0.05f : 0.15f;
                    mood.stress += 0.1f;
                    break;

                case SocialInteractionType.Play:
                    mood.happiness += 0.15f;
                    mood.excitement += 0.1f;
                    mood.stress -= 0.1f;
                    break;
            }

            // Clamp values
            mood.happiness = Mathf.Clamp01(mood.happiness);
            mood.stress = Mathf.Clamp01(mood.stress);
            mood.confidence = Mathf.Clamp01(mood.confidence);
            mood.stress = Mathf.Clamp01(mood.stress);
            mood.excitement = Mathf.Clamp01(mood.excitement);
        }

        private void CheckForMoodChanges(uint creatureId, CreaturePersonalityProfile profile)
        {
            // Simplified mood change detection
            float moodIntensity = profile.currentMood.happiness + profile.currentMood.stress + profile.currentMood.agitation;

            if (moodIntensity > 2f || moodIntensity < 0.5f)
            {
                OnMoodChanged?.Invoke(creatureId, profile.currentMood);
            }
        }

        private bool AreCreaturesNearby(uint creatureA, uint creatureB)
        {
            // Simplified proximity check - in a real implementation,
            // this would check actual GameObject positions
            return Random.Range(0f, 1f) < 0.1f; // 10% chance for demo purposes
        }

        private void HandleEliteCreature(CreatureGenome eliteCreature)
        {
            if (eliteCreature != null && logPersonalityEvents)
            {
                DebugManager.LogInfo($"Elite creature emerged with unique personality traits");
            }
        }

        private PersonalityType DeterminePersonalityType(PersonalityTrait traits)
        {
            if (traits.extroversion > 0.7f && traits.agreeableness > 0.6f)
                return PersonalityType.Social;
            else if (traits.openness > 0.7f && traits.conscientiousness > 0.6f)
                return PersonalityType.Explorer;
            else if (traits.aggressiveness > 0.7f && traits.extroversion > 0.5f)
                return PersonalityType.Dominant;
            else if (traits.conscientiousness > 0.7f && traits.neuroticism < 0.3f)
                return PersonalityType.Reliable;
            else
                return PersonalityType.Balanced;
        }

        private string[] GetDominantTraits(PersonalityTrait traits)
        {
            var dominantTraits = new List<string>();

            if (traits.extroversion > 0.7f) dominantTraits.Add("Extroverted");
            if (traits.agreeableness > 0.7f) dominantTraits.Add("Agreeable");
            if (traits.conscientiousness > 0.7f) dominantTraits.Add("Conscientious");
            if (traits.neuroticism > 0.7f) dominantTraits.Add("Neurotic");
            if (traits.openness > 0.7f) dominantTraits.Add("Open");
            if (traits.aggressiveness > 0.7f) dominantTraits.Add("Aggressive");

            return dominantTraits.ToArray();
        }

        private BehaviorPrediction[] PredictBehaviors(CreaturePersonalityProfile profile)
        {
            var predictions = new List<BehaviorPrediction>();

            var traits = profile.personalityTraits;

            if (traits.aggressiveness > 0.6f)
                predictions.Add(new BehaviorPrediction { behavior = "Territorial", probability = traits.aggressiveness });

            if (traits.openness > 0.6f)
                predictions.Add(new BehaviorPrediction { behavior = "Exploratory", probability = traits.openness });

            if (traits.extroversion > 0.6f)
                predictions.Add(new BehaviorPrediction { behavior = "Social", probability = traits.extroversion });

            return predictions.ToArray();
        }

        private float[] CalculateSocialCompatibility(uint creatureId)
        {
            // Simplified compatibility calculation
            return new float[] { 0.5f, 0.7f, 0.3f, 0.8f, 0.6f }; // Demo values
        }

        private string[] IdentifyStressFactors(CreaturePersonalityProfile profile)
        {
            var stressors = new List<string>();

            if (profile.currentMood.stress > 0.6f)
            {
                stressors.Add("High environmental pressure");
                stressors.Add("Resource scarcity");
                stressors.Add("Social conflict");
            }

            return stressors.ToArray();
        }

        private string[] GenerateRecommendations(CreaturePersonalityProfile profile)
        {
            var recommendations = new List<string>();

            if (profile.currentMood.stress > 0.7f)
                recommendations.Add("Reduce environmental stressors");

            if (profile.personalityTraits.extroversion > 0.6f && profile.currentMood.happiness < 0.4f)
                recommendations.Add("Increase social interactions");

            return recommendations.ToArray();
        }

        private void OnDrawGizmos()
        {
            if (!showPersonalityGizmos || !Application.isPlaying) return;

            // Draw personality influence ranges
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, socialInteractionRange);
        }

        private void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
            }
        }

        /// <summary>
        /// Converts CreatureGenome from Advanced genetics to GeneticProfile for personality system
        /// </summary>
        private Laboratory.Chimera.Genetics.GeneticProfile ConvertGenomeToGeneticProfile(CreatureGenome genome)
        {
            var geneticProfile = new Laboratory.Chimera.Genetics.GeneticProfile();

            // Copy basic data (using reflection or available setters)
            // This is a simplified conversion - in a real implementation you'd want more sophisticated mapping

            return geneticProfile;
        }

        /// <summary>
        /// Convert PersonalityProfile to PersonalityTrait for compatibility
        /// </summary>
        private PersonalityTrait ConvertProfileToPersonalityTrait(PersonalityProfile profile)
        {
            return new PersonalityTrait
            {
                name = "Combined",
                value = (profile.extraversion + profile.agreeableness + profile.conscientiousness + profile.neuroticism + profile.openness) / 5f,
                baseValue = 0.5f,
                modifier = 0f,
                extroversion = profile.extraversion,
                agreeableness = profile.agreeableness,
                conscientiousness = profile.conscientiousness,
                neuroticism = profile.neuroticism,
                openness = profile.openness,
                aggressiveness = profile.dominance
            };
        }

        /// <summary>
        /// Convert SocialInteraction to SocialInteractionContext for GeneticPersonalitySystem
        /// </summary>
        private SocialInteractionContext CreateSocialInteractionContext(SocialInteraction interaction, uint otherCreatureId)
        {
            return new SocialInteractionContext
            {
                otherCreatureId = otherCreatureId,
                interactionType = interaction.interactionType.ToString(),
                location = Vector3.zero, // Default location - could be enhanced with actual positions
                duration = Time.time - interaction.timestamp,
                isGroupInteraction = false // Could be determined based on interaction type
            };
        }
    }

    // Supporting data structures
    [System.Serializable]
    public class CreaturePersonalityProfile
    {
        public uint creatureId;
        public CreatureGenome genome;
        public PersonalityTrait personalityTraits;
        public MoodState currentMood;
        public BehavioralTendency behavioralTendencies;
        public float registrationTime;

        public string GetPersonalitySummary()
        {
            return $"E:{personalityTraits.extroversion:F2} A:{personalityTraits.agreeableness:F2} " +
                   $"C:{personalityTraits.conscientiousness:F2} N:{personalityTraits.neuroticism:F2} " +
                   $"O:{personalityTraits.openness:F2} Ag:{personalityTraits.aggressiveness:F2}";
        }
    }

    [System.Serializable]
    public class BehaviorModifiers
    {
        public float aggressionBoost;
        public float explorationBoost;
        public float socialityBoost;
        public float speedMultiplier = 1f;
        public float decisionSpeed = 1f;
        public float riskTolerance;

        public static BehaviorModifiers Default => new BehaviorModifiers();
    }

    [System.Serializable]
    public class SocialInteraction
    {
        public uint participantA;
        public uint participantB;
        public SocialInteractionType interactionType;
        public float timestamp;
        public bool success;
        public float intensity;
    }

    [System.Serializable]
    public class PersonalityAnalysis
    {
        public uint creatureId;
        public PersonalityType personalityType;
        public string[] dominantTraits;
        public MoodState currentMoodState;
        public BehaviorPrediction[] behavioralPredictions;
        public float[] socialCompatibility;
        public string[] stressFactors;
        public string[] recommendations;
    }

    [System.Serializable]
    public class BehaviorPrediction
    {
        public string behavior;
        public float probability;
    }


    [System.Serializable]
    public struct EnvironmentalStimulus
    {
        public EnvironmentalStimulusType type;
        public float intensity;
        public float duration;
        public float timestamp;
    }

    [System.Serializable]
    public struct PersonalityTrait
    {
        public string name;
        public float value;
        public float baseValue;
        public float modifier;

        // Big Five personality traits
        public float extroversion;
        public float agreeableness;
        public float conscientiousness;
        public float neuroticism;
        public float openness;

        // Additional traits
        public float aggressiveness;
    }

    [System.Serializable]
    public struct BehavioralTendency
    {
        // Movement patterns
        public float ExplorationTendency;
        public float TerritorialPatrol;
        public float SocialSeeking;
        public float HidingTendency;

        // Combat behaviors
        public float AggressionThreshold;
        public float FlightResponse;
        public float PackDefense;

        // Social behaviors
        public float CooperationWillingness;
        public float LeadershipDesire;
        public float SubmissionTendency;

        // Learning behaviors
        public float RoutinePreference;
        public float InnovationSeeking;
        public float ImitationLearning;
    }

    public enum PersonalityType
    {
        Balanced,
        Social,
        Explorer,
        Dominant,
        Reliable,
        Anxious,
        Creative
    }
}
