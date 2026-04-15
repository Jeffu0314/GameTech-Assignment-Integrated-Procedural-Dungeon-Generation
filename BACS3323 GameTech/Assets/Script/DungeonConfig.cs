using System.Collections.Generic;
using UnityEngine;

public class DungeonConfig
{
    public int size;
    public int seed;
    public float difficulty;
}

public class DungeonResult
{
    public Dictionary<Vector2Int, Tile> layout;
    public Dictionary<Vector2Int, string> content;
}
