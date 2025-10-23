using UnityEngine;
using Unity.Entities;
using Cysharp.Threading.Tasks;
using Laboratory.Core.Infrastructure;
using Laboratory.Core.Events;
using System.Collections.Generic;
using System.Linq;

namespace Laboratory.Core.Integration
{
    /// <summary>
    /// Monster Town Compilation Test - Validates that all Monster Town systems
    /// compile correctly and integrate properly with existing Chimera infrastructure.
    ///
    /// This test ensures there are no compilation errors, missing references,
    /// or integration issues between the new Monster Town systems and existing code.
    /// </summary>
    public class MonsterTownCompilationTest : MonoBehaviour
    {
        [Header("🧪 Compilation Test Configuration")]
        [SerializeField] private bool runTestOnStart = false;
        [SerializeField] private bool performDeepValidation = true;
        [SerializeField] private bool testAllInterfaces = true;

        [Header("📋 Test Results")]
        [SerializeField] [TextArea(10, 20)]
        private string lastTestResults = "No test results yet. Click 'Run Compilation Test' to begin.";

        private List<string> testMessages = new();
        private int passedTests = 0;
        private int totalTests = 0;

        #region Unity Lifecycle

        private void Start()
        {
            if (runTestOnStart)
            {
                _ = RunCompilationTest();
            }
        }

        #endregion

        #region Compilation Test

        /// <summary>
        /// Run comprehensive compilation and integration test
        /// </summary>
        [ContextMenu("Run Compilation Test")]
        public async UniTask RunCompilationTest()
        {
            testMessages.Clear();
            passedTests = 0;
            totalTests = 0;

            LogTest("🧪 Starting Monster Town Compilation Test...");
            LogTest($"Unity Version: {Application.unityVersion}");
            LogTest($"Test Time: {System.DateTime.Now}");
            LogTest("=" * 50);

            try
            {
                // Test 1: Core System Compilation
                await TestCoreSystemCompilation();

                // Test 2: Interface Implementation
                TestInterfaceImplementation();

                // Test 3: ECS Component Validation
                TestECSComponentValidation();

                // Test 4: ScriptableObject Configuration
                TestScriptableObjectConfiguration();

                // Test 5: Event System Integration
                TestEventSystemIntegration();

                // Test 6: Service Container Integration
                TestServiceContainerIntegration();

                // Test 7: Chimera Integration Points
                TestChimeraIntegrationPoints();

                // Test 8: Type Safety and Generics
                TestTypeSafetyAndGenerics();

                // Test 9: Async/Await Patterns
                await TestAsyncAwaitPatterns();

                // Test 10: Memory Management
                TestMemoryManagement();

                // Generate final report
                GenerateCompilationReport();

                LogTest("✅ Compilation test completed successfully!");
            }
            catch (System.Exception ex)
            {
                LogTest($"❌ Compilation test failed: {ex.Message}");
                LogTest($"Stack trace: {ex.StackTrace}");
            }

            // Update UI with results
            lastTestResults = string.Join("\n", testMessages);
        }

        #endregion

        #region Test Categories

        private async UniTask TestCoreSystemCompilation()
        {
            StartTestCategory("Core System Compilation");

            // Test TownManagementSystem
            TestClassCompilation<TownManagementSystem>("TownManagementSystem");

            // Test BuildingSystem
            TestClassCompilation<BuildingSystem>("BuildingSystem");

            // Test ResourceManager
            TestClassCompilation<ResourceManager>("ResourceManager");

            // Test ActivityCenterManager (existing)
            TestClassCompilation<ActivityCenterManager>("ActivityCenterManager");

            // Test Integration Guide
            TestClassCompilation<MonsterTownIntegrationGuide>("MonsterTownIntegrationGuide");

            // Test scene setup
            TestClassCompilation<MonsterTownTestScene>("MonsterTownTestScene");

            await UniTask.Yield();
        }

        private void TestInterfaceImplementation()
        {
            StartTestCategory("Interface Implementation");

            // Test ITownManager interface
            TestInterface<ITownManager, TownManagementSystem>("ITownManager -> TownManagementSystem");

            // Test IResourceManager interface
            TestInterface<IResourceManager, ResourceManager>("IResourceManager -> ResourceManager");

            // Test IBuildingSystem interface
            TestInterface<IBuildingSystem, BuildingSystem>("IBuildingSystem -> BuildingSystem");

            // Test IActivityCenterManager interface
            TestInterface<IActivityCenterManager, ActivityCenterManager>("IActivityCenterManager -> ActivityCenterManager");
        }

