using UnityEngine;
using static Tile;

[System.Serializable]
public class TileData : MonoBehaviour
{
    public GameObject prefab;

    public bool up;
    public bool down;
    public bool left;
    public bool right;

    public TileType tileType;
}
