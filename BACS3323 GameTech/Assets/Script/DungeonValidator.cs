using System.Collections.Generic;
using UnityEngine;

public class DungeonValidator
{
 
    public static bool IsReachable(WFC_Cell[,] grid, int width, int height)
    {
        bool[,] visited = new bool[width, height];

        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        Vector2Int startPos = FindTile(grid, width, height, WFCTile.RoomType.Start);
        Vector2Int bossPos = FindTile(grid, width, height, WFCTile.RoomType.Boss);

        queue.Enqueue(startPos);
        visited[startPos.x, startPos.y] = true;
        visited[0, 0] = true;

        while (queue.Count > 0)
        {
            Vector2Int pos = queue.Dequeue();

            // 提前结束（找到 boss）
            if (pos == bossPos)
                return true;

            WFCTile tile = grid[pos.x, pos.y].possibleTiles[0];

            CheckMove(pos, Vector2Int.up, tile, grid, visited, queue, width, height);
            CheckMove(pos, Vector2Int.down, tile, grid, visited, queue, width, height);
            CheckMove(pos, Vector2Int.left, tile, grid, visited, queue, width, height);
            CheckMove(pos, Vector2Int.right, tile, grid, visited, queue, width, height);
        }

        return false; // 到不了 boss
    }

    static Vector2Int FindTile(WFC_Cell[,] grid, int width, int height, WFCTile.RoomType type)
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                var cell = grid[x, y];

                if (cell.possibleTiles.Count == 0) continue;

                if (cell.possibleTiles[0].roomType == type)
                    return new Vector2Int(x, y);
            }
        }

        return Vector2Int.zero;
    }


    static void CheckMove(Vector2Int pos, Vector2Int dir, WFCTile currentTile,
    WFC_Cell[,] grid, bool[,] visited, Queue<Vector2Int> queue, int width, int height)
    {
        int nx = pos.x + dir.x;
        int ny = pos.y + dir.y;

        if (nx < 0 || ny < 0 || nx >= width || ny >= height)
            return;

        if (visited[nx, ny])
            return;

        WFCTile neighborTile = grid[nx, ny].possibleTiles[0];

        bool canMove = false;

        if (dir == Vector2Int.up)
            canMove = currentTile.up && neighborTile.down;

        else if (dir == Vector2Int.down)
            canMove = currentTile.down && neighborTile.up;

        else if (dir == Vector2Int.left)
            canMove = currentTile.left && neighborTile.right;

        else if (dir == Vector2Int.right)
            canMove = currentTile.right && neighborTile.left;

        if (!canMove)
            return;

        visited[nx, ny] = true;
        queue.Enqueue(new Vector2Int(nx, ny));
    }
}
