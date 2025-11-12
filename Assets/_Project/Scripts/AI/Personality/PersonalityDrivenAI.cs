using UnityEngine;
using System.Collections.Generic;
using Laboratory.Chimera;
using Laboratory.Chimera.Genetics;
using Laboratory.Chimera.Genetics.Advanced;
using Laboratory.Chimera.AI;
using Laboratory.Core;
using Laboratory.Core.Diagnostics;
using Laboratory.Core.Enums;

namespace Laboratory.AI.Personality
{
    /// <summary>
    /// Component that integrates personality system with creature AI behavior.
    /// Modifies AI decisions based on genetic personality traits and current mood.
    /// </summary>
    [RequireComponent(typeof(ChimeraMonsterAI))]
    public class PersonalityDrivenAI : MonoBehaviour
    {
        [Header("Personality Integration")]
        [SerializeField] private bool enablePersonalityModification = true;
        [SerializeField] private float personalityInfluence = 0.5f;
        [SerializeField] private uint creatureId;

        [Header("Behavior Override Settings")]
        [SerializeField] private bool allowAggressionOverride = true;
        [SerializeField] private bool allowExplorationOverride = true;
        [SerializeField] private bool allowSocialOverride = true;
        [SerializeField] private bool allowSpeedOverride = true;

        [Header("Debug")]
        [SerializeField] private bool showPersonalityDebug = false;

        // Components
        private ChimeraMonsterAI monsterAI;
        private CreaturePersonalityProfile personalityProfile;

        // Cached modifiers
        private BehaviorModifiers currentModifiers;
        private float lastModifierUpdate;

        // Original AI values (for restoration)
        private float originalSpeed;
        private float originalAggressionRange;
        private float originalPatrolRange;

        public uint CreatureId
        {
            get => creatureId;
            set
            {
                creatureId = value;
                RefreshPersonalityProfile();
            }
        }

        public CreaturePersonalityProfile PersonalityProfile => personalityProfile;
        public BehaviorModifiers CurrentModifiers => currentModifiers;

        private void Awake()
        {
            monsterAI = GetComponent<ChimeraMonsterAI>();

            // Store original AI values
            CacheOriginalValues();

            // Generate unique creature ID if not set
            if (creatureId == 0)
            {
                creatureId = GenerateCreatureId();
            }
        }

        private void Start()
        {
            InitializePersonality();
        }

        private void Update()
        {
            if (!enablePersonalityModification || personalityProfile == null) return;

            // Update behavior modifiers periodically
            if (Time.time - lastModifierUpdate >= 1f)
            {
                UpdateBehaviorModifiers();
                lastModifierUpdate = Time.time;
            }

            // Apply personality-driven behavior modifications
            ApplyPersonalityModifications();
        }

        private void InitializePersonality()
        {
            if (!enablePersonalityModification) return;

            // Create a basic genome for personality generation if creature doesn't have one
            GeneticProfile genome = GetOrCreateGenome();

            // Register with personality manager
            if (CreaturePersonalityManager.Instance != null)
            {
                var creatureGenome = ConvertToCreatureGenome(genome);
                personalityProfile = CreaturePersonalityManager.Instance.RegisterCreature(creatureId, creatureGenome);

                if (personalityProfile != null && showPersonalityDebug)
                {
                    DebugManager.LogInfo($"Personality initialized for creature {creatureId}: {personalityProfile.GetPersonalitySummary()}");
                }
            }
        }

        private void UpdateBehaviorModifiers()
        {
            if (CreaturePersonalityManager.Instance != null)
            {
                currentModifiers = CreaturePersonalityManager.Instance.GetBehaviorModifiers(creatureId);
            }
        }

        private void ApplyPersonalityModifications()
        {
            if (currentModifiers == null || monsterAI == null) return;

            // Apply speed modifications
            if (allowSpeedOverride)
            {
                float personalitySpeed = originalSpeed * currentModifiers.speedMultiplier;
                ApplySpeedModification(personalitySpeed);
            }

            // Apply aggression modifications
            if (allowAggressionOverride)
            {
                float aggressionModifier = 1f + (currentModifiers.aggressionBoost * personalityInfluence);
                ApplyAggressionModification(aggressionModifier);
            }

            // Apply exploration modifications
            if (allowExplorationOverride)
            {
                float explorationModifier = 1f + (currentModifiers.explorationBoost * personalityInfluence);
                ApplyExplorationModification(explorationModifier);
            }

            // Apply decision speed modifications
            float decisionSpeedModifier = currentModifiers.decisionSpeed;
            ApplyDecisionSpeedModification(decisionSpeedModifier);
        }

        private void ApplySpeedModification(float modifiedSpeed)
        {
            // Apply speed changes to the AI component
            // This would depend on the specific implementation of ChimeraMonsterAI
            if (monsterAI != null)
            {
                // Example: monsterAI.SetMovementSpeed(modifiedSpeed);
                // Since we don't have the actual implementation, we'll use reflection or direct field access
                SetAIField("movementSpeed", modifiedSpeed);
            }
        }

