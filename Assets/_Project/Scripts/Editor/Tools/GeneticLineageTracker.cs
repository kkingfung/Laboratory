using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using System;

namespace Laboratory.Editor.Tools
{
    /// <summary>
    /// Visual genetic lineage tracker for Project Chimera
    /// Displays creature family trees, breeding history, and trait inheritance
    /// Helps designers understand genetic diversity and breeding patterns
    /// </summary>
    public class GeneticLineageTracker : EditorWindow
    {
        private Vector2 _scrollPosition;
        private Vector2 _treeScrollPosition;
        private string _searchFilter = "";
        private LineageViewMode _viewMode = LineageViewMode.Tree;
        private string _selectedCreatureId = "";

        // Lineage data
        private readonly List<CreatureLineageData> _allCreatures = new List<CreatureLineageData>();
        private readonly Dictionary<string, CreatureLineageData> _creatureMap = new Dictionary<string, CreatureLineageData>();
        private readonly List<BreedingEvent> _breedingHistory = new List<BreedingEvent>();

        // Visualization
        private readonly Dictionary<string, Rect> _nodePositions = new Dictionary<string, Rect>();
        private float _zoomLevel = 1.0f;
        private Vector2 _panOffset = Vector2.zero;
        private const float NODE_WIDTH = 150f;
        private const float NODE_HEIGHT = 80f;
        private const float NODE_SPACING_X = 200f;
        private const float NODE_SPACING_Y = 120f;

        // Colors
        private readonly Color colorMale = new Color(0.5f, 0.7f, 1f);
        private readonly Color colorFemale = new Color(1f, 0.7f, 0.8f);
        private readonly Color colorUnknown = Color.gray;
        private readonly Color colorSelected = Color.yellow;
        private readonly Color colorHighGenetic = Color.green;
        private readonly Color colorLowGenetic = Color.red;

        private enum LineageViewMode
        {
            Tree,
            List,
            Statistics
        }

        [MenuItem("Chimera/Genetics/Lineage Tracker", false, 300)]
        private static void ShowWindow()
        {
            var window = GetWindow<GeneticLineageTracker>("Genetic Lineage Tracker");
            window.minSize = new Vector2(800, 600);
            window.Show();
        }

        private void OnEnable()
        {
            GenerateMockData();
        }

        private void OnGUI()
        {
            DrawToolbar();

            EditorGUILayout.BeginVertical();

            switch (_viewMode)
            {
                case LineageViewMode.Tree:
                    DrawTreeView();
                    break;

                case LineageViewMode.List:
                    DrawListView();
                    break;

                case LineageViewMode.Statistics:
                    DrawStatisticsView();
                    break;
            }

            EditorGUILayout.EndVertical();

            DrawBottomPanel();
        }

        #region Toolbar

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            // Title
            GUILayout.Label("🧬 Genetic Lineage Tracker", EditorStyles.boldLabel);

            GUILayout.FlexibleSpace();

            // View mode
            _viewMode = (LineageViewMode)EditorGUILayout.EnumPopup(_viewMode, EditorStyles.toolbarDropDown, GUILayout.Width(100));

            // Search
            _searchFilter = EditorGUILayout.TextField(_searchFilter, EditorStyles.toolbarSearchField, GUILayout.Width(200));

            // Actions
            if (GUILayout.Button("Refresh", EditorStyles.toolbarButton))
            {
                RefreshData();
            }

            if (GUILayout.Button("Export", EditorStyles.toolbarButton))
            {
                ExportLineageData();
            }

            if (GUILayout.Button("Clear", EditorStyles.toolbarButton))
            {
                ClearData();
            }

            EditorGUILayout.EndHorizontal();
        }

        #endregion

        #region Tree View

        private void DrawTreeView()
        {
            EditorGUILayout.BeginHorizontal();

            // Tree canvas
            DrawTreeCanvas();

            // Side panel for selected creature
            DrawCreatureDetailPanel();

            EditorGUILayout.EndHorizontal();
        }

