using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using Laboratory.Chimera.Genetics;

namespace Laboratory.Subsystems.Genetics
{
    /// <summary>
    /// Handles genetic mutations for Project Chimera.
    /// Processes mutation events, calculates probabilities, and applies genetic changes.
    /// Supports environmental mutation triggers and mutation stability tracking.
    /// </summary>
    public class MutationProcessor : MonoBehaviour, IMutationService
    {
        [Header("Configuration")]
        [SerializeField] private bool enableEnvironmentalMutations = true;
        [SerializeField] private bool enableMutationReversal = true;
        [SerializeField] private bool trackMutationHistory = true;

        private GeneticsSubsystemConfig _config;
        private readonly Dictionary<string, List<Mutation>> _mutationHistory = new();
        private readonly Queue<MutationOperation> _mutationQueue = new();

        // Events
        public event Action<MutationEvent> OnMutationOccurred;

        // Properties
        public bool IsInitialized { get; private set; }
        public int QueuedMutations => _mutationQueue.Count;

        #region Initialization

        public async Task InitializeAsync(GeneticsSubsystemConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));

            enableEnvironmentalMutations = _config.MutationConfig.EnableEnvironmentalMutations;
            enableMutationReversal = _config.MutationConfig.AllowMutationReversal;

            // Start background processing
            if (_config.PerformanceConfig.EnableBackgroundMutations)
            {
                StartCoroutine(ProcessMutationQueue());
            }

            IsInitialized = true;
            await Task.CompletedTask;

