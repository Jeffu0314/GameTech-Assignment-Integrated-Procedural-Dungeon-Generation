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

    [Header("Tiles")]
    public Tile[] tileObjects;
    public Tile startTile;
    public Tile bossTile;
    public Tile emptyTile;
    public float cellSpacing = 10f;


    void Start()
    {
        RunGeneration();
    }

    public void RunGeneration()
    {
        WFCGenerator wfc = new WFCGenerator();

        // 把 Inspector 的值传进去
        wfc.tileObjects = tileObjects;
        wfc.startTile = startTile;
        wfc.bossTile = bossTile;
        wfc.emptyTile = emptyTile;

        var result = wfc.Generate(size, seed, difficulty, enableBranches);

        Debug.Log("Generated: " + result.Count);

        Render(result);
    }

    void Render(Dictionary<Vector2Int, Tile> layout)
    {
        foreach (var kv in layout)
        {
            if (kv.Value == null || kv.Value.prefab == null)
            {
                Debug.LogError($"Missing tile or prefab at {kv.Key}");
                continue;
            }

            Vector3 pos = new Vector3(kv.Key.x * cellSpacing, 0, kv.Key.y * cellSpacing);
            Instantiate(kv.Value.prefab, pos, Quaternion.identity);
        }
    }
}
