using UnityEngine;
using Unity.Entities;
using Laboratory.Chimera.AI;
using Laboratory.Core.ECS.Components;
using Laboratory.Chimera.Configuration;
using Laboratory.Chimera.Genetics;
using Laboratory.Chimera.Breeding;
using System.Collections.Generic;

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
        public static GeneticDataComponent ToECSGeneticsComponent(this ChimeraMonsterAI monsterAI)
        {
            var genetics = monsterAI.GetGeneticsData();

            return new GeneticDataComponent
            {
                Aggression = genetics.GetTraitValue("Aggression", 0.5f),
                Sociability = genetics.GetTraitValue("Sociability", 0.5f),
                Curiosity = genetics.GetTraitValue("Curiosity", 0.5f),
                Intelligence = genetics.GetTraitValue("Intelligence", 0.5f),
                Size = genetics.GetTraitValue("Size", 1.0f),
                Speed = genetics.GetTraitValue("Speed", 1.0f),
                Stamina = genetics.GetTraitValue("Stamina", 1.0f),
                Fertility = genetics.GetTraitValue("Fertility", 0.7f),
                Dominance = genetics.GetTraitValue("Dominance", 0.5f),
                Metabolism = genetics.GetTraitValue("Metabolism", 1.0f),
                HeatTolerance = genetics.GetTraitValue("HeatTolerance", 0.5f),
                ColdTolerance = genetics.GetTraitValue("ColdTolerance", 0.5f),
                WaterAffinity = genetics.GetTraitValue("WaterAffinity", 0.5f),
                Adaptability = genetics.GetTraitValue("Adaptability", 0.6f),
                Camouflage = genetics.GetTraitValue("Camouflage", 0.5f),
                Caution = genetics.GetTraitValue("Caution", 0.5f),
                OverallFitness = genetics.CalculateFitness(),
                MutationRate = genetics.GetMutationRate(),
                NativeBiome = ConvertStringToBiomeType(genetics.GetNativeBiome())
            };
        }

        /// <summary>
        /// Get ECS-compatible behavior state from MonoBehaviour AI
        /// </summary>
        public static BehaviorStateComponent ToECSBehaviorState(this ChimeraMonsterAI monsterAI)
        {
            return new BehaviorStateComponent
            {
                CurrentBehavior = ConvertAIBehaviorToECS(monsterAI.GetCurrentBehaviorType()),
                BehaviorIntensity = monsterAI.GetBehaviorIntensity(),
                Stress = monsterAI.GetStressLevel(),
                Satisfaction = monsterAI.GetSatisfactionLevel(),
                DecisionConfidence = 0.7f,
                BehaviorTimer = monsterAI.GetBehaviorTimer(),
                LastDecisionTime = Time.time
            };
        }

        /// <summary>
        /// Apply ECS genetics data to MonoBehaviour AI
        /// </summary>
        public static void ApplyECSGenetics(this ChimeraMonsterAI monsterAI, GeneticDataComponent ecsGenetics)
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
        public static void ApplyECSBehaviorState(this ChimeraMonsterAI monsterAI, BehaviorStateComponent ecsBehavior)
        {
            var monoBehaviorType = ConvertECSBehaviorToAI(ecsBehavior.CurrentBehavior);
            monsterAI.SetBehaviorType(monoBehaviorType);
            monsterAI.SetBehaviorIntensity(ecsBehavior.BehaviorIntensity);
            monsterAI.SetStressLevel(ecsBehavior.Stress);
            monsterAI.SetSatisfactionLevel(ecsBehavior.Satisfaction);
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
        public static List<GeneticDataComponent> GetAllCreatureGeneticsECS(this ChimeraAIManager aiManager)
        {
            var genetics = new List<GeneticDataComponent>();
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
            // Apply pack behavior settings
            aiManager.SetPackCohesionRadius(config.Social.packFormationRadius);
            aiManager.SetFormationSpacing(config.Social.packFormationRadius * 0.3f);

            // Apply performance settings
            aiManager.SetMaxManagedCreatures(config.Performance.maxBehaviorUpdatesPerFrame / 10);
            aiManager.SetUpdateInterval(config.Behavior.decisionUpdateInterval);
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
            // This would sync formation data with ECS social territory component
            var world = World.DefaultGameObjectInjectionWorld;
            if (world?.EntityManager != null && world.EntityManager.HasComponent<SocialTerritoryComponent>(ecsEntity))
            {
                var socialComponent = world.EntityManager.GetComponentData<SocialTerritoryComponent>(ecsEntity);

                // Apply formation preferences to ECS social data
                socialComponent.PreferredPackSize = monster.GetPreferredPackSize();
                socialComponent.PackLoyalty = monster.GetPackLoyalty();

                world.EntityManager.SetComponentData(ecsEntity, socialComponent);
            }
        }

        #endregion

        #region GeneticProfile Extensions

        /// <summary>
        /// Convert existing GeneticProfile to ECS GeneticDataComponent
        /// </summary>
        public static GeneticDataComponent ToECSComponent(this GeneticProfile profile)
        {
            var component = new GeneticDataComponent();

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

            component.OverallFitness = profile.CalculateFitness();
            component.MutationRate = 0.02f; // Default mutation rate
            component.GeneticHash = CalculateHashFromProfile(profile);

            return component;
        }

        /// <summary>
        /// Create GeneticProfile from ECS GeneticDataComponent
        /// </summary>
        public static GeneticProfile FromECSComponent(GeneticDataComponent ecsGenetics)
        {
            var genes = new List<Gene>
            {
                CreateGeneFromTrait("Aggression", ecsGenetics.Aggression),
                CreateGeneFromTrait("Sociability", ecsGenetics.Sociability),
                CreateGeneFromTrait("Curiosity", ecsGenetics.Curiosity),
                CreateGeneFromTrait("Intelligence", ecsGenetics.Intelligence),
                CreateGeneFromTrait("Size", ecsGenetics.Size),
                CreateGeneFromTrait("Speed", ecsGenetics.Speed),
                CreateGeneFromTrait("Stamina", ecsGenetics.Stamina),
                CreateGeneFromTrait("Fertility", ecsGenetics.Fertility)
            };

            return new GeneticProfile(genes.ToArray(), 1);
        }

        private static Gene CreateGeneFromTrait(string traitName, float value)
        {
            return new Gene(
                System.Guid.NewGuid().ToString(),
                traitName,
                TraitType.Behavioral,
                Allele.CreateDominant(traitName, value),
                Allele.CreateRecessive(traitName, value * 0.8f)
            );
        }

        #endregion

        #region BreedingSystem Extensions

        /// <summary>
        /// Convert MonoBehaviour breeding result to ECS breeding component
        /// </summary>
        public static BreedingComponent ToECSBreedingComponent(this BreedingResult breedingResult)
        {
            return new BreedingComponent
            {
                Status = breedingResult.Success ? BreedingStatus.Mating : BreedingStatus.Cooldown,
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
            // This method would set up event handlers to sync breeding between systems
            // Implementation would depend on the existing event system
        }

        #endregion

        #region Configuration Extensions

        /// <summary>
        /// Get ECS-compatible biome data from existing biome config
        /// </summary>
        public static BiomeComponent ToECSBiomeComponent(this ChimeraBiomeConfig biomeConfig, BiomeType biomeType)
        {
            var biomeData = biomeConfig.GetBiomeData(biomeType.ToString());

            return new BiomeComponent
            {
                BiomeType = biomeType,
                Center = Vector3.zero, // Would be set based on world position
                Radius = biomeData?.territoryRadius ?? 50f,
                Temperature = biomeData?.averageTemperature ?? 20f,
                Humidity = biomeData?.humidity ?? 0.5f,
                ResourceDensity = biomeData?.resourceDensity ?? 0.7f,
                CarryingCapacity = biomeData?.maxCreatures ?? 20
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
                foreach (var biome in enhancedConfig.Ecosystem.biomes)
                {
                    var biomeData = biomeConfig.GetBiomeData(biome.type.ToString());
                    if (biomeData != null)
                    {
                        biome.resourceAbundance = biomeData.resourceDensity;
                        biome.carryingCapacity = biomeData.maxCreatures;
                    }
                }
            }

            return enhancedConfig;
        }

        #endregion

        #region Utility Conversion Methods

        private static BiomeType ConvertStringToBiomeType(string biomeString)
        {
            if (System.Enum.TryParse<BiomeType>(biomeString, true, out var biomeType))
                return biomeType;
            return BiomeType.Grassland;
        }

        private static CreatureBehaviorType ConvertAIBehaviorToECS(AIBehaviorType aiBehavior)
        {
            switch (aiBehavior)
            {
                case AIBehaviorType.Idle: return CreatureBehaviorType.Idle;
                case AIBehaviorType.Patrol: return CreatureBehaviorType.Exploring;
                case AIBehaviorType.Hunt: return CreatureBehaviorType.Hunting;
                case AIBehaviorType.Flee: return CreatureBehaviorType.Fleeing;
                case AIBehaviorType.Companion: return CreatureBehaviorType.Social;
                case AIBehaviorType.Territorial: return CreatureBehaviorType.Territorial;
                case AIBehaviorType.Foraging: return CreatureBehaviorType.Foraging;
                default: return CreatureBehaviorType.Idle;
            }
        }

        private static AIBehaviorType ConvertECSBehaviorToAI(CreatureBehaviorType ecsBehavior)
        {
            switch (ecsBehavior)
            {
                case CreatureBehaviorType.Idle: return AIBehaviorType.Idle;
                case CreatureBehaviorType.Exploring: return AIBehaviorType.Patrol;
                case CreatureBehaviorType.Hunting: return AIBehaviorType.Hunt;
                case CreatureBehaviorType.Fleeing: return AIBehaviorType.Flee;
                case CreatureBehaviorType.Social: return AIBehaviorType.Companion;
                case CreatureBehaviorType.Territorial: return AIBehaviorType.Territorial;
                case CreatureBehaviorType.Foraging: return AIBehaviorType.Foraging;
                default: return AIBehaviorType.Idle;
            }
        }

        private static uint CalculateHashFromProfile(GeneticProfile profile)
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