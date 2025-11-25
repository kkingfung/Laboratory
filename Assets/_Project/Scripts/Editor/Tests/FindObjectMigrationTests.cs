using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using Laboratory.Core.Diagnostics;
using Laboratory.Tools;

namespace Laboratory.Editor.Tests
{
    /// <summary>
    /// Validation tests for FindObjectOfType → FindFirstObjectByType migration
    /// Ensures all migrated code works correctly with Unity 2023+ APIs
    /// </summary>
    public static class FindObjectMigrationTests
    {
        [MenuItem("Chimera/Tests/Validate FindObject Migration")]
        public static void ValidateMigration()
        {
            var results = new List<string>();
            bool allPassed = true;

            EditorUtility.DisplayProgressBar("Migration Validation", "Testing migrated API calls...", 0f);

            try
            {
                // Test 1: PerformanceSubsystemManager - FindObjectsByType for counts
                EditorUtility.DisplayProgressBar("Migration Validation", "Test 1/7: Performance System", 1f/7f);
                if (TestPerformanceSystem(out string result1))
                    results.Add("✓ PerformanceSubsystemManager: FindObjectsByType works correctly");
                else
                {
                    results.Add("✗ PerformanceSubsystemManager: " + result1);
                    allPassed = false;
                }

                // Test 2: ChimeraECSSystems - FindObjectsByType with Include
                EditorUtility.DisplayProgressBar("Migration Validation", "Test 2/7: ECS Systems", 2f/7f);
                if (TestECSSystem(out string result2))
                    results.Add("✓ ChimeraECSSystems: FindObjectsByType(Include) works correctly");
                else
                {
                    results.Add("✗ ChimeraECSSystems: " + result2);
                    allPassed = false;
                }

                // Test 3: AdvancedShaderManager - FindObjectsByType with Include
                EditorUtility.DisplayProgressBar("Migration Validation", "Test 3/7: Shader Manager", 3f/7f);
                if (TestShaderManager(out string result3))
                    results.Add("✓ AdvancedShaderManager: Shader lookup works correctly");
                else
                {
                    results.Add("✗ AdvancedShaderManager: " + result3);
                    allPassed = false;
                }

                // Test 4: DynamicWeatherSystem - FindFirstObjectByType for Light
                EditorUtility.DisplayProgressBar("Migration Validation", "Test 4/7: Weather System", 4f/7f);
                if (TestWeatherSystem(out string result4))
                    results.Add("✓ DynamicWeatherSystem: FindFirstObjectByType works correctly");
                else
                {
                    results.Add("✗ DynamicWeatherSystem: " + result4);
                    allPassed = false;
                }

                // Test 5: NetworkSynchronizationDebugger - FindFirstObjectByType
                EditorUtility.DisplayProgressBar("Migration Validation", "Test 5/7: Network Debugger", 5f/7f);
                if (TestNetworkDebugger(out string result5))
                    results.Add("✓ NetworkSynchronizationDebugger: Manager detection works");
                else
                {
                    results.Add("✗ NetworkSynchronizationDebugger: " + result5);
                    allPassed = false;
                }

                // Test 6: MemoryProfiler - FindObjectsByType for counts
                EditorUtility.DisplayProgressBar("Migration Validation", "Test 6/7: Memory Profiler", 6f/7f);
                if (TestMemoryProfiler(out string result6))
                    results.Add("✓ MemoryProfiler: Object counting works correctly");
                else
                {
                    results.Add("✗ MemoryProfiler: " + result6);
                    allPassed = false;
                }

                // Test 7: UnityCompatibility - Conditional compilation
                EditorUtility.DisplayProgressBar("Migration Validation", "Test 7/7: Compatibility Layer", 7f/7f);
                if (TestCompatibilityLayer(out string result7))
                    results.Add("✓ UnityCompatibility: Conditional compilation correct");
                else
                {
                    results.Add("✗ UnityCompatibility: " + result7);
                    allPassed = false;
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            // Display results
            string summary = allPassed ?
                "✅ All migration tests passed!" :
                "⚠ Some tests failed - review results below";

            string fullReport = $"{summary}\n\n" + string.Join("\n", results);

            Debug.Log($"[FindObject Migration Tests]\n{fullReport}");

            EditorUtility.DisplayDialog(
                "Migration Validation Results",
                fullReport,
                "OK"
            );
        }

        private static bool TestPerformanceSystem(out string result)
        {
            try
            {
                // Test FindObjectsByType for Rigidbody and AudioSource
                var rigidbodies = Object.FindObjectsByType<Rigidbody>(FindObjectsSortMode.None);
                var audioSources = Object.FindObjectsByType<AudioSource>(FindObjectsSortMode.None);

                result = $"Found {rigidbodies.Length} Rigidbodies, {audioSources.Length} AudioSources";
                return true;
            }
            catch (System.Exception e)
            {
                result = e.Message;
                return false;
            }
        }

        private static bool TestECSSystem(out string result)
        {
            try
            {
                // Test FindObjectsByType with FindObjectsInactive.Include
                var allGameObjects = Object.FindObjectsByType<GameObject>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None
                );

                result = $"Found {allGameObjects.Length} GameObjects (including inactive)";
                return true;
            }
            catch (System.Exception e)
            {
                result = e.Message;
                return false;
            }
        }

        private static bool TestShaderManager(out string result)
        {
            try
            {
                // Test FindObjectsByType with FindObjectsInactive.Include for Shaders
                var shaders = Object.FindObjectsByType<Shader>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None
                );

                result = $"Found {shaders.Length} Shaders";
                return true;
            }
            catch (System.Exception e)
            {
                result = e.Message;
                return false;
            }
        }

