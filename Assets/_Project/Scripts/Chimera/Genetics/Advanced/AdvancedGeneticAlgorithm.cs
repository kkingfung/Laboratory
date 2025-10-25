using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Unity.Collections;
using Unity.Mathematics;

namespace Laboratory.Chimera.Genetics.Advanced
{
    /// <summary>
    /// Advanced genetic algorithm implementation with sophisticated inheritance patterns,
    /// population dynamics, evolutionary pressure, and genetic diversity management.
    /// Designed for complex creature breeding with emergent traits and behaviors.
    /// </summary>
    [System.Serializable]
    public class AdvancedGeneticAlgorithm
    {
        [Header("Population Settings")]
        [SerializeField] private int maxPopulationSize = 1000;
        [SerializeField] private float survivalRate = 0.3f;
        [SerializeField] private float elitePreservationRate = 0.1f;

        [Header("Genetic Parameters")]
        [SerializeField] private float baseMutationRate = 0.05f;
        [SerializeField] private float adaptiveMutationRate = 0.02f;
        [SerializeField] private float crossoverRate = 0.8f;
        [SerializeField] private int maxGenerations = 100;

        [Header("Diversity Management")]
        [SerializeField] private float diversityThreshold = 0.3f;
        [SerializeField] private float inbreedingPenalty = 0.2f;
        [SerializeField] private int genealogyDepth = 5;

        [Header("Environmental Factors")]
        [SerializeField] private bool enableEnvironmentalPressure = true;
        [SerializeField] private float resourceScarcity = 0.5f;
        [SerializeField] private float seasonalVariation = 0.3f;

        // Core data structures
        private List<CreatureGenome> population = new List<CreatureGenome>();
        private Dictionary<uint, CreatureGenome> genomeDatabase = new Dictionary<uint, CreatureGenome>();
        private Dictionary<uint, List<uint>> genealogyTree = new Dictionary<uint, List<uint>>();

        // Analytics and tracking
        private PopulationStatistics currentStats = new PopulationStatistics();
        private List<GenerationRecord> generationHistory = new List<GenerationRecord>();
        private EvolutionaryPressure currentPressure = new EvolutionaryPressure();

        // Random number generation
        private Unity.Mathematics.Random randomGenerator;
        private uint generationCounter = 0;

        public int PopulationSize => population.Count;
        public float AverageFitness => currentStats.averageFitness;
        public float GeneticDiversity => currentStats.geneticDiversity;
        public uint CurrentGeneration => generationCounter;
        public PopulationStatistics Statistics => currentStats;

        public event Action<GenerationRecord> OnGenerationComplete;
        public event Action<CreatureGenome, CreatureGenome, CreatureGenome> OnBreedingComplete;
        public event Action<string> OnEvolutionaryEvent;

        public AdvancedGeneticAlgorithm(uint seed = 0)
        {
            randomGenerator = seed == 0 ? new Unity.Mathematics.Random((uint)UnityEngine.Random.Range(1, int.MaxValue)) : new Unity.Mathematics.Random(seed);
            InitializeEvolutionaryPressure();

            Laboratory.Chimera.Debug.DebugManager.Log("Advanced Genetic Algorithm initialized");
        }

        private void InitializeEvolutionaryPressure()
        {
            currentPressure = new EvolutionaryPressure
            {
                resourceCompetition = resourceScarcity,
                environmentalStress = seasonalVariation,
                predationPressure = 0.3f,
                diseaseResistance = 0.2f,
                climaticStress = 0.1f
            };
        }

        /// <summary>
        /// Creates the initial population with genetic diversity
        /// </summary>
        public void InitializePopulation(int populationSize, CreatureSpeciesConfig speciesConfig)
        {
            population.Clear();
            genomeDatabase.Clear();
            genealogyTree.Clear();
            generationCounter = 0;

            Laboratory.Chimera.Debug.DebugManager.Log($"Initializing population of {populationSize} creatures");

            for (int i = 0; i < populationSize; i++)
            {
                var genome = GenerateRandomGenome(speciesConfig);
                AddCreatureToPopulation(genome);
            }

            CalculatePopulationStatistics();
            Laboratory.Chimera.Debug.DebugManager.Log($"Population initialized with diversity: {currentStats.geneticDiversity:F3}");
        }

