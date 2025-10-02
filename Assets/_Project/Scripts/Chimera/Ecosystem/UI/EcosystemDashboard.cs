using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Laboratory.Chimera.Ecosystem.Core;
using Laboratory.Chimera.Genetics.Core;
using Unity.Entities;

namespace Laboratory.Chimera.Ecosystem.UI
{
    /// <summary>
    /// Comprehensive dashboard for monitoring ecosystem health and population dynamics
    /// Provides real-time visualization of the living world simulation
    /// </summary>
    public class EcosystemDashboard : MonoBehaviour
    {
        [Header("Main Dashboard")]
        [SerializeField] private Canvas _dashboardCanvas;
        [SerializeField] private Button _toggleButton;
        [SerializeField] private TextMeshProUGUI _ecosystemTitle;
        [SerializeField] private Image _healthIndicator;

        [Header("Environmental Conditions")]
        [SerializeField] private Slider _temperatureSlider;
        [SerializeField] private Slider _humiditySlider;
        [SerializeField] private Slider _oxygenSlider;
        [SerializeField] private TextMeshProUGUI _temperatureText;
        [SerializeField] private TextMeshProUGUI _humidityText;
        [SerializeField] private TextMeshProUGUI _seasonText;
        [SerializeField] private Image _weatherIcon;

        [Header("Population Overview")]
        [SerializeField] private TextMeshProUGUI _totalPopulationText;
        [SerializeField] private TextMeshProUGUI _carryingCapacityText;
        [SerializeField] private TextMeshProUGUI _growthRateText;
        [SerializeField] private TextMeshProUGUI _diversityIndexText;
        [SerializeField] private Slider _populationBar;
        [SerializeField] private Image _populationTrendArrow;

        [Header("Resource Status")]
        [SerializeField] private Slider _foodAvailabilitySlider;
        [SerializeField] private Slider _waterAvailabilitySlider;
        [SerializeField] private Slider _shelterAvailabilitySlider;
        [SerializeField] private TextMeshProUGUI _foodStatusText;
        [SerializeField] private TextMeshProUGUI _waterStatusText;
        [SerializeField] private TextMeshProUGUI _shelterStatusText;

        [Header("Species List")]
        [SerializeField] private Transform _speciesContainer;
        [SerializeField] private GameObject _speciesEntryPrefab;
        [SerializeField] private ScrollRect _speciesScrollRect;
        [SerializeField] private Button _sortByPopulationButton;
        [SerializeField] private Button _sortByFitnessButton;

        [Header("Environmental Events")]
        [SerializeField] private Transform _eventsContainer;
        [SerializeField] private GameObject _eventEntryPrefab;
        [SerializeField] private TextMeshProUGUI _activeEventsCount;
        [SerializeField] private Button _eventHistoryButton;

        [Header("Charts and Graphs")]
        [SerializeField] private LineChart _populationChart;
        [SerializeField] private PieChart _speciesDistributionChart;
        [SerializeField] private BarChart _resourceChart;

        [Header("Simulation Controls")]
        [SerializeField] private Button _pauseSimulationButton;
        [SerializeField] private Slider _timeScaleSlider;
        [SerializeField] private TextMeshProUGUI _timeScaleText;
        [SerializeField] private TextMeshProUGUI _simulationTimeText;
        [SerializeField] private Button _addEventButton;
        [SerializeField] private Button _resetEcosystemButton;

        private EntityManager _entityManager;
        private List<EcosystemState> _ecosystems = new List<EcosystemState>();
        private List<CreaturePopulation> _populations = new List<CreaturePopulation>();
        private List<EnvironmentalEvent> _activeEvents = new List<EnvironmentalEvent>();
        private List<GameObject> _speciesEntries = new List<GameObject>();
        private List<GameObject> _eventEntries = new List<GameObject>();

