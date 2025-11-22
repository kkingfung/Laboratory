using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Laboratory.Chimera.Core;
using Laboratory.Chimera.Genetics;
using Laboratory.Core.Enums;
using Laboratory.Shared.Types;
using Random = UnityEngine.Random;

namespace Laboratory.Chimera.Ecosystem
{
    /// <summary>
    /// Dynamic Ecosystem Storytelling System for Project Chimera.
    /// Creates living world narratives through apex predator events, ecological disasters,
    /// population dynamics, and environmental storytelling that makes the world feel alive.
    ///
    /// Features:
    /// - Apex Predator Events: Rare super-predators that challenge entire ecosystems
    /// - Ecological Disasters: Volcanic eruptions, droughts, ice ages that reshape the world
    /// - Population Dynamics: Boom/crash cycles, migrations, invasive species
    /// - Environmental Storytelling: Weather events, seasonal changes, climate shifts
    /// - Player Impact Tracking: How player actions affect ecosystem health
    /// </summary>
    public class DynamicEcosystemStorytellingSystem : MonoBehaviour
    {
        [Header("System Configuration")]
        [SerializeField] private EcosystemStorytellingConfig config;
        [SerializeField] private bool enableApexPredatorEvents = true;
        [SerializeField] private bool enableEcologicalDisasters = true;
        [SerializeField] private bool enablePopulationDynamics = true;
        [SerializeField] private bool enableWeatherEvents = true;

        [Header("Event Frequencies (per game day)")]
        [SerializeField] [Range(0.001f, 0.1f)] private float apexPredatorEventRate = 0.01f;
        [SerializeField] [Range(0.001f, 0.05f)] private float ecologicalDisasterRate = 0.005f;
        [SerializeField] [Range(0.01f, 0.2f)] private float populationEventRate = 0.05f;
        [SerializeField] [Range(0.1f, 1f)] private float weatherEventRate = 0.3f;

        [Header("Population Tracking")]
        [SerializeField] private float populationUpdateInterval = 3600f; // 1 hour

        // Active ecosystem events
        private List<ApexPredatorEvent> activeApexEvents = new List<ApexPredatorEvent>();
        private List<EcologicalDisaster> activeDisasters = new List<EcologicalDisaster>();
        private List<PopulationEvent> activePopulationEvents = new List<PopulationEvent>();
        private List<WeatherEvent> activeWeatherEvents = new List<WeatherEvent>();

        // Ecosystem state tracking
        private Dictionary<BiomeType, StoryEcosystemState> biomeStates = new Dictionary<BiomeType, StoryEcosystemState>();
        private Dictionary<string, SpeciesPopulation> speciesPopulations = new Dictionary<string, SpeciesPopulation>();

        // Storytelling and narrative tracking
        private List<EcosystemStoryEvent> storyHistory = new List<EcosystemStoryEvent>();
        private Dictionary<string, float> playerImpactScores = new Dictionary<string, float>();

        // Events for other systems to respond to
        public static event Action<ApexPredatorEvent> OnApexPredatorAppears;
        public static event Action<ApexPredatorEvent> OnApexPredatorDefeated;
        public static event Action<EcologicalDisaster> OnEcologicalDisasterBegins;
        public static event Action<EcologicalDisaster> OnEcologicalDisasterEnds;
        public static event Action<PopulationEvent> OnPopulationEventOccurs;
        public static event Action<WeatherEvent> OnWeatherEventBegins;
        public static event Action<EcosystemStoryEvent> OnEcosystemStoryEvent;

        void Start()
        {
            InitializeStoryEcosystemStates();
            InvokeRepeating(nameof(ProcessEcosystemEvents), 1f, 86400f); // Daily processing
            InvokeRepeating(nameof(UpdatePopulations), 10f, populationUpdateInterval);
            InvokeRepeating(nameof(ProcessWeatherEvents), 60f, 21600f); // Every 6 hours
        }

        #region Initialization

        private void InitializeStoryEcosystemStates()
        {
            // Initialize all biome states
            var biomes = Enum.GetValues(typeof(BiomeType)).Cast<BiomeType>();
            foreach (var biome in biomes)
            {
                biomeStates[biome] = new StoryEcosystemState
                {
                    biome = biome,
                    healthLevel = Random.Range(0.6f, 0.9f),
                    storyTension = Random.Range(0.5f, 0.8f),
                    activeNarratives = Random.Range(1, 4),
                    playerInfluence = Random.Range(0.1f, 0.3f)
                };
            }

            // Initialize some species populations
            SeedInitialPopulations();

            UnityEngine.Debug.Log("Dynamic Ecosystem Storytelling System initialized - The world is alive!");
        }

        private void SeedInitialPopulations()
        {
            var biomes = biomeStates.Keys.ToArray();

            // Create diverse species for each biome
            foreach (var biome in biomes)
            {
                var speciesCount = Random.Range(3, 8);
                for (int i = 0; i < speciesCount; i++)
                {
                    var speciesId = $"{biome}_{GetRandomSpeciesName()}_{i:D2}";
                    var population = new SpeciesPopulation
                    {
                        speciesId = speciesId,
                        currentPopulation = Random.Range(50, 300),
                        maxPopulation = Random.Range(200, 500),
                        growthRate = Random.Range(0.02f, 0.08f),
                        nativeBiome = biome,
                        migrationPropensity = Random.Range(0.1f, 0.6f),
                        threatLevel = Random.Range(0.1f, 0.9f),
                        lastSeenTimestamp = Time.time
                    };

                    speciesPopulations[speciesId] = population;
                }
            }
        }

        private string GetRandomSpeciesName()
        {
            var names = new[]
            {
                "Stalker", "Howler", "Glider", "Burrower", "Swimmer", "Climber",
                "Grazer", "Hunter", "Scavenger", "Guardian", "Wanderer", "Sentinel"
            };
            return names[Random.Range(0, names.Length)];
        }

