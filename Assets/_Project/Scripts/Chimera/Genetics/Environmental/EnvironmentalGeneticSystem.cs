using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using UnityEngine;
using System.Collections.Generic;
using Laboratory.Chimera.Core;
using Laboratory.Chimera.Genetics;
using Laboratory.AI.Services;
using Laboratory.Chimera.ECS.Components;

namespace Laboratory.Chimera.Genetics.Environmental
{
    /// <summary>
    /// ENVIRONMENTAL GENETIC EXPRESSION SYSTEM - Dynamic trait adaptation based on environment
    /// PURPOSE: Enable creatures to dynamically express different genetic traits based on environmental conditions
    /// FEATURES: Environmental triggers, adaptive trait expression, phenotype plasticity, survival optimization
    /// BENEFITS: Realistic genetic adaptation, dynamic creature behaviors, environmental storytelling
    /// </summary>

    // Environmental genetic expression component
    public struct EnvironmentalGeneticComponent : IComponentData
    {
        public float adaptationRate;
        public float maxAdaptationStrength;
        public float currentStress;
        public BiomeType currentBiome;
        public BiomeType nativeBiome;
        public float timeInCurrentBiome;
        public float adaptationThreshold;
        public bool isAdapting;
        public int activeExpressionCount;
    }

    // Environmental triggers for genetic expression
    public struct EnvironmentalTriggerComponent : IBufferElementData
    {
        public TriggerType triggerType;
        public float threshold;
        public float currentValue;
        public TraitType affectedTrait;
        public ExpressionModifier modifier;
        public float intensity;
        public bool isActive;
        public float activationTime;
    }

    // Dynamic trait expression data
    public struct DynamicTraitExpressionComponent : IBufferElementData
    {
        public TraitType traitType;
        public float baseValue;
        public float environmentalModifier;
        public float currentExpression;
        public float targetExpression;
        public float adaptationSpeed;
        public ExpressionTrigger trigger;
        public float lastUpdateTime;
    }

    // Environmental pressure data
    public struct EnvironmentalPressureComponent : IComponentData
    {
        public float temperature;
        public float humidity;
        public float oxygenLevel;
        public float toxicity;
        public float predationPressure;
        public float foodAvailability;
        public float socialPressure;
        public float overallStress;
    }

    // Genetic adaptation memory
    public struct AdaptationMemoryComponent : IBufferElementData
    {
        public BiomeType biome;
        public TraitType trait;
        public float adaptedValue;
        public float adaptationStrength;
        public float timeToAdapt;
        public int generationsStable;
        public bool isInheritable;
    }

    public enum TriggerType : byte
    {
        Temperature,
        Humidity,
        Oxygen,
        Toxicity,
        Predation,
        Food,
        Social,
        Light,
        Pressure,
        Custom
    }

    public enum ExpressionModifier : byte
    {
        Increase,
        Decrease,
        Oscillate,
        Threshold,
        Proportional,
        Inverse
    }

    public enum ExpressionTrigger : byte
    {
        Environmental,
        Stress,
        Age,
        Social,
        Seasonal,
        Circadian,
        Emergency
    }

