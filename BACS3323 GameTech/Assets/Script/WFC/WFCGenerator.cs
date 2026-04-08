using System.Collections.Generic;
using UnityEngine;

public class WFCGenerator : MonoBehaviour
{
    public int width = 5;
    public int height = 5;

    public WFCTile[] allTiles;

    private WFC_Cell[,] grid;

    public void Start()
    {
        RunWFC();
    }

    void RunWFC()
    {
        InitializeGrid();

        while (!IsFinished())
        {
            CollapseRandomCell();
        }

        if (!DungeonValidator.IsConnected(grid, width, height))
        {
            Debug.Log("Dungeon disconnected. Regenerating...");
            RunWFC();
            return;
        }

        SpawnDungeon();
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

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                grid[x, y] = new WFC_Cell(new List<WFCTile>(allTiles));
            }
        }
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

        WFCTile tile = chosenCell.possibleTiles[
            Random.Range(0, chosenCell.possibleTiles.Count)];

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
            Debug.Log("Contradiction detected. Regenerating...");

            foreach (Transform child in transform)
                Destroy(child.gameObject);

            RunWFC();
            return;
        }

        // if only one tile remains, collapse immediately
        if (after == 1 && before > 1)
        {
            neighbor.collapsed = true;
            queue.Enqueue(new Vector2Int(nx, ny));
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
}
