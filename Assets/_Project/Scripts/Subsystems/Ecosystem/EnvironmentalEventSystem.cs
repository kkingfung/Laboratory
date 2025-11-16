using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using Laboratory.Core.Events;

namespace Laboratory.Subsystems.Ecosystem
{
    /// <summary>
    /// Environmental event system for managing ecosystem-wide events and their effects.
    /// Handles natural disasters, seasonal changes, migration events, and environmental phenomena.
    /// </summary>
    public class EnvironmentalEventSystem : MonoBehaviour, IEnvironmentalEventService
    {
        [Header("Event System Settings")]
        [SerializeField] private bool enableEnvironmentalEvents = true;
        [SerializeField] private bool enableRandomEvents = true;
        [SerializeField] [Range(60f, 3600f)] private float eventCheckInterval = 300f;
        [SerializeField] [Range(0f, 1f)] private float randomEventChance = 0.1f;

        [Header("Event Categories")]
        [SerializeField] private bool enableWeatherEvents = true;
        [SerializeField] private bool enableSeasonalEvents = true;
        [SerializeField] private bool enableMigrationEvents = true;
        [SerializeField] private bool enableDisasterEvents = true;
        [SerializeField] private bool enableResourceEvents = true;

        [Header("Event Severity")]
        [SerializeField] [Range(0f, 1f)] private float minorEventChance = 0.6f;
        [SerializeField] [Range(0f, 1f)] private float moderateEventChance = 0.3f;
        [SerializeField] [Range(0f, 1f)] private float severeEventChance = 0.1f;

        // Event tracking
        private readonly List<EnvironmentalEvent> _activeEvents = new();
        private readonly List<EnvironmentalEvent> _eventHistory = new();
        private readonly Dictionary<EnvironmentalEventType, EventTemplate> _eventTemplates = new();

        // Timing
        private float _lastEventCheck;
        private float _currentSeason = 0f; // 0-1 representing yearly cycle

        // Event effects
        private readonly Dictionary<string, EventEffect> _activeEffects = new();

        // Events
        public event Action<EnvironmentalEvent> OnEnvironmentalEvent; // Legacy compatibility
        public event Action<EnvironmentalEvent> OnEnvironmentalEventStarted;
        public event Action<EnvironmentalEvent> OnEnvironmentalEventEnded;
        public event Action<EnvironmentalEvent> OnEnvironmentalEventUpdated;
        public event Action<EventEffect> OnEventEffectApplied;

        // Properties
        public EnvironmentalEvent[] ActiveEvents => _activeEvents.ToArray();
        public EnvironmentalEvent[] EventHistory => _eventHistory.ToArray();
        public float CurrentSeason => _currentSeason;

        #region Unity Lifecycle

        private void Update()
        {
            if (!enableEnvironmentalEvents)
                return;

            // Update seasonal cycle
            UpdateSeasonalCycle();

            // Check for new events
            if (enableRandomEvents && Time.time - _lastEventCheck >= eventCheckInterval)
            {
                CheckForRandomEvents();
                _lastEventCheck = Time.time;
            }

            // Update active events
            UpdateActiveEvents();
        }

        #endregion

        #region Initialization

        public void Initialize(EcosystemSubsystemConfig config)
        {
            if (config?.EventConfig != null)
            {
                enableEnvironmentalEvents = config.EventConfig.EnableEnvironmentalEvents;
                // enableRandomEvents - Keep current value as EnableRandomEvents not available in config
                // eventCheckInterval - Keep current value as EventCheckInterval not available in config
                randomEventChance = config.EventConfig.BaseEventChance;
            }

            InitializeEventTemplates();
            StartCoroutine(EventSystemCoroutine());

            Debug.Log($"[EnvironmentalEventSystem] Initialized - Events: {enableEnvironmentalEvents}");
        }

        public async Task InitializeAsync(EcosystemSubsystemConfig config)
        {
            Initialize(config);
            await Task.CompletedTask;
        }