        private bool _isDashboardOpen = false;
        private bool _isSimulationPaused = false;
        private float _updateInterval = 1.0f;
        private float _lastUpdate = 0f;

        // Chart data
        private Queue<float> _populationHistory = new Queue<float>();
        private Queue<float> _healthHistory = new Queue<float>();
        private Queue<float> _diversityHistory = new Queue<float>();
        private const int MAX_CHART_POINTS = 60; // 60 data points for charts

        private static EcosystemDashboard _instance;
        public static EcosystemDashboard Instance => _instance;

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            SetupUI();
        }

        private void Start()
        {
            _entityManager = Unity.Entities.World.DefaultGameObjectInjectionWorld?.EntityManager;
            StartCoroutine(InitializeWithDelay());
        }

        /// <summary>
        /// Setup UI event handlers
        /// </summary>
        private void SetupUI()
        {
            if (_toggleButton != null)
                _toggleButton.onClick.AddListener(ToggleDashboard);

            if (_pauseSimulationButton != null)
                _pauseSimulationButton.onClick.AddListener(ToggleSimulation);

            if (_timeScaleSlider != null)
            {
                _timeScaleSlider.value = 1f;
                _timeScaleSlider.onValueChanged.AddListener(OnTimeScaleChanged);
            }

            if (_sortByPopulationButton != null)
                _sortByPopulationButton.onClick.AddListener(() => SortSpecies(SpeciesSortMode.Population));

            if (_sortByFitnessButton != null)
                _sortByFitnessButton.onClick.AddListener(() => SortSpecies(SpeciesSortMode.Fitness));

            if (_addEventButton != null)
                _addEventButton.onClick.AddListener(TriggerRandomEvent);

            if (_resetEcosystemButton != null)
                _resetEcosystemButton.onClick.AddListener(ResetEcosystem);

            if (_eventHistoryButton != null)
                _eventHistoryButton.onClick.AddListener(ShowEventHistory);

            // Start with dashboard closed
            if (_dashboardCanvas != null)
                _dashboardCanvas.gameObject.SetActive(false);
        }

        /// <summary>
        /// Initialize with slight delay to ensure ECS systems are ready
        /// </summary>
        private IEnumerator InitializeWithDelay()
        {
            yield return new WaitForSeconds(1f);
            CreateMockEcosystem();
            RefreshDashboard();
        }

        private void Update()
        {
            if (!_isDashboardOpen || _entityManager == null) return;

            // Update dashboard at regular intervals
            if (Time.time - _lastUpdate >= _updateInterval)
            {
                RefreshDashboard();
                _lastUpdate = Time.time;
            }

            UpdateSimulationTime();
        }

        /// <summary>
        /// Toggle dashboard visibility
        /// </summary>
        public void ToggleDashboard()
        {
            _isDashboardOpen = !_isDashboardOpen;

            if (_dashboardCanvas != null)
            {
                _dashboardCanvas.gameObject.SetActive(_isDashboardOpen);

                if (_isDashboardOpen)
                {
                    RefreshDashboard();
                }
            }
        }

        /// <summary>
        /// Refresh all dashboard data
        /// </summary>
        public void RefreshDashboard()
        {
            GatherEcosystemData();
            UpdateEnvironmentalDisplay();
            UpdatePopulationDisplay();
            UpdateResourceDisplay();
            UpdateSpeciesList();
            UpdateEventsList();
            UpdateCharts();
            UpdateHealthIndicator();
        }

