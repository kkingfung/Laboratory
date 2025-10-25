using UnityEngine;
using Unity.Entities;
using Unity.Collections;
using Unity.Mathematics;
using Laboratory.Core.ECS;
using System.Collections.Generic;
using System.Linq;

namespace Laboratory.Core.Network
{
    /// <summary>
    /// Advanced network synchronization debugger that validates multiplayer state consistency
    /// and provides real-time desync detection for Project Chimera's breeding simulation.
    /// Essential for maintaining genetic integrity across clients in multiplayer sessions.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class NetworkSynchronizationDebugger : MonoBehaviour
    {
        [Header("🌐 Network Debug Settings")]
        [SerializeField] private bool enableNetworkDebugging = true;
        [SerializeField] private KeyCode debugToggleKey = KeyCode.F10;
        [SerializeField] private bool autoDetectDesyncs = true;
        [SerializeField] private float desyncCheckInterval = 1f;

        [Header("🔍 Validation Settings")]
        [SerializeField] private bool validateCreatureStates = true;
        [SerializeField] private bool validateGeneticData = true;
        [SerializeField] private bool validateBreedingOperations = true;
        [SerializeField] private bool validateAIBehaviors = false; // Too noisy by default

        [Header("📊 Performance Monitoring")]
        [SerializeField] private bool trackNetworkPerformance = true;
        [SerializeField] private int maxValidatedEntities = 100;
        [SerializeField] private bool showDetailedLogs = false;

        [Header("🚨 Alert Settings")]
        [SerializeField] private Color desyncAlertColor = Color.red;
        [SerializeField] private float alertDisplayDuration = 5f;
        [SerializeField] private bool pauseOnCriticalDesync = false;

        // Network state tracking
        private Dictionary<Entity, NetworkEntityState> networkStates = new Dictionary<Entity, NetworkEntityState>();
        private Dictionary<uint, BreedingOperationState> breedingOperations = new Dictionary<uint, BreedingOperationState>();

        // Performance metrics
        private NetworkPerformanceMetrics performanceMetrics = new NetworkPerformanceMetrics();
        private float lastDesyncCheckTime;

        // UI state
        private bool debugWindowVisible = false;
        private Vector2 scrollPosition = Vector2.zero;
        private GUIStyle alertStyle;
        private GUIStyle headerStyle;
        private GUIStyle boxStyle;

        // Alert system
        private List<NetworkAlert> activeAlerts = new List<NetworkAlert>();

        // System references
        private EntityManager entityManager;
        private bool isServer;
        private bool isClient;

        private struct NetworkEntityState
        {
            public uint networkId;
            public float3 lastKnownPosition;
            public CreatureData creatureData;
            public CreatureStats stats;
            public uint geneticHash;
            public float lastUpdateTime;
            public int validationFailures;
        }

        private struct BreedingOperationState
        {
            public Entity parent1;
            public Entity parent2;
            public Entity offspring;
            public uint geneticSeed;
            public float operationTime;
            public bool serverValidated;
            public bool clientConfirmed;
        }

        public struct NetworkPerformanceMetrics
        {
            public int entitiesValidated;
            public int desyncDetections;
            public int breedingValidations;
            public float averageValidationTime;
            public int networkMessagesPerSecond;
            public float lastResetTime;
        }

        private struct NetworkAlert
        {
            public string message;
            public NetworkAlertType type;
            public float timestamp;
            public Entity relatedEntity;
        }

        private enum NetworkAlertType
        {
            Info,
            Warning,
            Critical,
            Desync
        }

        private void Start()
        {
            InitializeNetworkDebugger();

            if (enableNetworkDebugging)
            {
                InvokeRepeating(nameof(ValidateNetworkState), desyncCheckInterval, desyncCheckInterval);
            }
        }

        private void InitializeNetworkDebugger()
        {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world?.IsCreated != true)
            {
                Debug.LogError("NetworkSynchronizationDebugger: No ECS world found!");
                return;
            }

            entityManager = world.EntityManager;

            // Detect network role using available Unity networking solutions
            DetectNetworkRole();

            performanceMetrics.lastResetTime = Time.time;

            if (showDetailedLogs)
                Debug.Log($"🌐 Network Synchronization Debugger initialized - Server: {isServer}, Client: {isClient}");
        }

