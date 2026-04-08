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
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                WFCTile tile = grid[x, y].possibleTiles[0];

                Vector3 pos = new Vector3(x * 10, 0, y * 10);

                Instantiate(tile.prefab, pos, Quaternion.identity);
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
            Vector2Int currentPos = queue.Dequeue();
            WFC_Cell currentCell = grid[currentPos.x, currentPos.y];

            if (!currentCell.collapsed)
                continue;

            WFCTile currentTile = currentCell.possibleTiles[0];

            Vector2Int[] directions =
            {
                Vector2Int.up,
                Vector2Int.down,
                Vector2Int.left,
                Vector2Int.right
            };

            foreach (Vector2Int dir in directions)
            {
                int nx = currentPos.x + dir.x;
                int ny = currentPos.y + dir.y;

                if (nx < 0 || ny < 0 || nx >= width || ny >= height)
                    continue;

                WFC_Cell neighbor = grid[nx, ny];

                if (neighbor.collapsed)
                    continue;

                int before = neighbor.possibleTiles.Count;

                neighbor.possibleTiles.RemoveAll(tile =>
                    !Compatible(currentTile, tile, dir));

                int after = neighbor.possibleTiles.Count;

                // contradiction
                if (after == 0)
                {
                    Debug.Log("Contradiction! Restart map.");
                    RunWFC();
                    return;
                }

                // if neighbor got reduced to one possible tile, collapse it and continue propagating
                if (after == 1 && before > 1)
                {
                    neighbor.collapsed = true;
                    queue.Enqueue(new Vector2Int(nx, ny));
                }
            }
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
