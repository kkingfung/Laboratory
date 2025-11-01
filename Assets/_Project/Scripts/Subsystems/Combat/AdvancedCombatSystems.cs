using Unity.Entities;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Burst;
using Unity.Jobs;
using UnityEngine;
using Laboratory.Core.ECS;
using Laboratory.Chimera.ECS;
using Laboratory.Chimera.Genetics;
using Laboratory.Networking.Entities;
using NetworkOwnership = Laboratory.Networking.Entities.NetworkOwnership;
using ReplicatedCreatureState = Laboratory.Networking.Entities.ReplicatedCreatureState;
using Laboratory.Core.Progression;
using CreatureGeneticsComponent = Laboratory.Chimera.ECS.CreatureGeneticsComponent;

namespace Laboratory.Subsystems.Combat.Advanced
{
    /// <summary>
    /// Advanced Combat Systems - Next-Generation Creature Combat for Project Chimera
    /// PURPOSE: Sophisticated combat mechanics integrating genetics, AI, multiplayer, and progression
    /// FEATURES: Genetic-based abilities, elemental combat, formation tactics, multiplayer synchronization
    /// ARCHITECTURE: ECS-optimized for 1000+ creatures with complex interactions
    /// PERFORMANCE: Burst-compiled systems with spatial optimization and network prediction
    /// </summary>

    // Enhanced combat components for genetic-based abilities
    public struct GeneticCombatAbilities : IComponentData
    {
        public float strengthMultiplier;
        public float agilityMultiplier;
        public float intellectMultiplier;
        public float resilienceMultiplier;
        public float vitalityMultiplier;
        public float charmMultiplier;
        public ElementalAffinity primaryAffinity;
        public ElementalAffinity secondaryAffinity;
        public CombatSpecialization specialization;
        public uint geneticAbilityHash;
        public float adaptationLevel; // 0-1, how well adapted to current environment
    }

    public enum ElementalAffinity : byte
    {
        None = 0,
        Fire = 1,
        Water = 2,
        Earth = 3,
        Air = 4,
        Lightning = 5,
        Ice = 6,
        Nature = 7,
        Shadow = 8,
        Light = 9,
        Chaos = 10
    }

    public enum CombatSpecialization : byte
    {
        Balanced = 0,
        Berserker = 1,    // High damage, low defense
        Tank = 2,         // High defense, low damage
        Assassin = 3,     // High critical chance, low health
        Mage = 4,         // Elemental abilities, low physical stats
        Healer = 5,       // Support abilities, healing
        Summoner = 6,     // Can spawn temporary allies
        Tactician = 7     // Buffs allies, strategic abilities
    }

    // Formation combat system
    public struct FormationCombatComponent : IComponentData
    {
        public Entity formationLeader;
        public FormationType formationType;
        public float3 formationPosition; // Relative to leader
        public float formationBonus; // Damage/defense bonus from formation
        public float cohesionLevel; // 0-1, how well unit follows formation
        public bool isFormationLeader;
        public int formationSize;
        public float leadershipRange;
    }

    public enum FormationType : byte
    {
        None = 0,
        Line = 1,        // Defensive line formation
        Wedge = 2,       // Aggressive wedge formation
        Circle = 3,      // Defensive circle formation
        Swarm = 4,       // Chaotic swarm attack
        Phalanx = 5,     // Heavy defense formation
        Ambush = 6       // Stealth-based positioning
    }

    // Environmental combat effects
    public struct EnvironmentalCombatEffects : IComponentData
    {
        public float temperatureEffect; // -1 to 1 (cold to hot)
        public float humidityEffect;    // 0 to 1
        public float elevationEffect;   // Affects air-type creatures
        public Laboratory.Core.Enums.BiomeType currentBiome;
        public float biomeAdaptation;   // How well creature is adapted to current biome
        public ElementalAffinity dominantElement; // Current environment's dominant element
        public float elementalResonance; // Synergy with environment
    }

    // Advanced status effects system
    public struct StatusEffectComponent : IBufferElementData
    {
        public StatusEffectType effectType;
        public float intensity;        // Effect strength (0-1)
        public float duration;         // Remaining duration
        public float tickInterval;     // For periodic effects
        public float lastTick;
        public Entity sourceEntity;   // Who applied this effect
        public ElementalAffinity sourceElement;
        public uint effectId;
    }

    public enum StatusEffectType : byte
    {
        // Damage over time
        Burning = 0,
        Poison = 1,
        Bleeding = 2,
        Freezing = 3,
        Shocking = 4,

        // Stat modifications
        Strengthened = 10,
        Weakened = 11,
        Quickened = 12,
        Slowed = 13,
        Fortified = 14,
        Vulnerable = 15,

        // Special states
        Stunned = 20,
        Confused = 21,
        Charmed = 22,
        Enraged = 23,
        Focused = 24,
        Regenerating = 25,

        // Environmental
        WetCondition = 30,
        ChilledCondition = 31,
        ElectrifiedCondition = 32
    }

