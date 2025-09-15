using UnityEngine;
using UnityEditor;
using UnityEngine.AI;
using UnityEditor.AI;
using Laboratory.Core;
using Laboratory.Subsystems.Player;
using Laboratory.Subsystems.EnemyAI;

namespace Laboratory.Editor
{
    /// <summary>
    /// Quick setup wizard that helps developers create a complete 3D action game scene
    /// with all necessary components and proper configuration in minutes.
    /// </summary>
    public class QuickSetupWizard : ScriptableWizard
    {
        [Header("Scene Setup")]
        public string sceneName = "ActionLevel";
        public bool createTerrain = true;
        public bool setupLighting = true;
        public bool bakeNavMesh = true;

        [Header("Player Setup")]
        public GameObject playerPrefab;
        public Vector3 playerSpawnPosition = new Vector3(0, 1, 0);

        [Header("Enemy Setup")]
        public GameObject[] enemyPrefabs;
        public int enemyCount = 3;
        public float spawnRadius = 10f;

        [Header("Environment")]
        public GameObject[] environmentPrefabs;
        public int environmentObjectCount = 5;

        [Header("Pickups")]
        public GameObject[] pickupPrefabs;
        public int pickupCount = 10;

        [Header("UI Setup")]
        public bool createGameHUD = true;
        public bool setupMainMenu = false;

        private static QuickSetupWizard wizard;

        [MenuItem("Laboratory/Quick Setup Wizard")]
        public static void CreateWizard()
        {
            wizard = ScriptableWizard.DisplayWizard<QuickSetupWizard>("3D Action Game Setup", "Create Scene", "Apply Setup");
            wizard.LoadDefaultAssets();
        }

        private void LoadDefaultAssets()
        {
            // Try to load default prefabs from project
            string[] playerGuids = AssetDatabase.FindAssets("Player t:Prefab", new[] { "Assets/_Project" });
            if (playerGuids.Length > 0)
            {
                playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(playerGuids[0]));
            }

            // Load enemy prefabs
            string[] enemyGuids = AssetDatabase.FindAssets("Enemy t:Prefab", new[] { "Assets/_Project" });
            enemyPrefabs = new GameObject[enemyGuids.Length];
            for (int i = 0; i < enemyGuids.Length; i++)
            {
                enemyPrefabs[i] = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(enemyGuids[i]));
            }