        private CreatureGenome GenerateRandomGenome(CreatureSpeciesConfig speciesConfig)
        {
            var genome = new CreatureGenome
            {
                id = GenerateUniqueId(),
                generation = 0,
                parentA = 0,
                parentB = 0,
                birthTime = Time.time,
                species = speciesConfig.speciesName
            };

            // Generate base traits with variation
            genome.traits = new Dictionary<string, GeneticTrait>();

            foreach (var baseTrait in speciesConfig.baseTraits)
            {
                var trait = new GeneticTrait
                {
                    name = baseTrait.name,
                    value = baseTrait.baseValue + randomGenerator.NextFloat(-baseTrait.variation, baseTrait.variation),
                    dominance = randomGenerator.NextFloat(0f, 1f),
                    mutationRate = baseTrait.mutationRate,
                    environmentalSensitivity = baseTrait.environmentalSensitivity
                };

                trait.value = math.clamp(trait.value, baseTrait.minValue, baseTrait.maxValue);
                genome.traits[trait.name] = trait;
            }

            // Calculate initial fitness
            genome.fitness = CalculateFitness(genome);

            return genome;
        }

        /// <summary>
        /// Breeds two creatures to produce offspring with advanced inheritance
        /// </summary>
        public CreatureGenome BreedCreatures(uint parentAId, uint parentBId)
        {
            if (!genomeDatabase.TryGetValue(parentAId, out CreatureGenome parentA) ||
                !genomeDatabase.TryGetValue(parentBId, out CreatureGenome parentB))
            {
                Laboratory.Chimera.Debug.DebugManager.LogError($"Cannot breed: Parent genomes not found ({parentAId}, {parentBId})");
                return null;
            }

            // Check for inbreeding
            float inbreedingCoefficient = CalculateInbreedingCoefficient(parentA, parentB);
            if (inbreedingCoefficient > 0.25f) // High inbreeding threshold
            {
                Laboratory.Chimera.Debug.DebugManager.LogWarning($"High inbreeding coefficient detected: {inbreedingCoefficient:F3}");
            }

            var offspring = CreateOffspring(parentA, parentB, inbreedingCoefficient);

            AddCreatureToPopulation(offspring);
            OnBreedingComplete?.Invoke(parentA, parentB, offspring);

            Laboratory.Chimera.Debug.DebugManager.Log($"Breeding successful: Gen {offspring.generation}, Fitness {offspring.fitness:F3}");

            return offspring;
        }

        private CreatureGenome CreateOffspring(CreatureGenome parentA, CreatureGenome parentB, float inbreedingCoefficient)
        {
            var offspring = new CreatureGenome
            {
                id = GenerateUniqueId(),
                generation = math.max(parentA.generation, parentB.generation) + 1,
                parentA = parentA.id,
                parentB = parentB.id,
                birthTime = Time.time,
                species = parentA.species,
                traits = new Dictionary<string, GeneticTrait>()
            };

            // Inherit traits with crossover and mutation
            var allTraitNames = parentA.traits.Keys.Union(parentB.traits.Keys).ToList();

            foreach (string traitName in allTraitNames)
            {
                var inheritedTrait = InheritTrait(
                    parentA.traits.GetValueOrDefault(traitName),
                    parentB.traits.GetValueOrDefault(traitName),
                    traitName,
                    inbreedingCoefficient
                );

                offspring.traits[traitName] = inheritedTrait;
            }

            // Apply environmental mutations
            ApplyEnvironmentalMutations(offspring);

            // Calculate fitness with inbreeding penalty
            offspring.fitness = CalculateFitness(offspring) * (1f - inbreedingCoefficient * inbreedingPenalty);

            return offspring;
        }

