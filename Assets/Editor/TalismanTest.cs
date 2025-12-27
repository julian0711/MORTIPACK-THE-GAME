using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class TalismanTest
{
    private GameObject dungeonObj;
    private DungeonGeneratorV2 dungeonGen;

    [SetUp]
    public void Setup()
    {
        dungeonObj = new GameObject("DungeonGenerator");
        dungeonGen = dungeonObj.AddComponent<DungeonGeneratorV2>();
    }

    [TearDown]
    public void Teardown()
    {
        Object.Destroy(dungeonObj);
        foreach (var enemy in Object.FindObjectsOfType<EnemyMovement>())
        {
            Object.Destroy(enemy.gameObject);
        }
    }

    [UnityTest]
    public IEnumerator TalismanBanishesEnemies()
    {
        // 1. Spawn 5 Dummy Enemies
        for (int i = 0; i < 5; i++)
        {
            GameObject enemyGO = new GameObject($"Enemy_{i}");
            enemyGO.AddComponent<EnemyMovement>();
        }

        yield return null; // Wait for frame

        int initialCount = Object.FindObjectsOfType<EnemyMovement>().Length;
        Assert.AreEqual(5, initialCount, "Setup failed: Expected 5 enemies.");

        // 2. Execute Banishment
        int banishedCount = dungeonGen.BanishEnemies();

        yield return null;

        // 3. Verify
        int finalCount = Object.FindObjectsOfType<EnemyMovement>().Length;
        
        Debug.Log($"Initial: {initialCount}, Banished: {banishedCount}, Final: {finalCount}");

        Assert.Greater(banishedCount, 0, "Talisman should banish at least 1 enemy.");
        Assert.LessOrEqual(banishedCount, 3, "Talisman should banish at most 3 enemies.");
        Assert.AreEqual(initialCount - banishedCount, finalCount, "Remaining enemies count mismatch.");
    }
}