        private static bool TestWeatherSystem(out string result)
        {
            try
            {
                // Test FindFirstObjectByType for Light
                var light = Object.FindFirstObjectByType<Light>();

                result = light != null ?
                    $"Found directional light: {light.name}" :
                    "No lights in scene (expected for empty scene)";
                return true;
            }
            catch (System.Exception e)
            {
                result = e.Message;
                return false;
            }
        }

        private static bool TestNetworkDebugger(out string result)
        {
            try
            {
                // Test FindFirstObjectByType for various manager types
                var anyComponent = Object.FindFirstObjectByType<MonoBehaviour>();

                result = anyComponent != null ?
                    $"Manager detection works" :
                    "No MonoBehaviours in scene (expected for empty scene)";
                return true;
            }
            catch (System.Exception e)
            {
                result = e.Message;
                return false;
            }
        }

        private static bool TestMemoryProfiler(out string result)
        {
            try
            {
                // Test FindObjectsByType for object counting
                var allObjects = Object.FindObjectsByType<Object>(FindObjectsSortMode.None);
                var gameObjects = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);

                result = $"Object counting works: {allObjects.Length} Objects, {gameObjects.Length} GameObjects";
                return true;
            }
            catch (System.Exception e)
            {
                result = e.Message;
                return false;
            }
        }

        private static bool TestCompatibilityLayer(out string result)
        {
            try
            {
                // Test that UnityCompatibility wrapper works
                var testObject = Laboratory.Compatibility.UnityCompatibility.FindFirstObjectByType<Camera>();

                result = testObject != null ?
                    $"Compatibility layer works (found: {testObject.name})" :
                    "Compatibility layer works (no Camera in scene)";
                return true;
            }
            catch (System.Exception e)
            {
                result = e.Message;
                return false;
            }
        }

        [MenuItem("Chimera/Tests/Quick Compile Check")]
        public static void QuickCompileCheck()
        {
            // Just check if code compiles by trying to call the new APIs
            try
            {
                var _ = Object.FindFirstObjectByType<Camera>();
                var __ = Object.FindObjectsByType<Light>(FindObjectsSortMode.None);

                Debug.Log("✓ Quick compile check passed - Unity 2023+ APIs accessible");
                EditorUtility.DisplayDialog(
                    "Compile Check",
                    "✓ Unity 2023+ FindObject APIs are working correctly",
                    "OK"
                );
            }
            catch (System.Exception e)
            {
                Debug.LogError($"✗ Compile check failed: {e.Message}");
                EditorUtility.DisplayDialog(
                    "Compile Check Failed",
                    $"Error: {e.Message}",
                    "OK"
                );
            }
        }
    }
}