        private void InitializeEventTemplates()
        {
            _eventTemplates.Clear();

            // Weather events
            _eventTemplates[EnvironmentalEventType.Drought] = new EventTemplate
            {
                eventType = EnvironmentalEventType.Drought,
                name = "Drought",
                description = "Extended period of little or no precipitation",
                minDuration = 1800f, // 30 minutes
                maxDuration = 7200f, // 2 hours
                baseChance = 0.05f,
                seasonalModifier = CreateSeasonalModifier(summer: 2f, winter: 0.3f),
                effects = new List<EventEffectTemplate>
                {
                    new EventEffectTemplate { effectType = EventEffectType.WaterReduction, intensity = 0.7f },
                    new EventEffectTemplate { effectType = EventEffectType.VegetationStress, intensity = 0.8f },
                    new EventEffectTemplate { effectType = EventEffectType.TemperatureIncrease, intensity = 0.3f }
                }
            };

            _eventTemplates[EnvironmentalEventType.Flood] = new EventTemplate
            {
                eventType = EnvironmentalEventType.Flood,
                name = "Flood",
                description = "Overflow of water onto normally dry land",
                minDuration = 600f, // 10 minutes
                maxDuration = 3600f, // 1 hour
                baseChance = 0.03f,
                seasonalModifier = CreateSeasonalModifier(spring: 2f, autumn: 1.5f),
                effects = new List<EventEffectTemplate>
                {
                    new EventEffectTemplate { effectType = EventEffectType.HabitatDestruction, intensity = 0.6f },
                    new EventEffectTemplate { effectType = EventEffectType.WaterIncrease, intensity = 1.5f },
                    new EventEffectTemplate { effectType = EventEffectType.PopulationDisplacement, intensity = 0.8f }
                }
            };

            _eventTemplates[EnvironmentalEventType.Migration] = new EventTemplate
            {
                eventType = EnvironmentalEventType.Migration,
                name = "Migration Event",
                description = "Seasonal movement of creatures across biomes",
                minDuration = 1200f, // 20 minutes
                maxDuration = 3600f, // 1 hour
                baseChance = 0.2f,
                seasonalModifier = CreateSeasonalModifier(spring: 3f, autumn: 3f),
                effects = new List<EventEffectTemplate>
                {
                    new EventEffectTemplate { effectType = EventEffectType.PopulationFlux, intensity = 0.5f },
                    new EventEffectTemplate { effectType = EventEffectType.GeneticMixing, intensity = 0.8f },
                    new EventEffectTemplate { effectType = EventEffectType.ResourceCompetition, intensity = 0.6f }
                }
            };

            _eventTemplates[EnvironmentalEventType.FoodAbundance] = new EventTemplate
            {
                eventType = EnvironmentalEventType.FoodAbundance,
                name = "Resource Bloom",
                description = "Sudden abundance of food and resources",
                minDuration = 900f, // 15 minutes
                maxDuration = 2700f, // 45 minutes
                baseChance = 0.08f,
                seasonalModifier = CreateSeasonalModifier(spring: 2f, summer: 1.5f),
                effects = new List<EventEffectTemplate>
                {
                    new EventEffectTemplate { effectType = EventEffectType.FoodIncrease, intensity = 1.5f },
                    new EventEffectTemplate { effectType = EventEffectType.PopulationGrowth, intensity = 0.8f },
                    new EventEffectTemplate { effectType = EventEffectType.BreedingBonus, intensity = 1.2f }
                }
            };

            _eventTemplates[EnvironmentalEventType.Disease] = new EventTemplate
            {
                eventType = EnvironmentalEventType.Disease,
                name = "Disease Outbreak",
                description = "Spread of disease affecting local populations",
                minDuration = 1800f, // 30 minutes
                maxDuration = 5400f, // 90 minutes
                baseChance = 0.04f,
                seasonalModifier = CreateSeasonalModifier(winter: 1.5f, spring: 1.2f),
                effects = new List<EventEffectTemplate>
                {
                    new EventEffectTemplate { effectType = EventEffectType.PopulationReduction, intensity = 0.7f },
                    new EventEffectTemplate { effectType = EventEffectType.GeneticPressure, intensity = 0.9f },
                    new EventEffectTemplate { effectType = EventEffectType.ImmunityBuilding, intensity = 0.6f }
                }
            };

            // Add more event templates...
        }

        private Dictionary<Season, float> CreateSeasonalModifier(float spring = 1f, float summer = 1f, float autumn = 1f, float winter = 1f)
        {
            return new Dictionary<Season, float>
            {
                { Season.Spring, spring },
                { Season.Summer, summer },
                { Season.Autumn, autumn },
                { Season.Winter, winter }
            };
        }

