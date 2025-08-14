/*
using NUnit.Framework;
using UnityEngine;

public class PlayerEditModeTests
{
    private GameObject playerObject;
    private Player player;

    [SetUp]
    public void SetUp()
    {
        // Create a new player object before each test
        playerObject = new GameObject("Player");
        player = playerObject.AddComponent<Player>();
        player.health = 100;
    }

    [TearDown]
    public void TearDown()
    {
        // Clean up after each test
        Object.DestroyImmediate(playerObject);
    }

    [Test]
    public void Player_TakesDamage_ReducesHealth()
    {
        player.TakeDamage(30);
        Assert.AreEqual(70, player.health);
    }

    [Test]
    public void Player_Heals_IncreasesHealth()
    {
        player.TakeDamage(50);
        player.Heal(20);
        Assert.AreEqual(70, player.health);
    }
}
*/