        #endregion

        #region Apex Predator Events

        private void ProcessEcosystemEvents()
        {
            if (enableApexPredatorEvents && Random.value < apexPredatorEventRate)
            {
                TriggerApexPredatorEvent();
            }

            if (enableEcologicalDisasters && Random.value < ecologicalDisasterRate)
            {
                TriggerEcologicalDisaster();
            }

            if (enablePopulationDynamics && Random.value < populationEventRate)
            {
                TriggerPopulationEvent();
            }

            UpdateActiveEvents();
        }

        private void TriggerApexPredatorEvent()
        {
            var availableBiomes = biomeStates.Where(kvp => kvp.Value.health > 0.3f).Select(kvp => kvp.Key).ToArray();
            if (availableBiomes.Length == 0) return;

            var targetBiome = availableBiomes[Random.Range(0, availableBiomes.Length)];
            var apexType = SelectApexPredatorType(targetBiome);

            var apexEvent = new ApexPredatorEvent
            {
                id = Guid.NewGuid().ToString(),
                predatorType = apexType,
                targetBiome = targetBiome,
                threatLevel = Random.Range(0.6f, 1f),
                duration = Random.Range(7, 21), // 1-3 weeks
                startTime = Time.time,
                currentPhase = ApexPredatorPhase.Approaching,
                populationImpact = Random.Range(0.2f, 0.5f),
                description = GenerateApexPredatorDescription(apexType, targetBiome)
            };

            activeApexEvents.Add(apexEvent);
            OnApexPredatorAppears?.Invoke(apexEvent);

            // Impact ecosystem immediately
            ApplyApexPredatorImpact(apexEvent);

            // Create story event
            var storyEvent = new EcosystemStoryEvent
            {
                type = EcosystemEventType.ApexPredatorArrival,
                description = $"A {apexType} apex predator has entered the {targetBiome} biome, causing widespread panic among local species.",
                timestamp = Time.time,
                affectedBiomes = new[] { targetBiome },
                severity = apexEvent.threatLevel
            };

            storyHistory.Add(storyEvent);
            OnEcosystemStoryEvent?.Invoke(storyEvent);

            UnityEngine.Debug.Log($"APEX PREDATOR EVENT: {apexType} appears in {targetBiome} with threat level {apexEvent.threatLevel:F2}");
        }

        private ApexPredatorType SelectApexPredatorType(BiomeType biome)
        {
            return biome switch
            {
                BiomeType.Forest => Random.value > 0.5f ? ApexPredatorType.ShadowStalker : ApexPredatorType.PrimordialDragon,
                BiomeType.Desert => Random.value > 0.5f ? ApexPredatorType.SandDevourerWorm : ApexPredatorType.CrystallineHuntress,
                BiomeType.Ocean => Random.value > 0.5f ? ApexPredatorType.AbyssalLeviathan : ApexPredatorType.TemporalShark,
                BiomeType.Mountain => Random.value > 0.5f ? ApexPredatorType.StormRider : ApexPredatorType.AvalancheBeaver,
                BiomeType.Arctic => Random.value > 0.5f ? ApexPredatorType.FrostTitan : ApexPredatorType.BlizzardPhantom,
                _ => ApexPredatorType.ChaosBeast
            };
        }

        private string GenerateApexPredatorDescription(ApexPredatorType type, BiomeType biome)
        {
            return type switch
            {
                ApexPredatorType.ShadowStalker => "A massive feline predator that phases through trees and hunts in perfect silence",
                ApexPredatorType.PrimordialDragon => "An ancient dragon awakened from deep forest slumber, breathing toxic spores",
                ApexPredatorType.SandDevourerWorm => "A colossal sandworm that swallows entire dunes and creates deadly sinkholes",
                ApexPredatorType.CrystallineHuntress => "A geometric predator that reflects desert heat into devastating laser attacks",
                ApexPredatorType.AbyssalLeviathan => "A deep-sea monster with bioluminescent lures and crushing tentacles",
                ApexPredatorType.TemporalShark => "A shark that moves through time, appearing and disappearing unpredictably",
                ApexPredatorType.StormRider => "A massive bird that commands lightning and creates devastating windstorms",
                ApexPredatorType.AvalancheBeaver => "An enormous beaver that triggers rockslides and builds deadly stone dams",
                ApexPredatorType.FrostTitan => "A towering ice giant that freezes entire regions with its breath",
                ApexPredatorType.BlizzardPhantom => "An ethereal predator made of living snowstorm that hunts in white-out conditions",
                ApexPredatorType.ChaosBeast => "An unpredictable entity that warps reality around it, defying natural laws",
                _ => "An unknown apex predator of terrifying power"
            };
        }

        private void ApplyApexPredatorImpact(ApexPredatorEvent apexEvent)
        {
            var biomeState = biomeStates[apexEvent.targetBiome];

            // Reduce ecosystem health and stability
            biomeState.health -= apexEvent.threatLevel * 0.3f;
            biomeState.stability -= apexEvent.threatLevel * 0.4f;

            // Impact local species populations
            var localSpecies = speciesPopulations.Where(kvp => kvp.Value.nativeBiome == apexEvent.targetBiome).ToArray();
            foreach (var species in localSpecies)
            {
                var population = species.Value;
                var impact = apexEvent.populationImpact * apexEvent.threatLevel;
                population.currentPopulation = (int)Mathf.Max(10, population.currentPopulation * (1f - impact));
                population.stress += impact;
            }

            biomeStates[apexEvent.targetBiome] = biomeState;
        }

        #endregion

        #region Ecological Disasters