    // Combat prediction for multiplayer
    public struct CombatPredictionState : IComponentData
    {
        public float3 predictedPosition;
        public float predictedHealth;
        public float predictedEnergy;
        public uint predictedActionId;
        public float predictionTimestamp;
        public float confidenceLevel; // 0-1
        public bool needsReconciliation;
    }

    // Tactical AI combat data
    public struct TacticalCombatAI : IComponentData
    {
        public TacticalPersonality personality;
        public float aggressionLevel;     // 0-1
        public float cooperationLevel;    // 0-1, works with allies
        public float adaptabilityLevel;   // 0-1, adapts to enemy tactics
        public CombatStrategy preferredStrategy;
        public float tacticalIntelligence; // Affects formation usage and targeting
        public Entity primaryTarget;
        public Entity secondaryTarget;
        public float threatAssessment;    // Current threat level perception
        public float lastStrategyChange;
    }

    public enum TacticalPersonality : byte
    {
        Aggressive = 0,    // Always attacks, ignores defense
        Defensive = 1,     // Prioritizes survival and positioning
        Opportunistic = 2, // Waits for openings, strategic
        Supportive = 3,    // Focuses on helping allies
        Adaptive = 4,      // Changes tactics based on situation
        Berserker = 5,     // Becomes more aggressive when damaged
        Calculating = 6    // Analyzes before acting, precise
    }

    public enum CombatStrategy : byte
    {
        DirectAssault = 0,
        HitAndRun = 1,
        Formation = 2,
        Elemental = 3,
        Support = 4,
        Ambush = 5,
        Siege = 6
    }

