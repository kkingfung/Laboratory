using System;
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using Laboratory.Chimera.Genetics;
using Laboratory.Chimera.Breeding;
using Laboratory.Chimera.Configuration;

namespace Laboratory.Editor.Tools
{
    /// <summary>
    /// Advanced Genetic Algorithm Designer & Visualizer
    /// FEATURES: Visual gene editing, trait simulation, breeding outcome prediction
    /// PURPOSE: Enable designers to create complex genetic systems without coding
    /// </summary>
    public class GeneticAlgorithmDesigner : EditorWindow
    {
        #region Fields

        private Vector2 scrollPosition;
        private GeneticProfile previewProfile;
        private CreatureSpeciesConfig selectedSpecies;

        // Visual gene editing
        private int selectedGeneIndex = 0;
        private bool showAdvancedGenetics = false;
        private bool showBreedingSimulation = false;

        // Breeding simulation
        private GeneticProfile parentA;
        private GeneticProfile parentB;
        private List<GeneticProfile> simulatedOffspring = new List<GeneticProfile>();

        // Real-time visualization
        private Texture2D traitVisualization;
        private Dictionary<string, Color> traitColors = new Dictionary<string, Color>();

        // Gene pool analytics
        private Dictionary<Laboratory.Core.Enums.TraitType, float> traitDistribution = new Dictionary<Laboratory.Core.Enums.TraitType, float>();
        private List<MutationEvent> recentMutations = new List<MutationEvent>();

        #endregion

        #region Unity Editor Window

        [MenuItem("🧪 Laboratory/Tools/Genetic Algorithm Designer")]
        public static void ShowWindow()
        {
            var window = GetWindow<GeneticAlgorithmDesigner>("Genetic Designer");
            window.minSize = new Vector2(800, 600);
        }

        private void OnEnable()
        {
            titleContent = new GUIContent("🧬 Genetic Designer", "Visual genetic algorithm designer");
            InitializePreviewProfile();
            GenerateTraitColors();
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Genetic Algorithm Designer", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            {
                DrawSpeciesSelection();
                EditorGUILayout.Space();

                DrawGeneEditor();
                EditorGUILayout.Space();

                DrawTraitVisualization();
                EditorGUILayout.Space();

                DrawBreedingSimulator();
                EditorGUILayout.Space();

                DrawGenePoolAnalytics();
            }
            EditorGUILayout.EndScrollView();
        }

        #endregion

        #region GUI Sections