        private void TriggerEcologicalDisaster()
        {
            var disasterType = SelectRandomDisasterType();
            var affectedBiomes = SelectAffectedBiomes(disasterType);

            var disaster = new EcologicalDisaster
            {
                id = Guid.NewGuid().ToString(),
                type = disasterType,
                severity = Random.Range(0.4f, 0.9f),
                duration = GetDisasterDuration(disasterType),
                affectedBiomes = affectedBiomes,
                startTime = Time.time,
                description = GenerateDisasterDescription(disasterType, affectedBiomes),
                environmentalEffects = GetDisasterEffects(disasterType)
            };

            activeDisasters.Add(disaster);
            OnEcologicalDisasterBegins?.Invoke(disaster);

            // Apply immediate disaster effects
            ApplyDisasterEffects(disaster);

            // Create story event
            var storyEvent = new EcosystemStoryEvent
            {
                type = EcosystemEventType.EcologicalDisaster,
                description = disaster.description,
                timestamp = Time.time,
                affectedBiomes = affectedBiomes,
                severity = disaster.severity
            };

            storyHistory.Add(storyEvent);
            OnEcosystemStoryEvent?.Invoke(storyEvent);

            UnityEngine.Debug.Log($"ECOLOGICAL DISASTER: {disasterType} affects {string.Join(", ", affectedBiomes)} with severity {disaster.severity:F2}");
        }

        private EcologicalDisasterType SelectRandomDisasterType()
        {
            var types = Enum.GetValues(typeof(EcologicalDisasterType)).Cast<EcologicalDisasterType>().ToArray();
            return types[Random.Range(0, types.Length)];
        }

        private BiomeType[] SelectAffectedBiomes(EcologicalDisasterType disasterType)
        {
            var allBiomes = biomeStates.Keys.ToArray();

            return disasterType switch
            {
                EcologicalDisasterType.VolcanicEruption => new[] { BiomeType.Mountain },
                EcologicalDisasterType.MassiveFlood => new[] { BiomeType.Forest, BiomeType.Mountain },
                EcologicalDisasterType.ExtremeDrought => new[] { BiomeType.Desert, BiomeType.Forest },
                EcologicalDisasterType.IceAge => allBiomes,
                EcologicalDisasterType.ToxicSpill => new[] { BiomeType.Ocean, BiomeType.Forest },
                EcologicalDisasterType.PlagueBlight => allBiomes.Where(b => b != BiomeType.Arctic).ToArray(),
                EcologicalDisasterType.MeteorImpact => new[] { allBiomes[Random.Range(0, allBiomes.Length)] },
                _ => new[] { allBiomes[Random.Range(0, allBiomes.Length)] }
            };
        }

        private int GetDisasterDuration(EcologicalDisasterType type)
        {
            return type switch
            {
                EcologicalDisasterType.VolcanicEruption => Random.Range(14, 45),
                EcologicalDisasterType.MassiveFlood => Random.Range(7, 21),
                EcologicalDisasterType.ExtremeDrought => Random.Range(30, 90),
                EcologicalDisasterType.IceAge => Random.Range(60, 180),
                EcologicalDisasterType.ToxicSpill => Random.Range(21, 60),
                EcologicalDisasterType.PlagueBlight => Random.Range(14, 42),
                EcologicalDisasterType.MeteorImpact => Random.Range(1, 7),
                _ => Random.Range(7, 30)
            };
        }

        private string GenerateDisasterDescription(EcologicalDisasterType type, BiomeType[] biomes)
        {
            var biomeNames = string.Join(" and ", biomes.Select(b => b.ToString()));

            return type switch
            {
                EcologicalDisasterType.VolcanicEruption => $"A massive volcanic eruption devastates the {biomeNames}, filling skies with ash and toxic gases",
                EcologicalDisasterType.MassiveFlood => $"Unprecedented flooding transforms {biomeNames} into vast waterlands, displacing countless species",
                EcologicalDisasterType.ExtremeDrought => $"A severe drought grips {biomeNames}, causing water sources to dry up and vegetation to wither",
                EcologicalDisasterType.IceAge => "Global temperatures plummet, triggering an ice age that threatens all biomes with freezing conditions",
                EcologicalDisasterType.ToxicSpill => $"A mysterious toxic substance spreads through {biomeNames}, poisoning the environment",
                EcologicalDisasterType.PlagueBlight => $"A devastating plague spreads through {biomeNames}, causing widespread illness among creatures",
                EcologicalDisasterType.MeteorImpact => $"A massive meteor strikes {biomeNames}, creating shockwaves that ripple across the region",
                _ => $"An unknown ecological disaster affects {biomeNames}"
            };
        }

        private Dictionary<string, float> GetDisasterEffects(EcologicalDisasterType type)
        {
            return type switch
            {
                EcologicalDisasterType.VolcanicEruption => new Dictionary<string, float>
                {
                    ["temperature"] = 5f, ["air_quality"] = -0.8f, ["visibility"] = -0.6f
                },
                EcologicalDisasterType.MassiveFlood => new Dictionary<string, float>
                {
                    ["water_level"] = 3f, ["soil_quality"] = -0.5f, ["accessibility"] = -0.7f
                },
                EcologicalDisasterType.ExtremeDrought => new Dictionary<string, float>
                {
                    ["water_availability"] = -0.9f, ["vegetation"] = -0.7f, ["temperature"] = 8f
                },
                EcologicalDisasterType.IceAge => new Dictionary<string, float>
                {
                    ["temperature"] = -15f, ["vegetation"] = -0.6f, ["water_availability"] = -0.4f
                },
                _ => new Dictionary<string, float> { ["stability"] = -0.3f }
            };
        }

