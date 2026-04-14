using System.Collections.Generic;
using UnityEngine;

public class Tile : MonoBehaviour
{
    public bool up;
    public bool down;
    public bool left;
    public bool right;

    public Tile[] upNeighbours;
    public Tile[] rightNeighbours;
    public Tile[] downNeighbours;
    public Tile[] leftNeighbours;

    public TileType tileType;

    public RoomType roomType;

    public List<RoomType> allowedRoomTypes;

    public bool IsRoad()
    {
        return up || down || left || right;
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
        int count = 0;
        if (up) count++;
        if (down) count++;
        if (left) count++;
        if (right) count++;
        return count;
    }

    // 连接形状
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

    // 房间类型
    public enum RoomType
    {
        Corridor,
        Combat,
        Elite,
        Bonus,
        Boss
    }
}
