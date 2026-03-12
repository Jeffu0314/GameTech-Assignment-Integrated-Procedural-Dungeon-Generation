using UnityEngine;

public class DungeonCell
{
    public Vector2Int position;
    public RoomType roomType;

    public DungeonCell(int x, int y)
    {
        position = new Vector2Int(x, y);
        roomType = RoomType.Empty;
    }
}
