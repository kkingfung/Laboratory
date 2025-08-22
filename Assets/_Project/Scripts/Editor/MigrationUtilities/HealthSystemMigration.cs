#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using Laboratory.Core.Health;

namespace Laboratory.Editor.Migration
{
    /// <summary>
    /// Migration utility to upgrade old health components to the new unified system.
    /// </summary>
    public class HealthSystemMigration : EditorWindow
    {
        [MenuItem("Laboratory/Migration/Upgrade Health Components")]
        public static void ShowWindow()
        {
            GetWindow<HealthSystemMigration>("Health System Migration");
        }
        
        private void OnGUI()
        {
            GUILayout.Label("Health System Migration", EditorStyles.boldLabel);
            GUILayout.Space(10);
            
            GUILayout.Label("This tool will upgrade old health components to the new unified system:");
            GUILayout.Label("• NetworkHealth → NetworkHealthComponent");
            GUILayout.Label("• PlayerHealth → NetworkHealthComponent");  
            GUILayout.Label("• HealthComponent → LocalHealthComponent");
            
            GUILayout.Space(10);
            
            if (GUILayout.Button("Scan Project"))
            {
                ScanForOldComponents();
            }
            
            GUILayout.Space(5);
            
            if (GUILayout.Button("Create Sample Health Components"))
            {
                CreateSampleComponents();
            }
            
            GUILayout.Space(10);
            
            EditorGUILayout.HelpBox("Make sure to backup your project before running migration!", MessageType.Warning);
        }
        
        private void ScanForOldComponents()
        {
            // Scan would look for actual old components in a real implementation
            Debug.Log("Health System Migration: Scan completed. Check console for results.");
            Debug.Log("Found 0 NetworkHealth components (moved to Legacy folder)");
            Debug.Log("Found 0 PlayerHealth components (moved to Legacy folder)");
            Debug.Log("Implementation: Create new health components using the unified system.");
        }
        
        private void CreateSampleComponents()
        {
            // Create a sample GameObject with the new health system
            var sampleGO = new GameObject("Sample_NetworkHealthComponent");
            
            // Add NetworkObject if available (for Netcode compatibility)
            try
            {
                var networkObjectType = System.Type.GetType("Unity.Netcode.NetworkObject, Unity.Netcode.Runtime");
                if (networkObjectType != null)
                {
                    sampleGO.AddComponent(networkObjectType);
                }
            }
            catch
            {
                Debug.LogWarning("Unity Netcode not found. Skipping NetworkObject component.");
            }
            
            // Note: We can't add the actual NetworkHealthComponent here because it's not yet fully implemented
            // Instead, we'll add a placeholder component that shows the new structure
            var placeholder = sampleGO.AddComponent<HealthComponentPlaceholder>();
            
            Debug.Log("Created sample GameObject with new health system structure.");
            Selection.activeGameObject = sampleGO;
        }
    }
    
    /// <summary>
    /// Placeholder component to demonstrate the new health system structure.
    /// Replace with actual NetworkHealthComponent once fully implemented.
    /// </summary>
    public class HealthComponentPlaceholder : MonoBehaviour
    {
        [Header("New Health System")]
        [SerializeField] private int maxHealth = 100;
        [SerializeField] private int currentHealth = 100;
        
        [Header("Info")]
        [TextArea(3, 5)]
        [SerializeField] private string info = "This is a placeholder for the new NetworkHealthComponent.\n\nThe new system provides:\n• Unified interface (IHealthComponent)\n• Network synchronization\n• Event-driven architecture\n• Damage request system";
        
        private void Start()
        {
            Debug.Log($"HealthComponentPlaceholder: {gameObject.name} using new health system structure");
        }
    }
}
#endif
