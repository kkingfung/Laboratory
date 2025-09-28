using UnityEngine;
using Laboratory.Core.Performance;

namespace Laboratory.Core.Performance
{
    /// <summary>
    /// Demonstration script showing the benefits of optimized update patterns.
    /// Compares traditional Update() with optimized frequency-based updates.
    /// </summary>
    public class UpdateOptimizationDemo : MonoBehaviour
    {
        [Header("Demo Settings")]
        [SerializeField] private bool enableTraditionalUpdates = true;
        [SerializeField] private bool enableOptimizedUpdates = true;
        [SerializeField] private int systemCount = 50;

        [Header("Performance Monitoring")]
        [SerializeField] private bool logPerformanceStats = true;
        [SerializeField] private float logInterval = 5f;

        private TraditionalUpdateSystem[] traditionalSystems;
        private OptimizedUpdateSystem[] optimizedSystems;
        private float lastLogTime;

        private void Start()
        {
            CreateDemoSystems();
            InvokeRepeating(nameof(LogPerformanceComparison), logInterval, logInterval);
        }

        private void CreateDemoSystems()
        {
            // Create traditional Update() systems
            if (enableTraditionalUpdates)
            {
                var traditionalParent = new GameObject("Traditional Systems");
                traditionalParent.transform.SetParent(transform);

                traditionalSystems = new TraditionalUpdateSystem[systemCount];
                for (int i = 0; i < systemCount; i++)
                {
                    var go = new GameObject($"Traditional_{i}");
                    go.transform.SetParent(traditionalParent.transform);
                    traditionalSystems[i] = go.AddComponent<TraditionalUpdateSystem>();
                }
            }

            // Create optimized update systems
            if (enableOptimizedUpdates)
            {
                var optimizedParent = new GameObject("Optimized Systems");
                optimizedParent.transform.SetParent(transform);

                optimizedSystems = new OptimizedUpdateSystem[systemCount];
                for (int i = 0; i < systemCount; i++)
                {
                    var go = new GameObject($"Optimized_{i}");
                    go.transform.SetParent(optimizedParent.transform);
                    optimizedSystems[i] = go.AddComponent<OptimizedUpdateSystem>();
                }
            }
        }

        private void LogPerformanceComparison()
        {
            if (!logPerformanceStats) return;

            var stats = OptimizedUpdateManager.Instance.GetStatistics();

            Debug.Log($"[Update Optimization Demo] Performance Comparison:\n" +
                     $"  Traditional Systems: {(traditionalSystems?.Length ?? 0)} running at 60 FPS = {(traditionalSystems?.Length ?? 0) * 60} updates/sec\n" +
                     $"  Optimized Systems: {stats.TotalRegisteredSystems} total, {stats.SystemsUpdatedThisFrame} updated this frame\n" +
                     $"  Estimated Update Reduction: {CalculateUpdateReduction():F1}%\n" +
                     $"  Frame Time: {stats.AverageUpdateTime:F3}ms");

            // Log breakdown by frequency
            foreach (var kvp in stats.SystemsByFrequency)
            {
                Debug.Log($"    {kvp.Key}: {kvp.Value} systems");
            }
        }

        private float CalculateUpdateReduction()
        {
            var stats = OptimizedUpdateManager.Instance.GetStatistics();
            int traditionalUpdatesPerSecond = (traditionalSystems?.Length ?? 0) * 60;
            int optimizedUpdatesPerSecond = 0;

            // Estimate optimized updates per second based on frequencies
            foreach (var kvp in stats.SystemsByFrequency)
            {
                int frequency = kvp.Key switch
                {
                    OptimizedUpdateManager.UpdateFrequency.EveryFrame => 60,
                    OptimizedUpdateManager.UpdateFrequency.HighFrequency => 30,
                    OptimizedUpdateManager.UpdateFrequency.MediumFrequency => 15,
                    OptimizedUpdateManager.UpdateFrequency.LowFrequency => 5,
                    OptimizedUpdateManager.UpdateFrequency.VeryLowFrequency => 1,
                    _ => 60
                };
                optimizedUpdatesPerSecond += kvp.Value * frequency;
            }

            int totalTraditionalUpdates = traditionalUpdatesPerSecond + (stats.TotalRegisteredSystems * 60);
            int totalOptimizedUpdates = traditionalUpdatesPerSecond + optimizedUpdatesPerSecond;

            if (totalTraditionalUpdates == 0) return 0f;

            return ((float)(totalTraditionalUpdates - totalOptimizedUpdates) / totalTraditionalUpdates) * 100f;
        }

        [ContextMenu("Log Current Stats")]
        private void LogCurrentStats()
        {
            LogPerformanceComparison();
        }
    }

    /// <summary>
    /// Traditional MonoBehaviour with Update() for comparison
    /// </summary>
    public class TraditionalUpdateSystem : MonoBehaviour
    {
        private float value;
        private int updateCount;

        private void Update()
        {
            // Simulate some lightweight work
            value = Mathf.Sin(Time.time + GetInstanceID());
            updateCount++;

            // Simulate occasional heavier work
            if (updateCount % 60 == 0)
            {
                transform.position = new Vector3(value, 0, 0);
            }
        }
    }

    /// <summary>
    /// Optimized system using OptimizedMonoBehaviour
    /// </summary>
    public class OptimizedUpdateSystem : OptimizedMonoBehaviour
    {
        private float value;
        private int updateCount;

        protected override void Start()
        {
            base.Start();

            // Randomize update frequency for demonstration
            var frequencies = System.Enum.GetValues(typeof(OptimizedUpdateManager.UpdateFrequency));
            var randomFrequency = (OptimizedUpdateManager.UpdateFrequency)frequencies.GetValue(Random.Range(0, frequencies.Length));
            updateFrequency = randomFrequency;
        }

        public override void OnOptimizedUpdate(float deltaTime)
        {
            // Same work as traditional system, but at optimized frequency
            value = Mathf.Sin(Time.time + GetInstanceID());
            updateCount++;

            // Simulate occasional heavier work (frequency-adjusted)
            float frequencyMultiplier = updateFrequency switch
            {
                OptimizedUpdateManager.UpdateFrequency.EveryFrame => 60f,
                OptimizedUpdateManager.UpdateFrequency.HighFrequency => 30f,
                OptimizedUpdateManager.UpdateFrequency.MediumFrequency => 15f,
                OptimizedUpdateManager.UpdateFrequency.LowFrequency => 5f,
                OptimizedUpdateManager.UpdateFrequency.VeryLowFrequency => 1f,
                _ => 60f
            };

            if (updateCount % Mathf.Max(1, (int)frequencyMultiplier) == 0)
            {
                transform.position = new Vector3(value, 0, 0);
            }
        }
    }
}