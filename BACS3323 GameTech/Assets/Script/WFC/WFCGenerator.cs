using System.Collections.Generic;
using UnityEngine;

public class WFCGenerator : MonoBehaviour
{
    public int width = 5;
    public int height = 5;
    public WFCTile[] allTiles;
    private WFC_Cell[,] grid;
    List<Vector2Int> mainPath;
    bool hasContradiction;

    public WFCTile startTile;
    public WFCTile bossTile;

    void Start() { GenerateUntilValid(); }

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
                if (hasContradiction) break;
            }

            if (!hasContradiction) // 注意：Validator 内部也需要同步修改
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
            for (int y = 0; y < height; y++)
                if (!grid[x, y].collapsed) return false;
        return true;
    }

    void SpawnDungeon()
    {
        foreach (Transform child in transform) Destroy(child.gameObject);
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (grid[x, y].possibleOptions.Count == 0) continue;
                TileOption selected = grid[x, y].possibleOptions[0];
                Vector3 pos = new Vector3(x * 10, 0, y * 10);
                Quaternion rot = Quaternion.Euler(0, selected.rotationIndex * 90f, 0);
                Instantiate(selected.tile.prefab, pos, rot, transform);
            }
        }
    }

    void InitializeGrid()
    {
        grid = new WFC_Cell[width, height];
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                grid[x, y] = new WFC_Cell(new List<WFCTile>(allTiles));
                // 边界剔除逻辑
                if (y == height - 1) grid[x, y].possibleOptions.RemoveAll(o => o.connectivity.up);
                if (y == 0) grid[x, y].possibleOptions.RemoveAll(o => o.connectivity.down);
                if (x == 0) grid[x, y].possibleOptions.RemoveAll(o => o.connectivity.left);
                if (x == width - 1) grid[x, y].possibleOptions.RemoveAll(o => o.connectivity.right);
            }
        }
        mainPath = GenerateMainPath();
        BuildMainPath();
    }

    void CollapseRandomCell()
    {
        List<Vector2Int> candidates = new List<Vector2Int>();
        int lowestEntropy = int.MaxValue;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (grid[x, y].collapsed) continue;
                int entropy = grid[x, y].possibleOptions.Count;
                if (entropy < lowestEntropy)
                {
                    lowestEntropy = entropy;
                    candidates.Clear();
                    candidates.Add(new Vector2Int(x, y));
                }
                else if (entropy == lowestEntropy) candidates.Add(new Vector2Int(x, y));
            }
        }

        if (candidates.Count == 0) return;

        Vector2Int chosen = candidates[Random.Range(0, candidates.Count)];
        WFC_Cell chosenCell = grid[chosen.x, chosen.y];

        // 修复点：调用正确的随机权重函数
        TileOption selected = GetWeightedRandomOption(chosenCell.possibleOptions);

        if (selected == null) { hasContradiction = true; return; }

        chosenCell.possibleOptions = new List<TileOption> { selected };
        chosenCell.collapsed = true;

        DrawCellDebug(chosen.x, chosen.y, selected.connectivity);
        Propagate(chosen.x, chosen.y);
    }

    // 缺失的权重随机函数补全
    TileOption GetWeightedRandomOption(List<TileOption> options)
    {
        if (options == null || options.Count == 0) return null;
        float totalWeight = 0;
        foreach (var o in options) totalWeight += o.tile.weight;
        float rnd = Random.Range(0, totalWeight);
        float current = 0;
        foreach (var o in options)
        {
            current += o.tile.weight;
            if (rnd <= current) return o;
        }
        return options[0];
    }

    void Propagate(int startX, int startY)
    {
        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        queue.Enqueue(new Vector2Int(startX, startY));

        while (queue.Count > 0)
        {
            Vector2Int pos = queue.Dequeue();
            // 修复点：currentTile 现在是 TileOption
            TileOption currentOption = grid[pos.x, pos.y].possibleOptions[0];

            TryReduceNeighbor(pos, Vector2Int.up, currentOption, queue);
            TryReduceNeighbor(pos, Vector2Int.down, currentOption, queue);
            TryReduceNeighbor(pos, Vector2Int.left, currentOption, queue);
            TryReduceNeighbor(pos, Vector2Int.right, currentOption, queue);
        }
    }

    void TryReduceNeighbor(Vector2Int pos, Vector2Int dir, TileOption currentTile, Queue<Vector2Int> queue)
    {
        int nx = pos.x + dir.x;
        int ny = pos.y + dir.y;
        if (nx < 0 || ny < 0 || nx >= width || ny >= height) return;

        WFC_Cell neighbor = grid[nx, ny];
        if (neighbor.collapsed) return;

        int before = neighbor.possibleOptions.Count;
        // 修复点：使用 TileOption 的兼容性判断
        neighbor.possibleOptions.RemoveAll(opt => !Compatible(currentTile, opt, dir));

        int after = neighbor.possibleOptions.Count;
        if (after == 0) { hasContradiction = true; return; }
        if (after < before) queue.Enqueue(new Vector2Int(nx, ny));
        if (after == 1) neighbor.collapsed = true;
    }

    bool Compatible(TileOption a, TileOption b, Vector2Int dir)
    {
        if (dir == Vector2Int.up) return a.connectivity.up == b.connectivity.down;
        if (dir == Vector2Int.down) return a.connectivity.down == b.connectivity.up;
        if (dir == Vector2Int.left) return a.connectivity.left == b.connectivity.right;
        if (dir == Vector2Int.right) return a.connectivity.right == b.connectivity.left;
        return false;
    }

    void DrawCellDebug(int x, int y, Connectivity c)
    {
        Vector3 center = new Vector3(x * 10, 2, y * 10);
        if (c.up) Debug.DrawLine(center, center + Vector3.forward * 4, Color.green, 5f);
        if (c.down) Debug.DrawLine(center, center + Vector3.back * 4, Color.green, 5f);
        if (c.left) Debug.DrawLine(center, center + Vector3.left * 4, Color.green, 5f);
        if (c.right) Debug.DrawLine(center, center + Vector3.right * 4, Color.green, 5f);
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

            Vector2Int? prev = i > 0 ? mainPath[i - 1] : (Vector2Int?)null;
            Vector2Int? next = i < mainPath.Count - 1 ? mainPath[i + 1] : (Vector2Int?)null;

            WFCTile chosen = GetPathTile(prev, current, next);

            if (i == 0)
            {
                Vector2Int nextDir = mainPath[i + 1] - current;
                chosen = GetStartTile(nextDir);
            }
            else if (i == mainPath.Count - 1)
            {
                Vector2Int prevDir = mainPath[i - 1] - current;
                chosen = GetBossTile(prevDir);
            }

            // 强制设置该 Cell
            grid[current.x, current.y].possibleOptions = new List<TileOption> ();
            grid[current.x, current.y].collapsed = true;

            // 【关键修复】：每设置一个路径点，立即传播约束，限制邻居的可能类型
            Propagate(current.x, current.y);

        }
    }

    WFCTile GetPathTile(Vector2Int? prev, Vector2Int current, Vector2Int? next)
    {
        foreach (var tile in allTiles)
        {
            bool valid = true;

            if (prev.HasValue)
            {
                Vector2Int dir = prev.Value - current;

                if (dir == Vector2Int.up && !tile.up) valid = false;
                if (dir == Vector2Int.down && !tile.down) valid = false;
                if (dir == Vector2Int.left && !tile.left) valid = false;
                if (dir == Vector2Int.right && !tile.right) valid = false;
            }

            if (next.HasValue)
            {
                Vector2Int dir = next.Value - current;

                if (dir == Vector2Int.up && !tile.up) valid = false;
                if (dir == Vector2Int.down && !tile.down) valid = false;
                if (dir == Vector2Int.left && !tile.left) valid = false;
                if (dir == Vector2Int.right && !tile.right) valid = false;
            }

            if (valid)
                return tile;
        }

        Debug.LogError("No path tile found!");
        return allTiles[0];
    }

    WFCTile GetStartTile(Vector2Int dir)
    {
        foreach (var tile in allTiles)
        {
            if (tile == startTile) // optional: limit to start variants
            {
                if (dir == Vector2Int.right && tile.right) return tile;
                if (dir == Vector2Int.left && tile.left) return tile;
                if (dir == Vector2Int.up && tile.up) return tile;
                if (dir == Vector2Int.down && tile.down) return tile;
            }
        }

        Debug.LogError("No valid start tile!");
        return startTile;
    }

    WFCTile GetBossTile(Vector2Int dir)
    {
        foreach (var tile in allTiles)
        {
            if (tile == bossTile)
            {
                if (dir == Vector2Int.right && tile.right) return tile;
                if (dir == Vector2Int.left && tile.left) return tile;
                if (dir == Vector2Int.up && tile.up) return tile;
                if (dir == Vector2Int.down && tile.down) return tile;
            }
        }

        Debug.LogError("No valid boss tile!");
        return bossTile;
    }
}
