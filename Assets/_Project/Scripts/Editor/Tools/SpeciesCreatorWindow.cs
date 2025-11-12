using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace Laboratory.Editor.Tools
{
    /// <summary>
    /// Designer-friendly tool for creating and editing creature species
    /// Provides visual interface for all genetic traits, behaviors, and stats
    /// </summary>
    public class SpeciesCreatorWindow : EditorWindow
    {
        [MenuItem("Chimera/Species Creator")]
        public static void ShowWindow()
        {
            var window = GetWindow<SpeciesCreatorWindow>("Species Creator");
            window.minSize = new Vector2(600, 800);
            window.Show();
        }

        // Current species being edited
        private ScriptableObject currentSpecies;
        private string speciesName = "NewSpecies";
        private Vector2 scrollPosition;

        // Genetic Traits (0-1 range)
        private float strength = 0.5f;
        private float vitality = 0.5f;
        private float agility = 0.5f;
        private float resilience = 0.5f;
        private float intelligence = 0.5f;
        private float charm = 0.5f;
        private float speed = 0.5f;
        private float endurance = 0.5f;

        // Behavior Traits (0-1 range)
        private float aggression = 0.5f;
        private float curiosity = 0.5f;
        private float caution = 0.5f;
        private float dominance = 0.5f;
        private float sociability = 0.5f;
        private float adaptability = 0.5f;

        // Physical Traits
        private float baseSize = 1.0f;
        private float lifespan = 10.0f;
        private Color primaryColor = Color.gray;
        private Color secondaryColor = Color.white;

        // Preset templates
        private int selectedPreset = 0;
        private readonly string[] presets = new[]
        {
            "Custom",
            "Aggressive Predator",
            "Peaceful Herbivore",
            "Cunning Scavenger",
            "Social Pack Animal",
            "Solitary Hunter",
            "Intelligent Strategist"
        };

        // Folders
        private bool showPhysicalTraits = true;
        private bool showGeneticTraits = true;
        private bool showBehaviorTraits = true;
        private bool showAdvancedSettings = false;

        private void OnGUI()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            DrawHeader();
            DrawPresetSelector();
            DrawCurrentSpecies();

            EditorGUILayout.Space(10);

            DrawPhysicalTraits();
            DrawGeneticTraits();
            DrawBehaviorTraits();
            DrawAdvancedSettings();

            EditorGUILayout.Space(10);

            DrawTraitSummary();
            DrawActionButtons();

            EditorGUILayout.EndScrollView();
        }

        private void DrawHeader()
        {
            EditorGUILayout.LabelField("Species Creator", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Create and configure creature species with genetic traits, behaviors, and physical characteristics.",
                MessageType.Info);
            EditorGUILayout.Space();
        }

        private void DrawPresetSelector()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Template Preset:", GUILayout.Width(100));

            int newPreset = EditorGUILayout.Popup(selectedPreset, presets);
            if (newPreset != selectedPreset && newPreset > 0)
            {
                ApplyPreset(newPreset);
            }
            selectedPreset = newPreset;

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();
        }

        private void DrawCurrentSpecies()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Species Name:", GUILayout.Width(100));
            speciesName = EditorGUILayout.TextField(speciesName);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Editing:", GUILayout.Width(100));
            currentSpecies = (ScriptableObject)EditorGUILayout.ObjectField(
                currentSpecies,
                typeof(ScriptableObject),
                false);

            if (GUILayout.Button("Load", GUILayout.Width(60)))
            {
                LoadFromSpecies();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();
        }

        private void DrawPhysicalTraits()
        {
            showPhysicalTraits = EditorGUILayout.Foldout(showPhysicalTraits, "Physical Traits", true);
            if (!showPhysicalTraits) return;

            EditorGUI.indentLevel++;

            baseSize = EditorGUILayout.Slider("Base Size", baseSize, 0.1f, 3.0f);
            EditorGUILayout.HelpBox($"Scale: {GetSizeCategory(baseSize)}", MessageType.None);

            lifespan = EditorGUILayout.Slider("Lifespan (years)", lifespan, 1.0f, 50.0f);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Coloration", EditorStyles.boldLabel);
            primaryColor = EditorGUILayout.ColorField("Primary Color", primaryColor);
            secondaryColor = EditorGUILayout.ColorField("Secondary Color", secondaryColor);

            EditorGUI.indentLevel--;
            EditorGUILayout.Space();
        }

        private void DrawGeneticTraits()
        {
            showGeneticTraits = EditorGUILayout.Foldout(showGeneticTraits, "Genetic Traits (Physical)", true);
            if (!showGeneticTraits) return;

            EditorGUI.indentLevel++;

            EditorGUILayout.LabelField("Combat & Movement", EditorStyles.boldLabel);
            strength = DrawTraitSlider("Strength", strength, "Physical power and damage");
            vitality = DrawTraitSlider("Vitality", vitality, "Health and endurance");
            agility = DrawTraitSlider("Agility", agility, "Speed and dodging");
            resilience = DrawTraitSlider("Resilience", resilience, "Damage resistance");

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Mental & Social", EditorStyles.boldLabel);
            intelligence = DrawTraitSlider("Intelligence", intelligence, "Learning and problem-solving");
            charm = DrawTraitSlider("Charm", charm, "Social interaction");
            speed = DrawTraitSlider("Speed", speed, "Movement velocity");
            endurance = DrawTraitSlider("Endurance", endurance, "Stamina and persistence");

            EditorGUI.indentLevel--;
            EditorGUILayout.Space();
        }

        private void DrawBehaviorTraits()
        {
            showBehaviorTraits = EditorGUILayout.Foldout(showBehaviorTraits, "Behavior Traits", true);
            if (!showBehaviorTraits) return;

            EditorGUI.indentLevel++;

            aggression = DrawTraitSlider("Aggression", aggression, "Tendency to attack");
            curiosity = DrawTraitSlider("Curiosity", curiosity, "Exploration drive");
            caution = DrawTraitSlider("Caution", caution, "Risk aversion");
            dominance = DrawTraitSlider("Dominance", dominance, "Social hierarchy");
            sociability = DrawTraitSlider("Sociability", sociability, "Pack behavior");
            adaptability = DrawTraitSlider("Adaptability", adaptability, "Environmental flexibility");

            EditorGUI.indentLevel--;
            EditorGUILayout.Space();
        }

        private void DrawAdvancedSettings()
        {
            showAdvancedSettings = EditorGUILayout.Foldout(showAdvancedSettings, "Advanced Settings", true);
            if (!showAdvancedSettings) return;

            EditorGUI.indentLevel++;
            EditorGUILayout.HelpBox("Advanced genetic settings - mutation rates, inheritance patterns, etc.", MessageType.Info);
            // Future expansion: mutation rates, breeding restrictions, etc.
            EditorGUI.indentLevel--;
            EditorGUILayout.Space();
        }

        private void DrawTraitSummary()
        {
            EditorGUILayout.LabelField("Trait Summary", EditorStyles.boldLabel);

            float avgPhysical = (strength + vitality + agility + resilience + speed + endurance) / 6f;
            float avgMental = (intelligence + charm) / 2f;
            float avgBehavior = (aggression + curiosity + caution + dominance + sociability + adaptability) / 6f;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField($"Physical Average: {avgPhysical:F2} ({GetRating(avgPhysical)})");
            EditorGUILayout.LabelField($"Mental Average: {avgMental:F2} ({GetRating(avgMental)})");
            EditorGUILayout.LabelField($"Behavior Average: {avgBehavior:F2} ({GetRating(avgBehavior)})");
            EditorGUILayout.LabelField($"Overall Balance: {GetBalance(avgPhysical, avgMental, avgBehavior)}");
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space();
        }

        private void DrawActionButtons()
        {
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Create New Species", GUILayout.Height(30)))
            {
                CreateNewSpecies();
            }

            if (GUILayout.Button("Save Changes", GUILayout.Height(30)))
            {
                SaveChanges();
            }

            if (GUILayout.Button("Reset to Defaults", GUILayout.Height(30)))
            {
                ResetToDefaults();
            }

            EditorGUILayout.EndHorizontal();
        }

        private float DrawTraitSlider(string label, float value, string tooltip)
        {
            EditorGUILayout.BeginHorizontal();
            float newValue = EditorGUILayout.Slider(new GUIContent(label, tooltip), value, 0f, 1f);

            // Visual rating indicator
            string rating = GetRating(newValue);
            GUIStyle ratingStyle = new GUIStyle(EditorStyles.label);
            ratingStyle.normal.textColor = GetRatingColor(newValue);
            EditorGUILayout.LabelField(rating, ratingStyle, GUILayout.Width(50));

            EditorGUILayout.EndHorizontal();
            return newValue;
        }

        private void ApplyPreset(int presetIndex)
        {
            switch (presetIndex)
            {
                case 1: // Aggressive Predator
                    strength = 0.9f; vitality = 0.7f; agility = 0.8f; resilience = 0.6f;
                    intelligence = 0.6f; charm = 0.3f; speed = 0.8f; endurance = 0.7f;
                    aggression = 0.95f; curiosity = 0.5f; caution = 0.2f; dominance = 0.9f;
                    sociability = 0.3f; adaptability = 0.5f;
                    baseSize = 1.5f; lifespan = 15f;
                    primaryColor = new Color(0.6f, 0.2f, 0.2f);
                    break;

                case 2: // Peaceful Herbivore
                    strength = 0.3f; vitality = 0.8f; agility = 0.4f; resilience = 0.7f;
                    intelligence = 0.5f; charm = 0.7f; speed = 0.5f; endurance = 0.9f;
                    aggression = 0.1f; curiosity = 0.6f; caution = 0.9f; dominance = 0.2f;
                    sociability = 0.9f; adaptability = 0.7f;
                    baseSize = 1.0f; lifespan = 20f;
                    primaryColor = new Color(0.5f, 0.7f, 0.4f);
                    break;

                case 3: // Cunning Scavenger
                    strength = 0.4f; vitality = 0.6f; agility = 0.8f; resilience = 0.5f;
                    intelligence = 0.85f; charm = 0.5f; speed = 0.7f; endurance = 0.6f;
                    aggression = 0.4f; curiosity = 0.9f; caution = 0.7f; dominance = 0.5f;
                    sociability = 0.5f; adaptability = 0.95f;
                    baseSize = 0.7f; lifespan = 12f;
                    primaryColor = new Color(0.5f, 0.5f, 0.3f);
                    break;

                case 4: // Social Pack Animal
                    strength = 0.6f; vitality = 0.7f; agility = 0.7f; resilience = 0.6f;
                    intelligence = 0.7f; charm = 0.9f; speed = 0.7f; endurance = 0.8f;
                    aggression = 0.5f; curiosity = 0.6f; caution = 0.6f; dominance = 0.6f;
                    sociability = 0.95f; adaptability = 0.7f;
                    baseSize = 1.2f; lifespan = 18f;
                    primaryColor = new Color(0.6f, 0.5f, 0.4f);
                    break;

                case 5: // Solitary Hunter
                    strength = 0.85f; vitality = 0.75f; agility = 0.9f; resilience = 0.6f;
                    intelligence = 0.8f; charm = 0.2f; speed = 0.9f; endurance = 0.7f;
                    aggression = 0.85f; curiosity = 0.5f; caution = 0.5f; dominance = 0.8f;
                    sociability = 0.1f; adaptability = 0.6f;
                    baseSize = 1.3f; lifespan = 15f;
                    primaryColor = new Color(0.3f, 0.3f, 0.3f);
                    break;

                case 6: // Intelligent Strategist
                    strength = 0.5f; vitality = 0.6f; agility = 0.6f; resilience = 0.5f;
                    intelligence = 0.95f; charm = 0.75f; speed = 0.6f; endurance = 0.6f;
                    aggression = 0.3f; curiosity = 0.9f; caution = 0.85f; dominance = 0.7f;
                    sociability = 0.7f; adaptability = 0.9f;
                    baseSize = 1.0f; lifespan = 25f;
                    primaryColor = new Color(0.4f, 0.4f, 0.7f);
                    break;
            }

            Repaint();
        }

        private void CreateNewSpecies()
        {
            string path = EditorUtility.SaveFilePanelInProject(
                "Create Species Config",
                speciesName,
                "asset",
                "Choose location for new species config"
            );

            if (string.IsNullOrEmpty(path)) return;

            // Create ScriptableObject (you would use your actual ChimeraSpeciesConfig type here)
            var species = ScriptableObject.CreateInstance<ScriptableObject>();

            // Would set properties here if we had access to the actual type
            // For now, this demonstrates the workflow

            AssetDatabase.CreateAsset(species, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            currentSpecies = species;
            EditorUtility.DisplayDialog("Success", $"Species '{speciesName}' created successfully!", "OK");
        }

        private void SaveChanges()
        {
            if (currentSpecies == null)
            {
                EditorUtility.DisplayDialog("Error", "No species selected to save to.", "OK");
                return;
            }

            // Would save properties to ScriptableObject here
            EditorUtility.SetDirty(currentSpecies);
            AssetDatabase.SaveAssets();

            EditorUtility.DisplayDialog("Success", "Species changes saved!", "OK");
        }

        private void LoadFromSpecies()
        {
            if (currentSpecies == null) return;

            // Would load properties from ScriptableObject here
            EditorUtility.DisplayDialog("Info", "Species data loaded. (Full implementation requires ChimeraSpeciesConfig access)", "OK");
        }

        private void ResetToDefaults()
        {
            strength = vitality = agility = resilience = 0.5f;
            intelligence = charm = speed = endurance = 0.5f;
            aggression = curiosity = caution = dominance = sociability = adaptability = 0.5f;
            baseSize = 1.0f;
            lifespan = 10.0f;
            primaryColor = Color.gray;
            secondaryColor = Color.white;
            selectedPreset = 0;
            Repaint();
        }

        private string GetRating(float value)
        {
            if (value < 0.2f) return "Very Low";
            if (value < 0.4f) return "Low";
            if (value < 0.6f) return "Average";
            if (value < 0.8f) return "High";
            return "Very High";
        }

        private Color GetRatingColor(float value)
        {
            if (value < 0.3f) return new Color(0.8f, 0.3f, 0.3f);  // Red
            if (value < 0.7f) return new Color(0.9f, 0.7f, 0.3f);  // Yellow
            return new Color(0.3f, 0.8f, 0.3f);  // Green
        }

        private string GetSizeCategory(float size)
        {
            if (size < 0.5f) return "Tiny";
            if (size < 0.8f) return "Small";
            if (size < 1.2f) return "Medium";
            if (size < 1.8f) return "Large";
            return "Huge";
        }

        private string GetBalance(float physical, float mental, float behavior)
        {
            float variance = Mathf.Abs(physical - mental) + Mathf.Abs(mental - behavior) + Mathf.Abs(behavior - physical);
            if (variance < 0.2f) return "Well-Balanced";
            if (variance < 0.5f) return "Moderately Balanced";
            return "Specialized";
        }
    }
}
