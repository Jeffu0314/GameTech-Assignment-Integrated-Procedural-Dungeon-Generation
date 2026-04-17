using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[System.Serializable]
public class Tile
{
    public GameObject prefab;

    public bool up;
    public bool down;
    public bool left;
    public bool right;

    public TileType tileType;

    public float weight = 1f;

    public enum TileType
    {
        Start,
        Boss,
        CorridorStraight,
        CorridorTurn,
        T_Junction,
        Cross,
        DeadEnd,
        Empty
    }

    public bool Matches(Tile other, Vector2Int dir)
    {
        if (dir == Vector2Int.up) return up == other.down;
        if (dir == Vector2Int.down) return down == other.up;
        if (dir == Vector2Int.left) return left == other.right;
        if (dir == Vector2Int.right) return right == other.left;
        return false;
    }

    public bool HasConnection(Vector2Int dir)
    {
        if (dir == Vector2Int.up) return up;
        if (dir == Vector2Int.down) return down;
        if (dir == Vector2Int.left) return left;
        if (dir == Vector2Int.right) return right;
        return false;
    }

}