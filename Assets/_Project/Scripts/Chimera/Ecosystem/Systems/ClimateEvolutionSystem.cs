using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Laboratory.Chimera.Ecosystem.Data;

namespace Laboratory.Chimera.Ecosystem.Systems
{
    /// <summary>
    /// Manages global climate evolution, seasonal cycles, and weather patterns
    /// </summary>
    public class ClimateEvolutionSystem : MonoBehaviour
    {
        [Header("Climate Configuration")]
        [SerializeField] private float climateEvolutionRate = 0.1f;
        [SerializeField] private float seasonalCycleSpeed = 1.0f;
        [SerializeField] private bool enableClimateChange = true;
        [SerializeField] private bool enableWeatherSystems = true;

        [Header("Temperature Settings")]
        [SerializeField] private float baseGlobalTemperature = 15.0f;
        [SerializeField] private float temperatureVariance = 5.0f;
        [SerializeField] private float climateChangeRate = 0.01f;

        [Header("Weather Patterns")]
        [SerializeField] private int maxActiveWeatherSystems = 10;
        [SerializeField] private float weatherFormationRate = 0.1f;
        [SerializeField] private float weatherDissipationRate = 0.05f;

        private ClimateData currentClimate;
        private SeasonType currentSeason = SeasonType.Spring;
        private float seasonProgress = 0f;
        private List<WeatherPattern> activeWeatherPatterns = new();

        // Events
        public System.Action<ClimateData> OnClimateChanged;
        public System.Action<SeasonType> OnSeasonChanged;
        public System.Action<WeatherPattern> OnWeatherSystemFormed;
        public System.Action<WeatherPattern> OnWeatherSystemDissipated;

        private void Awake()
        {
            InitializeClimate();
        }

        private void Start()
        {
            StartCoroutine(ClimateEvolutionLoop());
        }

        private void InitializeClimate()
        {
            currentClimate = new ClimateData
            {
                GlobalTemperature = baseGlobalTemperature,
                SeaLevel = 0f,
                AtmosphericCO2 = 400f,
                OzoneLevel = 100f,
                TemperatureRange = new Vector2(-10f, 40f),
                ClimateStability = 0.8f,
                ActivePatterns = new List<WeatherPattern>(),
                ClimateChangeRate = climateChangeRate
            };

            Debug.Log("🌍 Climate evolution system initialized");
        }

        private IEnumerator ClimateEvolutionLoop()
        {
            while (true)
            {
                UpdateSeasonalCycle();

                if (enableClimateChange)
                {
                    UpdateGlobalClimate();
                }

                if (enableWeatherSystems)
                {
                    UpdateWeatherSystems();
                }

                OnClimateChanged?.Invoke(currentClimate);

                yield return new WaitForSeconds(1f / climateEvolutionRate);
            }
        }

        private void UpdateSeasonalCycle()
        {
            seasonProgress += Time.deltaTime * seasonalCycleSpeed * 0.1f;

            if (seasonProgress >= 1f)
            {
                seasonProgress = 0f;
                AdvanceSeason();
            }

            ApplySeasonalEffects();
        }

        private void AdvanceSeason()
        {
            var previousSeason = currentSeason;
            currentSeason = (SeasonType)(((int)currentSeason + 1) % 4);

            if (currentSeason != previousSeason)
            {
                Debug.Log($"🍂 Season changed from {previousSeason} to {currentSeason}");
                OnSeasonChanged?.Invoke(currentSeason);
            }
        }

        private void ApplySeasonalEffects()
        {
            float seasonalTempModifier = GetSeasonalTemperatureModifier();
            float seasonalHumidityModifier = GetSeasonalHumidityModifier();

            // Apply seasonal temperature changes
            float targetTemperature = baseGlobalTemperature + seasonalTempModifier;
            currentClimate.GlobalTemperature = Mathf.Lerp(
                currentClimate.GlobalTemperature,
                targetTemperature,
                Time.deltaTime * 0.1f
            );

            // Update temperature range based on season
            currentClimate.TemperatureRange = new Vector2(
                currentClimate.GlobalTemperature - temperatureVariance * (1f + seasonalTempModifier * 0.1f),
                currentClimate.GlobalTemperature + temperatureVariance * (1f + seasonalTempModifier * 0.1f)
            );
        }