            Debug.Log("[MutationProcessor] Initialized successfully");
        }

        #endregion

        #region Core Mutation Operations

        /// <summary>
        /// Applies a mutation to a genetic profile
        /// </summary>
        public async Task<bool> ApplyMutationAsync(
            GeneticProfile profile,
            MutationType mutationType,
            string targetTrait = null,
            float severity = 0.1f)
        {
            if (!IsInitialized)
            {
                throw new InvalidOperationException("MutationProcessor not initialized");
            }

            if (profile == null)
            {
                Debug.LogError("[MutationProcessor] Cannot apply mutation to null profile");
                return false;
            }

            try
            {
                // Validate mutation parameters
                severity = Mathf.Clamp(severity,
                    _config.MutationConfig.MinMutationSeverity,
                    _config.MutationConfig.MaxMutationSeverity);

                // Select target trait if not specified
                if (string.IsNullOrEmpty(targetTrait))
                {
                    targetTrait = SelectRandomTrait(profile);
                }

                if (string.IsNullOrEmpty(targetTrait))
                {
                    Debug.LogWarning("[MutationProcessor] No valid target trait found for mutation");
                    return false;
                }

                // Create mutation
                var mutation = CreateMutation(mutationType, targetTrait, severity, profile.Generation);

                // Apply mutation to profile
                var success = ApplyMutationToProfile(profile, mutation);

                if (success)
                {
                    // Track mutation history
                    if (trackMutationHistory)
                    {
                        RecordMutationHistory(profile.ProfileId, mutation);
                    }

                    // Fire mutation event
                    var mutationEvent = new MutationEvent
                    {
                        creatureId = profile.ProfileId,
                        profile = profile,
                        mutation = mutation,
                        timestamp = DateTime.UtcNow
                    };

                    OnMutationOccurred?.Invoke(mutationEvent);

                    Debug.Log($"[MutationProcessor] Applied {mutationType} mutation to {targetTrait} " +
                             $"(severity: {severity:F3}) on {profile.ProfileId}");
                }

                await Task.CompletedTask;
                return success;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MutationProcessor] Failed to apply mutation: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Calculates mutation probability for a profile
        /// </summary>
        public float CalculateMutationProbability(
            GeneticProfile profile,
            EnvironmentalFactors environment = null)
        {
            if (profile == null)
                return 0f;

            // Base mutation rate
            var baseProbability = _config.MutationConfig.BaseMutationRate;

            // Generation factor - older generations may be more stable
            var generationFactor = CalculateGenerationMutationFactor(profile.Generation);

            // Environmental factors
            var environmentalFactor = 1f;
            if (enableEnvironmentalMutations && environment != null)
            {
                environmentalFactor = CalculateEnvironmentalMutationFactor(environment);
            }

            // Existing mutation load - profiles with many mutations may be less stable
            var mutationLoadFactor = CalculateMutationLoadFactor(profile);

            // Genetic purity factor - pure lines may be more resistant to mutation
            var purityFactor = 1f + (1f - profile.GetGeneticPurity()) * 0.5f;

            var totalProbability = baseProbability * generationFactor * environmentalFactor *
                                 mutationLoadFactor * purityFactor;

            return Mathf.Clamp01(totalProbability);
        }

        /// <summary>
        /// Gets possible mutations for a genetic profile
        /// </summary>
        public List<PossibleMutation> GetPossibleMutations(GeneticProfile profile)
        {
            if (profile?.Genes == null)
                return new List<PossibleMutation>();

            var possibleMutations = new List<PossibleMutation>();

            // Check each trait for possible mutations
            foreach (var gene in profile.Genes)
            {
                if (!gene.isActive || !gene.value.HasValue)
                    continue;

                // Value shift mutations
                possibleMutations.Add(new PossibleMutation
                {
                    mutationType = MutationType.ValueShift,
                    targetTrait = gene.traitName,
                    probability = CalculateTraitMutationProbability(gene, MutationType.ValueShift),
                    expectedSeverity = _config.MutationConfig.AverageMutationSeverity,
                    isBeneficial = DetermineMutationBenefit(gene, MutationType.ValueShift),
                    description = $"Shift {gene.traitName} value by small amount"
                });

                // Dominance change mutations
                possibleMutations.Add(new PossibleMutation
                {
                    mutationType = MutationType.DominanceChange,
                    targetTrait = gene.traitName,
                    probability = CalculateTraitMutationProbability(gene, MutationType.DominanceChange),
                    expectedSeverity = _config.MutationConfig.AverageMutationSeverity * 0.5f,
                    isBeneficial = DetermineMutationBenefit(gene, MutationType.DominanceChange),
                    description = $"Change dominance of {gene.traitName}"
                });

                // Expression change mutations
                if (gene.expression == GeneExpression.Normal)
                {
                    possibleMutations.Add(new PossibleMutation
                    {
                        mutationType = MutationType.ExpressionChange,
                        targetTrait = gene.traitName,
                        probability = CalculateTraitMutationProbability(gene, MutationType.ExpressionChange),
                        expectedSeverity = _config.MutationConfig.AverageMutationSeverity * 0.7f,
                        isBeneficial = DetermineMutationBenefit(gene, MutationType.ExpressionChange),
                        description = $"Change expression level of {gene.traitName}"
                    });
                }
            }

            // Novel trait mutations (rare)
            possibleMutations.Add(new PossibleMutation
            {
                mutationType = MutationType.NewTrait,
                targetTrait = "Random",
                probability = _config.MutationConfig.NovelTraitRate,
                expectedSeverity = _config.MutationConfig.AverageMutationSeverity,
                isBeneficial = UnityEngine.Random.value < 0.4f, // 40% chance beneficial
                description = "Create entirely new trait"
            });

            return possibleMutations.Where(m => m.probability > 0.001f).ToList();
        }

        #endregion

        #region Mutation Implementation

        private Mutation CreateMutation(MutationType type, string targetTrait, float severity, int generation)
        {
            var mutationId = GenerateMutationId();
            var isBeneficial = DetermineMutationBenefit(type, severity);

            return new Mutation
            {
                mutationId = mutationId,
                affectedGeneId = targetTrait,
                type = type,
                effectStrength = severity,
                description = GenerateMutationDescription(type, targetTrait, severity),
                isBeneficial = isBeneficial,
                stabilityFactor = _config.MutationConfig.MutationStabilityBase,
                generationOccurred = generation
            };
        }

        private bool ApplyMutationToProfile(GeneticProfile profile, Mutation mutation)
        {
            switch (mutation.type)
            {
                case MutationType.ValueShift:
                    return ApplyValueShiftMutation(profile, mutation);

                case MutationType.DominanceChange:
                    return ApplyDominanceChangeMutation(profile, mutation);

                case MutationType.ExpressionChange:
                    return ApplyExpressionChangeMutation(profile, mutation);

                case MutationType.NewTrait:
                    return ApplyNewTraitMutation(profile, mutation);

                default:
                    Debug.LogWarning($"[MutationProcessor] Unsupported mutation type: {mutation.type}");
                    return false;
            }
        }

        private bool ApplyValueShiftMutation(GeneticProfile profile, Mutation mutation)
        {
            var targetGene = profile.Genes.FirstOrDefault(g => g.traitName == mutation.affectedGeneId);
            if (targetGene.traitName == null || !targetGene.value.HasValue)
                return false;

            // Calculate shift amount
            var shiftAmount = mutation.isBeneficial
                ? mutation.effectStrength
                : -mutation.effectStrength;

            // Add some randomness
            shiftAmount *= UnityEngine.Random.Range(0.7f, 1.3f);

            // Apply shift
            var newValue = Mathf.Clamp01(targetGene.value.Value + shiftAmount);
            targetGene.value = newValue;

            return true;
        }

        private bool ApplyDominanceChangeMutation(GeneticProfile profile, Mutation mutation)
        {
            var targetGene = profile.Genes.FirstOrDefault(g => g.traitName == mutation.affectedGeneId);
            if (targetGene.traitName == null)
                return false;

            // Modify dominance
            var dominanceChange = mutation.isBeneficial
                ? mutation.effectStrength * 0.5f
                : -mutation.effectStrength * 0.5f;

            targetGene.dominance = Mathf.Clamp01(targetGene.dominance + dominanceChange);

            return true;
        }

        private bool ApplyExpressionChangeMutation(GeneticProfile profile, Mutation mutation)
        {
            var targetGene = profile.Genes.FirstOrDefault(g => g.traitName == mutation.affectedGeneId);
            if (targetGene.traitName == null)
                return false;

            // Change expression level
            if (mutation.isBeneficial)
            {
                targetGene.expression = GeneExpression.Enhanced;
            }
            else
            {
                targetGene.expression = mutation.effectStrength > 0.15f
                    ? GeneExpression.Suppressed
                    : GeneExpression.Normal;
            }

            return true;
        }

        private bool ApplyNewTraitMutation(GeneticProfile profile, Mutation mutation)
        {
            // Create a completely new trait
            var newTraitNames = new[] {
                "Bioluminescence", "Telepathy", "Regeneration", "Camouflage",
                "Sonic Attack", "Poison Resistance", "Night Vision", "Aquatic Breathing"
            };

            var randomTrait = newTraitNames[UnityEngine.Random.Range(0, newTraitNames.Length)];

            // Check if trait already exists
            if (profile.Genes.Any(g => g.traitName == randomTrait))
                return false;

            // Trigger the new trait mutation using existing method
            profile.TriggerMutation(randomTrait);

            return true;
        }

        #endregion

        #region Probability Calculations

        private float CalculateGenerationMutationFactor(int generation)
        {
            // Higher generations might be more stable
            if (generation <= 3)
                return 1.2f; // Early generations more prone to mutation

            if (generation <= 10)
                return 1.0f; // Normal mutation rate

            return Mathf.Max(0.5f, 1.0f - (generation - 10) * 0.05f); // Decreasing rate for older generations
        }

        private float CalculateEnvironmentalMutationFactor(EnvironmentalFactors environment)
        {
            var factor = 1f;

            // Temperature extremes increase mutation rate
            var temperatureDeviation = Mathf.Abs(environment.temperature - 22f) / 50f; // 22°C is optimal
            factor += temperatureDeviation * 0.5f;

            // Low food availability can increase mutation rate (stress)
            if (environment.foodAvailability < 0.5f)
            {
                factor += (0.5f - environment.foodAvailability) * 0.8f;
            }

            // High predator pressure increases mutation rate
            if (environment.predatorPressure > 0.6f)
            {
                factor += (environment.predatorPressure - 0.6f) * 0.6f;
            }

            return Mathf.Clamp(factor, 0.5f, 3.0f);
        }

        private float CalculateMutationLoadFactor(GeneticProfile profile)
        {
            var mutationCount = profile.Mutations.Count;

            if (mutationCount == 0)
                return 1.0f;

            if (mutationCount <= 2)
                return 1.1f; // Slightly increased rate

            if (mutationCount <= 5)
                return 1.3f; // Moderately increased rate

            return 1.5f; // High mutation load makes more mutations likely
        }

        private float CalculateTraitMutationProbability(Gene gene, MutationType mutationType)
        {
            var baseProbability = _config.MutationConfig.BaseMutationRate;

            // Trait-specific factors
            var traitFactor = mutationType switch
            {
                MutationType.ValueShift => 1.0f,
                MutationType.DominanceChange => 0.6f,
                MutationType.ExpressionChange => 0.4f,
                MutationType.NewTrait => 0.1f,
                _ => 0.5f
            };

            // Gene activity factor
            var activityFactor = gene.isActive ? 1.0f : 0.3f;

            // Expression factor - highly expressed genes more likely to mutate
            var expressionFactor = gene.expression switch
            {
                GeneExpression.Enhanced => 1.3f,
                GeneExpression.Normal => 1.0f,
                GeneExpression.Suppressed => 0.7f,
                _ => 1.0f
            };

            return baseProbability * traitFactor * activityFactor * expressionFactor;
        }

        private bool DetermineMutationBenefit(Gene gene, MutationType mutationType)
        {
            // Generally, beneficial mutations are rarer
            var beneficialChance = _config.MutationConfig.BeneficialMutationRate /
                                 (_config.MutationConfig.BeneficialMutationRate + _config.MutationConfig.HarmfulMutationRate);

            // Some mutation types are more likely to be beneficial
            var typeBonus = mutationType switch
            {
                MutationType.Enhancement => 0.3f,
                MutationType.ExpressionChange => 0.1f,
                MutationType.NewTrait => 0.2f,
                _ => 0f
            };

            return UnityEngine.Random.value < (beneficialChance + typeBonus);
        }

        private bool DetermineMutationBenefit(MutationType mutationType, float severity)
        {
            // Smaller mutations more likely to be beneficial
            var severityFactor = 1f - severity;
            var beneficialChance = _config.MutationConfig.BeneficialMutationRate * (1f + severityFactor);

            return UnityEngine.Random.value < beneficialChance;
        }

        #endregion

        #region Utility Methods

        private string SelectRandomTrait(GeneticProfile profile)
        {
            var availableTraits = profile.Genes.Where(g => g.isActive).ToArray();
            if (availableTraits.Length == 0)
                return null;

            var randomGene = availableTraits[UnityEngine.Random.Range(0, availableTraits.Length)];
            return randomGene.traitName;
        }

        private string GenerateMutationId()
        {
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var random = UnityEngine.Random.Range(1000, 9999);
            return $"mut_{timestamp}_{random}";
        }

        private string GenerateMutationDescription(MutationType type, string trait, float severity)
        {
            var severityText = severity switch
            {
                < 0.1f => "minor",
                < 0.2f => "moderate",
                < 0.3f => "significant",
                _ => "major"
            };

            return type switch
            {
                MutationType.ValueShift => $"{severityText} shift in {trait} value",
                MutationType.DominanceChange => $"{severityText} change in {trait} dominance",
                MutationType.ExpressionChange => $"{severityText} change in {trait} expression",
                MutationType.NewTrait => $"emergence of novel {trait} trait",
                _ => $"{severityText} {type} mutation affecting {trait}"
            };
        }

        private void RecordMutationHistory(string profileId, Mutation mutation)
        {
            if (!_mutationHistory.ContainsKey(profileId))
            {
                _mutationHistory[profileId] = new List<Mutation>();
            }

            _mutationHistory[profileId].Add(mutation);

            // Limit history size for performance
            if (_mutationHistory[profileId].Count > 50)
            {
                _mutationHistory[profileId].RemoveAt(0);
            }
        }

        #endregion

        #region Background Processing

        private System.Collections.IEnumerator ProcessMutationQueue()
        {
            while (true)
            {
                if (_mutationQueue.Count > 0)
                {
                    var batchSize = _config.PerformanceConfig.BackgroundProcessingBatchSize;
                    var processed = 0;

                    while (_mutationQueue.Count > 0 && processed < batchSize)
                    {
                        var operation = _mutationQueue.Dequeue();
                        _ = ProcessMutationOperation(operation);
                        processed++;
                    }
                }

                yield return new WaitForSeconds(0.5f); // Process every half second
            }
        }

        private async Task ProcessMutationOperation(MutationOperation operation)
        {
            try
            {
                var success = await ApplyMutationAsync(
                    operation.profile,
                    operation.mutationType,
                    operation.targetTrait,
                    operation.severity);

                operation.onComplete?.Invoke(success);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MutationProcessor] Background mutation failed: {ex.Message}");
                operation.onComplete?.Invoke(false);
            }
        }

        public void QueueMutation(
            GeneticProfile profile,
            MutationType mutationType,
            string targetTrait = null,
            float severity = 0.1f,
            Action<bool> onComplete = null)
        {
            _mutationQueue.Enqueue(new MutationOperation
            {
                profile = profile,
                mutationType = mutationType,
                targetTrait = targetTrait,
                severity = severity,
                onComplete = onComplete
            });
        }

        #endregion

        #region Public Utilities

        /// <summary>
        /// Gets mutation history for a profile
        /// </summary>
        public List<Mutation> GetMutationHistory(string profileId)
        {
            return _mutationHistory.TryGetValue(profileId, out var history)
                ? new List<Mutation>(history)
                : new List<Mutation>();
        }

        /// <summary>
        /// Checks if a mutation can be reversed
        /// </summary>
        public bool CanReverseMutation(GeneticProfile profile, Mutation mutation)
        {
            if (!enableMutationReversal)
                return false;

            // Can only reverse recent mutations
            var generationDiff = profile.Generation - mutation.generationOccurred;
            return generationDiff <= 2 && mutation.stabilityFactor > 0.5f;
        }

        /// <summary>
        /// Attempts to reverse a mutation
        /// </summary>
        public async Task<bool> ReverseMutationAsync(GeneticProfile profile, Mutation mutation)
        {
            if (!CanReverseMutation(profile, mutation))
                return false;

            var reversalChance = _config.MutationConfig.ReversalChance * mutation.stabilityFactor;
            if (UnityEngine.Random.value > reversalChance)
                return false;

            // Create reverse mutation
            var reverseMutation = new Mutation
            {
                mutationId = $"rev_{mutation.mutationId}",
                affectedGeneId = mutation.affectedGeneId,
                type = mutation.type,
                effectStrength = -mutation.effectStrength, // Opposite effect
                description = $"Reversal of {mutation.description}",
                isBeneficial = !mutation.isBeneficial,
                stabilityFactor = 0.9f,
                generationOccurred = profile.Generation
            };

            return await ApplyMutationAsync(profile, reverseMutation.type,
                reverseMutation.affectedGeneId, Math.Abs(reverseMutation.effectStrength));
        }

        #endregion

        private class MutationOperation
        {
            public GeneticProfile profile;
            public MutationType mutationType;
            public string targetTrait;
            public float severity;
            public Action<bool> onComplete;
        }
    }
}