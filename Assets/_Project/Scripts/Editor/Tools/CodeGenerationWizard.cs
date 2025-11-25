using UnityEngine;
using UnityEditor;
using System.IO;
using System.Text;
using System.Collections.Generic;

namespace Laboratory.Editor.Tools
{
    /// <summary>
    /// Code generation wizard for common Project Chimera patterns
    /// Generates ECS systems, ScriptableObjects, authoring components, and more
    /// Reduces boilerplate and ensures architectural consistency
    /// </summary>
    public class CodeGenerationWizard : EditorWindow
    {
        private CodeGenerationType _generationType = CodeGenerationType.ECSSystem;
        private string _className = "";
        private string _namespace = "Laboratory.Chimera";
        private string _outputFolder = "Assets/_Project/Scripts/Generated";
        private bool _generateTests = true;
        private bool _addBurstCompilation = true;
        private bool _addXmlDocumentation = true;

        // ECS System specific
        private string _systemGroup = "SimulationSystemGroup";

        // ScriptableObject specific
        private string _menuPath = "Chimera/";
        private bool _generateConfig = false;

        // Authoring component specific
        private string _componentType = "GeneticDataComponent";

        private Vector2 _scrollPosition;

        private enum CodeGenerationType
        {
            ECSSystem,
            ScriptableObject,
            AuthoringComponent,
            ServiceInterface,
            DataStructure,
            EditorTool
        }

