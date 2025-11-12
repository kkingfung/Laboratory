using UnityEngine;
using Unity.Entities;
using Unity.Collections;
using Laboratory.Chimera.Genetics;
using Laboratory.Chimera.Configuration;
using Laboratory.Core.ECS;
using System.Collections.Generic;
using System.Linq;

namespace Laboratory.Chimera.Diagnostics
{
    /// <summary>
    /// Real-time genetic evolution visualizer that shows:
    /// - Trait distribution across populations
    /// - Generation-based evolution trends
    /// - Species diversity metrics
    /// - Mutation frequency analysis
    /// - Environmental adaptation patterns
    /// </summary>
    public class GeneticEvolutionVisualizer : MonoBehaviour
    {
        [Header("🧬 Visualization Settings")]
        [SerializeField] private bool enableVisualization = true;
        [SerializeField] private KeyCode toggleKey = KeyCode.F10;
        [SerializeField] private float updateInterval = 1f;
        [SerializeField] private int maxDataPoints = 100;

        [Header("📊 Display Options")]
        [SerializeField] private bool showTraitDistribution = true;
        [SerializeField] private bool showEvolutionTrends = true;
        [SerializeField] private bool showSpeciesDiversity = true;
        [SerializeField] private bool showMutationAnalysis = false;

        [Header("🎨 Visual Style")]
        [SerializeField] private Color primaryColor = Color.cyan;
        [SerializeField] private Color secondaryColor = Color.yellow;
        [SerializeField] private Color backgroundAlpha = new Color(0, 0, 0, 0.7f);
        [SerializeField] private int fontSize = 12;

        [Header("🎯 Tracked Traits")]
        [SerializeField] private string[] trackedTraits = new string[]
        {
            "Strength", "Vitality", "Agility", "Intelligence", "Resilience"
        };

        // Visualization data
        private bool visualizerVisible = false;
        private Rect windowRect = new Rect(50, 50, 800, 600);
        private Vector2 scrollPosition = Vector2.zero;
        private GUIStyle windowStyle;
        private GUIStyle headerStyle;
        private GUIStyle graphStyle;

        // Genetic data tracking
        private readonly Dictionary<string, List<float>> traitHistory = new Dictionary<string, List<float>>();
        private readonly Dictionary<int, SpeciesData> speciesData = new Dictionary<int, SpeciesData>();
        private readonly List<GenerationData> generationHistory = new List<GenerationData>();

        // ECS system references
        private EntityManager entityManager;
        private EntityQuery creatureQuery;

        // Performance optimization
        private float lastUpdateTime;
        private int currentGeneration = 1;
        private float averageFitness = 0f;
        private int totalCreatures = 0;
        private float mutationRate = 0f;

        private struct SpeciesData
        {
            public int speciesID;
            public string speciesName;
            public int population;
            public float averageFitness;
            public Dictionary<string, float> averageTraits;
            public int generation;
            public Color displayColor;
        }

        private struct GenerationData
        {
            public int generation;
            public float timestamp;
            public int totalPopulation;
            public float averageFitness;
            public Dictionary<string, float> averageTraits;
            public int speciesCount;
            public float diversityIndex;
        }

        private struct TraitDistribution
        {
            public string traitName;
            public float min;
            public float max;
            public float average;
            public float[] histogram; // 10 buckets
        }

        private void Start()
        {
            InitializeVisualizer();
        }

        private void InitializeVisualizer()
        {
            var world = Unity.Entities.World.DefaultGameObjectInjectionWorld;
            if (world?.IsCreated != true)
            {
                UnityEngine.Debug.LogError("GeneticEvolutionVisualizer: No ECS world found!");
                return;
            }

            entityManager = world.EntityManager;

            creatureQuery = entityManager.CreateEntityQuery(
                ComponentType.ReadOnly<CreatureData>(),
                ComponentType.ReadOnly<DynamicBuffer<CreatureGeneticTrait>>(),
                ComponentType.ReadOnly<CreatureSimulationTag>()
            );

            // Initialize trait history
            foreach (string trait in trackedTraits)
            {
                traitHistory[trait] = new List<float>();
            }

            UnityEngine.Debug.Log("✅ Genetic Evolution Visualizer initialized");
        }

        private void Update()
        {
            if (!enableVisualization) return;

            // Toggle visualizer
            if (Input.GetKeyDown(toggleKey))
            {
                visualizerVisible = !visualizerVisible;
            }

            // Update genetic data periodically
            if (Time.time - lastUpdateTime >= updateInterval)
            {
                UpdateGeneticData();
                lastUpdateTime = Time.time;
            }
        }

