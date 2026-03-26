using System.Collections.Generic;
using UnityEngine;

public class DungeonGenerator : MonoBehaviour
{
    public int width;
    public int height;

    DungeonData dungeon;

    void Start()
    {
        GenerateDungeon();
    }

    void GenerateDungeon()
    {
        WFC_Generator wfc = new WFC_Generator();

        RoomType[,] dungeon = wfc.Generate(width, height);

        Vector2Int start = SetStartRoom(dungeon);
        SetBossRoom(dungeon, start);

        PrintDungeon(dungeon);
    }

    void PrintDungeon(RoomType[,] dungeon)
    {
        int w = dungeon.GetLength(0);
        int h = dungeon.GetLength(1);

        for (int y = 0; y < h; y++)
        {
            string row = "";

            for (int x = 0; x < w; x++)
            {
                row += dungeon[x, y].ToString()[0] + " ";
            }

            Debug.Log(row);
        }
    }

    Vector2Int SetStartRoom(RoomType[,] dungeon)
    {
        int w = dungeon.GetLength(0);
        int h = dungeon.GetLength(1);

        //random the starting room
        int x = Random.Range(0, w);
        int y = Random.Range(0, h);

        dungeon[x, y] = RoomType.Start;

        return new Vector2Int(x, y);
    }

    //find the most far room
    Vector2Int FindFurthestRoom(RoomType[,] dungeon, Vector2Int start)
    {
        int w = dungeon.GetLength(0);
        int h = dungeon.GetLength(1);

        bool[,] visited = new bool[w, h];
        Queue<Vector2Int> queue = new Queue<Vector2Int>();

        queue.Enqueue(start);
        visited[start.x, start.y] = true;

        Vector2Int furthest = start;

        Vector2Int[] directions = {
        Vector2Int.up, Vector2Int.down,
        Vector2Int.left, Vector2Int.right
    };

        while (queue.Count > 0)
        {
            Vector2Int current = queue.Dequeue();
            furthest = current;

            foreach (var dir in directions)
            {
                Vector2Int next = current + dir;

                if (next.x >= 0 && next.x < w &&
                    next.y >= 0 && next.y < h &&
                    !visited[next.x, next.y])
                {
                    visited[next.x, next.y] = true;
                    queue.Enqueue(next);
                }
            }
        }

        return furthest;
    }

    //set the boss room in ending
    void SetBossRoom(RoomType[,] dungeon, Vector2Int start)
    {
        Vector2Int bossPos = FindFurthestRoom(dungeon, start);

        dungeon[bossPos.x, bossPos.y] = RoomType.Boss;
    }
}
