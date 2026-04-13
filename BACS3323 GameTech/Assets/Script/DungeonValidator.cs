using System.Collections.Generic;
using UnityEngine;

public class DungeonValidator
{
    public static bool IsReachable(WFC_Cell[,] grid, int width, int height)
    {
        bool[,] visited = new bool[width, height];

        Vector2Int start = Find(grid, width, height, WFCTile.RoomType.Start);
        Vector2Int boss = Find(grid, width, height, WFCTile.RoomType.Boss);

        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        queue.Enqueue(start);
        visited[start.x, start.y] = true;

        while (queue.Count > 0)
        {
            Vector2Int pos = queue.Dequeue();

            if (pos == boss)
                return true;

            WFCTile tile = grid[pos.x, pos.y].possibleTiles[0];

            Try(pos, Vector2Int.up, tile, grid, visited, queue);
            Try(pos, Vector2Int.down, tile, grid, visited, queue);
            Try(pos, Vector2Int.left, tile, grid, visited, queue);
            Try(pos, Vector2Int.right, tile, grid, visited, queue);
        }

        return false;
    }

    static Vector2Int Find(WFC_Cell[,] grid, int w, int h, WFCTile.RoomType type)
    {
        for (int x = 0; x < w; x++)
        {
            for (int y = 0; y < h; y++)
            {
                if (grid[x, y].possibleTiles.Count == 0) continue;

                if (grid[x, y].possibleTiles[0].roomType == type)
                    return new Vector2Int(x, y);
            }
        }
        return Vector2Int.zero;
    }

    static void Try(Vector2Int pos, Vector2Int dir, WFCTile tile,
        WFC_Cell[,] grid, bool[,] visited, Queue<Vector2Int> q)
    {
        int nx = pos.x + dir.x;
        int ny = pos.y + dir.y;

        if (nx < 0 || ny < 0 || nx >= grid.GetLength(0) || ny >= grid.GetLength(1))
            return;

        if (visited[nx, ny])
            return;

        WFCTile nt = grid[nx, ny].possibleTiles[0];

        bool ok =
            (dir == Vector2Int.up && tile.up && nt.down) ||
            (dir == Vector2Int.down && tile.down && nt.up) ||
            (dir == Vector2Int.left && tile.left && nt.right) ||
            (dir == Vector2Int.right && tile.right && nt.left);

        if (!ok) return;

        visited[nx, ny] = true;
        q.Enqueue(new Vector2Int(nx, ny));
    }
}