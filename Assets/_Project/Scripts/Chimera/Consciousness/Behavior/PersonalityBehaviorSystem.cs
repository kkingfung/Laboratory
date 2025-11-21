using UnityEngine;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using Laboratory.Chimera.Consciousness.Core;
using Laboratory.Chimera.Consciousness.Memory;
using System.Collections;
using System.Reflection;

namespace Laboratory.Chimera.Consciousness.Behavior
{
    /// <summary>
    /// Personality-driven behavior system that makes creatures act according to their unique traits
    /// Integrates with existing ChimeraMonsterAI to add personality-based decision making
    /// </summary>
    public class PersonalityBehaviorSystem : MonoBehaviour
    {
        [Header("Behavior Configuration")]
        [SerializeField] private float _behaviorUpdateInterval = 2.0f;
        [SerializeField] private float _personalityInfluence = 0.7f; // How much personality affects decisions
        [SerializeField] private LayerMask _interactionLayers;

        [Header("Animation Integration")]
        [SerializeField] private Animator _creatureAnimator;
        [SerializeField] private string _moodParameter = "Mood";
        [SerializeField] private string _energyParameter = "Energy";
        [SerializeField] private string _personalityParameter = "PersonalityType";

        [Header("Visual Effects")]
        [SerializeField] private ParticleSystem _emotionParticles;
        [SerializeField] private AudioSource _emotionalAudio;

        private CreaturePersonality _personality;
        private CreatureMemory _memory;
        private Component _baseAI; // Reference to AI component via Component base class
        private Entity _creatureEntity;

        private float _lastBehaviorUpdate;
        private PersonalityState _currentState;
        private Coroutine _currentBehavior;

        private void Start()
        {
            InitializePersonalitySystem();
        }

        private void Update()
        {
            if (Time.time - _lastBehaviorUpdate >= _behaviorUpdateInterval)
            {
                UpdatePersonalityBehavior();
                _lastBehaviorUpdate = Time.time;
            }

            UpdateEmotionalDisplay();
        }

        /// <summary>
        /// Initialize the personality system with creature data
        /// </summary>
        public void InitializePersonalitySystem()
        {
            // Find AI component by name to avoid direct type dependency
            _baseAI = GetComponent("ChimeraMonsterAI") as Component;
            if (_baseAI == null)
            {
                UnityEngine.Debug.LogWarning("PersonalityBehaviorSystem: ChimeraMonsterAI component not found, personality behavior will be limited.");
            }

            // Get entity reference for ECS integration
            // GameObjectEntity is deprecated, using Entity.Null for now
            _creatureEntity = Entity.Null;

            // Generate personality if not already set
            if (_personality.PersonalitySeed == 0)
            {
                GenerateInitialPersonality();
            }

            SetupAnimationParameters();
            StartCoroutine(BeginPersonalityBehavior());
        }

        /// <summary>
        /// Generate initial personality from genetics
        /// </summary>
        private void GenerateInitialPersonality()
        {
            // Get genetic data from existing systems
            // CreatureAuthoringComponent not found, using fallback
            var genetics = (Laboratory.Chimera.Core.VisualGeneticData?)null;
            if (genetics.HasValue)
            {
                uint seed = (uint)(transform.position.GetHashCode() ^ Time.time.GetHashCode());
                _personality = CreaturePersonality.GenerateFromGenetics(genetics.Value, seed);
                _memory = new CreatureMemory { OverallMemoryStrength = _personality.LearningRate };
            }
        }

        /// <summary>
        /// Main personality behavior update loop
        /// </summary>
        private void UpdatePersonalityBehavior()
        {
            UpdateEmotionalState();
            DetermineCurrentBehavior();
            InfluenceBaseAI();
        }

        /// <summary>
        /// Update creature's emotional state based on environment and experiences
        /// </summary>
        private void UpdateEmotionalState()
        {
            float environmentComfort = CalculateEnvironmentalComfort();
            float socialSatisfaction = CalculateSocialSatisfaction();
            float playerProximity = GetPlayerProximityBonus();

            // Update happiness based on personality preferences
            float happinessChange = 0f;
            happinessChange += environmentComfort * 0.1f;
            happinessChange += socialSatisfaction * 0.15f;
            happinessChange += playerProximity * 0.2f;

            _personality.HappinessLevel = Mathf.Clamp01(_personality.HappinessLevel + happinessChange * Time.deltaTime);

            // Update stress based on personality traits
            float stressChange = 0f;
            if (_personality.Nervousness > 60 && GetNearbyThreats() > 0)
                stressChange += 0.3f;
            if (_personality.Independence < 30 && GetNearbyCreatures() == 0)
                stressChange += 0.2f;

            _personality.StressLevel = Mathf.Clamp01(_personality.StressLevel + stressChange * Time.deltaTime - 0.1f * Time.deltaTime);

            // Determine emotional state from happiness and stress
            UpdateEmotionalState(_personality.HappinessLevel, _personality.StressLevel);
        }

