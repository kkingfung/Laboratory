using UnityEngine;
using UnityEditor;
using System.IO;
using Laboratory.Chimera.ECS;
using Laboratory.Chimera.Spawning;

namespace Laboratory.Editor.Tools
{
    /// <summary>
    /// Unity Editor tool for generating prefab templates for Project Chimera
    /// Provides designer-friendly interface for creating creature, UI, and spawner prefabs
    ///
    /// Usage: Tools → Chimera → Prefab Generator
    /// </summary>
    public class PrefabGeneratorTool : EditorWindow
    {
        private enum PrefabCategory
        {
            Creature,
            UI,
            Spawner
        }

        private enum CreatureTemplate
        {
            Basic,
            AI,
            Battle,
            Breeding,
            PlayerCompanion
        }

        private enum UITemplate
        {
            HUD,
            Menu,
            Dialog
        }

        private enum SpawnerTemplate
        {
            Random,
            Wave,
            Area,
            Event
        }

        // UI State
        private PrefabCategory _selectedCategory = PrefabCategory.Creature;
        private CreatureTemplate _selectedCreatureTemplate = CreatureTemplate.Basic;
        private UITemplate _selectedUITemplate = UITemplate.HUD;
        private SpawnerTemplate _selectedSpawnerTemplate = SpawnerTemplate.Random;

        private string _prefabName = "NewPrefab";
        private bool _addVisualPlaceholder = true;
        private bool _addCollider = true;
        private bool _addRigidbody = false;

        // Paths
        private const string PREFAB_ROOT = "Assets/_Project/Prefabs";
        private const string CREATURE_PATH = PREFAB_ROOT + "/Creatures/Templates";
        private const string UI_PATH = PREFAB_ROOT + "/UI";
        private const string SPAWNER_PATH = PREFAB_ROOT + "/Spawners";

        [MenuItem("Tools/Chimera/Prefab Generator")]
        public static void ShowWindow()
        {
            var window = GetWindow<PrefabGeneratorTool>("Prefab Generator");
            window.minSize = new Vector2(400, 500);
            window.Show();
        }