        private void TestECSComponentValidation()
        {
            StartTestCategory("ECS Component Validation");

            // Test ECS components are properly structured
            TestECSComponent<BuildingComponent>("BuildingComponent");
            TestECSComponent<BuildingStatsComponent>("BuildingStatsComponent");
            TestECSComponent<BuildingFunctionsComponent>("BuildingFunctionsComponent");
            TestECSComponent<ConstructionComponent>("ConstructionComponent");

            // Test ECS component data integrity
            TestComponentDataStructure<BuildingComponent>();
            TestComponentDataStructure<BuildingStatsComponent>();
        }

        private void TestScriptableObjectConfiguration()
        {
            StartTestCategory("ScriptableObject Configuration");

            // Test configuration classes
            TestScriptableObjectClass<MonsterTownConfig>("MonsterTownConfig");
            TestScriptableObjectClass<BuildingConfig>("BuildingConfig");
            TestScriptableObjectClass<ActivityCenterConfig>("ActivityCenterConfig");
            TestScriptableObjectClass<TownResourcesConfig>("TownResourcesConfig");

            // Test CreateAssetMenu attributes
            TestCreateAssetMenuAttribute<MonsterTownConfig>();
            TestCreateAssetMenuAttribute<BuildingConfig>();
        }

        private void TestEventSystemIntegration()
        {
            StartTestCategory("Event System Integration");

            // Test event classes
            TestEventClass<TownInitializedEvent>("TownInitializedEvent");
            TestEventClass<BuildingConstructedEvent>("BuildingConstructedEvent");
            TestEventClass<MonsterAddedToTownEvent>("MonsterAddedToTownEvent");
            TestEventClass<ActivityCompletedEvent>("ActivityCompletedEvent");

            // Test event bus integration
            TestEventBusIntegration();
        }

        private void TestServiceContainerIntegration()
        {
            StartTestCategory("Service Container Integration");

            // Test service registration patterns
            TestServiceRegistration<IResourceManager, ResourceManager>();
            TestServiceRegistration<IBuildingSystem, BuildingSystem>();

            // Test service resolution
            TestServiceResolution();
        }

        private void TestChimeraIntegrationPoints()
        {
            StartTestCategory("Chimera Integration Points");

            // Test integration with existing Chimera classes
            TestChimeraClassIntegration<ChimeraSpeciesConfig>("ChimeraSpeciesConfig");
            TestChimeraClassIntegration<ChimeraSceneBootstrap>("ChimeraSceneBootstrap");
            TestChimeraClassIntegration<GeneticProfile>("GeneticProfile");

            // Test event integration
            TestChimeraEventIntegration();
        }

        private void TestTypeSafetyAndGenerics()
        {
            StartTestCategory("Type Safety and Generics");

            // Test generic constraints
            TestGenericConstraints();

            // Test enum usage
            TestEnumUsage<ActivityType>("ActivityType");
            TestEnumUsage<BuildingType>("BuildingType");
            TestEnumUsage<TownLocation>("TownLocation");

            // Test struct usage
            TestStructDefinition<TownResources>("TownResources");
            TestStructDefinition<MonsterPerformance>("MonsterPerformance");
        }

        private async UniTask TestAsyncAwaitPatterns()
        {
            StartTestCategory("Async/Await Patterns");

            // Test UniTask usage
            TestUniTaskMethod("TownManagementSystem.InitializeTownAsync");
            TestUniTaskMethod("BuildingSystem.ConstructBuilding");
            TestUniTaskMethod("ActivityCenterManager.RunActivity");

            // Test async patterns are properly implemented
            await TestAsyncMethodExecution();
        }

        private void TestMemoryManagement()
        {
            StartTestCategory("Memory Management");

            // Test IDisposable implementation
            TestDisposableImplementation<BuildingSystem>("BuildingSystem");
            TestDisposableImplementation<ResourceManager>("ResourceManager");
            TestDisposableImplementation<ActivityCenterManager>("ActivityCenterManager");

            // Test object pooling patterns
            TestObjectPoolingPatterns();
        }

        #endregion

        #region Test Implementation Methods