        private void DrawTreeCanvas()
        {
            EditorGUILayout.BeginVertical(GUI.skin.box, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));

            // Controls
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Zoom:", GUILayout.Width(50));
            _zoomLevel = EditorGUILayout.Slider(_zoomLevel, 0.5f, 2.0f, GUILayout.Width(150));

            if (GUILayout.Button("Reset View", GUILayout.Width(100)))
            {
                _zoomLevel = 1.0f;
                _panOffset = Vector2.zero;
            }

            GUILayout.FlexibleSpace();
            GUILayout.Label($"Creatures: {_allCreatures.Count} | Generations: {CalculateMaxGeneration()}");
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);

            // Tree visualization
            Rect canvasRect = GUILayoutUtility.GetRect(0, 0, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));

            if (Event.current.type == EventType.Repaint)
            {
                GUI.Box(canvasRect, "", EditorStyles.helpBox);
            }

            // Calculate layout
            CalculateTreeLayout();

            // Draw connections first
            DrawConnections(canvasRect);

            // Draw nodes
            DrawNodes(canvasRect);

            // Handle input
            HandleTreeInput(canvasRect);

            EditorGUILayout.EndVertical();
        }

        private void CalculateTreeLayout()
        {
            _nodePositions.Clear();

            // Group creatures by generation
            var generationGroups = _allCreatures
                .GroupBy(c => c.generation)
                .OrderBy(g => g.Key)
                .ToList();

            float currentY = 50f;

            foreach (var generation in generationGroups)
            {
                var creatures = generation.OrderBy(c => c.speciesName).ToList();
                float currentX = 50f;

                foreach (var creature in creatures)
                {
                    _nodePositions[creature.creatureId] = new Rect(
                        currentX * _zoomLevel + _panOffset.x,
                        currentY * _zoomLevel + _panOffset.y,
                        NODE_WIDTH * _zoomLevel,
                        NODE_HEIGHT * _zoomLevel
                    );

                    currentX += NODE_SPACING_X;
                }

                currentY += NODE_SPACING_Y;
            }
        }

        private void DrawConnections(Rect canvasRect)
        {
            Handles.BeginGUI();

            foreach (var creature in _allCreatures)
            {
                if (creature.parent1Id != null && _nodePositions.ContainsKey(creature.parent1Id) &&
                    _nodePositions.ContainsKey(creature.creatureId))
                {
                    DrawLineage(
                        _nodePositions[creature.parent1Id],
                        _nodePositions[creature.creatureId],
                        Color.cyan
                    );
                }

                if (creature.parent2Id != null && _nodePositions.ContainsKey(creature.parent2Id) &&
                    _nodePositions.ContainsKey(creature.creatureId))
                {
                    DrawLineage(
                        _nodePositions[creature.parent2Id],
                        _nodePositions[creature.creatureId],
                        Color.magenta
                    );
                }
            }

            Handles.EndGUI();
        }

        private void DrawLineage(Rect from, Rect to, Color color)
        {
            Vector3 start = new Vector3(from.center.x, from.yMax, 0);
            Vector3 end = new Vector3(to.center.x, to.yMin, 0);

            Handles.color = color;
            Handles.DrawAAPolyLine(3f, start, end);
        }

        private void DrawNodes(Rect canvasRect)
        {
            foreach (var kvp in _nodePositions)
            {
                var creature = _creatureMap[kvp.Key];
                var nodeRect = kvp.Value;

                // Skip nodes outside visible area
                if (!nodeRect.Overlaps(canvasRect))
                    continue;

                DrawCreatureNode(creature, nodeRect);
            }
        }

        private void DrawCreatureNode(CreatureLineageData creature, Rect nodeRect)
        {
            bool isSelected = creature.creatureId == _selectedCreatureId;

            // Node background
            Color bgColor = GetGenderColor(creature.gender);
            if (isSelected)
                bgColor = Color.Lerp(bgColor, colorSelected, 0.5f);

            EditorGUI.DrawRect(nodeRect, bgColor);
            GUI.Box(nodeRect, "", EditorStyles.helpBox);

            // Content
            GUILayout.BeginArea(nodeRect);
            EditorGUILayout.BeginVertical();

            // Name
            var nameStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = (int)(10 * _zoomLevel), alignment = TextAnchor.MiddleCenter };
            EditorGUILayout.LabelField(creature.name, nameStyle);

            // Species
            var speciesStyle = new GUIStyle(EditorStyles.miniLabel) { fontSize = (int)(8 * _zoomLevel), alignment = TextAnchor.MiddleCenter };
            EditorGUILayout.LabelField(creature.speciesName, speciesStyle);

            // Generation
            EditorGUILayout.LabelField($"Gen {creature.generation}", speciesStyle);

            // Fitness
            float fitness = creature.geneticFitness;
            Color fitnessColor = Color.Lerp(colorLowGenetic, colorHighGenetic, fitness);
            var fitnessRect = GUILayoutUtility.GetRect(0, 3);
            EditorGUI.DrawRect(fitnessRect, fitnessColor);

            EditorGUILayout.EndVertical();
            GUILayout.EndArea();

            // Handle click
            if (Event.current.type == EventType.MouseDown && nodeRect.Contains(Event.current.mousePosition))
            {
                _selectedCreatureId = creature.creatureId;
                Event.current.Use();
                Repaint();
            }
        }

        private void HandleTreeInput(Rect canvasRect)
        {
            if (!canvasRect.Contains(Event.current.mousePosition))
                return;

            if (Event.current.type == EventType.ScrollWheel)
            {
                float zoomDelta = -Event.current.delta.y * 0.05f;
                _zoomLevel = Mathf.Clamp(_zoomLevel + zoomDelta, 0.5f, 2.0f);
                Event.current.Use();
                Repaint();
            }

            if (Event.current.type == EventType.MouseDrag && Event.current.button == 2)
            {
                _panOffset += Event.current.delta;
                Event.current.Use();
                Repaint();
            }
        }

        #endregion

        #region List View

        private void DrawListView()
        {
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            // Header
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Label("Name", EditorStyles.boldLabel, GUILayout.Width(150));
            GUILayout.Label("Species", EditorStyles.boldLabel, GUILayout.Width(100));
            GUILayout.Label("Gen", EditorStyles.boldLabel, GUILayout.Width(40));
            GUILayout.Label("Gender", EditorStyles.boldLabel, GUILayout.Width(60));
            GUILayout.Label("Fitness", EditorStyles.boldLabel, GUILayout.Width(60));
            GUILayout.Label("Offspring", EditorStyles.boldLabel, GUILayout.Width(70));
            GUILayout.Label("Born", EditorStyles.boldLabel, GUILayout.Width(100));
            EditorGUILayout.EndHorizontal();

            // Filter creatures
            var filteredCreatures = string.IsNullOrEmpty(_searchFilter)
                ? _allCreatures
                : _allCreatures.Where(c =>
                    c.name.Contains(_searchFilter, StringComparison.OrdinalIgnoreCase) ||
                    c.speciesName.Contains(_searchFilter, StringComparison.OrdinalIgnoreCase)
                ).ToList();

            // List items
            foreach (var creature in filteredCreatures.OrderByDescending(c => c.birthTime))
            {
                DrawListItem(creature);
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawListItem(CreatureLineageData creature)
        {
            bool isSelected = creature.creatureId == _selectedCreatureId;
            Color bgColor = isSelected ? colorSelected : (GUI.backgroundColor);

            GUI.backgroundColor = bgColor;

            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);

            GUILayout.Label(creature.name, GUILayout.Width(150));
            GUILayout.Label(creature.speciesName, GUILayout.Width(100));
            GUILayout.Label(creature.generation.ToString(), GUILayout.Width(40));

            GUI.color = GetGenderColor(creature.gender);
            GUILayout.Label(creature.gender.ToString(), GUILayout.Width(60));
            GUI.color = Color.white;

            GUILayout.Label($"{creature.geneticFitness:P0}", GUILayout.Width(60));
            GUILayout.Label(creature.offspringCount.ToString(), GUILayout.Width(70));
            GUILayout.Label(creature.birthTime.ToString("yyyy-MM-dd"), GUILayout.Width(100));

            if (GUILayout.Button("Select", GUILayout.Width(60)))
            {
                _selectedCreatureId = creature.creatureId;
            }

            EditorGUILayout.EndHorizontal();

            GUI.backgroundColor = Color.white;
        }

        #endregion

        #region Statistics View

        private void DrawStatisticsView()
        {
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            // Population statistics
            EditorGUILayout.LabelField("Population Statistics", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.LabelField($"Total Creatures: {_allCreatures.Count}");
            EditorGUILayout.LabelField($"Total Breeding Events: {_breedingHistory.Count}");
            EditorGUILayout.LabelField($"Max Generation: {CalculateMaxGeneration()}");
            EditorGUILayout.LabelField($"Avg Genetic Fitness: {CalculateAverageFitness():P1}");

            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(10);

            // Species breakdown
            EditorGUILayout.LabelField("Species Distribution", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            var speciesGroups = _allCreatures.GroupBy(c => c.speciesName).OrderByDescending(g => g.Count());
            foreach (var group in speciesGroups)
            {
                float percentage = (float)group.Count() / _allCreatures.Count;
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(group.Key, GUILayout.Width(150));
                EditorGUILayout.LabelField($"{group.Count()} ({percentage:P1})");
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(10);

            // Generation breakdown
            EditorGUILayout.LabelField("Generation Distribution", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            var generationGroups = _allCreatures.GroupBy(c => c.generation).OrderBy(g => g.Key);
            foreach (var group in generationGroups)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"Generation {group.Key}", GUILayout.Width(150));
                EditorGUILayout.LabelField($"{group.Count()} creatures");
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(10);

            // Genetic diversity
            EditorGUILayout.LabelField("Genetic Diversity", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            float avgFitness = CalculateAverageFitness();
            float diversityScore = CalculateGeneticDiversity();

            EditorGUILayout.LabelField($"Average Fitness: {avgFitness:P1}");
            EditorGUILayout.LabelField($"Diversity Score: {diversityScore:F2}");

            Color diversityColor = diversityScore > 0.7f ? colorHighGenetic : diversityScore > 0.4f ? Color.yellow : colorLowGenetic;
            string diversityRating = diversityScore > 0.7f ? "Excellent" : diversityScore > 0.4f ? "Good" : "Low";

            GUI.color = diversityColor;
            EditorGUILayout.LabelField($"Rating: {diversityRating}", EditorStyles.boldLabel);
            GUI.color = Color.white;

            EditorGUILayout.EndVertical();

            EditorGUILayout.EndScrollView();
        }

        #endregion

        #region Detail Panel

        private void DrawCreatureDetailPanel()
        {
            EditorGUILayout.BeginVertical(GUI.skin.box, GUILayout.Width(250));

            EditorGUILayout.LabelField("Creature Details", EditorStyles.boldLabel);

            if (string.IsNullOrEmpty(_selectedCreatureId) || !_creatureMap.ContainsKey(_selectedCreatureId))
            {
                EditorGUILayout.HelpBox("Select a creature to view details", MessageType.Info);
                EditorGUILayout.EndVertical();
                return;
            }

            var creature = _creatureMap[_selectedCreatureId];

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            // Basic info
            EditorGUILayout.LabelField("Name:", creature.name);
            EditorGUILayout.LabelField("Species:", creature.speciesName);
            EditorGUILayout.LabelField("Generation:", creature.generation.ToString());
            EditorGUILayout.LabelField("Gender:", creature.gender.ToString());
            EditorGUILayout.LabelField("Born:", creature.birthTime.ToString("yyyy-MM-dd HH:mm"));

            EditorGUILayout.Space(5);

            // Genetics
            EditorGUILayout.LabelField("Genetics", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"Fitness: {creature.geneticFitness:P1}");
            EditorGUILayout.LabelField($"Traits: {creature.traitCount}");

            EditorGUILayout.Space(5);

            // Parents
            EditorGUILayout.LabelField("Lineage", EditorStyles.boldLabel);

            if (!string.IsNullOrEmpty(creature.parent1Id) && _creatureMap.ContainsKey(creature.parent1Id))
            {
                var parent1 = _creatureMap[creature.parent1Id];
                if (GUILayout.Button($"Parent 1: {parent1.name}"))
                {
                    _selectedCreatureId = parent1.creatureId;
                }
            }
            else
            {
                EditorGUILayout.LabelField("Parent 1: Unknown (Gen 0)");
            }

            if (!string.IsNullOrEmpty(creature.parent2Id) && _creatureMap.ContainsKey(creature.parent2Id))
            {
                var parent2 = _creatureMap[creature.parent2Id];
                if (GUILayout.Button($"Parent 2: {parent2.name}"))
                {
                    _selectedCreatureId = parent2.creatureId;
                }
            }
            else if (creature.generation > 0)
            {
                EditorGUILayout.LabelField("Parent 2: Unknown");
            }

            EditorGUILayout.Space(5);

            // Offspring
            EditorGUILayout.LabelField("Offspring", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"Count: {creature.offspringCount}");

            var offspring = _allCreatures.Where(c => c.parent1Id == creature.creatureId || c.parent2Id == creature.creatureId).ToList();
            foreach (var child in offspring.Take(5))
            {
                if (GUILayout.Button($"→ {child.name}", EditorStyles.miniButton))
                {
                    _selectedCreatureId = child.creatureId;
                }
            }

            if (offspring.Count > 5)
            {
                EditorGUILayout.LabelField($"... and {offspring.Count - 5} more");
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        #endregion

        #region Bottom Panel

        private void DrawBottomPanel()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar, GUILayout.Height(20));

            GUILayout.Label($"Lineage Depth: {CalculateMaxGeneration()} | Avg Fitness: {CalculateAverageFitness():P1} | Diversity: {CalculateGeneticDiversity():F2}");

            GUILayout.FlexibleSpace();

            if (!string.IsNullOrEmpty(_selectedCreatureId))
            {
                GUILayout.Label($"Selected: {_creatureMap[_selectedCreatureId].name}");
            }

            EditorGUILayout.EndHorizontal();
        }

        #endregion

        #region Data Management

        private void RefreshData()
        {
            // In production, this would query the actual genetics system
            GenerateMockData();
            Repaint();
        }

        private void ClearData()
        {
            if (EditorUtility.DisplayDialog("Clear Data", "Are you sure you want to clear all lineage data?", "Yes", "No"))
            {
                _allCreatures.Clear();
                _creatureMap.Clear();
                _breedingHistory.Clear();
                _selectedCreatureId = "";
                Repaint();
            }
        }

        private void ExportLineageData()
        {
            string path = EditorUtility.SaveFilePanel("Export Lineage Data", "", "lineage_export.json", "json");
            if (!string.IsNullOrEmpty(path))
            {
                var exportData = new LineageExportData
                {
                    exportDate = DateTime.Now,
                    creatures = _allCreatures,
                    breedingEvents = _breedingHistory
                };

                string json = JsonUtility.ToJson(exportData, true);
                System.IO.File.WriteAllText(path, json);

                EditorUtility.DisplayDialog("Export Complete", $"Exported {_allCreatures.Count} creatures to:\n{path}", "OK");
            }
        }

        private void GenerateMockData()
        {
            _allCreatures.Clear();
            _creatureMap.Clear();
            _breedingHistory.Clear();

            // Generate founders (Gen 0)
            for (int i = 0; i < 8; i++)
            {
                var creature = new CreatureLineageData
                {
                    creatureId = Guid.NewGuid().ToString(),
                    name = $"Founder {i + 1}",
                    speciesName = i % 2 == 0 ? "Dragon" : "Phoenix",
                    generation = 0,
                    gender = (Gender)(i % 3),
                    geneticFitness = UnityEngine.Random.Range(0.5f, 0.9f),
                    traitCount = UnityEngine.Random.Range(5, 15),
                    birthTime = DateTime.Now.AddDays(-90),
                    parent1Id = null,
                    parent2Id = null,
                    offspringCount = 0
                };

                _allCreatures.Add(creature);
                _creatureMap[creature.creatureId] = creature;
            }

            // Generate offspring (Gen 1-3)
            for (int gen = 1; gen <= 3; gen++)
            {
                var previousGen = _allCreatures.Where(c => c.generation == gen - 1).ToList();

                for (int i = 0; i < 10; i++)
                {
                    if (previousGen.Count < 2) break;

                    var parent1 = previousGen[UnityEngine.Random.Range(0, previousGen.Count)];
                    var parent2 = previousGen[UnityEngine.Random.Range(0, previousGen.Count)];

                    if (parent1 == parent2) continue;

                    var offspring = new CreatureLineageData
                    {
                        creatureId = Guid.NewGuid().ToString(),
                        name = $"Gen{gen}-{i + 1}",
                        speciesName = UnityEngine.Random.value > 0.5f ? parent1.speciesName : parent2.speciesName,
                        generation = gen,
                        gender = (Gender)UnityEngine.Random.Range(0, 3),
                        geneticFitness = (parent1.geneticFitness + parent2.geneticFitness) / 2f + UnityEngine.Random.Range(-0.1f, 0.1f),
                        traitCount = (parent1.traitCount + parent2.traitCount) / 2,
                        birthTime = DateTime.Now.AddDays(-90 + gen * 30),
                        parent1Id = parent1.creatureId,
                        parent2Id = parent2.creatureId,
                        offspringCount = 0
                    };

                    offspring.geneticFitness = Mathf.Clamp01(offspring.geneticFitness);

                    _allCreatures.Add(offspring);
                    _creatureMap[offspring.creatureId] = offspring;

                    parent1.offspringCount++;
                    parent2.offspringCount++;
                }
            }

            Debug.Log($"[GeneticLineageTracker] Generated {_allCreatures.Count} mock creatures");
        }

        #endregion

        #region Helper Methods

        private Color GetGenderColor(Gender gender)
        {
            return gender switch
            {
                Gender.Male => colorMale,
                Gender.Female => colorFemale,
                _ => colorUnknown
            };
        }

        private int CalculateMaxGeneration()
        {
            return _allCreatures.Count > 0 ? _allCreatures.Max(c => c.generation) : 0;
        }

        private float CalculateAverageFitness()
        {
            return _allCreatures.Count > 0 ? _allCreatures.Average(c => c.geneticFitness) : 0f;
        }

        private float CalculateGeneticDiversity()
        {
            if (_allCreatures.Count < 2) return 0f;

            // Simple diversity calculation based on fitness variance
            float avgFitness = CalculateAverageFitness();
            float variance = _allCreatures.Average(c => Mathf.Pow(c.geneticFitness - avgFitness, 2));
            float stdDev = Mathf.Sqrt(variance);

            // Normalize to 0-1 range
            return Mathf.Clamp01(stdDev * 3f);
        }

        #endregion

        #region Data Structures

        [Serializable]
        private class CreatureLineageData
        {
            public string creatureId;
            public string name;
            public string speciesName;
            public int generation;
            public Gender gender;
            public float geneticFitness;
            public int traitCount;
            public DateTime birthTime;
            public string parent1Id;
            public string parent2Id;
            public int offspringCount;
        }

        [Serializable]
        private class BreedingEvent
        {
            public DateTime timestamp;
            public string parent1Id;
            public string parent2Id;
            public string offspringId;
            public float successRate;
        }

        [Serializable]
        private class LineageExportData
        {
            public DateTime exportDate;
            public List<CreatureLineageData> creatures;
            public List<BreedingEvent> breedingEvents;
        }

        private enum Gender
        {
            Male,
            Female,
            Unknown
        }

        #endregion
    }
}