        private GeneticTrait InheritTrait(GeneticTrait traitA, GeneticTrait traitB, string traitName, float inbreedingCoefficient)
        {
            // Handle case where one parent doesn't have this trait
            if (traitA == null && traitB == null) return null;
            if (traitA == null) return ApplyMutation(traitB, inbreedingCoefficient);
            if (traitB == null) return ApplyMutation(traitA, inbreedingCoefficient);

            var inheritedTrait = new GeneticTrait
            {
                name = traitName,
                mutationRate = math.lerp(traitA.mutationRate, traitB.mutationRate, 0.5f),
                environmentalSensitivity = math.lerp(traitA.environmentalSensitivity, traitB.environmentalSensitivity, 0.5f)
            };

            // Determine dominant trait based on dominance values and crossover
            if (randomGenerator.NextFloat() < crossoverRate)
            {
                // Crossover inheritance
                float blendFactor = traitA.dominance / (traitA.dominance + traitB.dominance);
                inheritedTrait.value = math.lerp(traitA.value, traitB.value, blendFactor);
                inheritedTrait.dominance = math.lerp(traitA.dominance, traitB.dominance, 0.5f);
            }
            else
            {
                // Dominant inheritance
                if (traitA.dominance > traitB.dominance)
                {
                    inheritedTrait.value = traitA.value;
                    inheritedTrait.dominance = traitA.dominance;
                }
                else
                {
                    inheritedTrait.value = traitB.value;
                    inheritedTrait.dominance = traitB.dominance;
                }
            }

            // Apply mutation
            return ApplyMutation(inheritedTrait, inbreedingCoefficient);
        }

        private GeneticTrait ApplyMutation(GeneticTrait trait, float inbreedingCoefficient)
        {
            if (trait == null) return null;

            var mutatedTrait = new GeneticTrait
            {
                name = trait.name,
                value = trait.value,
                dominance = trait.dominance,
                mutationRate = trait.mutationRate,
                environmentalSensitivity = trait.environmentalSensitivity
            };

            // Calculate effective mutation rate (higher for inbred offspring)
            float effectiveMutationRate = trait.mutationRate + (inbreedingCoefficient * adaptiveMutationRate);

            if (randomGenerator.NextFloat() < effectiveMutationRate)
            {
                float mutationStrength = UnityEngine.Random.Range(-0.1f, 0.1f); // Gaussian approximation
                mutatedTrait.value += mutationStrength;

                // Beneficial mutation tracking
                bool isBeneficial = mutationStrength > 0 && trait.name.Contains("Intelligence") ||
                                   mutationStrength < 0 && trait.name.Contains("Aggression");

                if (math.abs(mutationStrength) > 0.05f)
                {
                    OnEvolutionaryEvent?.Invoke($"Significant mutation in {trait.name}: {mutationStrength:F3}");
                }
            }

            return mutatedTrait;
        }

        private void ApplyEnvironmentalMutations(CreatureGenome offspring)
        {
            if (!enableEnvironmentalPressure) return;

            foreach (var trait in offspring.traits.Values.ToList())
            {
                float environmentalStress = CalculateEnvironmentalStress(trait);

                if (randomGenerator.NextFloat() < environmentalStress * trait.environmentalSensitivity)
                {
                    float adaptiveMutation = UnityEngine.Random.Range(-0.05f, 0.05f); // Gaussian approximation
                    trait.value += adaptiveMutation;

                    Laboratory.Chimera.Debug.DebugManager.Log($"Environmental adaptation in {trait.name}: {adaptiveMutation:F3}");
                }
            }
        }

        private float CalculateEnvironmentalStress(GeneticTrait trait)
        {
            // Environmental stress based on current pressures
            float stress = 0f;

            stress += currentPressure.resourceCompetition * 0.3f;
            stress += currentPressure.environmentalStress * 0.2f;
            stress += currentPressure.climaticStress * 0.1f;

            return math.clamp(stress, 0f, 1f);
        }