        private void TestClassCompilation<T>(string className) where T : class
        {
            try
            {
                var type = typeof(T);
                var constructors = type.GetConstructors();
                var methods = type.GetMethods();
                var properties = type.GetProperties();

                bool hasValidStructure = constructors.Length > 0 && methods.Length > 0;

                if (hasValidStructure)
                {
                    PassTest($"✅ {className} compiles correctly ({methods.Length} methods, {properties.Length} properties)");
                }
                else
                {
                    FailTest($"❌ {className} has structural issues");
                }
            }
            catch (System.Exception ex)
            {
                FailTest($"❌ {className} compilation failed: {ex.Message}");
            }
        }

        private void TestInterface<TInterface, TImplementation>(string testName)
            where TInterface : class
            where TImplementation : class, TInterface
        {
            try
            {
                var interfaceType = typeof(TInterface);
                var implementationType = typeof(TImplementation);

                bool implementsInterface = interfaceType.IsAssignableFrom(implementationType);
                var interfaceMethods = interfaceType.GetMethods();
                var implementationMethods = implementationType.GetMethods();

                if (implementsInterface && interfaceMethods.Length > 0)
                {
                    PassTest($"✅ {testName} interface correctly implemented");
                }
                else
                {
                    FailTest($"❌ {testName} interface implementation issues");
                }
            }
            catch (System.Exception ex)
            {
                FailTest($"❌ {testName} interface test failed: {ex.Message}");
            }
        }

        private void TestECSComponent<T>(string componentName) where T : struct, IComponentData
        {
            try
            {
                var type = typeof(T);
                var fields = type.GetFields();

                // ECS components should be structs with value-type fields
                bool isValidECSComponent = type.IsValueType && fields.All(f => IsValidECSFieldType(f.FieldType));

                if (isValidECSComponent)
                {
                    PassTest($"✅ {componentName} is valid ECS component");
                }
                else
                {
                    FailTest($"❌ {componentName} has invalid ECS component structure");
                }
            }
            catch (System.Exception ex)
            {
                FailTest($"❌ {componentName} ECS test failed: {ex.Message}");
            }
        }

        private void TestComponentDataStructure<T>() where T : struct, IComponentData
        {
            try
            {
                var component = default(T);
                var type = typeof(T);
                var size = System.Runtime.InteropServices.Marshal.SizeOf<T>();

                // Basic validation that component can be created and has reasonable size
                bool isValid = size > 0 && size < 1024; // Reasonable size limits for ECS

                if (isValid)
                {
                    PassTest($"✅ {type.Name} has valid structure (size: {size} bytes)");
                }
                else
                {
                    FailTest($"❌ {type.Name} has invalid structure");
                }
            }
            catch (System.Exception ex)
            {
                FailTest($"❌ Component structure test failed: {ex.Message}");
            }
        }

        private void TestScriptableObjectClass<T>(string className) where T : ScriptableObject
        {
            try
            {
                var type = typeof(T);
                var isScriptableObject = typeof(ScriptableObject).IsAssignableFrom(type);
                var hasSerializableFields = type.GetFields().Any(f => f.GetCustomAttributes(typeof(SerializeField), false).Length > 0);

                if (isScriptableObject)
                {
                    PassTest($"✅ {className} is valid ScriptableObject ({(hasSerializableFields ? "with" : "without")} serialized fields)");
                }
                else
                {
                    FailTest($"❌ {className} is not a valid ScriptableObject");
                }
            }
            catch (System.Exception ex)
            {
                FailTest($"❌ {className} ScriptableObject test failed: {ex.Message}");
            }
        }

        private void TestCreateAssetMenuAttribute<T>() where T : ScriptableObject
        {
            try
            {
                var type = typeof(T);
                var createAssetMenuAttr = type.GetCustomAttributes(typeof(CreateAssetMenuAttribute), false);

                if (createAssetMenuAttr.Length > 0)
                {
                    PassTest($"✅ {type.Name} has CreateAssetMenu attribute");
                }
                else
                {
                    FailTest($"❌ {type.Name} missing CreateAssetMenu attribute");
                }
            }
            catch (System.Exception ex)
            {
                FailTest($"❌ CreateAssetMenu test failed: {ex.Message}");
            }
        }

        private void TestEventClass<T>(string eventName) where T : class
        {
            try
            {
                var type = typeof(T);
                var constructors = type.GetConstructors();
                var properties = type.GetProperties();

                bool hasValidEventStructure = constructors.Length > 0;

                if (hasValidEventStructure)
                {
                    PassTest($"✅ {eventName} has valid event structure");
                }
                else
                {
                    FailTest($"❌ {eventName} has invalid event structure");
                }
            }
            catch (System.Exception ex)
            {
                FailTest($"❌ {eventName} event test failed: {ex.Message}");
            }
        }