        private void Update()
        {
            if (!enableNetworkDebugging) return;

            // Toggle debug window
            if (Input.GetKeyDown(debugToggleKey))
            {
                debugWindowVisible = !debugWindowVisible;
            }

            // Update performance metrics
            if (trackNetworkPerformance)
            {
                UpdatePerformanceMetrics();
            }

            // Clean up old alerts
            activeAlerts.RemoveAll(alert => Time.time - alert.timestamp > alertDisplayDuration);

            // Auto-validation if enabled
            if (autoDetectDesyncs && Time.time - lastDesyncCheckTime > desyncCheckInterval)
            {
                ValidateNetworkState();
                lastDesyncCheckTime = Time.time;
            }
        }

        private void ValidateNetworkState()
        {
            if (!entityManager.World.IsCreated) return;

            var validationStartTime = Time.realtimeSinceStartup;
            int validatedCount = 0;

            // Validate creature entities
            if (validateCreatureStates)
            {
                validatedCount += ValidateCreatureEntities();
            }

            // Validate breeding operations
            if (validateBreedingOperations)
            {
                validatedCount += ValidateBreedingOperations();
            }

            // Update performance metrics
            var validationTime = Time.realtimeSinceStartup - validationStartTime;
            performanceMetrics.entitiesValidated += validatedCount;
            performanceMetrics.averageValidationTime =
                (performanceMetrics.averageValidationTime + validationTime) * 0.5f;

            if (showDetailedLogs && validatedCount > 0)
            {
                Debug.Log($"🌐 Validated {validatedCount} network entities in {validationTime:F3}ms");
            }
        }

        private int ValidateCreatureEntities()
        {
            var query = entityManager.CreateEntityQuery(
                ComponentType.ReadOnly<CreatureData>(),
                ComponentType.ReadOnly<CreatureStats>(),
                ComponentType.ReadOnly<Unity.Transforms.LocalTransform>()
            );

            using (var entities = query.ToEntityArray(Allocator.TempJob))
            using (var creatureData = query.ToComponentDataArray<CreatureData>(Allocator.TempJob))
            using (var stats = query.ToComponentDataArray<CreatureStats>(Allocator.TempJob))
            using (var transforms = query.ToComponentDataArray<Unity.Transforms.LocalTransform>(Allocator.TempJob))
            {
                int validatedCount = 0;
                int entitiesToValidate = math.min(entities.Length, maxValidatedEntities);

                for (int i = 0; i < entitiesToValidate; i++)
                {
                    var entity = entities[i];
                    var data = creatureData[i];
                    var stat = stats[i];
                    var transform = transforms[i];

                    // Create or update network state
                    var networkState = new NetworkEntityState
                    {
                        networkId = (uint)entity.Index, // Use entity index as fallback network ID
                        lastKnownPosition = transform.Position,
                        creatureData = data,
                        stats = stat,
                        geneticHash = CalculateGeneticHash(data),
                        lastUpdateTime = Time.time,
                        validationFailures = networkStates.ContainsKey(entity) ?
                            networkStates[entity].validationFailures : 0
                    };

                    // Check for desyncs
                    if (networkStates.TryGetValue(entity, out var previousState))
                    {
                        ValidateEntityConsistency(entity, previousState, networkState);
                    }

                    networkStates[entity] = networkState;
                    validatedCount++;
                }

                query.Dispose();
                return validatedCount;
            }
        }

        private void ValidateEntityConsistency(Entity entity, NetworkEntityState previous, NetworkEntityState current)
        {
            bool hasIssue = false;
            var issues = new List<string>();

            // Position validation (allow for minor differences due to interpolation)
            var positionDifference = math.distance(previous.lastKnownPosition, current.lastKnownPosition);
            if (positionDifference > 50f) // Teleportation threshold
            {
                issues.Add($"Large position jump: {positionDifference:F2}m");
                hasIssue = true;
            }

            // Genetic data validation (should never change)
            if (validateGeneticData && previous.geneticHash != current.geneticHash)
            {
                issues.Add("Genetic data mismatch - critical desync!");
                hasIssue = true;
                AddAlert($"Critical genetic desync on Entity_{entity.Index}", NetworkAlertType.Critical, entity);
            }

            // Stats validation (check for impossible changes)
            var statsDifference = math.abs(previous.stats.health - current.stats.health);
            if (statsDifference > previous.stats.maxHealth) // Impossible health change
            {
                issues.Add($"Impossible health change: {statsDifference}");
                hasIssue = true;
            }

            // Age validation (should only increase)
            if (current.creatureData.age < previous.creatureData.age)
            {
                issues.Add("Creature age decreased - time desync");
                hasIssue = true;
            }

            if (hasIssue)
            {
                var failureCount = networkStates[entity].validationFailures + 1;
                var newState = current;
                newState.validationFailures = failureCount;
                networkStates[entity] = newState;

                performanceMetrics.desyncDetections++;

                var alertType = failureCount > 3 ? NetworkAlertType.Critical : NetworkAlertType.Warning;
                var message = $"Entity_{entity.Index}: {string.Join(", ", issues)}";
                AddAlert(message, alertType, entity);

                if (showDetailedLogs)
                {
                    Debug.LogWarning($"🌐 Network validation failed for Entity_{entity.Index}: {string.Join(", ", issues)}");
                }

                // Pause on critical desyncs if enabled
                if (pauseOnCriticalDesync && alertType == NetworkAlertType.Critical)
                {
                    Debug.Break();
                }
            }
        }

