using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Laboratory.Tools
{
    /// <summary>
    /// A/B testing framework for gameplay balance and feature testing.
    /// Allows designers to test multiple configurations simultaneously and analyze results.
    /// Provides statistical analysis to determine winning variants.
    /// </summary>
    public class ABTestingFramework : MonoBehaviour
    {
        #region Configuration

        [Header("Testing Settings")]
        [SerializeField] private bool enableABTesting = true;
        [SerializeField] private bool logTestEvents = true;
        [SerializeField] private int minSampleSize = 30;
        [SerializeField] private float confidenceLevel = 0.95f; // 95% confidence

        [Header("Persistence")]
        [SerializeField] private bool saveTestResults = true;
        [SerializeField] private string saveDirectory = "ABTestResults";

        #endregion

        #region Private Fields

        private static ABTestingFramework _instance;
        private readonly Dictionary<string, ABTest> _activeTests = new Dictionary<string, ABTest>();
        private readonly Dictionary<string, TestResults> _completedTests = new Dictionary<string, TestResults>();

        // Current user's assigned variants
        private readonly Dictionary<string, string> _userAssignments = new Dictionary<string, string>();

        // Statistics
        private int _totalTests;
        private int _totalVariantsEvaluated;
        private int _totalEventsRecorded;

        #endregion

        #region Properties

        public static ABTestingFramework Instance => _instance;
        public bool IsTestingEnabled => enableABTesting;
        public int ActiveTestCount => _activeTests.Count;
        public int CompletedTestCount => _completedTests.Count;

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

        private void OnApplicationQuit()
        {
            if (saveTestResults)
            {
                SaveAllTestResults();
            }
        }

        #endregion

        #region Initialization

        private void Initialize()
        {
            Debug.Log("[ABTestingFramework] Initializing...");

            // Load previous test results if available
            if (saveTestResults)
            {
                LoadTestResults();
            }

            Debug.Log("[ABTestingFramework] Initialized");
        }

        #endregion

        #region Test Management

        /// <summary>
        /// Create a new A/B test with multiple variants.
        /// </summary>
        public ABTest CreateTest(string testName, string description, params string[] variantNames)
        {
            if (_activeTests.ContainsKey(testName))
            {
                Debug.LogWarning($"[ABTestingFramework] Test '{testName}' already exists");
                return _activeTests[testName];
            }

            var test = new ABTest
            {
                testName = testName,
                description = description,
                startTime = DateTime.UtcNow,
                isActive = true
            };

            // Create variants
            foreach (var variantName in variantNames)
            {
                test.variants.Add(new TestVariant
                {
                    variantName = variantName,
                    testName = testName
                });
            }

            _activeTests[testName] = test;
            _totalTests++;

            if (logTestEvents)
            {
                Debug.Log($"[ABTestingFramework] Created test: {testName} with {variantNames.Length} variants");
            }

            return test;
        }

        /// <summary>
        /// Get or create a test.
        /// </summary>
        public ABTest GetOrCreateTest(string testName, string description, params string[] variantNames)
        {
            if (_activeTests.TryGetValue(testName, out var existingTest))
            {
                return existingTest;
            }

            return CreateTest(testName, description, variantNames);
        }

        /// <summary>
        /// Get the assigned variant for a user in a test.
        /// </summary>
        public string GetAssignedVariant(string testName)
        {
            if (!enableABTesting)
                return null;

            // Check if user already has an assignment
            if (_userAssignments.TryGetValue(testName, out var assigned))
            {
                return assigned;
            }

            // Assign a new variant
            if (_activeTests.TryGetValue(testName, out var test))
            {
                var variant = AssignVariant(test);
                _userAssignments[testName] = variant.variantName;
                return variant.variantName;
            }

            return null;
        }

        /// <summary>
        /// Check if a specific variant is active for the user.
        /// </summary>
        public bool IsVariantActive(string testName, string variantName)
        {
            var assigned = GetAssignedVariant(testName);
            return assigned == variantName;
        }

        private TestVariant AssignVariant(ABTest test)
        {
            if (test.variants.Count == 0)
                return null;

            // Simple round-robin assignment to ensure even distribution
            var leastAssigned = test.variants.OrderBy(v => v.assignedUsers).First();
            leastAssigned.assignedUsers++;

            return leastAssigned;
        }

        #endregion

        #region Event Recording

        /// <summary>
        /// Record an event for the currently assigned variant.
        /// </summary>
        public void RecordEvent(string testName, string eventType, float value = 1f)
        {
            if (!enableABTesting) return;

            var variantName = GetAssignedVariant(testName);
            if (variantName == null) return;

            RecordEventForVariant(testName, variantName, eventType, value);
        }

        /// <summary>
        /// Record an event for a specific variant.
        /// </summary>
        public void RecordEventForVariant(string testName, string variantName, string eventType, float value)
        {
            if (!_activeTests.TryGetValue(testName, out var test))
            {
                Debug.LogWarning($"[ABTestingFramework] Test '{testName}' not found");
                return;
            }

            var variant = test.variants.FirstOrDefault(v => v.variantName == variantName);
            if (variant == null)
            {
                Debug.LogWarning($"[ABTestingFramework] Variant '{variantName}' not found in test '{testName}'");
                return;
            }

            // Record the event
            if (!variant.events.ContainsKey(eventType))
            {
                variant.events[eventType] = new List<float>();
            }

            variant.events[eventType].Add(value);
            variant.totalEvents++;
            _totalEventsRecorded++;

            if (logTestEvents)
            {
                Debug.Log($"[ABTestingFramework] Recorded event: {testName}/{variantName}/{eventType} = {value}");
            }
        }

        /// <summary>
        /// Record a conversion (success event) for the current variant.
        /// </summary>
        public void RecordConversion(string testName)
        {
            RecordEvent(testName, "conversion", 1f);
        }

        /// <summary>
        /// Record a goal achievement with optional value.
        /// </summary>
        public void RecordGoal(string testName, string goalName, float value = 1f)
        {
            RecordEvent(testName, $"goal_{goalName}", value);
        }

        #endregion

        #region Analysis

        /// <summary>
        /// Analyze test results and determine the winning variant.
        /// </summary>
        public TestResults AnalyzeTest(string testName, string primaryMetric = "conversion")
        {
            if (!_activeTests.TryGetValue(testName, out var test))
            {
                Debug.LogError($"[ABTestingFramework] Test '{testName}' not found");
                return null;
            }

            var results = new TestResults
            {
                testName = testName,
                analysisTime = DateTime.UtcNow,
                primaryMetric = primaryMetric,
                sampleSize = test.variants.Sum(v => v.assignedUsers)
            };

            // Calculate metrics for each variant
            foreach (var variant in test.variants)
            {
                var metrics = CalculateVariantMetrics(variant, primaryMetric);
                results.variantMetrics[variant.variantName] = metrics;
            }

            // Determine winner
            if (results.sampleSize >= minSampleSize)
            {
                results.winner = DetermineWinner(results.variantMetrics, primaryMetric);
                results.isStatisticallySignificant = CheckStatisticalSignificance(results.variantMetrics, primaryMetric);
            }
            else
            {
                results.winner = "INSUFFICIENT_DATA";
                results.isStatisticallySignificant = false;
            }

            if (logTestEvents)
            {
                Debug.Log($"[ABTestingFramework] Analysis complete for '{testName}': Winner = {results.winner} (n={results.sampleSize})");
            }

            return results;
        }

        private VariantMetrics CalculateVariantMetrics(TestVariant variant, string primaryMetric)
        {
            var metrics = new VariantMetrics
            {
                variantName = variant.variantName,
                sampleSize = variant.assignedUsers,
                totalEvents = variant.totalEvents
            };

            // Calculate conversion rate
            if (variant.events.ContainsKey(primaryMetric))
            {
                var conversions = variant.events[primaryMetric];
                metrics.conversionRate = conversions.Count / (float)variant.assignedUsers;
                metrics.totalConversions = conversions.Count;
            }

            // Calculate average event values
            foreach (var kvp in variant.events)
            {
                var eventValues = kvp.Value;
                if (eventValues.Count > 0)
                {
                    metrics.averageValues[kvp.Key] = eventValues.Average();
                }
            }

            // Calculate standard deviation for primary metric
            if (variant.events.ContainsKey(primaryMetric))
            {
                var values = variant.events[primaryMetric];
                if (values.Count > 1)
                {
                    var avg = values.Average();
                    var sumSquares = values.Sum(v => (v - avg) * (v - avg));
                    metrics.standardDeviation = Mathf.Sqrt((float)(sumSquares / (values.Count - 1)));
                }
            }

            return metrics;
        }

        private string DetermineWinner(Dictionary<string, VariantMetrics> variantMetrics, string primaryMetric)
        {
            if (variantMetrics.Count == 0)
                return "NO_VARIANTS";

            // Find variant with highest conversion rate
            var winner = variantMetrics.OrderByDescending(v => v.Value.conversionRate).First();
            return winner.Key;
        }

        private bool CheckStatisticalSignificance(Dictionary<string, VariantMetrics> variantMetrics, string primaryMetric)
        {
            if (variantMetrics.Count < 2)
                return false;

            // Simplified significance check (in production, use proper statistical tests like Z-test)
            var variants = variantMetrics.Values.OrderByDescending(v => v.conversionRate).ToList();

            if (variants.Count < 2)
                return false;

            var best = variants[0];
            var secondBest = variants[1];

            // Check if difference is significant (simple threshold check)
            float difference = best.conversionRate - secondBest.conversionRate;
            float relativeDifference = difference / secondBest.conversionRate;

            // Require at least 10% improvement and minimum sample size
            return relativeDifference > 0.1f && best.sampleSize >= minSampleSize;
        }

        #endregion

        #region Test Control

        /// <summary>
        /// End a test and save results.
        /// </summary>
        public TestResults EndTest(string testName)
        {
            if (!_activeTests.TryGetValue(testName, out var test))
            {
                Debug.LogWarning($"[ABTestingFramework] Test '{testName}' not found");
                return null;
            }

            test.isActive = false;
            test.endTime = DateTime.UtcNow;

            var results = AnalyzeTest(testName);
            _completedTests[testName] = results;

            _activeTests.Remove(testName);

            if (saveTestResults)
            {
                SaveTestResults(testName, results);
            }

            if (logTestEvents)
            {
                Debug.Log($"[ABTestingFramework] Test ended: {testName}");
            }

            return results;
        }

        /// <summary>
        /// Reset a test (clear all data).
        /// </summary>
        public void ResetTest(string testName)
        {
            if (_activeTests.TryGetValue(testName, out var test))
            {
                foreach (var variant in test.variants)
                {
                    variant.assignedUsers = 0;
                    variant.totalEvents = 0;
                    variant.events.Clear();
                }

                _userAssignments.Remove(testName);

                if (logTestEvents)
                {
                    Debug.Log($"[ABTestingFramework] Test reset: {testName}");
                }
            }
        }

        /// <summary>
        /// Delete a test completely.
        /// </summary>
        public void DeleteTest(string testName)
        {
            _activeTests.Remove(testName);
            _completedTests.Remove(testName);
            _userAssignments.Remove(testName);

            if (logTestEvents)
            {
                Debug.Log($"[ABTestingFramework] Test deleted: {testName}");
            }
        }

        #endregion

        #region Persistence

        private void SaveTestResults(string testName, TestResults results)
        {
            try
            {
                string directory = System.IO.Path.Combine(Application.persistentDataPath, saveDirectory);
                System.IO.Directory.CreateDirectory(directory);

                string filename = $"{testName}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.json";
                string path = System.IO.Path.Combine(directory, filename);

                string json = JsonUtility.ToJson(results, true);
                System.IO.File.WriteAllText(path, json);

                if (logTestEvents)
                {
                    Debug.Log($"[ABTestingFramework] Saved test results: {path}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ABTestingFramework] Failed to save test results: {ex.Message}");
            }
        }

        private void SaveAllTestResults()
        {
            foreach (var kvp in _activeTests)
            {
                var results = AnalyzeTest(kvp.Key);
                if (results != null)
                {
                    SaveTestResults(kvp.Key, results);
                }
            }
        }

        private void LoadTestResults()
        {
            // Load previous results for reference
            try
            {
                string directory = System.IO.Path.Combine(Application.persistentDataPath, saveDirectory);
                if (!System.IO.Directory.Exists(directory))
                    return;

                var files = System.IO.Directory.GetFiles(directory, "*.json");
                foreach (var file in files)
                {
                    string json = System.IO.File.ReadAllText(file);
                    var results = JsonUtility.FromJson<TestResults>(json);

                    if (results != null)
                    {
                        _completedTests[results.testName] = results;
                    }
                }

                if (files.Length > 0 && logTestEvents)
                {
                    Debug.Log($"[ABTestingFramework] Loaded {files.Length} previous test results");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ABTestingFramework] Failed to load test results: {ex.Message}");
            }
        }

        #endregion

        #region Public API

        /// <summary>
        /// Get all active tests.
        /// </summary>
        public List<ABTest> GetActiveTests()
        {
            return new List<ABTest>(_activeTests.Values);
        }

        /// <summary>
        /// Get all completed tests.
        /// </summary>
        public List<TestResults> GetCompletedTests()
        {
            return new List<TestResults>(_completedTests.Values);
        }

        /// <summary>
        /// Get statistics for the A/B testing system.
        /// </summary>
        public ABTestingStats GetStats()
        {
            return new ABTestingStats
            {
                totalTests = _totalTests,
                activeTests = _activeTests.Count,
                completedTests = _completedTests.Count,
                totalVariantsEvaluated = _totalVariantsEvaluated,
                totalEventsRecorded = _totalEventsRecorded,
                isEnabled = enableABTesting
            };
        }

        #endregion

        #region Context Menu

        [ContextMenu("Print Statistics")]
        private void PrintStatistics()
        {
            var stats = GetStats();
            Debug.Log($"=== A/B Testing Statistics ===\n" +
                      $"Total Tests: {stats.totalTests}\n" +
                      $"Active Tests: {stats.activeTests}\n" +
                      $"Completed Tests: {stats.completedTests}\n" +
                      $"Total Events: {stats.totalEventsRecorded}\n" +
                      $"Enabled: {stats.isEnabled}");
        }

        [ContextMenu("List Active Tests")]
        private void ListActiveTests()
        {
            foreach (var test in _activeTests.Values)
            {
                Debug.Log($"Test: {test.testName} - {test.variants.Count} variants, {test.variants.Sum(v => v.assignedUsers)} users");
            }
        }

        #endregion
    }

    #region Data Structures

    /// <summary>
    /// Represents an A/B test with multiple variants.
    /// </summary>
    [Serializable]
    public class ABTest
    {
        public string testName;
        public string description;
        public DateTime startTime;
        public DateTime endTime;
        public bool isActive;
        public List<TestVariant> variants = new List<TestVariant>();
    }

    /// <summary>
    /// Represents a variant in an A/B test.
    /// </summary>
    [Serializable]
    public class TestVariant
    {
        public string variantName;
        public string testName;
        public int assignedUsers;
        public int totalEvents;
        public Dictionary<string, List<float>> events = new Dictionary<string, List<float>>();
    }

    /// <summary>
    /// Results and analysis of an A/B test.
    /// </summary>
    [Serializable]
    public class TestResults
    {
        public string testName;
        public DateTime analysisTime;
        public string primaryMetric;
        public int sampleSize;
        public string winner;
        public bool isStatisticallySignificant;
        public Dictionary<string, VariantMetrics> variantMetrics = new Dictionary<string, VariantMetrics>();
    }

    /// <summary>
    /// Metrics for a test variant.
    /// </summary>
    [Serializable]
    public class VariantMetrics
    {
        public string variantName;
        public int sampleSize;
        public int totalEvents;
        public int totalConversions;
        public float conversionRate;
        public float standardDeviation;
        public Dictionary<string, float> averageValues = new Dictionary<string, float>();
    }

    /// <summary>
    /// Statistics for the A/B testing system.
    /// </summary>
    [Serializable]
    public struct ABTestingStats
    {
        public int totalTests;
        public int activeTests;
        public int completedTests;
        public int totalVariantsEvaluated;
        public int totalEventsRecorded;
        public bool isEnabled;
    }

    #endregion
}
