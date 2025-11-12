using UnityEngine;
using Unity.Entities;
using Laboratory.Chimera.AI;
using Laboratory.Core.ECS.Components;
using Laboratory.Chimera.Configuration;
using Laboratory.Chimera.Genetics;
using Laboratory.Chimera.Breeding;
using Laboratory.Chimera.ECS;
using Laboratory.Chimera.Core;
using Laboratory.Core.Enums;
using Laboratory.Shared.Types;
using System.Collections.Generic;
using System.Linq;
using ChimeraGeneticProfile = Laboratory.Chimera.Genetics.GeneticProfile;

namespace Laboratory.Chimera.Integration
{
    /// <summary>
    /// EXTENSION METHODS - Bridge existing MonoBehaviour systems with new ECS architecture
    /// PURPOSE: Add ECS functionality to existing classes without modifying their source
    /// USAGE: Import this namespace and existing classes gain ECS integration methods
    /// </summary>
    public static class ChimeraExtensionMethods
    {
        #region ChimeraMonsterAI Extensions

        /// <summary>
        /// Convert MonoBehaviour AI to ECS components (extension method)
        /// </summary>
        public static Laboratory.Chimera.ECS.ChimeraGeneticDataComponent ToECSGeneticsComponent(this ChimeraMonsterAI monsterAI)
        {
            var genetics = monsterAI.GetGeneticsData();

            // Get individual trait values using enum-based calls
            var aggression = genetics.GetTraitValue(TraitType.Aggression, 0.5f);
            var sociability = genetics.GetTraitValue(TraitType.Sociability, 0.5f);
            var curiosity = genetics.GetTraitValue(TraitType.Curiosity, 0.5f);
            var intelligence = genetics.GetTraitValue(TraitType.Intelligence, 0.5f);
            var size = genetics.GetTraitValue(TraitType.Size, 1.0f);
            var speed = genetics.GetTraitValue(TraitType.Speed, 1.0f);
            var stamina = genetics.GetTraitValue(TraitType.Stamina, 1.0f);
            var adaptability = genetics.GetTraitValue(TraitType.Adaptability, 0.6f);

            // Calculate overall fitness from trait combination
            var overallFitness = CalculateOverallFitness(aggression, sociability, intelligence, size, speed, stamina, adaptability);

            // Calculate mutation rate based on generation and fitness
            var mutationRate = CalculateMutationRate(genetics.Generation, overallFitness);

            // Determine native biome based on environmental tolerance traits
            var heatTolerance = genetics.GetTraitValue(TraitType.HeatTolerance, 0.5f);
            var coldTolerance = genetics.GetTraitValue(TraitType.ColdTolerance, 0.5f);
            var waterAffinity = genetics.GetTraitValue(TraitType.WaterAffinity, 0.5f);
            var nativeBiome = DetermineNativeBiome(heatTolerance, coldTolerance, waterAffinity, size);

            return new Laboratory.Chimera.ECS.ChimeraGeneticDataComponent
            {
                Aggression = aggression,
                Sociability = sociability,
                Curiosity = curiosity,
                Intelligence = intelligence,
                Size = size,
                Speed = speed,
                Stamina = stamina,
                Fertility = genetics.GetTraitValue(TraitType.Fertility, 0.7f),
                Dominance = genetics.GetTraitValue(TraitType.Dominance, 0.5f),
                Metabolism = genetics.GetTraitValue(TraitType.Metabolism, 1.0f),
                HeatTolerance = heatTolerance,
                ColdTolerance = coldTolerance,
                WaterAffinity = waterAffinity,
                Adaptability = adaptability,
                Camouflage = genetics.GetTraitValue(TraitType.Camouflage, 0.5f),
                Caution = genetics.GetTraitValue(TraitType.Caution, 0.5f),
                OverallFitness = overallFitness, // Calculated fitness
                MutationRate = mutationRate, // Generation-based mutation rate
                NativeBiome = nativeBiome // Environment-based biome
            };
        }