        private void OnGUI()
        {
            GUILayout.Label("Project Chimera Prefab Generator", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            // Category Selection
            EditorGUILayout.LabelField("Prefab Category", EditorStyles.boldLabel);
            _selectedCategory = (PrefabCategory)EditorGUILayout.EnumPopup("Category:", _selectedCategory);
            EditorGUILayout.Space();

            // Template Selection based on category
            DrawTemplateSelection();
            EditorGUILayout.Space();

            // Common Settings
            DrawCommonSettings();
            EditorGUILayout.Space();

            // Category-specific Settings
            DrawCategorySettings();
            EditorGUILayout.Space();

            // Generation Button
            if (GUILayout.Button("Generate Prefab", GUILayout.Height(40)))
            {
                GeneratePrefab();
            }

            EditorGUILayout.Space();
            DrawHelpBox();
        }

        private void DrawTemplateSelection()
        {
            EditorGUILayout.LabelField("Template Type", EditorStyles.boldLabel);

            switch (_selectedCategory)
            {
                case PrefabCategory.Creature:
                    _selectedCreatureTemplate = (CreatureTemplate)EditorGUILayout.EnumPopup(
                        "Creature Template:", _selectedCreatureTemplate);
                    DrawCreatureTemplateDescription();
                    break;

                case PrefabCategory.UI:
                    _selectedUITemplate = (UITemplate)EditorGUILayout.EnumPopup(
                        "UI Template:", _selectedUITemplate);
                    DrawUITemplateDescription();
                    break;

                case PrefabCategory.Spawner:
                    _selectedSpawnerTemplate = (SpawnerTemplate)EditorGUILayout.EnumPopup(
                        "Spawner Template:", _selectedSpawnerTemplate);
                    DrawSpawnerTemplateDescription();
                    break;
            }
        }

        private void DrawCommonSettings()
        {
            EditorGUILayout.LabelField("Prefab Settings", EditorStyles.boldLabel);
            _prefabName = EditorGUILayout.TextField("Prefab Name:", _prefabName);

            if (_selectedCategory == PrefabCategory.Creature)
            {
                _addVisualPlaceholder = EditorGUILayout.Toggle("Add Visual Placeholder", _addVisualPlaceholder);
                _addCollider = EditorGUILayout.Toggle("Add Collider", _addCollider);
                _addRigidbody = EditorGUILayout.Toggle("Add Rigidbody", _addRigidbody);
            }
        }

        private void DrawCategorySettings()
        {
            // Reserved for category-specific settings in the future
        }

        private void DrawCreatureTemplateDescription()
        {
            string description = _selectedCreatureTemplate switch
            {
                CreatureTemplate.Basic => "Minimal creature setup for testing and prototyping",
                CreatureTemplate.AI => "Creature with full AI behavior systems",
                CreatureTemplate.Battle => "Combat-ready creature with abilities",
                CreatureTemplate.Breeding => "Breeding-enabled creature with genetics",
                CreatureTemplate.PlayerCompanion => "Player-bonded creature with UI integration",
                _ => ""
            };

            EditorGUILayout.HelpBox(description, MessageType.Info);
        }

        private void DrawUITemplateDescription()
        {
            string description = _selectedUITemplate switch
            {
                UITemplate.HUD => "In-game HUD elements (health, status, etc.)",
                UITemplate.Menu => "Full-screen menu interface",
                UITemplate.Dialog => "Popup dialog window",
                _ => ""
            };

            EditorGUILayout.HelpBox(description, MessageType.Info);
        }

        private void DrawSpawnerTemplateDescription()
        {
            string description = _selectedSpawnerTemplate switch
            {
                SpawnerTemplate.Random => "Spawns creatures randomly at intervals",
                SpawnerTemplate.Wave => "Wave-based spawning with multiple phases",
                SpawnerTemplate.Area => "Maintains population density in an area",
                SpawnerTemplate.Event => "Event-triggered spawning",
                _ => ""
            };

            EditorGUILayout.HelpBox(description, MessageType.Info);
        }

        private void DrawHelpBox()
        {
            EditorGUILayout.HelpBox(
                "This tool generates prefab templates with required components.\n\n" +
                "After generation:\n" +
                "1. Assign CreatureDefinition (for creatures)\n" +
                "2. Customize visual elements\n" +
                "3. Configure component settings\n" +
                "4. Test in play mode",
                MessageType.Info
            );
        }

        private void GeneratePrefab()
        {
            if (string.IsNullOrWhiteSpace(_prefabName))
            {
                EditorUtility.DisplayDialog("Error", "Please enter a prefab name", "OK");
                return;
            }

            try
            {
                switch (_selectedCategory)
                {
                    case PrefabCategory.Creature:
                        GenerateCreaturePrefab();
                        break;
                    case PrefabCategory.UI:
                        GenerateUIPrefab();
                        break;
                    case PrefabCategory.Spawner:
                        GenerateSpawnerPrefab();
                        break;
                }
            }
            catch (System.Exception ex)
            {
                EditorUtility.DisplayDialog("Error", $"Failed to generate prefab:\n{ex.Message}", "OK");
                Debug.LogError($"Prefab generation error: {ex}");
            }
        }

        private void GenerateCreaturePrefab()
        {
            // Ensure directory exists
            if (!AssetDatabase.IsValidFolder(CREATURE_PATH))
            {
                Directory.CreateDirectory(CREATURE_PATH);
            }

            // Create GameObject
            GameObject prefabObj = new GameObject(_prefabName);

            // Add EnhancedCreatureAuthoring component
            var creatureAuthoring = prefabObj.AddComponent<EnhancedCreatureAuthoring>();

            // Configure based on template
            switch (_selectedCreatureTemplate)
            {
                case CreatureTemplate.Basic:
                    // Minimal setup (EnhancedCreatureAuthoring already added)
                    break;

                case CreatureTemplate.AI:
                    // AI setup would go here
                    // Note: AI components would need to be added based on your AI system
                    break;

                case CreatureTemplate.Battle:
                    // Battle setup
                    break;

                case CreatureTemplate.Breeding:
                    // Breeding setup
                    break;

                case CreatureTemplate.PlayerCompanion:
                    // Companion setup
                    break;
            }

            // Add optional components
            if (_addVisualPlaceholder)
            {
                AddVisualPlaceholder(prefabObj);
            }

            if (_addCollider)
            {
                var collider = prefabObj.AddComponent<CapsuleCollider>();
                collider.radius = 0.5f;
                collider.height = 2f;
                collider.center = new Vector3(0, 1, 0);
            }

            if (_addRigidbody)
            {
                var rb = prefabObj.AddComponent<Rigidbody>();
                rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            }

            // Save as prefab
            string prefabPath = $"{CREATURE_PATH}/Creature_{_prefabName}.prefab";
            prefabPath = AssetDatabase.GenerateUniqueAssetPath(prefabPath);

            PrefabUtility.SaveAsPrefabAsset(prefabObj, prefabPath);
            DestroyImmediate(prefabObj);

            AssetDatabase.Refresh();
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

            EditorUtility.DisplayDialog("Success",
                $"Creature prefab created:\n{prefabPath}\n\n" +
                "Next steps:\n" +
                "1. Assign a CreatureDefinition ScriptableObject\n" +
                "2. Replace placeholder visual with actual model\n" +
                "3. Configure component settings",
                "OK");

            Debug.Log($"[PrefabGenerator] Created creature prefab: {prefabPath}");
        }

        private void GenerateUIPrefab()
        {
            // Ensure directory exists
            string fullPath = $"{UI_PATH}/{_selectedUITemplate}";
            if (!AssetDatabase.IsValidFolder(fullPath))
            {
                Directory.CreateDirectory(fullPath);
            }

            GameObject prefabObj;

            switch (_selectedUITemplate)
            {
                case UITemplate.HUD:
                    prefabObj = CreateHUDTemplate();
                    break;
                case UITemplate.Menu:
                    prefabObj = CreateMenuTemplate();
                    break;
                case UITemplate.Dialog:
                    prefabObj = CreateDialogTemplate();
                    break;
                default:
                    prefabObj = new GameObject(_prefabName);
                    break;
            }

            // Save as prefab
            string prefabPath = $"{fullPath}/UI_{_selectedUITemplate}_{_prefabName}.prefab";
            prefabPath = AssetDatabase.GenerateUniqueAssetPath(prefabPath);

            PrefabUtility.SaveAsPrefabAsset(prefabObj, prefabPath);
            DestroyImmediate(prefabObj);

            AssetDatabase.Refresh();
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

            EditorUtility.DisplayDialog("Success",
                $"UI prefab created:\n{prefabPath}\n\n" +
                "Next steps:\n" +
                "1. Customize UI elements\n" +
                "2. Hook up events and scripts\n" +
                "3. Test in Canvas",
                "OK");

            Debug.Log($"[PrefabGenerator] Created UI prefab: {prefabPath}");
        }

        private void GenerateSpawnerPrefab()
        {
            // Ensure directory exists
            string fullPath = $"{SPAWNER_PATH}/{_selectedSpawnerTemplate}";
            if (!AssetDatabase.IsValidFolder(fullPath))
            {
                Directory.CreateDirectory(fullPath);
            }

            // Create GameObject
            GameObject prefabObj = new GameObject($"Spawner_{_prefabName}");

            // Add CreatureSpawnerAuthoring if it exists
            var spawnerType = System.Type.GetType("Laboratory.Chimera.Spawning.CreatureSpawnerAuthoring");
            if (spawnerType != null)
            {
                prefabObj.AddComponent(spawnerType);
            }
            else
            {
                Debug.LogWarning("CreatureSpawnerAuthoring component not found. Adding placeholder.");
            }

            // Add gizmo drawer for editor visualization
            var gizmoDrawer = prefabObj.AddComponent<SpawnerGizmoDrawer>();
            gizmoDrawer.gizmoColor = new Color(0, 1, 0, 0.3f);
            gizmoDrawer.radius = 10f;

            // Save as prefab
            string prefabPath = $"{fullPath}/Spawner_{_selectedSpawnerTemplate}_{_prefabName}.prefab";
            prefabPath = AssetDatabase.GenerateUniqueAssetPath(prefabPath);

            PrefabUtility.SaveAsPrefabAsset(prefabObj, prefabPath);
            DestroyImmediate(prefabObj);

            AssetDatabase.Refresh();
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

            EditorUtility.DisplayDialog("Success",
                $"Spawner prefab created:\n{prefabPath}\n\n" +
                "Next steps:\n" +
                "1. Position in scene\n" +
                "2. Assign CreatureDefinitions to spawn\n" +
                "3. Configure spawn parameters",
                "OK");

            Debug.Log($"[PrefabGenerator] Created spawner prefab: {prefabPath}");
        }

        private void AddVisualPlaceholder(GameObject parent)
        {
            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            visual.name = "Visual_Placeholder";
            visual.transform.SetParent(parent.transform);
            visual.transform.localPosition = new Vector3(0, 1, 0);
            visual.transform.localRotation = Quaternion.identity;
            visual.transform.localScale = Vector3.one;

            // Remove collider from visual (parent already has one if enabled)
            DestroyImmediate(visual.GetComponent<Collider>());

            // Add colored material
            var renderer = visual.GetComponent<MeshRenderer>();
            Material mat = new Material(Shader.Find("Standard"));
            mat.color = new Color(0.5f, 0.8f, 1f, 1f); // Light blue
            renderer.material = mat;
        }

        private GameObject CreateHUDTemplate()
        {
            GameObject hud = new GameObject($"HUD_{_prefabName}");
            var rectTransform = hud.AddComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;

            return hud;
        }

        private GameObject CreateMenuTemplate()
        {
            GameObject menu = new GameObject($"Menu_{_prefabName}");
            var rectTransform = menu.AddComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;

            // Add background
            GameObject background = new GameObject("Background");
            background.transform.SetParent(menu.transform);
            var bgRect = background.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;

            var bgImage = background.AddComponent<UnityEngine.UI.Image>();
            bgImage.color = new Color(0.1f, 0.1f, 0.1f, 0.9f);

            return menu;
        }

        private GameObject CreateDialogTemplate()
        {
            GameObject dialog = new GameObject($"Dialog_{_prefabName}");
            var rectTransform = dialog.AddComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(400, 300);

            // Add panel background
            var image = dialog.AddComponent<UnityEngine.UI.Image>();
            image.color = new Color(0.2f, 0.2f, 0.2f, 1f);

            return dialog;
        }

        /// <summary>
        /// Simple component for drawing gizmos for spawners
        /// </summary>
        private class SpawnerGizmoDrawer : MonoBehaviour
        {
            public Color gizmoColor = new Color(0, 1, 0, 0.3f);
            public float radius = 10f;

            private void OnDrawGizmos()
            {
                Gizmos.color = gizmoColor;
                Gizmos.DrawWireSphere(transform.position, radius);
            }

            private void OnDrawGizmosSelected()
            {
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(transform.position, radius);
            }
        }
    }
}
