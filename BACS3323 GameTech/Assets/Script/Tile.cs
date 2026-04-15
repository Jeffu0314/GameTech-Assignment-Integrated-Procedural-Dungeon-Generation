using System.Collections.Generic;
using UnityEngine;

public class Tile : MonoBehaviour
{
    [Header("Connections")]
    public bool up;
    public bool down;
    public bool left;
    public bool right;

    [Header("Adjacency")]
    public Tile[] upNeighbours;
    public Tile[] rightNeighbours;
    public Tile[] downNeighbours;
    public Tile[] leftNeighbours;

    [Header("Type")]
    public TileType tileType;

    [Header("Room")]
    public RoomType roomType;

    public List<RoomType> allowedRoomTypes;

    private void Awake()
    {
        if (allowedRoomTypes == null || allowedRoomTypes.Count == 0)
        {
            allowedRoomTypes = new List<RoomType>()
            {
                RoomType.Corridor,
                RoomType.Combat,
                RoomType.Elite,
                RoomType.Bonus,
                RoomType.Boss
            };
        }
    }

    public bool HasConnection(Vector2Int dir)
    {
        if (dir == Vector2Int.up) return up;
        if (dir == Vector2Int.down) return down;
        if (dir == Vector2Int.left) return left;
        if (dir == Vector2Int.right) return right;
        return false;
    }

    public int ConnectionCount()
    {
        int c = 0;
        if (up) c++;
        if (down) c++;
        if (left) c++;
        if (right) c++;
        return c;
    }

    // ⭐ 主路径必须严格匹配（核心）
    public bool IsStrictPath(List<Vector2Int> requiredDirs, bool isStart, bool isEnd)
    {
        foreach (var d in requiredDirs)
        {
            if (!HasConnection(d))
                return false;
        }

        int count = ConnectionCount();

        if (isStart || isEnd)
            return count == 1;

        return count == requiredDirs.Count; // ⭐ 关键
    }

    public bool IsBranch()
    {
        return tileType == TileType.DeadEnd ||
               tileType == TileType.CorridorTurn;
    }

    public enum TileType
    {
        Start,
        Boss,
        CorridorStraight,
        CorridorTurn,
        T_Junction,
        Cross,
        DeadEnd
    }

    public enum RoomType
    {
        Corridor,
        Combat,
        Elite,
        Bonus,
        Boss
    }
}