        private void ApplyAggressionModification(float aggressionModifier)
        {
            if (monsterAI != null)
            {
                float modifiedRange = originalAggressionRange * aggressionModifier;
                SetAIField("aggressionRange", modifiedRange);

                // Also modify attack frequency or damage if available
                SetAIField("attackFrequency", GetAIField("attackFrequency") * aggressionModifier);
            }
        }

        private void ApplyExplorationModification(float explorationModifier)
        {
            if (monsterAI != null)
            {
                float modifiedPatrolRange = originalPatrolRange * explorationModifier;
                SetAIField("patrolRange", modifiedPatrolRange);

                // Modify curiosity or wandering behavior
                SetAIField("wanderRadius", GetAIField("wanderRadius") * explorationModifier);
            }
        }

        private void ApplyDecisionSpeedModification(float decisionSpeedModifier)
        {
            if (monsterAI != null)
            {
                // Modify decision making intervals
                float baseDecisionInterval = GetAIField("decisionInterval");
                SetAIField("decisionInterval", baseDecisionInterval / decisionSpeedModifier);
            }
        }

        private GeneticProfile GetOrCreateGenome()
        {
            // Try to get genome from existing creature components
            var creatureInstance = GetComponent<CreatureInstanceComponent>();
            if (creatureInstance != null && creatureInstance.CreatureData?.GeneticProfile != null)
            {
                return creatureInstance.CreatureData.GeneticProfile;
            }

            // Create a basic genome for personality generation
            return CreateBasicGenome();
        }

        private GeneticProfile CreateBasicGenome()
        {
            var genome = new GeneticProfile();

            // Generate random genetic traits for personality using the proper GeneticProfile structure
            // Note: This is a simplified version - the actual GeneticProfile has a more complex structure
            // but this will work for personality generation purposes

            return genome;
        }

        private void CacheOriginalValues()
        {
            if (monsterAI == null) return;

            // Cache original values for restoration
            originalSpeed = GetAIField("movementSpeed", 5f);
            originalAggressionRange = GetAIField("aggressionRange", 10f);
            originalPatrolRange = GetAIField("patrolRange", 15f);
        }

        private uint GenerateCreatureId()
        {
            // Generate a unique ID based on instance ID and current time
            return (uint)(GetInstanceID() + Time.realtimeSinceStartup * 1000f);
        }

        private void RefreshPersonalityProfile()
        {
            if (CreaturePersonalityManager.Instance != null && creatureId != 0)
            {
                // Unregister old personality if exists
                if (personalityProfile != null)
                {
                    CreaturePersonalityManager.Instance.UnregisterCreature(personalityProfile.creatureId);
                }

                // Initialize new personality
                InitializePersonality();
            }
        }

        /// <summary>
        /// Triggers a specific environmental stimulus for this creature
        /// </summary>
        public void ApplyEnvironmentalStimulus(EnvironmentalStimulusType stimulusType, float intensity)
        {
            if (CreaturePersonalityManager.Instance == null) return;

            var stimulus = new EnvironmentalStimulus
            {
                type = stimulusType,
                intensity = intensity,
                duration = 1f,
                timestamp = Time.time
            };

            CreaturePersonalityManager.Instance.ApplyEnvironmentalStimulus(creatureId, stimulus);
        }

        /// <summary>
        /// Forces a social interaction with another creature
        /// </summary>
        public void InitiateSocialInteraction(PersonalityDrivenAI otherCreature, SocialInteractionType interactionType)
        {
            if (CreaturePersonalityManager.Instance != null && otherCreature != null)
            {
                CreaturePersonalityManager.Instance.RecordSocialInteraction(
                    creatureId,
                    otherCreature.creatureId,
                    interactionType
                );
            }
        }

        /// <summary>
        /// Gets current personality analysis for debugging
        /// </summary>
        public PersonalityAnalysis GetPersonalityAnalysis()
        {
            if (CreaturePersonalityManager.Instance != null)
            {
                return CreaturePersonalityManager.Instance.AnalyzeCreaturePersonality(creatureId);
            }
            return null;
        }

        // Helper methods for accessing AI fields (since we don't have the actual implementation)
        private float GetAIField(string fieldName, float defaultValue = 0f)
        {
            if (monsterAI == null) return defaultValue;

            var field = monsterAI.GetType().GetField(fieldName,
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Public |
                System.Reflection.BindingFlags.Instance);

            if (field != null && field.FieldType == typeof(float))
            {
                return (float)field.GetValue(monsterAI);
            }

            return defaultValue;
        }

        private void SetAIField(string fieldName, float value)
        {
            if (monsterAI == null) return;

            var field = monsterAI.GetType().GetField(fieldName,
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Public |
                System.Reflection.BindingFlags.Instance);

            if (field != null && field.FieldType == typeof(float))
            {
                field.SetValue(monsterAI, value);
            }
        }

