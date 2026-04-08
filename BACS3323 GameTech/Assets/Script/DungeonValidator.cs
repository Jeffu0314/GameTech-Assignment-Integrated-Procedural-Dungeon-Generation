using System.Collections.Generic;
using UnityEngine;

public class DungeonValidator
{
    public static bool IsConnected(WFC_Cell[,] grid, int width, int height)
    {
        bool[,] visited = new bool[width, height];

        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        queue.Enqueue(new Vector2Int(0, 0));
        visited[0, 0] = true;

        while (queue.Count > 0)
        {
            Vector2Int pos = queue.Dequeue();
            WFCTile tile = grid[pos.x, pos.y].possibleTiles[0];

            CheckMove(pos, Vector2Int.up, tile.up, tile.down, grid, visited, queue, width, height);
            CheckMove(pos, Vector2Int.down, tile.down, tile.up, grid, visited, queue, width, height);
            CheckMove(pos, Vector2Int.left, tile.left, tile.right, grid, visited, queue, width, height);
            CheckMove(pos, Vector2Int.right, tile.right, tile.left, grid, visited, queue, width, height);
        }

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (!visited[x, y])
                    return false;
            }
        }

        return true;
    }


    static void CheckMove(
        Vector2Int pos,
        Vector2Int dir,
        bool currentHasDoor,
        bool unused,
        WFC_Cell[,] grid,
        bool[,] visited,
        Queue<Vector2Int> queue,
        int width,
        int height)
    {
        if (!currentHasDoor)
            return;

        int nx = pos.x + dir.x;
        int ny = pos.y + dir.y;

        if (nx < 0 || ny < 0 || nx >= width || ny >= height)
            return;

        if (visited[nx, ny])
            return;

        visited[nx, ny] = true;
        queue.Enqueue(new Vector2Int(nx, ny));
    }
}
