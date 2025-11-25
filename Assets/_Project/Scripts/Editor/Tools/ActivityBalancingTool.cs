using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace Laboratory.Editor.Tools
{
    /// <summary>
    /// Designer tool for balancing activity difficulty and rewards
    /// Provides visual feedback on completion times, XP rewards, and player engagement
    /// </summary>
    public class ActivityBalancingTool : EditorWindow
    {
        [MenuItem("Chimera/Activity Balancing Tool")]
        public static void ShowWindow()
        {
            var window = GetWindow<ActivityBalancingTool>("Activity Balancer");
            window.minSize = new Vector2(700, 600);
            window.Show();
        }

        // Activity selection
        private int selectedActivity = 0;
        private readonly string[] activityTypes = new[]
        {
            "Racing", "Combat", "Puzzle", "Strategy",
            "Music", "Adventure", "Platforming", "Crafting"
        };

        // Difficulty settings
        private int difficultyLevel = 1;
        private float baseDifficulty = 1.0f;
        private float difficultyScaling = 0.2f;

        // Reward settings
        private int baseXPReward = 100;
        private int baseGoldReward = 50;
        private float rewardScaling = 1.5f;

        // Performance targets
        private float targetCompletionTime = 60f; // seconds
        private float averagePlayerSkill = 0.5f;

        // Simulation results
        private float estimatedCompletionTime;
        private float estimatedXP;
        private float estimatedGold;
        private float engagementScore;

        // Genetic weights for each activity
        private Dictionary<string, float[]> activityWeights = new Dictionary<string, float[]>
        {
            { "Racing", new float[] { 0.4f, 0.3f, 0.3f } },      // Speed, Stamina, Agility
            { "Combat", new float[] { 0.5f, 0.3f, 0.2f } },      // Aggression, Size, Dominance
            { "Puzzle", new float[] { 0.7f, 0.3f, 0.0f } },      // Intelligence, Curiosity
            { "Strategy", new float[] { 0.6f, 0.4f, 0.0f } },    // Intelligence, Caution
            { "Music", new float[] { 0.4f, 0.6f, 0.0f } },       // Intelligence, Sociability
            { "Adventure", new float[] { 0.4f, 0.3f, 0.3f } },   // Curiosity, Adaptability, Stamina
            { "Platforming", new float[] { 0.6f, 0.4f, 0.0f } }, // Agility, Intelligence
            { "Crafting", new float[] { 0.5f, 0.5f, 0.0f } }     // Intelligence, Adaptability
        };

        private Vector2 scrollPosition;
        private bool showSimulation = true;

        private void OnGUI()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            DrawHeader();
            DrawActivitySelector();
            DrawDifficultySettings();
            DrawRewardSettings();
            DrawTargetPerformance();

            EditorGUILayout.Space(10);

            DrawSimulationResults();
            DrawRecommendations();
            DrawActionButtons();

            EditorGUILayout.EndScrollView();
        }

        private void DrawHeader()
        {
            EditorGUILayout.LabelField("Activity Balancing Tool", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Balance activity difficulty, rewards, and player engagement. " +
                "Adjust parameters and see real-time simulation of expected outcomes.",
                MessageType.Info);
            EditorGUILayout.Space();
        }

        private void DrawActivitySelector()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Activity Type:", GUILayout.Width(100));
            selectedActivity = EditorGUILayout.Popup(selectedActivity, activityTypes);
            EditorGUILayout.EndHorizontal();

            // Show genetic requirements
            string selectedName = activityTypes[selectedActivity];
            float[] weights = activityWeights[selectedName];
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Key Genetics:", EditorStyles.miniBoldLabel, GUILayout.Width(100));
            EditorGUILayout.LabelField(GetGeneticRequirements(selectedName), EditorStyles.miniLabel);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();
        }

        private void DrawDifficultySettings()
        {
            EditorGUILayout.LabelField("Difficulty Settings", EditorStyles.boldLabel);

            difficultyLevel = EditorGUILayout.IntSlider("Difficulty Level", difficultyLevel, 1, 10);

            baseDifficulty = EditorGUILayout.Slider(
                new GUIContent("Base Difficulty", "Baseline difficulty multiplier"),
                baseDifficulty, 0.5f, 3.0f);

            difficultyScaling = EditorGUILayout.Slider(
                new GUIContent("Level Scaling", "How much difficulty increases per level"),
                difficultyScaling, 0.1f, 0.5f);

            float actualDifficulty = baseDifficulty * (1 + difficultyScaling * (difficultyLevel - 1));

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField($"Actual Difficulty: {actualDifficulty:F2}x", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"Category: {GetDifficultyCategory(actualDifficulty)}");
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space();
        }

        private void DrawRewardSettings()
        {
            EditorGUILayout.LabelField("Reward Settings", EditorStyles.boldLabel);

            baseXPReward = EditorGUILayout.IntSlider("Base XP Reward", baseXPReward, 10, 500);
            baseGoldReward = EditorGUILayout.IntSlider("Base Gold Reward", baseGoldReward, 5, 250);

            rewardScaling = EditorGUILayout.Slider(
                new GUIContent("Reward Scaling", "Reward multiplier per difficulty level"),
                rewardScaling, 1.1f, 2.0f);

            float actualXP = baseXPReward * Mathf.Pow(rewardScaling, difficultyLevel - 1);
            float actualGold = baseGoldReward * Mathf.Pow(rewardScaling, difficultyLevel - 1);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField($"Level {difficultyLevel} Rewards:", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"XP: {actualXP:F0} | Gold: {actualGold:F0}");
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space();
        }

        private void DrawTargetPerformance()
        {
            EditorGUILayout.LabelField("Performance Targets", EditorStyles.boldLabel);

            targetCompletionTime = EditorGUILayout.Slider(
                new GUIContent("Target Time (seconds)", "Expected time for average player"),
                targetCompletionTime, 10f, 300f);

            averagePlayerSkill = EditorGUILayout.Slider(
                new GUIContent("Average Player Skill", "0.0 = Beginner, 1.0 = Expert"),
                averagePlayerSkill, 0.0f, 1.0f);

            EditorGUILayout.Space();
        }

        private void DrawSimulationResults()
        {
            showSimulation = EditorGUILayout.Foldout(showSimulation, "Simulation Results", true);
            if (!showSimulation) return;

            // Run simulation
            SimulateActivity();

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // Completion time
            EditorGUILayout.LabelField("Expected Outcomes", EditorStyles.boldLabel);
            DrawMetric("Completion Time", estimatedCompletionTime, targetCompletionTime, "seconds");
            DrawMetric("XP Earned", estimatedXP, baseXPReward * Mathf.Pow(rewardScaling, difficultyLevel - 1), "XP");
            DrawMetric("Gold Earned", estimatedGold, baseGoldReward * Mathf.Pow(rewardScaling, difficultyLevel - 1), "gold");

            EditorGUILayout.Space();

            // Engagement score
            EditorGUILayout.LabelField($"Engagement Score: {engagementScore:F1}/10", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(GetEngagementFeedback(engagementScore), MessageType.None);

            EditorGUILayout.EndVertical();

            EditorGUILayout.Space();
        }

        private void DrawRecommendations()
        {
            EditorGUILayout.LabelField("Balancing Recommendations", EditorStyles.boldLabel);

            List<string> recommendations = GenerateRecommendations();

            if (recommendations.Count == 0)
            {
                EditorGUILayout.HelpBox("✅ Activity appears well-balanced!", MessageType.Info);
            }
            else
            {
                foreach (var rec in recommendations)
                {
                    EditorGUILayout.HelpBox(rec, MessageType.Warning);
                }
            }

            EditorGUILayout.Space();
        }

        private void DrawActionButtons()
        {
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Apply to Config", GUILayout.Height(30)))
            {
                ApplyToConfig();
            }

            if (GUILayout.Button("Run Full Test", GUILayout.Height(30)))
            {
                RunFullTest();
            }

            if (GUILayout.Button("Reset to Defaults", GUILayout.Height(30)))
            {
                ResetToDefaults();
            }

            EditorGUILayout.EndHorizontal();
        }

        private void SimulateActivity()
        {
            float difficulty = baseDifficulty * (1 + difficultyScaling * (difficultyLevel - 1));

            // Estimate completion time based on difficulty and player skill
            float skillModifier = Mathf.Lerp(0.5f, 1.5f, averagePlayerSkill);
            estimatedCompletionTime = targetCompletionTime * difficulty / skillModifier;

            // Calculate rewards
            float performanceBonus = Mathf.Clamp01(targetCompletionTime / estimatedCompletionTime);
            estimatedXP = baseXPReward * Mathf.Pow(rewardScaling, difficultyLevel - 1) * performanceBonus;
            estimatedGold = baseGoldReward * Mathf.Pow(rewardScaling, difficultyLevel - 1) * performanceBonus;

            // Calculate engagement score (0-10)
            float timeBalance = 1.0f - Mathf.Abs(estimatedCompletionTime - targetCompletionTime) / targetCompletionTime;
            float rewardBalance = estimatedXP / (estimatedCompletionTime / 60f); // XP per minute
            float difficultyBalance = 1.0f - Mathf.Abs(difficulty - 1.5f) / 2.0f; // Sweet spot around 1.5x

            engagementScore = (timeBalance * 4f + Mathf.Clamp01(rewardBalance / 100f) * 3f + difficultyBalance * 3f);
            engagementScore = Mathf.Clamp(engagementScore, 0f, 10f);
        }

        private void DrawMetric(string label, float actual, float target, string unit)
        {
            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.LabelField($"{label}:", GUILayout.Width(120));
            EditorGUILayout.LabelField($"{actual:F1} {unit}", GUILayout.Width(100));

            float variance = Mathf.Abs(actual - target) / target;
            Color color = variance < 0.1f ? Color.green : variance < 0.3f ? Color.yellow : Color.red;

            GUIStyle statusStyle = new GUIStyle(EditorStyles.label);
            statusStyle.normal.textColor = color;
            string status = variance < 0.1f ? "✓ On Target" : variance < 0.3f ? "⚠ Close" : "✗ Off Target";
            EditorGUILayout.LabelField(status, statusStyle);

            EditorGUILayout.EndHorizontal();
        }

        private List<string> GenerateRecommendations()
        {
            var recommendations = new List<string>();

            float difficulty = baseDifficulty * (1 + difficultyScaling * (difficultyLevel - 1));

            // Check completion time
            float timeVariance = Mathf.Abs(estimatedCompletionTime - targetCompletionTime) / targetCompletionTime;
            if (timeVariance > 0.3f)
            {
                if (estimatedCompletionTime > targetCompletionTime * 1.3f)
                    recommendations.Add("⚠ Activity takes too long. Consider reducing difficulty or increasing player power.");
                else
                    recommendations.Add("⚠ Activity completes too quickly. Consider increasing difficulty.");
            }

            // Check engagement
            if (engagementScore < 5.0f)
            {
                recommendations.Add("⚠ Low engagement score. Review difficulty curve and reward ratios.");
            }

            // Check reward balance
            float xpPerMinute = estimatedXP / (estimatedCompletionTime / 60f);
            if (xpPerMinute < 50f)
                recommendations.Add("⚠ XP rewards may be too low for time investment.");
            if (xpPerMinute > 200f)
                recommendations.Add("⚠ XP rewards may be too high. Risk of exploitation.");

            // Check difficulty scaling
            if (difficultyLevel > 5 && difficulty < 1.5f)
                recommendations.Add("⚠ Difficulty scaling seems too gentle for high levels.");

            return recommendations;
        }

        private string GetGeneticRequirements(string activity)
        {
            return activity switch
            {
                "Racing" => "Speed (40%), Stamina (30%), Agility (30%)",
                "Combat" => "Aggression (50%), Size (30%), Dominance (20%)",
                "Puzzle" => "Intelligence (70%), Curiosity (30%)",
                "Strategy" => "Intelligence (60%), Caution (40%)",
                "Music" => "Intelligence (40%), Sociability (60%)",
                "Adventure" => "Curiosity (40%), Adaptability (30%), Stamina (30%)",
                "Platforming" => "Agility (60%), Intelligence (40%)",
                "Crafting" => "Intelligence (50%), Adaptability (50%)",
                _ => "Unknown"
            };
        }

        private string GetDifficultyCategory(float difficulty)
        {
            if (difficulty < 1.0f) return "Easy";
            if (difficulty < 1.5f) return "Normal";
            if (difficulty < 2.0f) return "Hard";
            if (difficulty < 2.5f) return "Very Hard";
            return "Extreme";
        }

        private string GetEngagementFeedback(float score)
        {
            if (score >= 8.0f) return "Excellent! This activity is well-balanced and engaging.";
            if (score >= 6.0f) return "Good balance. Minor tweaks may improve engagement.";
            if (score >= 4.0f) return "Moderate engagement. Consider adjusting difficulty or rewards.";
            return "Low engagement. Significant balancing needed.";
        }

        private void ApplyToConfig()
        {
            EditorUtility.DisplayDialog("Apply Settings",
                "This would apply current settings to the activity ScriptableObject config.\n\n" +
                $"Activity: {activityTypes[selectedActivity]}\n" +
                $"Difficulty: {baseDifficulty * (1 + difficultyScaling * (difficultyLevel - 1)):F2}x\n" +
                $"XP: {estimatedXP:F0} | Gold: {estimatedGold:F0}",
                "OK");
        }

        private void RunFullTest()
        {
            EditorUtility.DisplayDialog("Full Test",
                "This would run a full simulation with multiple player skill levels " +
                "and generate a comprehensive balance report.",
                "OK");
        }

        private void ResetToDefaults()
        {
            difficultyLevel = 1;
            baseDifficulty = 1.0f;
            difficultyScaling = 0.2f;
            baseXPReward = 100;
            baseGoldReward = 50;
            rewardScaling = 1.5f;
            targetCompletionTime = 60f;
            averagePlayerSkill = 0.5f;
            Repaint();
        }
    }
}