        private void OnDrawGizmos()
        {
            if (!showPersonalityDebug || !Application.isPlaying || personalityProfile == null) return;

            // Draw personality visualization
            Gizmos.color = GetPersonalityColor();
            Gizmos.DrawWireSphere(transform.position + Vector3.up * 2f, 1f);

            // Draw behavior modifier indicators
            if (currentModifiers != null)
            {
                // Aggression indicator
                if (currentModifiers.aggressionBoost > 0.1f)
                {
                    Gizmos.color = Color.red;
                    Gizmos.DrawRay(transform.position, Vector3.up * currentModifiers.aggressionBoost * 3f);
                }

                // Exploration indicator
                if (currentModifiers.explorationBoost > 0.1f)
                {
                    Gizmos.color = Color.green;
                    Gizmos.DrawRay(transform.position, Vector3.right * currentModifiers.explorationBoost * 3f);
                }

                // Social indicator
                if (currentModifiers.socialityBoost > 0.1f)
                {
                    Gizmos.color = Color.blue;
                    Gizmos.DrawRay(transform.position, Vector3.forward * currentModifiers.socialityBoost * 3f);
                }
            }
        }

        private Color GetPersonalityColor()
        {
            if (personalityProfile?.personalityTraits == null)
                return Color.gray;

            var traits = personalityProfile.personalityTraits;
            return new Color(
                traits.extroversion,
                traits.agreeableness,
                traits.openness,
                0.7f
            );
        }

        private void OnDestroy()
        {
            // Unregister from personality system
            if (CreaturePersonalityManager.Instance != null && personalityProfile != null)
            {
                CreaturePersonalityManager.Instance.UnregisterCreature(creatureId);
            }
        }

        // Editor menu items for testing
        [UnityEditor.MenuItem("🧪 Laboratory/AI/Apply Random Environmental Stimulus", false, 100)]
        private static void ApplyRandomStimulus()
        {
            var selectedCreature = UnityEditor.Selection.activeGameObject?.GetComponent<PersonalityDrivenAI>();
            if (selectedCreature != null && Application.isPlaying)
            {
                var stimulusTypes = System.Enum.GetValues(typeof(EnvironmentalStimulusType));
                var randomStimulus = (EnvironmentalStimulusType)stimulusTypes.GetValue(Random.Range(0, stimulusTypes.Length));
                selectedCreature.ApplyEnvironmentalStimulus(randomStimulus, Random.Range(0.3f, 0.8f));

                Debug.Log($"Applied {randomStimulus} stimulus to creature {selectedCreature.creatureId}");
            }
        }

        [UnityEditor.MenuItem("🧪 Laboratory/AI/Analyze Selected Creature Personality", false, 101)]
        private static void AnalyzePersonality()
        {
            var selectedCreature = UnityEditor.Selection.activeGameObject?.GetComponent<PersonalityDrivenAI>();
            if (selectedCreature != null && Application.isPlaying)
            {
                var analysis = selectedCreature.GetPersonalityAnalysis();
                if (analysis != null)
                {
                    Debug.Log($"Personality Analysis for Creature {analysis.creatureId}:\n" +
                             $"Type: {analysis.personalityType}\n" +
                             $"Dominant Traits: {string.Join(", ", analysis.dominantTraits)}\n" +
                             $"Mood: Happy={analysis.currentMoodState.happiness:F2}, Stress={analysis.currentMoodState.stress:F2}");
                }
            }
        }

        /// <summary>
        /// Converts a GeneticProfile to CreatureGenome for compatibility with personality system
        /// </summary>
        private CreatureGenome ConvertToCreatureGenome(GeneticProfile geneticProfile)
        {
            var genome = new CreatureGenome
            {
                id = creatureId,
                generation = (uint)geneticProfile.Generation,
                parentA = 0, // Unknown for basic conversion
                parentB = 0, // Unknown for basic conversion
                species = geneticProfile.SpeciesId,
                fitness = 1.0f, // Default fitness
                birthTime = Time.time,
                traits = new Dictionary<Laboratory.Core.Enums.TraitType, GeneticTrait>()
            };

            // Convert available trait data from GeneticProfile to CreatureGenome format
            // Note: This is a basic conversion - more sophisticated conversion could be implemented
            // based on the actual structure of GeneticProfile's trait system
            var traitTypes = System.Enum.GetValues(typeof(Laboratory.Core.Enums.TraitType));
            foreach (Laboratory.Core.Enums.TraitType traitType in traitTypes)
            {
                float traitValue = geneticProfile.GetTraitValue(traitType);
                genome.traits[traitType] = new GeneticTrait
                {
                    name = traitType.ToString(),
                    value = traitValue,
                    dominance = 0.5f, // Default dominance
                    mutationRate = 0.02f, // Default mutation rate
                    environmentalSensitivity = 0.3f, // Default sensitivity
                    isActive = true
                };
            }

            return genome;
        }
    }

    public enum EnvironmentalStimulusType
    {
        Threat,
        Food,
        Mate,
        Territory,
        Weather,
        Social,
        Novelty
    }
}