        /// <summary>
        /// Get ECS-compatible behavior state from MonoBehaviour AI
        /// </summary>
        public static Laboratory.Chimera.ECS.BehaviorStateComponent ToECSBehaviorState(this ChimeraMonsterAI monsterAI)
        {
            // Calculate decision confidence based on intelligence and experience
            var genetics = monsterAI.GetGeneticsData();
            var intelligence = genetics.GetTraitValue(TraitType.Intelligence, 0.5f);
            var stress = monsterAI.GetStressLevel();
            var satisfaction = monsterAI.GetSatisfactionLevel();

            // Higher intelligence and satisfaction, lower stress = higher confidence
            var decisionConfidence = Mathf.Clamp01((intelligence * 0.6f) + (satisfaction * 0.3f) + ((1f - stress) * 0.1f));

            return new Laboratory.Chimera.ECS.BehaviorStateComponent
            {
                CurrentBehavior = ConvertAIBehaviorToECS(monsterAI.GetCurrentBehaviorType()),
                BehaviorIntensity = monsterAI.GetBehaviorIntensity(),
                Stress = stress,
                Satisfaction = satisfaction,
                DecisionConfidence = decisionConfidence, // Calculated based on intelligence and state
                BehaviorTimer = monsterAI.GetBehaviorTimer(),
                LastDecisionTime = Time.time
            };
        }

        /// <summary>
        /// Extension method to set trait values in GeneticProfile using trait type enum
        /// </summary>
        public static void SetTraitValue(this ChimeraGeneticProfile profile, Laboratory.Core.Enums.TraitType traitType, float value)
        {
            SetTraitValue(profile, traitType.ToString(), value);
        }

