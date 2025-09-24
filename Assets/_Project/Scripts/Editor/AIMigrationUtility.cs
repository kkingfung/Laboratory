using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using Laboratory.Chimera.AI;
using Laboratory.Subsystems.EnemyAI;
using Laboratory.AI.Agents;
using Laboratory.AI.Pathfinding;

namespace Laboratory.AI.Tools
{
    /// <summary>
    /// Editor utility to help migrate existing AI controllers to the enhanced pathfinding system
    /// </summary>
    public class AIMigrationUtility : EditorWindow
    {
        private Vector2 scrollPosition;
        private bool showAdvancedOptions = false;
        private bool backupExistingComponents = true;
        private bool preserveSettings = true;
        private bool autoSetupPathfindingSystem = true;

        private List<ChimeraMonsterAI> chimeraAIs = new List<ChimeraMonsterAI>();
        private List<EnemyController> enemyControllers = new List<EnemyController>();
        private List<GameObject> objectsToMigrate = new List<GameObject>();

        [MenuItem("Laboratory/AI Tools/Migration Utility")]
        public static void ShowWindow()
        {
            GetWindow<AIMigrationUtility>("AI Migration Utility");
        }

        private void OnEnable()
        {
            RefreshAIList();
        }

        private void OnGUI()
        {
            GUILayout.Label("Enhanced AI Migration Utility", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            DrawSetupSection();
            EditorGUILayout.Space();
            
            DrawScanSection();
            EditorGUILayout.Space();
            
            DrawMigrationSection();
            EditorGUILayout.Space();
            
            DrawAdvancedOptions();

            EditorGUILayout.EndScrollView();
        }

        private void DrawSetupSection()
        {
            EditorGUILayout.LabelField("1. Setup Enhanced Pathfinding System", EditorStyles.boldLabel);
            
            var pathfindingSystem = FindFirstObjectByType<EnhancedPathfindingSystem>();
            if (pathfindingSystem == null)
            {
                EditorGUILayout.HelpBox("Enhanced Pathfinding System not found in scene!", MessageType.Warning);
                
                if (GUILayout.Button("Create Pathfinding System"))
                {
                    CreatePathfindingSystem();
                }
            }
            else
            {
                EditorGUILayout.HelpBox("Enhanced Pathfinding System found and ready!", MessageType.Info);
                EditorGUILayout.ObjectField("Pathfinding System", pathfindingSystem, typeof(EnhancedPathfindingSystem), true);
            }
        }

        private void DrawScanSection()
        {
            EditorGUILayout.LabelField("2. Scan for AI Controllers", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Refresh AI List"))
            {
                RefreshAIList();
            }
            if (GUILayout.Button("Select All"))
            {
                SelectAllAI();
            }
            if (GUILayout.Button("Clear Selection"))
            {
                ClearSelection();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();
            
            // Show found AI controllers
            EditorGUILayout.LabelField($"Chimera Monster AIs Found: {chimeraAIs.Count}", EditorStyles.boldLabel);
            foreach (var ai in chimeraAIs)
            {
                EditorGUILayout.BeginHorizontal();
                bool isSelected = objectsToMigrate.Contains(ai.gameObject);
                bool newSelected = EditorGUILayout.Toggle(isSelected, GUILayout.Width(20));
                
                if (newSelected != isSelected)
                {
                    if (newSelected)
                        objectsToMigrate.Add(ai.gameObject);
                    else
                        objectsToMigrate.Remove(ai.gameObject);
                }
                
                EditorGUILayout.ObjectField(ai, typeof(ChimeraMonsterAI), true);
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.Space();
            
            EditorGUILayout.LabelField($"Enemy Controllers Found: {enemyControllers.Count}", EditorStyles.boldLabel);
            foreach (var enemy in enemyControllers)
            {
                EditorGUILayout.BeginHorizontal();
                bool isSelected = objectsToMigrate.Contains(enemy.gameObject);
                bool newSelected = EditorGUILayout.Toggle(isSelected, GUILayout.Width(20));
                
                if (newSelected != isSelected)
                {
                    if (newSelected)
                        objectsToMigrate.Add(enemy.gameObject);
                    else
                        objectsToMigrate.Remove(enemy.gameObject);
                }
                
                EditorGUILayout.ObjectField(enemy, typeof(EnemyController), true);
                EditorGUILayout.EndHorizontal();
            }
        }

        private void DrawMigrationSection()
        {
            EditorGUILayout.LabelField("3. Migration Options", EditorStyles.boldLabel);
            
            backupExistingComponents = EditorGUILayout.Toggle("Backup Existing Components", backupExistingComponents);
            preserveSettings = EditorGUILayout.Toggle("Preserve Settings", preserveSettings);
            
            EditorGUILayout.Space();
            
            EditorGUILayout.LabelField($"Objects Selected for Migration: {objectsToMigrate.Count}");
            
            if (objectsToMigrate.Count > 0)
            {
                if (GUILayout.Button("Migrate Selected AI Controllers", GUILayout.Height(30)))
                {
                    MigrateSelectedAI();
                }
                
                EditorGUILayout.Space();
                
                if (GUILayout.Button("Preview Migration (Dry Run)"))
                {
                    PreviewMigration();
                }
            }
            else
            {
                EditorGUILayout.HelpBox("Select AI controllers to migrate.", MessageType.Info);
            }
        }

        private void DrawAdvancedOptions()
        {
            showAdvancedOptions = EditorGUILayout.Foldout(showAdvancedOptions, "Advanced Options");
            
            if (showAdvancedOptions)
            {
                EditorGUI.indentLevel++;
                
                autoSetupPathfindingSystem = EditorGUILayout.Toggle("Auto-setup Pathfinding System", autoSetupPathfindingSystem);
                
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Batch Operations", EditorStyles.boldLabel);
                
                if (GUILayout.Button("Add Enhanced AI Agent to All Selected"))
                {
                    AddEnhancedAgentToSelected();
                }
                
                if (GUILayout.Button("Remove Old Components from All Selected"))
                {
                    RemoveOldComponentsFromSelected();
                }
                
                EditorGUI.indentLevel--;
            }
        }

        private void RefreshAIList()
        {
            chimeraAIs.Clear();
            enemyControllers.Clear();
            
            chimeraAIs.AddRange(FindObjectsByType<ChimeraMonsterAI>(FindObjectsSortMode.None));
            enemyControllers.AddRange(FindObjectsByType<EnemyController>(FindObjectsSortMode.None));
            
            Debug.Log($"Found {chimeraAIs.Count} Chimera AIs and {enemyControllers.Count} Enemy Controllers");
        }

        private void SelectAllAI()
        {
            objectsToMigrate.Clear();
            
            foreach (var ai in chimeraAIs)
            {
                objectsToMigrate.Add(ai.gameObject);
            }
            
            foreach (var enemy in enemyControllers)
            {
                objectsToMigrate.Add(enemy.gameObject);
            }
        }

        private void ClearSelection()
        {
            objectsToMigrate.Clear();
        }

        private void CreatePathfindingSystem()
        {
            GameObject pathfindingGO = new GameObject("Enhanced Pathfinding System");
            pathfindingGO.AddComponent<EnhancedPathfindingSystem>();
            
            // Position it in the scene hierarchy
            pathfindingGO.transform.SetAsFirstSibling();
            
            EditorUtility.SetDirty(pathfindingGO);
            Selection.activeGameObject = pathfindingGO;
            
            Debug.Log("Enhanced Pathfinding System created!");
        }

        private void PreviewMigration()
        {
            Debug.Log("=== Migration Preview ===");
            
            foreach (var obj in objectsToMigrate)
            {
                var chimeraAI = obj.GetComponent<ChimeraMonsterAI>();
                var enemyController = obj.GetComponent<EnemyController>();
                
                if (chimeraAI != null)
                {
                    Debug.Log($"Would migrate ChimeraMonsterAI on {obj.name} to EnhancedChimeraMonsterAI");
                }
                else if (enemyController != null)
                {
                    Debug.Log($"Would migrate EnemyController on {obj.name} to EnhancedEnemyController");
                }
                
                if (obj.GetComponent<EnhancedAIAgent>() == null)
                {
                    Debug.Log($"Would add EnhancedAIAgent to {obj.name}");
                }
            }
            
            Debug.Log("=== End Preview ===");
        }

        private void MigrateSelectedAI()
        {
            if (EditorUtility.DisplayDialog("Confirm Migration", 
                $"This will migrate {objectsToMigrate.Count} AI controllers to the enhanced system. " +
                "This action cannot be undone (unless you have backups). Continue?", 
                "Migrate", "Cancel"))
            {
                int successCount = 0;
                
                foreach (var obj in objectsToMigrate)
                {
                    try
                    {
                        if (MigrateAIController(obj))
                        {
                            successCount++;
                        }
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogError($"Failed to migrate {obj.name}: {e.Message}");
                    }
                }
                
                Debug.Log($"Migration completed! Successfully migrated {successCount}/{objectsToMigrate.Count} AI controllers.");
                
                // Refresh the list
                RefreshAIList();
                ClearSelection();
            }
        }

        private bool MigrateAIController(GameObject obj)
        {
            bool migrated = false;
            
            // Migrate ChimeraMonsterAI
            var chimeraAI = obj.GetComponent<ChimeraMonsterAI>();
            if (chimeraAI != null)
            {
                migrated = MigrateChimeraAI(obj, chimeraAI);
            }
            
            // Migrate EnemyController
            var enemyController = obj.GetComponent<EnemyController>();
            if (enemyController != null)
            {
                migrated = MigrateEnemyController(obj, enemyController);
            }
            
            // Add EnhancedAIAgent if not present
            if (obj.GetComponent<EnhancedAIAgent>() == null)
            {
                obj.AddComponent<EnhancedAIAgent>();
                Debug.Log($"Added EnhancedAIAgent to {obj.name}");
            }
            
            EditorUtility.SetDirty(obj);
            return migrated;
        }

        private bool MigrateChimeraAI(GameObject obj, ChimeraMonsterAI oldAI)
        {
            // Add new enhanced AI
            var newAI = obj.AddComponent<EnhancedChimeraMonsterAI>();
            
            if (preserveSettings)
            {
                // Copy settings from old AI to new AI using reflection
                CopyComponentSettings(oldAI, newAI);
            }
            
            if (backupExistingComponents)
            {
                // Disable old component instead of destroying it
                oldAI.enabled = false;
                Debug.Log($"Disabled old ChimeraMonsterAI on {obj.name} (backup)");
            }
            else
            {
                // Remove old component
                DestroyImmediate(oldAI);
                Debug.Log($"Removed old ChimeraMonsterAI from {obj.name}");
            }
            
            Debug.Log($"Successfully migrated ChimeraMonsterAI on {obj.name}");
            return true;
        }

        private bool MigrateEnemyController(GameObject obj, EnemyController oldEnemy)
        {
            // Add new enhanced enemy
            var newEnemy = obj.AddComponent<EnhancedEnemyController>();
            
            if (preserveSettings)
            {
                // Copy settings from old enemy to new enemy
                CopyComponentSettings(oldEnemy, newEnemy);
            }
            
            if (backupExistingComponents)
            {
                // Disable old component instead of destroying it
                oldEnemy.enabled = false;
                Debug.Log($"Disabled old EnemyController on {obj.name} (backup)");
            }
            else
            {
                // Remove old component
                DestroyImmediate(oldEnemy);
                Debug.Log($"Removed old EnemyController from {obj.name}");
            }
            
            Debug.Log($"Successfully migrated EnemyController on {obj.name}");
            return true;
        }

        private void CopyComponentSettings(Component source, Component target)
        {
            // Use SerializedObject to copy serialized fields
            var sourceSerializedObject = new SerializedObject(source);
            var targetSerializedObject = new SerializedObject(target);
            
            var sourceProperty = sourceSerializedObject.GetIterator();
            
            while (sourceProperty.NextVisible(true))
            {
                if (sourceProperty.name == "m_Script") continue; // Skip script reference
                
                var targetProperty = targetSerializedObject.FindProperty(sourceProperty.name);
                if (targetProperty != null && targetProperty.propertyType == sourceProperty.propertyType)
                {
                    targetProperty.boxedValue = sourceProperty.boxedValue;
                }
            }
            
            targetSerializedObject.ApplyModifiedProperties();
        }

        private void AddEnhancedAgentToSelected()
        {
            int addedCount = 0;
            
            foreach (var obj in objectsToMigrate)
            {
                if (obj.GetComponent<EnhancedAIAgent>() == null)
                {
                    obj.AddComponent<EnhancedAIAgent>();
                    addedCount++;
                    EditorUtility.SetDirty(obj);
                }
            }
            
            Debug.Log($"Added EnhancedAIAgent to {addedCount} objects");
        }

        private void RemoveOldComponentsFromSelected()
        {
            if (EditorUtility.DisplayDialog("Remove Old Components", 
                "This will remove old AI components from selected objects. Continue?", 
                "Remove", "Cancel"))
            {
                int removedCount = 0;
                
                foreach (var obj in objectsToMigrate)
                {
                    var oldChimera = obj.GetComponent<ChimeraMonsterAI>();
                    var oldEnemy = obj.GetComponent<EnemyController>();
                    
                    if (oldChimera != null)
                    {
                        DestroyImmediate(oldChimera);
                        removedCount++;
                    }
                    
                    if (oldEnemy != null)
                    {
                        DestroyImmediate(oldEnemy);
                        removedCount++;
                    }
                    
                    EditorUtility.SetDirty(obj);
                }
                
                Debug.Log($"Removed {removedCount} old AI components");
                RefreshAIList();
            }
        }
    }

    /// <summary>
    /// Performance monitor for the Enhanced Pathfinding System
    /// </summary>
    public class PathfindingPerformanceMonitor : EditorWindow
    {
        private bool isMonitoring = false;
        private float updateInterval = 1f;
        private float lastUpdateTime;
        
        private int totalAgents;
        private int activePathRequests;
        private int cachedPaths;
        private float avgPathCalculationTime;
        private int pathsCalculatedLastSecond;
        
        [MenuItem("Laboratory/AI Tools/Performance Monitor")]
        public static void ShowWindow()
        {
            GetWindow<PathfindingPerformanceMonitor>("Pathfinding Performance Monitor");
        }

        private void OnGUI()
        {
            GUILayout.Label("Pathfinding Performance Monitor", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            var pathfindingSystem = FindFirstObjectByType<EnhancedPathfindingSystem>();
            if (pathfindingSystem == null)
            {
                EditorGUILayout.HelpBox("Enhanced Pathfinding System not found in scene!", MessageType.Warning);
                return;
            }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(isMonitoring ? "Stop Monitoring" : "Start Monitoring"))
            {
                isMonitoring = !isMonitoring;
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            if (isMonitoring && Application.isPlaying)
            {
                DrawPerformanceStats();
            }
            else if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Performance monitoring only available in Play Mode", MessageType.Info);
            }

            EditorGUILayout.Space();
            DrawSystemInfo(pathfindingSystem);
        }

        private void Update()
        {
            if (isMonitoring && Application.isPlaying && Time.time - lastUpdateTime >= updateInterval)
            {
                UpdatePerformanceStats();
                lastUpdateTime = Time.time;
                Repaint();
            }
        }

        private void UpdatePerformanceStats()
        {
            var pathfindingSystem = FindFirstObjectByType<EnhancedPathfindingSystem>();
            if (pathfindingSystem == null) return;

            // Get stats using reflection (since these might be private fields)
            var type = typeof(EnhancedPathfindingSystem);
            
            try
            {
                var registeredAgentsField = type.GetField("registeredAgents", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var pathRequestQueueField = type.GetField("pathRequestQueue", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var pathCacheField = type.GetField("pathCache", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                if (registeredAgentsField != null)
                {
                    var agents = registeredAgentsField.GetValue(pathfindingSystem) as System.Collections.ICollection;
                    totalAgents = agents?.Count ?? 0;
                }

                if (pathRequestQueueField != null)
                {
                    var queue = pathRequestQueueField.GetValue(pathfindingSystem) as System.Collections.ICollection;
                    activePathRequests = queue?.Count ?? 0;
                }

                if (pathCacheField != null)
                {
                    var cache = pathCacheField.GetValue(pathfindingSystem) as System.Collections.ICollection;
                    cachedPaths = cache?.Count ?? 0;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"Could not access performance stats: {e.Message}");
            }
        }

        private void DrawPerformanceStats()
        {
            EditorGUILayout.LabelField("Performance Statistics", EditorStyles.boldLabel);
            
            EditorGUILayout.LabelField($"Total Registered Agents: {totalAgents}");
            EditorGUILayout.LabelField($"Active Path Requests: {activePathRequests}");
            EditorGUILayout.LabelField($"Cached Paths: {cachedPaths}");
            EditorGUILayout.LabelField($"Paths/Second: {pathsCalculatedLastSecond}");
            
            // Performance indicators
            EditorGUILayout.Space();
            
            Color oldColor = GUI.color;
            
            if (activePathRequests > 10)
            {
                GUI.color = Color.red;
                EditorGUILayout.HelpBox("High path request queue! Consider optimizing AI update frequencies.", MessageType.Warning);
            }
            else if (activePathRequests > 5)
            {
                GUI.color = Color.yellow;
                EditorGUILayout.HelpBox("Moderate path request load.", MessageType.Info);
            }
            else
            {
                GUI.color = Color.green;
                EditorGUILayout.HelpBox("Pathfinding system running smoothly.", MessageType.Info);
            }
            
            GUI.color = oldColor;
        }

        private void DrawSystemInfo(EnhancedPathfindingSystem pathfindingSystem)
        {
            EditorGUILayout.LabelField("System Information", EditorStyles.boldLabel);
            
            EditorGUILayout.ObjectField("Pathfinding System", pathfindingSystem, typeof(EnhancedPathfindingSystem), true);
            
            if (pathfindingSystem != null)
            {
                EditorGUILayout.LabelField($"System Active: {pathfindingSystem.enabled}");
                EditorGUILayout.LabelField($"GameObject: {pathfindingSystem.gameObject.name}");
            }
        }
    }
}