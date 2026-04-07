using System.Collections.Generic;
using UnityEngine;

public class DungeonGenerator : MonoBehaviour
{
    [Header("Room Prefabs")]
    public GameObject startRoomPrefab;
    public GameObject combatRoomPrefab;
    public GameObject bonusRoomPrefab;
    public GameObject eliteRoomPrefab;
    public GameObject bossRoomPrefab;
    public GameObject corridorRoomPrefab;

    [Header("Dungeon Size")]
    public int width;
    public int height;
    public float tileSize = 10f;

    DungeonData dungeon;

    void Start()
    {
        GenerateDungeon();
    }

    //generate the dungeon with WFC algorithm
    void GenerateDungeon()
    {
        WFC_Generator wfc = new WFC_Generator();

        RoomType[,] dungeon = wfc.Generate(width, height);

        Vector2Int start = SetStartRoom(dungeon);
        SetBossRoom(dungeon, start);

        RoomData[,] roomData = BuildRoomData(dungeon);

        SpawnDungeon(roomData);

        PrintDungeon(dungeon);
    }

    void SpawnDungeon(RoomData[,] dungeon)
    {
        int w = dungeon.GetLength(0);
        int h = dungeon.GetLength(1);

        for (int x = 0; x < w; x++)
        {
            for (int y = 0; y < h; y++)
            {
                RoomData data = dungeon[x, y];

                GameObject prefab = GetPrefab(data.type);

                Vector3 pos = new Vector3(x * tileSize, 0, y * tileSize);

                GameObject room = Instantiate(prefab, pos, Quaternion.identity);

                RoomView view = room.GetComponent<RoomView>();
                view.SetRoom(data);
            }
        }
    }

    //print the dungeon log in console
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

    //set the starting room
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

    RoomData[,] BuildRoomData(RoomType[,] layout)
    {
        int w = layout.GetLength(0);
        int h = layout.GetLength(1);

        RoomData[,] result = new RoomData[w, h];

        for (int x = 0; x < w; x++)
        {
            for (int y = 0; y < h; y++)
            {
                RoomData room = new RoomData(layout[x, y]);

                room.up = (y < h - 1);
                room.down = (y > 0);
                room.left = (x > 0);
                room.right = (x < w - 1);

                result[x, y] = room;
            }
        }

        return result;
    }

    GameObject GetPrefab(RoomType type)
    {
        switch (type)
        {
            case RoomType.Start:
                return startRoomPrefab;

            case RoomType.Combat:
                return combatRoomPrefab;

            case RoomType.Bonus:
                return bonusRoomPrefab;

            case RoomType.Elite:
                return eliteRoomPrefab;

            case RoomType.Boss:
                return bossRoomPrefab;

            case RoomType.Corridor:
            default:
                return corridorRoomPrefab;
        }
    }

}
