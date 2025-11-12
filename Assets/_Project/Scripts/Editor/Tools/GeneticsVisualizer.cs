using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace Laboratory.Editor.Tools
{
    /// <summary>
    /// Visualizes genetic diversity, trait distributions, and population health
    /// Helps designers understand how genetics evolve over time
    /// </summary>
    public class GeneticsVisualizer : EditorWindow
    {
        [MenuItem("Chimera/Genetics Visualizer")]
        public static void ShowWindow()
        {
            var window = GetWindow<GeneticsVisualizer>("Genetics Visualizer");
            window.minSize = new Vector2(800, 600);
            window.Show();
        }

        // Simulation data (would come from actual game data)
        private class CreatureData
        {
            public string name;
            public int generation;
            public float strength, vitality, agility, resilience;
            public float intelligence, charm, speed, endurance;
            public float aggression, curiosity, caution, dominance;
            public float sociability, adaptability;
        }

        private List<CreatureData> populationSample = new List<CreatureData>();
        private int sampleSize = 100;
        private Vector2 scrollPosition;

        private int selectedTrait = 0;
        private readonly string[] traitNames = new[]
        {
            "Strength", "Vitality", "Agility", "Resilience",
            "Intelligence", "Charm", "Speed", "Endurance",
            "Aggression", "Curiosity", "Caution", "Dominance",
            "Sociability", "Adaptability"
        };

        private bool showDistribution = true;
        private bool showCorrelations = false;
        private bool showGenerations = false;
        private bool showDiversity = true;

        private void OnEnable()
        {
            GenerateSamplePopulation();
        }

        private void OnGUI()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            DrawHeader();
            DrawControls();
            DrawDiversityMetrics();

            if (showDistribution) DrawTraitDistribution();
            if (showCorrelations) DrawTraitCorrelations();
            if (showGenerations) DrawGenerationAnalysis();

            DrawActionButtons();

            EditorGUILayout.EndScrollView();
        }

        private void DrawHeader()
        {
            EditorGUILayout.LabelField("Genetics Visualizer", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Analyze genetic diversity, trait distributions, and population health. " +
                $"Currently analyzing {populationSample.Count} creatures.",
                MessageType.Info);
            EditorGUILayout.Space();
        }

        private void DrawControls()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Sample Size:", GUILayout.Width(100));
            sampleSize = EditorGUILayout.IntSlider(sampleSize, 10, 1000);
            if (GUILayout.Button("Regenerate", GUILayout.Width(100)))
            {
                GenerateSamplePopulation();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Analyze Trait:", GUILayout.Width(100));
            selectedTrait = EditorGUILayout.Popup(selectedTrait, traitNames);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            EditorGUILayout.BeginHorizontal();
            showDistribution = EditorGUILayout.ToggleLeft("Show Distribution", showDistribution, GUILayout.Width(150));
            showCorrelations = EditorGUILayout.ToggleLeft("Show Correlations", showCorrelations, GUILayout.Width(150));
            showGenerations = EditorGUILayout.ToggleLeft("Show Generations", showGenerations, GUILayout.Width(150));
            showDiversity = EditorGUILayout.ToggleLeft("Show Diversity", showDiversity, GUILayout.Width(150));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();
        }

        private void DrawDiversityMetrics()
        {
            if (!showDiversity) return;

            EditorGUILayout.LabelField("Population Diversity", EditorStyles.boldLabel);

            float[] diversityScores = CalculateDiversityScores();
            float avgDiversity = diversityScores.Average();

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // Overall diversity score
            Color diversityColor = GetDiversityColor(avgDiversity);
            GUIStyle diversityStyle = new GUIStyle(EditorStyles.boldLabel);
            diversityStyle.normal.textColor = diversityColor;

            EditorGUILayout.LabelField($"Overall Diversity: {avgDiversity:F2} ({GetDiversityRating(avgDiversity)})",
                diversityStyle);

            EditorGUILayout.Space();

            // Individual trait diversity
            EditorGUILayout.LabelField("Trait Diversity Breakdown:", EditorStyles.miniBoldLabel);
            for (int i = 0; i < traitNames.Length; i++)
            {
                DrawDiversityBar(traitNames[i], diversityScores[i]);
            }

            EditorGUILayout.EndVertical();

            EditorGUILayout.Space();
        }

        private void DrawTraitDistribution()
        {
            EditorGUILayout.LabelField("Trait Distribution", EditorStyles.boldLabel);

            float[] values = GetTraitValues(selectedTrait);
            float min = values.Min();
            float max = values.Max();
            float avg = values.Average();
            float stdDev = CalculateStandardDeviation(values);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // Statistics
            EditorGUILayout.LabelField($"Trait: {traitNames[selectedTrait]}", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"Min: {min:F2} | Max: {max:F2} | Avg: {avg:F2} | StdDev: {stdDev:F2}");

            EditorGUILayout.Space();

            // Histogram
            DrawHistogram(values);

            EditorGUILayout.EndVertical();

            EditorGUILayout.Space();
        }

        private void DrawTraitCorrelations()
        {
            EditorGUILayout.LabelField("Trait Correlations", EditorStyles.boldLabel);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.HelpBox(
                "Correlation shows how traits tend to appear together. " +
                "High positive values mean traits appear together frequently.",
                MessageType.Info);

            // Find strongest correlations for selected trait
            var correlations = CalculateCorrelations(selectedTrait);

            EditorGUILayout.LabelField($"Correlations with {traitNames[selectedTrait]}:", EditorStyles.boldLabel);

            foreach (var corr in correlations.OrderByDescending(c => Mathf.Abs(c.Value)).Take(5))
            {
                DrawCorrelationBar(corr.Key, corr.Value);
            }

            EditorGUILayout.EndVertical();

            EditorGUILayout.Space();
        }

        private void DrawGenerationAnalysis()
        {
            EditorGUILayout.LabelField("Generation Analysis", EditorStyles.boldLabel);

            var generations = populationSample.GroupBy(c => c.generation).OrderBy(g => g.Key);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            foreach (var gen in generations)
            {
                float[] genValues = gen.Select(c => GetCreatureTraitValue(c, selectedTrait)).ToArray();
                float genAvg = genValues.Average();

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"Gen {gen.Key}:", GUILayout.Width(60));
                EditorGUILayout.LabelField($"Count: {gen.Count()}", GUILayout.Width(80));
                EditorGUILayout.LabelField($"Avg {traitNames[selectedTrait]}: {genAvg:F2}", GUILayout.Width(150));

                // Visual bar
                Rect barRect = GUILayoutUtility.GetRect(100, 18);
                EditorGUI.DrawRect(barRect, Color.gray * 0.3f);
                Rect fillRect = new Rect(barRect.x, barRect.y, barRect.width * genAvg, barRect.height);
                EditorGUI.DrawRect(fillRect, Color.cyan);

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndVertical();

            EditorGUILayout.Space();
        }

        private void DrawDiversityBar(string label, float score)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(label, GUILayout.Width(120));

            Rect barRect = GUILayoutUtility.GetRect(200, 18);
            EditorGUI.DrawRect(barRect, Color.gray * 0.3f);

            Rect fillRect = new Rect(barRect.x, barRect.y, barRect.width * score, barRect.height);
            Color barColor = GetDiversityColor(score);
            EditorGUI.DrawRect(fillRect, barColor);

            EditorGUILayout.LabelField($"{score:F2}", GUILayout.Width(40));

            EditorGUILayout.EndHorizontal();
        }

        private void DrawCorrelationBar(string trait, float correlation)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(trait, GUILayout.Width(120));

            Rect barRect = GUILayoutUtility.GetRect(200, 18);
            EditorGUI.DrawRect(barRect, Color.gray * 0.3f);

            float absCorr = Mathf.Abs(correlation);
            Rect fillRect = new Rect(barRect.x, barRect.y, barRect.width * absCorr, barRect.height);
            Color barColor = correlation > 0 ? Color.green : Color.red;
            EditorGUI.DrawRect(fillRect, barColor * 0.7f);

            EditorGUILayout.LabelField($"{correlation:F2}", GUILayout.Width(50));

            EditorGUILayout.EndHorizontal();
        }

        private void DrawHistogram(float[] values)
        {
            const int binCount = 10;
            int[] bins = new int[binCount];

            // Fill bins
            foreach (float value in values)
            {
                int binIndex = Mathf.Clamp(Mathf.FloorToInt(value * binCount), 0, binCount - 1);
                bins[binIndex]++;
            }

            int maxBin = bins.Max();

            // Draw histogram
            Rect histogramRect = GUILayoutUtility.GetRect(0, 150, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(histogramRect, Color.black * 0.2f);

            float binWidth = histogramRect.width / binCount;

            for (int i = 0; i < binCount; i++)
            {
                if (bins[i] == 0) continue;

                float binHeight = (bins[i] / (float)maxBin) * (histogramRect.height - 20);
                Rect binRect = new Rect(
                    histogramRect.x + i * binWidth + 2,
                    histogramRect.y + histogramRect.height - binHeight - 10,
                    binWidth - 4,
                    binHeight
                );

                EditorGUI.DrawRect(binRect, Color.cyan * 0.8f);

                // Draw count label
                GUIStyle labelStyle = new GUIStyle(EditorStyles.miniLabel);
                labelStyle.alignment = TextAnchor.MiddleCenter;
                Rect labelRect = new Rect(binRect.x, binRect.y - 15, binRect.width, 15);
                EditorGUI.LabelField(labelRect, bins[i].ToString(), labelStyle);
            }

            // Draw axis labels
            GUIStyle axisStyle = new GUIStyle(EditorStyles.miniLabel);
            EditorGUI.LabelField(new Rect(histogramRect.x, histogramRect.yMax - 10, 30, 10), "0.0", axisStyle);
            EditorGUI.LabelField(new Rect(histogramRect.xMax - 30, histogramRect.yMax - 10, 30, 10), "1.0", axisStyle);
        }

        private void DrawActionButtons()
        {
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Export Report", GUILayout.Height(30)))
            {
                ExportReport();
            }

            if (GUILayout.Button("Refresh Data", GUILayout.Height(30)))
            {
                GenerateSamplePopulation();
            }

            EditorGUILayout.EndHorizontal();
        }

        private void GenerateSamplePopulation()
        {
            populationSample.Clear();
            System.Random random = new System.Random();

            for (int i = 0; i < sampleSize; i++)
            {
                var creature = new CreatureData
                {
                    name = $"Creature_{i}",
                    generation = random.Next(1, 11),
                    strength = (float)random.NextDouble(),
                    vitality = (float)random.NextDouble(),
                    agility = (float)random.NextDouble(),
                    resilience = (float)random.NextDouble(),
                    intelligence = (float)random.NextDouble(),
                    charm = (float)random.NextDouble(),
                    speed = (float)random.NextDouble(),
                    endurance = (float)random.NextDouble(),
                    aggression = (float)random.NextDouble(),
                    curiosity = (float)random.NextDouble(),
                    caution = (float)random.NextDouble(),
                    dominance = (float)random.NextDouble(),
                    sociability = (float)random.NextDouble(),
                    adaptability = (float)random.NextDouble()
                };

                populationSample.Add(creature);
            }

            Repaint();
        }

        private float[] GetTraitValues(int traitIndex)
        {
            return populationSample.Select(c => GetCreatureTraitValue(c, traitIndex)).ToArray();
        }

        private float GetCreatureTraitValue(CreatureData creature, int traitIndex)
        {
            return traitIndex switch
            {
                0 => creature.strength,
                1 => creature.vitality,
                2 => creature.agility,
                3 => creature.resilience,
                4 => creature.intelligence,
                5 => creature.charm,
                6 => creature.speed,
                7 => creature.endurance,
                8 => creature.aggression,
                9 => creature.curiosity,
                10 => creature.caution,
                11 => creature.dominance,
                12 => creature.sociability,
                13 => creature.adaptability,
                _ => 0f
            };
        }

        private float[] CalculateDiversityScores()
        {
            float[] scores = new float[traitNames.Length];

            for (int i = 0; i < traitNames.Length; i++)
            {
                float[] values = GetTraitValues(i);
                float stdDev = CalculateStandardDeviation(values);
                // Normalize to 0-1 range (stdDev of ~0.3 is ideal diversity)
                scores[i] = Mathf.Clamp01(stdDev / 0.3f);
            }

            return scores;
        }

        private Dictionary<string, float> CalculateCorrelations(int traitIndex)
        {
            var correlations = new Dictionary<string, float>();
            float[] baseValues = GetTraitValues(traitIndex);

            for (int i = 0; i < traitNames.Length; i++)
            {
                if (i == traitIndex) continue;

                float[] compareValues = GetTraitValues(i);
                float correlation = CalculatePearsonCorrelation(baseValues, compareValues);
                correlations[traitNames[i]] = correlation;
            }

            return correlations;
        }

        private float CalculateStandardDeviation(float[] values)
        {
            float avg = values.Average();
            float sumSquaredDiff = values.Sum(v => (v - avg) * (v - avg));
            return Mathf.Sqrt(sumSquaredDiff / values.Length);
        }

        private float CalculatePearsonCorrelation(float[] x, float[] y)
        {
            float avgX = x.Average();
            float avgY = y.Average();

            float numerator = 0;
            float sumXSq = 0;
            float sumYSq = 0;

            for (int i = 0; i < x.Length; i++)
            {
                float xDiff = x[i] - avgX;
                float yDiff = y[i] - avgY;
                numerator += xDiff * yDiff;
                sumXSq += xDiff * xDiff;
                sumYSq += yDiff * yDiff;
            }

            if (sumXSq == 0 || sumYSq == 0) return 0;
            return numerator / Mathf.Sqrt(sumXSq * sumYSq);
        }

        private Color GetDiversityColor(float diversity)
        {
            if (diversity < 0.3f) return Color.red;
            if (diversity < 0.6f) return Color.yellow;
            return Color.green;
        }

        private string GetDiversityRating(float diversity)
        {
            if (diversity < 0.3f) return "Low - Risk of Inbreeding";
            if (diversity < 0.6f) return "Moderate";
            if (diversity < 0.8f) return "Good";
            return "Excellent";
        }

        private void ExportReport()
        {
            string path = EditorUtility.SaveFilePanel("Export Genetics Report", "", "genetics_report.txt", "txt");
            if (string.IsNullOrEmpty(path)) return;

            System.Text.StringBuilder report = new System.Text.StringBuilder();
            report.AppendLine("=== GENETICS POPULATION REPORT ===");
            report.AppendLine($"Generated: {System.DateTime.Now}");
            report.AppendLine($"Sample Size: {populationSample.Count}");
            report.AppendLine();

            float[] diversity = CalculateDiversityScores();
            report.AppendLine("DIVERSITY SCORES:");
            for (int i = 0; i < traitNames.Length; i++)
            {
                report.AppendLine($"{traitNames[i]}: {diversity[i]:F3}");
            }

            System.IO.File.WriteAllText(path, report.ToString());

            EditorUtility.DisplayDialog("Success", $"Report exported to:\n{path}", "OK");
        }
    }
}