        #endregion

        #region Event Management

        public void TriggerEvent(EnvironmentalEventType eventType, string biomeId, float severity)
        {
            TriggerEvent(eventType, severity, -1f);
        }

        public void TriggerEvent(EnvironmentalEventType eventType, float intensity = 1f, float duration = -1f)
        {
            if (!_eventTemplates.TryGetValue(eventType, out var template))
            {
                Debug.LogWarning($"[EnvironmentalEventSystem] No template found for event type: {eventType}");
                return;
            }

            var eventDuration = duration > 0 ? duration : UnityEngine.Random.Range(template.minDuration, template.maxDuration);

            var environmentalEvent = new EnvironmentalEvent
            {
                eventId = Guid.NewGuid().ToString(),
                eventType = eventType,
                name = template.name,
                description = template.description,
                intensity = Mathf.Clamp01(intensity),
                severity = intensity, // Float severity for backward compatibility
                duration = eventDuration,
                remainingTime = eventDuration,
                startTime = DateTime.UtcNow,
                isActive = true,
                affectedBiomes = GetAffectedBiomes(eventType),
                severityEnum = CalculateEventSeverity(intensity)
            };

            _activeEvents.Add(environmentalEvent);
            _eventHistory.Add(environmentalEvent);

            // Apply event effects
            ApplyEventEffects(environmentalEvent, template);

            OnEnvironmentalEvent?.Invoke(environmentalEvent); // Legacy compatibility
            OnEnvironmentalEventStarted?.Invoke(environmentalEvent);
            Debug.Log($"[EnvironmentalEventSystem] Started event: {environmentalEvent.name} (Duration: {eventDuration:F0}s)");
        }

        public void EndEvent(string eventId)
        {
            var environmentalEvent = _activeEvents.FirstOrDefault(e => e.eventId == eventId);
            if (environmentalEvent == null)
            {
                Debug.LogWarning($"[EnvironmentalEventSystem] Event not found: {eventId}");
                return;
            }

            environmentalEvent.isActive = false;
            environmentalEvent.endTime = DateTime.UtcNow;

            // Remove event effects
            RemoveEventEffects(environmentalEvent);

            _activeEvents.Remove(environmentalEvent);
            OnEnvironmentalEventEnded?.Invoke(environmentalEvent);

            Debug.Log($"[EnvironmentalEventSystem] Ended event: {environmentalEvent.name}");
        }

        public void ModifyEventIntensity(string eventId, float newIntensity)
        {
            var environmentalEvent = _activeEvents.FirstOrDefault(e => e.eventId == eventId);
            if (environmentalEvent == null)
                return;

            var oldIntensity = environmentalEvent.intensity;
            environmentalEvent.intensity = Mathf.Clamp01(newIntensity);
            environmentalEvent.severity = environmentalEvent.intensity; // Float severity
            environmentalEvent.severityEnum = CalculateEventSeverity(environmentalEvent.intensity);

            // Update effects
            UpdateEventEffects(environmentalEvent);

            OnEnvironmentalEventUpdated?.Invoke(environmentalEvent);
            Debug.Log($"[EnvironmentalEventSystem] Modified event intensity: {environmentalEvent.name} ({oldIntensity:F2} -> {newIntensity:F2})");
        }

        #endregion

        #region Event Processing

        private void UpdateActiveEvents()
        {
            for (int i = _activeEvents.Count - 1; i >= 0; i--)
            {
                var environmentalEvent = _activeEvents[i];
                environmentalEvent.remainingTime -= Time.deltaTime;

                if (environmentalEvent.remainingTime <= 0)
                {
                    EndEvent(environmentalEvent.eventId);
                }
                else
                {
                    // Update event progress
                    var progress = 1f - (environmentalEvent.remainingTime / environmentalEvent.duration);
                    UpdateEventProgress(environmentalEvent, progress);
                }
            }
        }

