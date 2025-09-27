using UnityEngine;
using System.Linq;
using Laboratory.Chimera.Creatures;
using Laboratory.Chimera.Genetics;
using Laboratory.Chimera.Breeding;
using Laboratory.Chimera.Core;
using Laboratory.Core.Events;

namespace Laboratory.Chimera.AI
{
    /// <summary>
    /// Advanced adapter that makes creature AI behavior emerge from genetic traits.
    /// This creates the magic where genetics directly influence personality and behavior patterns.
    /// </summary>
    [RequireComponent(typeof(ChimeraMonsterAI))]
    public class GeneticBehaviorAdapter : MonoBehaviour
    {
        [Header("Genetic Behavior Mapping")]
        [SerializeField] private bool enableGeneticPersonality = true;
        [SerializeField] private bool enableEnvironmentalAdaptation = true;
        [SerializeField] private float adaptationRate = 0.1f;
        [SerializeField] private bool showGeneticDebug = false;

        [Header("Personality Ranges")]
        [SerializeField] private Vector2 aggressionRange = new Vector2(0.1f, 0.9f);
        [SerializeField] private Vector2 curiosityRange = new Vector2(5f, 25f); // patrol radius
        [SerializeField] private Vector2 detectionRange = new Vector2(8f, 30f);
        [SerializeField] private Vector2 loyaltyRange = new Vector2(2f, 15f); // follow distance

        // Components
        private ChimeraMonsterAI monsterAI;
        private EnemyDetectionSystem detectionSystem;
        private CreatureInstance creatureInstance;
        private IEventBus eventBus;

        // Genetic traits cache
        private float cachedAggression = 0.5f;
        private float cachedCuriosity = 0.5f;
        private float cachedIntelligence = 0.5f;
        private float cachedLoyalty = 0.5f;
        private float cachedSocial = 0.5f;
        private float cachedPlayfulness = 0.5f;
        private float lastGeneticUpdate = 0f;

        // Environmental adaptation
        private BiomeType currentBiome = BiomeType.Forest;
        private float timeInCurrentBiome = 0f;
        private float adaptationProgress = 0f;

        #region Unity Lifecycle

        private void Awake()
        {
            InitializeComponents();
        }

        private void Start()
        {
            InitializeGeneticBehavior();
            SubscribeToEvents();
        }

        private void Update()
        {
            if (enableGeneticPersonality && Time.time - lastGeneticUpdate > 1f)
            {
                UpdateGeneticTraits();
                ApplyGeneticBehavior();
                lastGeneticUpdate = Time.time;
            }

            if (enableEnvironmentalAdaptation)
            {
                UpdateEnvironmentalAdaptation();
            }

            if (showGeneticDebug && Time.frameCount % 120 == 0) // Every 2 seconds
            {
                LogGeneticState();
            }
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
        }

        #endregion

        #region Initialization

        private void InitializeComponents()
        {
            monsterAI = GetComponent<ChimeraMonsterAI>();
            detectionSystem = GetComponent<EnemyDetectionSystem>();
            creatureInstance = GetComponent<CreatureInstance>();

            // Try to get event bus from global services
            if (Laboratory.Core.DI.GlobalServiceProvider.IsInitialized)
            {
                Laboratory.Core.DI.GlobalServiceProvider.TryResolve<IEventBus>(out eventBus);
            }

            if (monsterAI == null)
            {
                UnityEngine.Debug.LogError($"[GeneticBehaviorAdapter] ChimeraMonsterAI not found on {gameObject.name}");
                enabled = false;
            }
        }

        private void InitializeGeneticBehavior()
        {
            if (creatureInstance?.GeneticProfile == null)
            {
                UnityEngine.Debug.LogWarning($"[GeneticBehaviorAdapter] No genetic profile found for {gameObject.name}. Creating default genetics.");
                CreateDefaultGenetics();
            }

            UpdateGeneticTraits();
            ApplyGeneticBehavior();
            
            UnityEngine.Debug.Log($"[GeneticBehaviorAdapter] Initialized genetic behavior for {gameObject.name}");
        }

        private void CreateDefaultGenetics()
        {
            // Create basic genetic profile if none exists
            var defaultGenes = new Gene[]
            {
                new Gene { traitName = "Aggression", value = Random.Range(0.3f, 0.7f), dominance = 0.5f, isActive = true },
                new Gene { traitName = "Curiosity", value = Random.Range(0.3f, 0.7f), dominance = 0.5f, isActive = true },
                new Gene { traitName = "Intelligence", value = Random.Range(0.3f, 0.7f), dominance = 0.5f, isActive = true },
                new Gene { traitName = "Loyalty", value = Random.Range(0.4f, 0.8f), dominance = 0.5f, isActive = true },
                new Gene { traitName = "Social", value = Random.Range(0.3f, 0.7f), dominance = 0.5f, isActive = true },
                new Gene { traitName = "Playfulness", value = Random.Range(0.2f, 0.8f), dominance = 0.5f, isActive = true }
            };

            if (creatureInstance != null)
            {
                creatureInstance.GeneticProfile = new GeneticProfile(defaultGenes, 1);
            }
        }