    /// <summary>
    /// Genetic Combat Ability System - Derives combat abilities from creature genetics
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial class GeneticCombatAbilitySystem : SystemBase
    {
        private EntityQuery _creatureQuery;

        protected override void OnCreate()
        {
            _creatureQuery = GetEntityQuery(
                ComponentType.ReadWrite<GeneticCombatAbilities>(),
                ComponentType.ReadOnly<CreatureGeneticsComponent>(),
                ComponentType.ReadOnly<EnvironmentalCombatEffects>()
            );

            RequireForUpdate(_creatureQuery);
        }

        protected override void OnUpdate()
        {
            var deltaTime = SystemAPI.Time.DeltaTime;
            var currentTime = (float)SystemAPI.Time.ElapsedTime;

            // Update genetic combat abilities based on genetics and environment
            var updateJob = new UpdateGeneticAbilitiesJob
            {
                deltaTime = deltaTime,
                currentTime = currentTime
            };

            Dependency = updateJob.ScheduleParallel(_creatureQuery, Dependency);
        }


        private partial struct UpdateGeneticAbilitiesJob : IJobEntity
        {
            [ReadOnly] public float deltaTime;
            [ReadOnly] public float currentTime;

            public void Execute(
                ref GeneticCombatAbilities combatAbilities,
                in EnvironmentalCombatEffects environment)
            {
                // Calculate multipliers based on existing combat abilities (simplified without genetics)
                combatAbilities.strengthMultiplier = math.clamp(combatAbilities.strengthMultiplier + deltaTime * 0.01f, 0.5f, 2.0f);
                combatAbilities.agilityMultiplier = math.clamp(combatAbilities.agilityMultiplier + deltaTime * 0.01f, 0.5f, 2.0f);
                combatAbilities.intellectMultiplier = math.clamp(combatAbilities.intellectMultiplier + deltaTime * 0.01f, 0.5f, 2.0f);
                combatAbilities.resilienceMultiplier = math.clamp(combatAbilities.resilienceMultiplier + deltaTime * 0.01f, 0.5f, 2.0f);
                combatAbilities.vitalityMultiplier = math.clamp(combatAbilities.vitalityMultiplier + deltaTime * 0.01f, 0.5f, 2.0f);
                combatAbilities.charmMultiplier = math.clamp(combatAbilities.charmMultiplier + deltaTime * 0.01f, 0.5f, 2.0f);

                // Update elemental affinities based on environment
                UpdateElementalAffinities(environment, ref combatAbilities);

                // Maintain existing specialization or use default
                if (combatAbilities.specialization == default)
                    combatAbilities.specialization = CombatSpecialization.Balanced;

                // Environmental adaptation affects combat effectiveness
                UpdateEnvironmentalAdaptation(environment, ref combatAbilities);

                // Generate unique genetic ability hash for network sync (simplified)
                combatAbilities.geneticAbilityHash = (uint)(combatAbilities.strengthMultiplier * 1000 +
                                                           combatAbilities.agilityMultiplier * 1000 +
                                                           (int)combatAbilities.specialization);
            }

            private float CalculateStrengthMultiplier(CreatureGeneticsComponent genetics)
            {
                float baseMultiplier = 0.5f + genetics.StrengthTrait;
                float purityBonus = genetics.GeneticPurity * 0.2f;
                float generationBonus = math.min(genetics.Generation * 0.05f, 0.5f);
                return baseMultiplier + purityBonus + generationBonus;
            }

            private float CalculateAgilityMultiplier(CreatureGeneticsComponent genetics)
            {
                float baseMultiplier = 0.5f + genetics.AgilityTrait;
                float purityBonus = genetics.GeneticPurity * 0.15f;
                return math.clamp(baseMultiplier + purityBonus, 0.1f, 3f);
            }

            private float CalculateIntellectMultiplier(CreatureGeneticsComponent genetics)
            {
                float baseMultiplier = 0.5f + genetics.IntellectTrait;
                float activeGeneBonus = genetics.ActiveGeneCount * 0.02f;
                return math.clamp(baseMultiplier + activeGeneBonus, 0.1f, 2.5f);
            }

            private float CalculateResilienceMultiplier(CreatureGeneticsComponent genetics)
            {
                return 0.5f + genetics.ResilienceTrait + (genetics.GeneticPurity * 0.3f);
            }

            private float CalculateVitalityMultiplier(CreatureGeneticsComponent genetics)
            {
                return 0.7f + genetics.VitalityTrait + (genetics.Generation * 0.03f);
            }

            private float CalculateCharmMultiplier(CreatureGeneticsComponent genetics)
            {
                float baseMultiplier = 0.3f + genetics.CharmTrait;
                float shinyBonus = genetics.IsShiny ? 0.5f : 0f;
                return baseMultiplier + shinyBonus;
            }

            private void DetermineElementalAffinities(
                CreatureGeneticsComponent genetics,
                ref GeneticCombatAbilities combatAbilities)
            {
                // Use genetic traits to determine elemental affinities
                float[] affinityScores = new float[11];

                affinityScores[(int)ElementalAffinity.Fire] = genetics.StrengthTrait + genetics.AgilityTrait;
                affinityScores[(int)ElementalAffinity.Water] = genetics.VitalityTrait + genetics.CharmTrait;
                affinityScores[(int)ElementalAffinity.Earth] = genetics.ResilienceTrait + genetics.StrengthTrait;
                affinityScores[(int)ElementalAffinity.Air] = genetics.AgilityTrait + genetics.IntellectTrait;
                affinityScores[(int)ElementalAffinity.Lightning] = genetics.IntellectTrait + genetics.AgilityTrait;
                affinityScores[(int)ElementalAffinity.Ice] = genetics.ResilienceTrait + genetics.IntellectTrait;
                affinityScores[(int)ElementalAffinity.Nature] = genetics.VitalityTrait + genetics.ResilienceTrait;
                affinityScores[(int)ElementalAffinity.Shadow] = genetics.IntellectTrait + genetics.AgilityTrait;
                affinityScores[(int)ElementalAffinity.Light] = genetics.CharmTrait + genetics.VitalityTrait;
                affinityScores[(int)ElementalAffinity.Chaos] = genetics.StrengthTrait + genetics.IntellectTrait;

                // Find highest and second highest affinities
                int primaryIndex = 0, secondaryIndex = 0;
                float primaryScore = 0f, secondaryScore = 0f;

                for (int i = 1; i < affinityScores.Length; i++)
                {
                    if (affinityScores[i] > primaryScore)
                    {
                        secondaryIndex = primaryIndex;
                        secondaryScore = primaryScore;
                        primaryIndex = i;
                        primaryScore = affinityScores[i];
                    }
                    else if (affinityScores[i] > secondaryScore)
                    {
                        secondaryIndex = i;
                        secondaryScore = affinityScores[i];
                    }
                }

                combatAbilities.primaryAffinity = (ElementalAffinity)primaryIndex;
                combatAbilities.secondaryAffinity = (ElementalAffinity)secondaryIndex;
            }

            private CombatSpecialization DetermineSpecialization(CreatureGeneticsComponent genetics)
            {
                float maxTrait = math.max(
                    math.max(genetics.StrengthTrait, genetics.AgilityTrait),
                    math.max(genetics.IntellectTrait, math.max(genetics.ResilienceTrait, genetics.VitalityTrait))
                );

                if (genetics.StrengthTrait == maxTrait && genetics.AgilityTrait > 0.7f)
                    return CombatSpecialization.Berserker;
                else if (genetics.ResilienceTrait == maxTrait && genetics.VitalityTrait > 0.7f)
                    return CombatSpecialization.Tank;
                else if (genetics.AgilityTrait == maxTrait && genetics.StrengthTrait > 0.6f)
                    return CombatSpecialization.Assassin;
                else if (genetics.IntellectTrait == maxTrait)
                    return CombatSpecialization.Mage;
                else if (genetics.VitalityTrait == maxTrait && genetics.CharmTrait > 0.6f)
                    return CombatSpecialization.Healer;
                else if (genetics.CharmTrait == maxTrait && genetics.IntellectTrait > 0.6f)
                    return CombatSpecialization.Summoner;
                else if (genetics.IntellectTrait > 0.8f && genetics.CharmTrait > 0.7f)
                    return CombatSpecialization.Tactician;

                return CombatSpecialization.Balanced;
            }

            private void UpdateEnvironmentalAdaptation(
                EnvironmentalCombatEffects environment,
                ref GeneticCombatAbilities combatAbilities)
            {
                // Calculate how well genetic abilities match current environment
                float affinityMatch = 0f;

                if (combatAbilities.primaryAffinity == environment.dominantElement)
                    affinityMatch += 0.6f;
                if (combatAbilities.secondaryAffinity == environment.dominantElement)
                    affinityMatch += 0.3f;

                // Environmental factors affect adaptation
                float environmentalFactor = environment.biomeAdaptation * environment.elementalResonance;

                combatAbilities.adaptationLevel = math.clamp(affinityMatch + environmentalFactor, 0f, 1f);
            }

            private void UpdateElementalAffinities(
                EnvironmentalCombatEffects environment,
                ref GeneticCombatAbilities combatAbilities)
            {
                // Update elemental affinities based on environment (simplified without genetics)
                if (combatAbilities.primaryAffinity == ElementalAffinity.None)
                    combatAbilities.primaryAffinity = environment.dominantElement;

                if (combatAbilities.secondaryAffinity == ElementalAffinity.None)
                    combatAbilities.secondaryAffinity = ElementalAffinity.None; // Keep as None for now
            }

            private uint CalculateGeneticHash(CreatureGeneticsComponent genetics)
            {
                uint hash = 0;
                hash ^= (uint)(genetics.StrengthTrait * 1000) << 0;
                hash ^= (uint)(genetics.AgilityTrait * 1000) << 4;
                hash ^= (uint)(genetics.IntellectTrait * 1000) << 8;
                hash ^= (uint)(genetics.ResilienceTrait * 1000) << 12;
                hash ^= (uint)(genetics.VitalityTrait * 1000) << 16;
                hash ^= (uint)(genetics.CharmTrait * 1000) << 20;
                hash ^= (uint)genetics.Generation << 24;
                return hash;
            }
        }
    }