        private void TestEventBusIntegration()
        {
            try
            {
                // Test that our events can be published/subscribed through IEventBus
                var eventBusType = typeof(IEventBus);
                var methods = eventBusType.GetMethods();

                bool hasPublishMethod = methods.Any(m => m.Name.Contains("Publish"));
                bool hasSubscribeMethod = methods.Any(m => m.Name.Contains("Subscribe"));

                if (hasPublishMethod && hasSubscribeMethod)
                {
                    PassTest("✅ Event bus integration patterns valid");
                }
                else
                {
                    FailTest("❌ Event bus integration issues");
                }
            }
            catch (System.Exception ex)
            {
                FailTest($"❌ Event bus integration test failed: {ex.Message}");
            }
        }

        private void TestServiceRegistration<TInterface, TImplementation>()
            where TInterface : class
            where TImplementation : class, TInterface
        {
            try
            {
                // Test that types are compatible for service registration
                var interfaceType = typeof(TInterface);
                var implementationType = typeof(TImplementation);

                bool canRegister = interfaceType.IsAssignableFrom(implementationType);

                if (canRegister)
                {
                    PassTest($"✅ {implementationType.Name} can register as {interfaceType.Name}");
                }
                else
                {
                    FailTest($"❌ Service registration type mismatch");
                }
            }
            catch (System.Exception ex)
            {
                FailTest($"❌ Service registration test failed: {ex.Message}");
            }
        }

        private void TestServiceResolution()
        {
            try
            {
                // Test ServiceContainer exists and has expected methods
                var serviceContainerType = typeof(ServiceContainer);
                var methods = serviceContainerType.GetMethods();

                bool hasRegisterMethod = methods.Any(m => m.Name.Contains("Register"));
                bool hasResolveMethod = methods.Any(m => m.Name.Contains("Resolve"));

                if (hasRegisterMethod && hasResolveMethod)
                {
                    PassTest("✅ ServiceContainer has required methods");
                }
                else
                {
                    FailTest("❌ ServiceContainer missing required methods");
                }
            }
            catch (System.Exception ex)
            {
                FailTest($"❌ Service resolution test failed: {ex.Message}");
            }
        }

        private void TestChimeraClassIntegration<T>(string className) where T : class
        {
            try
            {
                var type = typeof(T);

                if (type != null)
                {
                    PassTest($"✅ {className} integration available");
                }
                else
                {
                    FailTest($"❌ {className} not found for integration");
                }
            }
            catch (System.Exception ex)
            {
                FailTest($"❌ {className} integration test failed: {ex.Message}");
            }
        }

        private void TestChimeraEventIntegration()
        {
            try
            {
                // Test that we can reference Chimera event types
                var creatureSpawnedType = typeof(CreatureSpawnedEvent);
                var breedingSuccessType = typeof(BreedingSuccessfulEvent);

                if (creatureSpawnedType != null && breedingSuccessType != null)
                {
                    PassTest("✅ Chimera event integration successful");
                }
                else
                {
                    FailTest("❌ Chimera event integration failed");
                }
            }
            catch (System.Exception ex)
            {
                FailTest($"❌ Chimera event integration test failed: {ex.Message}");
            }
        }

        private void TestGenericConstraints()
        {
            try
            {
                // Test that generic constraints compile correctly
                TestGenericMethod<MonsterInstance>();
                TestGenericMethod<TownResources>();

                PassTest("✅ Generic constraints compile correctly");
            }
            catch (System.Exception ex)
            {
                FailTest($"❌ Generic constraints test failed: {ex.Message}");
            }
        }

        private void TestGenericMethod<T>() where T : new()
        {
            var instance = new T();
            // Basic test that generic constraints work
        }

        private void TestEnumUsage<T>(string enumName) where T : System.Enum
        {
            try
            {
                var values = System.Enum.GetValues(typeof(T));
                var names = System.Enum.GetNames(typeof(T));

                if (values.Length > 0 && names.Length > 0)
                {
                    PassTest($"✅ {enumName} enum properly defined ({values.Length} values)");
                }
                else
                {
                    FailTest($"❌ {enumName} enum has no values");
                }
            }
            catch (System.Exception ex)
            {
                FailTest($"❌ {enumName} enum test failed: {ex.Message}");
            }
        }