        #endregion

        #region Genetic Behavior Integration

        private void UpdateGeneticTraits()
        {
            if (creatureInstance?.GeneticProfile == null) return;

            var genetics = creatureInstance.GeneticProfile;

            // Cache genetic trait values
            cachedAggression = GetGeneticTrait(genetics, "Aggression", 0.5f);
            cachedCuriosity = GetGeneticTrait(genetics, "Curiosity", 0.5f);
            cachedIntelligence = GetGeneticTrait(genetics, "Intelligence", 0.5f);
            cachedLoyalty = GetGeneticTrait(genetics, "Loyalty", 0.5f);
            cachedSocial = GetGeneticTrait(genetics, "Social", 0.5f);
            cachedPlayfulness = GetGeneticTrait(genetics, "Playfulness", 0.5f);
        }

        private void ApplyGeneticBehavior()
        {
            if (monsterAI == null) return;

            // Apply aggression to AI behavior
            ApplyAggressionGenetics();
            
            // Apply curiosity to exploration behavior
            ApplyCuriosityGenetics();
            
            // Apply intelligence to detection capabilities
            ApplyIntelligenceGenetics();
            
            // Apply loyalty to following behavior
            ApplyLoyaltyGenetics();
            
            // Apply social traits to pack behavior
            ApplySocialGenetics();
            
            // Apply playfulness to idle behavior
            ApplyPlayfulnessGenetics();
            
            UnityEngine.Debug.Log($"[GeneticBehavior] Applied genetics to {gameObject.name}: Aggr={cachedAggression:F2}, Cur={cachedCuriosity:F2}, Int={cachedIntelligence:F2}");
        }

        private void ApplyAggressionGenetics()
        {
            // High aggression = more likely to engage in combat
            float aggressionLevel = Mathf.Lerp(aggressionRange.x, aggressionRange.y, cachedAggression);
            
            // Modify AI behavior type based on aggression
            if (cachedAggression > 0.8f)
            {
                monsterAI.SetBehaviorType(AIBehaviorType.Aggressive);
            }
            else if (cachedAggression < 0.3f)
            {
                monsterAI.SetBehaviorType(AIBehaviorType.Passive);
            }
            else if (cachedLoyalty > 0.6f)
            {
                monsterAI.SetBehaviorType(AIBehaviorType.Companion);
            }
        }

        private void ApplyCuriosityGenetics()
        {
            // High curiosity = larger patrol radius and more exploration
            float patrolRadius = Mathf.Lerp(curiosityRange.x, curiosityRange.y, cachedCuriosity);
            
            // Curious creatures are more likely to investigate sounds
            if (detectionSystem != null)
            {
                // High curiosity makes creatures more responsive to distant sounds
                // Enhanced hearing range based on curiosity
            }
        }

        private void ApplyIntelligenceGenetics()
        {
            // High intelligence = better detection and faster learning
            float detectionRangeValue = Mathf.Lerp(detectionRange.x, detectionRange.y, cachedIntelligence);
            
            if (detectionSystem != null)
            {
                // Intelligent creatures have better detection capabilities
                // This would require extending the EnemyDetectionSystem
            }
        }

        private void ApplyLoyaltyGenetics()
        {
            // High loyalty = stays closer to player, defends more aggressively
            float followDistance = Mathf.Lerp(loyaltyRange.x, loyaltyRange.y, 1f - cachedLoyalty);
            
            // Apply genetic follow distance to AI
            monsterAI.SetGeneticFollowDistance(followDistance);
            
            // Loyal creatures prioritize defending the player
            if (cachedLoyalty > 0.7f)
            {
                monsterAI.SetBehaviorType(AIBehaviorType.Defensive);
            }
        }

        private void ApplySocialGenetics()
        {
            // High social traits = better pack coordination
            // Social creatures respond better to other monsters' distress calls
            if (cachedSocial > 0.6f)
            {
                // Enhanced pack behavior would be implemented here
            }
        }

        private void ApplyPlayfulnessGenetics()
        {
            // Playful creatures have different idle animations and behaviors
            if (cachedPlayfulness > 0.7f)
            {
                // Playful creatures might perform special idle behaviors
            }
        }

