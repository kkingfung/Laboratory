using System;
using System.Collections;
using UnityEngine;
using Laboratory.Core;
using Laboratory.Core.Infrastructure;

namespace Laboratory.Advanced
{
    /// <summary>
    /// Dynamic weather and environment system.
    /// Handles weather states, time of day, lighting, and environmental effects.
    /// Integrates with gameplay systems for immersive weather impact.
    /// </summary>
    public class DynamicWeatherSystem : MonoBehaviour
    {
        #region Configuration

        [Header("Time of Day")]
        [SerializeField] private bool enableTimeProgression = true;
        [SerializeField] private float dayLengthMinutes = 24f; // Real-time minutes for full day
        [SerializeField] private float startingTimeOfDay = 6f; // 6 AM
        [SerializeField] private AnimationCurve sunIntensityCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        [Header("Weather")]
        [SerializeField] private bool enableDynamicWeather = true;
        [SerializeField] private float weatherChangeInterval = 300f; // 5 minutes
        [SerializeField] private float weatherTransitionDuration = 30f; // 30 seconds
        [SerializeField] private WeatherType startingWeather = WeatherType.Clear;

        [Header("Lighting")]
        [SerializeField] private Light directionalLight;
        [SerializeField] private Gradient dayNightColorGradient;
        [SerializeField] private float maxSunIntensity = 1.5f;
        [SerializeField] private float nightIntensity = 0.1f;

        [Header("Effects")]
        [SerializeField] private ParticleSystem rainParticles;
        [SerializeField] private ParticleSystem snowParticles;
        [SerializeField] private float maxWindStrength = 20f;

        [Header("Gameplay Impact")]
        [SerializeField] private bool affectVisibility = true;
        [SerializeField] private bool affectMovement = true;
        [SerializeField] private float rainMovementPenalty = 0.9f;
        [SerializeField] private float snowMovementPenalty = 0.8f;

        #endregion

        #region Private Fields

        private static DynamicWeatherSystem _instance;

        // Time
        private float _currentTimeOfDay = 6f; // 0-24 hours
        private int _currentDay = 0;

        // Weather
        private WeatherType _currentWeather = WeatherType.Clear;
        private WeatherType _targetWeather = WeatherType.Clear;
        private float _weatherTransitionProgress = 1f;
        private float _lastWeatherChangeTime = 0f;

        // Environment
        private float _currentWindStrength = 0f;
        private Vector3 _windDirection = Vector3.forward;
        private float _currentVisibilityRange = 1000f;

        // Statistics
        private int _totalWeatherChanges = 0;
        private float _totalTimePassed = 0f;

        // Events
        public event Action<float> OnTimeChanged;
        public event Action<int> OnDayChanged;
        public event Action<WeatherType, WeatherType> OnWeatherChanged;
        public event Action<float> OnWindStrengthChanged;

        #endregion

        #region Properties

        public static DynamicWeatherSystem Instance => _instance;
        public float CurrentTimeOfDay => _currentTimeOfDay;
        public int CurrentDay => _currentDay;
        public WeatherType CurrentWeather => _currentWeather;
        public float WindStrength => _currentWindStrength;
        public float VisibilityRange => _currentVisibilityRange;
        public bool IsNight => _currentTimeOfDay < GameConstants.DAWN_HOUR || _currentTimeOfDay > GameConstants.DUSK_HOUR;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
                Initialize();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Update()
        {
            if (enableTimeProgression)
            {
                UpdateTime();
            }

            if (enableDynamicWeather)
            {
                UpdateWeather();
            }

            UpdateLighting();
            UpdateEnvironmentEffects();
        }

        #endregion

        #region Initialization

        private void Initialize()
        {
            Debug.Log("[DynamicWeatherSystem] Initializing...");

            _currentTimeOfDay = startingTimeOfDay;
            _currentWeather = startingWeather;
            _targetWeather = startingWeather;

            // Initialize lighting
            if (directionalLight == null)
            {
                directionalLight = FindFirstObjectByType<Light>();
            }

            // Initialize gradient if not set
            if (dayNightColorGradient == null)
            {
                dayNightColorGradient = CreateDefaultGradient();
            }

            ApplyWeather(_currentWeather, true);

            Debug.Log("[DynamicWeatherSystem] Initialized");
        }

