using System.Collections.Generic;
using UnityEngine;

public class WFCGenerator : MonoBehaviour
{
    public int width = 5;
    public int height = 5;

    public WFCTile[] allTiles;

    private WFC_Cell[,] grid;

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
    void Propagate(int x, int y)
    {
        WFCTile current = grid[x, y].possibleTiles[0];

        CheckNeighbor(x, y + 1, tile => tile.down == current.up);
        CheckNeighbor(x, y - 1, tile => tile.up == current.down);
        CheckNeighbor(x - 1, y, tile => tile.right == current.left);
        CheckNeighbor(x + 1, y, tile => tile.left == current.right);
    }
    // Check a neighbor cell and remove incompatible tiles based on the given rule
    void CheckNeighbor(int x, int y, System.Predicate<WFCTile> rule)
    {
        if (x < 0 || y < 0 || x >= width || y >= height)
            return;

        WFC_Cell neighbor = grid[x, y];

        if (neighbor.collapsed)
            return;

        neighbor.possibleTiles.RemoveAll(tile => !rule(tile));
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
