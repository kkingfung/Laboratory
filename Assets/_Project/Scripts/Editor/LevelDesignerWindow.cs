using UnityEngine;
using UnityEditor;
using UnityEngine.AI;
using UnityEditor.AI;
using System.Collections.Generic;
using Laboratory.Core;
using Laboratory.Gameplay;

namespace Laboratory.Editor
{
    /// <summary>
    /// Comprehensive level design tool that allows quick placement and configuration
    /// of spawn points, objectives, pickups, and interactive objects.
    /// </summary>
    public class LevelDesignerWindow : EditorWindow
    {
        private enum DesignMode
        {
            PlayerSpawn,
            EnemySpawn,
            Pickups,
            Interactables,
            Objectives,
            NavMesh
        }

        private DesignMode currentMode = DesignMode.PlayerSpawn;
        private Vector2 scrollPosition;
        
        // References
        private LevelManager levelManager;
        private GameObject selectedPrefab;
        
        // Prefab arrays
        private GameObject[] enemyPrefabs;
        private GameObject[] pickupPrefabs;
        private GameObject[] interactablePrefabs;
        
        // Settings
        private bool snapToGrid = true;
        private float gridSize = 1f;
        private bool showGizmos = true;
        private bool autoSelectLevelManager = true;
        
        // Colors for visualization
        private Color playerSpawnColor = Color.green;
        private Color enemySpawnColor = Color.red;
        private Color pickupColor = Color.yellow;
        private Color interactableColor = Color.blue;

        [MenuItem("🧪 Laboratory/Tools/Level Designer")]
        public static void ShowWindow()
        {
            LevelDesignerWindow window = GetWindow<LevelDesignerWindow>("Level Designer");
            window.minSize = new Vector2(300, 500);
        }

        private void OnEnable()
        {
            SceneView.duringSceneGui += OnSceneGUI;
            LoadPrefabs();
            
            if (autoSelectLevelManager)
            {
                FindLevelManager();
            }
        }

        private void OnDisable()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Level Designer", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            DrawLevelManagerSection();
            DrawModeSelection();
            DrawCurrentModeOptions();
            DrawSettingsSection();
            DrawUtilitySection();
        }

        private void DrawLevelManagerSection()
        {
            EditorGUILayout.LabelField("Level Manager", EditorStyles.boldLabel);
            
            EditorGUI.BeginChangeCheck();
            levelManager = (LevelManager)EditorGUILayout.ObjectField("Level Manager", levelManager, typeof(LevelManager), true);
            if (EditorGUI.EndChangeCheck() && levelManager == null)
            {
                FindLevelManager();
            }

            if (levelManager == null)
            {
                EditorGUILayout.HelpBox("No LevelManager found in scene. Create one or assign manually.", MessageType.Warning);
                if (GUILayout.Button("Create Level Manager"))
                {
                    CreateLevelManager();
                }
                return;
            }

            EditorGUILayout.Space();
        }

        private void DrawModeSelection()
        {
            EditorGUILayout.LabelField("Design Mode", EditorStyles.boldLabel);
            
            GUILayout.BeginHorizontal();
            if (GUILayout.Toggle(currentMode == DesignMode.PlayerSpawn, "Player Spawn", "Button"))
                currentMode = DesignMode.PlayerSpawn;
            if (GUILayout.Toggle(currentMode == DesignMode.EnemySpawn, "Enemy Spawn", "Button"))
                currentMode = DesignMode.EnemySpawn;
            if (GUILayout.Toggle(currentMode == DesignMode.Pickups, "Pickups", "Button"))
                currentMode = DesignMode.Pickups;
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Toggle(currentMode == DesignMode.Interactables, "Interactables", "Button"))
                currentMode = DesignMode.Interactables;
            if (GUILayout.Toggle(currentMode == DesignMode.Objectives, "Objectives", "Button"))
                currentMode = DesignMode.Objectives;
            if (GUILayout.Toggle(currentMode == DesignMode.NavMesh, "NavMesh", "Button"))
                currentMode = DesignMode.NavMesh;
            GUILayout.EndHorizontal();

