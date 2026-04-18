using System.Collections.Generic;
using System.Linq;
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

    Dictionary<Vector2Int, RoomData> roomDataMap = new();
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

        PlaceGameplayContent(result);

        Debug.Log("Gameplay Placed: " + result.Count);

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

            if (roomDataMap.ContainsKey(kv.Key))
            {
                var content = roomDataMap[kv.Key].content;
                Debug.Log($"{kv.Key} -> {content}");
            }

            Vector3 pos = new Vector3(kv.Key.x * cellSpacing, 4, kv.Key.y * cellSpacing);

            var go = Instantiate(kv.Value.prefab, pos, Quaternion.identity);
            spawned.Add(go);
        }
    }


    // =========================
    // Content Placement
    // =========================
    void PlaceGameplayContent(Dictionary<Vector2Int, Tile> layout)
    {
        roomDataMap.Clear();

        Dictionary<RoomData.RoomContentType, int> used = new();

        foreach (RoomData.RoomContentType type in System.Enum.GetValues(typeof(RoomData.RoomContentType)))
        {
            used[type] = 0;
        }

        used[RoomData.RoomContentType.Combat] = 0;
        used[RoomData.RoomContentType.Treasure] = 0;
        used[RoomData.RoomContentType.Trap] = 0;

        List<Vector2Int> rooms = layout.Keys
            .Where(p => layout[p].tileType != Tile.TileType.Start &&
                        layout[p].tileType != Tile.TileType.Boss)
            .OrderBy(x => Random.value)
            .ToList();

        foreach (var pos in rooms)
        {
            var tile = layout[pos];

            var content = DecideContentType(used);

            RoomData data = new RoomData();
            data.pos = pos;
            data.tile = tile;
            data.content = content;

            roomDataMap[pos] = data;

            used[content]++;
        }
    }

    RoomData.RoomContentType DecideContentType(Dictionary<RoomData.RoomContentType, int> used)
    {
        List<RoomData.RoomContentType> candidates = new();

        if (used[RoomData.RoomContentType.Combat] < maxCombat)
            candidates.Add(RoomData.RoomContentType.Combat);

        if (used[RoomData.RoomContentType.Treasure] < maxTreasure)
            candidates.Add(RoomData.RoomContentType.Treasure);

        if (used[RoomData.RoomContentType.Trap] < maxTrap)
            candidates.Add(RoomData.RoomContentType.Trap);

        if (candidates.Count == 0)
            return RoomData.RoomContentType.Empty;

        return candidates[Random.Range(0, candidates.Count)];
    }
}