        /// <summary>
        /// Runs natural selection on the population
        /// </summary>
        public void RunNaturalSelection()
        {
            if (population.Count <= 1) return;

            Laboratory.Chimera.Debug.DebugManager.Log($"Running natural selection on population of {population.Count}");

            // Calculate fitness for all creatures
            foreach (var creature in population)
            {
                creature.fitness = CalculateFitness(creature);
            }

            // Sort by fitness (highest first)
            population.Sort((a, b) => b.fitness.CompareTo(a.fitness));

            // Calculate survival numbers
            int survivorCount = Mathf.RoundToInt(population.Count * survivalRate);
            int eliteCount = Mathf.RoundToInt(population.Count * elitePreservationRate);

            survivorCount = math.max(survivorCount, 2); // Ensure minimum breeding population
            eliteCount = math.min(eliteCount, survivorCount);

            // Record generation statistics
            RecordGenerationStatistics();

            // Select survivors (elites + tournament selection)
            var survivors = new List<CreatureGenome>();

            // Preserve elites
            for (int i = 0; i < eliteCount; i++)
            {
                survivors.Add(population[i]);
            }

            // Tournament selection for remaining survivors
            for (int i = eliteCount; i < survivorCount; i++)
            {
                var selected = TournamentSelection(3); // Tournament size of 3
                if (!survivors.Contains(selected))
                {
                    survivors.Add(selected);
                }
            }

            // Update population
            population = survivors;
            generationCounter++;

            CalculatePopulationStatistics();

            OnEvolutionaryEvent?.Invoke($"Natural selection complete: {survivors.Count} survivors, generation {generationCounter}");
            Laboratory.Chimera.Debug.DebugManager.Log($"Natural selection: {survivors.Count} survivors, avg fitness: {currentStats.averageFitness:F3}");
        }

        private CreatureGenome TournamentSelection(int tournamentSize)
        {
            CreatureGenome best = null;
            float bestFitness = float.MinValue;

            for (int i = 0; i < tournamentSize; i++)
            {
                var candidate = population[randomGenerator.NextInt(0, population.Count)];
                if (candidate.fitness > bestFitness)
                {
                    best = candidate;
                    bestFitness = candidate.fitness;
                }
            }

            return best;
        }

        private float CalculateFitness(CreatureGenome genome)
        {
            float fitness = 0f;

            foreach (var trait in genome.traits.Values)
            {
                // Base fitness contribution
                fitness += CalculateTraitFitnessContribution(trait);
            }

            // Apply environmental pressures
            if (enableEnvironmentalPressure)
            {
                fitness *= CalculateEnvironmentalFitnessModifier(genome);
            }

            // Age-based fitness decline (optional)
            float age = Time.time - genome.birthTime;
            float agePenalty = math.max(0f, (age - 100f) * 0.001f); // Decline after 100 time units
            fitness *= (1f - agePenalty);

            return math.max(0.001f, fitness); // Ensure minimum fitness
        }

        private float CalculateTraitFitnessContribution(GeneticTrait trait)
        {
            // Different traits contribute differently to fitness
            return trait.name switch
            {
                "Health" => trait.value * 0.3f,
                "Intelligence" => trait.value * 0.25f,
                "Strength" => trait.value * 0.2f,
                "Agility" => trait.value * 0.15f,
                "Fertility" => trait.value * 0.1f,
                _ => trait.value * 0.05f // Unknown traits have minimal impact
            };
        }

        private float CalculateEnvironmentalFitnessModifier(CreatureGenome genome)
        {
            float modifier = 1f;

            // Resource competition affects all creatures
            modifier *= 1f - (currentPressure.resourceCompetition * 0.2f);

            // Environmental stress affects sensitive creatures more
            if (genome.traits.TryGetValue("Environmental Resistance", out var resistance))
            {
                modifier *= 1f - (currentPressure.environmentalStress * (1f - resistance.value));
            }
            else
            {
                modifier *= 1f - (currentPressure.environmentalStress * 0.3f);
            }

            return math.max(0.1f, modifier);
        }

        private float CalculateInbreedingCoefficient(CreatureGenome parentA, CreatureGenome parentB)
        {
            if (!genealogyTree.ContainsKey(parentA.id) || !genealogyTree.ContainsKey(parentB.id))
            {
                return 0f; // No shared ancestry data
            }

            var ancestryA = GetAncestry(parentA.id, genealogyDepth);
            var ancestryB = GetAncestry(parentB.id, genealogyDepth);

            int sharedAncestors = ancestryA.Intersect(ancestryB).Count();
            int totalAncestors = ancestryA.Union(ancestryB).Count();

            return totalAncestors > 0 ? (float)sharedAncestors / totalAncestors : 0f;
        }

