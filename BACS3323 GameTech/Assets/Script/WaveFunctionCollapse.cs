using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static Tile;
public class WaveFunctionCollapse : MonoBehaviour
{
    public DungeonConfig config;
    public int dimensions;
    public Tile[] tileObjects;
    public List<Cell> gridComponenets;
    public Cell cellObj;
    public Tile backupTile;
    public float cellSpacing = 10f;
    public Tile startTile;
    public Tile bossTile;
    private int iteration;
    //private int mainPathIndex = 0;
    List<Vector2Int> mainPath = new List<Vector2Int>();
    List<Vector2Int> branchSeeds = new List<Vector2Int>();
    Dictionary<Vector2Int, RoomType> roomMap = new();
    Dictionary<Vector2Int, Tile> collapsedTiles = new(); // 记录已放置的 tile
    private void Awake()
    {
        gridComponenets = new List<Cell>();
        InitializeGrid();
        GenerateAdjacencyRules();
        GenerateMainPath();
        PrintMainPath();
        AssignRooms();
        // 先强制 collapse 整条主路径
        CollapseMainPathFirst();
        StartCoroutine(CheckEntropy());
    }
    void InitializeGrid()
    {
        for (int y = 0; y < dimensions; y++)
        {
            for (int x = 0; x < dimensions; x++)
            {
                Cell newCell = Instantiate(cellObj, new Vector3(x * cellSpacing, 0, y * cellSpacing), Quaternion.identity);
                newCell.gridPos = new Vector2Int(x, y);
                newCell.CreateCell(false, tileObjects);
                gridComponenets.Add(newCell);
            }
        }
    }
    // ========== 关键修改：先处理整条主路径 ==========
    void CollapseMainPathFirst()
    {
        Debug.Log("=== COLLAPSING MAIN PATH ===");
        for (int i = 0; i < mainPath.Count; i++)
        {
            Vector2Int pos = mainPath[i];
            int index = pos.x + pos.y * dimensions;
            Cell cell = gridComponenets[index];
            Tile selected = null;
            // START
            if (i == 0)
            {
                selected = startTile;
                Debug.Log($"[{i}] START at {pos}");
            }
            // BOSS
            else if (i == mainPath.Count - 1)
            {
                selected = bossTile;
                Debug.Log($"[{i}] BOSS at {pos}");
            }
            // 中间路径
            else
            {
                selected = PickMainPathTile(pos, i);
                Debug.Log($"[{i}] PATH at {pos} -> {(selected != null ? selected.name : "NULL")}");
            }
            if (selected == null)
            {
                Debug.LogError($"❌ Failed to find tile for main path at {pos}");
                selected = backupTile;
            }
            // 放置 tile
            cell.collapsed = true;
            cell.tileOptions = new Tile[] { selected };
            collapsedTiles[pos] = selected;
            Tile spawned = Instantiate(selected, cell.transform.position, Quaternion.identity);
            if (roomMap.ContainsKey(pos))
            {
                spawned.roomType = roomMap[pos];
            }
        }
        // 主路径完成后，传播约束到相邻格子
        PropagateAllConstraints();
        Debug.Log("=== MAIN PATH COMPLETE ===");
    }
    // 为主路径选择 tile（必须连接前后节点）
    Tile PickMainPathTile(Vector2Int pos, int pathIndex)
    {
        List<Vector2Int> requiredDirs = new List<Vector2Int>();
        // 必须连接前一个
        if (pathIndex > 0)
        {
            Vector2Int prev = mainPath[pathIndex - 1];
            requiredDirs.Add(prev - pos);
        }
        // 必须连接后一个
        if (pathIndex < mainPath.Count - 1)
        {
            Vector2Int next = mainPath[pathIndex + 1];
            requiredDirs.Add(next - pos);
        }
        // 过滤 tile
        List<Tile> valid = tileObjects.Where(t =>
        {
            foreach (var dir in requiredDirs)
            {
                if (!t.HasConnection(dir))
                    return false;
            }
            return true;
        }).ToList();
        // 考虑房间类型
        if (roomMap.ContainsKey(pos))
        {
            RoomType rt = roomMap[pos];
            valid = valid.Where(t => t.allowedRoomTypes.Contains(rt)).ToList();
        }
        if (valid.Count == 0)
        {
            Debug.LogError($"❌ No valid tile for main path at {pos}, required dirs: {string.Join(",", requiredDirs)}");
            return null;
        }
        return valid[UnityEngine.Random.Range(0, valid.Count)];
    }
    // 传播所有已 collapse 的约束
    void PropagateAllConstraints()
    {
        for (int y = 0; y < dimensions; y++)
        {
            for (int x = 0; x < dimensions; x++)
            {
                int index = x + y * dimensions;
                Cell cell = gridComponenets[index];
                if (cell.collapsed) continue;
                List<Tile> options = new List<Tile>(tileObjects);
                // UP
                if (y > 0)
                {
                    Cell neighbor = gridComponenets[x + (y - 1) * dimensions];
                    if (neighbor.collapsed && neighbor.tileOptions.Length > 0)
                    {
                        Tile nt = neighbor.tileOptions[0];
                        options = options.Where(t => nt.downNeighbours.Contains(t)).ToList();
                    }
                }
                // DOWN
                if (y < dimensions - 1)
                {
                    Cell neighbor = gridComponenets[x + (y + 1) * dimensions];
                    if (neighbor.collapsed && neighbor.tileOptions.Length > 0)
                    {
                        Tile nt = neighbor.tileOptions[0];
                        options = options.Where(t => nt.upNeighbours.Contains(t)).ToList();
                    }
                }
                // LEFT
                if (x > 0)
                {
                    Cell neighbor = gridComponenets[(x - 1) + y * dimensions];
                    if (neighbor.collapsed && neighbor.tileOptions.Length > 0)
                    {
                        Tile nt = neighbor.tileOptions[0];
                        options = options.Where(t => nt.rightNeighbours.Contains(t)).ToList();
                    }
                }
                // RIGHT
                if (x < dimensions - 1)
                {
                    Cell neighbor = gridComponenets[(x + 1) + y * dimensions];
                    if (neighbor.collapsed && neighbor.tileOptions.Length > 0)
                    {
                        Tile nt = neighbor.tileOptions[0];
                        options = options.Where(t => nt.leftNeighbours.Contains(t)).ToList();
                    }
                }
                if (options.Count == 0)
                {
                    Debug.LogWarning($"⚠️ Cell {x},{y} has no valid options after propagation");
                    options = new List<Tile>(tileObjects);
                }
                cell.RecreateCell(options.ToArray());
            }
        }
    }
    IEnumerator CheckEntropy()
    {
        List<Cell> tempGrid = gridComponenets.Where(c => !c.collapsed).ToList();
        if (tempGrid.Count == 0)
        {
            Debug.Log("🎉 GENERATION COMPLETE");
            ValidateConnectivity();
            yield break;
        }
        // 按熵排序
        tempGrid.Sort((a, b) => a.tileOptions.Length - b.tileOptions.Length);
        int minEntropy = tempGrid[0].tileOptions.Length;
        tempGrid = tempGrid.Where(c => c.tileOptions.Length == minEntropy).ToList();
        yield return new WaitForSeconds(0.02f);
        CollapseCell(tempGrid);
    }
    void CollapseCell(List<Cell> tempGrid)
    {
        if (tempGrid.Count == 0)
        {
            Debug.Log("❌ tempGrid EMPTY");
            return;
        }

        Cell cell = tempGrid[UnityEngine.Random.Range(0, tempGrid.Count)];
        Vector2Int pos = cell.gridPos;

        if (cell.tileOptions.Length == 0)
        {
            Debug.LogError("❌ CONTRADICTION → RESTART");
            RestartGeneration();
            return;
        }

        Tile selected = cell.tileOptions[UnityEngine.Random.Range(0, cell.tileOptions.Length)];
        cell.collapsed = true;
        cell.tileOptions = new Tile[] { selected };
        collapsedTiles[pos] = selected;
        Tile spawned = Instantiate(selected, cell.transform.position, Quaternion.identity);

        if (roomMap.ContainsKey(pos))
        {
            spawned.roomType = roomMap[pos];
        }

        Debug.Log($"Collapsed {pos} -> {selected.name}");
        UpdateGeneration();
    }