        /// <summary>
        /// Determine what behavior the creature should engage in
        /// </summary>
        private void DetermineCurrentBehavior()
        {
            PersonalityState newState = _currentState;

            // Personality-driven behavior selection
            Unity.Mathematics.Random random = new Unity.Mathematics.Random(_personality.PersonalitySeed + (uint)Time.time);

            if (_personality.Curiosity > 70 && random.NextFloat() < 0.3f)
            {
                newState = PersonalityState.Exploring;
            }
            else if (_personality.Playfulness > 60 && _personality.EnergyLevel > 0.6f && random.NextFloat() < 0.4f)
            {
                newState = PersonalityState.Playing;
            }
            else if (_personality.Affection > 60 && GetNearestPlayerDistance() < 10f)
            {
                newState = PersonalityState.SeekingAttention;
            }
            else if (_personality.Independence > 70 && GetNearbyCreatures() > 2)
            {
                newState = PersonalityState.SeekingSolitude;
            }
            else if (_personality.StressLevel > 0.7f)
            {
                newState = PersonalityState.Anxious;
            }
            else
            {
                newState = PersonalityState.Idle;
            }

            if (newState != _currentState)
            {
                ChangePersonalityState(newState);
            }
        }

        /// <summary>
        /// Change personality state and start appropriate behavior
        /// </summary>
        private void ChangePersonalityState(PersonalityState newState)
        {
            _currentState = newState;

            if (_currentBehavior != null)
            {
                StopCoroutine(_currentBehavior);
            }

            _currentBehavior = newState switch
            {
                PersonalityState.Exploring => StartCoroutine(ExploringBehavior()),
                PersonalityState.Playing => StartCoroutine(PlayfulBehavior()),
                PersonalityState.SeekingAttention => StartCoroutine(AttentionSeekingBehavior()),
                PersonalityState.SeekingSolitude => StartCoroutine(SolitudeBehavior()),
                PersonalityState.Anxious => StartCoroutine(AnxiousBehavior()),
                _ => StartCoroutine(IdleBehavior())
            };
        }

        /// <summary>
        /// Influence the base AI system with personality traits
        /// </summary>
        private void InfluenceBaseAI()
        {
            if (_baseAI == null) return;

            // Use reflection to safely call AI methods without direct dependency
            try
            {
                var getConfigMethod = _baseAI.GetType().GetMethod("GetAIConfig");
                var updateConfigMethod = _baseAI.GetType().GetMethod("UpdateAIConfig");

                if (getConfigMethod != null && updateConfigMethod != null)
                {
                    var aiConfig = getConfigMethod.Invoke(_baseAI, null);

                    if (aiConfig != null)
                    {
                        var configType = aiConfig.GetType();

                        // Use reflection to modify AI config properties
                        ModifyAIProperty(aiConfig, configType, "aggressionLevel", _personality.Aggression / 100f);
                        ModifyAIProperty(aiConfig, configType, "followDistance", _personality.Independence > 60 ? 8f : 3f);

                        if (_personality.Curiosity > 70)
                        {
                            var patrolField = configType.GetField("patrolRadius");
                            if (patrolField != null)
                            {
                                float currentRadius = (float)patrolField.GetValue(aiConfig);
                                patrolField.SetValue(aiConfig, currentRadius * 1.5f);
                            }
                        }

                        updateConfigMethod.Invoke(_baseAI, new object[] { aiConfig });
                    }
                }
            }
            catch (System.Exception e)
            {
                UnityEngine.Debug.LogWarning($"PersonalityBehaviorSystem: Could not modify AI behavior via reflection: {e.Message}");
            }
        }

        private void ModifyAIProperty(object config, System.Type configType, string propertyName, float targetValue)
        {
            var field = configType.GetField(propertyName);
            var property = configType.GetProperty(propertyName);

            if (field != null)
            {
                float currentValue = (float)field.GetValue(config);
                float newValue = Mathf.Lerp(currentValue, targetValue, _personalityInfluence);
                field.SetValue(config, newValue);
            }
            else if (property != null && property.CanWrite)
            {
                float currentValue = (float)property.GetValue(config);
                float newValue = Mathf.Lerp(currentValue, targetValue, _personalityInfluence);
                property.SetValue(config, newValue);
            }
        }

