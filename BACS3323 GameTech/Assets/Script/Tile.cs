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
}