        private void UpdateGeneticData()
        {
            if (entityManager == null || !entityManager.IsQueryValid(creatureQuery)) return;

            // Clear current data
            speciesData.Clear();
            var traitSums = new Dictionary<string, List<float>>();
            foreach (string trait in trackedTraits)
            {
                traitSums[trait] = new List<float>();
            }

            totalCreatures = 0;
            float fitnessSum = 0f;
            int maxGeneration = 0;

            // Collect data from all creatures
            using (var entities = creatureQuery.ToEntityArray(Allocator.TempJob))
            using (var creatureDataArray = creatureQuery.ToComponentDataArray<CreatureData>(Allocator.TempJob))
            {
                for (int i = 0; i < entities.Length; i++)
                {
                    var entity = entities[i];
                    var creatureData = creatureDataArray[i];

                    if (!creatureData.isAlive) continue;

                    totalCreatures++;
                    maxGeneration = Mathf.Max(maxGeneration, creatureData.generation);

                    // Get genetic traits
                    var traits = entityManager.GetBuffer<CreatureGeneticTrait>(entity);
                    var creatureTraits = new Dictionary<string, float>();

                    foreach (var trait in traits)
                    {
                        string traitName = GetTraitName(trait.traitName);
                        creatureTraits[traitName] = trait.value;

                        if (traitSums.ContainsKey(traitName))
                        {
                            traitSums[traitName].Add(trait.value);
                        }
                    }

                    // Calculate creature fitness (simplified)
                    float fitness = CalculateCreatureFitness(creatureTraits);
                    fitnessSum += fitness;

                    // Update species data
                    if (!speciesData.ContainsKey(creatureData.speciesID))
                    {
                        speciesData[creatureData.speciesID] = new SpeciesData
                        {
                            speciesID = creatureData.speciesID,
                            speciesName = GetSpeciesName(creatureData.speciesID),
                            population = 0,
                            averageFitness = 0f,
                            averageTraits = new Dictionary<string, float>(),
                            generation = creatureData.generation,
                            displayColor = GetSpeciesColor(creatureData.speciesID)
                        };
                    }

                    var species = speciesData[creatureData.speciesID];
                    species.population++;
                    species.averageFitness = (species.averageFitness + fitness) / 2f;

                    // Update species trait averages
                    foreach (var trait in creatureTraits)
                    {
                        if (!species.averageTraits.ContainsKey(trait.Key))
                            species.averageTraits[trait.Key] = 0f;

                        species.averageTraits[trait.Key] =
                            (species.averageTraits[trait.Key] + trait.Value) / 2f;
                    }

                    speciesData[creatureData.speciesID] = species;
                }
            }

            // Update global statistics
            currentGeneration = maxGeneration;
            averageFitness = totalCreatures > 0 ? fitnessSum / totalCreatures : 0f;

            // Update trait history
            foreach (var trait in traitSums)
            {
                if (trait.Value.Count > 0)
                {
                    float average = trait.Value.Average();
                    traitHistory[trait.Key].Add(average);

                    // Limit history size
                    if (traitHistory[trait.Key].Count > maxDataPoints)
                    {
                        traitHistory[trait.Key].RemoveAt(0);
                    }
                }
            }

            // Record generation data
            if (totalCreatures > 0)
            {
                var generationData = new GenerationData
                {
                    generation = currentGeneration,
                    timestamp = Time.time,
                    totalPopulation = totalCreatures,
                    averageFitness = averageFitness,
                    averageTraits = traitSums.ToDictionary(t => t.Key, t => t.Value.Count > 0 ? t.Value.Average() : 0f),
                    speciesCount = speciesData.Count,
                    diversityIndex = CalculateDiversityIndex()
                };

                generationHistory.Add(generationData);

                // Limit generation history
                if (generationHistory.Count > maxDataPoints)
                {
                    generationHistory.RemoveAt(0);
                }
            }
        }

        private void OnGUI()
        {
            if (!enableVisualization || !visualizerVisible) return;

            InitializeGUIStyles();
            windowRect = GUILayout.Window(12345, windowRect, DrawVisualizerWindow, "🧬 Genetic Evolution Visualizer", windowStyle);
        }

        private void InitializeGUIStyles()
        {
            if (windowStyle == null)
            {
                windowStyle = new GUIStyle(GUI.skin.window);
                windowStyle.fontSize = fontSize;
            }

            if (headerStyle == null)
            {
                headerStyle = new GUIStyle(GUI.skin.label)
                {
                    fontStyle = FontStyle.Bold,
                    fontSize = fontSize + 2,
                    normal = { textColor = primaryColor }
                };
            }

            if (graphStyle == null)
            {
                graphStyle = new GUIStyle(GUI.skin.box)
                {
                    normal = { background = MakeBackgroundTexture() }
                };
            }
        }

