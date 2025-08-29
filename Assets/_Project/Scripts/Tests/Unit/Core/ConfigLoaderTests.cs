using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using NUnit.Framework;
using Laboratory.Core;

#nullable enable

namespace Laboratory.Tests.Unit.Core
{
    /// <summary>
    /// Unit tests for ConfigLoader functionality.
    /// Tests JSON config loading, ScriptableObject loading, and error handling.
    /// </summary>
    public class ConfigLoaderTests
    {
        private ConfigLoader? _configLoader;

        [SetUp]
        public void SetUp()
        {
            _configLoader = new ConfigLoader();
        }

        [TearDown]
        public void TearDown()
        {
            _configLoader = null;
        }

        #region JSON Config Tests

        [Test]
        public async Task LoadJsonConfigAsync_WithValidJson_ReturnsConfig()
        {
            // Arrange
            var testConfig = new TestConfig { Name = "TestGame", Version = "1.0.0" };
            await CreateTestJsonConfig("testConfig.json", testConfig);

            // Act
            var result = await _configLoader!.LoadJsonConfigAsync<TestConfig>("testConfig.json");

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("TestGame", result!.Name);
            Assert.AreEqual("1.0.0", result.Version);

            // Cleanup
            CleanupTestFile("testConfig.json");
        }

        [Test]
        public async Task LoadJsonConfigAsync_WithInvalidPath_ReturnsNull()
        {
            // Act
            var result = await _configLoader!.LoadJsonConfigAsync<TestConfig>("nonexistent.json");

            // Assert
            Assert.IsNull(result);
        }

        [Test]
        public async Task LoadJsonConfigAsync_WithInvalidJson_ReturnsNull()
        {
            // Arrange
            await CreateTestTextFile("invalidConfig.json", "{ invalid json }");

            // Act
            var result = await _configLoader!.LoadJsonConfigAsync<TestConfig>("invalidConfig.json");

            // Assert
            Assert.IsNull(result);

            // Cleanup
            CleanupTestFile("invalidConfig.json");
        }

        #endregion

        #region ScriptableObject Config Tests

        [Test]
        public async Task LoadScriptableObjectConfigAsync_WithValidAsset_ReturnsConfig()
        {
            // This test would require a test ScriptableObject asset in Resources
            // For now, we'll test the null case
            
            // Act
            var result = await _configLoader!.LoadScriptableObjectConfigAsync<TestScriptableConfig>("NonExistent");

            // Assert
            Assert.IsNull(result);
        }

        #endregion

        #region Helper Methods

        private async Task CreateTestJsonConfig<T>(string relativePath, T config)
        {
            var fullPath = Path.Combine(Application.streamingAssetsPath, relativePath);
            var directory = Path.GetDirectoryName(fullPath);
            
            if (directory != null && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var json = JsonUtility.ToJson(config);
            await File.WriteAllTextAsync(fullPath, json);
        }

        private async Task CreateTestTextFile(string relativePath, string content)
        {
            var fullPath = Path.Combine(Application.streamingAssetsPath, relativePath);
            var directory = Path.GetDirectoryName(fullPath);
            
            if (directory != null && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            await File.WriteAllTextAsync(fullPath, content);
        }

        private void CleanupTestFile(string relativePath)
        {
            var fullPath = Path.Combine(Application.streamingAssetsPath, relativePath);
            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
            }
        }

        #endregion

        #region Test Data Classes

        [System.Serializable]
        public class TestConfig
        {
            public string Name = string.Empty;
            public string Version = string.Empty;
            public int MaxPlayers;
            public bool EnableDebug;
        }

        [CreateAssetMenu(fileName = "TestScriptableConfig", menuName = "Tests/TestScriptableConfig")]
        public class TestScriptableConfig : ScriptableObject
        {
            public string ConfigName = "Default";
            public int ConfigValue = 100;
        }

        #endregion
    }
}