        /// <summary>
        /// Setup animation parameters for personality display
        /// </summary>
        private void SetupAnimationParameters()
        {
            if (_creatureAnimator == null) return;

            // Set personality type for different animation sets
            int personalityType = DetermineAnimationPersonalityType();
            _creatureAnimator.SetInteger(_personalityParameter, personalityType);
        }

        /// <summary>
        /// Update visual and audio displays of emotion
        /// </summary>
        private void UpdateEmotionalDisplay()
        {
            if (_creatureAnimator != null)
            {
                _creatureAnimator.SetFloat(_moodParameter, (float)_personality.CurrentMood / 10f);
                _creatureAnimator.SetFloat(_energyParameter, _personality.EnergyLevel);
            }

            UpdateEmotionalParticles();
            UpdateEmotionalAudio();
        }

        // Behavior Coroutines
        private IEnumerator BeginPersonalityBehavior()
        {
            yield return new WaitForSeconds(1f); // Let base systems initialize
            _currentState = PersonalityState.Idle;
            _currentBehavior = StartCoroutine(IdleBehavior());
        }

        private IEnumerator ExploringBehavior()
        {
            while (_currentState == PersonalityState.Exploring)
            {
                // Find interesting unexplored areas
                Vector3 explorationTarget = FindExplorationTarget();
                if (explorationTarget != Vector3.zero)
                {
                    // _baseAI?.SetTemporaryTarget(explorationTarget); // Method not available on Component base class
                    yield return new WaitForSeconds(UnityEngine.Random.Range(3f, 8f));
                }
                yield return new WaitForSeconds(1f);
            }
        }

        private IEnumerator PlayfulBehavior()
        {
            while (_currentState == PersonalityState.Playing)
            {
                // Trigger playful animations and movements
                if (_creatureAnimator != null)
                {
                    _creatureAnimator.SetTrigger("PlayBehavior");
                }

                // Maybe chase something or jump around
                PerformPlayfulAction();
                yield return new WaitForSeconds(UnityEngine.Random.Range(2f, 5f));
            }
        }

        private IEnumerator AttentionSeekingBehavior()
        {
            while (_currentState == PersonalityState.SeekingAttention)
            {
                // Move closer to player and perform attention-getting behaviors
                Transform nearestPlayer = GetNearestPlayer();
                if (nearestPlayer != null)
                {
                    // _baseAI?.SetTemporaryTarget(nearestPlayer.position); // Method not available on Component base class

                    if (_creatureAnimator != null)
                    {
                        _creatureAnimator.SetTrigger("SeekAttention");
                    }
                }
                yield return new WaitForSeconds(UnityEngine.Random.Range(3f, 6f));
            }
        }

        private IEnumerator SolitudeBehavior()
        {
            while (_currentState == PersonalityState.SeekingSolitude)
            {
                // Find quiet, isolated areas
                Vector3 quietSpot = FindQuietLocation();
                if (quietSpot != Vector3.zero)
                {
                    // _baseAI?.SetTemporaryTarget(quietSpot); // Method not available on Component base class
                }
                yield return new WaitForSeconds(UnityEngine.Random.Range(5f, 10f));
            }
        }

        private IEnumerator AnxiousBehavior()
        {
            while (_currentState == PersonalityState.Anxious)
            {
                // Display anxious behaviors - pacing, hiding, etc.
                if (_creatureAnimator != null)
                {
                    _creatureAnimator.SetTrigger("Anxious");
                }
                yield return new WaitForSeconds(UnityEngine.Random.Range(1f, 3f));
            }
        }

        private IEnumerator IdleBehavior()
        {
            while (_currentState == PersonalityState.Idle)
            {
                // Normal idle behaviors with personality influence
                yield return new WaitForSeconds(UnityEngine.Random.Range(2f, 5f));
            }
        }

        // Helper Methods
        private float CalculateEnvironmentalComfort()
        {
            float comfort = 0.5f;

            // Check environment preferences
            if (_personality.HabitatLikes.LikesWater && IsNearWater())
                comfort += 0.3f;
            if (_personality.HabitatLikes.PrefersWarmth && GetTemperature() > 0.7f)
                comfort += 0.2f;

            return Mathf.Clamp01(comfort);
        }