        #endregion

        #region Environmental Adaptation

        private void UpdateEnvironmentalAdaptation()
        {
            // Detect current biome
            BiomeType detectedBiome = DetectCurrentBiome();
            
            if (detectedBiome != currentBiome)
            {
                // Biome changed - start adaptation process
                OnBiomeChanged(currentBiome, detectedBiome);
                currentBiome = detectedBiome;
                timeInCurrentBiome = 0f;
                adaptationProgress = 0f;
            }
            else
            {
                timeInCurrentBiome += Time.deltaTime;
            }
            
            // Gradual adaptation over time
            if (timeInCurrentBiome > 60f) // Start adapting after 1 minute in new biome
            {
                float targetAdaptation = 1f;
                adaptationProgress = Mathf.MoveTowards(adaptationProgress, targetAdaptation, adaptationRate * Time.deltaTime);
                
                if (adaptationProgress >= 0.99f && adaptationProgress < 1f)
                {
                    // Adaptation complete - trigger genetic adaptation event
                    OnEnvironmentalAdaptationComplete();
                    adaptationProgress = 1f;
                }
            }
        }

        private BiomeType DetectCurrentBiome()
        {
            // Simple biome detection based on position
            Vector3 pos = transform.position;
            
            if (pos.y > 50f) return BiomeType.Mountain;
            if (pos.y < -10f) return BiomeType.Mountain; // Underground/Cave areas
            if (Physics.OverlapSphere(pos, 10f, LayerMask.GetMask("Water")).Length > 0) return BiomeType.Ocean;
            
            return BiomeType.Forest; // Default
        }

        private void OnBiomeChanged(BiomeType oldBiome, BiomeType newBiome)
        {
            UnityEngine.Debug.Log($"[GeneticBehavior] {gameObject.name} moved from {oldBiome} to {newBiome}");
            
            // Publish environmental change event
            if (eventBus != null && creatureInstance != null)
            {
                var oldEnv = EnvironmentalFactors.FromBiome(oldBiome);
                var newEnv = EnvironmentalFactors.FromBiome(newBiome);
                
                eventBus.Publish(new EnvironmentalAdaptationEvent(
                    creatureInstance, 
                    oldEnv, 
                    newEnv, 
                    new[] { "Temperature Resistance", "Movement Adaptation" }
                ));
            }
        }

        private void OnEnvironmentalAdaptationComplete()
        {
            UnityEngine.Debug.Log($"[GeneticBehavior] {gameObject.name} has adapted to {currentBiome}");
            
            // Apply biome-specific behavioral changes
            ApplyBiomeAdaptation(currentBiome);
        }

        private void ApplyBiomeAdaptation(BiomeType biome)
        {
            switch (biome)
            {
                case BiomeType.Desert:
                    // Desert creatures become more aggressive due to resource scarcity
                    cachedAggression = Mathf.Min(1f, cachedAggression + 0.1f);
                    break;
                    
                case BiomeType.Arctic:
                    // Cold creatures become more social for warmth
                    cachedSocial = Mathf.Min(1f, cachedSocial + 0.15f);
                    break;
                    
                case BiomeType.Ocean:
                    // Aquatic creatures become more curious
                    cachedCuriosity = Mathf.Min(1f, cachedCuriosity + 0.1f);
                    break;
                    
                case BiomeType.Mountain:
                    // Mountain creatures become more independent
                    cachedLoyalty = Mathf.Max(0f, cachedLoyalty - 0.05f);
                    cachedCuriosity = Mathf.Min(1f, cachedCuriosity + 0.1f);
                    break;
            }
            
            // Reapply genetic behavior with adapted traits
            ApplyGeneticBehavior();
        }

        #endregion

        #region Event Handling

        private void SubscribeToEvents()
        {
            if (eventBus == null) return;
            
            eventBus.Subscribe<BreedingSuccessfulEvent>(OnBreedingSuccess);
            eventBus.Subscribe<CreatureMaturedEvent>(OnCreatureMatured);
            eventBus.Subscribe<MutationOccurredEvent>(OnMutationOccurred);
        }

        private void UnsubscribeFromEvents()
        {
            if (eventBus == null) return;
            
            // R3 handles unsubscription automatically on object destruction
            // But we could manually unsubscribe here if needed
        }

        private void OnBreedingSuccess(BreedingSuccessfulEvent evt)
        {
            // If this creature was involved in breeding, it might affect behavior
            if (evt.Parent1 == creatureInstance || evt.Parent2 == creatureInstance)
            {
                // Successful breeding increases happiness, which might affect playfulness
                cachedPlayfulness = Mathf.Min(1f, cachedPlayfulness + 0.05f);
                ApplyPlayfulnessGenetics();
            }
        }