        /// <summary>
        /// Gather data from ECS entities
        /// </summary>
        private void GatherEcosystemData()
        {
            if (_entityManager == null) return;

            _ecosystems.Clear();
            _populations.Clear();
            _activeEvents.Clear();

            // Gather ecosystem data
            var ecosystemQuery = _entityManager.CreateEntityQuery(typeof(EcosystemState));
            var ecosystemEntities = ecosystemQuery.ToEntityArray(Unity.Collections.Allocator.Temp);

            foreach (var entity in ecosystemEntities)
            {
                if (_entityManager.HasComponent<EcosystemState>(entity))
                {
                    _ecosystems.Add(_entityManager.GetComponentData<EcosystemState>(entity));
                }
            }
            ecosystemEntities.Dispose();

            // Gather population data
            var populationQuery = _entityManager.CreateEntityQuery(typeof(CreaturePopulation));
            var populationEntities = populationQuery.ToEntityArray(Unity.Collections.Allocator.Temp);

            foreach (var entity in populationEntities)
            {
                if (_entityManager.HasComponent<CreaturePopulation>(entity))
                {
                    _populations.Add(_entityManager.GetComponentData<CreaturePopulation>(entity));
                }
            }
            populationEntities.Dispose();

            // Gather event data
            var eventQuery = _entityManager.CreateEntityQuery(typeof(EnvironmentalEvent));
            var eventEntities = eventQuery.ToEntityArray(Unity.Collections.Allocator.Temp);

            foreach (var entity in eventEntities)
            {
                if (_entityManager.HasComponent<EnvironmentalEvent>(entity))
                {
                    _activeEvents.Add(_entityManager.GetComponentData<EnvironmentalEvent>(entity));
                }
            }
            eventEntities.Dispose();
        }

        /// <summary>
        /// Update environmental conditions display
        /// </summary>
        private void UpdateEnvironmentalDisplay()
        {
            if (_ecosystems.Count == 0) return;

            var ecosystem = _ecosystems[0]; // Use first ecosystem

            // Temperature
            if (_temperatureSlider != null)
            {
                float tempNormalized = (ecosystem.Temperature + 50f) / 100f; // Normalize -50 to 50 range
                _temperatureSlider.value = tempNormalized;
            }

            if (_temperatureText != null)
                _temperatureText.text = $"{ecosystem.Temperature:F1}°C";

            // Humidity
            if (_humiditySlider != null)
                _humiditySlider.value = ecosystem.Humidity;

            if (_humidityText != null)
                _humidityText.text = $"{ecosystem.Humidity:P0}";

            // Oxygen
            if (_oxygenSlider != null)
                _oxygenSlider.value = ecosystem.Oxygen;

            // Season
            if (_seasonText != null)
                _seasonText.text = $"{ecosystem.CurrentSeason} (Day {ecosystem.DaysSinceGenesis})";

            // Weather icon
            if (_weatherIcon != null)
                _weatherIcon.color = GetWeatherColor(ecosystem);

            // Ecosystem title
            if (_ecosystemTitle != null)
                _ecosystemTitle.text = $"{ecosystem.PrimaryBiome} Ecosystem";
        }

        /// <summary>
        /// Update population overview display
        /// </summary>
        private void UpdatePopulationDisplay()
        {
            if (_ecosystems.Count == 0) return;

            var ecosystem = _ecosystems[0];

            if (_totalPopulationText != null)
                _totalPopulationText.text = $"Population: {ecosystem.TotalPopulation:N0}";

            if (_carryingCapacityText != null)
                _carryingCapacityText.text = $"Capacity: {ecosystem.CarryingCapacity:N0}";

            if (_growthRateText != null)
            {
                string trend = ecosystem.PopulationGrowthRate > 0 ? "↗" : ecosystem.PopulationGrowthRate < 0 ? "↘" : "→";
                _growthRateText.text = $"Growth: {ecosystem.PopulationGrowthRate:P1} {trend}";
            }

            if (_diversityIndexText != null)
                _diversityIndexText.text = $"Diversity: {ecosystem.DiversityIndex:F2}";

            if (_populationBar != null)
            {
                float populationRatio = (float)ecosystem.TotalPopulation / ecosystem.CarryingCapacity;
                _populationBar.value = populationRatio;

                // Color code the bar
                Color barColor = populationRatio < 0.3f ? Color.red :
                                populationRatio < 0.7f ? Color.yellow :
                                populationRatio < 1.0f ? Color.green :
                                Color.red; // Overpopulation

                var fillImage = _populationBar.fillRect.GetComponent<Image>();
                if (fillImage != null) fillImage.color = barColor;
            }

            if (_populationTrendArrow != null)
            {
                _populationTrendArrow.color = ecosystem.PopulationGrowthRate > 0 ? Color.green :
                                             ecosystem.PopulationGrowthRate < 0 ? Color.red : Color.yellow;
            }

            // Update chart data
            UpdateChartData(ecosystem);
        }