        private Gradient CreateDefaultGradient()
        {
            var gradient = new Gradient();
            var colors = new GradientColorKey[5];
            var alphas = new GradientAlphaKey[5];

            // Midnight
            colors[0] = new GradientColorKey(new Color(0.05f, 0.05f, 0.15f), 0f);
            // Sunrise
            colors[1] = new GradientColorKey(new Color(1f, 0.7f, 0.4f), 0.25f);
            // Noon
            colors[2] = new GradientColorKey(new Color(1f, 1f, 1f), 0.5f);
            // Sunset
            colors[3] = new GradientColorKey(new Color(1f, 0.5f, 0.3f), 0.75f);
            // Midnight
            colors[4] = new GradientColorKey(new Color(0.05f, 0.05f, 0.15f), 1f);

            for (int i = 0; i < 5; i++)
            {
                alphas[i] = new GradientAlphaKey(1f, colors[i].time);
            }

            gradient.SetKeys(colors, alphas);
            return gradient;
        }

        #endregion

        #region Time of Day

        private void UpdateTime()
        {
            float hoursPerSecond = GameConstants.DAY_LENGTH_HOURS / (dayLengthMinutes * 60f);
            float previousTime = _currentTimeOfDay;

            _currentTimeOfDay += hoursPerSecond * Time.deltaTime;
            _totalTimePassed += Time.deltaTime;

            // Handle day rollover
            if (_currentTimeOfDay >= GameConstants.DAY_LENGTH_HOURS)
            {
                _currentTimeOfDay -= GameConstants.DAY_LENGTH_HOURS;
                _currentDay++;
                OnDayChanged?.Invoke(_currentDay);
                Debug.Log($"[DynamicWeatherSystem] Day {_currentDay} started");
            }

            OnTimeChanged?.Invoke(_currentTimeOfDay);
        }

        /// <summary>
        /// Set time of day (0-24 hours).
        /// </summary>
        public void SetTimeOfDay(float hours)
        {
            _currentTimeOfDay = Mathf.Clamp(hours, 0f, GameConstants.DAY_LENGTH_HOURS);
            Debug.Log($"[DynamicWeatherSystem] Time set to {hours:F1}:00");
        }

        /// <summary>
        /// Set day length in real-time minutes.
        /// </summary>
        public void SetDayLength(float minutes)
        {
            dayLengthMinutes = Mathf.Max(1f, minutes);
            Debug.Log($"[DynamicWeatherSystem] Day length set to {minutes} minutes");
        }

        #endregion

        #region Weather

        private void UpdateWeather()
        {
            // Check for weather change
            if (Time.time - _lastWeatherChangeTime >= weatherChangeInterval)
            {
                ChangeWeather();
            }

            // Update transition
            if (_weatherTransitionProgress < 1f)
            {
                _weatherTransitionProgress += Time.deltaTime / weatherTransitionDuration;
                _weatherTransitionProgress = Mathf.Clamp01(_weatherTransitionProgress);

                BlendWeather(_currentWeather, _targetWeather, _weatherTransitionProgress);

                if (_weatherTransitionProgress >= 1f)
                {
                    _currentWeather = _targetWeather;
                    Debug.Log($"[DynamicWeatherSystem] Weather transition complete: {_currentWeather}");
                }
            }
        }

        private void ChangeWeather()
        {
            WeatherType previousWeather = _currentWeather;

            // Random weather selection (weighted)
            float roll = UnityEngine.Random.value;

            if (roll < 0.4f)
                _targetWeather = WeatherType.Clear;
            else if (roll < 0.6f)
                _targetWeather = WeatherType.Cloudy;
            else if (roll < 0.8f)
                _targetWeather = WeatherType.Rain;
            else if (roll < 0.95f)
                _targetWeather = WeatherType.Storm;
            else
                _targetWeather = WeatherType.Snow;

            // Don't transition to same weather
            if (_targetWeather == _currentWeather)
            {
                _lastWeatherChangeTime = Time.time;
                return;
            }

            _weatherTransitionProgress = 0f;
            _lastWeatherChangeTime = Time.time;
            _totalWeatherChanges++;

            OnWeatherChanged?.Invoke(previousWeather, _targetWeather);

            Debug.Log($"[DynamicWeatherSystem] Weather changing: {previousWeather} → {_targetWeather}");
        }

        /// <summary>
        /// Force weather change.
        /// </summary>
        public void SetWeather(WeatherType weather)
        {
            WeatherType previousWeather = _currentWeather;
            _targetWeather = weather;
            _weatherTransitionProgress = 0f;

            OnWeatherChanged?.Invoke(previousWeather, weather);

            Debug.Log($"[DynamicWeatherSystem] Weather set to: {weather}");
        }

