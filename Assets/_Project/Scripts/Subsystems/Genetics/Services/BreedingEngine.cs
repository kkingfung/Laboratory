using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using Laboratory.Chimera.Genetics;

namespace Laboratory.Subsystems.Genetics
{
    /// <summary>
    /// Core breeding engine for Project Chimera genetics.
    /// Handles genetic inheritance, compatibility calculations, and offspring generation.
    /// Implements scientifically accurate Mendelian genetics with environmental factors.
    /// </summary>
    public class BreedingEngine : MonoBehaviour, IBreedingService
    {
        [Header("Configuration")]
        [SerializeField] private bool enableAdvancedInheritance = true;
        [SerializeField] private bool enableEnvironmentalEffects = true;
        [SerializeField] private bool enablePredictions = true;

        private GeneticsSubsystemConfig _config;
        private readonly Queue<BreedingOperation> _breedingQueue = new();
        private readonly Dictionary<string, BreedingPrediction> _predictionCache = new();

        // Events
        public event Action<GeneticBreedingResult> OnBreedingComplete;

        // Properties
        public bool IsInitialized { get; private set; }
        public int QueuedBreedings => _breedingQueue.Count;

        #region Initialization

        public async Task InitializeAsync(GeneticsSubsystemConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));

            enableAdvancedInheritance = _config.EnableAdvancedInheritance;
            enableEnvironmentalEffects = _config.EnableEnvironmentalEffects;
            enablePredictions = _config.EnableBreedingPredictions;

            // Start background processing coroutine
            if (_config.PerformanceConfig.EnableBackgroundBreeding)
            {
                StartCoroutine(ProcessBreedingQueue());
            }

            IsInitialized = true;
            await Task.CompletedTask;

