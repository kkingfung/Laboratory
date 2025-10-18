using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Laboratory.Chimera.Ecosystem.Data;

namespace Laboratory.Chimera.Ecosystem.Systems
{
    /// <summary>
    /// Monitors ecosystem health metrics including biodiversity, stability, and sustainability
    /// </summary>
    public class EcosystemHealthMonitor : MonoBehaviour
    {
        [Header("Health Monitoring Configuration")]
        [SerializeField] private float assessmentInterval = 30.0f;
        [SerializeField] private float criticalThreshold = 0.3f;
        [SerializeField] private float warningThreshold = 0.5f;
        [SerializeField] private bool enableRealTimeAssessment = true;

        [Header("Biodiversity Metrics")]
        [SerializeField] private float speciesDiversityWeight = 0.3f;
        [SerializeField] private float geneticDiversityWeight = 0.2f;
        [SerializeField] private float functionalDiversityWeight = 0.2f;
        [SerializeField] private float ecosystemComplexityWeight = 0.3f;

        [Header("Stability Indicators")]
        [SerializeField] private int stabilityHistoryLength = 10;
        [SerializeField] private float stabilityVarianceThreshold = 0.2f;
        [SerializeField] private float resilienceDecayRate = 0.05f;

        private EcosystemHealth currentHealth;
        private Dictionary<Vector2, EcosystemHealth> regionalHealth = new();
        private List<EcosystemMetrics> metricsHistory = new();
        private Dictionary<string, float> healthIndicators = new();

        // Dependencies
        private ClimateEvolutionSystem climateSystem;
        private BiomeTransitionSystem biomeSystem;
        private ResourceFlowSystem resourceSystem;
        private SpeciesInteractionSystem speciesSystem;

        // Events
        public System.Action<EcosystemHealth> OnHealthAssessmentComplete;
        public System.Action<string> OnHealthWarning;
        public System.Action<string> OnHealthCritical;
        public System.Action<Vector2, float> OnRegionalHealthChanged;
        public System.Action<EcosystemMetrics> OnMetricsUpdated;

        private void Awake()
        {
            FindDependencies();
            InitializeHealthSystem();
        }

        private void Start()
        {
            StartCoroutine(HealthAssessmentLoop());
        }

        private void FindDependencies()
        {
            climateSystem = FindObjectOfType<ClimateEvolutionSystem>();
            biomeSystem = FindObjectOfType<BiomeTransitionSystem>();
            resourceSystem = FindObjectOfType<ResourceFlowSystem>();
            speciesSystem = FindObjectOfType<SpeciesInteractionSystem>();
        }

        private void InitializeHealthSystem()
        {
            currentHealth = new EcosystemHealth
            {
                BiodiversityIndex = 0.8f,
                TrophicBalance = 0.7f,
                ResourceSustainability = 0.9f,
                PopulationStability = 0.8f,
                HabitatQuality = 0.85f,
                OverallHealthScore = 0.8f,
                Threats = new List<string>(),
                Opportunities = new List<string>(),
                LastAssessment = System.DateTime.Now
            };

            healthIndicators = new Dictionary<string, float>
            {
                ["BiodiversityTrend"] = 0f,
                ["ExtinctionRate"] = 0f,
                ["InvasiveSpecies"] = 0f,
                ["HabitatFragmentation"] = 0f,
                ["PollutionLevel"] = 0f,
                ["ClimateStress"] = 0f,
                ["ResourceDepletion"] = 0f,
                ["PopulationVariability"] = 0f
            };

            Debug.Log("🌿 Ecosystem health monitoring system initialized");
        }

        private IEnumerator HealthAssessmentLoop()
        {
            while (enableRealTimeAssessment)
            {
                PerformHealthAssessment();
                UpdateRegionalHealth();
                CheckHealthThresholds();
                UpdateMetricsHistory();

                yield return new WaitForSeconds(assessmentInterval);
            }
        }

        private void PerformHealthAssessment()
        {
            var metrics = CollectEcosystemMetrics();
            var health = CalculateEcosystemHealth(metrics);

            currentHealth = health;
            OnHealthAssessmentComplete?.Invoke(health);

            Debug.Log($"🌿 Ecosystem health assessment: {health.OverallHealthScore:F2} " +
                     $"(Biodiversity: {health.BiodiversityIndex:F2}, Stability: {health.PopulationStability:F2})");
        }