        /// <summary>
        /// Update resource availability display
        /// </summary>
        private void UpdateResourceDisplay()
        {
            if (_ecosystems.Count == 0) return;

            var ecosystem = _ecosystems[0];

            // Food availability
            if (_foodAvailabilitySlider != null)
                _foodAvailabilitySlider.value = ecosystem.FoodAvailability;

            if (_foodStatusText != null)
            {
                string status = ecosystem.FoodAvailability > 0.7f ? "Abundant" :
                               ecosystem.FoodAvailability > 0.4f ? "Adequate" :
                               ecosystem.FoodAvailability > 0.2f ? "Scarce" : "Critical";
                _foodStatusText.text = $"Food: {status}";
            }

            // Water availability
            if (_waterAvailabilitySlider != null)
                _waterAvailabilitySlider.value = ecosystem.WaterAvailability;

            if (_waterStatusText != null)
            {
                string status = ecosystem.WaterAvailability > 0.7f ? "Abundant" :
                               ecosystem.WaterAvailability > 0.4f ? "Adequate" :
                               ecosystem.WaterAvailability > 0.2f ? "Scarce" : "Critical";
                _waterStatusText.text = $"Water: {status}";
            }

            // Shelter (simplified as environmental comfort)
            float shelterScore = 1.0f - ecosystem.EnvironmentalPressure;
            if (_shelterAvailabilitySlider != null)
                _shelterAvailabilitySlider.value = shelterScore;

            if (_shelterStatusText != null)
            {
                string status = shelterScore > 0.7f ? "Safe" :
                               shelterScore > 0.4f ? "Moderate" :
                               shelterScore > 0.2f ? "Stressed" : "Dangerous";
                _shelterStatusText.text = $"Environment: {status}";
            }
        }

        /// <summary>
        /// Update species list display
        /// </summary>
        private void UpdateSpeciesList()
        {
            ClearSpeciesEntries();

            foreach (var population in _populations)
            {
                CreateSpeciesEntry(population);
            }
        }

        /// <summary>
        /// Create individual species entry
        /// </summary>
        private void CreateSpeciesEntry(CreaturePopulation population)
        {
            if (_speciesEntryPrefab == null || _speciesContainer == null) return;

            GameObject entryObj = Instantiate(_speciesEntryPrefab, _speciesContainer);
            _speciesEntries.Add(entryObj);

            var entryComponent = entryObj.GetComponent<SpeciesEntry>();
            if (entryComponent != null)
            {
                entryComponent.SetupEntry(population);
            }
        }

        /// <summary>
        /// Update environmental events list
        /// </summary>
        private void UpdateEventsList()
        {
            ClearEventEntries();

            if (_activeEventsCount != null)
                _activeEventsCount.text = $"Active Events: {_activeEvents.Count}";

            foreach (var envEvent in _activeEvents)
            {
                CreateEventEntry(envEvent);
            }
        }

        /// <summary>
        /// Create individual event entry
        /// </summary>
        private void CreateEventEntry(EnvironmentalEvent envEvent)
        {
            if (_eventEntryPrefab == null || _eventsContainer == null) return;

            GameObject entryObj = Instantiate(_eventEntryPrefab, _eventsContainer);
            _eventEntries.Add(entryObj);

            var entryComponent = entryObj.GetComponent<EventEntry>();
            if (entryComponent != null)
            {
                entryComponent.SetupEntry(envEvent);
            }
        }