        private float GetSeasonalTemperatureModifier()
        {
            return currentSeason switch
            {
                SeasonType.Spring => Mathf.Sin(seasonProgress * Mathf.PI) * 2f,
                SeasonType.Summer => 5f + Mathf.Sin(seasonProgress * Mathf.PI) * 3f,
                SeasonType.Autumn => Mathf.Sin(seasonProgress * Mathf.PI) * -2f,
                SeasonType.Winter => -8f + Mathf.Sin(seasonProgress * Mathf.PI) * -2f,
                _ => 0f
            };
        }

        private float GetSeasonalHumidityModifier()
        {
            return currentSeason switch
            {
                SeasonType.Spring => 0.3f,
                SeasonType.Summer => -0.2f,
                SeasonType.Autumn => 0.1f,
                SeasonType.Winter => 0.4f,
                _ => 0f
            };
        }

        private void UpdateGlobalClimate()
        {
            // Gradual climate change over time
            currentClimate.GlobalTemperature += climateChangeRate * Time.deltaTime * 0.1f;
            currentClimate.AtmosphericCO2 += Random.Range(-0.1f, 0.2f) * Time.deltaTime;
            currentClimate.SeaLevel += climateChangeRate * 0.5f * Time.deltaTime;

            // Update climate stability based on change rate
            float changeRate = Mathf.Abs(climateChangeRate);
            currentClimate.ClimateStability = Mathf.Lerp(
                currentClimate.ClimateStability,
                Mathf.Max(0.1f, 1f - changeRate * 10f),
                Time.deltaTime * 0.01f
            );

            // Clamp values to reasonable ranges
            currentClimate.AtmosphericCO2 = Mathf.Clamp(currentClimate.AtmosphericCO2, 280f, 1000f);
            currentClimate.SeaLevel = Mathf.Clamp(currentClimate.SeaLevel, -10f, 50f);
        }

        private void UpdateWeatherSystems()
        {
            // Update existing weather patterns
            for (int i = activeWeatherPatterns.Count - 1; i >= 0; i--)
            {
                var pattern = activeWeatherPatterns[i];
                pattern = UpdateWeatherPattern(pattern);

                if (pattern.Duration <= 0f || !pattern.IsActive)
                {
                    OnWeatherSystemDissipated?.Invoke(pattern);
                    activeWeatherPatterns.RemoveAt(i);
                }
                else
                {
                    activeWeatherPatterns[i] = pattern;
                }
            }

            // Generate new weather systems
            if (activeWeatherPatterns.Count < maxActiveWeatherSystems && Random.value < weatherFormationRate * Time.deltaTime)
            {
                var newPattern = GenerateWeatherPattern();
                if (newPattern.IsActive)
                {
                    activeWeatherPatterns.Add(newPattern);
                    OnWeatherSystemFormed?.Invoke(newPattern);
                }
            }

            currentClimate.ActivePatterns = new List<WeatherPattern>(activeWeatherPatterns);
        }

        private WeatherPattern UpdateWeatherPattern(WeatherPattern pattern)
        {
            // Reduce duration
            pattern.Duration -= Time.deltaTime;

            // Move the pattern
            pattern.Location += pattern.MovementDirection * pattern.Speed * Time.deltaTime;

            // Update intensity based on local conditions
            float intensityChange = Random.Range(-0.1f, 0.1f) * Time.deltaTime;
            pattern.Intensity = Mathf.Clamp01(pattern.Intensity + intensityChange);

            // Apply seasonal modifiers
            float seasonalModifier = GetWeatherSeasonalModifier(pattern.Type);
            pattern.Intensity *= seasonalModifier;

            // Check if pattern should dissipate
            if (pattern.Intensity < 0.1f || Random.value < weatherDissipationRate * Time.deltaTime)
            {
                pattern.IsActive = false;
            }

            return pattern;
        }

        private WeatherPattern GenerateWeatherPattern()
        {
            var weatherTypes = System.Enum.GetValues(typeof(WeatherType));
            var randomType = (WeatherType)weatherTypes.GetValue(Random.Range(0, weatherTypes.Length));

            var pattern = new WeatherPattern
            {
                Type = randomType,
                Location = new Vector2(Random.Range(-100f, 100f), Random.Range(-100f, 100f)),
                Intensity = Random.Range(0.3f, 1.0f),
                Duration = Random.Range(300f, 1800f), // 5-30 minutes
                MovementDirection = Random.insideUnitCircle.normalized,
                Speed = Random.Range(0.1f, 2.0f),
                IsActive = true,
                LocalEffects = GenerateWeatherEffects(randomType)
            };

            // Apply seasonal probability modifiers
            float seasonalProbability = GetWeatherSeasonalProbability(randomType);
            if (Random.value > seasonalProbability)
            {
                pattern.IsActive = false;
            }

            return pattern;
        }