        private void UpdateEventProgress(EnvironmentalEvent environmentalEvent, float progress)
        {
            // Some events might have changing intensity over time
            switch (environmentalEvent.eventType)
            {
                case EnvironmentalEventType.Drought:
                    // Drought intensifies over time
                    environmentalEvent.intensity = Mathf.Lerp(0.3f, 1f, progress);
                    break;

                case EnvironmentalEventType.Migration:
                    // Migration has peak in the middle
                    environmentalEvent.intensity = Mathf.Sin(progress * Mathf.PI);
                    break;

                case EnvironmentalEventType.FoodAbundance:
                    // Resource bloom starts strong then diminishes
                    environmentalEvent.intensity = Mathf.Lerp(1f, 0.2f, progress);
                    break;
            }

            UpdateEventEffects(environmentalEvent);
        }

        private void CheckForRandomEvents()
        {
            if (UnityEngine.Random.value > randomEventChance)
                return;

            // Select random event type
            var availableEvents = _eventTemplates.Keys.ToList();
            var selectedEventType = availableEvents[UnityEngine.Random.Range(0, availableEvents.Count)];

            // Check if event type is enabled
            if (!IsEventTypeEnabled(selectedEventType))
                return;

            // Calculate event probability with seasonal modifiers
            var template = _eventTemplates[selectedEventType];
            var seasonalChance = GetSeasonalModifier(template, GetCurrentSeason());
            var finalChance = template.baseChance * seasonalChance;

            if (UnityEngine.Random.value <= finalChance)
            {
                var intensity = GenerateEventIntensity();
                TriggerEvent(selectedEventType, intensity);
            }
        }

        private void UpdateSeasonalCycle()
        {
            // Update seasonal cycle (simplified - could be tied to actual game time)
            _currentSeason += Time.deltaTime / 3600f; // 1 hour = 1 season cycle
            if (_currentSeason >= 1f)
                _currentSeason = 0f;
        }

        #endregion

        #region Event Effects

        private void ApplyEventEffects(EnvironmentalEvent environmentalEvent, EventTemplate template)
        {
            foreach (var effectTemplate in template.effects)
            {
                var effect = new EventEffect
                {
                    effectId = Guid.NewGuid().ToString(),
                    eventId = environmentalEvent.eventId,
                    effectType = effectTemplate.effectType,
                    intensity = effectTemplate.intensity * environmentalEvent.intensity,
                    isActive = true,
                    startTime = DateTime.UtcNow
                };

                _activeEffects[effect.effectId] = effect;
                ApplyEffect(effect);
                OnEventEffectApplied?.Invoke(effect);
            }
        }

        private void RemoveEventEffects(EnvironmentalEvent environmentalEvent)
        {
            var effectsToRemove = _activeEffects.Values
                .Where(e => e.eventId == environmentalEvent.eventId)
                .ToList();

            foreach (var effect in effectsToRemove)
            {
                RemoveEffect(effect);
                _activeEffects.Remove(effect.effectId);
            }
        }

        private void UpdateEventEffects(EnvironmentalEvent environmentalEvent)
        {
            var eventEffects = _activeEffects.Values
                .Where(e => e.eventId == environmentalEvent.eventId)
                .ToList();

            foreach (var effect in eventEffects)
            {
                // Update effect intensity based on event intensity
                var template = GetEffectTemplate(environmentalEvent.eventType, effect.effectType);
                if (template != null)
                {
                    effect.intensity = template.intensity * environmentalEvent.intensity;
                    UpdateEffect(effect);
                }
            }
        }

        private void ApplyEffect(EventEffect effect)
        {
            // Apply effects to the ecosystem
            switch (effect.effectType)
            {
                case EventEffectType.PopulationReduction:
                    ApplyPopulationEffect(effect.intensity, -1);
                    break;

                case EventEffectType.PopulationGrowth:
                    ApplyPopulationEffect(effect.intensity, 1);
                    break;

                case EventEffectType.HabitatDestruction:
                    ApplyHabitatEffect(effect.intensity, -1);
                    break;

                case EventEffectType.VegetationStress:
                    ApplyVegetationEffect(effect.intensity, -1);
                    break;

                case EventEffectType.WaterReduction:
                    ApplyWaterEffect(effect.intensity, -1);
                    break;

                case EventEffectType.WaterIncrease:
                    ApplyWaterEffect(effect.intensity, 1);
                    break;

                case EventEffectType.TemperatureIncrease:
                    ApplyTemperatureEffect(effect.intensity, 1);
                    break;

                case EventEffectType.FoodIncrease:
                    ApplyFoodEffect(effect.intensity, 1);
                    break;

                case EventEffectType.GeneticMixing:
                    ApplyGeneticEffect(effect.intensity);
                    break;
            }
        }

