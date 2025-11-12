using UnityEngine;
using UnityEditor;
using Laboratory.Subsystems.Combat;
using Laboratory.Core.Health;

namespace Laboratory.Editor
{
    /// <summary>
    /// Custom inspector for combat components that provides real-time testing,
    /// damage preview, and quick configuration options.
    /// </summary>
    [CustomEditor(typeof(Laboratory.Subsystems.Player.PlayerController))]
    public class PlayerControllerEditor : UnityEditor.Editor
    {
        private Laboratory.Subsystems.Player.PlayerController playerController;
        private bool showCombatTesting = false;
        private bool showMovementTesting = false;
        private bool showHealthTesting = false;

        private void OnEnable()
        {
            playerController = (Laboratory.Subsystems.Player.PlayerController)target;
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Developer Tools", EditorStyles.boldLabel);

            DrawMovementTesting();
            DrawCombatTesting();
            DrawHealthTesting();
            DrawQuickActions();
        }

        private void DrawMovementTesting()
        {
            showMovementTesting = EditorGUILayout.Foldout(showMovementTesting, "Movement Testing");
            if (showMovementTesting)
            {
                EditorGUI.indentLevel++;
                
                if (Application.isPlaying)
                {
                    EditorGUILayout.HelpBox("Movement testing available during play mode", MessageType.Info);
                    
                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button("Test Jump"))
                    {
                        // Simulate jump input
                        UnityEngine.Debug.Log("Jump test triggered");
                    }
                    if (GUILayout.Button("Test Run"))
                    {
                        UnityEngine.Debug.Log("Run test triggered");
                    }
                    GUILayout.EndHorizontal();
                }
                else
                {
                    EditorGUILayout.HelpBox("Enter play mode to test movement", MessageType.Warning);
                }
                
                EditorGUI.indentLevel--;
            }
        }

        private void DrawCombatTesting()
        {
            showCombatTesting = EditorGUILayout.Foldout(showCombatTesting, "Combat Testing");
            if (showCombatTesting)
            {
                EditorGUI.indentLevel++;
                
                if (Application.isPlaying)
                {
                    EditorGUILayout.LabelField($"Current Health: {playerController.CurrentHealth}/{playerController.MaxHealth}");
                    EditorGUILayout.LabelField($"Is Alive: {playerController.IsAlive}");
                    
                    EditorGUILayout.Space();
                    
                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button("Test Attack"))
                    {
                        // Trigger attack animation or logic
                        UnityEngine.Debug.Log("Attack test triggered");
                    }
                    if (GUILayout.Button("Take 10 Damage"))
                    {
                        var damageRequest = new DamageRequest(10f, null, DamageType.Physical);
                        playerController.TakeDamage(damageRequest);
                    }
                    GUILayout.EndHorizontal();
                    
                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button("Heal 25"))
                    {
                        playerController.Heal(25);
                    }
                    if (GUILayout.Button("Kill Player"))
                    {
                        var damageRequest = new DamageRequest(playerController.CurrentHealth, null, DamageType.True);
                        playerController.TakeDamage(damageRequest);
                    }
                    GUILayout.EndHorizontal();
                }
                else
                {
                    EditorGUILayout.HelpBox("Enter play mode to test combat", MessageType.Warning);
                }
                