        /// <summary>
        /// Update charts and graphs
        /// </summary>
        private void UpdateCharts()
        {
            if (_ecosystems.Count == 0) return;

            var ecosystem = _ecosystems[0];

            // Update population chart
            if (_populationChart != null)
            {
                var populationData = _populationHistory.ToArray();
                _populationChart.UpdateChart(populationData, "Population Over Time");
            }

            // Update species distribution pie chart
            if (_speciesDistributionChart != null)
            {
                var speciesData = _populations.Select(p => (float)p.CurrentPopulation).ToArray();
                var speciesNames = _populations.Select(p => p.SpeciesName.ToString()).ToArray();
                _speciesDistributionChart.UpdateChart(speciesData, speciesNames);
            }

            // Update resource bar chart
            if (_resourceChart != null)
            {
                var resourceData = new float[] { ecosystem.FoodAvailability, ecosystem.WaterAvailability, 1.0f - ecosystem.EnvironmentalPressure };
                var resourceNames = new string[] { "Food", "Water", "Safety" };
                _resourceChart.UpdateChart(resourceData, resourceNames);
            }
        }

        /// <summary>
        /// Update chart data history
        /// </summary>
        private void UpdateChartData(EcosystemState ecosystem)
        {
            // Add current data points
            _populationHistory.Enqueue(ecosystem.TotalPopulation);
            _healthHistory.Enqueue(ecosystem.GetEcosystemHealth());
            _diversityHistory.Enqueue(ecosystem.DiversityIndex);

            // Maintain max points
            while (_populationHistory.Count > MAX_CHART_POINTS)
                _populationHistory.Dequeue();
            while (_healthHistory.Count > MAX_CHART_POINTS)
                _healthHistory.Dequeue();
            while (_diversityHistory.Count > MAX_CHART_POINTS)
                _diversityHistory.Dequeue();
        }

        /// <summary>
        /// Update ecosystem health indicator
        /// </summary>
        private void UpdateHealthIndicator()
        {
            if (_ecosystems.Count == 0 || _healthIndicator == null) return;

            float health = _ecosystems[0].GetEcosystemHealth();
            Color healthColor = health > 0.7f ? Color.green :
                               health > 0.4f ? Color.yellow : Color.red;

            _healthIndicator.color = healthColor;
            _healthIndicator.fillAmount = health;
        }

        /// <summary>
        /// Update simulation time display
        /// </summary>
        private void UpdateSimulationTime()
        {
            if (_simulationTimeText == null || _ecosystems.Count == 0) return;

            int days = _ecosystems[0].DaysSinceGenesis;
            int years = days / 365;
            int remainingDays = days % 365;

            _simulationTimeText.text = $"Year {years}, Day {remainingDays}";
        }

        /// <summary>
        /// UI Event Handlers
        /// </summary>
        private void ToggleSimulation()
        {
            _isSimulationPaused = !_isSimulationPaused;
            Time.timeScale = _isSimulationPaused ? 0f : _timeScaleSlider?.value ?? 1f;

            if (_pauseSimulationButton != null)
            {
                var buttonText = _pauseSimulationButton.GetComponentInChildren<TextMeshProUGUI>();
                if (buttonText != null)
                    buttonText.text = _isSimulationPaused ? "Resume" : "Pause";
            }
        }

        private void OnTimeScaleChanged(float timeScale)
        {
            if (!_isSimulationPaused)
                Time.timeScale = timeScale;

            if (_timeScaleText != null)
                _timeScaleText.text = $"Speed: {timeScale:F1}x";
        }