        private int ValidateBreedingOperations()
        {
            // This would validate ongoing breeding operations
            // For now, we'll simulate some basic validation

            var validatedOperations = 0;
            var operationsToRemove = new List<uint>();

            foreach (var kvp in breedingOperations.ToArray())
            {
                var operationId = kvp.Key;
                var operation = kvp.Value;

                // Check if operation is too old
                if (Time.time - operation.operationTime > 30f) // 30 second timeout
                {
                    operationsToRemove.Add(operationId);
                    continue;
                }

                // Validate that parents still exist
                if (!entityManager.Exists(operation.parent1) || !entityManager.Exists(operation.parent2))
                {
                    AddAlert($"Breeding operation {operationId}: Parent entities no longer exist",
                        NetworkAlertType.Warning, Entity.Null);
                    operationsToRemove.Add(operationId);
                    continue;
                }

                validatedOperations++;
            }

            // Clean up old operations
            foreach (var id in operationsToRemove)
            {
                breedingOperations.Remove(id);
            }

            performanceMetrics.breedingValidations += validatedOperations;
            return validatedOperations;
        }

        private uint CalculateGeneticHash(CreatureData data)
        {
            // Simple hash based on genetic seed and species
            return (uint)(data.geneticSeed ^ (data.speciesID << 16));
        }

        private void UpdatePerformanceMetrics()
        {
            // Reset metrics every 10 seconds
            if (Time.time - performanceMetrics.lastResetTime > 10f)
            {
                performanceMetrics.networkMessagesPerSecond = 0; // Would be tracked by network system
                performanceMetrics.lastResetTime = Time.time;
            }
        }

        private void AddAlert(string message, NetworkAlertType type, Entity relatedEntity)
        {
            var alert = new NetworkAlert
            {
                message = message,
                type = type,
                timestamp = Time.time,
                relatedEntity = relatedEntity
            };

            activeAlerts.Add(alert);

            // Limit active alerts
            if (activeAlerts.Count > 50)
            {
                activeAlerts.RemoveAt(0);
            }
        }

        /// <summary>
        /// Register a breeding operation for validation
        /// </summary>
        public void RegisterBreedingOperation(uint operationId, Entity parent1, Entity parent2, uint geneticSeed)
        {
            var operation = new BreedingOperationState
            {
                parent1 = parent1,
                parent2 = parent2,
                offspring = Entity.Null,
                geneticSeed = geneticSeed,
                operationTime = Time.time,
                serverValidated = isServer,
                clientConfirmed = false
            };

            breedingOperations[operationId] = operation;

            AddAlert($"Breeding operation {operationId} registered", NetworkAlertType.Info, Entity.Null);
        }

        /// <summary>
        /// Confirm breeding operation completion
        /// </summary>
        public void ConfirmBreedingOperation(uint operationId, Entity offspring)
        {
            if (breedingOperations.TryGetValue(operationId, out var operation))
            {
                operation.offspring = offspring;
                operation.clientConfirmed = true;
                breedingOperations[operationId] = operation;

                AddAlert($"Breeding operation {operationId} completed", NetworkAlertType.Info, offspring);
            }
        }

        private void OnGUI()
        {
            if (!enableNetworkDebugging || !debugWindowVisible) return;

            InitializeGUIStyles();
            DrawDebugWindow();
            DrawActiveAlerts();
        }

        private void InitializeGUIStyles()
        {
            if (alertStyle == null)
            {
                alertStyle = new GUIStyle(GUI.skin.box)
                {
                    normal = { textColor = Color.white },
                    fontSize = 12,
                    padding = new RectOffset(10, 10, 5, 5)
                };
            }

            if (headerStyle == null)
            {
                headerStyle = new GUIStyle(GUI.skin.label)
                {
                    fontStyle = FontStyle.Bold,
                    fontSize = 14,
                    normal = { textColor = Color.white }
                };
            }

            if (boxStyle == null)
            {
                boxStyle = new GUIStyle(GUI.skin.box)
                {
                    padding = new RectOffset(10, 10, 10, 10),
                    margin = new RectOffset(5, 5, 5, 5)
                };
            }
        }

