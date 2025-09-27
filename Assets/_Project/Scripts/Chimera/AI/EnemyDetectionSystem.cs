using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace Laboratory.Chimera.AI
{
    /// <summary>
    /// Advanced enemy detection system for Chimera monsters.
    /// Provides vision, hearing, and smell-based detection capabilities.
    /// Integrates with ChimeraMonsterAI for enhanced awareness.
    /// </summary>
    public class EnemyDetectionSystem : MonoBehaviour
    {
        [Header("Detection Capabilities")]
        [SerializeField] private float visionRange = 15f;
        [SerializeField] private float visionAngle = 90f; // Field of view in degrees
        [SerializeField] private bool enableHearing = true;
        [SerializeField] private float hearingRange = 10f;
        [SerializeField] private bool enableSmell = false;
        [SerializeField] private float smellRange = 8f;

        [Header("Detection Layers")]
        [SerializeField] private LayerMask enemyLayers = 1 << 8;
        [SerializeField] private LayerMask obstacleLayers = 1 << 0;

        [Header("Detection Settings")]
        [SerializeField] private float detectionUpdateInterval = 0.2f;
        [SerializeField] private bool requireLineOfSight = true;
        [SerializeField] private bool enablePrediction = true;

        [Header("Debug")]
        [SerializeField] private bool showDetectionGizmos = false;
        [SerializeField] private Color visionColor = Color.yellow;
        [SerializeField] private Color hearingColor = Color.blue;
        [SerializeField] private Color smellColor = Color.green;

        // Detection data
        private List<DetectedEnemy> detectedEnemies = new List<DetectedEnemy>();
        private float lastDetectionUpdate = 0f;
        private ChimeraMonsterAI connectedAI;

        // Events
        public System.Action<Transform> OnEnemyDetected;
        public System.Action<Transform> OnEnemyLost;
        public System.Action<Transform> OnEnemySuspected;

        #region Properties

        public IReadOnlyList<DetectedEnemy> DetectedEnemies => detectedEnemies;
        public int EnemyCount => detectedEnemies.Count;
        public bool HasEnemiesDetected => detectedEnemies.Count > 0;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            connectedAI = GetComponent<ChimeraMonsterAI>();
        }

        private void Update()
        {
            if (Time.time - lastDetectionUpdate >= detectionUpdateInterval)
            {
                UpdateDetection();
                lastDetectionUpdate = Time.time;
            }
        }

        #endregion

        #region Detection System

        private void UpdateDetection()
        {
            // Clear old detections
            var previousEnemies = detectedEnemies.ToList();
            detectedEnemies.Clear();

            // Vision-based detection
            if (visionRange > 0f)
            {
                DetectByVision();
            }

            // Hearing-based detection
            if (enableHearing && hearingRange > 0f)
            {
                DetectByHearing();
            }

            // Smell-based detection
            if (enableSmell && smellRange > 0f)
            {
                DetectBySmell();
            }

            // Process detection changes
            ProcessDetectionChanges(previousEnemies);
        }

        private void DetectByVision()
        {
            var nearbyColliders = Physics.OverlapSphere(transform.position, visionRange, enemyLayers);

            foreach (var collider in nearbyColliders)
            {
                if (collider.transform == transform) continue;

                var enemy = collider.transform;
                var directionToEnemy = enemy.position - transform.position;
                
                // Check if enemy is within field of view
                if (IsWithinFieldOfView(directionToEnemy))
                {
                    // Check line of sight
                    if (!requireLineOfSight || HasLineOfSight(enemy))
                    {
                        AddDetection(enemy, DetectionType.Vision, 1f);
                    }
                }
            }
        }

        private void DetectByHearing()
        {
            var nearbyColliders = Physics.OverlapSphere(transform.position, hearingRange, enemyLayers);

            foreach (var collider in nearbyColliders)
            {
                if (collider.transform == transform) continue;

                var enemy = collider.transform;
                var distance = Vector3.Distance(transform.position, enemy.position);
                
                // Check if enemy is making noise (moving)
                var enemyRigidbody = enemy.GetComponent<Rigidbody>();
                bool isMoving = enemyRigidbody != null && enemyRigidbody.linearVelocity.magnitude > 0.5f;
                
                if (isMoving)
                {
                    // Hearing confidence decreases with distance
                    float confidence = 1f - (distance / hearingRange);
                    AddDetection(enemy, DetectionType.Hearing, confidence);
                }
            }
        }

        private void DetectBySmell()
        {
            var nearbyColliders = Physics.OverlapSphere(transform.position, smellRange, enemyLayers);

            foreach (var collider in nearbyColliders)
            {
                if (collider.transform == transform) continue;

                var enemy = collider.transform;
                var distance = Vector3.Distance(transform.position, enemy.position);
                
                // Smell works better at close range
                float confidence = 1f - (distance / smellRange);
                confidence = Mathf.Pow(confidence, 2f); // Quadratic falloff for smell
                
                AddDetection(enemy, DetectionType.Smell, confidence);
            }
        }

        private bool IsWithinFieldOfView(Vector3 directionToEnemy)
        {
            var angle = Vector3.Angle(transform.forward, directionToEnemy);
            return angle < visionAngle / 2f;
        }

        private bool HasLineOfSight(Transform enemy)
        {
            var directionToEnemy = enemy.position - transform.position;
            var distance = directionToEnemy.magnitude;

            // Cast ray from eye level
            var rayOrigin = transform.position + Vector3.up * 1.6f;
            var enemyCenter = enemy.position + Vector3.up * 1f;
            var rayDirection = (enemyCenter - rayOrigin).normalized;

            if (Physics.Raycast(rayOrigin, rayDirection, out RaycastHit hit, distance, obstacleLayers))
            {
                return hit.collider.transform == enemy;
            }

            return true; // No obstacles found
        }

        private void AddDetection(Transform enemy, DetectionType type, float confidence)
        {
            var existing = detectedEnemies.FirstOrDefault(d => d.Enemy == enemy);
            if (existing != null)
            {
                // Update existing detection with highest confidence
                if (confidence > existing.Confidence)
                {
                    existing.UpdateDetection(type, confidence);
                }
            }
            else
            {
                // Add new detection
                var detection = new DetectedEnemy(enemy, type, confidence);
                detectedEnemies.Add(detection);
            }
        }

        private void ProcessDetectionChanges(List<DetectedEnemy> previousEnemies)
        {
            // Find new detections
            foreach (var detection in detectedEnemies)
            {
                if (!previousEnemies.Any(p => p.Enemy == detection.Enemy))
                {
                    OnEnemyDetected?.Invoke(detection.Enemy);
                    UnityEngine.Debug.Log($"[Detection] New enemy detected: {detection.Enemy.name} via {detection.Type}");
                }
            }

            // Find lost detections
            foreach (var prevDetection in previousEnemies)
            {
                if (!detectedEnemies.Any(d => d.Enemy == prevDetection.Enemy))
                {
                    OnEnemyLost?.Invoke(prevDetection.Enemy);
                    UnityEngine.Debug.Log($"[Detection] Enemy lost: {prevDetection.Enemy?.name ?? "Unknown"}");
                }
            }

            // Apply predictive detection if enabled
            if (enablePrediction)
            {
                ApplyPredictiveDetection();
            }

            // Update AI with current detections
            if (connectedAI != null)
            {
                // This would integrate with the AI system
                // For now, we'll just log the count
                if (detectedEnemies.Count > 0)
                {
                    UnityEngine.Debug.Log($"[Detection] Reporting {detectedEnemies.Count} enemies to AI");
                }
            }
        }

        private void ApplyPredictiveDetection()
        {
            // Predictive detection tries to anticipate where enemies might be
            // based on their last known positions and movement patterns
            foreach (var detection in detectedEnemies.ToList())
            {
                if (detection.Enemy == null) continue;

                // Try to predict enemy movement
                var enemyRigidbody = detection.Enemy.GetComponent<Rigidbody>();
                if (enemyRigidbody != null && enemyRigidbody.linearVelocity.magnitude > 0.1f)
                {
                    // Predict where the enemy will be in the next few seconds
                    var predictedPosition = detection.Enemy.position + enemyRigidbody.linearVelocity * 2f;
                    var distanceToPredicted = Vector3.Distance(transform.position, predictedPosition);

                    // If predicted position is within detection range, increase confidence
                    if (distanceToPredicted <= visionRange * 1.2f)
                    {
                        detection.UpdateDetection(DetectionType.Combined, 
                            Mathf.Min(1f, detection.Confidence + 0.1f));
                        
                        // Trigger suspicion event for predicted locations
                        OnEnemySuspected?.Invoke(detection.Enemy);
                    }
                }

                // Check for patterns in enemy behavior
                if (detection.TimeSinceDetection > 1f && detection.TimeSinceDetection < 3f)
                {
                    // Recently lost enemies might be hiding - increase awareness
                    var nearbyPosition = detection.LastKnownPosition;
                    var searchRadius = 5f;
                    
                    if (Vector3.Distance(transform.position, nearbyPosition) <= searchRadius)
                    {
                        // We're near where we last saw the enemy - stay alert
                        OnEnemySuspected?.Invoke(detection.Enemy);
                    }
                }
            }
        }

        #endregion

        #region Public API

        /// <summary>
        /// Get the closest detected enemy
        /// </summary>
        public Transform GetClosestEnemy()
        {
            if (detectedEnemies.Count == 0) return null;

            Transform closest = null;
            float closestDistance = float.MaxValue;

            foreach (var detection in detectedEnemies)
            {
                if (detection.Enemy == null) continue;

                float distance = Vector3.Distance(transform.position, detection.Enemy.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closest = detection.Enemy;
                }
            }

            return closest;
        }

        /// <summary>
        /// Get the most confident detection
        /// </summary>
        public DetectedEnemy GetMostConfidentDetection()
        {
            if (detectedEnemies.Count == 0) return null;

            return detectedEnemies.OrderByDescending(d => d.Confidence).First();
        }

        /// <summary>
        /// Check if a specific enemy is detected
        /// </summary>
        public bool IsEnemyDetected(Transform enemy)
        {
            return detectedEnemies.Any(d => d.Enemy == enemy);
        }

        /// <summary>
        /// Get detection info for a specific enemy
        /// </summary>
        public DetectedEnemy GetDetectionInfo(Transform enemy)
        {
            return detectedEnemies.FirstOrDefault(d => d.Enemy == enemy);
        }

        /// <summary>
        /// Force immediate detection update
        /// </summary>
        public void ForceDetectionUpdate()
        {
            UpdateDetection();
        }

        #endregion

        #region Debug & Gizmos

        private void OnDrawGizmosSelected()
        {
            if (!showDetectionGizmos) return;

            // Draw vision cone
            if (visionRange > 0f)
            {
                Gizmos.color = visionColor;
                DrawVisionCone();
            }

            // Draw hearing range
            if (enableHearing && hearingRange > 0f)
            {
                Gizmos.color = hearingColor;
                Gizmos.DrawWireSphere(transform.position, hearingRange);
            }

            // Draw smell range
            if (enableSmell && smellRange > 0f)
            {
                Gizmos.color = smellColor;
                Gizmos.DrawWireSphere(transform.position, smellRange);
            }

            // Draw detected enemies
            Gizmos.color = Color.red;
            foreach (var detection in detectedEnemies)
            {
                if (detection.Enemy != null)
                {
                    Gizmos.DrawLine(transform.position, detection.Enemy.position);
                    Gizmos.DrawWireCube(detection.Enemy.position + Vector3.up * 2f, Vector3.one * 0.5f);
                }
            }
        }

        private void DrawVisionCone()
        {
            var leftBoundary = Quaternion.Euler(0, -visionAngle / 2f, 0) * transform.forward * visionRange;
            var rightBoundary = Quaternion.Euler(0, visionAngle / 2f, 0) * transform.forward * visionRange;

            // Draw vision cone lines
            Gizmos.DrawRay(transform.position, leftBoundary);
            Gizmos.DrawRay(transform.position, rightBoundary);
            Gizmos.DrawRay(transform.position, transform.forward * visionRange);

            // Draw arc (simplified)
            var steps = 10;
            var stepAngle = visionAngle / steps;
            var startAngle = -visionAngle / 2f;

            for (int i = 0; i < steps; i++)
            {
                var angle1 = startAngle + stepAngle * i;
                var angle2 = startAngle + stepAngle * (i + 1);

                var dir1 = Quaternion.Euler(0, angle1, 0) * transform.forward * visionRange;
                var dir2 = Quaternion.Euler(0, angle2, 0) * transform.forward * visionRange;

                Gizmos.DrawLine(transform.position + dir1, transform.position + dir2);
            }
        }

        #endregion
    }

    #region Supporting Classes

    /// <summary>
    /// Represents a detected enemy with detection metadata
    /// </summary>
    [System.Serializable]
    public class DetectedEnemy
    {
        public Transform Enemy { get; private set; }
        public DetectionType Type { get; private set; }
        public float Confidence { get; private set; }
        public float DetectionTime { get; private set; }
        public Vector3 LastKnownPosition { get; private set; }

        public DetectedEnemy(Transform enemy, DetectionType type, float confidence)
        {
            Enemy = enemy;
            Type = type;
            Confidence = confidence;
            DetectionTime = Time.time;
            LastKnownPosition = enemy.position;
        }

        public void UpdateDetection(DetectionType newType, float newConfidence)
        {
            if (newConfidence > Confidence)
            {
                Type = newType;
                Confidence = newConfidence;
            }

            if (Enemy != null)
            {
                LastKnownPosition = Enemy.position;
            }
        }

        public float TimeSinceDetection => Time.time - DetectionTime;

        public bool IsStale => TimeSinceDetection > 5f; // 5 seconds

        public override string ToString()
        {
            return $"Enemy: {Enemy?.name ?? "Unknown"}, Type: {Type}, Confidence: {Confidence:F2}, Age: {TimeSinceDetection:F1}s";
        }
    }

    /// <summary>
    /// Types of detection methods
    /// </summary>
    public enum DetectionType
    {
        Vision,
        Hearing,
        Smell,
        Combined
    }

    #endregion
}