        private void SortSpecies(SpeciesSortMode sortMode)
        {
            switch (sortMode)
            {
                case SpeciesSortMode.Population:
                    _populations.Sort((a, b) => b.CurrentPopulation.CompareTo(a.CurrentPopulation));
                    break;
                case SpeciesSortMode.Fitness:
                    _populations.Sort((a, b) => b.EnvironmentalFitness.CompareTo(a.EnvironmentalFitness));
                    break;
            }

            UpdateSpeciesList();
        }

        private void TriggerRandomEvent()
        {
            // This would trigger a random environmental event
            UnityEngine.Debug.Log("🌪️ Random environmental event triggered!");
            // In real implementation, would create event entity
        }

        private void ResetEcosystem()
        {
            // This would reset the ecosystem to initial state
            UnityEngine.Debug.Log("🔄 Ecosystem reset requested!");
            // In real implementation, would clear and recreate ecosystem entities
        }

        private void ShowEventHistory()
        {
            // This would show a detailed event history
            UnityEngine.Debug.Log("📜 Event history requested!");
            // In real implementation, would open event history panel
        }

        /// <summary>
        /// Helper methods
        /// </summary>
        private Color GetWeatherColor(EcosystemState ecosystem)
        {
            if (ecosystem.Temperature > 30f) return new Color(1f, 0.5f, 0f); // Hot - orange
            if (ecosystem.Temperature < 5f) return new Color(0.5f, 0.8f, 1f); // Cold - light blue
            if (ecosystem.Humidity > 0.8f) return Color.blue; // Wet - blue
            if (ecosystem.Humidity < 0.3f) return Color.yellow; // Dry - yellow
            return Color.green; // Mild - green
        }

        private void ClearSpeciesEntries()
        {
            foreach (var entry in _speciesEntries)
            {
                if (entry != null) Destroy(entry);
            }
            _speciesEntries.Clear();
        }

        private void ClearEventEntries()
        {
            foreach (var entry in _eventEntries)
            {
                if (entry != null) Destroy(entry);
            }
            _eventEntries.Clear();
        }

        /// <summary>
        /// Create mock ecosystem for demonstration
        /// </summary>
        private void CreateMockEcosystem()
        {
            if (_entityManager == null) return;

            // Create mock ecosystem entity
            var ecosystemEntity = _entityManager.CreateEntity();
            _entityManager.AddComponentData(ecosystemEntity, new EcosystemState
            {
                EcosystemID = new Unity.Collections.FixedString64Bytes("MockEcosystem001"),
                PrimaryBiome = BiomeType.TemperateForest,
                Temperature = 18f,
                Humidity = 0.6f,
                Oxygen = 0.21f,
                FoodAvailability = 0.8f,
                WaterAvailability = 0.9f,
                TotalPopulation = 150,
                CarryingCapacity = 200,
                PopulationGrowthRate = 0.02f,
                DiversityIndex = 1.8f,
                CurrentSeason = Season.Spring,
                BiomeStability = 0.85f,
                EnvironmentalPressure = 0.2f
            });

            // Create mock populations
            for (int i = 0; i < 5; i++)
            {
                var populationEntity = _entityManager.CreateEntity();
                _entityManager.AddComponentData(populationEntity, new CreaturePopulation
                {
                    SpeciesName = new Unity.Collections.FixedString64Bytes($"Species{i + 1}"),
                    CurrentPopulation = Random.Range(10, 50),
                    HealthyIndividuals = Random.Range(8, 45),
                    ReproductiveAdults = Random.Range(5, 30),
                    SurvivalRate = Random.Range(0.7f, 0.95f),
                    EnvironmentalFitness = Random.Range(0.5f, 0.9f),
                    GeneticDiversity = Random.Range(0.6f, 0.9f)
                });
            }

            UnityEngine.Debug.Log("🌍 Mock ecosystem created with 5 species populations");
        }

        /// <summary>
        /// Public API
        /// </summary>
        public static void OpenDashboard()
        {
            if (Instance != null && !Instance._isDashboardOpen)
            {
                Instance.ToggleDashboard();
            }
        }

