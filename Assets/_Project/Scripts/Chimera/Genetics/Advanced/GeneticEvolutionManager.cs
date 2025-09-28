using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Laboratory.Core.Events;

namespace Laboratory.Chimera.Genetics.Advanced
{
    /// <summary>
    /// Manager system that integrates the advanced genetic algorithm with the game world.
    /// Handles population management, environmental updates, and evolutionary events.
    /// </summary>
    public class GeneticEvolutionManager : MonoBehaviour
    {
        [Header("Evolution Settings")]
        [SerializeField] private bool autoEvolution = true;
        [SerializeField] private float evolutionInterval = 300f; // 5 minutes
        [SerializeField] private int initialPopulationSize = 50;
        [SerializeField] private uint geneticSeed = 0;

        [Header("Species Configuration")]
        [SerializeField] private CreatureSpeciesConfig[] availableSpecies;
        [SerializeField] private CreatureSpeciesConfig defaultSpecies;

        [Header("Environmental Control")]
        [SerializeField] private bool enableDynamicEnvironment = true;
        [SerializeField] private float environmentUpdateInterval = 60f; // 1 minute
        [SerializeField, Range(0f, 1f)] private float baseResourceLevel = 0.7f;
        [SerializeField, Range(0f, 1f)] private float climaticVariability = 0.3f;

        // Core systems
        private AdvancedGeneticAlgorithm geneticAlgorithm;
        private Dictionary<string, AdvancedGeneticAlgorithm> speciesPopulations = new Dictionary<string, AdvancedGeneticAlgorithm>();

        // Timers
        private float lastEvolutionTime;
        private float lastEnvironmentUpdate;

        // Events
        public System.Action<PopulationReport> OnPopulationReportGenerated;
        public System.Action<CreatureGenome> OnEliteCreatureEmerged;
        public System.Action<string> OnEvolutionaryMilestone;

        // Analytics
        private EvolutionAnalytics analytics = new EvolutionAnalytics();

        // Singleton access
        private static GeneticEvolutionManager instance;
        public static GeneticEvolutionManager Instance => instance;

        public AdvancedGeneticAlgorithm MainPopulation => geneticAlgorithm;
        public int TotalPopulationSize => speciesPopulations.Values.Sum(pop => pop.PopulationSize);
        public float AveragePopulationFitness => speciesPopulations.Values.Average(pop => pop.AverageFitness);

        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeGeneticSystem();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void InitializeGeneticSystem()
        {
            DebugManager.LogInfo("Initializing Genetic Evolution Manager");

            // Create main population
            geneticAlgorithm = new AdvancedGeneticAlgorithm(geneticSeed);

            // Subscribe to genetic algorithm events
            geneticAlgorithm.OnGenerationComplete += HandleGenerationComplete;
            geneticAlgorithm.OnBreedingComplete += HandleBreedingComplete;
            geneticAlgorithm.OnEvolutionaryEvent += HandleEvolutionaryEvent;

            // Initialize species populations
            InitializeSpeciesPopulations();

            // Set up timers
            lastEvolutionTime = Time.time;
            lastEnvironmentUpdate = Time.time;

            DebugManager.LogInfo($"Genetic system initialized with {availableSpecies?.Length ?? 0} species");
        }

        private void InitializeSpeciesPopulations()
        {
            if (availableSpecies == null || availableSpecies.Length == 0)
            {
                // Create default species if none configured
                if (defaultSpecies != null)
                {
                    geneticAlgorithm.InitializePopulation(initialPopulationSize, defaultSpecies);
                    speciesPopulations["Default"] = geneticAlgorithm;
                }
                return;
            }

            // Initialize population for each species
            foreach (var species in availableSpecies)
            {
                var speciesGA = new AdvancedGeneticAlgorithm(geneticSeed + (uint)species.GetHashCode());
                speciesGA.InitializePopulation(initialPopulationSize / availableSpecies.Length, species);

                speciesPopulations[species.speciesName] = speciesGA;

                // Subscribe to events
                speciesGA.OnGenerationComplete += HandleGenerationComplete;
                speciesGA.OnBreedingComplete += HandleBreedingComplete;
                speciesGA.OnEvolutionaryEvent += HandleEvolutionaryEvent;
            }

            // Set main population to first species
            if (speciesPopulations.Count > 0)
            {
                geneticAlgorithm = speciesPopulations.Values.First();
            }
        }

        private void Update()
        {
            // Auto evolution
            if (autoEvolution && Time.time - lastEvolutionTime >= evolutionInterval)
            {
                RunEvolutionCycle();
                lastEvolutionTime = Time.time;
            }

            // Environmental updates
            if (enableDynamicEnvironment && Time.time - lastEnvironmentUpdate >= environmentUpdateInterval)
            {
                UpdateEnvironmentalPressures();
                lastEnvironmentUpdate = Time.time;
            }

            // Update analytics
            UpdateAnalytics();
        }