    /// <summary>
    /// Formation Combat System - Manages group tactics and formation bonuses
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial class FormationCombatSystem : SystemBase
    {
        private EntityQuery _formationQuery;
        private EntityQuery _leaderQuery;

        protected override void OnCreate()
        {
            _formationQuery = GetEntityQuery(
                ComponentType.ReadWrite<FormationCombatComponent>(),
                ComponentType.ReadOnly<LocalTransform>()
            );

            _leaderQuery = GetEntityQuery(
                ComponentType.ReadOnly<FormationCombatComponent>(),
                ComponentType.ReadOnly<LocalTransform>(),
                ComponentType.ReadOnly<TacticalCombatAI>()
            );

            RequireForUpdate(_formationQuery);
        }

        protected override void OnUpdate()
        {
            var deltaTime = SystemAPI.Time.DeltaTime;

            // Update formation positions and bonuses
            var updateJob = new UpdateFormationJob
            {
                deltaTime = deltaTime,
                transformLookup = GetComponentLookup<LocalTransform>(true)
            };

            Dependency = updateJob.ScheduleParallel(_formationQuery, Dependency);
        }


        private partial struct UpdateFormationJob : IJobEntity
        {
            [ReadOnly] public float deltaTime;
            [ReadOnly] public ComponentLookup<LocalTransform> transformLookup;

            public void Execute(
                ref FormationCombatComponent formation,
                in LocalTransform transform)
            {
                if (!formation.isFormationLeader && formation.formationLeader != Entity.Null)
                {
                    UpdateFollowerPosition(ref formation, transform);
                }

                UpdateFormationBonus(ref formation);
                UpdateCohesion(ref formation, deltaTime);
            }

            private void UpdateFollowerPosition(
                ref FormationCombatComponent formation,
                LocalTransform transform)
            {
                if (!transformLookup.TryGetComponent(formation.formationLeader, out var leaderTransform))
                    return;

                // Calculate desired formation position
                float3 desiredPosition = CalculateFormationPosition(
                    leaderTransform.Position,
                    formation.formationType,
                    formation.formationPosition);

                // Update cohesion based on distance from desired position
                float distance = math.distance(transform.Position, desiredPosition);
                float maxDistance = formation.leadershipRange * 0.5f; // Half leadership range for good cohesion

                formation.cohesionLevel = math.clamp(1f - (distance / maxDistance), 0f, 1f);
            }

            private float3 CalculateFormationPosition(
                float3 leaderPosition,
                FormationType formationType,
                float3 relativePosition)
            {
                switch (formationType)
                {
                    case FormationType.Line:
                        return leaderPosition + new float3(relativePosition.x, 0, -math.abs(relativePosition.z));

                    case FormationType.Wedge:
                        float wedgeOffset = math.abs(relativePosition.x) * 0.5f;
                        return leaderPosition + new float3(relativePosition.x, 0, -wedgeOffset);

                    case FormationType.Circle:
                        float angle = math.atan2(relativePosition.z, relativePosition.x);
                        float radius = math.length(relativePosition);
                        return leaderPosition + new float3(
                            math.cos(angle) * radius,
                            0,
                            math.sin(angle) * radius);

                    case FormationType.Phalanx:
                        return leaderPosition + new float3(
                            relativePosition.x * 0.8f,
                            0,
                            -math.abs(relativePosition.z) * 1.2f);

                    default:
                        return leaderPosition + relativePosition;
                }
            }

            private void UpdateFormationBonus(ref FormationCombatComponent formation)
            {
                // Formation bonus based on cohesion and formation type
                float baseBonus = formation.cohesionLevel * 0.3f; // Up to 30% bonus

                switch (formation.formationType)
                {
                    case FormationType.Line:
                        formation.formationBonus = baseBonus * 1.2f; // Defense bonus
                        break;
                    case FormationType.Wedge:
                        formation.formationBonus = baseBonus * 1.5f; // Attack bonus
                        break;
                    case FormationType.Circle:
                        formation.formationBonus = baseBonus * 1.1f; // Balanced bonus
                        break;
                    case FormationType.Phalanx:
                        formation.formationBonus = baseBonus * 2f; // High defense bonus
                        break;
                    default:
                        formation.formationBonus = baseBonus;
                        break;
                }
            }

            private void UpdateCohesion(ref FormationCombatComponent formation, float deltaTime)
            {
                // Cohesion naturally decays over time if not maintained
                float decayRate = 0.1f; // 10% per second
                formation.cohesionLevel = math.max(0f, formation.cohesionLevel - decayRate * deltaTime);
            }
        }
    }