            EditorGUILayout.Space();
        }

        private void DrawCurrentModeOptions()
        {
            EditorGUILayout.LabelField($"{currentMode} Options", EditorStyles.boldLabel);

            switch (currentMode)
            {
                case DesignMode.PlayerSpawn:
                    DrawPlayerSpawnOptions();
                    break;
                case DesignMode.EnemySpawn:
                    DrawEnemySpawnOptions();
                    break;
                case DesignMode.Pickups:
                    DrawPickupOptions();
                    break;
                case DesignMode.Interactables:
                    DrawInteractableOptions();
                    break;
                case DesignMode.Objectives:
                    DrawObjectiveOptions();
                    break;
                case DesignMode.NavMesh:
                    DrawNavMeshOptions();
                    break;
            }

            EditorGUILayout.Space();
        }

        private void DrawPlayerSpawnOptions()
        {
            EditorGUILayout.HelpBox("Click in scene view to place player spawn point. Only one spawn point allowed.", MessageType.Info);
            
            if (GUILayout.Button("Clear Player Spawn"))
            {
                ClearPlayerSpawn();
            }
        }

        private void DrawEnemySpawnOptions()
        {
            EditorGUILayout.LabelField("Enemy Prefabs:");
            
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(100));
            for (int i = 0; i < enemyPrefabs.Length; i++)
            {
                if (enemyPrefabs[i] != null)
                {
                    bool isSelected = selectedPrefab == enemyPrefabs[i];
                    if (GUILayout.Toggle(isSelected, enemyPrefabs[i].name, "Button"))
                    {
                        selectedPrefab = enemyPrefabs[i];
                    }
                }
            }
            EditorGUILayout.EndScrollView();

            EditorGUILayout.HelpBox("Select an enemy prefab and click in scene view to place spawn points.", MessageType.Info);
            
            if (GUILayout.Button("Clear All Enemy Spawns"))
            {
                ClearEnemySpawns();
            }
        }

        private void DrawPickupOptions()
        {
            EditorGUILayout.LabelField("Pickup Prefabs:");
            
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(100));
            for (int i = 0; i < pickupPrefabs.Length; i++)
            {
                if (pickupPrefabs[i] != null)
                {
                    bool isSelected = selectedPrefab == pickupPrefabs[i];
                    if (GUILayout.Toggle(isSelected, pickupPrefabs[i].name, "Button"))
                    {
                        selectedPrefab = pickupPrefabs[i];
                    }
                }
            }
            EditorGUILayout.EndScrollView();

            EditorGUILayout.HelpBox("Select a pickup prefab and click in scene view to place.", MessageType.Info);
            
            if (GUILayout.Button("Clear All Pickups"))
            {
                ClearPickups();
            }
        }

        private void DrawInteractableOptions()
        {
            EditorGUILayout.LabelField("Interactable Prefabs:");
            
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(100));
            for (int i = 0; i < interactablePrefabs.Length; i++)
            {
                if (interactablePrefabs[i] != null)
                {
                    bool isSelected = selectedPrefab == interactablePrefabs[i];
                    if (GUILayout.Toggle(isSelected, interactablePrefabs[i].name, "Button"))
                    {
                        selectedPrefab = interactablePrefabs[i];
                    }
                }
            }
            EditorGUILayout.EndScrollView();

            EditorGUILayout.HelpBox("Select an interactable prefab and click in scene view to place.", MessageType.Info);
        }

        private void DrawObjectiveOptions()
        {
            EditorGUILayout.HelpBox("Objective management coming soon. Use LevelManager inspector for now.", MessageType.Info);
        }

        private void DrawNavMeshOptions()
        {
            EditorGUILayout.HelpBox("NavMesh Tools", MessageType.Info);
            
            if (GUILayout.Button("Bake NavMesh"))
            {
                // Use the Window menu to open NavMesh baking
                EditorApplication.ExecuteMenuItem("Window/AI/Navigation");
                Debug.Log("Please use the Navigation window to bake NavMesh");
            }
            
            if (GUILayout.Button("Clear NavMesh"))
            {
                UnityEngine.AI.NavMesh.RemoveAllNavMeshData();
            }
        }

        private void DrawSettingsSection()
        {
            EditorGUILayout.LabelField("Settings", EditorStyles.boldLabel);
            
            snapToGrid = EditorGUILayout.Toggle("Snap to Grid", snapToGrid);
            if (snapToGrid)
            {
                gridSize = EditorGUILayout.FloatField("Grid Size", gridSize);
            }
            
            showGizmos = EditorGUILayout.Toggle("Show Gizmos", showGizmos);
            autoSelectLevelManager = EditorGUILayout.Toggle("Auto Select Level Manager", autoSelectLevelManager);

            EditorGUILayout.Space();
        }

        private void DrawUtilitySection()
        {
            EditorGUILayout.LabelField("Utilities", EditorStyles.boldLabel);
            
            if (GUILayout.Button("Refresh Prefabs"))
            {
                LoadPrefabs();
            }
            
            if (GUILayout.Button("Focus on Level Manager"))
            {
                if (levelManager != null)
                {
                    Selection.activeGameObject = levelManager.gameObject;
                    SceneView.FrameLastActiveSceneView();
                }
            }
            
            if (GUILayout.Button("Validate Level Setup"))
            {
                ValidateLevelSetup();
            }
        }

        private void OnSceneGUI(SceneView sceneView)
        {
            if (levelManager == null) return;

            Event e = Event.current;
            
            if (e.type == EventType.MouseDown && e.button == 0 && !e.alt)
            {
                Vector3 mousePosition = e.mousePosition;
                Ray ray = HandleUtility.GUIPointToWorldRay(mousePosition);
                
                if (Physics.Raycast(ray, out RaycastHit hit))
                {
                    Vector3 spawnPosition = hit.point;
                    
                    if (snapToGrid)
                    {
                        spawnPosition = SnapToGrid(spawnPosition);
                    }
                    
                    HandlePlacement(spawnPosition);
                    e.Use();
                }
            }

            if (showGizmos)
            {
                DrawSceneGizmos();
            }
        }

        private void HandlePlacement(Vector3 position)
        {
            switch (currentMode)
            {
                case DesignMode.PlayerSpawn:
                    PlacePlayerSpawn(position);
                    break;
                case DesignMode.EnemySpawn:
                    PlaceEnemySpawn(position);
                    break;
                case DesignMode.Pickups:
                    PlacePickup(position);
                    break;
                case DesignMode.Interactables:
                    PlaceInteractable(position);
                    break;
            }
        }

        private void PlacePlayerSpawn(Vector3 position)
        {
            // Create or move existing player spawn
            Transform existingSpawn = GameObject.FindGameObjectWithTag("PlayerSpawn")?.transform;
            
            if (existingSpawn == null)
            {
                GameObject spawn = new GameObject("PlayerSpawn");
                spawn.tag = "PlayerSpawn";
                spawn.transform.position = position;
                
                // Add visual indicator
                GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cube.transform.SetParent(spawn.transform);
                cube.transform.localScale = Vector3.one * 0.5f;
                cube.GetComponent<Renderer>().material.color = playerSpawnColor;
                DestroyImmediate(cube.GetComponent<Collider>());
            }
            else
            {
                existingSpawn.position = position;
            }
            
            Undo.RegisterCreatedObjectUndo(existingSpawn?.gameObject, "Place Player Spawn");
        }

        private void PlaceEnemySpawn(Vector3 position)
        {
            if (selectedPrefab == null)
            {
                UnityEngine.Debug.LogWarning("No enemy prefab selected!");
                return;
            }

            GameObject spawnPoint = new GameObject($"EnemySpawn_{selectedPrefab.name}");
            spawnPoint.transform.position = position;
            
            // Add visual indicator
            GameObject indicator = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            indicator.transform.SetParent(spawnPoint.transform);
            indicator.transform.localScale = new Vector3(0.5f, 0.1f, 0.5f);
            indicator.GetComponent<Renderer>().material.color = enemySpawnColor;
            DestroyImmediate(indicator.GetComponent<Collider>());
            
            Undo.RegisterCreatedObjectUndo(spawnPoint, "Place Enemy Spawn");
        }

        private void PlacePickup(Vector3 position)
        {
            if (selectedPrefab == null)
            {
                UnityEngine.Debug.LogWarning("No pickup prefab selected!");
                return;
            }

            GameObject pickup = PrefabUtility.InstantiatePrefab(selectedPrefab) as GameObject;
            pickup.transform.position = position;
            
            Undo.RegisterCreatedObjectUndo(pickup, "Place Pickup");
        }

        private void PlaceInteractable(Vector3 position)
        {
            if (selectedPrefab == null)
            {
                UnityEngine.Debug.LogWarning("No interactable prefab selected!");
                return;
            }

            GameObject interactable = PrefabUtility.InstantiatePrefab(selectedPrefab) as GameObject;
            interactable.transform.position = position;
            
            Undo.RegisterCreatedObjectUndo(interactable, "Place Interactable");
        }

        private Vector3 SnapToGrid(Vector3 position)
        {
            float snappedX = Mathf.Round(position.x / gridSize) * gridSize;
            float snappedZ = Mathf.Round(position.z / gridSize) * gridSize;
            return new Vector3(snappedX, position.y, snappedZ);
        }

        private void DrawSceneGizmos()
        {
            // Draw grid if enabled
            if (snapToGrid)
            {
                Handles.color = Color.gray;
                DrawGrid();
            }
        }

        private void DrawGrid()
        {
            Vector3 center = SceneView.lastActiveSceneView.camera.transform.position;
            int gridCount = 50;
            
            for (int i = -gridCount; i <= gridCount; i++)
            {
                Vector3 start = new Vector3(center.x - gridCount * gridSize, center.y, center.z + i * gridSize);
                Vector3 end = new Vector3(center.x + gridCount * gridSize, center.y, center.z + i * gridSize);
                Handles.DrawLine(start, end);
                
                start = new Vector3(center.x + i * gridSize, center.y, center.z - gridCount * gridSize);
                end = new Vector3(center.x + i * gridSize, center.y, center.z + gridCount * gridSize);
                Handles.DrawLine(start, end);
            }
        }

        private void LoadPrefabs()
        {
            // Load enemy prefabs
            string[] enemyGuids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/_Project/Prefabs/Enemies" });
            enemyPrefabs = new GameObject[enemyGuids.Length];
            for (int i = 0; i < enemyGuids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(enemyGuids[i]);
                enemyPrefabs[i] = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            }

            // Load pickup prefabs
            string[] pickupGuids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/_Project/Prefabs/Pickups" });
            pickupPrefabs = new GameObject[pickupGuids.Length];
            for (int i = 0; i < pickupGuids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(pickupGuids[i]);
                pickupPrefabs[i] = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            }

            // Load interactable prefabs
            string[] interactableGuids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/_Project/Prefabs/Interactables" });
            interactablePrefabs = new GameObject[interactableGuids.Length];
            for (int i = 0; i < interactableGuids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(interactableGuids[i]);
                interactablePrefabs[i] = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            }
        }

        private void FindLevelManager()
        {
            levelManager = FindFirstObjectByType<LevelManager>();
        }

        private void CreateLevelManager()
        {
            GameObject go = new GameObject("LevelManager");
            levelManager = go.AddComponent<LevelManager>();
            Undo.RegisterCreatedObjectUndo(go, "Create Level Manager");
        }

        private void ClearPlayerSpawn()
        {
            GameObject spawn = GameObject.FindGameObjectWithTag("PlayerSpawn");
            if (spawn != null)
            {
                Undo.DestroyObjectImmediate(spawn);
            }
        }

        private void ClearEnemySpawns()
        {
            GameObject[] spawns = GameObject.FindGameObjectsWithTag("EnemySpawn");
            foreach (GameObject spawn in spawns)
            {
                Undo.DestroyObjectImmediate(spawn);
            }
        }

        private void ClearPickups()
        {
            Pickup[] pickups = FindObjectsByType<Pickup>(FindObjectsSortMode.None);
            foreach (Pickup pickup in pickups)
            {
                Undo.DestroyObjectImmediate(pickup.gameObject);
            }
        }

        private void ValidateLevelSetup()
        {
            List<string> issues = new List<string>();

            if (levelManager == null)
                issues.Add("No LevelManager in scene");

            if (GameObject.FindGameObjectWithTag("PlayerSpawn") == null)
                issues.Add("No PlayerSpawn point set");

            if (FindObjectsByType<Laboratory.Subsystems.EnemyAI.EnemyController>(FindObjectsSortMode.None).Length == 0)
                issues.Add("No enemies in scene");

            if (issues.Count == 0)
            {
                EditorUtility.DisplayDialog("Level Validation", "Level setup looks good!", "OK");
            }
            else
            {
                string message = "Issues found:\n" + string.Join("\n", issues);
                EditorUtility.DisplayDialog("Level Validation", message, "OK");
            }
        }
    }
}
