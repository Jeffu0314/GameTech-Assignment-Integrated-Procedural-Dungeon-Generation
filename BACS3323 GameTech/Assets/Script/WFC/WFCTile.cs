using UnityEngine;

[CreateAssetMenu(menuName = "WFC/Room Tile")]
public class WFCTile : ScriptableObject
{
    public string tileName;
    public GameObject prefab;

    public bool up;
    public bool down;
    public bool left;
    public bool right;

    [Range(0f, 1f)]
    public float weight = 1f;

    public enum RoomType
    {
        Corridor,
        Combat,
        Treasure,
        Start,
        Boss
    }

    public RoomType roomType;
}