        private Dictionary<string, float> GenerateWeatherEffects(WeatherType weatherType)
        {
            var effects = new Dictionary<string, float>();

            switch (weatherType)
            {
                case WeatherType.Rain:
                    effects["Humidity"] = 0.3f;
                    effects["Temperature"] = -0.1f;
                    effects["Visibility"] = -0.2f;
                    break;
                case WeatherType.Snow:
                    effects["Temperature"] = -0.4f;
                    effects["Humidity"] = 0.1f;
                    effects["Visibility"] = -0.5f;
                    break;
                case WeatherType.Storm:
                    effects["Wind"] = 0.8f;
                    effects["Humidity"] = 0.5f;
                    effects["Temperature"] = -0.2f;
                    effects["Danger"] = 0.6f;
                    break;
                case WeatherType.Drought:
                    effects["Humidity"] = -0.7f;
                    effects["Temperature"] = 0.3f;
                    effects["Water"] = -0.8f;
                    break;
                case WeatherType.Heatwave:
                    effects["Temperature"] = 0.6f;
                    effects["Humidity"] = -0.3f;
                    effects["Water"] = -0.4f;
                    break;
            }

            return effects;
        }

        private float GetWeatherSeasonalModifier(WeatherType weatherType)
        {
            return currentSeason switch
            {
                SeasonType.Spring when weatherType == WeatherType.Rain => 1.5f,
                SeasonType.Summer when weatherType == WeatherType.Heatwave => 1.8f,
                SeasonType.Summer when weatherType == WeatherType.Storm => 1.3f,
                SeasonType.Autumn when weatherType == WeatherType.Wind => 1.4f,
                SeasonType.Winter when weatherType == WeatherType.Snow => 2.0f,
                SeasonType.Winter when weatherType == WeatherType.Cold => 1.6f,
                _ => 1.0f
            };
        }

        private float GetWeatherSeasonalProbability(WeatherType weatherType)
        {
            return (currentSeason, weatherType) switch
            {
                (SeasonType.Spring, WeatherType.Rain) => 0.8f,
                (SeasonType.Summer, WeatherType.Heatwave) => 0.7f,
                (SeasonType.Summer, WeatherType.Storm) => 0.6f,
                (SeasonType.Autumn, WeatherType.Wind) => 0.7f,
                (SeasonType.Winter, WeatherType.Snow) => 0.9f,
                (SeasonType.Winter, WeatherType.Cold) => 0.8f,
                _ => 0.5f
            };
        }

        public ClimateData GetCurrentClimate() => currentClimate;
        public SeasonType GetCurrentSeason() => currentSeason;
        public float GetSeasonProgress() => seasonProgress;
        public List<WeatherPattern> GetActiveWeatherPatterns() => new List<WeatherPattern>(activeWeatherPatterns);

        public void SetClimateChangeRate(float rate)
        {
            climateChangeRate = Mathf.Clamp(rate, -1f, 1f);
            currentClimate.ClimateChangeRate = climateChangeRate;
        }

        public void TriggerWeatherEvent(WeatherType type, Vector2 location, float intensity, float duration)
        {
            if (activeWeatherPatterns.Count >= maxActiveWeatherSystems) return;

            var customPattern = new WeatherPattern
            {
                Type = type,
                Location = location,
                Intensity = Mathf.Clamp01(intensity),
                Duration = duration,
                MovementDirection = Random.insideUnitCircle.normalized,
                Speed = Random.Range(0.1f, 1.0f),
                IsActive = true,
                LocalEffects = GenerateWeatherEffects(type)
            };

            activeWeatherPatterns.Add(customPattern);
            OnWeatherSystemFormed?.Invoke(customPattern);
            Debug.Log($"🌦️ Triggered weather event: {type} at {location}");
        }

        private void OnDestroy()
        {
            StopAllCoroutines();
        }
    }
}