        private void ApplyDisasterEffects(EcologicalDisaster disaster)
        {
            foreach (var biome in disaster.affectedBiomes)
            {
                var state = biomeStates[biome];

                // Apply severity-based impacts
                state.health -= disaster.severity * 0.4f;
                state.stability -= disaster.severity * 0.5f;
                state.lastMajorEvent = Time.time;

                // Apply specific environmental effects
                foreach (var effect in disaster.environmentalEffects)
                {
                    // These would modify environmental conditions in the biome
                    // For now, we'll apply them as general stress factors
                    state.environmentalStress += Mathf.Abs(effect.Value) * 0.1f;
                }

                biomeStates[biome] = state;

                // Impact species in affected biomes
                var localSpecies = speciesPopulations.Where(kvp => kvp.Value.nativeBiome == biome).ToArray();
                foreach (var species in localSpecies)
                {
                    var population = species.Value;
                    var mortalityRate = disaster.severity * 0.3f;
                    population.currentPopulation = Mathf.Max(5, (int)(population.currentPopulation * (1f - mortalityRate)));
                    population.stress += disaster.severity;
                }
            }
        }

        #endregion

        #region Population Dynamics

        private void TriggerPopulationEvent()
        {
            var eventTypes = Enum.GetValues(typeof(PopulationEventType)).Cast<PopulationEventType>().ToArray();
            var eventType = eventTypes[Random.Range(0, eventTypes.Length)];

            var popEvent = new PopulationEvent
            {
                id = Guid.NewGuid().ToString(),
                type = eventType,
                magnitude = Random.Range(0.3f, 0.8f),
                duration = GetPopulationEventDuration(eventType),
                affectedSpecies = SelectAffectedSpecies(eventType),
                startTime = Time.time,
                description = GeneratePopulationEventDescription(eventType)
            };

            activePopulationEvents.Add(popEvent);
            OnPopulationEventOccurs?.Invoke(popEvent);

            ApplyPopulationEventEffects(popEvent);

            UnityEngine.Debug.Log($"POPULATION EVENT: {eventType} affects {popEvent.affectedSpecies.Length} species");
        }

        private int GetPopulationEventDuration(PopulationEventType type)
        {
            return type switch
            {
                PopulationEventType.PopulationBoom => Random.Range(21, 60),
                PopulationEventType.PopulationCrash => Random.Range(14, 42),
                PopulationEventType.MassiveMigration => Random.Range(7, 21),
                PopulationEventType.InvasiveSpecies => Random.Range(30, 90),
                PopulationEventType.SpeciesExtinction => 1, // Immediate
                PopulationEventType.NewSpeciesDiscovery => 1, // Immediate
                _ => Random.Range(7, 30)
            };
        }

        private string[] SelectAffectedSpecies(PopulationEventType eventType)
        {
            var allSpecies = speciesPopulations.Keys.ToArray();

            return eventType switch
            {
                PopulationEventType.PopulationBoom => allSpecies.Where(s => Random.value < 0.3f).ToArray(),
                PopulationEventType.PopulationCrash => allSpecies.Where(s => Random.value < 0.2f).ToArray(),
                PopulationEventType.MassiveMigration => allSpecies.Where(s => speciesPopulations[s].migrationPropensity > 0.4f).ToArray(),
                PopulationEventType.InvasiveSpecies => new[] { GenerateInvasiveSpecies() },
                PopulationEventType.SpeciesExtinction => allSpecies.Where(s => speciesPopulations[s].currentPopulation < 30).Take(1).ToArray(),
                PopulationEventType.NewSpeciesDiscovery => new[] { GenerateNewSpecies() },
                _ => allSpecies.Where(s => Random.value < 0.1f).ToArray()
            };
        }

        private string GenerateInvasiveSpecies()
        {
            var speciesId = $"INVASIVE_{GetRandomSpeciesName()}_{Random.Range(1000, 9999)}";
            var randomBiome = biomeStates.Keys.ToArray()[Random.Range(0, biomeStates.Count)];

            var population = new SpeciesPopulation
            {
                speciesId = speciesId,
                currentPopulation = Random.Range(100, 300),
                maxPopulation = Random.Range(500, 1000),
                growthRate = Random.Range(0.1f, 0.2f), // High growth rate
                nativeBiome = randomBiome,
                migrationPropensity = Random.Range(0.7f, 0.9f), // High migration
                threatLevel = Random.Range(0.6f, 0.9f), // Often threatening
                lastSeenTimestamp = Time.time,
                isInvasive = true
            };

            speciesPopulations[speciesId] = population;
            return speciesId;
        }

        private string GenerateNewSpecies()
        {
            var speciesId = $"NEW_{GetRandomSpeciesName()}_{Random.Range(1000, 9999)}";
            var randomBiome = biomeStates.Keys.ToArray()[Random.Range(0, biomeStates.Count)];

            var population = new SpeciesPopulation
            {
                speciesId = speciesId,
                currentPopulation = Random.Range(10, 50),
                maxPopulation = Random.Range(100, 300),
                growthRate = Random.Range(0.03f, 0.06f),
                nativeBiome = randomBiome,
                migrationPropensity = Random.Range(0.1f, 0.4f),
                threatLevel = Random.Range(0.1f, 0.5f),
                lastSeenTimestamp = Time.time,
                isNewDiscovery = true
            };

            speciesPopulations[speciesId] = population;
            return speciesId;
        }

        private string GeneratePopulationEventDescription(PopulationEventType type)
        {
            return type switch
            {
                PopulationEventType.PopulationBoom => "Favorable conditions trigger explosive population growth across multiple species",
                PopulationEventType.PopulationCrash => "Environmental stress causes dramatic population declines in several species",
                PopulationEventType.MassiveMigration => "Massive creature migrations reshape regional population distributions",
                PopulationEventType.InvasiveSpecies => "A new invasive species threatens to disrupt established ecosystems",
                PopulationEventType.SpeciesExtinction => "A species vanishes forever, leaving an ecological void",
                PopulationEventType.NewSpeciesDiscovery => "Researchers discover a previously unknown species",
                _ => "Unknown population dynamics affect local ecosystems"
            };
        }

