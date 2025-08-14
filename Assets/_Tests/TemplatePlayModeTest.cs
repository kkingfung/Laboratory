/*
using UnityEngine;
using UnityEngine.TestTools;
using NUnit.Framework;
using System.Collections;

public class PlayerPlayModeTests
{
    private GameObject playerObject;
    private Player player;

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        playerObject = new GameObject("Player");
        player = playerObject.AddComponent<Player>();
        player.health = 100;
        yield return null; // wait one frame
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        Object.Destroy(playerObject);
        yield return null; // wait one frame
    }

    [UnityTest]
    public IEnumerator Player_TakesDamage_InPlayMode()
    {
        player.TakeDamage(40);
        yield return null;
        Assert.AreEqual(60, player.health);
    }

    [UnityTest]
    public IEnumerator Player_MovesForward()
    {
        Vector3 startPos = playerObject.transform.position;
        playerObject.transform.Translate(Vector3.forward * 1f);
        yield return null;
        Assert.AreEqual(startPos + Vector3.forward, playerObject.transform.position);
    }
}
*/
