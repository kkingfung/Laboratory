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
    /// Weather system for dynamic environmental conditions.
    /// Manages weather patterns, transitions, and their effects on the ecosystem.
    /// </summary>
    public class WeatherSystem : MonoBehaviour, IWeatherService
    {
        [Header("Current Weather")]
        [SerializeField] private WeatherType currentWeatherType = WeatherType.Sunny;
        [SerializeField] [Range(0f, 1f)] private float currentIntensity = 0.5f;
        [SerializeField] private float remainingDuration = 300f;

        [Header("Weather Settings")]
        [SerializeField] private bool enableDynamicWeather = true;
        [SerializeField] [Range(60f, 3600f)] private float minWeatherDuration = 300f;
        [SerializeField] [Range(300f, 7200f)] private float maxWeatherDuration = 1800f;
        [SerializeField] [Range(5f, 60f)] private float transitionDuration = 15f;

        [Header("Weather Probabilities")]
        [SerializeField] [Range(0f, 1f)] private float clearWeatherChance = 0.4f;
        [SerializeField] [Range(0f, 1f)] private float cloudyWeatherChance = 0.3f;
        [SerializeField] [Range(0f, 1f)] private float rainWeatherChance = 0.2f;
        [SerializeField] [Range(0f, 1f)] private float stormWeatherChance = 0.1f;

        // Current weather state
        private WeatherData _currentWeather;
        private WeatherData _targetWeather;
        private bool _isTransitioning = false;
        private float _transitionProgress = 0f;
        private Coroutine _weatherUpdateCoroutine;

        // Weather effects
        private readonly Dictionary<WeatherType, WeatherEffect> _weatherEffects = new();

        // Events
        public event Action<WeatherEvent> OnWeatherChanged;
        public event Action<WeatherData> OnWeatherUpdated;

        // Properties
        public WeatherData CurrentWeather => _currentWeather;
        public bool IsTransitioning => _isTransitioning;

        #region Unity Lifecycle

        private void Awake()
        {
            InitializeWeatherData();
            SetupWeatherEffects();
        }

        private void Start()
        {
            if (enableDynamicWeather)
            {
                StartWeatherSystem();
            }
        }

        private void OnDestroy()
        {
            StopWeatherSystem();
        }

        #endregion

        #region Initialization

        public void Initialize(EcosystemSubsystemConfig config)
        {
            if (config?.WeatherConfig != null)
            {
                enableDynamicWeather = config.WeatherConfig.EnableDynamicWeather;
                minWeatherDuration = config.WeatherConfig.WeatherChangeFrequency * 60f; // Convert hours to minutes
                maxWeatherDuration = config.WeatherConfig.WeatherChangeFrequency * 120f; // Convert hours to minutes
                transitionDuration = 15f; // Default transition duration since not available in config
            }

            InitializeWeatherData();
            Debug.Log($"[WeatherSystem] Initialized - Dynamic: {enableDynamicWeather}");
        }

        public async Task InitializeAsync(EcosystemSubsystemConfig config)
        {
            Initialize(config);
            await Task.CompletedTask;
        }

        private void InitializeWeatherData()
        {
            _currentWeather = new WeatherData
            {
                weatherType = currentWeatherType,
                intensity = currentIntensity,
                remainingDuration = remainingDuration,
                windDirection = UnityEngine.Random.insideUnitSphere.normalized,
                temperature = CalculateTemperature(currentWeatherType, currentIntensity),
                humidity = CalculateHumidity(currentWeatherType, currentIntensity),
                startTime = DateTime.UtcNow
            };

            _targetWeather = _currentWeather;
        }

        private void SetupWeatherEffects()
        {
            _weatherEffects[WeatherType.Sunny] = new WeatherEffect
            {
                lightIntensity = 1f,
                fogDensity = 0f,
                windStrength = 0.2f,
                particleIntensity = 0f
            };

            _weatherEffects[WeatherType.Cloudy] = new WeatherEffect
            {
                lightIntensity = 0.7f,
                fogDensity = 0.1f,
                windStrength = 0.4f,
                particleIntensity = 0f
            };

            _weatherEffects[WeatherType.Rainy] = new WeatherEffect
            {
                lightIntensity = 0.5f,
                fogDensity = 0.3f,
                windStrength = 0.6f,
                particleIntensity = 0.8f
            };

            _weatherEffects[WeatherType.Stormy] = new WeatherEffect
            {
                lightIntensity = 0.3f,
                fogDensity = 0.5f,
                windStrength = 1f,
                particleIntensity = 1f
            };

            _weatherEffects[WeatherType.Snowy] = new WeatherEffect
            {
                lightIntensity = 0.8f,
                fogDensity = 0.2f,
                windStrength = 0.3f,
                particleIntensity = 0.6f
            };

            _weatherEffects[WeatherType.Foggy] = new WeatherEffect
            {
                lightIntensity = 0.4f,
                fogDensity = 0.9f,
                windStrength = 0.1f,
                particleIntensity = 0.2f
            };
        }

        #endregion

        #region Weather Control

        public void SetWeather(WeatherType weatherType, float intensity, float duration)
        {
            if (intensity < 0f || intensity > 1f)
            {
                Debug.LogWarning($"[WeatherSystem] Invalid intensity: {intensity}. Clamping to 0-1 range.");
                intensity = Mathf.Clamp01(intensity);
            }

            var newWeather = new WeatherData
            {
                weatherType = weatherType,
                intensity = intensity,
                remainingDuration = duration,
                windDirection = GenerateWindDirection(weatherType),
                temperature = CalculateTemperature(weatherType, intensity),
                humidity = CalculateHumidity(weatherType, intensity),
                startTime = DateTime.UtcNow
            };

            TransitionToWeather(newWeather);
        }

        public void TransitionToWeather(WeatherData targetWeather)
        {
            if (_isTransitioning)
            {
                Debug.LogWarning("[WeatherSystem] Weather transition already in progress");
                return;
            }

            _targetWeather = targetWeather;
            StartCoroutine(WeatherTransitionCoroutine());

            var weatherEvent = new WeatherEvent
            {
                previousWeatherData = _currentWeather,
                newWeatherData = targetWeather,
                changeType = WeatherChangeType.TypeChange,
                affectedBiomes = new List<string>(),
                description = $"Weather transition from {_currentWeather.weatherType} to {targetWeather.weatherType}",
                timestamp = DateTime.UtcNow
            };

            OnWeatherChanged?.Invoke(weatherEvent);
            Debug.Log($"[WeatherSystem] Starting transition from {_currentWeather.weatherType} to {targetWeather.weatherType}");
        }

        public void ForceWeatherChange(WeatherType weatherType, float intensity = 0.5f)
        {
            var immediateWeather = new WeatherData
            {
                weatherType = weatherType,
                intensity = intensity,
                remainingDuration = UnityEngine.Random.Range(minWeatherDuration, maxWeatherDuration),
                windDirection = GenerateWindDirection(weatherType),
                temperature = CalculateTemperature(weatherType, intensity),
                humidity = CalculateHumidity(weatherType, intensity),
                startTime = DateTime.UtcNow
            };

            _currentWeather = immediateWeather;
            _targetWeather = immediateWeather;

            ApplyWeatherEffects();
            OnWeatherUpdated?.Invoke(_currentWeather);

            Debug.Log($"[WeatherSystem] Forced weather change to {weatherType}");
        }

        #endregion

        #region Weather System Management

        public void StartWeatherSystem()
        {
            if (_weatherUpdateCoroutine != null)
            {
                StopCoroutine(_weatherUpdateCoroutine);
            }

            _weatherUpdateCoroutine = StartCoroutine(WeatherUpdateCoroutine());
            Debug.Log("[WeatherSystem] Weather system started");
        }

        public void StopWeatherSystem()
        {
            if (_weatherUpdateCoroutine != null)
            {
                StopCoroutine(_weatherUpdateCoroutine);
                _weatherUpdateCoroutine = null;
            }

            Debug.Log("[WeatherSystem] Weather system stopped");
        }

        public void PauseWeatherSystem()
        {
            enabled = false;
            Debug.Log("[WeatherSystem] Weather system paused");
        }

        public void ResumeWeatherSystem()
        {
            enabled = true;
            Debug.Log("[WeatherSystem] Weather system resumed");
        }

        #endregion

        #region Weather Data Access

        public WeatherType GetCurrentWeatherType()
        {
            return _currentWeather.weatherType;
        }

        public float GetCurrentIntensity()
        {
            return _currentWeather.intensity;
        }

        public float GetRemainingDuration()
        {
            return _currentWeather.remainingDuration;
        }

        public Vector3 GetWindDirection()
        {
            return _currentWeather.windDirection;
        }

        public float GetTemperature()
        {
            return _currentWeather.temperature;
        }

        public float GetHumidity()
        {
            return _currentWeather.humidity;
        }

        // Interface implementation
        public WeatherData GetCurrentWeather()
        {
            return _currentWeather;
        }

        public WeatherForecast GetWeatherForecast(float hoursAhead = 24f)
        {
            // Simple forecast implementation - in a real system this would be more sophisticated
            var predictions = new List<WeatherPrediction>();
            var currentTime = 0f;
            var currentWeatherType = _currentWeather.weatherType;

            while (currentTime < hoursAhead)
            {
                // Simple prediction: weather changes every 4-8 hours
                var nextChangeTime = currentTime + UnityEngine.Random.Range(4f, 8f);
                if (nextChangeTime > hoursAhead)
                    nextChangeTime = hoursAhead;

                // Predict next weather type based on seasonal probabilities
                var nextWeatherType = PredictNextWeatherType(currentWeatherType);

                predictions.Add(new WeatherPrediction
                {
                    hoursFromNow = nextChangeTime,
                    predictedWeather = nextWeatherType,
                    expectedIntensity = UnityEngine.Random.Range(0.3f, 0.9f),
                    confidence = UnityEngine.Random.Range(0.6f, 0.9f)
                });

                currentTime = nextChangeTime;
                currentWeatherType = nextWeatherType;
            }

            return new WeatherForecast
            {
                predictions = predictions.ToArray(),
                confidence = predictions.Count > 0 ? predictions.Average(p => p.confidence) : 1f,
                warnings = new string[0]
            };
        }

        private WeatherType PredictNextWeatherType(WeatherType current)
        {
            // Simple weather transition logic
            return current switch
            {
                WeatherType.Sunny => UnityEngine.Random.value > 0.7f ? WeatherType.Cloudy : WeatherType.Sunny,
                WeatherType.Cloudy => UnityEngine.Random.value > 0.5f ? WeatherType.Rainy : WeatherType.Sunny,
                WeatherType.Rainy => UnityEngine.Random.value > 0.8f ? WeatherType.Stormy : WeatherType.Cloudy,
                WeatherType.Stormy => WeatherType.Rainy,
                WeatherType.Snowy => UnityEngine.Random.value > 0.6f ? WeatherType.Cloudy : WeatherType.Snowy,
                WeatherType.Foggy => WeatherType.Cloudy,
                _ => WeatherType.Sunny
            };
        }

        public WeatherEffect GetCurrentWeatherEffect()
        {
            return _weatherEffects.TryGetValue(_currentWeather.weatherType, out var effect) ? effect : new WeatherEffect();
        }

        #endregion

        #region Weather Coroutines

        private IEnumerator WeatherUpdateCoroutine()
        {
            while (true)
            {
                yield return new WaitForSeconds(1f);

                if (!_isTransitioning)
                {
                    // Update weather duration
                    _currentWeather.remainingDuration -= 1f;

                    // Check if weather should change
                    if (_currentWeather.remainingDuration <= 0f)
                    {
                        GenerateNextWeather();
                    }

                    OnWeatherUpdated?.Invoke(_currentWeather);
                }
            }
        }

        private IEnumerator WeatherTransitionCoroutine()
        {
            _isTransitioning = true;
            _transitionProgress = 0f;

            var startWeather = _currentWeather;
            var transitionStartTime = Time.time;

            while (_transitionProgress < 1f)
            {
                _transitionProgress = (Time.time - transitionStartTime) / transitionDuration;
                _transitionProgress = Mathf.Clamp01(_transitionProgress);

                // Interpolate weather properties
                _currentWeather = InterpolateWeather(startWeather, _targetWeather, _transitionProgress);

                ApplyWeatherEffects();
                OnWeatherUpdated?.Invoke(_currentWeather);

                yield return null;
            }

            _currentWeather = _targetWeather;
            _isTransitioning = false;

            Debug.Log($"[WeatherSystem] Weather transition completed: {_currentWeather.weatherType}");
        }

        #endregion

        #region Weather Generation

        private void GenerateNextWeather()
        {
            var nextWeatherType = SelectNextWeatherType();
            var nextIntensity = UnityEngine.Random.Range(0.3f, 1f);
            var nextDuration = UnityEngine.Random.Range(minWeatherDuration, maxWeatherDuration);

            var nextWeather = new WeatherData
            {
                weatherType = nextWeatherType,
                intensity = nextIntensity,
                remainingDuration = nextDuration,
                windDirection = GenerateWindDirection(nextWeatherType),
                temperature = CalculateTemperature(nextWeatherType, nextIntensity),
                humidity = CalculateHumidity(nextWeatherType, nextIntensity),
                startTime = DateTime.UtcNow
            };

            TransitionToWeather(nextWeather);
        }

        private WeatherType SelectNextWeatherType()
        {
            var random = UnityEngine.Random.value;
            var cumulative = 0f;

            if (random <= (cumulative += clearWeatherChance))
                return WeatherType.Sunny;
            if (random <= (cumulative += cloudyWeatherChance))
                return WeatherType.Cloudy;
            if (random <= (cumulative += rainWeatherChance))
                return WeatherType.Rainy;
            if (random <= (cumulative += stormWeatherChance))
                return WeatherType.Stormy;

            return WeatherType.Sunny;
        }

        private Vector3 GenerateWindDirection(WeatherType weatherType)
        {
            return weatherType switch
            {
                WeatherType.Stormy => UnityEngine.Random.insideUnitSphere.normalized,
                WeatherType.Rainy => new Vector3(UnityEngine.Random.Range(-0.5f, 0.5f), 0, UnityEngine.Random.Range(-1f, 1f)).normalized,
                _ => new Vector3(UnityEngine.Random.Range(-1f, 1f), 0, UnityEngine.Random.Range(-1f, 1f)).normalized
            };
        }

        private float CalculateTemperature(WeatherType weatherType, float intensity)
        {
            return weatherType switch
            {
                WeatherType.Sunny => Mathf.Lerp(20f, 30f, intensity),
                WeatherType.Cloudy => Mathf.Lerp(15f, 25f, intensity),
                WeatherType.Rainy => Mathf.Lerp(10f, 20f, intensity),
                WeatherType.Stormy => Mathf.Lerp(5f, 15f, intensity),
                WeatherType.Snowy => Mathf.Lerp(-5f, 5f, intensity),
                WeatherType.Foggy => Mathf.Lerp(10f, 20f, intensity),
                _ => 20f
            };
        }

        private float CalculateHumidity(WeatherType weatherType, float intensity)
        {
            return weatherType switch
            {
                WeatherType.Sunny => Mathf.Lerp(0.3f, 0.5f, intensity),
                WeatherType.Cloudy => Mathf.Lerp(0.5f, 0.7f, intensity),
                WeatherType.Rainy => Mathf.Lerp(0.8f, 1f, intensity),
                WeatherType.Stormy => Mathf.Lerp(0.9f, 1f, intensity),
                WeatherType.Snowy => Mathf.Lerp(0.7f, 0.9f, intensity),
                WeatherType.Foggy => Mathf.Lerp(0.9f, 1f, intensity),
                _ => 0.5f
            };
        }

        #endregion

        #region Weather Effects

        private void ApplyWeatherEffects()
        {
            if (!_weatherEffects.TryGetValue(_currentWeather.weatherType, out var effect))
                return;

            // Apply lighting effects
            ApplyLightingEffects(effect);

            // Apply particle effects
            ApplyParticleEffects(effect);

            // Apply audio effects
            ApplyAudioEffects(effect);

            // Apply environmental effects
            ApplyEnvironmentalEffects(effect);
        }

        private void ApplyLightingEffects(WeatherEffect effect)
        {
            // Lighting effects would be applied here
            // This would integrate with the lighting system
        }

        private void ApplyParticleEffects(WeatherEffect effect)
        {
            // Particle effects would be applied here
            // This would control rain, snow, fog particles
        }

        private void ApplyAudioEffects(WeatherEffect effect)
        {
            // Audio effects would be applied here
            // This would integrate with the audio subsystem
        }

        private void ApplyEnvironmentalEffects(WeatherEffect effect)
        {
            // Environmental effects would be applied here
            // This would affect creature behavior, plant growth, etc.
        }

        #endregion

        #region Helper Methods

        private WeatherData InterpolateWeather(WeatherData from, WeatherData to, float t)
        {
            return new WeatherData
            {
                weatherType = t > 0.5f ? to.weatherType : from.weatherType,
                intensity = Mathf.Lerp(from.intensity, to.intensity, t),
                remainingDuration = to.remainingDuration,
                windDirection = Vector3.Slerp(from.windDirection, to.windDirection, t),
                temperature = Mathf.Lerp(from.temperature, to.temperature, t),
                humidity = Mathf.Lerp(from.humidity, to.humidity, t),
                startTime = to.startTime
            };
        }

        #endregion

        #region Update Methods

        public void UpdateWeather(float deltaTime)
        {
            if (!enabled || !enableDynamicWeather)
                return;

            // This method is called by the EcosystemSubsystemManager
            // The actual weather updates are handled by the coroutines
            // We can use this for any additional per-frame weather logic
        }

        #endregion

        #region Debug Methods

        [ContextMenu("Change to Clear Weather")]
        private void DebugClearWeather()
        {
            ForceWeatherChange(WeatherType.Sunny, 0.5f);
        }

        [ContextMenu("Change to Rain")]
        private void DebugRainWeather()
        {
            ForceWeatherChange(WeatherType.Rainy, 0.8f);
        }

        [ContextMenu("Change to Storm")]
        private void DebugStormWeather()
        {
            ForceWeatherChange(WeatherType.Stormy, 1f);
        }

        #endregion
    }

    #region Supporting Classes

    [Serializable]
    public class WeatherEffect
    {
        public float lightIntensity = 1f;
        public float fogDensity = 0f;
        public float windStrength = 0.5f;
        public float particleIntensity = 0f;
        public Color atmosphereColor = Color.white;
        public float audioVolume = 1f;
    }


    #endregion
}