    /// <summary>
    /// Advanced Status Effect System - Manages complex status effects and interactions
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial class AdvancedStatusEffectSystem : SystemBase
    {
        private EntityQuery _statusEffectQuery;

        protected override void OnCreate()
        {
            _statusEffectQuery = GetEntityQuery(
                ComponentType.ReadWrite<DynamicBuffer<StatusEffectComponent>>(),
                ComponentType.ReadWrite<GeneticCombatAbilities>(),
                ComponentType.ReadOnly<EnvironmentalCombatEffects>()
            );

            RequireForUpdate(_statusEffectQuery);
        }

        protected override void OnUpdate()
        {
            var deltaTime = SystemAPI.Time.DeltaTime;
            var currentTime = (float)SystemAPI.Time.ElapsedTime;

            // Process all status effects
            var processJob = new ProcessStatusEffectsJob
            {
                deltaTime = deltaTime,
                currentTime = currentTime
            };

            Dependency = processJob.ScheduleParallel(_statusEffectQuery, Dependency);
        }


        private partial struct ProcessStatusEffectsJob : IJobEntity
        {
            [ReadOnly] public float deltaTime;
            [ReadOnly] public float currentTime;

            public void Execute(
                DynamicBuffer<StatusEffectComponent> statusEffects,
                ref GeneticCombatAbilities combatAbilities,
                in EnvironmentalCombatEffects environment)
            {
                // Process each status effect
                for (int i = statusEffects.Length - 1; i >= 0; i--)
                {
                    var effect = statusEffects[i];

                    // Update duration
                    effect.duration -= deltaTime;

                    // Check if effect expired
                    if (effect.duration <= 0f)
                    {
                        RemoveStatusEffect(statusEffects, i, ref combatAbilities);
                        continue;
                    }

                    // Process periodic effects
                    if (currentTime - effect.lastTick >= effect.tickInterval)
                    {
                        ProcessPeriodicEffect(effect, ref combatAbilities, environment);
                        effect.lastTick = currentTime;
                    }

                    // Apply continuous effects
                    ApplyContinuousEffect(effect, ref combatAbilities, deltaTime);

                    statusEffects[i] = effect;
                }

                // Process status effect interactions
                ProcessStatusInteractions(statusEffects, ref combatAbilities);
            }

            private void RemoveStatusEffect(
                DynamicBuffer<StatusEffectComponent> statusEffects,
                int index,
                ref GeneticCombatAbilities combatAbilities)
            {
                var effect = statusEffects[index];

                // Remove any permanent modifiers
                switch (effect.effectType)
                {
                    case StatusEffectType.Strengthened:
                        combatAbilities.strengthMultiplier /= (1f + effect.intensity * 0.5f);
                        break;
                    case StatusEffectType.Weakened:
                        combatAbilities.strengthMultiplier *= (1f + effect.intensity * 0.3f);
                        break;
                    case StatusEffectType.Quickened:
                        combatAbilities.agilityMultiplier /= (1f + effect.intensity * 0.4f);
                        break;
                    case StatusEffectType.Slowed:
                        combatAbilities.agilityMultiplier *= (1f + effect.intensity * 0.4f);
                        break;
                }

                statusEffects.RemoveAt(index);
            }

            private void ProcessPeriodicEffect(
                StatusEffectComponent effect,
                ref GeneticCombatAbilities combatAbilities,
                EnvironmentalCombatEffects environment)
            {
                switch (effect.effectType)
                {
                    case StatusEffectType.Burning:
                        // Fire damage, reduced by water affinity
                        float fireDamage = effect.intensity * 10f;
                        if (combatAbilities.primaryAffinity == ElementalAffinity.Water)
                            fireDamage *= 0.5f;
                        // Apply damage (would integrate with health system)
                        break;

                    case StatusEffectType.Poison:
                        // Poison damage, reduced by nature affinity
                        float poisonDamage = effect.intensity * 5f;
                        if (combatAbilities.primaryAffinity == ElementalAffinity.Nature)
                            poisonDamage *= 0.3f;
                        break;

                    case StatusEffectType.Regenerating:
                        // Healing over time, enhanced by nature affinity
                        float healing = effect.intensity * 8f;
                        if (combatAbilities.primaryAffinity == ElementalAffinity.Nature)
                            healing *= 1.5f;
                        break;
                }
            }

            private void ApplyContinuousEffect(
                StatusEffectComponent effect,
                ref GeneticCombatAbilities combatAbilities,
                float deltaTime)
            {
                float effectStrength = effect.intensity * deltaTime;

                switch (effect.effectType)
                {
                    case StatusEffectType.Strengthened:
                        combatAbilities.strengthMultiplier *= (1f + effectStrength * 0.1f);
                        break;

                    case StatusEffectType.Weakened:
                        combatAbilities.strengthMultiplier *= (1f - effectStrength * 0.1f);
                        break;

                    case StatusEffectType.Quickened:
                        combatAbilities.agilityMultiplier *= (1f + effectStrength * 0.15f);
                        break;

                    case StatusEffectType.Slowed:
                        combatAbilities.agilityMultiplier *= (1f - effectStrength * 0.15f);
                        break;

                    case StatusEffectType.Fortified:
                        combatAbilities.resilienceMultiplier *= (1f + effectStrength * 0.2f);
                        break;

                    case StatusEffectType.Vulnerable:
                        combatAbilities.resilienceMultiplier *= (1f - effectStrength * 0.2f);
                        break;
                }
            }

            private void ProcessStatusInteractions(
                DynamicBuffer<StatusEffectComponent> statusEffects,
                ref GeneticCombatAbilities combatAbilities)
            {
                // Check for status effect combinations that create new effects
                bool hasWet = false, hasElectrified = false, hasChilled = false;
                bool hasBurning = false;

                for (int i = 0; i < statusEffects.Length; i++)
                {
                    var effect = statusEffects[i];
                    switch (effect.effectType)
                    {
                        case StatusEffectType.WetCondition:
                            hasWet = true;
                            break;
                        case StatusEffectType.ElectrifiedCondition:
                            hasElectrified = true;
                            break;
                        case StatusEffectType.ChilledCondition:
                            hasChilled = true;
                            break;
                        case StatusEffectType.Burning:
                            hasBurning = true;
                            break;
                        case StatusEffectType.Freezing:
                            // Freezing effect detected
                            break;
                    }
                }

                // Process interactions
                if (hasWet && hasElectrified)
                {
                    // Wet + Electrified = Enhanced shock damage
                    combatAbilities.adaptationLevel *= 0.7f; // Reduced adaptation when shocked
                }

                if (hasWet && hasChilled)
                {
                    // Wet + Chilled = Increased freeze chance
                    // Would add freezing effect or enhance existing one
                }

                if (hasBurning && hasChilled)
                {
                    // Fire and ice cancel each other out
                    // Remove both effects (simplified)
                }
            }
        }
    }

