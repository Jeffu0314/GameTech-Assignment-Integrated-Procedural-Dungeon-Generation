using UnityEngine;

[System.Serializable]
public class RoomData
{
    public RoomType type;

    public bool up;
    public bool down;
    public bool left;
    public bool right;

    public RoomData(RoomType type)
    {
        this.type = type;
    }
}