        private HashSet<uint> GetAncestry(uint creatureId, int depth)
        {
            var ancestry = new HashSet<uint>();
            var toProcess = new Queue<(uint id, int currentDepth)>();
            toProcess.Enqueue((creatureId, 0));

            while (toProcess.Count > 0 && toProcess.Peek().currentDepth < depth)
            {
                var (id, currentDepth) = toProcess.Dequeue();

                if (genomeDatabase.TryGetValue(id, out var genome))
                {
                    if (genome.parentA != 0)
                    {
                        ancestry.Add(genome.parentA);
                        toProcess.Enqueue((genome.parentA, currentDepth + 1));
                    }
                    if (genome.parentB != 0)
                    {
                        ancestry.Add(genome.parentB);
                        toProcess.Enqueue((genome.parentB, currentDepth + 1));
                    }
                }
            }

            return ancestry;
        }

        private void CalculatePopulationStatistics()
        {
            if (population.Count == 0) return;

            currentStats.populationSize = population.Count;
            currentStats.averageFitness = population.Average(c => c.fitness);
            currentStats.maxFitness = population.Max(c => c.fitness);
            currentStats.minFitness = population.Min(c => c.fitness);
            currentStats.averageGeneration = (float)population.Average(c => c.generation);
            currentStats.maxGeneration = population.Max(c => c.generation);
            currentStats.geneticDiversity = CalculateGeneticDiversity();

            // Note: SetDebugData methods not available, consider implementing or using Log
            // Laboratory.Chimera.Debug.DebugManager.Log($"Debug data: Population {currentStats.populationSize}, Fitness {currentStats.averageFitness:F3}, Diversity {currentStats.geneticDiversity:F3}");
        }

        private float CalculateGeneticDiversity()
        {
            if (population.Count <= 1) return 0f;

            // Calculate diversity based on trait variance
            float totalDiversity = 0f;
            var allTraitNames = population.SelectMany(c => c.traits.Keys).Distinct().ToList();

            foreach (string traitName in allTraitNames)
            {
                var traitValues = population
                    .Where(c => c.traits.ContainsKey(traitName))
                    .Select(c => c.traits[traitName].value)
                    .ToList();

                if (traitValues.Count > 1)
                {
                    float variance = CalculateVariance(traitValues);
                    totalDiversity += variance;
                }
            }

            return allTraitNames.Count > 0 ? totalDiversity / allTraitNames.Count : 0f;
        }

        private float CalculateVariance(List<float> values)
        {
            if (values.Count <= 1) return 0f;

            float mean = values.Average();
            float sumSquaredDiffs = values.Sum(v => (v - mean) * (v - mean));

            return sumSquaredDiffs / (values.Count - 1);
        }

        private void RecordGenerationStatistics()
        {
            var record = new GenerationRecord
            {
                generation = generationCounter,
                populationSize = population.Count,
                averageFitness = population.Average(c => c.fitness),
                maxFitness = population.Max(c => c.fitness),
                geneticDiversity = CalculateGeneticDiversity(),
                timestamp = Time.time
            };

            generationHistory.Add(record);

            // Keep history manageable
            if (generationHistory.Count > 100)
            {
                generationHistory.RemoveAt(0);
            }

            OnGenerationComplete?.Invoke(record);
        }

        private void AddCreatureToPopulation(CreatureGenome genome)
        {
            population.Add(genome);
            genomeDatabase[genome.id] = genome;

            // Update genealogy tree
            if (genome.parentA != 0 || genome.parentB != 0)
            {
                genealogyTree[genome.id] = new List<uint> { genome.parentA, genome.parentB };
            }
        }

        private uint GenerateUniqueId()
        {
            uint id;
            do
            {
                id = randomGenerator.NextUInt();
            } while (genomeDatabase.ContainsKey(id) || id == 0);

            return id;
        }

