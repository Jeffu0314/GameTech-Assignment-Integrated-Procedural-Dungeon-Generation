using System.Collections.Generic;
using UnityEngine;

public class RoomData
{
    public Vector2Int pos;
    public Tile tile;

    public RoomContentType content;

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

