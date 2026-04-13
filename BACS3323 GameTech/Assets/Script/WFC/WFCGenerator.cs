using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEditor.PlayerSettings;
using static WFCTile;

public class WFCGenerator : MonoBehaviour
{
    public int width = 5;
    public int height = 5;

    public WFCTile[] allTiles;

    private WFC_Cell[,] grid;

    public List<WFCTile> normalTiles;


    List<Vector2Int> mainPath;
    HashSet<Vector2Int> mainPathSet;

    bool hasContradiction;

    // Specify tile
    public WFCTile startTile;
    public WFCTile bossTile;

    void Awake()
    {
        normalTiles = new List<WFCTile>();

        foreach (var t in allTiles)
        {
            if (t.roomType == WFCTile.RoomType.Corridor ||
                t.roomType == WFCTile.RoomType.Combat ||
                t.roomType == WFCTile.RoomType.Treasure)
            {
                normalTiles.Add(t);
            }
        }
    }


    void Start()
    {
        DebugTileStats();
        GenerateUntilValid();
        AssignRoomTypes();
    }

    void DebugTileStats()
    {
        int up = 0, down = 0, left = 0, right = 0;

        foreach (var t in allTiles)
        {
            if (t.up) up++;
            if (t.down) down++;
            if (t.left) left++;
            if (t.right) right++;
        }

        Debug.Log($"UP:{up} DOWN:{down} LEFT:{left} RIGHT:{right}");
    }

    void GenerateUntilValid()
    {
        hasContradiction = false;

        int attempts = 0;

        while (attempts < 50)
        {
            attempts++;

            InitializeGrid();

            while (!IsFinished())
            {
                CollapseRandomCell();

                if (hasContradiction)
                    break;
            }

            //only spawn if valid
            if (!hasContradiction && DungeonValidator.IsReachable(grid, width, height))
            {
                Debug.Log("Valid dungeon generated!");
                SpawnDungeon();
                return;
            }

            Debug.Log("Retry " + attempts);
        }

        Debug.LogError("Failed to generate valid dungeon");
    }

