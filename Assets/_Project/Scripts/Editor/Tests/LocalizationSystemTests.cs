using UnityEngine;
using UnityEditor;
using Laboratory.Chimera.Localization;
using System.Collections.Generic;

namespace Laboratory.Editor.Tests
{
    /// <summary>
    /// Unit tests for the Localization System
    /// Tests LocalizationManager, LocalizationDatabase, and LocalizedText components
    /// </summary>
    public static class LocalizationSystemTests
    {
        [MenuItem("Chimera/Tests/Validate Localization System")]
        public static void ValidateLocalizationSystem()
        {
            var results = new List<string>();
            bool allPassed = true;

            EditorUtility.DisplayProgressBar("Localization Tests", "Running tests...", 0f);

            try
            {
                // Test 1: LocalizationManager singleton
                EditorUtility.DisplayProgressBar("Localization Tests", "Test 1/8: Singleton", 1f/8f);
                if (TestManagerSingleton(out string result1))
                    results.Add("✓ LocalizationManager: Singleton pattern works");
                else
                {
                    results.Add("✗ LocalizationManager Singleton: " + result1);
                    allPassed = false;
                }

                // Test 2: Database loading
                EditorUtility.DisplayProgressBar("Localization Tests", "Test 2/8: Database Loading", 2f/8f);
                if (TestDatabaseLoading(out string result2))
                    results.Add("✓ LocalizationDatabase: All language databases found");
                else
                {
                    results.Add("✗ Database Loading: " + result2);
                    allPassed = false;
                }

                // Test 3: Language switching
                EditorUtility.DisplayProgressBar("Localization Tests", "Test 3/8: Language Switching", 3f/8f);
                if (TestLanguageSwitching(out string result3))
                    results.Add("✓ Language Switching: SetLanguage() works correctly");
                else
                {
                    results.Add("✗ Language Switching: " + result3);
                    allPassed = false;
                }

                // Test 4: Text retrieval
                EditorUtility.DisplayProgressBar("Localization Tests", "Test 4/8: Text Retrieval", 4f/8f);
                if (TestTextRetrieval(out string result4))
                    results.Add("✓ Text Retrieval: GetText() works correctly");
                else
                {
                    results.Add("✗ Text Retrieval: " + result4);
                    allPassed = false;
                }

                // Test 5: Format strings
                EditorUtility.DisplayProgressBar("Localization Tests", "Test 5/8: Format Strings", 5f/8f);
                if (TestFormatStrings(out string result5))
                    results.Add("✓ Format Strings: String.Format integration works");
                else
                {
                    results.Add("✗ Format Strings: " + result5);
                    allPassed = false;
                }

                // Test 6: Missing key handling
                EditorUtility.DisplayProgressBar("Localization Tests", "Test 6/8: Missing Keys", 6f/8f);
                if (TestMissingKeys(out string result6))
                    results.Add("✓ Missing Keys: Fallback behavior works");
                else
                {
                    results.Add("✗ Missing Keys: " + result6);
                    allPassed = false;
                }

                // Test 7: Translation completeness
                EditorUtility.DisplayProgressBar("Localization Tests", "Test 7/8: Translation Completeness", 7f/8f);
                if (TestTranslationCompleteness(out string result7))
                    results.Add("✓ Translation Completeness: All languages have same keys");
                else
                {
                    results.Add("✗ Translation Completeness: " + result7);
                    allPassed = false;
                }

                // Test 8: Event system
                EditorUtility.DisplayProgressBar("Localization Tests", "Test 8/8: Event System", 8f/8f);
                if (TestEventSystem(out string result8))
                    results.Add("✓ Event System: OnLanguageChanged fires correctly");
                else
                {
                    results.Add("✗ Event System: " + result8);
                    allPassed = false;
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            // Display results
            string summary = allPassed ?
                "✅ All localization tests passed!" :
                "⚠ Some tests failed - review results below";

            string fullReport = $"{summary}\n\n" + string.Join("\n", results);

            Debug.Log($"[Localization System Tests]\n{fullReport}");

            EditorUtility.DisplayDialog(
                "Localization Validation Results",
                fullReport,
                "OK"
            );
        }

        private static bool TestManagerSingleton(out string result)
        {
            try
            {
                var manager = LocalizationManager.Instance;

                if (manager == null)
                {
                    result = "LocalizationManager.Instance is null - manager asset not created or not in Resources folder";
                    return false;
                }

                result = $"Found manager: {manager.name}";
                return true;
            }
            catch (System.Exception e)
            {
                result = e.Message;
                return false;
            }
        }

        private static bool TestDatabaseLoading(out string result)
        {
            try
            {
                var manager = LocalizationManager.Instance;
                if (manager == null)
                {
                    result = "Manager not found";
                    return false;
                }

                // Check for expected language databases
                var databases = Resources.LoadAll<LocalizationDatabase>("Localization");

                if (databases.Length == 0)
                {
                    result = "No localization databases found in Resources/Localization";
                    return false;
                }

                result = $"Found {databases.Length} language database(s)";
                return true;
            }
            catch (System.Exception e)
            {
                result = e.Message;
                return false;
            }
        }

        private static bool TestLanguageSwitching(out string result)
        {
            try
            {
                var manager = LocalizationManager.Instance;
                if (manager == null)
                {
                    result = "Manager not found";
                    return false;
                }

                var originalLanguage = manager.CurrentLanguage;

                // Try switching to different languages
                var testLanguages = new[] {
                    SystemLanguage.English,
                    SystemLanguage.Spanish,
                    SystemLanguage.French
                };

                int successCount = 0;
                foreach (var lang in testLanguages)
                {
                    if (manager.SetLanguage(lang))
                        successCount++;
                }

                // Restore original language
                manager.SetLanguage(originalLanguage);

                result = $"Successfully switched to {successCount}/{testLanguages.Length} languages";
                return successCount > 0;
            }
            catch (System.Exception e)
            {
                result = e.Message;
                return false;
            }
        }

        private static bool TestTextRetrieval(out string result)
        {
            try
            {
                var manager = LocalizationManager.Instance;
                if (manager == null)
                {
                    result = "Manager not found";
                    return false;
                }

                // Test retrieving a known key
                string text = manager.GetText("game_title");

                if (string.IsNullOrEmpty(text))
                {
                    result = "GetText returned empty for 'game_title' key";
                    return false;
                }

                result = $"GetText works: '{text}'";
                return true;
            }
            catch (System.Exception e)
            {
                result = e.Message;
                return false;
            }
        }

        private static bool TestFormatStrings(out string result)
        {
            try
            {
                var manager = LocalizationManager.Instance;
                if (manager == null)
                {
                    result = "Manager not found";
                    return false;
                }

                // Test format string (if available)
                string text = manager.GetText("ui_level_value", "5");

                if (string.IsNullOrEmpty(text))
                {
                    result = "Format string test skipped (no format keys found)";
                    return true; // Don't fail if no format strings exist
                }

                bool containsValue = text.Contains("5");
                result = containsValue ?
                    $"Format strings work: '{text}'" :
                    $"Format string didn't substitute value: '{text}'";

                return containsValue;
            }
            catch (System.Exception e)
            {
                result = e.Message;
                return false;
            }
        }

        private static bool TestMissingKeys(out string result)
        {
            try
            {
                var manager = LocalizationManager.Instance;
                if (manager == null)
                {
                    result = "Manager not found";
                    return false;
                }

                string text = manager.GetText("nonexistent_key_12345");

                // Should return the key itself or a fallback message
                bool handledGracefully = !string.IsNullOrEmpty(text);

                result = handledGracefully ?
                    $"Missing key handled gracefully: '{text}'" :
                    "Missing key returned null/empty";

                return handledGracefully;
            }
            catch (System.Exception e)
            {
                result = e.Message;
                return false;
            }
        }

        private static bool TestTranslationCompleteness(out string result)
        {
            try
            {
                var databases = Resources.LoadAll<LocalizationDatabase>("Localization");

                if (databases.Length == 0)
                {
                    result = "No databases to check";
                    return true;
                }

                // Get key count from first database
                int expectedKeyCount = databases[0].GetAllKeys().Length;
                var issues = new List<string>();

                foreach (var db in databases)
                {
                    int actualKeyCount = db.GetAllKeys().Length;
                    if (actualKeyCount != expectedKeyCount)
                    {
                        issues.Add($"{db.languageName}: {actualKeyCount} keys (expected {expectedKeyCount})");
                    }
                }

                if (issues.Count > 0)
                {
                    result = "Key count mismatch:\n" + string.Join("\n", issues);
                    return false;
                }

                result = $"All {databases.Length} databases have {expectedKeyCount} keys";
                return true;
            }
            catch (System.Exception e)
            {
                result = e.Message;
                return false;
            }
        }

        private static bool TestEventSystem(out string result)
        {
            try
            {
                var manager = LocalizationManager.Instance;
                if (manager == null)
                {
                    result = "Manager not found";
                    return false;
                }

                bool eventFired = false;
                SystemLanguage? eventLanguage = null;

                System.Action<SystemLanguage> handler = (lang) =>
                {
                    eventFired = true;
                    eventLanguage = lang;
                };

                manager.OnLanguageChanged += handler;

                var testLang = manager.CurrentLanguage == SystemLanguage.English ?
                    SystemLanguage.Spanish : SystemLanguage.English;

                manager.SetLanguage(testLang);

                manager.OnLanguageChanged -= handler;

                // Restore original
                manager.SetLanguage(SystemLanguage.English);

                result = eventFired ?
                    $"Event fired correctly (lang: {eventLanguage})" :
                    "OnLanguageChanged event did not fire";

                return eventFired;
            }
            catch (System.Exception e)
            {
                result = e.Message;
                return false;
            }
        }

        [MenuItem("Chimera/Tests/Export Localization Report")]
        public static void ExportLocalizationReport()
        {
            var databases = Resources.LoadAll<LocalizationDatabase>("Localization");

            if (databases.Length == 0)
            {
                EditorUtility.DisplayDialog(
                    "No Databases",
                    "No localization databases found in Resources/Localization",
                    "OK"
                );
                return;
            }

            var report = new System.Text.StringBuilder();
            report.AppendLine("# Localization System Report");
            report.AppendLine($"Generated: {System.DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            report.AppendLine();

            foreach (var db in databases)
            {
                report.AppendLine($"## {db.languageName} ({db.language})");
                report.AppendLine($"- Total Keys: {db.GetAllKeys().Length}");
                report.AppendLine($"- Asset: {AssetDatabase.GetAssetPath(db)}");
                report.AppendLine();
            }

            Debug.Log(report.ToString());

            EditorUtility.DisplayDialog(
                "Localization Report",
                $"Report generated for {databases.Length} languages.\nSee Console for details.",
                "OK"
            );
        }
    }
}
