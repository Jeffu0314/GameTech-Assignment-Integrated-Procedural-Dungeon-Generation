using System.Collections.Generic;
using UnityEngine;

public class RoomData
{
    public Vector2Int pos;
    public Tile tile;

    public RoomContentType content;

    Dictionary<Vector2Int, RoomData> placed;

    public enum RoomContentType
    {
        Empty,
        Combat,
        Treasure,
        Shop,
        Trap,
        Boss,
        Start
    }
}