        private void RemoveEffect(EventEffect effect)
        {
            // Remove or reverse effects
            effect.isActive = false;
            effect.endTime = DateTime.UtcNow;
        }

        private void UpdateEffect(EventEffect effect)
        {
            // Update ongoing effects
            ApplyEffect(effect);
        }

        #endregion

        #region Effect Application

        private void ApplyPopulationEffect(float intensity, int direction)
        {
            // This would integrate with the population management system
            Debug.Log($"[EnvironmentalEventSystem] Applying population effect: {intensity * direction:F2}");
        }

        private void ApplyHabitatEffect(float intensity, int direction)
        {
            // This would integrate with the biome/habitat system
            Debug.Log($"[EnvironmentalEventSystem] Applying habitat effect: {intensity * direction:F2}");
        }

        private void ApplyVegetationEffect(float intensity, int direction)
        {
            // This would affect vegetation/plant systems
            Debug.Log($"[EnvironmentalEventSystem] Applying vegetation effect: {intensity * direction:F2}");
        }

        private void ApplyWaterEffect(float intensity, int direction)
        {
            // This would affect water resources
            Debug.Log($"[EnvironmentalEventSystem] Applying water effect: {intensity * direction:F2}");
        }

        private void ApplyTemperatureEffect(float intensity, int direction)
        {
            // This would integrate with the weather system
            Debug.Log($"[EnvironmentalEventSystem] Applying temperature effect: {intensity * direction:F2}");
        }

        private void ApplyFoodEffect(float intensity, int direction)
        {
            // This would affect food resources
            Debug.Log($"[EnvironmentalEventSystem] Applying food effect: {intensity * direction:F2}");
        }

        private void ApplyGeneticEffect(float intensity)
        {
            // This would integrate with the genetics system
            Debug.Log($"[EnvironmentalEventSystem] Applying genetic effect: {intensity:F2}");
        }

        #endregion

        #region Helper Methods

        private bool IsEventTypeEnabled(EnvironmentalEventType eventType)
        {
            return eventType switch
            {
                EnvironmentalEventType.Drought => enableWeatherEvents,
                EnvironmentalEventType.Flood => enableWeatherEvents,
                EnvironmentalEventType.Wildfire => enableWeatherEvents,
                EnvironmentalEventType.Migration => enableMigrationEvents,
                EnvironmentalEventType.Disease => enableDisasterEvents,
                EnvironmentalEventType.FoodAbundance => enableResourceEvents,
                _ => true
            };
        }

        private Season GetCurrentSeason()
        {
            var seasonIndex = Mathf.FloorToInt(_currentSeason * 4f);
            return (Season)(seasonIndex % 4);
        }

        private float GetSeasonalModifier(EventTemplate template, Season currentSeason)
        {
            return template.seasonalModifier.TryGetValue(currentSeason, out var modifier) ? modifier : 1f;
        }

        private float GenerateEventIntensity()
        {
            var random = UnityEngine.Random.value;
            if (random <= minorEventChance)
                return UnityEngine.Random.Range(0.2f, 0.5f); // Minor event
            else if (random <= minorEventChance + moderateEventChance)
                return UnityEngine.Random.Range(0.5f, 0.8f); // Moderate event
            else
                return UnityEngine.Random.Range(0.8f, 1f); // Severe event
        }

        private EventSeverity CalculateEventSeverity(float intensity)
        {
            if (intensity >= 0.8f)
                return EventSeverity.Severe;
            else if (intensity >= 0.5f)
                return EventSeverity.Moderate;
            else
                return EventSeverity.Minor;
        }

        private List<string> GetAffectedBiomes(EnvironmentalEventType eventType)
        {
            // This would return actual biome IDs from the biome system
            return new List<string> { "forest_1", "grassland_1" };
        }

        private EventEffectTemplate GetEffectTemplate(EnvironmentalEventType eventType, EventEffectType effectType)
        {
            if (!_eventTemplates.TryGetValue(eventType, out var template))
                return null;

            return template.effects.FirstOrDefault(e => e.effectType == effectType);
        }