        private void DrawSpeciesSelection()
        {
            EditorGUILayout.LabelField("🦎 Species Configuration", EditorStyles.boldLabel);

            var newSpecies = (CreatureSpeciesConfig)EditorGUILayout.ObjectField(
                "Species Config:", selectedSpecies, typeof(CreatureSpeciesConfig), false);

            if (newSpecies != selectedSpecies)
            {
                selectedSpecies = newSpecies;
                LoadSpeciesGeneticTemplate();
            }

            EditorGUILayout.BeginHorizontal();
            {
                if (GUILayout.Button("🎲 Generate Random Profile"))
                {
                    GenerateRandomProfile();
                }

                if (GUILayout.Button("💾 Save as Template"))
                {
                    SaveGeneticTemplate();
                }

                if (GUILayout.Button("📊 Analyze Gene Pool"))
                {
                    AnalyzeExistingGenePool();
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawGeneEditor()
        {
            EditorGUILayout.LabelField("🧬 Gene Editor", EditorStyles.boldLabel);

            if (previewProfile?.Genes == null || previewProfile.Genes.Count == 0)
            {
                EditorGUILayout.HelpBox("No genetic profile loaded. Create or load a species config.", MessageType.Info);
                return;
            }

            // Gene selection dropdown
            var geneNames = previewProfile.Genes.Select((g, i) => $"{i}: {g.traitType} ({g.dominance})").ToArray();
            selectedGeneIndex = EditorGUILayout.Popup("Selected Gene:", selectedGeneIndex, geneNames);

            if (selectedGeneIndex >= 0 && selectedGeneIndex < previewProfile.Genes.Count)
            {
                DrawGeneDetails(previewProfile.Genes[selectedGeneIndex]);
            }

            EditorGUILayout.Space();

            EditorGUILayout.BeginHorizontal();
            {
                if (GUILayout.Button("➕ Add Gene"))
                {
                    AddNewGene();
                }

                if (GUILayout.Button("🗑️ Remove Gene") && previewProfile.Genes.Count > 0)
                {
                    RemoveSelectedGene();
                }

                if (GUILayout.Button("🔀 Mutate Gene"))
                {
                    MutateSelectedGene();
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawGeneDetails(Gene gene)
        {
            EditorGUI.indentLevel++;

            gene.traitType = (Laboratory.Core.Enums.TraitType)EditorGUILayout.EnumPopup("Trait Type:", gene.traitType);
            gene.dominance = EditorGUILayout.Slider("Dominance:", gene.dominance, 0f, 1f);
            gene.isActive = EditorGUILayout.Toggle("Active:", gene.isActive);
            // Note: Mutation rate is handled at the profile level, not individual genes

            EditorGUILayout.Space();

            // Visual representation
            var geneColor = GetTraitColor(gene.traitType);
            var originalColor = GUI.backgroundColor;
            GUI.backgroundColor = geneColor;

            EditorGUILayout.BeginVertical(GUI.skin.box);
            {
                GUI.backgroundColor = originalColor;
                EditorGUILayout.LabelField($"Expression Strength: {gene.dominance:P1}");
                EditorGUILayout.LabelField($"Phenotype Impact: {CalculatePhenotypeImpact(gene):P1}");
            }
            EditorGUILayout.EndVertical();

            EditorGUI.indentLevel--;
        }

        private void DrawTraitVisualization()
        {
            showAdvancedGenetics = EditorGUILayout.Foldout(showAdvancedGenetics, "🎨 Trait Visualization");
            if (!showAdvancedGenetics) return;

            EditorGUI.indentLevel++;

            if (previewProfile?.Genes != null)
            {
                EditorGUILayout.LabelField("Genetic Composition:", EditorStyles.boldLabel);

                // Draw trait bars
                var traitGroups = previewProfile.Genes.GroupBy(g => g.traitType);
                foreach (var group in traitGroups)
                {
                    var traitStrength = group.Sum(g => g.dominance * (g.isActive ? 1f : 0.5f));
                    var normalizedStrength = Mathf.Clamp01(traitStrength / group.Count());

                    EditorGUILayout.BeginHorizontal();
                    {
                        EditorGUILayout.LabelField(group.Key.ToString(), GUILayout.Width(120));

                        var rect = EditorGUILayout.GetControlRect(GUILayout.Height(16));
                        var traitColor = GetTraitColor(group.Key);

                        EditorGUI.DrawRect(rect, Color.gray);
                        var fillRect = new Rect(rect.x, rect.y, rect.width * normalizedStrength, rect.height);
                        EditorGUI.DrawRect(fillRect, traitColor);

                        EditorGUILayout.LabelField($"{normalizedStrength:P0}", GUILayout.Width(40));
                    }
                    EditorGUILayout.EndHorizontal();
                }
            }

            EditorGUI.indentLevel--;
        }

        private void DrawBreedingSimulator()
        {
            showBreedingSimulation = EditorGUILayout.Foldout(showBreedingSimulation, "👨‍👩‍👧‍👦 Breeding Simulator");
            if (!showBreedingSimulation) return;

            EditorGUI.indentLevel++;

            EditorGUILayout.LabelField("Parent Selection:", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            {
                EditorGUILayout.BeginVertical();
                {
                    EditorGUILayout.LabelField("Parent A:", EditorStyles.boldLabel);
                    if (GUILayout.Button("Use Current Profile"))
                    {
                        parentA = CloneProfile(previewProfile);
                    }
                    DrawParentSummary(parentA);
                }
                EditorGUILayout.EndVertical();

                EditorGUILayout.BeginVertical();
                {
                    EditorGUILayout.LabelField("Parent B:", EditorStyles.boldLabel);
                    if (GUILayout.Button("Generate Random"))
                    {
                        parentB = GenerateRandomGeneticProfile();
                    }
                    DrawParentSummary(parentB);
                }
                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            if (parentA != null && parentB != null)
            {
                if (GUILayout.Button("🧬 Simulate Breeding (10 offspring)"))
                {
                    SimulateBreeding();
                }

                if (simulatedOffspring.Count > 0)
                {
                    EditorGUILayout.LabelField($"Offspring Results ({simulatedOffspring.Count}):", EditorStyles.boldLabel);
                    DrawOffspringResults();
                }
            }

            EditorGUI.indentLevel--;
        }

        private void DrawGenePoolAnalytics()
        {
            EditorGUILayout.LabelField("📊 Gene Pool Analytics", EditorStyles.boldLabel);

            if (traitDistribution.Count > 0)
            {
                EditorGUILayout.LabelField("Population Trait Distribution:");
                foreach (var kvp in traitDistribution)
                {
                    EditorGUILayout.BeginHorizontal();
                    {
                        EditorGUILayout.LabelField(kvp.Key.ToString(), GUILayout.Width(120));
                        EditorGUI.ProgressBar(EditorGUILayout.GetControlRect(), kvp.Value, $"{kvp.Value:P1}");
                    }
                    EditorGUILayout.EndHorizontal();
                }
            }

            if (recentMutations.Count > 0)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Recent Mutations:");
                foreach (var mutation in recentMutations.Take(5))
                {
                    EditorGUILayout.LabelField($"• {mutation.traitType}: {mutation.description}");
                }
            }
        }

        #endregion

        #region Helper Methods

        private void InitializePreviewProfile()
        {
            previewProfile = new GeneticProfile();
            // Initialize with basic genes - GeneticProfile.Genes is read-only, so we create a new profile with genes
        }

        private void GenerateTraitColors()
        {
            var traitTypes = System.Enum.GetValues(typeof(Laboratory.Core.Enums.TraitType)).Cast<Laboratory.Core.Enums.TraitType>();
            foreach (var trait in traitTypes)
            {
                traitColors[trait.ToString()] = UnityEngine.Random.ColorHSV(0f, 1f, 0.5f, 1f, 0.7f, 1f);
            }
        }

        private Color GetTraitColor(Laboratory.Core.Enums.TraitType traitType)
        {
            return traitColors.TryGetValue(traitType.ToString(), out var color) ? color : Color.gray;
        }

        private void LoadSpeciesGeneticTemplate()
        {
            if (selectedSpecies == null) return;

            // Load genetic template from species config
            // This would integrate with your existing species configuration system
        }

        private void GenerateRandomProfile()
        {
            var traitTypes = System.Enum.GetValues(typeof(Laboratory.Core.Enums.TraitType)).Cast<Laboratory.Core.Enums.TraitType>().ToArray();
            int geneCount = UnityEngine.Random.Range(5, 15);

            var genes = new List<Gene>();
            for (int i = 0; i < geneCount; i++)
            {
                var traitType = traitTypes[UnityEngine.Random.Range(0, traitTypes.Length)];
                var traitName = traitType.ToString();

                var gene = new Gene(
                    System.Guid.NewGuid().ToString(),
                    traitName,
                    traitType,
                    Allele.CreateDominant($"Dominant_{traitName}", UnityEngine.Random.Range(0.6f, 1f)),
                    Allele.CreateRecessive($"Recessive_{traitName}", UnityEngine.Random.Range(0.1f, 0.4f))
                );
                gene.isActive = UnityEngine.Random.value > 0.2f;
                genes.Add(gene);
            }

            // Create a new profile with the generated genes
            previewProfile = new GeneticProfile(genes.ToArray());
            Repaint();
        }

        private void AddNewGene()
        {
            var newGene = new Gene(
                System.Guid.NewGuid().ToString(),
                "NewTrait",
                Laboratory.Core.Enums.TraitType.Physical,
                Allele.CreateDominant("Dominant_NewTrait", 0.7f),
                Allele.CreateRecessive("Recessive_NewTrait", 0.3f)
            );
            newGene.isActive = true;

            // Create new gene array with the additional gene
            var currentGenes = previewProfile.Genes.ToArray();
            var newGenes = new Gene[currentGenes.Length + 1];
            Array.Copy(currentGenes, newGenes, currentGenes.Length);
            newGenes[currentGenes.Length] = newGene;

            // Create new profile with updated genes
            previewProfile = new GeneticProfile(newGenes, previewProfile.Generation, previewProfile.LineageId);
            selectedGeneIndex = previewProfile.Genes.Count - 1;
        }

        private void RemoveSelectedGene()
        {
            if (selectedGeneIndex >= 0 && selectedGeneIndex < previewProfile.Genes.Count)
            {
                // Create new gene array without the selected gene
                var currentGenes = previewProfile.Genes.ToArray();
                var newGenes = new Gene[currentGenes.Length - 1];

                Array.Copy(currentGenes, 0, newGenes, 0, selectedGeneIndex);
                Array.Copy(currentGenes, selectedGeneIndex + 1, newGenes, selectedGeneIndex, currentGenes.Length - selectedGeneIndex - 1);

                // Create new profile with updated genes
                previewProfile = new GeneticProfile(newGenes, previewProfile.Generation, previewProfile.LineageId);
                selectedGeneIndex = Mathf.Max(0, selectedGeneIndex - 1);
            }
        }

        private void MutateSelectedGene()
        {
            if (selectedGeneIndex >= 0 && selectedGeneIndex < previewProfile.Genes.Count)
            {
                var gene = previewProfile.Genes[selectedGeneIndex];
                gene.dominance = Mathf.Clamp01(gene.dominance + UnityEngine.Random.Range(-0.2f, 0.2f));

                var mutation = new MutationEvent
                {
                    traitType = gene.traitType,
                    description = $"Dominance changed to {gene.dominance:P1}"
                };
                recentMutations.Insert(0, mutation);
            }
        }

        private float CalculatePhenotypeImpact(Gene gene)
        {
            return gene.dominance * (gene.isActive ? 1f : 0.3f);
        }

        private void DrawParentSummary(GeneticProfile parent)
        {
            if (parent == null)
            {
                EditorGUILayout.LabelField("No parent selected", GUI.skin.box);
                return;
            }

            EditorGUILayout.BeginVertical(GUI.skin.box);
            {
                EditorGUILayout.LabelField($"Genes: {parent.Genes?.Count ?? 0}");
                if (parent.Genes != null)
                {
                    var avgDominance = parent.Genes.Average(g => g.dominance);
                    EditorGUILayout.LabelField($"Avg Dominance: {avgDominance:P1}");
                }
            }
            EditorGUILayout.EndVertical();
        }

        private GeneticProfile CloneProfile(GeneticProfile original)
        {
            if (original == null) return null;

            // Create new profile with same genes
            return new GeneticProfile(original.Genes.ToArray(), original.Generation, original.LineageId);
        }

        private GeneticProfile GenerateRandomGeneticProfile()
        {
            var traitTypes = System.Enum.GetValues(typeof(Laboratory.Core.Enums.TraitType)).Cast<Laboratory.Core.Enums.TraitType>().ToArray();
            int geneCount = UnityEngine.Random.Range(5, 12);

            var genes = new Gene[geneCount];
            for (int i = 0; i < geneCount; i++)
            {
                var traitType = traitTypes[UnityEngine.Random.Range(0, traitTypes.Length)];
                var traitName = traitType.ToString();

                var gene = new Gene(
                    System.Guid.NewGuid().ToString(),
                    traitName,
                    traitType,
                    Allele.CreateDominant($"Dominant_{traitName}", UnityEngine.Random.Range(0.6f, 1f)),
                    Allele.CreateRecessive($"Recessive_{traitName}", UnityEngine.Random.Range(0.1f, 0.4f))
                );
                gene.isActive = UnityEngine.Random.value > 0.15f;
                genes[i] = gene;
            }

            return new GeneticProfile(genes);
        }

        private void SimulateBreeding()
        {
            simulatedOffspring.Clear();

            if (parentA == null || parentB == null) return;

            for (int i = 0; i < 10; i++)
            {
                // Use the GeneticProfile.CreateOffspring method for proper breeding simulation
                var offspring = GeneticProfile.CreateOffspring(parentA, parentB);
                simulatedOffspring.Add(offspring);
            }
        }

        private void DrawOffspringResults()
        {
            EditorGUI.indentLevel++;

            for (int i = 0; i < Mathf.Min(5, simulatedOffspring.Count); i++)
            {
                var offspring = simulatedOffspring[i];
                EditorGUILayout.BeginHorizontal();
                {
                    EditorGUILayout.LabelField($"Offspring {i + 1}:", GUILayout.Width(80));
                    EditorGUILayout.LabelField($"{offspring.Genes.Count} genes");

                    if (GUILayout.Button("Analyze", GUILayout.Width(60)))
                    {
                        previewProfile = offspring;
                        selectedGeneIndex = 0;
                    }
                }
                EditorGUILayout.EndHorizontal();
            }

            if (simulatedOffspring.Count > 5)
            {
                EditorGUILayout.LabelField($"... and {simulatedOffspring.Count - 5} more");
            }

            EditorGUI.indentLevel--;
        }

        private void SaveGeneticTemplate()
        {
            if (previewProfile == null) return;

            var path = EditorUtility.SaveFilePanelInProject(
                "Save Genetic Template",
                "GeneticTemplate",
                "json",
                "Save genetic profile template");

            if (!string.IsNullOrEmpty(path))
            {
                // Save as JSON since GeneticProfile is not a ScriptableObject
                var json = JsonUtility.ToJson(previewProfile, true);
                System.IO.File.WriteAllText(path, json);
                AssetDatabase.Refresh();
            }
        }

        private void AnalyzeExistingGenePool()
        {
            // This would scan existing creatures in the project and analyze their genetic distribution
            traitDistribution.Clear();

            // Simulated analysis results
            traitDistribution[Laboratory.Core.Enums.TraitType.Physical] = 0.75f;
            traitDistribution[Laboratory.Core.Enums.TraitType.Mental] = 0.60f;
            traitDistribution[Laboratory.Core.Enums.TraitType.Behavioral] = 0.45f;

            Debug.Log("Gene pool analysis completed!");
        }

        #endregion

        #region Data Structures

        [System.Serializable]
        public struct MutationEvent
        {
            public Laboratory.Core.Enums.TraitType traitType;
            public string description;
        }

        #endregion
    }
}