        private void DrawVisualizerWindow(int windowID)
        {
            scrollPosition = GUILayout.BeginScrollView(scrollPosition);

            // Summary statistics
            DrawSummaryStatistics();

            GUILayout.Space(10);

            // Trait distribution
            if (showTraitDistribution)
            {
                DrawTraitDistribution();
                GUILayout.Space(10);
            }

            // Evolution trends
            if (showEvolutionTrends)
            {
                DrawEvolutionTrends();
                GUILayout.Space(10);
            }

            // Species diversity
            if (showSpeciesDiversity)
            {
                DrawSpeciesDiversity();
                GUILayout.Space(10);
            }

            // Mutation analysis
            if (showMutationAnalysis)
            {
                DrawMutationAnalysis();
                GUILayout.Space(10);
            }

            GUILayout.EndScrollView();

            GUI.DragWindow();
        }

        private void DrawSummaryStatistics()
        {
            GUILayout.Label("📊 Population Summary", headerStyle);

            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical();
            GUILayout.Label($"Total Creatures: {totalCreatures}");
            GUILayout.Label($"Current Generation: {currentGeneration}");
            GUILayout.Label($"Species Count: {speciesData.Count}");
            GUILayout.EndVertical();

            GUILayout.BeginVertical();
            GUILayout.Label($"Average Fitness: {averageFitness:F2}");
            GUILayout.Label($"Diversity Index: {CalculateDiversityIndex():F3}");
            GUILayout.Label($"Mutation Rate: {mutationRate:P1}");
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
        }

        private void DrawTraitDistribution()
        {
            GUILayout.Label("📈 Trait Distribution", headerStyle);

            foreach (string trait in trackedTraits)
            {
                if (traitHistory.ContainsKey(trait) && traitHistory[trait].Count > 0)
                {
                    var distribution = CalculateTraitDistribution(trait);
                    DrawTraitHistogram(trait, distribution);
                }
            }
        }

        private void DrawEvolutionTrends()
        {
            GUILayout.Label("📉 Evolution Trends", headerStyle);

            if (generationHistory.Count > 1)
            {
                // Draw fitness trend
                DrawTrendGraph("Average Fitness",
                    generationHistory.Select(g => g.averageFitness).ToArray(),
                    primaryColor);

                // Draw population trend
                DrawTrendGraph("Population Size",
                    generationHistory.Select(g => (float)g.totalPopulation).ToArray(),
                    secondaryColor);
            }
            else
            {
                GUILayout.Label("Collecting data...");
            }
        }

        private void DrawSpeciesDiversity()
        {
            GUILayout.Label("🌈 Species Diversity", headerStyle);

            foreach (var species in speciesData.Values.OrderByDescending(s => s.population))
            {
                GUILayout.BeginHorizontal();

                // Species color indicator
                var colorRect = GUILayoutUtility.GetRect(20, 20);
                EditorGUI.DrawRect(colorRect, species.displayColor);

                GUILayout.BeginVertical();
                GUILayout.Label($"{species.speciesName} (Gen {species.generation})");
                GUILayout.Label($"Population: {species.population} | Fitness: {species.averageFitness:F2}");

                // Top traits for this species
                if (species.averageTraits.Count > 0)
                {
                    var topTraits = species.averageTraits
                        .OrderByDescending(t => t.Value)
                        .Take(3)
                        .Select(t => $"{t.Key}: {t.Value:F2}");
                    GUILayout.Label($"Top Traits: {string.Join(", ", topTraits)}");
                }

                GUILayout.EndVertical();
                GUILayout.EndHorizontal();

                GUILayout.Space(5);
            }
        }

        private void DrawMutationAnalysis()
        {
            GUILayout.Label("🧪 Mutation Analysis", headerStyle);

            // This would analyze mutation patterns
            GUILayout.Label("Mutation frequency analysis would go here");
            GUILayout.Label("Beneficial vs. harmful mutation ratios");
            GUILayout.Label("Trait-specific mutation impacts");
        }

        private void DrawTraitHistogram(string traitName, TraitDistribution distribution)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label($"{traitName}:", GUILayout.Width(80));
            GUILayout.Label($"Avg: {distribution.average:F2}", GUILayout.Width(60));
            GUILayout.Label($"Range: {distribution.min:F2}-{distribution.max:F2}", GUILayout.Width(100));

            // Simple text-based histogram
            var histogramRect = GUILayoutUtility.GetRect(200, 20);
            DrawHistogramBars(histogramRect, distribution.histogram, primaryColor);

            GUILayout.EndHorizontal();
        }

        private void DrawTrendGraph(string title, float[] data, Color color)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label($"{title}:", GUILayout.Width(120));

            var graphRect = GUILayoutUtility.GetRect(300, 60);
            DrawTrendLine(graphRect, data, color);