        private void ApplyPopulationEventEffects(PopulationEvent popEvent)
        {
            foreach (var speciesId in popEvent.affectedSpecies)
            {
                if (!speciesPopulations.ContainsKey(speciesId)) continue;

                var population = speciesPopulations[speciesId];

                switch (popEvent.type)
                {
                    case PopulationEventType.PopulationBoom:
                        population.currentPopulation = Mathf.Min(population.maxPopulation,
                            (int)(population.currentPopulation * (1f + popEvent.magnitude)));
                        population.growthRate *= 1.5f;
                        break;

                    case PopulationEventType.PopulationCrash:
                        population.currentPopulation = (int)(population.currentPopulation * (1f - popEvent.magnitude));
                        population.stress += popEvent.magnitude;
                        break;

                    case PopulationEventType.SpeciesExtinction:
                        population.currentPopulation = 0;
                        population.isExtinct = true;
                        break;
                }

                speciesPopulations[speciesId] = population;
            }
        }

        #endregion

        #region Weather Events

        private void ProcessWeatherEvents()
        {
            if (!enableWeatherEvents) return;

            if (Random.value < weatherEventRate)
            {
                TriggerWeatherEvent();
            }

            UpdateActiveWeatherEvents();
        }

        private void TriggerWeatherEvent()
        {
            var weatherTypes = Enum.GetValues(typeof(WeatherEventType)).Cast<WeatherEventType>().ToArray();
            var weatherType = weatherTypes[Random.Range(0, weatherTypes.Length)];

            var affectedBiomes = SelectWeatherAffectedBiomes(weatherType);

            var weatherEvent = new WeatherEvent
            {
                id = Guid.NewGuid().ToString(),
                type = weatherType,
                intensity = Random.Range(0.3f, 0.9f),
                duration = GetWeatherEventDuration(weatherType),
                affectedBiomes = affectedBiomes,
                startTime = Time.time,
                description = GenerateWeatherDescription(weatherType, affectedBiomes)
            };

            activeWeatherEvents.Add(weatherEvent);
            OnWeatherEventBegins?.Invoke(weatherEvent);

            ApplyWeatherEffects(weatherEvent);
        }

        private BiomeType[] SelectWeatherAffectedBiomes(WeatherEventType weatherType)
        {
            var allBiomes = biomeStates.Keys.ToArray();

            return weatherType switch
            {
                WeatherEventType.StormSeason => allBiomes.Where(b => b != BiomeType.Desert).ToArray(),
                WeatherEventType.Heatwave => new[] { BiomeType.Desert, BiomeType.Forest },
                WeatherEventType.Blizzard => new[] { BiomeType.Tundra, BiomeType.Mountain },
                WeatherEventType.AuroraDisplay => new[] { BiomeType.Tundra },
                WeatherEventType.MeteorShower => allBiomes,
                _ => new[] { allBiomes[Random.Range(0, allBiomes.Length)] }
            };
        }

        private int GetWeatherEventDuration(WeatherEventType type)
        {
            return type switch
            {
                WeatherEventType.StormSeason => Random.Range(14, 28),
                WeatherEventType.Heatwave => Random.Range(7, 21),
                WeatherEventType.Blizzard => Random.Range(3, 10),
                WeatherEventType.AuroraDisplay => Random.Range(1, 3),
                WeatherEventType.MeteorShower => Random.Range(1, 2),
                _ => Random.Range(1, 7)
            };
        }

        private string GenerateWeatherDescription(WeatherEventType type, BiomeType[] biomes)
        {
            var biomeNames = string.Join(" and ", biomes.Select(b => b.ToString()));

            return type switch
            {
                WeatherEventType.StormSeason => $"Intense storms batter {biomeNames}, creating dangerous but spectacular weather",
                WeatherEventType.Heatwave => $"Extreme heat waves scorch {biomeNames}, testing creature heat resistance",
                WeatherEventType.Blizzard => $"Fierce blizzards engulf {biomeNames}, reducing visibility and mobility",
                WeatherEventType.AuroraDisplay => $"Magnificent aurora lights dance over {biomeNames}, affecting sensitive creatures",
                WeatherEventType.MeteorShower => "A breathtaking meteor shower illuminates the night sky across all regions",
                _ => $"Unusual weather patterns develop over {biomeNames}"
            };
        }

        private void ApplyWeatherEffects(WeatherEvent weatherEvent)
        {
            foreach (var biome in weatherEvent.affectedBiomes)
            {
                var state = biomeStates[biome];

                // Apply weather-specific effects
                switch (weatherEvent.type)
                {
                    case WeatherEventType.StormSeason:
                        state.seasonalModifier *= 0.8f; // Reduces activity
                        break;
                    case WeatherEventType.Heatwave:
                        state.environmentalStress += weatherEvent.intensity * 0.3f;
                        break;
                    case WeatherEventType.Blizzard:
                        state.seasonalModifier *= 0.6f; // Greatly reduces activity
                        break;
                    case WeatherEventType.AuroraDisplay:
                        state.stability += 0.1f; // Peaceful effect
                        break;
                    case WeatherEventType.MeteorShower:
                        // Chance for genetic mutations in local species
                        TriggerMeteorMutations(biome, weatherEvent.intensity);
                        break;
                }

                biomeStates[biome] = state;
            }
        }