            Debug.Log("[BreedingEngine] Initialized successfully");
        }

        #endregion

        #region Core Breeding Operations

        /// <summary>
        /// Breeds two genetic profiles to create offspring
        /// </summary>
        public async Task<GeneticBreedingResult> BreedAsync(
            GeneticProfile parent1,
            GeneticProfile parent2,
            EnvironmentalFactors environment = null)
        {
            if (!IsInitialized)
            {
                throw new InvalidOperationException("BreedingEngine not initialized");
            }

            if (parent1 == null || parent2 == null)
            {
                return CreateFailedResult(parent1?.ProfileId, parent2?.ProfileId, "Parent profiles cannot be null");
            }

            try
            {
                // Calculate compatibility
                var compatibility = CalculateCompatibility(parent1, parent2);

                // Check minimum compatibility requirements
                if (compatibility < _config.BreedingConfig.MinimumCompatibility)
                {
                    return CreateFailedResult(parent1.ProfileId, parent2.ProfileId,
                        $"Compatibility too low: {compatibility:F2} < {_config.BreedingConfig.MinimumCompatibility:F2}");
                }

                // Check inbreeding restrictions
                if (!_config.BreedingConfig.AllowInbreeding && IsInbred(parent1, parent2))
                {
                    return CreateFailedResult(parent1.ProfileId, parent2.ProfileId, "Inbreeding not allowed");
                }

                // Perform breeding
                var result = await PerformBreeding(parent1, parent2, environment, compatibility);

                // Cache prediction if enabled
                if (enablePredictions && result.isSuccessful)
                {
                    CachePrediction(parent1, parent2, result);
                }

                // Fire event
                OnBreedingComplete?.Invoke(result);

                return result;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[BreedingEngine] Breeding failed: {ex.Message}");
                return CreateFailedResult(parent1?.ProfileId, parent2?.ProfileId, ex.Message);
            }
        }

        /// <summary>
        /// Calculates compatibility between two genetic profiles
        /// </summary>
        public float CalculateCompatibility(GeneticProfile parent1, GeneticProfile parent2)
        {
            if (parent1?.Genes == null || parent2?.Genes == null)
                return 0f;

            // Base genetic similarity
            var geneticSimilarity = parent1.GetGeneticSimilarity(parent2);

            // Optimal compatibility is not too similar (inbreeding) or too different (incompatible)
            var diversityScore = CalculateDiversityScore(geneticSimilarity);

            // Generation bonus
            var generationBonus = CalculateGenerationBonus(parent1, parent2);

            // Environmental factors
            var environmentalBonus = 0f;
            if (enableEnvironmentalEffects)
            {
                // Bonus for complementary traits in current environment
                environmentalBonus = CalculateEnvironmentalCompatibility(parent1, parent2);
            }

            // Final compatibility score
            var totalCompatibility = (diversityScore * 0.6f) +
                                   (generationBonus * 0.2f) +
                                   (environmentalBonus * 0.2f);

            return Mathf.Clamp01(totalCompatibility);
        }

        /// <summary>
        /// Predicts breeding outcomes without actually breeding
        /// </summary>
        public BreedingPrediction PredictOutcome(
            GeneticProfile parent1,
            GeneticProfile parent2,
            EnvironmentalFactors environment = null)
        {
            if (!enablePredictions)
                return null;

            var cacheKey = GetPredictionCacheKey(parent1, parent2, environment);
            if (_predictionCache.TryGetValue(cacheKey, out var cachedPrediction))
            {
                return cachedPrediction;
            }

            var prediction = new BreedingPrediction
            {
                compatibility = CalculateCompatibility(parent1, parent2),
                predictedTraits = PredictTraitInheritance(parent1, parent2, environment),
                possibleMutations = PredictMutations(parent1, parent2, environment),
                statRanges = PredictStatRanges(parent1, parent2),
                warnings = GenerateBreedingWarnings(parent1, parent2)
            };

            prediction.successProbability = CalculateSuccessProbability(prediction);

            // Cache the prediction
            _predictionCache[cacheKey] = prediction;

            return prediction;
        }

        #endregion

        #region Breeding Implementation

        private async Task<GeneticBreedingResult> PerformBreeding(
            GeneticProfile parent1,
            GeneticProfile parent2,
            EnvironmentalFactors environment,
            float compatibility)
        {
            // Determine success
            var successRoll = UnityEngine.Random.value;
            var successThreshold = CalculateSuccessThreshold(compatibility);

            if (successRoll > successThreshold)
            {
                return CreateFailedResult(parent1.ProfileId, parent2.ProfileId,
                    $"Breeding failed (roll: {successRoll:F3} > threshold: {successThreshold:F3})");
            }

            // Create offspring genetic profile
            var offspring = CreateOffspring(parent1, parent2, environment);

            // Generate unique offspring ID
            var offspringId = GenerateOffspringId(parent1.ProfileId, parent2.ProfileId);

            // Track inherited traits
            var inheritedTraits = TrackInheritedTraits(offspring, parent1, parent2);

            // Apply mutations if any occurred during breeding
            var mutations = ApplyBreedingMutations(offspring, environment);

            var result = new GeneticBreedingResult
            {
                parent1Id = parent1.ProfileId,
                parent2Id = parent2.ProfileId,
                offspringId = offspringId,
                offspring = offspring,
                compatibility = compatibility,
                inheritedTraits = inheritedTraits,
                mutations = mutations,
                isSuccessful = true,
                environment = environment,
                breedingTime = System.DateTime.UtcNow
            };

            await Task.CompletedTask; // Placeholder for any async operations
            return result;
        }

        private GeneticProfile CreateOffspring(
            GeneticProfile parent1,
            GeneticProfile parent2,
            EnvironmentalFactors environment)
        {
            if (enableAdvancedInheritance)
            {
                return GeneticProfile.CreateOffspring(parent1, parent2, environment);
            }
            else
            {
                // Simple inheritance for performance
                return CreateSimpleOffspring(parent1, parent2);
            }
        }

        private GeneticProfile CreateSimpleOffspring(GeneticProfile parent1, GeneticProfile parent2)
        {
            var allTraits = parent1.Genes.Concat(parent2.Genes)
                .GroupBy(g => g.traitName)
                .Select(group => ChooseRandomGene(group.ToArray()))
                .ToArray();

            return new GeneticProfile(allTraits,
                Math.Max(parent1.Generation, parent2.Generation) + 1,
                $"{parent1.LineageId}x{parent2.LineageId}");
        }

        private Gene ChooseRandomGene(Gene[] genes)
        {
            if (genes.Length == 1)
                return genes[0];

            // Simple 50/50 inheritance
            return UnityEngine.Random.value < 0.5f ? genes[0] : genes[1];
        }

        #endregion

        #region Prediction Logic

        private List<TraitPrediction> PredictTraitInheritance(
            GeneticProfile parent1,
            GeneticProfile parent2,
            EnvironmentalFactors environment)
        {
            var predictions = new List<TraitPrediction>();

            var allTraits = parent1.Genes.Concat(parent2.Genes)
                .GroupBy(g => g.traitName)
                .ToList();

            foreach (var traitGroup in allTraits)
            {
                var genes = traitGroup.ToArray();
                var prediction = PredictSingleTrait(genes, environment);
                predictions.Add(prediction);
            }

            return predictions;
        }

        private TraitPrediction PredictSingleTrait(Gene[] genes, EnvironmentalFactors environment)
        {
            var traitName = genes[0].traitName;
            var values = genes.Where(g => g.value.HasValue).Select(g => g.value.Value).ToArray();

            if (values.Length == 0)
            {
                return new TraitPrediction
                {
                    traitName = traitName,
                    probability = 0f,
                    minValue = 0f,
                    maxValue = 0f,
                    averageValue = 0f,
                    isNovelTrait = false
                };
            }

            var minValue = values.Min();
            var maxValue = values.Max();
            var averageValue = values.Average();

            // Environmental bias can shift the average
            var environmentalBias = environment?.GetTraitBias(traitName) ?? 0f;
            averageValue = Mathf.Clamp01(averageValue + environmentalBias);

            return new TraitPrediction
            {
                traitName = traitName,
                probability = 1f, // Trait will definitely be inherited
                minValue = minValue,
                maxValue = maxValue,
                averageValue = averageValue,
                isNovelTrait = false
            };
        }

        private List<MutationPrediction> PredictMutations(
            GeneticProfile parent1,
            GeneticProfile parent2,
            EnvironmentalFactors environment)
        {
            var predictions = new List<MutationPrediction>();

            // Base mutation rate from config
            var baseMutationRate = _config.MutationConfig.BaseMutationRate;

            // Environmental factors can increase mutation rate
            var environmentalMultiplier = environment != null && _config.MutationConfig.EnableEnvironmentalMutations
                ? _config.MutationConfig.EnvironmentalMutationMultiplier
                : 1f;

            var adjustedMutationRate = baseMutationRate * environmentalMultiplier;

            // Predict different types of mutations
            foreach (MutationType mutationType in Enum.GetValues(typeof(MutationType)))
            {
                var probability = adjustedMutationRate * GetMutationTypeWeight(mutationType);

                if (probability > 0.001f) // Only include mutations with reasonable probability
                {
                    predictions.Add(new MutationPrediction
                    {
                        mutationType = mutationType,
                        affectedTrait = "Random", // Could be more specific based on traits
                        probability = probability,
                        averageSeverity = _config.MutationConfig.AverageMutationSeverity,
                        isBeneficial = UnityEngine.Random.value < 0.3f // 30% chance beneficial
                    });
                }
            }

            return predictions;
        }

        private float[] PredictStatRanges(GeneticProfile parent1, GeneticProfile parent2)
        {
            // Predict min/max stat ranges based on parent genetics
            var stats = new[] { "Health", "Attack", "Defense", "Speed", "Intelligence", "Charisma" };
            var ranges = new float[stats.Length * 2]; // Min/max pairs

            for (int i = 0; i < stats.Length; i++)
            {
                var statName = stats[i];
                var parent1Modifier = GetStatModifier(parent1, statName);
                var parent2Modifier = GetStatModifier(parent2, statName);

                ranges[i * 2] = Mathf.Min(parent1Modifier, parent2Modifier) * 0.8f; // Min with some variance
                ranges[i * 2 + 1] = Mathf.Max(parent1Modifier, parent2Modifier) * 1.2f; // Max with some variance
            }

            return ranges;
        }

        private string[] GenerateBreedingWarnings(GeneticProfile parent1, GeneticProfile parent2)
        {
            var warnings = new List<string>();

            // Check for inbreeding
            if (IsInbred(parent1, parent2))
            {
                warnings.Add("Warning: Genetic similarity is high (possible inbreeding)");
            }

            // Check for low compatibility
            var compatibility = CalculateCompatibility(parent1, parent2);
            if (compatibility < _config.BreedingConfig.OptimalCompatibility)
            {
                warnings.Add($"Warning: Low compatibility ({compatibility:P0})");
            }

            // Check for harmful mutations in lineage
            var harmfulMutations = parent1.Mutations.Concat(parent2.Mutations)
                .Where(m => m.isHarmful)
                .Count();

            if (harmfulMutations > 2)
            {
                warnings.Add($"Warning: Parents carry {harmfulMutations} harmful mutations");
            }

            return warnings.ToArray();
        }

        #endregion

        #region Utility Methods

        private float CalculateDiversityScore(float geneticSimilarity)
        {
            // Optimal compatibility curve - peaks around 30-70% similarity
            var optimalRange = 0.5f;
            var distance = Mathf.Abs(geneticSimilarity - optimalRange);
            return 1f - (distance * 2f); // Linear falloff from optimal
        }

        private float CalculateGenerationBonus(GeneticProfile parent1, GeneticProfile parent2)
        {
            if (!_config.BreedingConfig.EnableGenerationBonuses)
                return 0f;

            var avgGeneration = (parent1.Generation + parent2.Generation) * 0.5f;
            return Mathf.Min(avgGeneration * _config.BreedingConfig.GenerationBonusPerLevel, 0.2f);
        }

        private float CalculateEnvironmentalCompatibility(GeneticProfile parent1, GeneticProfile parent2)
        {
            // Placeholder for environmental compatibility calculation
            // Would check if parents have complementary environmental adaptations
            return 0.05f; // Small bonus for now
        }

        private bool IsInbred(GeneticProfile parent1, GeneticProfile parent2)
        {
            var similarity = parent1.GetGeneticSimilarity(parent2);
            return similarity > 0.8f; // High similarity suggests inbreeding
        }

        private float CalculateSuccessThreshold(float compatibility)
        {
            var baseSuccess = _config.BreedingConfig.BaseSuccessRate;
            var compatibilityBonus = (compatibility - 0.5f) * 0.4f; // Up to ±20% based on compatibility
            return Mathf.Clamp01(baseSuccess + compatibilityBonus);
        }

        private float CalculateSuccessProbability(BreedingPrediction prediction)
        {
            var baseRate = _config.BreedingConfig.BaseSuccessRate;
            var compatibilityModifier = (prediction.compatibility - 0.5f) * 0.4f;

            return Mathf.Clamp01(baseRate + compatibilityModifier);
        }

        private List<string> TrackInheritedTraits(GeneticProfile offspring, GeneticProfile parent1, GeneticProfile parent2)
        {
            var inheritedTraits = new List<string>();

            foreach (var gene in offspring.Genes)
            {
                var fromParent1 = parent1.Genes.Any(g => g.traitName == gene.traitName);
                var fromParent2 = parent2.Genes.Any(g => g.traitName == gene.traitName);

                if (fromParent1 && fromParent2)
                {
                    inheritedTraits.Add($"{gene.traitName} (both parents)");
                }
                else if (fromParent1)
                {
                    inheritedTraits.Add($"{gene.traitName} (parent 1)");
                }
                else if (fromParent2)
                {
                    inheritedTraits.Add($"{gene.traitName} (parent 2)");
                }
            }

            return inheritedTraits;
        }

        private List<Mutation> ApplyBreedingMutations(GeneticProfile offspring, EnvironmentalFactors environment)
        {
            var mutations = new List<Mutation>();

            // Check for mutations based on config
            var mutationRate = _config.MutationConfig.BaseMutationRate;

            if (environment != null && _config.MutationConfig.EnableEnvironmentalMutations)
            {
                mutationRate *= _config.MutationConfig.EnvironmentalMutationMultiplier;
            }

            foreach (var gene in offspring.Genes)
            {
                if (UnityEngine.Random.value < mutationRate)
                {
                    var mutation = CreateRandomMutation(gene.traitName, offspring.Generation);
                    mutations.Add(mutation);

                    // Apply mutation to the gene
                    offspring.TriggerMutation(gene.traitName);
                }
            }

            return mutations;
        }

        private Mutation CreateRandomMutation(string traitName, int generation)
        {
            var mutationTypes = Enum.GetValues(typeof(MutationType)).Cast<MutationType>().ToArray();
            var randomType = mutationTypes[UnityEngine.Random.Range(0, mutationTypes.Length)];

            return new Mutation
            {
                mutationType = randomType,
                affectedTrait = traitName,
                severity = UnityEngine.Random.Range(
                    _config.MutationConfig.MinMutationSeverity,
                    _config.MutationConfig.MaxMutationSeverity),
                generation = generation,
                isHarmful = UnityEngine.Random.value < 0.3f // 30% chance of harmful mutation
            };
        }

        private float GetMutationTypeWeight(MutationType mutationType)
        {
            return mutationType switch
            {
                MutationType.ValueShift => 0.5f,
                MutationType.DominanceChange => 0.3f,
                MutationType.ExpressionChange => 0.15f,
                MutationType.NewTrait => 0.05f,
                _ => 0.1f
            };
        }

        private float GetStatModifier(GeneticProfile profile, string statName)
        {
            // Calculate stat modifier based on relevant genes
            var relevantGenes = profile.Genes.Where(g => IsStatRelevant(g.traitName, statName)).ToArray();

            if (relevantGenes.Length == 0)
                return 1f; // Base modifier

            var totalModifier = 0f;
            foreach (var gene in relevantGenes)
            {
                if (gene.value.HasValue)
                {
                    totalModifier += gene.value.Value;
                }
            }

            return totalModifier / relevantGenes.Length; // Average modifier
        }

        private bool IsStatRelevant(string traitName, string statName)
        {
            // Map traits to stats
            return (statName, traitName) switch
            {
                ("Health", "Vitality") => true,
                ("Health", "Constitution") => true,
                ("Attack", "Strength") => true,
                ("Defense", "Constitution") => true,
                ("Speed", "Agility") => true,
                ("Intelligence", "Intelligence") => true,
                ("Charisma", "Charisma") => true,
                _ => false
            };
        }

        private string GenerateOffspringId(string parent1Id, string parent2Id)
        {
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var hash = (parent1Id + parent2Id + timestamp).GetHashCode();
            return $"offspring_{Math.Abs(hash):X8}";
        }

        private GeneticBreedingResult CreateFailedResult(string parent1Id, string parent2Id, string error)
        {
            return new GeneticBreedingResult
            {
                parent1Id = parent1Id ?? "unknown",
                parent2Id = parent2Id ?? "unknown",
                offspringId = null,
                offspring = null,
                compatibility = 0f,
                inheritedTraits = new List<string>(),
                mutations = new List<Mutation>(),
                isSuccessful = false,
                errorMessage = error,
                breedingTime = System.DateTime.UtcNow
            };
        }

        private string GetPredictionCacheKey(GeneticProfile parent1, GeneticProfile parent2, EnvironmentalFactors environment)
        {
            var envHash = environment?.GetHashCode() ?? 0;
            return $"{parent1.ProfileId}_{parent2.ProfileId}_{envHash}";
        }

        private void CachePrediction(GeneticProfile parent1, GeneticProfile parent2, GeneticBreedingResult result)
        {
            // Cache successful breeding results for future predictions
            var prediction = new BreedingPrediction
            {
                compatibility = result.compatibility,
                successProbability = 1f, // It succeeded
                predictedTraits = result.inheritedTraits.Select(t => new TraitPrediction
                {
                    traitName = t,
                    probability = 1f,
                    averageValue = 0.5f // Placeholder
                }).ToList(),
                possibleMutations = result.mutations.Select(m => new MutationPrediction
                {
                    mutationType = m.mutationType,
                    affectedTrait = m.affectedTrait,
                    probability = 1f,
                    averageSeverity = m.severity,
                    isBeneficial = !m.isHarmful
                }).ToList()
            };

            var cacheKey = GetPredictionCacheKey(parent1, parent2, result.environment);
            _predictionCache[cacheKey] = prediction;
        }

        #endregion

        #region Background Processing

        private System.Collections.IEnumerator ProcessBreedingQueue()
        {
            while (true)
            {
                if (_breedingQueue.Count > 0)
                {
                    var batchSize = _config.PerformanceConfig.BackgroundProcessingBatchSize;
                    var processed = 0;

                    while (_breedingQueue.Count > 0 && processed < batchSize)
                    {
                        var operation = _breedingQueue.Dequeue();
                        _ = ProcessBreedingOperation(operation);
                        processed++;
                    }
                }

                yield return new WaitForSeconds(1f); // Process every second
            }
        }

        private async Task ProcessBreedingOperation(BreedingOperation operation)
        {
            try
            {
                var result = await BreedAsync(operation.parent1, operation.parent2, operation.environment);
                operation.onComplete?.Invoke(result);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[BreedingEngine] Background breeding failed: {ex.Message}");
                operation.onComplete?.Invoke(CreateFailedResult(
                    operation.parent1?.ProfileId,
                    operation.parent2?.ProfileId,
                    ex.Message));
            }
        }

        #endregion

        private class BreedingOperation
        {
            public GeneticProfile parent1;
            public GeneticProfile parent2;
            public EnvironmentalFactors environment;
            public Action<GeneticBreedingResult> onComplete;
        }
    }
}