        private EcosystemMetrics CollectEcosystemMetrics()
        {
            var metrics = new EcosystemMetrics();

            // Collect species data
            if (speciesSystem != null)
            {
                var populations = speciesSystem.GetAllPopulations();
                metrics.TotalSpeciesCount = populations.Count;
                metrics.AveragePopulationSize = populations.Count > 0 ? populations.Values.Average() : 0f;

                // Count endangered species (below 20% of carrying capacity)
                metrics.EndangeredSpeciesCount = 0;
                foreach (var speciesId in populations.Keys)
                {
                    var popData = speciesSystem.GetPopulationData(speciesId);
                    if (popData.CurrentPopulation < popData.MaxPopulation * 0.2f)
                    {
                        metrics.EndangeredSpeciesCount++;
                    }
                }

                // Calculate population stability
                metrics.PopulationStability = CalculatePopulationStability(populations);
            }

            // Collect biome distribution
            if (biomeSystem != null)
            {
                var biomeDistribution = biomeSystem.GetBiomeDistribution();
                metrics.BiomeDistribution = biomeDistribution.ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value / (float)biomeDistribution.Values.Sum()
                );
            }

            // Collect resource data
            if (resourceSystem != null)
            {
                var globalResources = resourceSystem.GetGlobalResourceLevels();
                float totalResourceUtilization = 0f;
                foreach (var resource in globalResources.Values)
                {
                    totalResourceUtilization += Mathf.Clamp01(resource / 1000f); // Normalize to capacity
                }
                metrics.CarryingCapacityUtilization = globalResources.Count > 0 ?
                    totalResourceUtilization / globalResources.Count : 0f;
            }

            // Calculate derived metrics
            metrics.ExtinctionRate = CalculateExtinctionRate();
            metrics.SpeciationRate = CalculateSpeciationRate();
            metrics.GeneticDiversity = CalculateGeneticDiversity();
            metrics.EcosystemResilience = CalculateEcosystemResilience();

            return metrics;
        }

        private EcosystemHealth CalculateEcosystemHealth(EcosystemMetrics metrics)
        {
            var health = new EcosystemHealth();

            // Calculate biodiversity index
            health.BiodiversityIndex = CalculateBiodiversityIndex(metrics);

            // Calculate trophic balance
            health.TrophicBalance = CalculateTrophicBalance(metrics);

            // Calculate resource sustainability
            health.ResourceSustainability = CalculateResourceSustainability(metrics);

            // Population stability
            health.PopulationStability = metrics.PopulationStability;

            // Habitat quality
            health.HabitatQuality = CalculateHabitatQuality(metrics);

            // Overall health score (weighted average)
            health.OverallHealthScore = (
                health.BiodiversityIndex * speciesDiversityWeight +
                health.TrophicBalance * functionalDiversityWeight +
                health.ResourceSustainability * 0.25f +
                health.PopulationStability * 0.25f +
                health.HabitatQuality * 0.25f
            );

            // Identify threats and opportunities
            health.Threats = IdentifyThreats(metrics, health);
            health.Opportunities = IdentifyOpportunities(metrics, health);
            health.LastAssessment = System.DateTime.Now;

            return health;
        }

        private float CalculateBiodiversityIndex(EcosystemMetrics metrics)
        {
            // Shannon-Weaver diversity index approximation
            float speciesRichness = metrics.TotalSpeciesCount / 50f; // Normalize to expected maximum
            float evenness = CalculateSpeciesEvenness();
            float geneticComponent = metrics.GeneticDiversity;

            return Mathf.Clamp01((speciesRichness * 0.4f + evenness * 0.4f + geneticComponent * 0.2f));
        }

        private float CalculateSpeciesEvenness()
        {
            if (speciesSystem == null) return 0.5f;

            var populations = speciesSystem.GetAllPopulations();
            if (populations.Count == 0) return 0f;

            float totalPopulation = populations.Values.Sum();
            if (totalPopulation == 0) return 0f;

            // Calculate Shannon evenness
            float shannonH = 0f;
            foreach (var population in populations.Values)
            {
                if (population > 0)
                {
                    float proportion = population / totalPopulation;
                    shannonH -= proportion * Mathf.Log(proportion);
                }
            }

            float maxH = Mathf.Log(populations.Count);
            return maxH > 0 ? shannonH / maxH : 0f;
        }

        private float CalculateTrophicBalance(EcosystemMetrics metrics)
        {
            if (metrics.TrophicDistribution == null || metrics.TrophicDistribution.Count == 0)
                return 0.5f;

            // Ideal trophic pyramid: many producers, fewer consumers at each level
            var idealDistribution = new Dictionary<TrophicLevel, float>
            {
                [TrophicLevel.Producer] = 0.4f,
                [TrophicLevel.PrimaryConsumer] = 0.3f,
                [TrophicLevel.SecondaryConsumer] = 0.2f,
                [TrophicLevel.TertiaryConsumer] = 0.1f
            };

            float totalSpecies = metrics.TrophicDistribution.Values.Sum();
            if (totalSpecies == 0) return 0f;

            float balance = 0f;
            foreach (var kvp in idealDistribution)
            {
                float actualProportion = metrics.TrophicDistribution.GetValueOrDefault(kvp.Key, 0) / totalSpecies;
                float difference = Mathf.Abs(actualProportion - kvp.Value);
                balance += 1f - difference;
            }

            return balance / idealDistribution.Count;
        }