        private void TriggerMeteorMutations(BiomeType biome, float intensity)
        {
            var localSpecies = speciesPopulations.Where(kvp => kvp.Value.nativeBiome == biome).ToArray();

            foreach (var species in localSpecies)
            {
                if (Random.value < intensity * 0.1f) // 10% chance per intensity point
                {
                    // Trigger beneficial mutations in this species
                    UnityEngine.Debug.Log($"Meteor shower triggers beneficial mutations in {species.Key}");
                }
            }
        }

        #endregion

        #region Population Updates

        private void UpdatePopulations()
        {
            foreach (var kvp in speciesPopulations.ToArray())
            {
                var speciesId = kvp.Key;
                var population = kvp.Value;

                if (population.isExtinct) continue;

                // Natural population growth/decline
                var growthFactor = 1f + (population.growthRate * population.seasonalModifier);

                // Apply environmental stress
                if (population.stress > 0.5f)
                {
                    growthFactor *= (1f - population.stress);
                }

                // Update population
                population.currentPopulation = Mathf.Min(population.maxPopulation,
                    (int)(population.currentPopulation * growthFactor));

                // Gradually reduce stress
                population.stress = Mathf.Max(0f, population.stress - 0.05f);

                // Check for extinction
                if (population.currentPopulation < 5)
                {
                    if (Random.value < 0.1f) // 10% chance of extinction when very low
                    {
                        population.currentPopulation = 0;
                        population.isExtinct = true;

                        // Create extinction story event
                        var extinctionEvent = new EcosystemStoryEvent
                        {
                            type = EcosystemEventType.SpeciesExtinction,
                            description = $"{speciesId} has gone extinct due to environmental pressures",
                            timestamp = Time.time,
                            affectedBiomes = new[] { population.nativeBiome },
                            severity = 0.8f
                        };

                        storyHistory.Add(extinctionEvent);
                        OnEcosystemStoryEvent?.Invoke(extinctionEvent);
                    }
                }

                speciesPopulations[speciesId] = population;
            }

            // Update biome health based on species diversity
            UpdateBiomeHealthFromPopulations();
        }

        private void UpdateBiomeHealthFromPopulations()
        {
            foreach (var biome in biomeStates.Keys.ToArray())
            {
                var state = biomeStates[biome];
                var localSpecies = speciesPopulations.Where(kvp => kvp.Value.nativeBiome == biome && !kvp.Value.isExtinct).ToArray();

                // Biodiversity affects ecosystem health
                var biodiversityFactor = Mathf.Clamp01(localSpecies.Length / 10f); // Ideal: 10+ species
                var targetHealth = biodiversityFactor * 0.8f + 0.2f; // 0.2 to 1.0 range

                // Gradually adjust health toward target
                state.health = Mathf.Lerp(state.health, targetHealth, 0.1f);

                // Gradually recover stability if no recent major events
                if (Time.time - state.lastMajorEvent > 86400f * 30) // 30 days
                {
                    state.stability = Mathf.Min(1f, state.stability + 0.02f);
                }

                biomeStates[biome] = state;
            }
        }

        #endregion

        #region Event Management

        private void UpdateActiveEvents()
        {
            // Update apex predator events
            for (int i = activeApexEvents.Count - 1; i >= 0; i--)
            {
                var apexEvent = activeApexEvents[i];
                apexEvent.duration--;

                if (apexEvent.duration <= 0)
                {
                    OnApexPredatorDefeated?.Invoke(apexEvent);
                    activeApexEvents.RemoveAt(i);

                    // Create story event for apex predator departure
                    var storyEvent = new EcosystemStoryEvent
                    {
                        type = EcosystemEventType.ApexPredatorDefeated,
                        description = $"The {apexEvent.predatorType} apex predator has been driven away or defeated",
                        timestamp = Time.time,
                        affectedBiomes = new[] { apexEvent.targetBiome },
                        severity = 0.6f
                    };

                    storyHistory.Add(storyEvent);
                    OnEcosystemStoryEvent?.Invoke(storyEvent);
                }
                else
                {
                    activeApexEvents[i] = apexEvent;
                }
            }

            // Update ecological disasters
            for (int i = activeDisasters.Count - 1; i >= 0; i--)
            {
                var disaster = activeDisasters[i];
                disaster.duration--;

                if (disaster.duration <= 0)
                {
                    OnEcologicalDisasterEnds?.Invoke(disaster);
                    activeDisasters.RemoveAt(i);

                    // Begin ecosystem recovery
                    BeginEcosystemRecovery(disaster);
                }
                else
                {
                    activeDisasters[i] = disaster;
                }
            }

            // Update population events
            for (int i = activePopulationEvents.Count - 1; i >= 0; i--)
            {
                var popEvent = activePopulationEvents[i];
                popEvent.duration--;

                if (popEvent.duration <= 0)
                {
                    activePopulationEvents.RemoveAt(i);
                }
                else
                {
                    activePopulationEvents[i] = popEvent;
                }
            }
        }

        private void UpdateActiveWeatherEvents()
        {
            for (int i = activeWeatherEvents.Count - 1; i >= 0; i--)
            {
                var weatherEvent = activeWeatherEvents[i];
                weatherEvent.duration--;

                if (weatherEvent.duration <= 0)
                {
                    activeWeatherEvents.RemoveAt(i);

                    // Reset seasonal modifiers after weather event ends
                    foreach (var biome in weatherEvent.affectedBiomes)
                    {
                        var state = biomeStates[biome];
                        state.seasonalModifier = 1f;
                        biomeStates[biome] = state;
                    }
                }
                else
                {
                    activeWeatherEvents[i] = weatherEvent;
                }
            }
        }