                EditorGUI.indentLevel--;
            }
        }

        private void DrawHealthTesting()
        {
            showHealthTesting = EditorGUILayout.Foldout(showHealthTesting, "Health Testing");
            if (showHealthTesting)
            {
                EditorGUI.indentLevel++;
                
                if (Application.isPlaying)
                {
                    // Health bar visualization
                    float healthPercent = playerController.CurrentHealth / playerController.MaxHealth;
                    EditorGUILayout.LabelField("Health Percentage:");
                    Rect healthRect = GUILayoutUtility.GetRect(18, 18, "TextField");
                    EditorGUI.ProgressBar(healthRect, healthPercent, $"{playerController.CurrentHealth:F1}/{playerController.MaxHealth}");
                    
                    // Health color indicator
                    Color healthColor = Color.green;
                    if (healthPercent < 0.3f) healthColor = Color.red;
                    else if (healthPercent < 0.6f) healthColor = Color.yellow;
                    
                    GUI.backgroundColor = healthColor;
                    EditorGUILayout.LabelField("", GUI.skin.horizontalSlider, GUILayout.Height(5));
                    GUI.backgroundColor = Color.white;
                }
                else
                {
                    EditorGUILayout.HelpBox("Health visualization available in play mode", MessageType.Info);
                }
                
                EditorGUI.indentLevel--;
            }
        }

        private void DrawQuickActions()
        {
            EditorGUILayout.LabelField("Quick Actions", EditorStyles.boldLabel);
            
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Focus Camera"))
            {
                SceneView.FrameLastActiveSceneView();
                Selection.activeGameObject = playerController.gameObject;
            }
            if (GUILayout.Button("Validate Setup"))
            {
                ValidatePlayerSetup();
            }
            GUILayout.EndHorizontal();
        }

        private void ValidatePlayerSetup()
        {
            bool hasCharacterController = playerController.GetComponent<CharacterController>() != null;
            bool hasAnimator = playerController.GetComponent<Animator>() != null;
            bool hasAudioSource = playerController.GetComponent<AudioSource>() != null;

            string message = "Player Setup Validation:\n\n";
            message += $"✓ CharacterController: {(hasCharacterController ? "Present" : "MISSING")}\n";
            message += $"✓ Animator: {(hasAnimator ? "Present" : "MISSING")}\n";
            message += $"✓ AudioSource: {(hasAudioSource ? "Present" : "MISSING")}\n";

            if (hasCharacterController && hasAnimator && hasAudioSource)
            {
                message += "\n✅ Player setup is complete!";
                EditorUtility.DisplayDialog("Validation Complete", message, "OK");
            }
            else
            {
                message += "\n⚠️ Some components are missing!";
                EditorUtility.DisplayDialog("Validation Issues", message, "OK");
            }
        }
    }

    /// <summary>
    /// Custom inspector for enemy controllers with AI testing capabilities
    /// </summary>
    [CustomEditor(typeof(Laboratory.Subsystems.EnemyAI.EnemyController))]
    public class EnemyControllerEditor : UnityEditor.Editor
    {
        private Laboratory.Subsystems.EnemyAI.EnemyController enemyController;
        private bool showAITesting = false;
        private bool showCombatTesting = false;

        private void OnEnable()
        {
            enemyController = (Laboratory.Subsystems.EnemyAI.EnemyController)target;
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("AI Developer Tools", EditorStyles.boldLabel);

            DrawAITesting();
            DrawCombatTesting();
        }

        private void DrawAITesting()
        {
            showAITesting = EditorGUILayout.Foldout(showAITesting, "AI State Testing");
            if (showAITesting)
            {
                EditorGUI.indentLevel++;
                
                if (Application.isPlaying)
                {
                    // Show current AI state (this would require exposing the state in EnemyController)
                    EditorGUILayout.LabelField("Current State: [Would show AI state here]");
                    EditorGUILayout.LabelField($"Health: {enemyController.CurrentHealth}/{enemyController.MaxHealth}");
                    EditorGUILayout.LabelField($"Is Alive: {enemyController.IsAlive}");
                    
                    EditorGUILayout.Space();
                    
                    if (GUILayout.Button("Force Patrol State"))
                    {
                        UnityEngine.Debug.Log("Forcing patrol state");
                    }
                    
                    if (GUILayout.Button("Force Attack State"))
                    {
                        UnityEngine.Debug.Log("Forcing attack state");
                    }
                }
                else
                {
                    EditorGUILayout.HelpBox("AI testing available in play mode", MessageType.Info);
                }
                
                EditorGUI.indentLevel--;
            }
        }

        private void DrawCombatTesting()
        {
            showCombatTesting = EditorGUILayout.Foldout(showCombatTesting, "Combat Testing");
            if (showCombatTesting)
            {
                EditorGUI.indentLevel++;
                
                if (Application.isPlaying)
                {
                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button("Take 25 Damage"))
                    {
                        var damageRequest = new DamageRequest(25f, null, DamageType.Physical);
                        enemyController.TakeDamage(damageRequest);
                    }
                    if (GUILayout.Button("Kill Enemy"))
                    {
                        var damageRequest = new DamageRequest(enemyController.CurrentHealth, null, DamageType.True);
                        enemyController.TakeDamage(damageRequest);
                    }
                    GUILayout.EndHorizontal();
                }
                else
                {
                    EditorGUILayout.HelpBox("Combat testing available in play mode", MessageType.Info);
                }
                
                EditorGUI.indentLevel--;
            }
        }
    }

    /// <summary>
    /// Scene view overlay that shows combat information
    /// </summary>
    public class CombatDebugOverlay
    {
        [DrawGizmo(GizmoType.Selected | GizmoType.Active)]
        static void DrawPlayerCombatGizmos(Laboratory.Subsystems.Player.PlayerController player, GizmoType gizmoType)
        {
            if (!Application.isPlaying) return;

            // Draw health bar above player
            Vector3 healthBarPos = player.transform.position + Vector3.up * 2.5f;
            DrawHealthBar(healthBarPos, player.CurrentHealth, player.MaxHealth);

            // Draw attack range
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(player.transform.position, 2f); // Attack range
        }

        [DrawGizmo(GizmoType.Selected | GizmoType.Active)]
        static void DrawEnemyCombatGizmos(Laboratory.Subsystems.EnemyAI.EnemyController enemy, GizmoType gizmoType)
        {
            if (!Application.isPlaying) return;

            // Draw health bar above enemy
            Vector3 healthBarPos = enemy.transform.position + Vector3.up * 2.5f;
            DrawHealthBar(healthBarPos, enemy.CurrentHealth, enemy.MaxHealth);

            // Draw detection range
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(enemy.transform.position, 10f); // Detection range

            // Draw attack range
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(enemy.transform.position, 2f); // Attack range
        }

        private static void DrawHealthBar(Vector3 position, float currentHealth, float maxHealth)
        {
            float healthPercent = currentHealth / maxHealth;
            Vector3 barSize = new Vector3(1f, 0.1f, 0f);
            
            // Background
            Gizmos.color = Color.red;
            Gizmos.DrawCube(position, barSize);
            
            // Health fill
            Color healthColor = Color.green;
            if (healthPercent < 0.3f) healthColor = Color.red;
            else if (healthPercent < 0.6f) healthColor = Color.yellow;
            
            Gizmos.color = healthColor;
            Vector3 fillSize = new Vector3(barSize.x * healthPercent, barSize.y, barSize.z);
            Vector3 fillPos = position + Vector3.left * (barSize.x * (1f - healthPercent) * 0.5f);
            Gizmos.DrawCube(fillPos, fillSize);
        }
    }
}