        private void ApplyWeather(WeatherType weather, bool instant)
        {
            if (instant)
            {
                _currentWeather = weather;
                _targetWeather = weather;
                _weatherTransitionProgress = 1f;
            }

            switch (weather)
            {
                case WeatherType.Clear:
                    _currentWindStrength = 0f;
                    _currentVisibilityRange = 1000f;
                    StopWeatherEffects();
                    break;

                case WeatherType.Cloudy:
                    _currentWindStrength = 5f;
                    _currentVisibilityRange = 800f;
                    StopWeatherEffects();
                    break;

                case WeatherType.Rain:
                    _currentWindStrength = 10f;
                    _currentVisibilityRange = 400f;
                    if (rainParticles != null) rainParticles.Play();
                    if (snowParticles != null) snowParticles.Stop();
                    break;

                case WeatherType.Storm:
                    _currentWindStrength = maxWindStrength;
                    _currentVisibilityRange = 200f;
                    if (rainParticles != null) rainParticles.Play();
                    if (snowParticles != null) snowParticles.Stop();
                    break;

                case WeatherType.Snow:
                    _currentWindStrength = 7f;
                    _currentVisibilityRange = 300f;
                    if (snowParticles != null) snowParticles.Play();
                    if (rainParticles != null) rainParticles.Stop();
                    break;
            }
        }

        private void BlendWeather(WeatherType from, WeatherType to, float t)
        {
            // Blend wind strength
            float fromWind = GetWeatherWindStrength(from);
            float toWind = GetWeatherWindStrength(to);
            _currentWindStrength = Mathf.Lerp(fromWind, toWind, t);

            // Blend visibility
            float fromVis = GetWeatherVisibility(from);
            float toVis = GetWeatherVisibility(to);
            _currentVisibilityRange = Mathf.Lerp(fromVis, toVis, t);

            // Update particles
            if (t > 0.5f)
            {
                ApplyWeather(to, false);
            }

            OnWindStrengthChanged?.Invoke(_currentWindStrength);
        }

        private float GetWeatherWindStrength(WeatherType weather)
        {
            return weather switch
            {
                WeatherType.Clear => 0f,
                WeatherType.Cloudy => 5f,
                WeatherType.Rain => 10f,
                WeatherType.Storm => maxWindStrength,
                WeatherType.Snow => 7f,
                _ => 0f
            };
        }

        private float GetWeatherVisibility(WeatherType weather)
        {
            return weather switch
            {
                WeatherType.Clear => 1000f,
                WeatherType.Cloudy => 800f,
                WeatherType.Rain => 400f,
                WeatherType.Storm => 200f,
                WeatherType.Snow => 300f,
                _ => 1000f
            };
        }

        private void StopWeatherEffects()
        {
            if (rainParticles != null) rainParticles.Stop();
            if (snowParticles != null) snowParticles.Stop();
        }

        #endregion

        #region Lighting

        private void UpdateLighting()
        {
            if (directionalLight == null) return;

            // Calculate time factor (0-1 for full day)
            float timeFactor = _currentTimeOfDay / GameConstants.DAY_LENGTH_HOURS;

            // Sun rotation
            float angle = (timeFactor - 0.25f) * 360f;
            directionalLight.transform.rotation = Quaternion.Euler(angle, 0, 0);

            // Sun intensity
            float baseIntensity = sunIntensityCurve.Evaluate(timeFactor);

            // Adjust for night time
            if (IsNight)
            {
                baseIntensity = Mathf.Lerp(baseIntensity, nightIntensity, Mathf.Abs(Mathf.Sin(timeFactor * Mathf.PI)));
            }
            else
            {
                baseIntensity *= maxSunIntensity;
            }

            // Adjust for weather
            float weatherMultiplier = _currentWeather switch
            {
                WeatherType.Clear => 1f,
                WeatherType.Cloudy => 0.7f,
                WeatherType.Rain => 0.5f,
                WeatherType.Storm => 0.3f,
                WeatherType.Snow => 0.6f,
                _ => 1f
            };

            directionalLight.intensity = baseIntensity * weatherMultiplier;

            // Sun color
            directionalLight.color = dayNightColorGradient.Evaluate(timeFactor);
        }

        #endregion

        #region Environment Effects