    void UpdateGeneration()
    {
        PropagateAllConstraints();
        iteration++;
        int remaining = gridComponenets.Count(c => !c.collapsed);
        Debug.Log($"Iteration {iteration}, remaining: {remaining}");
        if (remaining > 0)
        {
            StartCoroutine(CheckEntropy());
        }
        else
        {
            Debug.Log("🎉 GENERATION COMPLETE");
            ValidateConnectivity();
        }
    }
    // ========== 验证连通性 ==========
    void ValidateConnectivity()
    {
        Debug.Log("=== VALIDATING CONNECTIVITY ===");
        HashSet<Vector2Int> visited = new HashSet<Vector2Int>();
        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        Vector2Int start = mainPath[0];
        queue.Enqueue(start);
        visited.Add(start);
        while (queue.Count > 0)
        {
            Vector2Int current = queue.Dequeue();
            if (!collapsedTiles.ContainsKey(current)) continue;
            Tile tile = collapsedTiles[current];
            // 检查四个方向
            CheckConnection(current, Vector2Int.up, tile.up, visited, queue);
            CheckConnection(current, Vector2Int.down, tile.down, visited, queue);
            CheckConnection(current, Vector2Int.left, tile.left, visited, queue);
            CheckConnection(current, Vector2Int.right, tile.right, visited, queue);
        }
        Vector2Int boss = mainPath[mainPath.Count - 1];
        if (visited.Contains(boss))
        {
            Debug.Log("✅ START -> BOSS 连通!");
        }
        else
        {
            Debug.LogError("❌ START -> BOSS 不连通!");
        }
        Debug.Log($"Visited {visited.Count} cells");
    }
    void CheckConnection(Vector2Int current, Vector2Int dir, bool hasConnection, HashSet<Vector2Int> visited, Queue<Vector2Int> queue)
    {
        if (!hasConnection) return;
        Vector2Int neighbor = current + dir;
        if (neighbor.x < 0 || neighbor.x >= dimensions || neighbor.y < 0 || neighbor.y >= dimensions)
            return;
        if (visited.Contains(neighbor)) return;
        if (!collapsedTiles.ContainsKey(neighbor)) return;
        Tile neighborTile = collapsedTiles[neighbor];
        // 检查邻居是否有反向连接
        Vector2Int reverseDir = -dir;
        if (neighborTile.HasConnection(reverseDir))
        {
            visited.Add(neighbor);
            queue.Enqueue(neighbor);
        }
    }
    void GenerateAdjacencyRules()
    {
        foreach (Tile tile in tileObjects)
        {
            List<Tile> upList = new();
            List<Tile> downList = new();
            List<Tile> leftList = new();
            List<Tile> rightList = new();
            foreach (Tile other in tileObjects)
            {
                // UP: 当前 tile 上方开口，对方下方开口 OR 双方都封闭
                if (tile.up == other.down)
                    upList.Add(other);
                if (tile.down == other.up)
                    downList.Add(other);
                if (tile.left == other.right)
                    leftList.Add(other);
                if (tile.right == other.left)
                    rightList.Add(other);
            }
            tile.upNeighbours = upList.ToArray();
            tile.downNeighbours = downList.ToArray();
            tile.leftNeighbours = leftList.ToArray();
            tile.rightNeighbours = rightList.ToArray();
        }
    }
    void GenerateMainPath()
    {
        mainPath.Clear();
        Vector2Int current = new Vector2Int(0, 0);
        mainPath.Add(current);
        int steps = config.mainPathLength;
        int attempts = 0;
        int maxAttempts = steps * 10;
        while (mainPath.Count < steps + 1 && attempts < maxAttempts)
        {
            attempts++;
            List<Vector2Int> possible = new List<Vector2Int>();
            // 优先向右和向上（朝 boss 方向）
            if (current.x < dimensions - 1)
                possible.Add(Vector2Int.right);
            if (current.y < dimensions - 1)
                possible.Add(Vector2Int.up);
            // 偶尔允许回退
            if (UnityEngine.Random.value < 0.2f)
            {
                if (current.x > 0)
                    possible.Add(Vector2Int.left);
                if (current.y > 0)
                    possible.Add(Vector2Int.down);
            }
            if (possible.Count == 0) break;
            Vector2Int dir = possible[UnityEngine.Random.Range(0, possible.Count)];
            Vector2Int next = current + dir;
            if (!mainPath.Contains(next))
            {
                mainPath.Add(next);
                current = next;
            }
        }
        Debug.Log($"Main path length: {mainPath.Count}");
    }
    void AssignRooms()
    {
        roomMap[mainPath[0]] = RoomType.Corridor;
        roomMap[mainPath[mainPath.Count - 1]] = RoomType.Boss;
        List<Vector2Int> middle = mainPath.Skip(1).Take(mainPath.Count - 2).ToList();
        Shuffle(middle);
        int idx = 0;
        for (int i = 0; i < config.combatRooms && idx < middle.Count; i++)
            roomMap[middle[idx++]] = RoomType.Combat;
        for (int i = 0; i < config.eliteRooms && idx < middle.Count; i++)
            roomMap[middle[idx++]] = RoomType.Elite;
        for (int i = 0; i < config.bonusRooms && idx < middle.Count; i++)
            roomMap[middle[idx++]] = RoomType.Bonus;
        foreach (var p in middle)
        {
            if (!roomMap.ContainsKey(p))
                roomMap[p] = RoomType.Corridor;
        }
    }
    void Shuffle<T>(List<T> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int r = UnityEngine.Random.Range(i, list.Count);
            (list[i], list[r]) = (list[r], list[i]);
        }
    }
    void PrintMainPath()
    {
        Debug.Log("=== MAIN PATH ===");

        for (int i = 0; i < mainPath.Count; i++)
        {
            string label = i == 0 ? "(START)" : i == mainPath.Count - 1 ? "(BOSS)" : "";
            Debug.Log($"[{i}] {mainPath[i]} {label}");
        }
    }

    void RestartGeneration()
    {
        StopAllCoroutines();

        foreach (var cell in gridComponenets)
            Destroy(cell.gameObject);

        gridComponenets.Clear();
        collapsedTiles.Clear();

        iteration = 0;

        Awake(); // 重来
    }
}