        private void DrawDebugWindow()
        {
            float windowWidth = 450f;
            float windowHeight = Screen.height * 0.8f;
            Rect windowRect = new Rect(10, 10, windowWidth, windowHeight);

            GUILayout.BeginArea(windowRect, boxStyle);

            // Header
            GUILayout.Label("🌐 Network Synchronization Debugger", headerStyle);
            GUILayout.Space(10);

            // Network role info
            GUILayout.Label($"Role: {(isServer ? "Server" : "")} {(isClient ? "Client" : "")}");
            GUILayout.Label($"Entities Tracked: {networkStates.Count}");
            GUILayout.Label($"Breeding Operations: {breedingOperations.Count}");
            GUILayout.Space(10);

            // Performance metrics
            DrawPerformanceMetrics();
            GUILayout.Space(10);

            // Settings
            DrawSettings();
            GUILayout.Space(10);

            // Entity list
            DrawEntityList();

            GUILayout.EndArea();
        }

        private void DrawPerformanceMetrics()
        {
            GUILayout.Label("Performance Metrics:", headerStyle);

            GUILayout.BeginVertical(boxStyle);
            GUILayout.Label($"Entities Validated: {performanceMetrics.entitiesValidated}");
            GUILayout.Label($"Desync Detections: {performanceMetrics.desyncDetections}");
            GUILayout.Label($"Breeding Validations: {performanceMetrics.breedingValidations}");
            GUILayout.Label($"Avg Validation Time: {performanceMetrics.averageValidationTime * 1000:F2}ms");
            GUILayout.Label($"Network Messages/sec: {performanceMetrics.networkMessagesPerSecond}");
            GUILayout.EndVertical();
        }

        private void DrawSettings()
        {
            GUILayout.Label("Settings:", headerStyle);

            GUILayout.BeginVertical(boxStyle);
            autoDetectDesyncs = GUILayout.Toggle(autoDetectDesyncs, "Auto Detect Desyncs");
            validateCreatureStates = GUILayout.Toggle(validateCreatureStates, "Validate Creature States");
            validateGeneticData = GUILayout.Toggle(validateGeneticData, "Validate Genetic Data");
            validateBreedingOperations = GUILayout.Toggle(validateBreedingOperations, "Validate Breeding Operations");
            showDetailedLogs = GUILayout.Toggle(showDetailedLogs, "Detailed Console Logs");

            GUILayout.BeginHorizontal();
            GUILayout.Label("Check Interval:", GUILayout.Width(100));
            var newInterval = GUILayout.HorizontalSlider(desyncCheckInterval, 0.1f, 5f);
            GUILayout.Label($"{newInterval:F1}s", GUILayout.Width(40));
            GUILayout.EndHorizontal();

            if (math.abs(newInterval - desyncCheckInterval) > 0.01f)
            {
                desyncCheckInterval = newInterval;
                CancelInvoke(nameof(ValidateNetworkState));
                InvokeRepeating(nameof(ValidateNetworkState), desyncCheckInterval, desyncCheckInterval);
            }
            GUILayout.EndVertical();
        }

        private void DrawEntityList()
        {
            GUILayout.Label("Network Entities:", headerStyle);

            scrollPosition = GUILayout.BeginScrollView(scrollPosition, GUILayout.Height(200));

            foreach (var kvp in networkStates.Take(20)) // Limit display for performance
            {
                var entity = kvp.Key;
                var state = kvp.Value;

                var statusColor = state.validationFailures == 0 ? Color.green :
                                state.validationFailures > 3 ? Color.red : Color.yellow;

                var originalColor = GUI.backgroundColor;
                GUI.backgroundColor = statusColor;

                var displayName = $"Entity_{entity.Index} (ID: {state.networkId})";
                if (GUILayout.Button(displayName, GUILayout.Height(25)))
                {
                    // Focus on entity in scene view
                    if (showDetailedLogs)
                        Debug.Log($"🌐 Focused on {displayName} - Failures: {state.validationFailures}");
                }

                GUI.backgroundColor = originalColor;
            }

            if (networkStates.Count > 20)
            {
                GUILayout.Label($"... and {networkStates.Count - 20} more entities");
            }

            GUILayout.EndScrollView();
        }