        /// <summary>
        /// Manually triggers an evolution cycle for all species
        /// </summary>
        public void RunEvolutionCycle()
        {
            DebugManager.LogInfo("Starting evolution cycle for all species");

            int totalSurvivors = 0;
            foreach (var kvp in speciesPopulations)
            {
                string speciesName = kvp.Key;
                var population = kvp.Value;

                int initialSize = population.PopulationSize;
                population.RunNaturalSelection();
                int survivors = population.PopulationSize;
                totalSurvivors += survivors;

                DebugManager.LogInfo($"Species {speciesName}: {initialSize} → {survivors} survivors");
            }

            analytics.totalEvolutionCycles++;
            OnEvolutionaryMilestone?.Invoke($"Evolution cycle complete: {totalSurvivors} total survivors");
        }

        /// <summary>
        /// Updates environmental pressures affecting all populations
        /// </summary>
        public void UpdateEnvironmentalPressures()
        {
            // Simulate dynamic environmental changes
            float currentResourceLevel = baseResourceLevel + Mathf.Sin(Time.time * 0.01f) * 0.2f;
            float currentClimaticStress = climaticVariability * Mathf.PerlinNoise(Time.time * 0.001f, 0f);
            float diseaseOutbreak = Random.Range(0f, 0.1f); // Occasional disease events

            // Apply to all species populations
            foreach (var population in speciesPopulations.Values)
            {
                population.UpdateEnvironmentalPressure(currentResourceLevel, currentClimaticStress, diseaseOutbreak);
            }

            DebugManager.LogInfo($"Environmental update: Resources {currentResourceLevel:F2}, Climate {currentClimaticStress:F2}");
        }

        /// <summary>
        /// Breeds two creatures from specified species
        /// </summary>
        public CreatureGenome BreedCreatures(string speciesName, uint parentAId, uint parentBId)
        {
            if (!speciesPopulations.TryGetValue(speciesName, out var population))
            {
                DebugManager.LogError($"Species not found: {speciesName}");
                return null;
            }

            var offspring = population.BreedCreatures(parentAId, parentBId);
            if (offspring != null)
            {
                analytics.totalBreedingEvents++;
                DebugManager.SetDebugData("Genetics.TotalBreedings", analytics.totalBreedingEvents);
            }

            return offspring;
        }

        /// <summary>
        /// Gets the top performers across all species
        /// </summary>
        public CreatureGenome[] GetTopPerformers(int count = 10)
        {
            var allCreatures = new List<CreatureGenome>();

            foreach (var population in speciesPopulations.Values)
            {
                var report = population.GeneratePopulationReport();
                allCreatures.AddRange(report.topPerformers);
            }

            return allCreatures.OrderByDescending(c => c.fitness).Take(count).ToArray();
        }

        /// <summary>
        /// Analyzes a specific creature
        /// </summary>
        public CreatureAnalysis AnalyzeCreature(string speciesName, uint creatureId)
        {
            if (!speciesPopulations.TryGetValue(speciesName, out var population))
            {
                return null;
            }

            return population.AnalyzeCreature(creatureId);
        }

        /// <summary>
        /// Generates comprehensive population report
        /// </summary>
        public GlobalPopulationReport GenerateGlobalReport()
        {
            var report = new GlobalPopulationReport
            {
                timestamp = Time.time,
                totalPopulationSize = TotalPopulationSize,
                averageFitness = AveragePopulationFitness,
                speciesReports = new Dictionary<string, PopulationReport>(),
                topPerformers = GetTopPerformers(20),
                evolutionAnalytics = analytics
            };

            foreach (var kvp in speciesPopulations)
            {
                report.speciesReports[kvp.Key] = kvp.Value.GeneratePopulationReport();
            }

            OnPopulationReportGenerated?.Invoke(report.speciesReports.Values.FirstOrDefault());

            return report;
        }

        /// <summary>
        /// Introduces a new creature with specific traits
        /// </summary>
        public CreatureGenome IntroduceCreature(string speciesName, Dictionary<string, float> customTraits)
        {
            if (!speciesPopulations.TryGetValue(speciesName, out var population))
            {
                DebugManager.LogError($"Cannot introduce creature: Species {speciesName} not found");
                return null;
            }

            // Create custom genome (this would need to be implemented in the genetic algorithm)
            DebugManager.LogInfo($"Introducing custom creature to species {speciesName}");
            analytics.totalIntroductions++;

            // For now, return null - this would require extending the genetic algorithm
            return null;
        }