        /// <summary>
        /// Updates environmental pressures based on external factors
        /// </summary>
        public void UpdateEnvironmentalPressure(float resourceLevel, float climaticStress, float diseaseOutbreak)
        {
            currentPressure.resourceCompetition = 1f - resourceLevel;
            currentPressure.climaticStress = climaticStress;
            currentPressure.diseaseResistance = diseaseOutbreak;

            OnEvolutionaryEvent?.Invoke($"Environmental pressure updated: Resources {resourceLevel:F2}, Climate {climaticStress:F2}");
        }

        /// <summary>
        /// Gets detailed information about a specific creature
        /// </summary>
        public CreatureAnalysis AnalyzeCreature(uint creatureId)
        {
            if (!genomeDatabase.TryGetValue(creatureId, out var genome))
            {
                return null;
            }

            return new CreatureAnalysis
            {
                genome = genome,
                ancestry = GetAncestry(creatureId, genealogyDepth),
                inbreedingCoefficient = CalculateInbreedingCoefficient(genome, genome),
                predictedOffspringFitness = PredictOffspringFitness(genome),
                environmentalAdaptation = CalculateEnvironmentalAdaptation(genome)
            };
        }

        private float PredictOffspringFitness(CreatureGenome genome)
        {
            // Simplified prediction based on current fitness and genetic quality
            float basePredicton = genome.fitness;
            float geneticQuality = genome.traits.Values.Average(t => t.value);

            return (basePredicton + geneticQuality) * 0.5f;
        }

        private float CalculateEnvironmentalAdaptation(CreatureGenome genome)
        {
            return CalculateEnvironmentalFitnessModifier(genome);
        }

        /// <summary>
        /// Exports population data for analysis
        /// </summary>
        public PopulationReport GeneratePopulationReport()
        {
            return new PopulationReport
            {
                currentStatistics = currentStats,
                generationHistory = generationHistory.ToArray(),
                environmentalPressure = currentPressure,
                topPerformers = population.OrderByDescending(c => c.fitness).Take(10).ToArray(),
                geneticDiversityTrend = generationHistory.TakeLast(10).Select(g => g.geneticDiversity).ToArray()
            };
        }
    }

    // Data structures for the genetic system
    [System.Serializable]
    public class CreatureGenome
    {
        public uint id;
        public uint generation;
        public uint parentA;
        public uint parentB;
        public string species;
        public float fitness;
        public float birthTime;
        public Dictionary<string, GeneticTrait> traits;
    }

    [System.Serializable]
    public class GeneticTrait
    {
        public string name;
        public float value;
        public float dominance;
        public float mutationRate;
        public float environmentalSensitivity;
    }

    [System.Serializable]
    public class PopulationStatistics
    {
        public int populationSize;
        public float averageFitness;
        public float maxFitness;
        public float minFitness;
        public float averageGeneration;
        public uint maxGeneration;
        public float geneticDiversity;
    }

    [System.Serializable]
    public class GenerationRecord
    {
        public uint generation;
        public int populationSize;
        public float averageFitness;
        public float maxFitness;
        public float geneticDiversity;
        public float timestamp;
    }

    [System.Serializable]
    public class EvolutionaryPressure
    {
        public float resourceCompetition;
        public float environmentalStress;
        public float predationPressure;
        public float diseaseResistance;
        public float climaticStress;
    }

    [System.Serializable]
    public class CreatureAnalysis
    {
        public CreatureGenome genome;
        public HashSet<uint> ancestry;
        public float inbreedingCoefficient;
        public float predictedOffspringFitness;
        public float environmentalAdaptation;
    }

    [System.Serializable]
    public class PopulationReport
    {
        public PopulationStatistics currentStatistics;
        public GenerationRecord[] generationHistory;
        public EvolutionaryPressure environmentalPressure;
        public CreatureGenome[] topPerformers;
        public float[] geneticDiversityTrend;
    }

    [System.Serializable]
    public class CreatureSpeciesConfig : ScriptableObject
    {
        public string speciesName;
        public BaseTraitConfig[] baseTraits;
    }

    [System.Serializable]
    public class BaseTraitConfig
    {
        public string name;
        public float baseValue;
        public float minValue;
        public float maxValue;
        public float variation;
        public float mutationRate;
        public float environmentalSensitivity;
    }
}