        public static void AddNotification(string message)
        {
            UnityEngine.Debug.Log($"🔔 Ecosystem Notification: {message}");
            // In real implementation, would show notification UI
        }
    }

    public enum SpeciesSortMode
    {
        Population,
        Fitness,
        Name,
        Diversity
    }

    /// <summary>
    /// Individual species entry component
    /// </summary>
    public class SpeciesEntry : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _speciesName;
        [SerializeField] private TextMeshProUGUI _populationCount;
        [SerializeField] private TextMeshProUGUI _fitnessScore;
        [SerializeField] private Slider _healthBar;
        [SerializeField] private Image _statusIndicator;

        public void SetupEntry(CreaturePopulation population)
        {
            if (_speciesName != null)
                _speciesName.text = population.SpeciesName.ToString();

            if (_populationCount != null)
                _populationCount.text = population.CurrentPopulation.ToString();

            if (_fitnessScore != null)
                _fitnessScore.text = $"{population.EnvironmentalFitness:P0}";

            if (_healthBar != null)
                _healthBar.value = population.GetViabilityScore();

            if (_statusIndicator != null)
            {
                Color statusColor = population.GetViabilityScore() > 0.7f ? Color.green :
                                   population.GetViabilityScore() > 0.4f ? Color.yellow : Color.red;
                _statusIndicator.color = statusColor;
            }
        }
    }

    /// <summary>
    /// Individual event entry component
    /// </summary>
    public class EventEntry : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _eventName;
        [SerializeField] private TextMeshProUGUI _eventDuration;
        [SerializeField] private TextMeshProUGUI _eventIntensity;
        [SerializeField] private Slider _timeRemaining;
        [SerializeField] private Image _eventIcon;

        public void SetupEntry(EnvironmentalEvent envEvent)
        {
            if (_eventName != null)
                _eventName.text = envEvent.Type.ToString();

            if (_eventDuration != null)
                _eventDuration.text = $"{envEvent.TimeRemaining:F1}d";

            if (_eventIntensity != null)
                _eventIntensity.text = $"{envEvent.Intensity:P0}";

            if (_timeRemaining != null)
                _timeRemaining.value = envEvent.TimeRemaining / envEvent.Duration;

            if (_eventIcon != null)
                _eventIcon.color = GetEventColor(envEvent.Type);
        }

        private Color GetEventColor(Laboratory.Chimera.Ecosystem.Core.EcosystemEventType eventType)
        {
            return eventType switch
            {
                Laboratory.Chimera.Ecosystem.Core.EcosystemEventType.Drought => Color.yellow,
                Laboratory.Chimera.Ecosystem.Core.EcosystemEventType.Flood => Color.blue,
                Laboratory.Chimera.Ecosystem.Core.EcosystemEventType.Wildfire => Color.red,
                Laboratory.Chimera.Ecosystem.Core.EcosystemEventType.Disease => Color.magenta,
                Laboratory.Chimera.Ecosystem.Core.EcosystemEventType.Meteor => Color.gray,
                _ => Color.white
            };
        }
    }

    /// <summary>
    /// Simple chart components (placeholders for real chart library)
    /// </summary>
    public class LineChart : MonoBehaviour
    {
        public void UpdateChart(float[] data, string title)
        {
            // Placeholder for line chart implementation
            UnityEngine.Debug.Log($"Updating line chart: {title} with {data.Length} points");
        }
    }

    public class PieChart : MonoBehaviour
    {
        public void UpdateChart(float[] data, string[] labels)
        {
            // Placeholder for pie chart implementation
            UnityEngine.Debug.Log($"Updating pie chart with {data.Length} segments");
        }
    }

    public class BarChart : MonoBehaviour
    {
        public void UpdateChart(float[] data, string[] labels)
        {
            // Placeholder for bar chart implementation
            UnityEngine.Debug.Log($"Updating bar chart with {data.Length} bars");
        }
    }
}