        private void DrawActiveAlerts()
        {
            // Draw alerts as overlay
            float alertY = Screen.height - 200f;

            foreach (var alert in activeAlerts.TakeLast(5)) // Show last 5 alerts
            {
                var alertColor = alert.type switch
                {
                    NetworkAlertType.Info => Color.blue,
                    NetworkAlertType.Warning => Color.yellow,
                    NetworkAlertType.Critical => Color.red,
                    NetworkAlertType.Desync => desyncAlertColor,
                    _ => Color.white
                };

                var originalColor = GUI.backgroundColor;
                GUI.backgroundColor = alertColor;

                var timeAgo = Time.time - alert.timestamp;
                var alertText = $"[{alert.type}] {alert.message} ({timeAgo:F1}s ago)";

                GUILayout.BeginArea(new Rect(Screen.width - 400f, alertY, 390f, 30f), alertStyle);
                GUILayout.Label(alertText);
                GUILayout.EndArea();

                GUI.backgroundColor = originalColor;
                alertY -= 35f;
            }
        }

        private void OnDestroy()
        {
            CancelInvoke();
        }

        /// <summary>
        /// Detect network role based on available networking solutions
        /// </summary>
        private void DetectNetworkRole()
        {
            // Reset to defaults
            isClient = false;
            isServer = false;

            // Check for Unity Netcode for GameObjects
            var netcodeNetworkManager = FindObjectOfType<Unity.Netcode.NetworkManager>();
            if (netcodeNetworkManager != null)
            {
                isServer = netcodeNetworkManager.IsServer;
                isClient = netcodeNetworkManager.IsClient;
                if (showDetailedLogs)
                    Debug.Log($"🌐 Detected Unity Netcode - Server: {isServer}, Client: {isClient}");
                return;
            }

            // Check for Mirror Networking (if available)
            var mirrorNetworkManager = GameObject.FindObjectOfType<MonoBehaviour>();
            if (mirrorNetworkManager != null && mirrorNetworkManager.GetType().Name.Contains("NetworkManager"))
            {
                // Use reflection to safely check Mirror's NetworkManager
                var type = mirrorNetworkManager.GetType();
                var isServerProperty = type.GetProperty("isServer");
                var isClientProperty = type.GetProperty("isClient");

                if (isServerProperty != null && isClientProperty != null)
                {
                    isServer = (bool)isServerProperty.GetValue(mirrorNetworkManager);
                    isClient = (bool)isClientProperty.GetValue(mirrorNetworkManager);
                    if (showDetailedLogs)
                        Debug.Log($"🌐 Detected Mirror Networking - Server: {isServer}, Client: {isClient}");
                    return;
                }
            }

            // Check for custom network implementations
            var customNetworkComponents = FindObjectsOfType<MonoBehaviour>();
            foreach (var component in customNetworkComponents)
            {
                var typeName = component.GetType().Name.ToLower();
                if (typeName.Contains("network") && (typeName.Contains("manager") || typeName.Contains("controller")))
                {
                    // Try to detect server/client status from common property names
                    var type = component.GetType();
                    var serverProp = type.GetProperty("IsServer") ?? type.GetProperty("isServer") ?? type.GetProperty("IsHost");
                    var clientProp = type.GetProperty("IsClient") ?? type.GetProperty("isClient");

                    if (serverProp != null)
                    {
                        try
                        {
                            isServer = (bool)serverProp.GetValue(component);
                        }
                        catch { /* Ignore if property access fails */ }
                    }

                    if (clientProp != null)
                    {
                        try
                        {
                            isClient = (bool)clientProp.GetValue(component);
                        }
                        catch { /* Ignore if property access fails */ }
                    }

                    if (isServer || isClient)
                    {
                        if (showDetailedLogs)
                            Debug.Log($"🌐 Detected custom networking ({typeName}) - Server: {isServer}, Client: {isClient}");
                        return;
                    }
                }
            }

            // Default to standalone mode
            if (showDetailedLogs)
                Debug.Log("🌐 No networking solution detected - running in standalone mode");
        }

        /// <summary>
        /// Get current network synchronization statistics
        /// </summary>
        public NetworkPerformanceMetrics GetPerformanceMetrics()
        {
            return performanceMetrics;
        }

        /// <summary>
        /// Force immediate validation of all network entities
        /// </summary>
        [ContextMenu("Force Network Validation")]
        public void ForceValidation()
        {
            ValidateNetworkState();
            Debug.Log("🌐 Network validation completed manually");
        }

        /// <summary>
        /// Clear all tracked network states (use after scene transition)
        /// </summary>
        public void ClearNetworkStates()
        {
            networkStates.Clear();
            breedingOperations.Clear();
            activeAlerts.Clear();

            Debug.Log("🌐 Network state tracking cleared");
        }
    }
}