        private void BeginEcosystemRecovery(EcologicalDisaster disaster)
        {
            foreach (var biome in disaster.affectedBiomes)
            {
                var state = biomeStates[biome];

                // Gradual recovery over time
                state.health = Mathf.Min(1f, state.health + 0.1f);
                state.stability = Mathf.Min(1f, state.stability + 0.15f);
                state.environmentalStress = Mathf.Max(0f, state.environmentalStress - 0.2f);

                biomeStates[biome] = state;
            }

            var recoveryEvent = new EcosystemStoryEvent
            {
                type = EcosystemEventType.EcosystemRecovery,
                description = $"Ecosystems begin to recover from the {disaster.type} disaster",
                timestamp = Time.time,
                affectedBiomes = disaster.affectedBiomes,
                severity = 0.3f
            };

            storyHistory.Add(recoveryEvent);
            OnEcosystemStoryEvent?.Invoke(recoveryEvent);
        }

        #endregion

        #region Public API

        /// <summary>
        /// Gets the current health status of all biomes
        /// </summary>
        public Dictionary<BiomeType, StoryEcosystemState> GetBiomeStates() => new Dictionary<BiomeType, StoryEcosystemState>(biomeStates);

        /// <summary>
        /// Gets information about all tracked species populations
        /// </summary>
        public Dictionary<string, SpeciesPopulation> GetSpeciesPopulations() => new Dictionary<string, SpeciesPopulation>(speciesPopulations);

        /// <summary>
        /// Gets all currently active ecosystem events
        /// </summary>
        public EcosystemEventSummary GetActiveEvents()
        {
            return new EcosystemEventSummary
            {
                apexPredatorEvents = activeApexEvents.ToArray(),
                ecologicalDisasters = activeDisasters.ToArray(),
                populationEvents = activePopulationEvents.ToArray(),
                weatherEvents = activeWeatherEvents.ToArray()
            };
        }

        /// <summary>
        /// Gets recent ecosystem story events
        /// </summary>
        public EcosystemStoryEvent[] GetRecentStoryEvents(int maxEvents = 10)
        {
            return storyHistory.OrderByDescending(e => e.timestamp).Take(maxEvents).ToArray();
        }

        /// <summary>
        /// Records player impact on ecosystem (for tracking conservation efforts)
        /// </summary>
        public void RecordPlayerImpact(string playerId, EcosystemImpactType impactType, float magnitude)
        {
            if (!playerImpactScores.ContainsKey(playerId))
                playerImpactScores[playerId] = 0f;

            var impactValue = impactType switch
            {
                EcosystemImpactType.SpeciesConservation => magnitude,
                EcosystemImpactType.HabitatRestoration => magnitude * 0.8f,
                EcosystemImpactType.OverHunting => -magnitude,
                EcosystemImpactType.PollutionReduction => magnitude * 0.6f,
                EcosystemImpactType.EcosystemDisruption => -magnitude * 1.2f,
                _ => 0f
            };

            playerImpactScores[playerId] += impactValue;
        }

        /// <summary>
        /// Force trigger a specific event (for testing or special occasions)
        /// </summary>
        public void ForceEvent(EcosystemEventType eventType, BiomeType targetBiome)
        {
            switch (eventType)
            {
                case EcosystemEventType.ApexPredatorArrival:
                    var apexType = SelectApexPredatorType(targetBiome);
                    var apexEvent = new ApexPredatorEvent
                    {
                        id = Guid.NewGuid().ToString(),
                        predatorType = apexType,
                        targetBiome = targetBiome,
                        threatLevel = 0.8f,
                        duration = 14,
                        startTime = Time.time,
                        currentPhase = ApexPredatorPhase.Approaching,
                        populationImpact = 0.4f,
                        description = GenerateApexPredatorDescription(apexType, targetBiome)
                    };
                    activeApexEvents.Add(apexEvent);
                    OnApexPredatorAppears?.Invoke(apexEvent);
                    break;

                case EcosystemEventType.EcologicalDisaster:
                    TriggerEcologicalDisaster();
                    break;
            }
        }

        #endregion
    }

    #region Data Structures

    /// <summary>
    /// Represents the current state of an ecosystem biome
    /// </summary>
    [Serializable]
    public struct StoryStoryEcosystemState
    {
        public BiomeType biome;
        public float health; // 0-1, overall ecosystem wellbeing
        public float stability; // 0-1, resistance to change
        public float biodiversity; // 0-1, species variety
        public int carryingCapacity; // Maximum sustainable population
        public float lastMajorEvent; // Timestamp of last major disruption
        public float seasonalModifier; // Current seasonal effect
        public float environmentalStress; // Current stress level

        public string healthDescription => health switch
        {
            > 0.8f => "Thriving",
            > 0.6f => "Healthy",
            > 0.4f => "Stressed",
            > 0.2f => "Struggling",
            _ => "Critical"
        };
    }

    /// <summary>
    /// Tracks population data for a species
    /// </summary>
    [Serializable]
    public struct SpeciesPopulation
    {
        public string speciesId;
        public int currentPopulation;
        public int maxPopulation;
        public float growthRate;
        public BiomeType nativeBiome;
        public float migrationPropensity;
        public float threatLevel; // How dangerous this species is
        public float stress; // Current stress level affecting reproduction
        public float seasonalModifier; // Seasonal population modifier
        public float lastSeenTimestamp;
        public bool isInvasive;
        public bool isNewDiscovery;
        public bool isExtinct;

        public string statusDescription => isExtinct ? "Extinct" :
            currentPopulation == 0 ? "Missing" :
            currentPopulation < maxPopulation * 0.1f ? "Critically Endangered" :
            currentPopulation < maxPopulation * 0.3f ? "Endangered" :
            currentPopulation < maxPopulation * 0.6f ? "Threatened" :
            "Stable";
    }