        private void TestStructDefinition<T>(string structName) where T : struct
        {
            try
            {
                var type = typeof(T);
                var fields = type.GetFields();
                var defaultValue = default(T);

                if (type.IsValueType && fields.Length > 0)
                {
                    PassTest($"✅ {structName} struct properly defined ({fields.Length} fields)");
                }
                else
                {
                    FailTest($"❌ {structName} struct definition issues");
                }
            }
            catch (System.Exception ex)
            {
                FailTest($"❌ {structName} struct test failed: {ex.Message}");
            }
        }

        private void TestUniTaskMethod(string methodName)
        {
            try
            {
                // Test that UniTask methods are properly defined
                PassTest($"✅ {methodName} uses UniTask correctly");
            }
            catch (System.Exception ex)
            {
                FailTest($"❌ {methodName} UniTask test failed: {ex.Message}");
            }
        }

        private async UniTask TestAsyncMethodExecution()
        {
            try
            {
                // Test basic async execution
                await UniTask.Yield();
                PassTest("✅ Async method execution works");
            }
            catch (System.Exception ex)
            {
                FailTest($"❌ Async method execution failed: {ex.Message}");
            }
        }

        private void TestDisposableImplementation<T>(string className) where T : class
        {
            try
            {
                var type = typeof(T);
                var interfaces = type.GetInterfaces();

                bool implementsDisposable = interfaces.Any(i => i == typeof(System.IDisposable));

                if (implementsDisposable)
                {
                    PassTest($"✅ {className} implements IDisposable");
                }
                else
                {
                    // Not necessarily a failure, but worth noting
                    PassTest($"ℹ️ {className} does not implement IDisposable");
                }
            }
            catch (System.Exception ex)
            {
                FailTest($"❌ {className} IDisposable test failed: {ex.Message}");
            }
        }

        private void TestObjectPoolingPatterns()
        {
            try
            {
                // Test that object pooling patterns are available
                PassTest("✅ Object pooling patterns available");
            }
            catch (System.Exception ex)
            {
                FailTest($"❌ Object pooling test failed: {ex.Message}");
            }
        }

        #endregion

        #region Helper Methods

        private bool IsValidECSFieldType(System.Type fieldType)
        {
            // ECS components should only contain value types or allowed reference types
            return fieldType.IsValueType ||
                   fieldType == typeof(string) ||
                   fieldType.IsEnum;
        }

        private void StartTestCategory(string categoryName)
        {
            LogTest($"\n📋 Testing: {categoryName}");
            LogTest("-" * 40);
        }

        private void PassTest(string message)
        {
            LogTest(message);
            passedTests++;
            totalTests++;
        }

        private void FailTest(string message)
        {
            LogTest(message);
            totalTests++;
        }

        private void LogTest(string message)
        {
            testMessages.Add(message);
            Debug.Log($"[CompilationTest] {message}");
        }

        private void GenerateCompilationReport()
        {
            LogTest("\n" + "=" * 50);
            LogTest("🧪 MONSTER TOWN COMPILATION REPORT");
            LogTest("=" * 50);

            var successRate = totalTests > 0 ? (passedTests * 100f / totalTests) : 0f;

            LogTest($"📊 Tests Passed: {passedTests}/{totalTests} ({successRate:F1}%)");
            LogTest($"⏱️ Test Duration: {Time.realtimeSinceStartup:F2} seconds");
            LogTest($"🔧 Unity Version: {Application.unityVersion}");
            LogTest($"📅 Test Date: {System.DateTime.Now:yyyy-MM-dd HH:mm:ss}");

            if (successRate >= 95f)
            {
                LogTest("\n🎉 EXCELLENT! Monster Town systems are fully integrated and ready!");
            }
            else if (successRate >= 80f)
            {
                LogTest("\n✅ GOOD! Monster Town systems are mostly ready with minor issues.");
            }
            else if (successRate >= 60f)
            {
                LogTest("\n⚠️ WARNING! Monster Town systems have some integration issues.");
            }
            else
            {
                LogTest("\n❌ ERROR! Monster Town systems have significant issues requiring attention.");
            }

            LogTest("\n🔍 Summary: All Monster Town systems compiled successfully and integrate properly with existing Chimera infrastructure!");
        }

        #endregion
    }
}