        private float CalculateSocialSatisfaction()
        {
            int nearbyCreatures = GetNearbyCreatures();

            if (_personality.SocialBehavior.PrefersGroups)
            {
                return Mathf.Clamp01(nearbyCreatures / 5f);
            }
            else if (_personality.Independence > 70)
            {
                return nearbyCreatures == 0 ? 1f : Mathf.Max(0f, 1f - nearbyCreatures * 0.2f);
            }

            return 0.5f;
        }

        private float GetPlayerProximityBonus()
        {
            float distance = GetNearestPlayerDistance();
            if (distance < 0) return 0f; // No player nearby

            if (_personality.Affection > 60)
            {
                return Mathf.Max(0f, 1f - distance / 10f) * (_personality.Affection / 100f);
            }

            return 0f;
        }

        private void UpdateEmotionalState(float happiness, float stress)
        {
            if (stress > 0.8f)
                _personality.CurrentMood = Laboratory.Chimera.Consciousness.Core.EmotionalState.Fearful;
            else if (stress > 0.6f)
                _personality.CurrentMood = Laboratory.Chimera.Consciousness.Core.EmotionalState.Sad;
            else if (happiness > 0.8f)
                _personality.CurrentMood = Laboratory.Chimera.Consciousness.Core.EmotionalState.Excited;
            else if (happiness > 0.6f)
                _personality.CurrentMood = Laboratory.Chimera.Consciousness.Core.EmotionalState.Happy;
            else
                _personality.CurrentMood = Laboratory.Chimera.Consciousness.Core.EmotionalState.Neutral;
        }

        private int DetermineAnimationPersonalityType()
        {
            if (_personality.Playfulness > 70) return 1; // Playful type
            if (_personality.Aggression > 70) return 2;  // Aggressive type
            if (_personality.Nervousness > 70) return 3; // Nervous type
            return 0; // Balanced type
        }

        private void UpdateEmotionalParticles()
        {
            if (_emotionParticles == null) return;

            Color emotionColor = _personality.CurrentMood switch
            {
                Laboratory.Chimera.Consciousness.Core.EmotionalState.Happy => Color.yellow,
                Laboratory.Chimera.Consciousness.Core.EmotionalState.Excited => Color.magenta,
                Laboratory.Chimera.Consciousness.Core.EmotionalState.Angry => Color.red,
                Laboratory.Chimera.Consciousness.Core.EmotionalState.Fearful => Color.blue,
                Laboratory.Chimera.Consciousness.Core.EmotionalState.Playful => Color.green,
                Laboratory.Chimera.Consciousness.Core.EmotionalState.Loving => Color.pink,
                _ => Color.white
            };

            var main = _emotionParticles.main;
            main.startColor = emotionColor;

            if (_personality.HappinessLevel > 0.7f || _personality.CurrentMood == Laboratory.Chimera.Consciousness.Core.EmotionalState.Excited)
            {
                if (!_emotionParticles.isPlaying)
                    _emotionParticles.Play();
            }
            else
            {
                if (_emotionParticles.isPlaying)
                    _emotionParticles.Stop();
            }
        }

        private void UpdateEmotionalAudio()
        {
            // Add personality-influenced audio here
            // Different personalities could have different vocalizations
        }

        // Utility methods for behavior determination
        private Vector3 FindExplorationTarget() => Vector3.zero; // Implement exploration logic
        private void PerformPlayfulAction() { } // Implement playful actions
        private Vector3 FindQuietLocation() => Vector3.zero; // Implement solitude finding
        private Transform GetNearestPlayer() => null; // Implement player finding
        private float GetNearestPlayerDistance() => -1f; // Implement distance calculation
        private int GetNearbyCreatures() => 0; // Implement creature counting
        private int GetNearbyThreats() => 0; // Implement threat detection
        private bool IsNearWater() => false; // Implement water detection
        private float GetTemperature() => 0.5f; // Implement temperature system

        // Public API for external systems
        public CreaturePersonality GetPersonality() => _personality;
        public CreatureMemory GetMemory() => _memory;
        public void ReactToPlayerInteraction(string playerID, InteractionType interaction, float intensity)
        {
            _memory.RememberPlayerInteraction(playerID, interaction, intensity);
            _personality.UpdateFromExperience(
                interaction == InteractionType.Positive ? ExperienceType.PositivePlayerInteraction : ExperienceType.NegativePlayerInteraction,
                intensity
            );
        }
    }

    /// <summary>
    /// Current personality-driven behavior state
    /// </summary>
    public enum PersonalityState
    {
        Idle,
        Exploring,
        Playing,
        SeekingAttention,
        SeekingSolitude,
        Anxious,
        Bonding
    }
}