    bool IsFinished()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (!grid[x, y].collapsed)
                    return false;
            }
        }

        return true;
    }

    void SpawnDungeon()
    {
        foreach (Transform child in transform)
            Destroy(child.gameObject);

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (grid[x, y].possibleTiles.Count == 0)
                    continue;

                WFCTile tile = grid[x, y].possibleTiles[0];

                if (tile.prefab == null)
                {
                    Debug.LogError($"Missing prefab on tile: {tile.tileName}");
                    continue;
                }

                Vector3 pos = new Vector3(x * 10, 3, y * 10);

                RoomType type = grid[x, y].assignedType;

                GameObject prefabToSpawn = GetPrefabByType(type, tile);

                Instantiate(prefabToSpawn, pos, Quaternion.identity, transform);
            }
        }
    }

    // Initialize the grid with all possible tiles in each cell
    void InitializeGrid()
    {
        grid = new WFC_Cell[width, height];

        mainPath = GenerateMainPath();
        mainPathSet = new HashSet<Vector2Int>(mainPath);

        // 1️⃣ 先全部填满 tile options
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                grid[x, y] = new WFC_Cell(new List<WFCTile>(normalTiles));
            }
        }

        // 2️⃣ 强制路径（关键）
        BuildMainPath();

        foreach (var pos in mainPath)
        {
            Propagate(pos.x, pos.y);

            if (hasContradiction)
                return;
        }
    }

    // Collapse a random cell with the lowest entropy (fewest possible tiles)
    void CollapseRandomCell()
    {
        List<Vector2Int> candidates = new List<Vector2Int>();
        mainPathSet = new HashSet<Vector2Int>(mainPath);

        int lowestEntropy = int.MaxValue;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                WFC_Cell cell = grid[x, y];

                if (mainPathSet.Contains(new Vector2Int(x, y)))
                    continue;

                if (cell.collapsed || cell.locked) continue;

                int entropy = cell.possibleTiles.Count;

                if (entropy < lowestEntropy)
                {
                    lowestEntropy = entropy;
                    candidates.Clear();
                    candidates.Add(new Vector2Int(x, y));
                }
                else if (entropy == lowestEntropy)
                {
                    candidates.Add(new Vector2Int(x, y));
                }
            }
        }

        if (candidates.Count == 0)
            return;

        Vector2Int chosen = candidates[Random.Range(0, candidates.Count)];
        WFC_Cell chosenCell = grid[chosen.x, chosen.y];

        WFCTile tile = GetWeightedRandom(chosenCell.possibleTiles);

        if (tile == null)
        {
            Debug.Log("No valid tile -> restart");
            return;
        }

        chosenCell.possibleTiles = new List<WFCTile> { tile };
        chosenCell.collapsed = true;

        Propagate(chosen.x, chosen.y);
    }

    // Propagate constraints to neighbors after collapsing a cell
    void Propagate(int startX, int startY)
    {
        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        queue.Enqueue(new Vector2Int(startX, startY));

        while (queue.Count > 0)
        {
            Vector2Int pos = queue.Dequeue();
            WFCTile currentTile = grid[pos.x, pos.y].possibleTiles[0];

            TryReduceNeighbor(pos, Vector2Int.up, currentTile, queue);
            TryReduceNeighbor(pos, Vector2Int.down, currentTile, queue);
            TryReduceNeighbor(pos, Vector2Int.left, currentTile, queue);
            TryReduceNeighbor(pos, Vector2Int.right, currentTile, queue);
        }
    }

    void TryReduceNeighbor(Vector2Int pos, Vector2Int dir,
    WFCTile currentTile, Queue<Vector2Int> queue)
    {
        int nx = pos.x + dir.x;
        int ny = pos.y + dir.y;

        if (mainPathSet.Contains(new Vector2Int(nx, ny)))
            return;

        if (nx < 0 || ny < 0 || nx >= width || ny >= height)
            return;

        WFC_Cell neighbor = grid[nx, ny];

        if (neighbor.locked)
            return;

        if (neighbor.collapsed)
            return;

        int before = neighbor.possibleTiles.Count;

        neighbor.possibleTiles.RemoveAll(tile =>
            !Compatible(currentTile, tile, dir));

        int after = neighbor.possibleTiles.Count;

        // contradiction: impossible map
        if (after == 0)
        {
            hasContradiction = true;
            Debug.Log($"Reducing neighbor at {nx},{ny} from {before} to {after}");
            Debug.Log("Contradiction!");
            return;
        }

        if (after < before)
        {
            queue.Enqueue(new Vector2Int(nx, ny));
        }

        if (after == 1)
        {
            neighbor.collapsed = true;
            queue.Enqueue(new Vector2Int(nx, ny));
        }
    }

    // Check if two tiles are compatible in a given direction
    bool Compatible(WFCTile a, WFCTile b, Vector2Int dir)
    {
        if (dir == Vector2Int.up)
            return !(a.up && !b.down);

        if (dir == Vector2Int.down)
            return !(a.down && !b.up);

        if (dir == Vector2Int.left)
            return !(a.left && !b.right);

        if (dir == Vector2Int.right)
            return !(a.right && !b.left);

        return true;
    }

    // Get a random tile from a list, weighted by their weight property
    WFCTile GetWeightedRandom(List<WFCTile> tiles)
    {
        if (tiles == null || tiles.Count == 0)
            return null;   // avoid crash

        float totalWeight = 0f;

        foreach (var t in tiles)
            totalWeight += t.weight;

        float random = Random.Range(0, totalWeight);

        float current = 0f;

        foreach (var t in tiles)
        {
            current += t.weight;

            if (random <= current)
                return t;
        }

        return tiles[0]; // fallback
    }

    WFCTile GetPathTile(Vector2Int prev, Vector2Int current, Vector2Int next)
    {
        Vector2Int inDir = current - prev;
        Vector2Int outDir = next - current;

        foreach (var tile in allTiles)
        {
            if (!Match(tile, inDir)) continue;
            if (!Match(tile, outDir)) continue;

            return tile;
        }

        return null;
    }

    bool Match(WFCTile t, Vector2Int dir)
    {
        if (dir == Vector2Int.up) return t.down;
        if (dir == Vector2Int.down) return t.up;
        if (dir == Vector2Int.left) return t.right;
        if (dir == Vector2Int.right) return t.left;
        return false;
    }

    List<Vector2Int> GenerateMainPath()
    {
        List<Vector2Int> path = new List<Vector2Int>();

        int x = 0;
        int y = 0;

        path.Add(new Vector2Int(x, y));

        while (x != width - 1 || y != height - 1)
        {
            if (Random.value < 0.5f && x < width - 1)
                x++;
            else if (y < height - 1)
                y++;

            path.Add(new Vector2Int(x, y));
        }

        return path;
    }

    void BuildMainPath()
    {
        for (int i = 0; i < mainPath.Count; i++)
        {
            Vector2Int current = mainPath[i];

            WFC_Cell cell = new WFC_Cell(new List<WFCTile>(normalTiles));

            if (i == 0)
            {
                Vector2Int next = mainPath[i + 1];
                Vector2Int dir = next - current;

                cell = new WFC_Cell(new List<WFCTile> { startTile });
            }
            else if (i == mainPath.Count - 1)
            {
                cell = new WFC_Cell(new List<WFCTile> { bossTile });
            }
            else
            {
                Vector2Int prev = mainPath[i - 1];
                Vector2Int next = mainPath[i + 1];

                WFCTile tile = PickDirectionalTile(prev, current);
                cell = new WFC_Cell(new List<WFCTile> { tile });
            }

            cell.collapsed = true;
            cell.locked = true;

            grid[current.x, current.y] = cell;
        }
    }


    WFCTile PickDirectionalTile(Vector2Int from, Vector2Int to)
    {
        Vector2Int dir = to - from;

        foreach (var tile in normalTiles)
        {
            if (dir == Vector2Int.up && tile.down && tile.up) return tile;
            if (dir == Vector2Int.down && tile.up && tile.down) return tile;
            if (dir == Vector2Int.left && tile.right && tile.left) return tile;
            if (dir == Vector2Int.right && tile.left && tile.right) return tile;
        }

        return normalTiles[0];
    }

    Dictionary<Vector2Int, int> ComputeDepthMap()
    {
        Dictionary<Vector2Int, int> depth = new Dictionary<Vector2Int, int>();

        Queue<Vector2Int> queue = new Queue<Vector2Int>();

        Vector2Int start = mainPath[0];

        queue.Enqueue(start);
        depth[start] = 0;

        while (queue.Count > 0)
        {
            Vector2Int pos = queue.Dequeue();
            int d = depth[pos];

            WFCTile tile = grid[pos.x, pos.y].possibleTiles[0];

            TryVisit(pos, Vector2Int.up, tile, queue, depth, d);
            TryVisit(pos, Vector2Int.down, tile, queue, depth, d);
            TryVisit(pos, Vector2Int.left, tile, queue, depth, d);
            TryVisit(pos, Vector2Int.right, tile, queue, depth, d);
        }

        return depth;
    }

    void TryVisit(Vector2Int pos, Vector2Int dir, WFCTile tile,
    Queue<Vector2Int> queue,
    Dictionary<Vector2Int, int> depth,
    int currentDepth)
    {
        int nx = pos.x + dir.x;
        int ny = pos.y + dir.y;

        if (nx < 0 || ny < 0 || nx >= width || ny >= height)
            return;

        Vector2Int next = new Vector2Int(nx, ny);

        if (depth.ContainsKey(next))
            return;

        WFCTile neighbor = grid[nx, ny].possibleTiles[0];

        bool canMove = false;

        if (dir == Vector2Int.up)
            canMove = tile.up && neighbor.down;
        else if (dir == Vector2Int.down)
            canMove = tile.down && neighbor.up;
        else if (dir == Vector2Int.left)
            canMove = tile.left && neighbor.right;
        else if (dir == Vector2Int.right)
            canMove = tile.right && neighbor.left;

        if (!canMove) return;

        depth[next] = currentDepth + 1;
        queue.Enqueue(next);
    }

    void AssignRoomTypes()
    {
        var depthMap = ComputeDepthMap();

        int maxDepth = 0;

        foreach (var d in depthMap.Values)
            if (d > maxDepth) maxDepth = d;

        foreach (var kv in depthMap)
        {
            Vector2Int pos = kv.Key;
            int depth = kv.Value;

            var cell = grid[pos.x, pos.y];

            // Start
            if (pos == mainPath[0])
            {
                cell.assignedType = RoomType.Start;
                continue;
            }

            // Boss
            if (pos == mainPath[mainPath.Count - 1])
            {
                cell.assignedType = RoomType.Boss;
                continue;
            }

            float normalizedDepth = (float)depth / maxDepth;

            float rand = Random.value;

            // 概率规则（你可以调）
            if (rand < 0.5f * normalizedDepth)
            {
                cell.assignedType = RoomType.Combat;
            }
            else if (rand < 0.3f)
            {
                cell.assignedType = RoomType.Treasure;
            }
            else
            {
                cell.assignedType = RoomType.Corridor;
            }
        }
    }

    GameObject GetPrefabByType(RoomType type, WFCTile tile)
    {
        switch (type)
        {
            case RoomType.Start:
                return startTile.prefab;

            case RoomType.Boss:
                return bossTile.prefab;

            case RoomType.Combat:
                return tile.prefab; // or combat prefab

            case RoomType.Treasure:
                return tile.prefab; // later换 treasure prefab

            default:
                return tile.prefab;
        }
    }
}
