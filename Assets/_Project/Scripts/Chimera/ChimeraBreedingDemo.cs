using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Cysharp.Threading.Tasks;
using Laboratory.Chimera.Creatures;
using Laboratory.Chimera.Genetics;
using Laboratory.Chimera.Breeding;
using Laboratory.Chimera.Core;
using Laboratory.Core.Infrastructure;
using Laboratory.Core.Events;
using System;

namespace Laboratory.Chimera
{
    /// <summary>
    /// Example demonstration of Project Chimera's breeding system in action.
    /// This script shows how to create creatures, breed them, and observe genetic inheritance.
    /// Perfect for testing and showcasing the core mechanics to stakeholders.
    /// </summary>
    public class ChimeraBreedingDemo : MonoBehaviour
    {
        [Header("Demo Configuration")]
        [SerializeField] private bool autoStartDemo = false;
        [SerializeField] private float demoInterval = 2f;
        [SerializeField] private int maxGenerations = 3;
        [SerializeField] private Laboratory.Chimera.Core.BiomeType demoBiome = Laboratory.Chimera.Core.BiomeType.Forest;
        [SerializeField] private bool enableDetailedLogging = true;
        
        private IBreedingSystem _breedingSystem;
        private IEventBus _eventBus;
        private List<CreatureInstance> _currentPopulation = new();
        private int _currentGeneration = 1;
        
        #region Unity Lifecycle
        
        private async void Start()
        {
            InitializeServices();
            SubscribeToEvents();
            await CreateInitialPopulation();
            
            if (autoStartDemo)
            {
                await StartBreedingDemo();
            }
            
            LogDemo(" Project Chimera Demo Ready!");
            LogDemo("Use the context menu to start breeding demonstrations.");
        }
        
        private void OnDestroy()
        {
            _breedingSystem?.Dispose();
        }
        
        #endregion
        
        #region Demo Methods
        
        [ContextMenu("Start Breeding Demo")]
        public async void StartBreedingDemoFromMenu()
        {
            await StartBreedingDemo();
        }
        
        [ContextMenu("Show Population Stats")]
        public void ShowPopulationStats()
        {
            LogDemo($"\n POPULATION STATISTICS (Generation {_currentGeneration})");
            LogDemo($"Total Creatures: {_currentPopulation.Count}");
            
            if (_currentPopulation.Count > 0)
            {
                var speciesCount = new Dictionary<string, int>();
                var averageAge = 0f;
                
                foreach (var creature in _currentPopulation)
                {
                    var speciesName = creature.Definition.speciesName;
                    speciesCount[speciesName] = speciesCount.GetValueOrDefault(speciesName, 0) + 1;
                    averageAge += creature.AgeInDays;
                }
                
                averageAge /= _currentPopulation.Count;
                LogDemo($"Average Age: {averageAge:F1} days");
                
                LogDemo("\nSpecies Distribution:");
                foreach (var kvp in speciesCount)
                {
                    LogDemo($"  {kvp.Key}: {kvp.Value} creatures");
                }
            }
        }
        
        private async UniTask StartBreedingDemo()
        {
            LogDemo("\n🧬 STARTING BREEDING DEMO");
            
            for (int generation = _currentGeneration; generation <= maxGenerations; generation++)
            {
                LogDemo($"\n=== GENERATION {generation} ===");
                
                if (_currentPopulation.Count < 2)
                {
                    await CreateInitialPopulation();
                }
                
                var breedingPairs = SelectBreedingPairs();
                LogDemo($"Selected {breedingPairs.Count} breeding pairs");
                
                var newOffspring = new List<CreatureInstance>();
                
                foreach (var pair in breedingPairs)
                {
                    var result = await DemonstrateBreeding(pair.Item1, pair.Item2);
                    if (result.Success)
                    {
                        newOffspring.Add(result.Offspring);
                    }
                    
                    await UniTask.Delay((int)(demoInterval * 1000));
                }
                
                _currentPopulation.AddRange(newOffspring);
                _currentGeneration = generation + 1;
                
                LogDemo($"Generation {generation} complete: {newOffspring.Count} new creatures born");
                await UniTask.Delay((int)(demoInterval * 2000));
            }
            
            LogDemo("\n BREEDING DEMO COMPLETE!");
            ShowPopulationStats();
        }
        