        private float CalculateResourceSustainability(EcosystemMetrics metrics)
        {
            float carryingCapacityScore = 1f - Mathf.Abs(metrics.CarryingCapacityUtilization - 0.7f) / 0.7f;
            float extinctionRateScore = 1f - Mathf.Min(metrics.ExtinctionRate * 10f, 1f);
            float resourceAvailabilityScore = CalculateResourceAvailabilityScore();

            return (carryingCapacityScore + extinctionRateScore + resourceAvailabilityScore) / 3f;
        }

        private float CalculateResourceAvailabilityScore()
        {
            if (resourceSystem == null) return 0.5f;

            var globalResources = resourceSystem.GetGlobalResourceLevels();
            if (globalResources.Count == 0) return 0f;

            float totalScore = 0f;
            foreach (var resource in globalResources.Values)
            {
                float normalizedAvailability = Mathf.Clamp01(resource / 500f); // Normalize to baseline
                totalScore += normalizedAvailability;
            }

            return totalScore / globalResources.Count;
        }

        private float CalculateHabitatQuality(EcosystemMetrics metrics)
        {
            float biomeStability = CalculateBiomeStability();
            float climateStability = CalculateClimateStability();
            float fragmentationScore = 1f - healthIndicators.GetValueOrDefault("HabitatFragmentation", 0f);

            return (biomeStability + climateStability + fragmentationScore) / 3f;
        }

        private float CalculateBiomeStability()
        {
            if (biomeSystem == null) return 0.5f;

            var activeTransitions = biomeSystem.GetActiveTransitions();
            float transitionPressure = activeTransitions.Count / 10f; // Normalize to expected maximum
            return Mathf.Clamp01(1f - transitionPressure);
        }

        private float CalculateClimateStability()
        {
            if (climateSystem == null) return 0.5f;

            var climate = climateSystem.GetCurrentClimate();
            return climate.ClimateStability;
        }

        private float CalculatePopulationStability(Dictionary<uint, float> populations)
        {
            if (populations.Count == 0) return 0f;

            float totalVariability = 0f;
            foreach (var speciesId in populations.Keys)
            {
                var popData = speciesSystem.GetPopulationData(speciesId);
                float variability = Mathf.Abs(popData.CurrentPopulation - popData.MaxPopulation * 0.7f) / popData.MaxPopulation;
                totalVariability += variability;
            }

            float averageVariability = totalVariability / populations.Count;
            return Mathf.Clamp01(1f - averageVariability);
        }

        private float CalculateExtinctionRate()
        {
            // Simplified calculation - would track actual extinctions over time
            return healthIndicators.GetValueOrDefault("ExtinctionRate", 0.02f);
        }

        private float CalculateSpeciationRate()
        {
            // Simplified calculation - would track new species emergence
            return 0.01f; // Placeholder
        }

        private float CalculateGeneticDiversity()
        {
            // Simplified calculation - would analyze actual genetic profiles
            return 0.7f; // Placeholder
        }

        private float CalculateEcosystemResilience()
        {
            if (metricsHistory.Count < 2) return 0.5f;

            // Calculate how quickly ecosystem recovers from disturbances
            float stabilityVariance = CalculateStabilityVariance();
            float recoverySpeed = CalculateRecoverySpeed();

            return (1f - stabilityVariance) * 0.6f + recoverySpeed * 0.4f;
        }

        private float CalculateStabilityVariance()
        {
            if (metricsHistory.Count < 2) return 0f;

            var recentMetrics = metricsHistory.TakeLast(stabilityHistoryLength);
            var stabilities = recentMetrics.Select(m => m.PopulationStability).ToArray();

            if (stabilities.Length < 2) return 0f;

            float mean = stabilities.Average();
            float variance = stabilities.Select(s => Mathf.Pow(s - mean, 2)).Average();

            return Mathf.Clamp01(variance / stabilityVarianceThreshold);
        }

        private float CalculateRecoverySpeed()
        {
            // Simplified recovery speed calculation
            return 0.6f; // Placeholder
        }