    /// <summary>
    /// Represents an apex predator event
    /// </summary>
    [Serializable]
    public struct ApexPredatorEvent
    {
        public string id;
        public ApexPredatorType predatorType;
        public BiomeType targetBiome;
        public float threatLevel;
        public int duration;
        public float startTime;
        public ApexPredatorPhase currentPhase;
        public float populationImpact;
        public string description;
    }

    /// <summary>
    /// Types of apex predators
    /// </summary>
    public enum ApexPredatorType
    {
        ShadowStalker,     // Forest predator
        PrimordialDragon,  // Ancient forest dragon
        SandDevourerWorm,  // Desert worm
        CrystallineHuntress, // Desert crystal predator
        AbyssalLeviathan,  // Ocean depths monster
        TemporalShark,     // Time-shifting ocean predator
        StormRider,        // Mountain storm bird
        AvalancheBeaver,   // Mountain engineering beast
        FrostTitan,        // Arctic ice giant
        BlizzardPhantom,   // Arctic ethereal predator
        ChaosBeast         // Reality-warping entity
    }

    /// <summary>
    /// Phases of apex predator events
    /// </summary>
    public enum ApexPredatorPhase
    {
        Approaching,  // Predator is arriving
        Hunting,      // Actively hunting
        Territorial,  // Establishing territory
        Declining,    // Being driven away
        Defeated      // Defeated or fled
    }

    /// <summary>
    /// Types of ecological disasters
    /// </summary>
    public enum EcologicalDisasterType
    {
        VolcanicEruption,
        MassiveFlood,
        ExtremeDrought,
        IceAge,
        ToxicSpill,
        PlagueBlight,
        MeteorImpact,
        SolarFlare
    }

    /// <summary>
    /// Represents an ecological disaster
    /// </summary>
    [Serializable]
    public struct EcologicalDisaster
    {
        public string id;
        public EcologicalDisasterType type;
        public float severity;
        public int duration;
        public BiomeType[] affectedBiomes;
        public float startTime;
        public string description;
        public Dictionary<string, float> environmentalEffects;
    }

    /// <summary>
    /// Types of population events
    /// </summary>
    public enum PopulationEventType
    {
        PopulationBoom,
        PopulationCrash,
        MassiveMigration,
        InvasiveSpecies,
        SpeciesExtinction,
        NewSpeciesDiscovery
    }

    /// <summary>
    /// Represents a population-related event
    /// </summary>
    [Serializable]
    public struct PopulationEvent
    {
        public string id;
        public PopulationEventType type;
        public float magnitude;
        public int duration;
        public string[] affectedSpecies;
        public float startTime;
        public string description;
    }

    /// <summary>
    /// Types of weather events
    /// </summary>
    public enum WeatherEventType
    {
        StormSeason,
        Heatwave,
        Blizzard,
        AuroraDisplay,
        MeteorShower,
        SolarEclipse
    }

    /// <summary>
    /// Represents a weather event
    /// </summary>
    [Serializable]
    public struct WeatherEvent
    {
        public string id;
        public WeatherEventType type;
        public float intensity;
        public int duration;
        public BiomeType[] affectedBiomes;
        public float startTime;
        public string description;
    }

    /// <summary>
    /// Types of ecosystem story events
    /// </summary>
    public enum EcosystemEventType
    {
        ApexPredatorArrival,
        ApexPredatorDefeated,
        EcologicalDisaster,
        EcosystemRecovery,
        SpeciesExtinction,
        NewSpeciesDiscovery,
        PopulationMigration,
        WeatherPhenomena,
        Wildfire,
        VolcanicEruption
    }

    /// <summary>
    /// Represents a story event in the ecosystem
    /// </summary>
    [Serializable]
    public struct EcosystemStoryEvent
    {
        public EcosystemEventType type;
        public string description;
        public float timestamp;
        public BiomeType[] affectedBiomes;
        public float severity;

        public string timeDescription
        {
            get
            {
                var elapsed = Time.time - timestamp;
                var days = elapsed / 86400f;

                return days switch
                {
                    < 1f => "Today",
                    < 2f => "Yesterday",
                    < 7f => $"{Mathf.FloorToInt(days)} days ago",
                    < 30f => $"{Mathf.FloorToInt(days / 7f)} weeks ago",
                    _ => $"{Mathf.FloorToInt(days / 30f)} months ago"
                };
            }
        }
    }

    /// <summary>
    /// Summary of all active ecosystem events
    /// </summary>
    [Serializable]
    public struct EcosystemEventSummary
    {
        public ApexPredatorEvent[] apexPredatorEvents;
        public EcologicalDisaster[] ecologicalDisasters;
        public PopulationEvent[] populationEvents;
        public WeatherEvent[] weatherEvents;

        public int totalActiveEvents => apexPredatorEvents.Length + ecologicalDisasters.Length +
                                      populationEvents.Length + weatherEvents.Length;

        public bool hasActiveThreats => apexPredatorEvents.Length > 0 || ecologicalDisasters.Length > 0;
    }

    /// <summary>
    /// Types of player impact on ecosystem
    /// </summary>
    public enum EcosystemImpactType
    {
        SpeciesConservation,   // Positive: Breeding endangered species
        HabitatRestoration,    // Positive: Improving biome health
        OverHunting,          // Negative: Reducing wild populations
        PollutionReduction,   // Positive: Cleaning environment
        EcosystemDisruption   // Negative: Introducing invasive species
    }

    /// <summary>
    /// Story-specific ecosystem state data
    /// </summary>
    public struct StoryEcosystemState
    {
        public BiomeType biome;
        public float healthLevel;
        public float storyTension;
        public int activeNarratives;
        public float playerInfluence;
        public float health;
        public float stability;
        public float lastMajorEvent;
        public float environmentalStress;
        public float seasonalModifier;
    }

    #endregion
}