        private async UniTask<BreedingResult> DemonstrateBreeding(CreatureInstance parent1, CreatureInstance parent2)
        {
            LogDemo($"\n Breeding: {parent1.Definition.speciesName} × {parent2.Definition.speciesName}");
            
            var environment = new BreedingEnvironment
            {
                BiomeType = demoBiome,
                Temperature = GetBiomeTemperature(demoBiome),
                FoodAvailability = UnityEngine.Random.Range(0.6f, 1.0f),
                PredatorPressure = UnityEngine.Random.Range(0.1f, 0.5f),
                PopulationDensity = Mathf.Clamp01(_currentPopulation.Count / 20f)
            };
            
            var result = await _breedingSystem.BreedCreaturesAsync(parent1, parent2);
            
            if (result.Success)
            {
                LogDemo($"  Success! 1 offspring produced");
                LogDemo($"  Compatibility: {result.CompatibilityScore:P1}");
                
                var offspring = result.Offspring;
                {
                    AnalyzeOffspring(offspring);
                }
            }
            else
            {
                LogDemo($" Failed: {result.ErrorMessage}");
            }
            
            return result;
        }
        
        private void AnalyzeOffspring(CreatureInstance offspring)
        {
            if (!enableDetailedLogging) return;
            
            var genetics = offspring.GeneticProfile;
            LogDemo($"     Offspring: Gen-{genetics.Generation}");
            
            var traitSummary = genetics.GetTraitSummary(3);
            if (!string.IsNullOrEmpty(traitSummary))
            {
                LogDemo($"     Strong Traits: {traitSummary}");
            }
            
            if (genetics.Mutations.Count > 0)
            {
                LogDemo($"     Mutations: {genetics.Mutations.Count} detected");
            }
            
            LogDemo($"     Genetic Purity: {genetics.GetGeneticPurity():P1}");
        }
        
        #endregion
        
        #region Helper Methods
        
        private void InitializeServices()
        {
            var serviceContainer = ServiceContainer.Instance;
            if (serviceContainer != null)
            {
                _eventBus = serviceContainer.ResolveService<IEventBus>();
                _breedingSystem = serviceContainer.ResolveService<IBreedingSystem>();
            }
            
            if (_eventBus == null)
            {
                _eventBus = new UnifiedEventBus();
            }
            
            if (_breedingSystem == null)
            {
                _breedingSystem = new BreedingSystem(_eventBus);
            }
        }
        
        private void SubscribeToEvents()
        {
            _eventBus.Subscribe<BreedingSuccessfulEvent>(OnBreedingSuccess);
            _eventBus.Subscribe<BreedingFailedEvent>(OnBreedingFailure);
        }
        
        private async UniTask CreateInitialPopulation()
        {
            LogDemo("\n Creating initial creature population...");
            
            _currentPopulation.Clear();
            await CreateDefaultCreatures();
            
            LogDemo($" Created {_currentPopulation.Count} creatures");
        }
        
        private async UniTask CreateDefaultCreatures()
        {
            var defaultSpecies = new CreatureDefinition[]
            {
                CreateDefaultSpecies("Forest Dragon", CreatureSize.Large, 1),
                CreateDefaultSpecies("Mountain Drake", CreatureSize.Medium, 1),
                CreateDefaultSpecies("River Spirit", CreatureSize.Small, 2),
            };
            
            foreach (var species in defaultSpecies)
            {
                for (int i = 0; i < 2; i++)
                {
                    var creature = CreateRandomCreature(species);
                    _currentPopulation.Add(creature);
                }
            }
            
            await UniTask.Yield();
        }
        
        private CreatureDefinition CreateDefaultSpecies(string name, CreatureSize size, int compatibilityGroup)
        {
            var definition = ScriptableObject.CreateInstance<CreatureDefinition>();
            definition.speciesName = name;
            definition.size = size;
            definition.breedingCompatibilityGroup = compatibilityGroup;
            definition.fertilityRate = UnityEngine.Random.Range(0.6f, 0.9f);
            definition.maturationAge = 90;
            definition.maxLifespan = 365 * 5;
            definition.baseStats = new CreatureStats
            {
                health = UnityEngine.Random.Range(80, 120),
                attack = UnityEngine.Random.Range(15, 25),
                defense = UnityEngine.Random.Range(10, 20),
                speed = UnityEngine.Random.Range(8, 15),
                intelligence = UnityEngine.Random.Range(3, 10),
                charisma = UnityEngine.Random.Range(3, 10)
            };
            definition.preferredBiomes = new Laboratory.Chimera.Core.BiomeType[] { Laboratory.Chimera.Core.BiomeType.Forest };
            definition.biomeCompatibility = new float[] { 1.0f };
            
            return definition;
        }
        