        private void OnCreatureMatured(CreatureMaturedEvent evt)
        {
            if (evt.Creature == creatureInstance)
            {
                // Creature matured - might unlock new behaviors
                UnityEngine.Debug.Log($"[GeneticBehavior] {gameObject.name} has reached maturity");
                
                // Mature creatures might become more independent
                cachedLoyalty = Mathf.Max(0f, cachedLoyalty - 0.1f);
                ApplyLoyaltyGenetics();
            }
        }

        private void OnMutationOccurred(MutationOccurredEvent evt)
        {
            if (evt.Creature == creatureInstance)
            {
                UnityEngine.Debug.Log($"[GeneticBehavior] Mutation occurred in {gameObject.name}: {evt.Mutation.affectedTrait}");
                
                // Mutation occurred - update genetic traits and behavior
                UpdateGeneticTraits();
                ApplyGeneticBehavior();
            }
        }

        #endregion

        #region Utility Methods

        private float GetGeneticTrait(GeneticProfile genetics, string traitName, float defaultValue)
        {
            var gene = genetics.Genes.FirstOrDefault(g => g.traitName == traitName && g.isActive);
            return gene.traitName != null ? gene.value ?? defaultValue : defaultValue;
        }

        private void LogGeneticState()
        {
            UnityEngine.Debug.Log($"[GeneticDebug] {gameObject.name} - " +
                     $"Aggr: {cachedAggression:F2}, " +
                     $"Cur: {cachedCuriosity:F2}, " +
                     $"Int: {cachedIntelligence:F2}, " +
                     $"Loy: {cachedLoyalty:F2}, " +
                     $"Soc: {cachedSocial:F2}, " +
                     $"Play: {cachedPlayfulness:F2}, " +
                     $"Biome: {currentBiome}, " +
                     $"Adapt: {adaptationProgress:F2}");
        }

        #endregion

        #region Public API

        /// <summary>
        /// Force refresh of genetic behavior (useful after genetic modifications)
        /// </summary>
        public void RefreshGeneticBehavior()
        {
            UpdateGeneticTraits();
            ApplyGeneticBehavior();
        }

        /// <summary>
        /// Get current genetic personality summary
        /// </summary>
        public string GetPersonalitySummary()
        {
            return $"Aggressive: {cachedAggression:P0}, " +
                   $"Curious: {cachedCuriosity:P0}, " +
                   $"Intelligent: {cachedIntelligence:P0}, " +
                   $"Loyal: {cachedLoyalty:P0}";
        }

        /// <summary>
        /// Get current environmental adaptation status
        /// </summary>
        public float GetAdaptationProgress()
        {
            return adaptationProgress;
        }

        /// <summary>
        /// Get the creature's dominant personality trait
        /// </summary>
        public string GetDominantTrait()
        {
            var traits = new[]
            {
                ("Aggressive", cachedAggression),
                ("Curious", cachedCuriosity),
                ("Intelligent", cachedIntelligence),
                ("Loyal", cachedLoyalty),
                ("Social", cachedSocial),
                ("Playful", cachedPlayfulness)
            };
            
            return traits.OrderByDescending(t => t.Item2).First().Item1;
        }

        #endregion

        #region Debug Gizmos

        private void OnDrawGizmosSelected()
        {
            if (!showGeneticDebug) return;
            
            // Draw adaptation progress as a color-coded sphere
            Gizmos.color = Color.Lerp(Color.red, Color.green, adaptationProgress);
            Gizmos.DrawWireSphere(transform.position + Vector3.up * 3f, 1f);
            
            // Draw personality traits as bars
            Vector3 basePos = transform.position + Vector3.up * 4f;
            float barHeight = 2f;
            
            // Aggression (Red)
            Gizmos.color = Color.red;
            Gizmos.DrawLine(basePos, basePos + Vector3.up * (barHeight * cachedAggression));
            
            // Curiosity (Yellow)
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(basePos + Vector3.right * 0.3f, basePos + Vector3.right * 0.3f + Vector3.up * (barHeight * cachedCuriosity));
            
            // Intelligence (Blue)
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(basePos + Vector3.right * 0.6f, basePos + Vector3.right * 0.6f + Vector3.up * (barHeight * cachedIntelligence));
            
            // Loyalty (Green)
            Gizmos.color = Color.green;
            Gizmos.DrawLine(basePos + Vector3.right * 0.9f, basePos + Vector3.right * 0.9f + Vector3.up * (barHeight * cachedLoyalty));
        }

        #endregion
    }
}