        private void HandleGenerationComplete(GenerationRecord record)
        {
            DebugManager.LogInfo($"Generation {record.generation} complete: Avg fitness {record.averageFitness:F3}, Diversity {record.geneticDiversity:F3}");

            // Check for elite emergence
            if (record.maxFitness > analytics.bestFitnessEver)
            {
                analytics.bestFitnessEver = record.maxFitness;
                OnEliteCreatureEmerged?.Invoke(null); // Would need the actual creature reference
            }

            // Update debug data
            DebugManager.SetDebugData("Genetics.CurrentGeneration", record.generation);
            DebugManager.SetDebugData("Genetics.AverageFitness", record.averageFitness);
            DebugManager.SetDebugData("Genetics.GeneticDiversity", record.geneticDiversity);
        }

        private void HandleBreedingComplete(CreatureGenome parentA, CreatureGenome parentB, CreatureGenome offspring)
        {
            DebugManager.LogInfo($"Breeding complete: Parents (Gen {parentA.generation}, {parentB.generation}) → Offspring (Gen {offspring.generation}, Fitness {offspring.fitness:F3})");

            // Track successful breedings
            analytics.successfulBreedings++;

            // Check for fitness improvements
            if (offspring.fitness > Mathf.Max(parentA.fitness, parentB.fitness))
            {
                analytics.fitnessImprovements++;
                DebugManager.LogInfo("Breeding resulted in fitness improvement!");
            }
        }

        private void HandleEvolutionaryEvent(string eventMessage)
        {
            DebugManager.LogInfo($"Evolutionary event: {eventMessage}");

            // Use circular buffer to prevent unbounded memory growth
            analytics.evolutionaryEvents.Add(new EvolutionaryEvent
            {
                message = eventMessage,
                timestamp = Time.time
            });

            // Keep event log at fixed size - use circular buffer approach
            const int MAX_EVENTS = 50; // Reduced from 100 for better memory usage
            while (analytics.evolutionaryEvents.Count > MAX_EVENTS)
            {
                // Remove oldest events efficiently
                analytics.evolutionaryEvents.RemoveRange(0, analytics.evolutionaryEvents.Count - MAX_EVENTS);
            }
        }

        private void UpdateAnalytics()
        {
            if (speciesPopulations.Count == 0) return;

            // Update real-time analytics
            analytics.currentTotalPopulation = TotalPopulationSize;
            analytics.currentAverageFitness = AveragePopulationFitness;
            analytics.currentGeneticDiversity = speciesPopulations.Values.Average(p => p.GeneticDiversity);

            // Update debug data for monitoring
            DebugManager.SetDebugData("Genetics.TotalPopulation", analytics.currentTotalPopulation);
            DebugManager.SetDebugData("Genetics.TotalSpecies", speciesPopulations.Count);
        }

        /// <summary>
        /// Exports all genetic data for external analysis
        /// </summary>
        public void ExportGeneticData(string filePath)
        {
            var globalReport = GenerateGlobalReport();
            string jsonData = JsonUtility.ToJson(globalReport, true);

            try
            {
                System.IO.File.WriteAllText(filePath, jsonData);
                DebugManager.LogInfo($"Genetic data exported to: {filePath}");
            }
            catch (System.Exception ex)
            {
                DebugManager.LogError($"Failed to export genetic data: {ex.Message}");
            }
        }

        private void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
            }
        }

        // Menu items for easy access
        [UnityEditor.MenuItem("🧪 Laboratory/Genetics/Run Evolution Cycle")]
        private static void MenuRunEvolution()
        {
            if (Application.isPlaying && Instance != null)
            {
                Instance.RunEvolutionCycle();
            }
            else
            {
                Debug.LogWarning("Genetic Evolution Manager not available");
            }
        }

        [UnityEditor.MenuItem("🧪 Laboratory/Genetics/Generate Population Report")]
        private static void MenuGenerateReport()
        {
            if (Application.isPlaying && Instance != null)
            {
                var report = Instance.GenerateGlobalReport();
                Debug.Log($"Population Report: {report.totalPopulationSize} creatures, {report.averageFitness:F3} avg fitness");
            }
            else
            {
                Debug.LogWarning("Genetic Evolution Manager not available");
            }
        }
    }

    // Analytics and reporting structures
    [System.Serializable]
    public class EvolutionAnalytics
    {
        public int totalEvolutionCycles = 0;
        public int totalBreedingEvents = 0;
        public int successfulBreedings = 0;
        public int fitnessImprovements = 0;
        public int totalIntroductions = 0;
        public float bestFitnessEver = 0f;
        public int currentTotalPopulation = 0;
        public float currentAverageFitness = 0f;
        public float currentGeneticDiversity = 0f;
        public List<EvolutionaryEvent> evolutionaryEvents = new List<EvolutionaryEvent>();
    }

    [System.Serializable]
    public class EvolutionaryEvent
    {
        public string message;
        public float timestamp;
    }

    [System.Serializable]
    public class GlobalPopulationReport
    {
        public float timestamp;
        public int totalPopulationSize;
        public float averageFitness;
        public Dictionary<string, PopulationReport> speciesReports;
        public CreatureGenome[] topPerformers;
        public EvolutionAnalytics evolutionAnalytics;
    }
}