        private CreatureInstance CreateRandomCreature(CreatureDefinition definition)
        {
            var genes = new Gene[]
            {
                new Gene { traitName = "Strength", value = UnityEngine.Random.Range(0.3f, 0.9f), dominance = UnityEngine.Random.Range(0.2f, 0.8f), isActive = true },
                new Gene { traitName = "Speed", value = UnityEngine.Random.Range(0.3f, 0.9f), dominance = UnityEngine.Random.Range(0.2f, 0.8f), isActive = true },
                new Gene { traitName = "Intelligence", value = UnityEngine.Random.Range(0.3f, 0.9f), dominance = UnityEngine.Random.Range(0.2f, 0.8f), isActive = true },
                new Gene { traitName = "Resilience", value = UnityEngine.Random.Range(0.3f, 0.9f), dominance = UnityEngine.Random.Range(0.2f, 0.8f), isActive = true }
            };
            
            var genetics = new Laboratory.Chimera.Genetics.GeneticProfile(genes, 1);
            
            return new CreatureInstance
            {
                Definition = definition,
                GeneticProfile = genetics,
                AgeInDays = UnityEngine.Random.Range(90, 200),
                CurrentHealth = definition.baseStats.health,
                Happiness = UnityEngine.Random.Range(0.6f, 0.9f),
                Level = 1,
                IsWild = UnityEngine.Random.value > 0.5f
            };
        }
        
        private List<(CreatureInstance, CreatureInstance)> SelectBreedingPairs()
        {
            var pairs = new List<(CreatureInstance, CreatureInstance)>();
            var availableCreatures = _currentPopulation.Where(c => c.IsAdult).ToList();
            
            int pairCount = Mathf.Min(availableCreatures.Count / 2, 2);
            
            for (int i = 0; i < pairCount && availableCreatures.Count >= 2; i++)
            {
                int index1 = UnityEngine.Random.Range(0, availableCreatures.Count);
                var creature1 = availableCreatures[index1];
                availableCreatures.RemoveAt(index1);
                
                var compatiblePartners = availableCreatures
                    .Where(c => creature1.Definition.CanBreedWith(c.Definition))
                    .ToList();
                
                if (compatiblePartners.Count > 0)
                {
                    var creature2 = compatiblePartners[UnityEngine.Random.Range(0, compatiblePartners.Count)];
                    availableCreatures.Remove(creature2);
                    pairs.Add((creature1, creature2));
                }
            }
            
            return pairs;
        }
        
        private float GetBiomeTemperature(Laboratory.Chimera.Core.BiomeType biome)
        {
            return biome switch
            {
                Laboratory.Chimera.Core.BiomeType.Desert => UnityEngine.Random.Range(35f, 50f),
                Laboratory.Chimera.Core.BiomeType.Arctic => UnityEngine.Random.Range(-20f, 5f),
                Laboratory.Chimera.Core.BiomeType.Forest => UnityEngine.Random.Range(15f, 25f),
                Laboratory.Chimera.Core.BiomeType.Mountain => UnityEngine.Random.Range(0f, 15f),
                Laboratory.Chimera.Core.BiomeType.Ocean => UnityEngine.Random.Range(10f, 20f),
                _ => UnityEngine.Random.Range(18f, 25f)
            };
        }
        
        private void LogDemo(string message)
        {
            if (enableDetailedLogging)
            {
                UnityEngine.Debug.Log($"[Chimera Demo] {message}");
            }
        }
        
        #endregion
        
        #region Event Handlers
        
        private void OnBreedingSuccess(BreedingSuccessfulEvent evt)
        {
            LogDemo($" Breeding event: Success with {evt.Result.Offspring.Length} offspring");
        }
        
        private void OnBreedingFailure(BreedingFailedEvent evt)
        {
            LogDemo($" Breeding failed: {evt.Reason}");
        }
        
        #endregion
    }
}