    // Main environmental genetic system
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(UnifiedAIStateSystem))]
    public partial class EnvironmentalGeneticSystem : SystemBase
    {
        private EntityQuery _environmentalGeneticQuery;
        private IEnvironmentService _environmentService;
        private Random _random;

        // Environmental expression profiles
        private readonly Dictionary<BiomeType, EnvironmentalProfile> _biomeProfiles = new Dictionary<BiomeType, EnvironmentalProfile>();

        protected override void OnCreate()
        {
            _environmentalGeneticQuery = GetEntityQuery(
                ComponentType.ReadWrite<EnvironmentalGeneticComponent>(),
                ComponentType.ReadWrite<CreatureGeneticsComponent>(),
                ComponentType.ReadOnly<Unity.Transforms.LocalTransform>()
            );

            _random = new Random((uint)System.DateTime.Now.Ticks);
            InitializeBiomeProfiles();

            RequireForUpdate(_environmentalGeneticQuery);
        }

        protected override void OnUpdate()
        {
            float deltaTime = SystemAPI.Time.DeltaTime;
            float currentTime = (float)SystemAPI.Time.ElapsedTime;

            // Initialize environment service if needed
            if (_environmentService == null)
            {
                _environmentService = AIServiceManager.Get<IEnvironmentService>();
            }

            // Process environmental genetic adaptations
            Entities
                .WithAll<EnvironmentalGeneticComponent>()
                .ForEach((Entity entity, ref EnvironmentalGeneticComponent envGenetic, ref GeneticProfile genetics,
                    in Unity.Transforms.LocalTransform transform, in DynamicBuffer<EnvironmentalTriggerComponent> triggers,
                    in DynamicBuffer<DynamicTraitExpressionComponent> expressions) =>
                {
                    // Update environmental conditions
                    UpdateEnvironmentalConditions(entity, ref envGenetic, transform.Position);

                    // Process environmental triggers
                    ProcessEnvironmentalTriggers(entity, ref envGenetic, triggers, currentTime, deltaTime);

                    // Update trait expressions
                    UpdateTraitExpressions(entity, ref envGenetic, ref genetics, expressions, deltaTime);

                    // Handle adaptation process
                    ProcessAdaptation(entity, ref envGenetic, ref genetics, currentTime, deltaTime);

                }).WithoutBurst().Run(); // WithoutBurst for service access
        }

        private void UpdateEnvironmentalConditions(Entity entity, ref EnvironmentalGeneticComponent envGenetic, float3 position)
        {
            if (_environmentService == null) return;

            // Get current biome
            var currentBiome = _environmentService.GetBiomeAt(position);

            if (currentBiome != envGenetic.currentBiome)
            {
                // Biome changed - reset adaptation timer
                envGenetic.currentBiome = currentBiome;
                envGenetic.timeInCurrentBiome = 0f;
                envGenetic.isAdapting = true;
            }
            else
            {
                envGenetic.timeInCurrentBiome += SystemAPI.Time.DeltaTime;
            }

            // Calculate environmental stress
            var pressure = _environmentService.GetEnvironmentalPressure(position, entity);
            envGenetic.currentStress = pressure;

            // Update environmental pressure component if it exists
            if (EntityManager.HasComponent<EnvironmentalPressureComponent>(entity))
            {
                var pressureComponent = EntityManager.GetComponentData<EnvironmentalPressureComponent>(entity);
                UpdatePressureValues(ref pressureComponent, position, currentBiome);
                EntityManager.SetComponentData(entity, pressureComponent);
            }
            else
            {
                // Create pressure component
                var newPressure = new EnvironmentalPressureComponent();
                UpdatePressureValues(ref newPressure, position, currentBiome);
                EntityManager.AddComponentData(entity, newPressure);
            }
        }

        private void UpdatePressureValues(ref EnvironmentalPressureComponent pressure, float3 position, BiomeType biome)
        {
            if (_biomeProfiles.TryGetValue(biome, out var profile))
            {
                pressure.temperature = profile.averageTemperature + _random.NextFloat(-profile.temperatureVariance, profile.temperatureVariance);
                pressure.humidity = profile.averageHumidity + _random.NextFloat(-profile.humidityVariance, profile.humidityVariance);
                pressure.oxygenLevel = profile.oxygenLevel;
                pressure.toxicity = profile.toxicityLevel;
                pressure.predationPressure = profile.predationPressure;
                pressure.foodAvailability = profile.foodAvailability;
                pressure.socialPressure = profile.socialPressure;

                // Calculate overall stress
                var stressFactors = new float[]
                {
                    math.abs(pressure.temperature - 20f) / 40f, // Temperature stress (optimal around 20°C)
                    math.abs(pressure.humidity - 0.5f) / 0.5f, // Humidity stress (optimal around 50%)
                    math.max(0f, 1f - pressure.oxygenLevel), // Oxygen deficit stress
                    pressure.toxicity, // Toxicity stress
                    pressure.predationPressure, // Predation stress
                    math.max(0f, 1f - pressure.foodAvailability), // Food scarcity stress
                    pressure.socialPressure // Social stress
                };

                pressure.overallStress = 0f;
                for (int i = 0; i < stressFactors.Length; i++)
                {
                    pressure.overallStress += stressFactors[i];
                }
                pressure.overallStress /= stressFactors.Length;
            }
        }

        private void ProcessEnvironmentalTriggers(Entity entity, ref EnvironmentalGeneticComponent envGenetic,
            DynamicBuffer<EnvironmentalTriggerComponent> triggers, float currentTime, float deltaTime)
        {
            var updatedTriggers = EntityManager.GetBuffer<EnvironmentalTriggerComponent>(entity);

            for (int i = 0; i < triggers.Length; i++)
            {
                var trigger = triggers[i];
                bool wasActive = trigger.isActive;

                // Update trigger current value based on type
                UpdateTriggerValue(ref trigger, entity);

                // Check trigger activation
                bool shouldActivate = trigger.currentValue >= trigger.threshold;

                if (shouldActivate && !wasActive)
                {
                    trigger.isActive = true;
                    trigger.activationTime = currentTime;
                    OnTriggerActivated(entity, trigger);
                }
                else if (!shouldActivate && wasActive)
                {
                    trigger.isActive = false;
                    OnTriggerDeactivated(entity, trigger);
                }

                updatedTriggers[i] = trigger;
            }
        }

        private void UpdateTriggerValue(ref EnvironmentalTriggerComponent trigger, Entity entity)
        {
            if (EntityManager.HasComponent<EnvironmentalPressureComponent>(entity))
            {
                var pressure = EntityManager.GetComponentData<EnvironmentalPressureComponent>(entity);

                trigger.currentValue = trigger.triggerType switch
                {
                    TriggerType.Temperature => pressure.temperature,
                    TriggerType.Humidity => pressure.humidity,
                    TriggerType.Oxygen => pressure.oxygenLevel,
                    TriggerType.Toxicity => pressure.toxicity,
                    TriggerType.Predation => pressure.predationPressure,
                    TriggerType.Food => pressure.foodAvailability,
                    TriggerType.Social => pressure.socialPressure,
                    _ => trigger.currentValue
                };
            }
        }

        private void OnTriggerActivated(Entity entity, EnvironmentalTriggerComponent trigger)
        {
            // Trigger environmental genetic response
            if (EntityManager.HasBuffer<DynamicTraitExpressionComponent>(entity))
            {
                var expressions = EntityManager.GetBuffer<DynamicTraitExpressionComponent>(entity);

                for (int i = 0; i < expressions.Length; i++)
                {
                    var expression = expressions[i];
                    if (expression.traitType == trigger.affectedTrait)
                    {
                        // Modify trait expression based on trigger
                        ModifyTraitExpression(ref expression, trigger);
                        expressions[i] = expression;
                    }
                }
            }

            Debug.Log($"Environmental trigger activated: {trigger.triggerType} affecting {trigger.affectedTrait}");
        }

        private void OnTriggerDeactivated(Entity entity, EnvironmentalTriggerComponent trigger)
        {
            // Gradually return to baseline expression
            Debug.Log($"Environmental trigger deactivated: {trigger.triggerType}");
        }

        private void ModifyTraitExpression(ref DynamicTraitExpressionComponent expression, EnvironmentalTriggerComponent trigger)
        {
            switch (trigger.modifier)
            {
                case ExpressionModifier.Increase:
                    expression.targetExpression = math.min(1f, expression.baseValue + trigger.intensity);
                    break;

                case ExpressionModifier.Decrease:
                    expression.targetExpression = math.max(0f, expression.baseValue - trigger.intensity);
                    break;

                case ExpressionModifier.Proportional:
                    expression.targetExpression = expression.baseValue * (1f + trigger.intensity);
                    break;

                case ExpressionModifier.Inverse:
                    expression.targetExpression = 1f - expression.baseValue * trigger.intensity;
                    break;

                case ExpressionModifier.Threshold:
                    expression.targetExpression = trigger.currentValue >= trigger.threshold ? 1f : 0f;
                    break;
            }

            expression.targetExpression = math.clamp(expression.targetExpression, 0f, 1f);
        }

        private void UpdateTraitExpressions(Entity entity, ref EnvironmentalGeneticComponent envGenetic, ref GeneticProfile genetics,
            DynamicBuffer<DynamicTraitExpressionComponent> expressions, float deltaTime)
        {
            var updatedExpressions = EntityManager.GetBuffer<DynamicTraitExpressionComponent>(entity);

            for (int i = 0; i < expressions.Length; i++)
            {
                var expression = expressions[i];

                // Lerp current expression towards target
                expression.currentExpression = math.lerp(expression.currentExpression, expression.targetExpression,
                    expression.adaptationSpeed * deltaTime);

                // Update environmental modifier
                expression.environmentalModifier = expression.currentExpression - expression.baseValue;

                // Apply expression to genetic profile
                ApplyExpressionToGenetics(ref genetics, expression);

                expression.lastUpdateTime = (float)SystemAPI.Time.ElapsedTime;
                updatedExpressions[i] = expression;
            }
        }

        private void ApplyExpressionToGenetics(ref GeneticProfile genetics, DynamicTraitExpressionComponent expression)
        {
            // Apply environmental expression to the genetic profile
            // This would modify the actual expressed traits based on environmental conditions
            switch (expression.traitType)
            {
                case TraitType.Speed:
                    // Apply speed modification (example)
                    break;

                case TraitType.Strength:
                    // Apply strength modification (example)
                    break;

                case TraitType.Intelligence:
                    // Apply intelligence modification (example)
                    break;

                case TraitType.Adaptability:
                    // Apply adaptability modification (example)
                    break;
            }
        }

        private void ProcessAdaptation(Entity entity, ref EnvironmentalGeneticComponent envGenetic, ref GeneticProfile genetics,
            float currentTime, float deltaTime)
        {
            if (!envGenetic.isAdapting) return;

            // Check if enough time has passed for adaptation
            if (envGenetic.timeInCurrentBiome >= envGenetic.adaptationThreshold)
            {
                // Process adaptation to current environment
                PerformAdaptation(entity, ref envGenetic, ref genetics);
                envGenetic.isAdapting = false;
            }
        }

        private void PerformAdaptation(Entity entity, ref EnvironmentalGeneticComponent envGenetic, ref GeneticProfile genetics)
        {
            // Create or update adaptation memory
            if (EntityManager.HasBuffer<AdaptationMemoryComponent>(entity))
            {
                var memoryBuffer = EntityManager.GetBuffer<AdaptationMemoryComponent>(entity);

                // Check if we already have an adaptation for this biome
                bool foundExisting = false;
                for (int i = 0; i < memoryBuffer.Length; i++)
                {
                    var memory = memoryBuffer[i];
                    if (memory.biome == envGenetic.currentBiome)
                    {
                        // Strengthen existing adaptation
                        memory.adaptationStrength = math.min(1f, memory.adaptationStrength + 0.1f);
                        memory.generationsStable++;
                        memoryBuffer[i] = memory;
                        foundExisting = true;
                        break;
                    }
                }

                if (!foundExisting)
                {
                    // Create new adaptation memory
                    memoryBuffer.Add(new AdaptationMemoryComponent
                    {
                        biome = envGenetic.currentBiome,
                        trait = TraitType.Adaptability, // Primary adaptation trait
                        adaptedValue = envGenetic.currentStress < 0.5f ? 1.2f : 0.8f,
                        adaptationStrength = 0.2f,
                        timeToAdapt = envGenetic.timeInCurrentBiome,
                        generationsStable = 1,
                        isInheritable = envGenetic.timeInCurrentBiome > envGenetic.adaptationThreshold * 2f
                    });
                }
            }
            else
            {
                // Add adaptation memory buffer
                var memoryBuffer = EntityManager.AddBuffer<AdaptationMemoryComponent>(entity);
                memoryBuffer.Add(new AdaptationMemoryComponent
                {
                    biome = envGenetic.currentBiome,
                    trait = TraitType.Adaptability,
                    adaptedValue = 1.1f,
                    adaptationStrength = 0.2f,
                    timeToAdapt = envGenetic.timeInCurrentBiome,
                    generationsStable = 1,
                    isInheritable = false
                });
            }

            Debug.Log($"Entity {entity} adapted to {envGenetic.currentBiome} biome");
        }

        private void InitializeBiomeProfiles()
        {
            _biomeProfiles[BiomeType.Desert] = new EnvironmentalProfile
            {
                averageTemperature = 40f,
                temperatureVariance = 15f,
                averageHumidity = 0.1f,
                humidityVariance = 0.05f,
                oxygenLevel = 0.95f,
                toxicityLevel = 0.1f,
                predationPressure = 0.6f,
                foodAvailability = 0.3f,
                socialPressure = 0.4f
            };

            _biomeProfiles[BiomeType.Forest] = new EnvironmentalProfile
            {
                averageTemperature = 20f,
                temperatureVariance = 10f,
                averageHumidity = 0.7f,
                humidityVariance = 0.2f,
                oxygenLevel = 1.1f,
                toxicityLevel = 0.05f,
                predationPressure = 0.5f,
                foodAvailability = 0.8f,
                socialPressure = 0.3f
            };

            _biomeProfiles[BiomeType.Mountain] = new EnvironmentalProfile
            {
                averageTemperature = 5f,
                temperatureVariance = 20f,
                averageHumidity = 0.4f,
                humidityVariance = 0.3f,
                oxygenLevel = 0.7f,
                toxicityLevel = 0.02f,
                predationPressure = 0.7f,
                foodAvailability = 0.4f,
                socialPressure = 0.2f
            };

            _biomeProfiles[BiomeType.Ocean] = new EnvironmentalProfile
            {
                averageTemperature = 15f,
                temperatureVariance = 5f,
                averageHumidity = 1.0f,
                humidityVariance = 0f,
                oxygenLevel = 0.6f,
                toxicityLevel = 0.1f,
                predationPressure = 0.8f,
                foodAvailability = 0.7f,
                socialPressure = 0.5f
            };

            _biomeProfiles[BiomeType.Temperate] = new EnvironmentalProfile
            {
                averageTemperature = 18f,
                temperatureVariance = 12f,
                averageHumidity = 0.6f,
                humidityVariance = 0.2f,
                oxygenLevel = 1.0f,
                toxicityLevel = 0.03f,
                predationPressure = 0.4f,
                foodAvailability = 0.9f,
                socialPressure = 0.3f
            };
        }

        // Public API
        public void SetAdaptationRate(Entity entity, float rate)
        {
            if (EntityManager.HasComponent<EnvironmentalGeneticComponent>(entity))
            {
                var component = EntityManager.GetComponentData<EnvironmentalGeneticComponent>(entity);
                component.adaptationRate = math.clamp(rate, 0f, 1f);
                EntityManager.SetComponentData(entity, component);
            }
        }

        public void AddEnvironmentalTrigger(Entity entity, TriggerType triggerType, TraitType affectedTrait,
            float threshold, ExpressionModifier modifier, float intensity)
        {
            if (EntityManager.HasBuffer<EnvironmentalTriggerComponent>(entity))
            {
                var triggers = EntityManager.GetBuffer<EnvironmentalTriggerComponent>(entity);
                triggers.Add(new EnvironmentalTriggerComponent
                {
                    triggerType = triggerType,
                    threshold = threshold,
                    affectedTrait = affectedTrait,
                    modifier = modifier,
                    intensity = intensity,
                    isActive = false,
                    currentValue = 0f
                });
            }
        }
    }

    // Supporting data structures
    public struct EnvironmentalProfile
    {
        public float averageTemperature;
        public float temperatureVariance;
        public float averageHumidity;
        public float humidityVariance;
        public float oxygenLevel;
        public float toxicityLevel;
        public float predationPressure;
        public float foodAvailability;
        public float socialPressure;
    }
}