        private void UpdateEnvironmentEffects()
        {
            // Update wind direction
            float windVariation = Mathf.PerlinNoise(Time.time * 0.1f, 0f) * 2f - 1f;
            _windDirection = Quaternion.Euler(0, windVariation * 30f, 0) * Vector3.forward;

            // Update particle effects based on wind
            if (rainParticles != null && rainParticles.isPlaying)
            {
                var velocity = rainParticles.velocityOverLifetime;
                velocity.x = _windDirection.x * _currentWindStrength;
                velocity.z = _windDirection.z * _currentWindStrength;
            }

            if (snowParticles != null && snowParticles.isPlaying)
            {
                var velocity = snowParticles.velocityOverLifetime;
                velocity.x = _windDirection.x * _currentWindStrength * 0.5f;
                velocity.z = _windDirection.z * _currentWindStrength * 0.5f;
            }
        }

        #endregion

        #region Gameplay Impact

        /// <summary>
        /// Get movement speed multiplier based on weather.
        /// </summary>
        public float GetMovementSpeedMultiplier()
        {
            if (!affectMovement) return 1f;

            return _currentWeather switch
            {
                WeatherType.Rain => rainMovementPenalty,
                WeatherType.Storm => rainMovementPenalty * 0.9f,
                WeatherType.Snow => snowMovementPenalty,
                _ => 1f
            };
        }

        /// <summary>
        /// Get visibility range.
        /// </summary>
        public float GetVisibilityRange()
        {
            return affectVisibility ? _currentVisibilityRange : 1000f;
        }

        #endregion

        #region Public API

        /// <summary>
        /// Get weather system statistics.
        /// </summary>
        public WeatherStats GetStats()
        {
            return new WeatherStats
            {
                currentDay = _currentDay,
                currentTimeOfDay = _currentTimeOfDay,
                currentWeather = _currentWeather,
                totalWeatherChanges = _totalWeatherChanges,
                totalTimePassed = _totalTimePassed,
                windStrength = _currentWindStrength,
                visibilityRange = _currentVisibilityRange
            };
        }

        #endregion

        #region Context Menu

        [ContextMenu("Set Time: Dawn (6:00)")]
        private void SetTimeDawn()
        {
            SetTimeOfDay(6f);
        }

        [ContextMenu("Set Time: Noon (12:00)")]
        private void SetTimeNoon()
        {
            SetTimeOfDay(12f);
        }

        [ContextMenu("Set Time: Dusk (18:00)")]
        private void SetTimeDusk()
        {
            SetTimeOfDay(18f);
        }

        [ContextMenu("Set Time: Midnight (0:00)")]
        private void SetTimeMidnight()
        {
            SetTimeOfDay(0f);
        }

        [ContextMenu("Set Weather: Clear")]
        private void SetWeatherClear()
        {
            SetWeather(WeatherType.Clear);
        }

        [ContextMenu("Set Weather: Rain")]
        private void SetWeatherRain()
        {
            SetWeather(WeatherType.Rain);
        }

        [ContextMenu("Set Weather: Storm")]
        private void SetWeatherStorm()
        {
            SetWeather(WeatherType.Storm);
        }

        [ContextMenu("Print Statistics")]
        private void PrintStatistics()
        {
            var stats = GetStats();
            Debug.Log($"=== Dynamic Weather Statistics ===\n" +
                      $"Day: {stats.currentDay}\n" +
                      $"Time: {stats.currentTimeOfDay:F1}:00\n" +
                      $"Weather: {stats.currentWeather}\n" +
                      $"Weather Changes: {stats.totalWeatherChanges}\n" +
                      $"Time Passed: {stats.totalTimePassed / 60f:F1} minutes\n" +
                      $"Wind Strength: {stats.windStrength:F1}\n" +
                      $"Visibility: {stats.visibilityRange:F0}m");
        }

        #endregion
    }

    #region Data Structures

    /// <summary>
    /// Weather types.
    /// </summary>
    public enum WeatherType
    {
        Clear,
        Cloudy,
        Rain,
        Storm,
        Snow
    }

    /// <summary>
    /// Weather statistics.
    /// </summary>
    [Serializable]
    public struct WeatherStats
    {
        public int currentDay;
        public float currentTimeOfDay;
        public WeatherType currentWeather;
        public int totalWeatherChanges;
        public float totalTimePassed;
        public float windStrength;
        public float visibilityRange;
    }

    #endregion
}