        /// <summary>
        /// Extension method to set trait values in GeneticProfile (string overload for compatibility)
        /// </summary>
        public static void SetTraitValue(this ChimeraGeneticProfile profile, string traitName, float value)
        {
            // Get current genes and find matching trait
            var genesList = profile.Genes.ToList();

            for (int i = 0; i < genesList.Count; i++)
            {
                var gene = genesList[i];
                if (gene.traitName.Equals(traitName, System.StringComparison.OrdinalIgnoreCase))
                {
                    // Update the gene's expressed value by modifying its alleles
                    var updatedGene = new Gene(
                        gene.geneId,
                        gene.traitName,
                        gene.traitType,
                        Allele.CreateDominant(gene.traitName, value),
                        Allele.CreateRecessive(gene.traitName, value * 0.8f)
                    );

                    genesList[i] = updatedGene;

                    // Use reflection to update the private genes array (this is the proper fix)
                    var genesField = typeof(ChimeraGeneticProfile).GetField("genes",
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    genesField?.SetValue(profile, genesList.ToArray());
                    return;
                }
            }

            // If trait not found, add new gene
            var newGene = CreateGeneFromTrait(traitName, value, Laboratory.Core.Enums.TraitType.Intelligence);
            genesList.Add(newGene);

            // Update the genes array
            var genesFieldNew = typeof(ChimeraGeneticProfile).GetField("genes",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            genesFieldNew?.SetValue(profile, genesList.ToArray());
        }

        /// <summary>
        /// Extension method to update AI behavior based on genetic data
        /// </summary>
        public static void UpdateBehaviorFromGenetics(this ChimeraMonsterAI monsterAI)
        {
            var genetics = monsterAI.GetGeneticsData();

            // Apply genetic modifiers to AI behavior
            var aggression = genetics.GetTraitValue(TraitType.Aggression, 0.5f);
            var sociability = genetics.GetTraitValue(TraitType.Sociability, 0.5f);
            var curiosity = genetics.GetTraitValue(TraitType.Curiosity, 0.5f);

            monsterAI.SetGeneticAggressionModifier(aggression);
            monsterAI.SetGeneticDetectionRangeModifier(curiosity * 1.5f); // Curious creatures detect more

            // Adjust behavior type based on genetics
            if (aggression > 0.7f)
            {
                monsterAI.SetBehaviorType(Laboratory.Chimera.AI.AIBehaviorType.Aggressive);
            }
            else if (sociability > 0.7f)
            {
                monsterAI.SetBehaviorType(Laboratory.Chimera.AI.AIBehaviorType.Social);
            }
            else if (curiosity > 0.7f)
            {
                monsterAI.SetBehaviorType(Laboratory.Chimera.AI.AIBehaviorType.Investigate);
            }
            else
            {
                monsterAI.SetBehaviorType(Laboratory.Chimera.AI.AIBehaviorType.Passive);
            }
        }

        /// <summary>
        /// Apply ECS genetics data to MonoBehaviour AI
        /// </summary>
        public static void ApplyECSGenetics(this ChimeraMonsterAI monsterAI, Laboratory.Chimera.ECS.ChimeraGeneticDataComponent ecsGenetics)
        {
            var genetics = monsterAI.GetGeneticsData();

            genetics.SetTraitValue("Aggression", ecsGenetics.Aggression);
            genetics.SetTraitValue("Sociability", ecsGenetics.Sociability);
            genetics.SetTraitValue("Curiosity", ecsGenetics.Curiosity);
            genetics.SetTraitValue("Intelligence", ecsGenetics.Intelligence);
            genetics.SetTraitValue("Size", ecsGenetics.Size);
            genetics.SetTraitValue("Speed", ecsGenetics.Speed);
            genetics.SetTraitValue("Stamina", ecsGenetics.Stamina);
            genetics.SetTraitValue("Fertility", ecsGenetics.Fertility);
            genetics.SetTraitValue("Dominance", ecsGenetics.Dominance);
            genetics.SetTraitValue("Metabolism", ecsGenetics.Metabolism);

            // Update MonoBehaviour behavior based on new genetics
            monsterAI.UpdateBehaviorFromGenetics();
        }

        /// <summary>
        /// Apply ECS behavior state to MonoBehaviour AI
        /// </summary>
        public static void ApplyECSBehaviorState(this ChimeraMonsterAI monsterAI, Laboratory.Chimera.ECS.BehaviorStateComponent ecsBehavior)
        {
            var monoBehaviorType = ConvertECSBehaviorToAI(ecsBehavior.CurrentBehavior);
            monsterAI.SetBehaviorType(monoBehaviorType);

            // Apply additional behavioral influences based on ECS state
            if (ecsBehavior.Stress > 0.7f)
            {
                // High stress should make creature more cautious
                monsterAI.SetBehaviorType(Laboratory.Chimera.AI.AIBehaviorType.Flee);
            }
            else if (ecsBehavior.Satisfaction > 0.8f && ecsBehavior.DecisionConfidence > 0.7f)
            {
                // High satisfaction and confidence should enable social behaviors
                if (monoBehaviorType == Laboratory.Chimera.AI.AIBehaviorType.Idle)
                {
                    monsterAI.SetBehaviorType(Laboratory.Chimera.AI.AIBehaviorType.Social);
                }
            }
        }

        /// <summary>
        /// Check if MonoBehaviour AI has ECS bridge component
        /// </summary>
        public static bool HasECSBridge(this ChimeraMonsterAI monsterAI)
        {
            return monsterAI.GetComponent<ECSBridgeComponent>() != null;
        }

        /// <summary>
        /// Get the ECS entity associated with this MonoBehaviour AI
        /// </summary>
        public static Entity GetECSEntity(this ChimeraMonsterAI monsterAI)
        {
            var bridge = monsterAI.GetComponent<ECSBridgeComponent>();
            return bridge != null ? bridge.linkedEntity : Entity.Null;
        }

        #endregion

        #region ChimeraAIManager Extensions

        /// <summary>
        /// Get all managed creatures as ECS-compatible data
        /// </summary>
        public static List<Laboratory.Chimera.ECS.ChimeraGeneticDataComponent> GetAllCreatureGeneticsECS(this ChimeraAIManager aiManager)
        {
            var genetics = new List<Laboratory.Chimera.ECS.ChimeraGeneticDataComponent>();
            var monsters = aiManager.GetManagedMonsters();

            foreach (var monster in monsters)
            {
                genetics.Add(monster.ToECSGeneticsComponent());
            }

            return genetics;
        }

        /// <summary>
        /// Apply unified configuration to AI manager
        /// </summary>
        public static void ApplyUnifiedConfiguration(this ChimeraAIManager aiManager, ChimeraUniverseConfiguration config)
        {
            if (config == null) return;

            // Apply pack behavior settings to all managed creatures
            var monsters = aiManager.GetManagedMonsters();
            foreach (var monster in monsters)
            {
                if (monster != null)
                {
                    // Configure detection range based on AI config
                    var detectionRange = config.Social.conflictDetectionRadius;
                    monster.SetGeneticDetectionRangeModifier(detectionRange / 20f); // Normalize to 0-1 range

                    // Apply pack behavior based on genetics
                    var genetics = monster.GetGeneticsData();
                    var sociability = genetics.GetTraitValue(TraitType.Sociability, 0.5f);

                    if (sociability > 0.6f)
                    {
                        monster.SetBehaviorType(Laboratory.Chimera.AI.AIBehaviorType.Social);
                    }
                }
            }

            // Apply performance constraints
            if (monsters.Count > config.Performance.maxBehaviorUpdatesPerFrame)
            {
                UnityEngine.Debug.LogWarning($"AI Manager has {monsters.Count} creatures but performance config limits to {config.Performance.maxBehaviorUpdatesPerFrame}");
            }
        }

        /// <summary>
        /// Sync AI manager with ECS behavior system
        /// </summary>
        public static void SyncWithECSBehaviorSystem(this ChimeraAIManager aiManager)
        {
            var monsters = aiManager.GetManagedMonsters();

            foreach (var monster in monsters)
            {
                if (monster.HasECSBridge())
                {
                    // Sync formation behavior with ECS social system
                    var ecsEntity = monster.GetECSEntity();
                    if (ecsEntity != Entity.Null)
                    {
                        SyncFormationWithECS(monster, ecsEntity);
                    }
                }
            }
        }

        private static void SyncFormationWithECS(ChimeraMonsterAI monster, Entity ecsEntity)
        {
            // Formation data sync with ECS social territory component
            var world = Unity.Entities.World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.EntityManager.Exists(ecsEntity))
                return;

            var entityManager = world.EntityManager;

            // Sync formation behavior based on genetics
            var genetics = monster.GetGeneticsData();
            var sociability = genetics.GetTraitValue(TraitType.Sociability, 0.5f);
            var dominance = genetics.GetTraitValue(TraitType.Dominance, 0.5f);

            // Update social territory component if it exists
            if (entityManager.HasComponent<SocialTerritoryComponent>(ecsEntity))
            {
                var socialComponent = entityManager.GetComponentData<SocialTerritoryComponent>(ecsEntity);

                socialComponent.PreferredPackSize = Mathf.RoundToInt(sociability * 8f + 2f); // 2-10 pack size based on sociability
                socialComponent.PackLoyalty = sociability;
                socialComponent.TerritoryRadius = 5f + (dominance * 15f); // 5-20 radius based on dominance

                entityManager.SetComponentData(ecsEntity, socialComponent);
            }
        }

        #endregion

        #region GeneticProfile Extensions

        /// <summary>
        /// Convert existing GeneticProfile to ECS ChimeraGeneticDataComponent
        /// </summary>
        public static Laboratory.Chimera.ECS.ChimeraGeneticDataComponent ToECSComponent(this Laboratory.Chimera.Genetics.GeneticProfile profile)
        {
            var component = new Laboratory.Chimera.ECS.ChimeraGeneticDataComponent();

            foreach (var gene in profile.Genes)
            {
                var traitValue = gene.GetExpressedValue();

                switch (gene.TraitName.ToLower())
                {
                    case "aggression":
                    case "territorial aggression":
                        component.Aggression = traitValue;
                        break;
                    case "sociability":
                    case "pack bonding":
                        component.Sociability = traitValue;
                        break;
                    case "curiosity":
                    case "exploration drive":
                        component.Curiosity = traitValue;
                        break;
                    case "intelligence":
                    case "problem solving":
                        component.Intelligence = traitValue;
                        break;
                    case "size":
                    case "body mass":
                        component.Size = traitValue;
                        break;
                    case "speed":
                    case "movement speed":
                        component.Speed = traitValue;
                        break;
                    case "stamina":
                    case "endurance":
                        component.Stamina = traitValue;
                        break;
                    case "fertility":
                    case "reproductive success":
                        component.Fertility = traitValue;
                        break;
                }
            }

            component.OverallFitness = 0.5f; // Default fitness value
            component.MutationRate = 0.02f; // Default mutation rate
            component.GeneticHash = CalculateHashFromProfile(profile);

            return component;
        }

        /// <summary>
        /// Create GeneticProfile from ECS ChimeraGeneticDataComponent
        /// </summary>
        public static Laboratory.Chimera.Genetics.GeneticProfile FromECSComponent(Laboratory.Chimera.ECS.ChimeraGeneticDataComponent ecsGenetics)
        {
            var genes = new List<Gene>
            {
                CreateGeneFromTrait("Aggression", ecsGenetics.Aggression, Laboratory.Core.Enums.TraitType.Aggression),
                CreateGeneFromTrait("Sociability", ecsGenetics.Sociability, Laboratory.Core.Enums.TraitType.Sociability),
                CreateGeneFromTrait("Curiosity", ecsGenetics.Curiosity, Laboratory.Core.Enums.TraitType.Curiosity),
                CreateGeneFromTrait("Intelligence", ecsGenetics.Intelligence, Laboratory.Core.Enums.TraitType.Intelligence),
                CreateGeneFromTrait("Size", ecsGenetics.Size, Laboratory.Core.Enums.TraitType.Size),
                CreateGeneFromTrait("Speed", ecsGenetics.Speed, Laboratory.Core.Enums.TraitType.Speed),
                CreateGeneFromTrait("Stamina", ecsGenetics.Stamina, Laboratory.Core.Enums.TraitType.Stamina),
                CreateGeneFromTrait("Fertility", ecsGenetics.Fertility, Laboratory.Core.Enums.TraitType.Fertility)
            };

            return new Laboratory.Chimera.Genetics.GeneticProfile(genes.ToArray(), 1);
        }

        private static Gene CreateGeneFromTrait(string traitName, float value, Laboratory.Core.Enums.TraitType traitType)
        {
            return new Gene(
                System.Guid.NewGuid().ToString(),
                traitName,
                traitType,
                Allele.CreateDominant(traitName, value),
                Allele.CreateRecessive(traitName, value * 0.8f)
            );
        }

        #endregion

        #region BreedingSystem Extensions

        /// <summary>
        /// Convert MonoBehaviour breeding result to ECS breeding component
        /// </summary>
        public static Laboratory.Chimera.ECS.BreedingComponent ToECSBreedingComponent(this BreedingResult breedingResult)
        {
            return new Laboratory.Chimera.ECS.BreedingComponent
            {
                Status = breedingResult.Success ? Laboratory.Chimera.ECS.BreedingStatus.Mating : Laboratory.Chimera.ECS.BreedingStatus.Cooldown,
                BreedingReadiness = breedingResult.CompatibilityScore,
                CourtshipProgress = breedingResult.Success ? 1f : 0f,
                PartnerCompatibility = breedingResult.CompatibilityScore,
                ExpectedOffspring = breedingResult.Success ? 1 : 0
            };
        }

        /// <summary>
        /// Bridge MonoBehaviour breeding with ECS breeding system
        /// </summary>
        public static void IntegrateWithECSBreeding(this BreedingSystem breedingSystem, ChimeraUniverseConfiguration config)
        {
            if (config?.Breeding == null) return;

            // Configure breeding system based on unified config
            var breedingSettings = config.Breeding;

            // Apply breeding parameters to all active breeding pairs
            var allResults = new List<BreedingResult>(); // Get from breeding system's active results

            foreach (var result in allResults)
            {
                if (result != null)
                {
                    // Apply config-based breeding constraints
                    var compatibilityThreshold = breedingSettings.hybridViabilityThreshold;
                    var gestationTime = breedingSettings.gestationTime;

                    // Adjust compatibility based on genetic diversity requirements
                    if (result.CompatibilityScore < compatibilityThreshold)
                    {
                        result.Success = false;
                        UnityEngine.Debug.Log($"Breeding blocked: Compatibility {result.CompatibilityScore:F2} below threshold {compatibilityThreshold:F2}");
                    }
                }
            }

            UnityEngine.Debug.Log($"ECS Breeding integration applied with compatibility threshold: {breedingSettings.hybridViabilityThreshold:F2}");
        }

        #endregion

        #region Configuration Extensions

        /// <summary>
        /// Get ECS-compatible biome data from existing biome config
        /// </summary>
        public static Laboratory.Chimera.ECS.BiomeComponent ToECSBiomeComponent(this ChimeraBiomeConfig biomeConfig, BiomeType biomeType)
        {
            var biomeData = biomeConfig.GetBiomeData(biomeType.ToString());

            // Calculate biome parameters based on type and configuration
            var temperature = CalculateBiomeTemperature(biomeType);
            var humidity = CalculateBiomeHumidity(biomeType);
            var radius = CalculateBiomeRadius(biomeType, biomeData.carryingCapacity);

            return new Laboratory.Chimera.ECS.BiomeComponent
            {
                BiomeType = biomeType,
                Center = Vector3.zero, // Will be set by world generation systems
                Radius = radius, // Calculated based on biome type and capacity
                Temperature = temperature, // Realistic temperature for biome type
                Humidity = humidity, // Realistic humidity for biome type
                ResourceDensity = biomeData.resourceAbundance,
                CarryingCapacity = biomeData.carryingCapacity
            };
        }

        /// <summary>
        /// Merge specialized configs with unified config
        /// </summary>
        public static ChimeraUniverseConfiguration MergeWithSpecializedConfigs(this ChimeraUniverseConfiguration unifiedConfig,
            GeneticTraitLibrary traitLibrary, ChimeraBiomeConfig biomeConfig, ChimeraSpeciesConfig speciesConfig)
        {
            // Create enhanced version of unified config with specialized data
            var enhancedConfig = Object.Instantiate(unifiedConfig);

            // Merge genetic settings
            if (traitLibrary != null)
            {
                enhancedConfig.Genetics.baseMutationRate = traitLibrary.GetRecommendedMutationRate();
            }

            // Merge biome settings
            if (biomeConfig != null)
            {
                for (int i = 0; i < enhancedConfig.Ecosystem.biomes.Length; i++)
                {
                    var biome = enhancedConfig.Ecosystem.biomes[i];
                    var biomeData = biomeConfig.GetBiomeData(biome.type.ToString());

                    biome.resourceAbundance = biomeData.resourceAbundance;
                    biome.carryingCapacity = biomeData.carryingCapacity;
                    enhancedConfig.Ecosystem.biomes[i] = biome;
                }
            }

            return enhancedConfig;
        }

        #endregion

        #region Calculation Helper Methods

        private static float CalculateOverallFitness(float aggression, float sociability, float intelligence,
                                                   float size, float speed, float stamina, float adaptability)
        {
            // Weighted average of traits that contribute to survival fitness
            var physicalFitness = (size * 0.2f + speed * 0.3f + stamina * 0.3f) * 0.4f;
            var mentalFitness = (intelligence * 0.6f + adaptability * 0.4f) * 0.3f;
            var socialFitness = (sociability * 0.7f + (1f - aggression) * 0.3f) * 0.3f;

            return Mathf.Clamp01(physicalFitness + mentalFitness + socialFitness);
        }

        private static float CalculateMutationRate(int generation, float fitness)
        {
            // Base mutation rate increases with generation, but decreases with fitness
            var baseMutationRate = 0.02f;
            var generationModifier = 1f + (generation * 0.005f); // Slight increase per generation
            var fitnessModifier = 2f - fitness; // Lower fitness = higher mutation rate

            return Mathf.Clamp(baseMutationRate * generationModifier * fitnessModifier, 0.005f, 0.1f);
        }

        private static BiomeType DetermineNativeBiome(float heatTolerance, float coldTolerance, float waterAffinity, float size)
        {
            // Determine biome based on environmental trait combinations
            if (waterAffinity > 0.7f)
                return BiomeType.Ocean;
            else if (heatTolerance > 0.8f && coldTolerance < 0.3f)
                return BiomeType.Desert;
            else if (coldTolerance > 0.8f && heatTolerance < 0.3f)
                return BiomeType.Arctic;
            else if (size > 1.3f)
                return BiomeType.Mountain; // Large creatures prefer mountains
            else if (heatTolerance > 0.6f && waterAffinity > 0.5f)
                return BiomeType.Swamp;
            else if (heatTolerance > 0.7f)
                return BiomeType.Volcanic;
            else
                return BiomeType.Forest; // Default temperate biome
        }

        private static float CalculateBiomeTemperature(BiomeType biomeType)
        {
            return biomeType switch
            {
                BiomeType.Arctic => -10f,
                BiomeType.Mountain => 5f,
                BiomeType.Forest => 18f,
                BiomeType.Grassland => 22f,
                BiomeType.Temperate => 20f,
                BiomeType.Swamp => 25f,
                BiomeType.Desert => 35f,
                BiomeType.Volcanic => 45f,
                BiomeType.Ocean => 15f,
                BiomeType.Underground => 12f,
                _ => 20f
            };
        }

        private static float CalculateBiomeHumidity(BiomeType biomeType)
        {
            return biomeType switch
            {
                BiomeType.Desert => 0.1f,
                BiomeType.Volcanic => 0.2f,
                BiomeType.Arctic => 0.3f,
                BiomeType.Mountain => 0.4f,
                BiomeType.Grassland => 0.5f,
                BiomeType.Temperate => 0.6f,
                BiomeType.Forest => 0.7f,
                BiomeType.Underground => 0.8f,
                BiomeType.Swamp => 0.9f,
                BiomeType.Ocean => 1.0f,
                _ => 0.5f
            };
        }

        private static float CalculateBiomeRadius(BiomeType biomeType, int carryingCapacity)
        {
            // Base radius varies by biome type, scaled by carrying capacity
            var baseRadius = biomeType switch
            {
                BiomeType.Ocean => 200f,
                BiomeType.Desert => 150f,
                BiomeType.Grassland => 100f,
                BiomeType.Forest => 80f,
                BiomeType.Mountain => 120f,
                BiomeType.Swamp => 60f,
                BiomeType.Arctic => 180f,
                BiomeType.Volcanic => 40f,
                BiomeType.Underground => 30f,
                _ => 75f
            };

            // Scale by carrying capacity (more capacity = larger area needed)
            var capacityModifier = Mathf.Sqrt(carryingCapacity / 100f);
            return baseRadius * capacityModifier;
        }

        #endregion

        #region Utility Conversion Methods

        private static BiomeType ConvertStringToBiomeType(string biomeString)
        {
            if (System.Enum.TryParse<BiomeType>(biomeString, true, out var biomeType))
                return biomeType;
            return BiomeType.Grassland;
        }

        private static CreatureBehaviorType ConvertAIBehaviorToECS(Laboratory.Chimera.AI.AIBehaviorType aiBehavior)
        {
            switch (aiBehavior)
            {
                case Laboratory.Chimera.AI.AIBehaviorType.Idle: return CreatureBehaviorType.Idle;
                case Laboratory.Chimera.AI.AIBehaviorType.Patrol: return CreatureBehaviorType.Exploring;
                case Laboratory.Chimera.AI.AIBehaviorType.Hunt: return CreatureBehaviorType.Hunting;
                case Laboratory.Chimera.AI.AIBehaviorType.Flee: return CreatureBehaviorType.Fleeing;
                case Laboratory.Chimera.AI.AIBehaviorType.Companion: return CreatureBehaviorType.Social;
                case Laboratory.Chimera.AI.AIBehaviorType.Territorial: return CreatureBehaviorType.Territorial;
                case Laboratory.Chimera.AI.AIBehaviorType.Foraging: return CreatureBehaviorType.Foraging;
                default: return CreatureBehaviorType.Idle;
            }
        }

        private static Laboratory.Chimera.AI.AIBehaviorType ConvertECSBehaviorToAI(CreatureBehaviorType ecsBehavior)
        {
            switch (ecsBehavior)
            {
                case CreatureBehaviorType.Idle: return Laboratory.Chimera.AI.AIBehaviorType.Idle;
                case CreatureBehaviorType.Exploring: return Laboratory.Chimera.AI.AIBehaviorType.Patrol;
                case CreatureBehaviorType.Hunting: return Laboratory.Chimera.AI.AIBehaviorType.Hunt;
                case CreatureBehaviorType.Fleeing: return Laboratory.Chimera.AI.AIBehaviorType.Flee;
                case CreatureBehaviorType.Social: return Laboratory.Chimera.AI.AIBehaviorType.Companion;
                case CreatureBehaviorType.Territorial: return Laboratory.Chimera.AI.AIBehaviorType.Territorial;
                case CreatureBehaviorType.Foraging: return Laboratory.Chimera.AI.AIBehaviorType.Foraging;
                default: return Laboratory.Chimera.AI.AIBehaviorType.Idle;
            }
        }

        private static uint CalculateHashFromProfile(Laboratory.Chimera.Genetics.GeneticProfile profile)
        {
            uint hash = 0;
            foreach (var gene in profile.Genes)
            {
                hash ^= (uint)(gene.GetExpressedValue() * 1000) + (uint)gene.TraitName.GetHashCode();
            }
            return hash;
        }

        #endregion
    }

    /// <summary>
    /// Component to attach to MonoBehaviour creatures that have ECS bridges
    /// </summary>
    [System.Serializable]
    public class ECSBridgeComponent : MonoBehaviour
    {
        public Entity linkedEntity = Entity.Null;
        public bool syncEnabled = true;
        public float lastSyncTime = 0f;

        public bool IsLinked => linkedEntity != Entity.Null;

        public void LinkToEntity(Entity entity)
        {
            linkedEntity = entity;
            lastSyncTime = Time.time;
        }

        public void UnlinkEntity()
        {
            linkedEntity = Entity.Null;
        }
    }
}