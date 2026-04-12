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

    // Specify tile
    public WFCTile startTile;
    public WFCTile bossTile;

    void Start()
    {
        GenerateUntilValid();
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
            if (!hasContradiction && DungeonValidator.IsConnected(grid, width, height))
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

                Vector3 pos = new Vector3(x * 10, 0, y * 10);

                Instantiate(tile.prefab, pos, Quaternion.identity, transform);
            }
        }
    }

    // Initialize the grid with all possible tiles in each cell
    void InitializeGrid()
    {
        grid = new WFC_Cell[width, height];

        mainPath = GenerateMainPath();

        // 1️⃣ 先全部填满 tile options
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                grid[x, y] = new WFC_Cell(new List<WFCTile>(allTiles));
            }
        }

        // 2️⃣ 强制路径（关键）
        BuildMainPath();
    }

    // Collapse a random cell with the lowest entropy (fewest possible tiles)
    void CollapseRandomCell()
    {
        List<Vector2Int> candidates = new List<Vector2Int>();

        int lowestEntropy = int.MaxValue;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                WFC_Cell cell = grid[x, y];

                if (cell.collapsed) continue;

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

        if (nx < 0 || ny < 0 || nx >= width || ny >= height)
            return;

        WFC_Cell neighbor = grid[nx, ny];

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
        }
    }

    // Check if two tiles are compatible in a given direction
    bool Compatible(WFCTile a, WFCTile b, Vector2Int dir)
    {
        if (dir == Vector2Int.up)
            return a.up == b.down;

        if (dir == Vector2Int.down)
            return a.down == b.up;

        if (dir == Vector2Int.left)
            return a.left == b.right;

        if (dir == Vector2Int.right)
            return a.right == b.left;

        return false;
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
                chosen = startTile;
            else if (i == mainPath.Count - 1)
                chosen = bossTile;

            grid[current.x, current.y] = new WFC_Cell(new List<WFCTile> { chosen });
            grid[current.x, current.y].collapsed = true;

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
}