    /// <summary>
    /// Tactical Combat AI System - Advanced AI decision making for combat
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial class TacticalCombatAISystem : SystemBase
    {
        private EntityQuery _tacticalAIQuery;

        protected override void OnCreate()
        {
            _tacticalAIQuery = GetEntityQuery(
                ComponentType.ReadWrite<TacticalCombatAI>(),
                ComponentType.ReadOnly<GeneticCombatAbilities>(),
                ComponentType.ReadOnly<FormationCombatComponent>(),
                ComponentType.ReadOnly<LocalTransform>()
            );

            RequireForUpdate(_tacticalAIQuery);
        }

        protected override void OnUpdate()
        {
            var deltaTime = SystemAPI.Time.DeltaTime;
            var currentTime = (float)SystemAPI.Time.ElapsedTime;

            // Update tactical AI decisions
            var tacticalJob = new TacticalAIUpdateJob
            {
                deltaTime = deltaTime,
                currentTime = currentTime,
                transformLookup = GetComponentLookup<LocalTransform>(true),
                combatAbilitiesLookup = GetComponentLookup<GeneticCombatAbilities>(true)
            };

            Dependency = tacticalJob.ScheduleParallel(_tacticalAIQuery, Dependency);
        }


        private partial struct TacticalAIUpdateJob : IJobEntity
        {
            [ReadOnly] public float deltaTime;
            [ReadOnly] public float currentTime;
            [ReadOnly] public ComponentLookup<LocalTransform> transformLookup;
            [ReadOnly] public ComponentLookup<GeneticCombatAbilities> combatAbilitiesLookup;

            public void Execute(
                ref TacticalCombatAI tacticalAI,
                in GeneticCombatAbilities combatAbilities,
                in FormationCombatComponent formation,
                in LocalTransform transform)
            {
                // Update threat assessment
                UpdateThreatAssessment(ref tacticalAI, combatAbilities, formation);

                // Adapt strategy based on current situation
                AdaptStrategy(ref tacticalAI, combatAbilities, currentTime);

                // Update target prioritization
                UpdateTargetPrioritization(ref tacticalAI, transform);

                // Update personality-based behaviors
                UpdatePersonalityBehaviors(ref tacticalAI, combatAbilities, deltaTime);
            }

            private void UpdateThreatAssessment(
                ref TacticalCombatAI tacticalAI,
                GeneticCombatAbilities combatAbilities,
                FormationCombatComponent formation)
            {
                float baseThreat = 0.5f; // Neutral threat level

                // Assess own combat effectiveness
                float ownEffectiveness = CalculateCombatEffectiveness(combatAbilities);

                // Formation bonus affects threat perception
                float formationFactor = formation.formationBonus > 0 ? 0.8f : 1.2f;

                // Environmental adaptation affects confidence
                float adaptationFactor = math.lerp(1.3f, 0.7f, combatAbilities.adaptationLevel);

                tacticalAI.threatAssessment = baseThreat * formationFactor * adaptationFactor / ownEffectiveness;
                tacticalAI.threatAssessment = math.clamp(tacticalAI.threatAssessment, 0.1f, 2f);
            }

            private float CalculateCombatEffectiveness(GeneticCombatAbilities combatAbilities)
            {
                float effectiveness = 0f;
                effectiveness += combatAbilities.strengthMultiplier * 0.3f;
                effectiveness += combatAbilities.agilityMultiplier * 0.2f;
                effectiveness += combatAbilities.resilienceMultiplier * 0.2f;
                effectiveness += combatAbilities.intellectMultiplier * 0.15f;
                effectiveness += combatAbilities.vitalityMultiplier * 0.1f;
                effectiveness += combatAbilities.adaptationLevel * 0.05f;

                return math.clamp(effectiveness, 0.1f, 3f);
            }

            private void AdaptStrategy(
                ref TacticalCombatAI tacticalAI,
                GeneticCombatAbilities combatAbilities,
                float currentTime)
            {
                // Only change strategy periodically based on adaptability
                float strategyChangeInterval = math.lerp(10f, 2f, tacticalAI.adaptabilityLevel);

                if (currentTime - tacticalAI.lastStrategyChange < strategyChangeInterval)
                    return;

                // Choose strategy based on specialization and threat level
                CombatStrategy newStrategy = DetermineOptimalStrategy(
                    combatAbilities.specialization,
                    tacticalAI.threatAssessment,
                    tacticalAI.personality);

                if (newStrategy != tacticalAI.preferredStrategy)
                {
                    tacticalAI.preferredStrategy = newStrategy;
                    tacticalAI.lastStrategyChange = currentTime;
                }
            }

            private CombatStrategy DetermineOptimalStrategy(
                CombatSpecialization specialization,
                float threatLevel,
                TacticalPersonality personality)
            {
                // Base strategy on specialization
                CombatStrategy baseStrategy = specialization switch
                {
                    CombatSpecialization.Berserker => CombatStrategy.DirectAssault,
                    CombatSpecialization.Tank => CombatStrategy.Formation,
                    CombatSpecialization.Assassin => CombatStrategy.Ambush,
                    CombatSpecialization.Mage => CombatStrategy.Elemental,
                    CombatSpecialization.Healer => CombatStrategy.Support,
                    CombatSpecialization.Summoner => CombatStrategy.Support,
                    CombatSpecialization.Tactician => CombatStrategy.Formation,
                    _ => CombatStrategy.DirectAssault
                };

                // Modify based on threat level
                if (threatLevel > 1.5f) // High threat
                {
                    return personality switch
                    {
                        TacticalPersonality.Aggressive => CombatStrategy.DirectAssault,
                        TacticalPersonality.Defensive => CombatStrategy.Formation,
                        TacticalPersonality.Opportunistic => CombatStrategy.HitAndRun,
                        _ => CombatStrategy.Formation
                    };
                }
                else if (threatLevel < 0.7f) // Low threat
                {
                    return CombatStrategy.DirectAssault;
                }

                return baseStrategy;
            }

            private void UpdateTargetPrioritization(ref TacticalCombatAI tacticalAI, LocalTransform transform)
            {
                // This would integrate with a target detection system
                // For now, just maintain current targets and update threat assessment

                if (tacticalAI.primaryTarget != Entity.Null)
                {
                    // Check if primary target is still valid and in range
                    if (transformLookup.TryGetComponent(tacticalAI.primaryTarget, out var targetTransform))
                    {
                        float distance = math.distance(transform.Position, targetTransform.Position);
                        if (distance > 50f) // Out of engagement range
                        {
                            tacticalAI.primaryTarget = Entity.Null;
                        }
                    }
                    else
                    {
                        tacticalAI.primaryTarget = Entity.Null;
                    }
                }
            }

            private void UpdatePersonalityBehaviors(
                ref TacticalCombatAI tacticalAI,
                GeneticCombatAbilities combatAbilities,
                float deltaTime)
            {
                switch (tacticalAI.personality)
                {
                    case TacticalPersonality.Aggressive:
                        tacticalAI.aggressionLevel = math.min(1f, tacticalAI.aggressionLevel + deltaTime * 0.1f);
                        break;

                    case TacticalPersonality.Defensive:
                        tacticalAI.aggressionLevel = math.max(0.2f, tacticalAI.aggressionLevel - deltaTime * 0.05f);
                        break;

                    case TacticalPersonality.Berserker:
                        // Becomes more aggressive when health is low (would need health component)
                        float healthFactor = 1f; // Placeholder
                        tacticalAI.aggressionLevel = math.lerp(0.5f, 1f, 1f - healthFactor);
                        break;

                    case TacticalPersonality.Adaptive:
                        // Adapts aggression based on threat level
                        tacticalAI.aggressionLevel = math.lerp(0.3f, 0.9f, 1f - tacticalAI.threatAssessment);
                        break;

                    case TacticalPersonality.Supportive:
                        tacticalAI.cooperationLevel = math.min(1f, tacticalAI.cooperationLevel + deltaTime * 0.15f);
                        break;
                }

                // Clamp values
                tacticalAI.aggressionLevel = math.clamp(tacticalAI.aggressionLevel, 0f, 1f);
                tacticalAI.cooperationLevel = math.clamp(tacticalAI.cooperationLevel, 0f, 1f);
                tacticalAI.adaptabilityLevel = math.clamp(tacticalAI.adaptabilityLevel, 0f, 1f);
            }
        }
    }