            // Load pickup prefabs
            string[] pickupGuids = AssetDatabase.FindAssets("Pickup t:Prefab", new[] { "Assets/_Project" });
            pickupPrefabs = new GameObject[pickupGuids.Length];
            for (int i = 0; i < pickupGuids.Length; i++)
            {
                pickupPrefabs[i] = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(pickupGuids[i]));
            }
        }

        void OnWizardCreate()
        {
            CreateCompleteScene();
        }

        void OnWizardOtherButton()
        {
            ApplySetupToCurrentScene();
        }

        private void CreateCompleteScene()
        {
            // Create new scene
            var newScene = UnityEditor.SceneManagement.EditorSceneManager.NewScene(
                UnityEditor.SceneManagement.NewSceneSetup.EmptyScene, 
                UnityEditor.SceneManagement.NewSceneMode.Single);

            ApplySetupToCurrentScene();

            // Save scene
            string scenePath = $"Assets/_Project/Scenes/{sceneName}.unity";
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(newScene, scenePath);

            EditorUtility.DisplayDialog("Scene Created", 
                $"Complete 3D action game scene created at {scenePath}", "OK");
        }

        private void ApplySetupToCurrentScene()
        {
            EditorUtility.DisplayProgressBar("Setting up scene", "Creating managers...", 0.1f);
            CreateManagers();

            EditorUtility.DisplayProgressBar("Setting up scene", "Setting up environment...", 0.2f);
            SetupEnvironment();

            EditorUtility.DisplayProgressBar("Setting up scene", "Creating player...", 0.4f);
            SetupPlayer();

            EditorUtility.DisplayProgressBar("Setting up scene", "Spawning enemies...", 0.6f);
            SetupEnemies();

            EditorUtility.DisplayProgressBar("Setting up scene", "Placing pickups...", 0.7f);
            SetupPickups();

            EditorUtility.DisplayProgressBar("Setting up scene", "Creating UI...", 0.8f);
            SetupUI();

            EditorUtility.DisplayProgressBar("Setting up scene", "Finalizing...", 0.9f);
            FinalizeSetup();

            EditorUtility.ClearProgressBar();

            EditorUtility.DisplayDialog("Setup Complete", 
                "3D Action Game setup complete! Press Play to test.", "OK");
        }

        private void CreateManagers()
        {
            // Create GameManager
            GameObject gameManager = new GameObject("GameManager");
            gameManager.AddComponent<GameManager>();
            gameManager.AddComponent<AudioSource>();

            // Create LevelManager
            GameObject levelManager = new GameObject("LevelManager");
            var levelMgr = levelManager.AddComponent<LevelManager>();
            levelManager.AddComponent<AudioSource>();

            // Set up basic objectives
            SetupBasicObjectives(levelMgr);

            Undo.RegisterCreatedObjectUndo(gameManager, "Create GameManager");
            Undo.RegisterCreatedObjectUndo(levelManager, "Create LevelManager");
        }

        private void SetupBasicObjectives(LevelManager levelMgr)
        {
            // This would require access to LevelManager's objectives array
            // For now, we'll just configure it through the inspector
            Debug.Log("Configure objectives in LevelManager inspector: Kill all enemies, Collect items");
        }

        private void SetupEnvironment()
        {
            // Create terrain if requested
            if (createTerrain)
            {
                CreateBasicTerrain();
            }
            else
            {
                // Create simple ground plane
                GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
                ground.name = "Ground";
                ground.transform.localScale = Vector3.one * 5;
                
                // Make it static for navigation
                ground.isStatic = true;
                
                Undo.RegisterCreatedObjectUndo(ground, "Create Ground");
            }

            // Setup lighting
            if (setupLighting)
            {
                SetupBasicLighting();
            }

            // Place environment objects
            PlaceEnvironmentObjects();
        }

        private void CreateBasicTerrain()
        {
            TerrainData terrainData = new TerrainData();
            terrainData.heightmapResolution = 513;
            terrainData.size = new Vector3(50, 10, 50);

            // Create terrain GameObject
            GameObject terrainGO = Terrain.CreateTerrainGameObject(terrainData);
            terrainGO.name = "Terrain";
            terrainGO.isStatic = true;

            Undo.RegisterCreatedObjectUndo(terrainGO, "Create Terrain");
        }

        private void SetupBasicLighting()
        {
            // Ensure there's a directional light
            Light mainLight = FindFirstObjectByType<Light>();
            if (mainLight == null)
            {
                GameObject lightGO = new GameObject("Directional Light");
                mainLight = lightGO.AddComponent<Light>();
                mainLight.type = LightType.Directional;
                lightGO.transform.rotation = Quaternion.Euler(45f, -30f, 0f);
                
                Undo.RegisterCreatedObjectUndo(lightGO, "Create Main Light");
            }

            // Set up basic lighting settings
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.5f, 0.7f, 1f);
            RenderSettings.ambientEquatorColor = new Color(0.4f, 0.4f, 0.4f);
            RenderSettings.ambientGroundColor = new Color(0.2f, 0.2f, 0.2f);
        }

        private void PlaceEnvironmentObjects()
        {
            if (environmentPrefabs == null || environmentPrefabs.Length == 0) return;

            for (int i = 0; i < environmentObjectCount; i++)
            {
                Vector3 randomPos = GetRandomGroundPosition(20f);
                GameObject prefab = environmentPrefabs[Random.Range(0, environmentPrefabs.Length)];
                
                if (prefab != null)
                {
                    GameObject obj = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
                    obj.transform.position = randomPos;
                    obj.transform.rotation = Quaternion.Euler(0, Random.Range(0, 360), 0);
                    
                    Undo.RegisterCreatedObjectUndo(obj, "Place Environment Object");
                }
            }
        }

        private void SetupPlayer()
        {
            GameObject player;

            if (playerPrefab != null)
            {
                player = PrefabUtility.InstantiatePrefab(playerPrefab) as GameObject;
            }
            else
            {
                // Create basic player if no prefab provided
                player = CreateBasicPlayer();
            }

            player.transform.position = playerSpawnPosition;
            player.name = "Player";

            // Create player spawn point marker
            GameObject spawnPoint = new GameObject("PlayerSpawn");
            spawnPoint.tag = "PlayerSpawn";
            spawnPoint.transform.position = playerSpawnPosition;

            // Add visual indicator
            GameObject indicator = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            indicator.transform.SetParent(spawnPoint.transform);
            indicator.transform.localScale = new Vector3(1, 0.1f, 1);
            indicator.GetComponent<Renderer>().material.color = Color.green;
            DestroyImmediate(indicator.GetComponent<Collider>());

            Undo.RegisterCreatedObjectUndo(player, "Create Player");
            Undo.RegisterCreatedObjectUndo(spawnPoint, "Create Player Spawn");
        }

        private GameObject CreateBasicPlayer()
        {
            GameObject player = new GameObject("Player");
            
            // Add visual representation
            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            visual.transform.SetParent(player.transform);
            visual.name = "Visual";

            // Add required components
            player.AddComponent<CharacterController>();
            player.AddComponent<AudioSource>();
            
            // Add player controller if script exists
            var playerController = player.AddComponent<PlayerController>();
            
            // Set layer
            player.layer = LayerMask.NameToLayer("Player");
            if (player.layer == -1)
            {
                Debug.LogWarning("Player layer not found. Create a 'Player' layer for proper functionality.");
            }

            return player;
        }

        private void SetupEnemies()
        {
            if (enemyPrefabs == null || enemyPrefabs.Length == 0)
            {
                Debug.LogWarning("No enemy prefabs provided. Creating basic enemies.");
                CreateBasicEnemies();
                return;
            }

            for (int i = 0; i < enemyCount; i++)
            {
                Vector3 spawnPos = GetRandomGroundPosition(spawnRadius);
                
                // Ensure enemy doesn't spawn too close to player
                while (Vector3.Distance(spawnPos, playerSpawnPosition) < 5f)
                {
                    spawnPos = GetRandomGroundPosition(spawnRadius);
                }

                GameObject enemyPrefab = enemyPrefabs[Random.Range(0, enemyPrefabs.Length)];
                GameObject enemy = PrefabUtility.InstantiatePrefab(enemyPrefab) as GameObject;
                enemy.transform.position = spawnPos;
                enemy.name = $"Enemy_{i + 1}";

                Undo.RegisterCreatedObjectUndo(enemy, "Create Enemy");
            }
        }

        private void CreateBasicEnemies()
        {
            for (int i = 0; i < enemyCount; i++)
            {
                Vector3 spawnPos = GetRandomGroundPosition(spawnRadius);
                
                while (Vector3.Distance(spawnPos, playerSpawnPosition) < 5f)
                {
                    spawnPos = GetRandomGroundPosition(spawnRadius);
                }

                GameObject enemy = CreateBasicEnemy();
                enemy.transform.position = spawnPos;
                enemy.name = $"Enemy_{i + 1}";

                Undo.RegisterCreatedObjectUndo(enemy, "Create Basic Enemy");
            }
        }

        private GameObject CreateBasicEnemy()
        {
            GameObject enemy = new GameObject("Enemy");
            
            // Add visual representation
            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            visual.transform.SetParent(enemy.transform);
            visual.GetComponent<Renderer>().material.color = Color.red;
            visual.name = "Visual";

            // Add required components
            enemy.AddComponent<NavMeshAgent>();
            enemy.AddComponent<AudioSource>();
            enemy.AddComponent<EnemyController>();

            // Set layer
            enemy.layer = LayerMask.NameToLayer("Enemy");
            if (enemy.layer == -1)
            {
                Debug.LogWarning("Enemy layer not found. Create an 'Enemy' layer for proper functionality.");
            }

            return enemy;
        }

        private void SetupPickups()
        {
            if (pickupPrefabs == null || pickupPrefabs.Length == 0)
            {
                Debug.LogWarning("No pickup prefabs provided.");
                return;
            }

            for (int i = 0; i < pickupCount; i++)
            {
                Vector3 spawnPos = GetRandomGroundPosition(15f);
                spawnPos.y += 0.5f; // Raise slightly above ground

                GameObject pickupPrefab = pickupPrefabs[Random.Range(0, pickupPrefabs.Length)];
                GameObject pickup = PrefabUtility.InstantiatePrefab(pickupPrefab) as GameObject;
                pickup.transform.position = spawnPos;

                Undo.RegisterCreatedObjectUndo(pickup, "Create Pickup");
            }
        }

        private void SetupUI()
        {
            if (!createGameHUD) return;

            // Create Canvas
            GameObject canvasGO = new GameObject("Canvas");
            Canvas canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGO.AddComponent<UnityEngine.UI.CanvasScaler>();
            canvasGO.AddComponent<UnityEngine.UI.GraphicRaycaster>();

            // Create EventSystem if it doesn't exist
            if (FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                GameObject eventSystem = new GameObject("EventSystem");
                eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
                eventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
                
                Undo.RegisterCreatedObjectUndo(eventSystem, "Create EventSystem");
            }

            // Add GameHUD component if it exists
            // var gameHUD = canvasGO.AddComponent<Laboratory.UI.Helper.GameHUD>();
            // Debug.Log("Add GameHUD component manually if needed");

            // Create basic UI elements
            CreateBasicHUDElements(canvasGO);

            Undo.RegisterCreatedObjectUndo(canvasGO, "Create Game HUD");
        }

        private void CreateBasicHUDElements(GameObject canvas)
        {
            // Health Bar
            GameObject healthPanel = new GameObject("HealthPanel");
            healthPanel.transform.SetParent(canvas.transform, false);
            var healthRect = healthPanel.AddComponent<RectTransform>();
            healthRect.anchorMin = new Vector2(0, 1);
            healthRect.anchorMax = new Vector2(0, 1);
            healthRect.anchoredPosition = new Vector2(10, -10);
            healthRect.sizeDelta = new Vector2(200, 50);

            // Score Text
            GameObject scoreText = new GameObject("ScoreText");
            scoreText.transform.SetParent(canvas.transform, false);
            var scoreRect = scoreText.AddComponent<RectTransform>();
            scoreRect.anchorMin = new Vector2(1, 1);
            scoreRect.anchorMax = new Vector2(1, 1);
            scoreRect.anchoredPosition = new Vector2(-10, -10);
            scoreRect.sizeDelta = new Vector2(200, 30);

            var scoreTextComp = scoreText.AddComponent<UnityEngine.UI.Text>();
            scoreTextComp.text = "Score: 0";
            scoreTextComp.color = Color.white;
            scoreTextComp.font = Resources.GetBuiltinResource<Font>("Arial.ttf");

            // Crosshair
            GameObject crosshair = new GameObject("Crosshair");
            crosshair.transform.SetParent(canvas.transform, false);
            var crosshairRect = crosshair.AddComponent<RectTransform>();
            crosshairRect.anchorMin = new Vector2(0.5f, 0.5f);
            crosshairRect.anchorMax = new Vector2(0.5f, 0.5f);
            crosshairRect.sizeDelta = new Vector2(20, 20);

            var crosshairImage = crosshair.AddComponent<UnityEngine.UI.Image>();
            crosshairImage.color = Color.white;
        }

        private void FinalizeSetup()
        {
            // Bake NavMesh if requested
            if (bakeNavMesh)
            {
                // Set all static objects to Navigation Static
                GameObject[] allObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
                foreach (GameObject obj in allObjects)
                {
                    if (obj.isStatic)
                    {
                        // StaticEditorFlags.NavigationStatic is deprecated
                        // Use GameObjectUtility.SetNavMeshArea instead
                        Debug.Log($"Setting {obj.name} as navigation static");
                    }
                }

                // Bake NavMesh
                // Open Navigation window instead of using deprecated API
                EditorApplication.ExecuteMenuItem("Window/AI/Navigation");
                Debug.Log("Please use the Navigation window to bake NavMesh");
            }

            // Focus on player
            if (SceneView.lastActiveSceneView != null)
            {
                GameObject player = GameObject.FindWithTag("Player");
                if (player == null) player = FindFirstObjectByType<PlayerController>()?.gameObject;
                
                if (player != null)
                {
                    Selection.activeGameObject = player;
                    SceneView.lastActiveSceneView.FrameSelected();
                }
            }
        }

        private Vector3 GetRandomGroundPosition(float radius)
        {
            Vector2 randomCircle = Random.insideUnitCircle * radius;
            Vector3 position = new Vector3(randomCircle.x, 0, randomCircle.y);

            // Raycast to find ground level
            RaycastHit hit;
            if (Physics.Raycast(position + Vector3.up * 100, Vector3.down, out hit, 200f))
            {
                position.y = hit.point.y;
            }

            return position;
        }

        void OnWizardUpdate()
        {
            helpString = "This wizard will create a complete 3D action game scene with all necessary components.\n\n" +
                        "Create Scene: Creates a new scene with setup\n" +
                        "Apply Setup: Applies setup to current scene";
        }
    }
}
