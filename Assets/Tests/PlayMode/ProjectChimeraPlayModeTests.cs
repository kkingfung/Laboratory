using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// Play mode tests for Project Chimera.
/// These tests run in play mode and can test runtime behavior.
/// </summary>
public class ProjectChimeraPlayModeTests
{
    /// <summary>
    /// Test to verify play mode functionality.
    /// This ensures the play mode test framework is working.
    /// </summary>
    [UnityTest]
    public System.Collections.IEnumerator PlayModeTest_ShouldWork()
    {
        // Arrange
        var testObject = new GameObject("PlayModeTestObject");
        
        // Act
        yield return null; // Wait one frame
        
        // Assert
        Assert.IsNotNull(testObject);
        Assert.IsTrue(testObject.activeInHierarchy);
        
        // Cleanup
        Object.DestroyImmediate(testObject);
    }

    /// <summary>
    /// Test to verify coroutine functionality.
    /// This ensures Unity's coroutine system is working properly.
    /// </summary>
    [UnityTest]
    public System.Collections.IEnumerator CoroutineTest_ShouldWork()
    {
        // Arrange
        float startTime = Time.time;
        
        // Act
        yield return new WaitForSeconds(0.1f);
        
        // Assert
        float elapsedTime = Time.time - startTime;
        Assert.IsTrue(elapsedTime >= 0.1f);
    }

    /// <summary>
    /// Test to verify GameObject lifecycle in play mode.
    /// This ensures Unity's object management is working properly.
    /// </summary>
    [UnityTest]
    public System.Collections.IEnumerator GameObjectLifecycle_ShouldWork()
    {
        // Arrange
        var testObject = new GameObject("LifecycleTestObject");
        var originalPosition = testObject.transform.position;
        
        // Act
        testObject.transform.position = Vector3.one;
        yield return null; // Wait one frame
        
        // Assert
        Assert.AreEqual(Vector3.one, testObject.transform.position);
        Assert.AreNotEqual(originalPosition, testObject.transform.position);
        
        // Cleanup
        Object.DestroyImmediate(testObject);
    }

    /// <summary>
    /// Test to verify Time.deltaTime functionality.
    /// This ensures Unity's time system is working properly.
    /// </summary>
    [UnityTest]
    public System.Collections.IEnumerator TimeDeltaTime_ShouldWork()
    {
        // Arrange
        float startTime = Time.time;
        
        // Act
        yield return new WaitForSeconds(0.1f);
        
        // Assert
        float elapsedTime = Time.time - startTime;
        Assert.IsTrue(elapsedTime >= 0.1f);
        Assert.IsTrue(Time.deltaTime > 0);
    }
}
