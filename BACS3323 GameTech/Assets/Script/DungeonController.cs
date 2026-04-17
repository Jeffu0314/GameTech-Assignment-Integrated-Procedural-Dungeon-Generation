using System.Collections.Generic;
using UnityEngine;

public class DungeonController : MonoBehaviour
{
    [Header("WFC Settings")]
    public int size = 8;
    public int seed = 100;

    [Header("Difficulty")]
    [Range(0f, 1f)]
    public float difficulty = 0.5f;

    public bool enableBranches = true;

    [Header("Content Placement")]
    public int maxCombat = 1;
    public int maxTreasure = 1;
    public int maxTrap = 1;

    [Header("Tiles")]
    public Tile[] tileObjects;
    public Tile startTile;
    public Tile bossTile;
    public Tile emptyTile;
    public float cellSpacing = 10f;


    List<GameObject> spawned = new List<GameObject>();

    public void RunGeneration()
    {
        WFCGenerator wfc = new WFCGenerator();

        // 把 Inspector 的值传进去
        wfc.tileObjects = tileObjects;
        wfc.startTile = startTile;
        wfc.bossTile = bossTile;
        wfc.emptyTile = emptyTile;

        DungeonConfig config = new DungeonConfig();

        config.size = size;
        config.seed = seed;
        config.difficulty = difficulty;
        config.enableBranches = enableBranches;
        config.cellSpacing = cellSpacing;

        var result = DungeonAPI.GenerateDungeon(
            config,
            tileObjects,
            startTile,
            bossTile,
            emptyTile
        );

        Debug.Log("Generated: " + result.Count);

        Render(result);
    }

    void Render(Dictionary<Vector2Int, Tile> layout)
    {
        // ⭐ 清掉旧的
        foreach (var obj in spawned)
            Destroy(obj);

        spawned.Clear();

        foreach (var kv in layout)
        {
            if (kv.Value == null || kv.Value.prefab == null)
            {
                Debug.LogError($"Missing tile at {kv.Key}");
                continue;
            }

            Vector3 pos = new Vector3(kv.Key.x * cellSpacing, 4, kv.Key.y * cellSpacing);

            var go = Instantiate(kv.Value.prefab, pos, Quaternion.identity);
            spawned.Add(go);
        }
    }
}