        private IEnumerator EventSystemCoroutine()
        {
            while (true)
            {
                yield return new WaitForSeconds(10f);

                // Periodic maintenance
                CleanupOldEvents();
            }
        }

        private void CleanupOldEvents()
        {
            // Remove old events from history to prevent memory issues
            if (_eventHistory.Count > 1000)
            {
                _eventHistory.RemoveRange(0, _eventHistory.Count - 1000);
            }
        }

        #endregion

        #region Update Methods

        public void UpdateEvents(float deltaTime, WeatherData weather, List<PopulationData> populations)
        {
            if (!enableEnvironmentalEvents)
                return;

            // This method is called by the EcosystemSubsystemManager
            // The actual event updates are handled by the Update() method and coroutines
            // We can use this for additional ecosystem-aware event logic

            // Update event probabilities based on ecosystem state
            UpdateEventProbabilities(weather, populations);
        }

        private void UpdateEventProbabilities(WeatherData weather, List<PopulationData> populations)
        {
            // Adjust event chances based on current ecosystem conditions
            // For example, drought is more likely in hot, dry weather
            // Disease outbreaks more likely with high population density
        }

        #endregion

        #region Public API

        public EnvironmentalEvent GetEvent(string eventId)
        {
            return _activeEvents.FirstOrDefault(e => e.eventId == eventId);
        }

        public EnvironmentalEvent[] GetEventsByType(EnvironmentalEventType eventType)
        {
            return _activeEvents.Where(e => e.eventType == eventType).ToArray();
        }

        public EnvironmentalEvent[] GetEventsBySeverity(EventSeverity severity)
        {
            return _activeEvents.Where(e => e.severityEnum == severity).ToArray();
        }

        public bool IsEventActive(EnvironmentalEventType eventType)
        {
            return _activeEvents.Any(e => e.eventType == eventType);
        }

        public int GetActiveEventCount()
        {
            return _activeEvents.Count;
        }

        public List<EnvironmentalEvent> GetActiveEvents()
        {
            return _activeEvents.ToList();
        }

        #endregion

        #region Debug Methods

        [ContextMenu("Trigger Drought")]
        private void DebugTriggerDrought()
        {
            TriggerEvent(EnvironmentalEventType.Drought, 0.8f);
        }

        [ContextMenu("Trigger Migration")]
        private void DebugTriggerMigration()
        {
            TriggerEvent(EnvironmentalEventType.Migration, 0.6f);
        }

        [ContextMenu("End All Events")]
        private void DebugEndAllEvents()
        {
            var activeEventIds = _activeEvents.Select(e => e.eventId).ToList();
            foreach (var eventId in activeEventIds)
            {
                EndEvent(eventId);
            }
        }

        #endregion
    }

    #region Supporting Classes and Enums


    [Serializable]
    public class EventTemplate
    {
        public EnvironmentalEventType eventType;
        public string name;
        public string description;
        public float minDuration;
        public float maxDuration;
        public float baseChance;
        public Dictionary<Season, float> seasonalModifier;
        public List<EventEffectTemplate> effects;
    }

    [Serializable]
    public class EventEffectTemplate
    {
        public EventEffectType effectType;
        public float intensity;
    }

    [Serializable]
    public class EventEffect
    {
        public string effectId;
        public string eventId;
        public EventEffectType effectType;
        public float intensity;
        public bool isActive;
        public DateTime startTime;
        public DateTime? endTime;
    }


    public enum EventEffectType
    {
        PopulationReduction,
        PopulationGrowth,
        PopulationDisplacement,
        PopulationFlux,
        HabitatDestruction,
        HabitatImprovement,
        VegetationStress,
        VegetationGrowth,
        WaterReduction,
        WaterIncrease,
        TemperatureIncrease,
        TemperatureDecrease,
        FoodIncrease,
        FoodDecrease,
        GeneticMixing,
        GeneticPressure,
        ResourceCompetition,
        BreedingBonus,
        ImmunityBuilding
    }

    public enum EventSeverity
    {
        Minor,
        Moderate,
        Severe,
        Catastrophic
    }


    #endregion
}

