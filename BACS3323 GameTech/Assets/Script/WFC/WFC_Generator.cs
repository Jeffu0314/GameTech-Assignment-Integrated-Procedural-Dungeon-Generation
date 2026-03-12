using System.Collections.Generic;
using UnityEngine;

public class WFC_Generator
{
    int width;
    int height;

    WFC_Cell[,] grid;

    List<RoomType> allStates = new List<RoomType>()
    {
        RoomType.Combat,
        RoomType.Elite,
        RoomType.Bonus
    };

    public RoomType[,] Generate(int w, int h)
    {
        width = w;
        height = h;

        grid = new WFC_Cell[w, h];

        InitializeGrid();

        while (!IsFullyCollapsed())
        {
            Vector2Int cellPos = GetLowestEntropyCell();
            CollapseCell(cellPos);
        }

        return ConvertToRoomTypeGrid();
    }

    void InitializeGrid()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                grid[x, y] = new WFC_Cell(allStates);
            }
        }
    }

    bool IsFullyCollapsed()
    {
        foreach (var cell in grid)
        {
            if (!cell.collapsed)
                return false;
        }

        return true;
    }

    Vector2Int GetLowestEntropyCell()
    {
        int minEntropy = int.MaxValue;
        Vector2Int chosen = Vector2Int.zero;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                WFC_Cell cell = grid[x, y];

                if (cell.collapsed)
                    continue;

                int entropy = cell.Entropy();

                if (entropy < minEntropy)
                {
                    minEntropy = entropy;
                    chosen = new Vector2Int(x, y);
                }
            }
        }

        return chosen;
    }

    void CollapseCell(Vector2Int pos)
    {
        WFC_Cell cell = grid[pos.x, pos.y];

        int index = Random.Range(0, cell.possibleStates.Count);
        RoomType chosen = cell.possibleStates[index];

        cell.possibleStates = new List<RoomType>() { chosen };
        cell.collapsed = true;
    }

    RoomType[,] ConvertToRoomTypeGrid()
    {
        RoomType[,] result = new RoomType[width, height];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                result[x, y] = grid[x, y].possibleStates[0];
            }
        }

        return result;
    }

}
