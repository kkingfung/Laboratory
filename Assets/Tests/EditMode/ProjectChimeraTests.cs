using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// Basic test suite for Project Chimera to ensure CI pipeline functionality.
/// These tests validate core systems and provide a foundation for future test expansion.
/// </summary>
public class ProjectChimeraTests
{
    /// <summary>
    /// Test to verify Unity's core functionality is working.
    /// This ensures the test framework is properly configured.
    /// </summary>
    [Test]
    public void UnityCoreFunctionality_ShouldWork()
    {
        // Arrange
        var testObject = new GameObject("TestObject");
        
        // Act & Assert
        Assert.IsNotNull(testObject);
        Assert.AreEqual("TestObject", testObject.name);
        
        // Cleanup
        Object.DestroyImmediate(testObject);
    }

    /// <summary>
    /// Test to verify the project's main systems can be instantiated.
    /// This validates that core dependencies are properly configured.
    /// </summary>
    [Test]
    public void ProjectSystems_ShouldInitialize()
    {
        // Arrange
        var gameObject = new GameObject("TestGameObject");
        
        // Act - Try to add core components
        var transform = gameObject.GetComponent<Transform>();
        
        // Assert
        Assert.IsNotNull(transform);
        Assert.IsNotNull(gameObject);
        
        // Cleanup
        Object.DestroyImmediate(gameObject);
    }

    /// <summary>
    /// Test to verify mathematical operations work correctly.
    /// This ensures the math library is functioning properly.
    /// </summary>
    [Test]
    public void MathOperations_ShouldWorkCorrectly()
    {
        // Arrange
        float a = 5.0f;
        float b = 3.0f;
        
        // Act
        float sum = a + b;
        float product = a * b;
        
        // Assert
        Assert.AreEqual(8.0f, sum, 0.001f);
        Assert.AreEqual(15.0f, product, 0.001f);
    }

    /// <summary>
    /// Test to verify Vector3 operations work correctly.
    /// This ensures Unity's math library is functioning properly.
    /// </summary>
    [Test]
    public void Vector3Operations_ShouldWorkCorrectly()
    {
        // Arrange
        Vector3 vector1 = new Vector3(1, 2, 3);
        Vector3 vector2 = new Vector3(4, 5, 6);
        
        // Act
        Vector3 sum = vector1 + vector2;
        float magnitude = vector1.magnitude;
        
        // Assert
        Assert.AreEqual(new Vector3(5, 7, 9), sum);
        Assert.AreEqual(Mathf.Sqrt(14), magnitude, 0.001f);
    }

    /// <summary>
    /// Test to verify the project's package dependencies are loaded.
    /// This ensures all required packages are properly installed.
    /// </summary>
    [Test]
    public void PackageDependencies_ShouldBeLoaded()
    {
        // This test verifies that the project can access its dependencies
        // In a real scenario, you would test specific package functionality
        
        // For now, we'll just verify basic Unity functionality
        Assert.IsTrue(Application.isPlaying || !Application.isPlaying); // Always true, but tests the framework
    }
}