        private List<string> IdentifyThreats(EcosystemMetrics metrics, EcosystemHealth health)
        {
            var threats = new List<string>();

            if (health.BiodiversityIndex < warningThreshold)
                threats.Add("Low biodiversity");

            if (metrics.ExtinctionRate > 0.05f)
                threats.Add("High extinction rate");

            if (health.ResourceSustainability < warningThreshold)
                threats.Add("Resource depletion");

            if (health.PopulationStability < warningThreshold)
                threats.Add("Population instability");

            if (climateSystem != null && climateSystem.GetCurrentClimate().ClimateStability < warningThreshold)
                threats.Add("Climate instability");

            if (healthIndicators.GetValueOrDefault("PollutionLevel", 0f) > warningThreshold)
                threats.Add("High pollution levels");

            return threats;
        }

        private List<string> IdentifyOpportunities(EcosystemMetrics metrics, EcosystemHealth health)
        {
            var opportunities = new List<string>();

            if (health.BiodiversityIndex > 0.8f)
                opportunities.Add("High biodiversity supports resilience");

            if (metrics.SpeciationRate > 0.02f)
                opportunities.Add("Active speciation occurring");

            if (health.ResourceSustainability > 0.8f)
                opportunities.Add("Sustainable resource use");

            if (health.HabitatQuality > 0.8f)
                opportunities.Add("High-quality habitats available");

            if (metrics.EcosystemResilience > 0.7f)
                opportunities.Add("Strong ecosystem resilience");

            return opportunities;
        }

        private void UpdateRegionalHealth()
        {
            // Simplified regional health calculation
            for (int x = -50; x <= 50; x += 25)
            {
                for (int y = -50; y <= 50; y += 25)
                {
                    var location = new Vector2(x, y);
                    var regionalMetrics = CollectRegionalMetrics(location);
                    var regionalHealthValue = CalculateRegionalHealth(regionalMetrics);

                    regionalHealth[location] = new EcosystemHealth
                    {
                        OverallHealthScore = regionalHealthValue,
                        LastAssessment = System.DateTime.Now
                    };

                    OnRegionalHealthChanged?.Invoke(location, regionalHealthValue);
                }
            }
        }

        private EcosystemMetrics CollectRegionalMetrics(Vector2 location)
        {
            // Simplified regional metrics collection
            return new EcosystemMetrics
            {
                TotalSpeciesCount = Random.Range(5, 15),
                AveragePopulationSize = Random.Range(50f, 200f),
                PopulationStability = Random.Range(0.3f, 0.9f)
            };
        }

        private float CalculateRegionalHealth(EcosystemMetrics metrics)
        {
            // Simplified regional health calculation
            return Random.Range(0.4f, 0.9f);
        }

        private void CheckHealthThresholds()
        {
            if (currentHealth.OverallHealthScore < criticalThreshold)
            {
                OnHealthCritical?.Invoke($"Ecosystem health critical: {currentHealth.OverallHealthScore:F2}");
            }
            else if (currentHealth.OverallHealthScore < warningThreshold)
            {
                OnHealthWarning?.Invoke($"Ecosystem health warning: {currentHealth.OverallHealthScore:F2}");
            }

            // Check individual component thresholds
            CheckComponentThresholds();
        }

        private void CheckComponentThresholds()
        {
            if (currentHealth.BiodiversityIndex < criticalThreshold)
                OnHealthCritical?.Invoke("Biodiversity crisis detected");

            if (currentHealth.ResourceSustainability < criticalThreshold)
                OnHealthCritical?.Invoke("Resource sustainability crisis");

            if (currentHealth.PopulationStability < criticalThreshold)
                OnHealthWarning?.Invoke("Population instability detected");
        }

        private void UpdateMetricsHistory()
        {
            var currentMetrics = CollectEcosystemMetrics();
            metricsHistory.Add(currentMetrics);

            // Limit history size
            if (metricsHistory.Count > 100)
            {
                metricsHistory.RemoveAt(0);
            }

            OnMetricsUpdated?.Invoke(currentMetrics);
        }

        public EcosystemHealth GetCurrentHealth() => currentHealth;
        public Dictionary<Vector2, EcosystemHealth> GetRegionalHealth() => new Dictionary<Vector2, EcosystemHealth>(regionalHealth);
        public List<EcosystemMetrics> GetMetricsHistory() => new List<EcosystemMetrics>(metricsHistory);
        public Dictionary<string, float> GetHealthIndicators() => new Dictionary<string, float>(healthIndicators);

        public void SetHealthIndicator(string indicator, float value)
        {
            healthIndicators[indicator] = Mathf.Clamp01(value);
        }

        public void TriggerHealthAssessment()
        {
            PerformHealthAssessment();
        }

        private void OnDestroy()
        {
            StopAllCoroutines();
        }
    }
}