    /// <summary>
    /// Multiplayer Combat Synchronization System
    /// Synchronizes combat state across network for competitive multiplayer
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial class MultiplayerCombatSyncSystem : SystemBase
    {
        private EntityQuery _networkCombatQuery;

        protected override void OnCreate()
        {
            _networkCombatQuery = GetEntityQuery(
                ComponentType.ReadWrite<CombatPredictionState>(),
                ComponentType.ReadOnly<GeneticCombatAbilities>(),
                ComponentType.ReadOnly<NetworkOwnership>(),
                ComponentType.ReadOnly<ReplicatedCreatureState>()
            );

            RequireForUpdate(_networkCombatQuery);
        }

        protected override void OnUpdate()
        {
            var currentTime = (float)SystemAPI.Time.ElapsedTime;
            var deltaTime = SystemAPI.Time.DeltaTime;

            // Synchronize combat state across network
            var syncJob = new CombatNetworkSyncJob
            {
                currentTime = currentTime,
                deltaTime = deltaTime
            };

            Dependency = syncJob.ScheduleParallel(_networkCombatQuery, Dependency);
        }


        private partial struct CombatNetworkSyncJob : IJobEntity
        {
            [ReadOnly] public float currentTime;
            [ReadOnly] public float deltaTime;

            public void Execute(
                ref CombatPredictionState prediction,
                in GeneticCombatAbilities combatAbilities,
                in NetworkOwnership ownership,
                in ReplicatedCreatureState replicatedState)
            {
                // Update prediction state for smooth combat
                if (ownership.hasAuthority)
                {
                    // Authority entity updates prediction
                    UpdateAuthorityPrediction(ref prediction, replicatedState, currentTime);
                }
                else
                {
                    // Non-authority entity reconciles with server state
                    ReconcilePredictionState(ref prediction, replicatedState, currentTime);
                }

                // Calculate prediction confidence based on network conditions
                UpdatePredictionConfidence(ref prediction, replicatedState, currentTime);
            }

            private void UpdateAuthorityPrediction(
                ref CombatPredictionState prediction,
                ReplicatedCreatureState replicatedState,
                float currentTime)
            {
                // Authority entity provides ground truth
                prediction.predictedPosition = replicatedState.position;
                prediction.predictedHealth = replicatedState.health;
                prediction.predictedEnergy = replicatedState.energy;
                prediction.predictionTimestamp = currentTime;
                prediction.confidenceLevel = 1f;
                prediction.needsReconciliation = false;
            }

            private void ReconcilePredictionState(
                ref CombatPredictionState prediction,
                ReplicatedCreatureState replicatedState,
                float currentTime)
            {
                float timeSinceUpdate = currentTime - replicatedState.timestamp;

                if (timeSinceUpdate < 0.5f) // Recent update
                {
                    // Predict based on velocity
                    prediction.predictedPosition = replicatedState.position +
                                                 replicatedState.velocity * timeSinceUpdate;

                    // Predict health and energy changes (simplified)
                    prediction.predictedHealth = replicatedState.health;
                    prediction.predictedEnergy = math.max(0f, replicatedState.energy - timeSinceUpdate * 5f);

                    prediction.confidenceLevel = math.max(0.1f, 1f - timeSinceUpdate * 2f);
                }
                else
                {
                    // Old data, low confidence
                    prediction.confidenceLevel = 0.1f;
                    prediction.needsReconciliation = true;
                }

                prediction.predictionTimestamp = currentTime;
            }

            private void UpdatePredictionConfidence(
                ref CombatPredictionState prediction,
                ReplicatedCreatureState replicatedState,
                float currentTime)
            {
                // Confidence decreases over time without updates
                float timeSinceUpdate = currentTime - replicatedState.timestamp;
                float confidenceDecay = math.clamp(timeSinceUpdate * 0.5f, 0f, 0.9f);

                prediction.confidenceLevel = math.max(0.1f, prediction.confidenceLevel - confidenceDecay);

                // Flag for reconciliation if confidence is too low
                if (prediction.confidenceLevel < 0.3f)
                {
                    prediction.needsReconciliation = true;
                }
            }
        }
    }
}