            GUILayout.EndHorizontal();
        }

        private void DrawHistogramBars(Rect rect, float[] data, Color color)
        {
            if (data == null || data.Length == 0) return;

            float maxValue = data.Max();
            if (maxValue <= 0) return;

            float barWidth = rect.width / data.Length;

            for (int i = 0; i < data.Length; i++)
            {
                float barHeight = (data[i] / maxValue) * rect.height;
                var barRect = new Rect(
                    rect.x + i * barWidth,
                    rect.y + rect.height - barHeight,
                    barWidth - 1,
                    barHeight
                );

                EditorGUI.DrawRect(barRect, color);
            }
        }

        private void DrawTrendLine(Rect rect, float[] data, Color color)
        {
            if (data == null || data.Length < 2) return;

            float minValue = data.Min();
            float maxValue = data.Max();
            float range = maxValue - minValue;

            if (range <= 0) return;

            // Draw trend line using GUI.DrawTexture with colored pixels
            // This is a simplified representation
            var prevPoint = Vector2.zero;

            for (int i = 0; i < data.Length; i++)
            {
                float x = rect.x + (i / (float)(data.Length - 1)) * rect.width;
                float y = rect.y + rect.height - ((data[i] - minValue) / range) * rect.height;

                var currentPoint = new Vector2(x, y);

                if (i > 0)
                {
                    // Draw line segment (simplified as points)
                    var pointRect = new Rect(currentPoint.x - 1, currentPoint.y - 1, 2, 2);
                    EditorGUI.DrawRect(pointRect, color);
                }

                prevPoint = currentPoint;
            }
        }

        private TraitDistribution CalculateTraitDistribution(string traitName)
        {
            var history = traitHistory[traitName];
            if (history.Count == 0)
            {
                return new TraitDistribution
                {
                    traitName = traitName,
                    histogram = new float[10]
                };
            }

            var distribution = new TraitDistribution
            {
                traitName = traitName,
                min = history.Min(),
                max = history.Max(),
                average = history.Average(),
                histogram = new float[10]
            };

            // Calculate histogram
            float range = distribution.max - distribution.min;
            if (range > 0)
            {
                foreach (float value in history)
                {
                    int bucket = Mathf.FloorToInt(((value - distribution.min) / range) * 9.99f);
                    bucket = Mathf.Clamp(bucket, 0, 9);
                    distribution.histogram[bucket]++;
                }
            }

            return distribution;
        }

        private float CalculateCreatureFitness(Dictionary<string, float> traits)
        {
            // Simplified fitness calculation
            float fitness = 0f;
            foreach (var trait in traits)
            {
                fitness += trait.Value * GetTraitWeight(trait.Key);
            }
            return fitness / traits.Count;
        }

        private float GetTraitWeight(string traitName)
        {
            // Trait importance weights
            return traitName switch
            {
                "Strength" => 1.2f,
                "Vitality" => 1.5f,
                "Intelligence" => 1.1f,
                "Agility" => 1.0f,
                "Resilience" => 1.3f,
                _ => 1.0f
            };
        }

        private float CalculateDiversityIndex()
        {
            if (speciesData.Count <= 1) return 0f;

            // Shannon diversity index
            float totalPopulation = speciesData.Values.Sum(s => s.population);
            float diversity = 0f;

            foreach (var species in speciesData.Values)
            {
                float proportion = species.population / totalPopulation;
                if (proportion > 0)
                {
                    diversity -= proportion * Mathf.Log(proportion);
                }
            }

            return diversity;
        }

        private string GetTraitName(int traitHash)
        {
            // Convert hash back to trait name (simplified)
            foreach (string trait in trackedTraits)
            {
                if (trait.GetHashCode() == traitHash)
                    return trait;
            }
            return $"Trait_{traitHash}";
        }

        private string GetSpeciesName(int speciesID)
        {
            return $"Species_{Mathf.Abs(speciesID % 1000)}";
        }

        private Color GetSpeciesColor(int speciesID)
        {
            // Generate consistent colors for species
            Random.InitState(speciesID);
            return new Color(
                Random.Range(0.3f, 1f),
                Random.Range(0.3f, 1f),
                Random.Range(0.3f, 1f),
                1f
            );
        }

        private Texture2D MakeBackgroundTexture()
        {
            var texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, backgroundAlpha);
            texture.Apply();
            return texture;
        }

        private void OnDestroy()
        {
            if (entityManager != null && entityManager.IsQueryValid(creatureQuery))
                creatureQuery.Dispose();
        }

        private static class EditorGUI
        {
            public static void DrawRect(Rect rect, Color color)
            {
                var originalColor = GUI.color;
                GUI.color = color;
                GUI.DrawTexture(rect, Texture2D.whiteTexture);
                GUI.color = originalColor;
            }
        }

        /// <summary>
        /// Export genetic data to CSV for external analysis
        /// </summary>
        [ContextMenu("Export Genetic Data")]
        public void ExportGeneticData()
        {
            // This would export current genetic data to a CSV file
            UnityEngine.Debug.Log("🧬 Genetic data export would be implemented here");
        }
    }
}