        [MenuItem("Chimera/Code Generation/Open Wizard", false, 200)]
        private static void ShowWindow()
        {
            var window = GetWindow<CodeGenerationWizard>("Code Generation Wizard");
            window.minSize = new Vector2(500, 600);
            window.Show();
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(10);

            EditorGUILayout.LabelField("Code Generation Wizard", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Generate boilerplate code following Project Chimera patterns", MessageType.Info);

            EditorGUILayout.Space(10);

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            // Generation type selection
            EditorGUILayout.LabelField("Code Type", EditorStyles.boldLabel);
            _generationType = (CodeGenerationType)EditorGUILayout.EnumPopup("Type:", _generationType);

            EditorGUILayout.Space(5);

            // Common settings
            EditorGUILayout.LabelField("Common Settings", EditorStyles.boldLabel);
            _className = EditorGUILayout.TextField("Class Name:", _className);
            _namespace = EditorGUILayout.TextField("Namespace:", _namespace);
            _outputFolder = EditorGUILayout.TextField("Output Folder:", _outputFolder);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Browse..."))
            {
                string path = EditorUtility.OpenFolderPanel("Select Output Folder", "Assets", "");
                if (!string.IsNullOrEmpty(path) && path.StartsWith(Application.dataPath))
                {
                    _outputFolder = "Assets" + path.Substring(Application.dataPath.Length);
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);

            // Options
            EditorGUILayout.LabelField("Options", EditorStyles.boldLabel);
            _generateTests = EditorGUILayout.Toggle("Generate Tests:", _generateTests);
            _addXmlDocumentation = EditorGUILayout.Toggle("Add XML Docs:", _addXmlDocumentation);

            EditorGUILayout.Space(5);

            // Type-specific settings
            DrawTypeSpecificSettings();

            EditorGUILayout.Space(10);

            // Generate button
            GUI.enabled = !string.IsNullOrEmpty(_className);
            if (GUILayout.Button("Generate Code", GUILayout.Height(40)))
            {
                GenerateCode();
            }
            GUI.enabled = true;

            EditorGUILayout.EndScrollView();
        }

        private void DrawTypeSpecificSettings()
        {
            EditorGUILayout.LabelField("Type-Specific Settings", EditorStyles.boldLabel);

            switch (_generationType)
            {
                case CodeGenerationType.ECSSystem:
                    _addBurstCompilation = EditorGUILayout.Toggle("Add [BurstCompile]:", _addBurstCompilation);
                    _systemGroup = EditorGUILayout.TextField("System Group:", _systemGroup);
                    break;

                case CodeGenerationType.ScriptableObject:
                    _menuPath = EditorGUILayout.TextField("Menu Path:", _menuPath);
                    _generateConfig = EditorGUILayout.Toggle("Generate Config Class:", _generateConfig);
                    break;

                case CodeGenerationType.AuthoringComponent:
                    _componentType = EditorGUILayout.TextField("Component Type:", _componentType);
                    break;

                case CodeGenerationType.ServiceInterface:
                    EditorGUILayout.HelpBox("Will generate interface and implementation class", MessageType.Info);
                    break;

                case CodeGenerationType.DataStructure:
                    EditorGUILayout.HelpBox("Will generate IComponentData struct with ECS compliance", MessageType.Info);
                    break;

                case CodeGenerationType.EditorTool:
                    EditorGUILayout.HelpBox("Will generate EditorWindow with basic UI", MessageType.Info);
                    break;
            }
        }

        private void GenerateCode()
        {
            if (string.IsNullOrEmpty(_className))
            {
                EditorUtility.DisplayDialog("Error", "Class name is required", "OK");
                return;
            }

            if (!Directory.Exists(_outputFolder))
            {
                Directory.CreateDirectory(_outputFolder);
            }

            string generatedCode = "";

            switch (_generationType)
            {
                case CodeGenerationType.ECSSystem:
                    generatedCode = GenerateECSSystem();
                    break;

                case CodeGenerationType.ScriptableObject:
                    generatedCode = GenerateScriptableObject();
                    break;

                case CodeGenerationType.AuthoringComponent:
                    generatedCode = GenerateAuthoringComponent();
                    break;

                case CodeGenerationType.ServiceInterface:
                    generatedCode = GenerateServiceInterface();
                    break;

                case CodeGenerationType.DataStructure:
                    generatedCode = GenerateDataStructure();
                    break;

                case CodeGenerationType.EditorTool:
                    generatedCode = GenerateEditorTool();
                    break;
            }

            string fileName = Path.Combine(_outputFolder, $"{_className}.cs");
            File.WriteAllText(fileName, generatedCode);

            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog("Success",
                $"Generated {_className}.cs\n\nLocation: {fileName}",
                "OK");

            EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(fileName));

            // Generate tests if requested
            if (_generateTests)
            {
                GenerateTestFile();
            }
        }

        private string GenerateECSSystem()
        {
            var sb = new StringBuilder();

            // Using statements
            sb.AppendLine("using Unity.Entities;");
            sb.AppendLine("using Unity.Collections;");
            if (_addBurstCompilation)
            {
                sb.AppendLine("using Unity.Burst;");
            }
            sb.AppendLine("using UnityEngine;");
            sb.AppendLine();

            // Namespace
            sb.AppendLine($"namespace {_namespace}");
            sb.AppendLine("{");

            // XML documentation
            if (_addXmlDocumentation)
            {
                sb.AppendLine("    /// <summary>");
                sb.AppendLine($"    /// {_className} - Generated ECS System");
                sb.AppendLine("    /// </summary>");
            }

            // System attributes
            if (_addBurstCompilation)
            {
                sb.AppendLine("    [BurstCompile]");
            }
            sb.AppendLine($"    [UpdateInGroup(typeof({_systemGroup}))]");

            // Class declaration
            sb.AppendLine($"    public partial struct {_className} : ISystem");
            sb.AppendLine("    {");

            // OnCreate
            sb.AppendLine("        public void OnCreate(ref SystemState state)");
            sb.AppendLine("        {");
            sb.AppendLine("            // Initialize system");
            sb.AppendLine("        }");
            sb.AppendLine();

            // OnUpdate
            if (_addBurstCompilation)
            {
                sb.AppendLine("        [BurstCompile]");
            }
            sb.AppendLine("        public void OnUpdate(ref SystemState state)");
            sb.AppendLine("        {");
            sb.AppendLine("            // Process entities");
            sb.AppendLine("            var job = new ProcessJob");
            sb.AppendLine("            {");
            sb.AppendLine("                deltaTime = SystemAPI.Time.DeltaTime");
            sb.AppendLine("            };");
            sb.AppendLine();
            sb.AppendLine("            job.ScheduleParallel();");
            sb.AppendLine("        }");
            sb.AppendLine();

            // OnDestroy
            sb.AppendLine("        public void OnDestroy(ref SystemState state)");
            sb.AppendLine("        {");
            sb.AppendLine("            // Cleanup system");
            sb.AppendLine("        }");
            sb.AppendLine();

            // Job struct
            if (_addBurstCompilation)
            {
                sb.AppendLine("        [BurstCompile]");
            }
            sb.AppendLine("        private partial struct ProcessJob : IJobEntity");
            sb.AppendLine("        {");
            sb.AppendLine("            public float deltaTime;");
            sb.AppendLine();
            sb.AppendLine("            public void Execute(/* Add components here */)");
            sb.AppendLine("            {");
            sb.AppendLine("                // Process entity");
            sb.AppendLine("            }");
            sb.AppendLine("        }");

            sb.AppendLine("    }");
            sb.AppendLine("}");

            return sb.ToString();
        }

        private string GenerateScriptableObject()
        {
            var sb = new StringBuilder();

            sb.AppendLine("using UnityEngine;");
            sb.AppendLine();

            sb.AppendLine($"namespace {_namespace}");
            sb.AppendLine("{");

            if (_addXmlDocumentation)
            {
                sb.AppendLine("    /// <summary>");
                sb.AppendLine($"    /// {_className} - Configuration ScriptableObject");
                sb.AppendLine("    /// </summary>");
            }

            sb.AppendLine($"    [CreateAssetMenu(fileName = \"{_className}\", menuName = \"{_menuPath}{_className}\")]");
            sb.AppendLine($"    public class {_className} : ScriptableObject");
            sb.AppendLine("    {");

            sb.AppendLine("        [Header(\"Configuration\")]");
            sb.AppendLine("        [SerializeField] private string configName;");
            sb.AppendLine("        [SerializeField] private bool isEnabled = true;");
            sb.AppendLine();

            sb.AppendLine("        public string ConfigName => configName;");
            sb.AppendLine("        public bool IsEnabled => isEnabled;");
            sb.AppendLine();

            sb.AppendLine("        private void OnValidate()");
            sb.AppendLine("        {");
            sb.AppendLine("            // Validate configuration values");
            sb.AppendLine("        }");

            sb.AppendLine("    }");
            sb.AppendLine("}");

            return sb.ToString();
        }

        private string GenerateAuthoringComponent()
        {
            var sb = new StringBuilder();

            sb.AppendLine("using Unity.Entities;");
            sb.AppendLine("using UnityEngine;");
            sb.AppendLine();

            sb.AppendLine($"namespace {_namespace}");
            sb.AppendLine("{");

            if (_addXmlDocumentation)
            {
                sb.AppendLine("    /// <summary>");
                sb.AppendLine($"    /// {_className} - Authoring component for {_componentType}");
                sb.AppendLine("    /// </summary>");
            }

            sb.AppendLine($"    public class {_className} : MonoBehaviour");
            sb.AppendLine("    {");
            sb.AppendLine("        [Header(\"Component Data\")]");
            sb.AppendLine("        [SerializeField] private float sampleValue = 1.0f;");
            sb.AppendLine();

            sb.AppendLine("        class Baker : Baker<" + _className + ">");
            sb.AppendLine("        {");
            sb.AppendLine("            public override void Bake(" + _className + " authoring)");
            sb.AppendLine("            {");
            sb.AppendLine("                var entity = GetEntity(TransformUsageFlags.Dynamic);");
            sb.AppendLine($"                AddComponent(entity, new {_componentType}");
            sb.AppendLine("                {");
            sb.AppendLine("                    value = authoring.sampleValue");
            sb.AppendLine("                });");
            sb.AppendLine("            }");
            sb.AppendLine("        }");

            sb.AppendLine("    }");
            sb.AppendLine("}");

            return sb.ToString();
        }

        private string GenerateServiceInterface()
        {
            var sb = new StringBuilder();

            sb.AppendLine("using System.Threading.Tasks;");
            sb.AppendLine();

            sb.AppendLine($"namespace {_namespace}");
            sb.AppendLine("{");

            if (_addXmlDocumentation)
            {
                sb.AppendLine("    /// <summary>");
                sb.AppendLine($"    /// Service interface for {_className}");
                sb.AppendLine("    /// </summary>");
            }

            string interfaceName = $"I{_className}";
            sb.AppendLine($"    public interface {interfaceName}");
            sb.AppendLine("    {");
            sb.AppendLine("        Task<bool> InitializeAsync();");
            sb.AppendLine("        void Update();");
            sb.AppendLine("        void Shutdown();");
            sb.AppendLine("    }");
            sb.AppendLine();

            if (_addXmlDocumentation)
            {
                sb.AppendLine("    /// <summary>");
                sb.AppendLine($"    /// Implementation of {interfaceName}");
                sb.AppendLine("    /// </summary>");
            }

            sb.AppendLine($"    public class {_className} : {interfaceName}");
            sb.AppendLine("    {");
            sb.AppendLine("        private bool _isInitialized = false;");
            sb.AppendLine();

            sb.AppendLine("        public async Task<bool> InitializeAsync()");
            sb.AppendLine("        {");
            sb.AppendLine("            _isInitialized = true;");
            sb.AppendLine("            return true;");
            sb.AppendLine("        }");
            sb.AppendLine();

            sb.AppendLine("        public void Update()");
            sb.AppendLine("        {");
            sb.AppendLine("            if (!_isInitialized) return;");
            sb.AppendLine("            // Update logic");
            sb.AppendLine("        }");
            sb.AppendLine();

            sb.AppendLine("        public void Shutdown()");
            sb.AppendLine("        {");
            sb.AppendLine("            _isInitialized = false;");
            sb.AppendLine("        }");

            sb.AppendLine("    }");
            sb.AppendLine("}");

            return sb.ToString();
        }

        private string GenerateDataStructure()
        {
            var sb = new StringBuilder();

            sb.AppendLine("using Unity.Entities;");
            sb.AppendLine();

            sb.AppendLine($"namespace {_namespace}");
            sb.AppendLine("{");

            if (_addXmlDocumentation)
            {
                sb.AppendLine("    /// <summary>");
                sb.AppendLine($"    /// {_className} - ECS Component Data");
                sb.AppendLine("    /// </summary>");
            }

            sb.AppendLine($"    public struct {_className} : IComponentData");
            sb.AppendLine("    {");
            sb.AppendLine("        public float value;");
            sb.AppendLine("        public int count;");
            sb.AppendLine("        public bool isEnabled;");
            sb.AppendLine("    }");

            sb.AppendLine("}");

            return sb.ToString();
        }

        private string GenerateEditorTool()
        {
            var sb = new StringBuilder();

            sb.AppendLine("using UnityEngine;");
            sb.AppendLine("using UnityEditor;");
            sb.AppendLine();

            sb.AppendLine($"namespace {_namespace}");
            sb.AppendLine("{");

            if (_addXmlDocumentation)
            {
                sb.AppendLine("    /// <summary>");
                sb.AppendLine($"    /// {_className} - Custom Editor Tool");
                sb.AppendLine("    /// </summary>");
            }

            sb.AppendLine($"    public class {_className} : EditorWindow");
            sb.AppendLine("    {");

            sb.AppendLine($"        [MenuItem(\"Chimera/Tools/{_className}\")]");
            sb.AppendLine("        private static void ShowWindow()");
            sb.AppendLine("        {");
            sb.AppendLine($"            var window = GetWindow<{_className}>(\"{_className}\");");
            sb.AppendLine("            window.minSize = new Vector2(400, 300);");
            sb.AppendLine("            window.Show();");
            sb.AppendLine("        }");
            sb.AppendLine();

            sb.AppendLine("        private void OnGUI()");
            sb.AppendLine("        {");
            sb.AppendLine("            EditorGUILayout.Space(10);");
            sb.AppendLine($"            EditorGUILayout.LabelField(\"{_className}\", EditorStyles.boldLabel);");
            sb.AppendLine("            EditorGUILayout.Space(10);");
            sb.AppendLine();
            sb.AppendLine("            // Add your UI here");
            sb.AppendLine();
            sb.AppendLine("            if (GUILayout.Button(\"Execute\", GUILayout.Height(30)))");
            sb.AppendLine("            {");
            sb.AppendLine("                Execute();");
            sb.AppendLine("            }");
            sb.AppendLine("        }");
            sb.AppendLine();

            sb.AppendLine("        private void Execute()");
            sb.AppendLine("        {");
            sb.AppendLine("            Debug.Log(\"Execute called\");");
            sb.AppendLine("        }");

            sb.AppendLine("    }");
            sb.AppendLine("}");

            return sb.ToString();
        }

        private void GenerateTestFile()
        {
            string testFolder = Path.Combine(_outputFolder, "Tests");
            if (!Directory.Exists(testFolder))
            {
                Directory.CreateDirectory(testFolder);
            }

            var sb = new StringBuilder();

            sb.AppendLine("using NUnit.Framework;");
            sb.AppendLine("using UnityEngine;");
            sb.AppendLine();

            sb.AppendLine($"namespace {_namespace}.Tests");
            sb.AppendLine("{");

            sb.AppendLine($"    public class {_className}Tests");
            sb.AppendLine("    {");

            sb.AppendLine("        [Test]");
            sb.AppendLine("        public void Initialization_Succeeds()");
            sb.AppendLine("        {");
            sb.AppendLine("            // Arrange");
            sb.AppendLine($"            var instance = new {_className}();");
            sb.AppendLine();
            sb.AppendLine("            // Act");
            sb.AppendLine("            // Add test logic");
            sb.AppendLine();
            sb.AppendLine("            // Assert");
            sb.AppendLine("            Assert.IsNotNull(instance);");
            sb.AppendLine("        }");

            sb.AppendLine("    }");
            sb.AppendLine("}");

            string testFileName = Path.Combine(testFolder, $"{_className}Tests.cs");
            File.WriteAllText(testFileName, sb.ToString());

            Debug.Log($"Generated test file: {testFileName}");
        }
    }
}
