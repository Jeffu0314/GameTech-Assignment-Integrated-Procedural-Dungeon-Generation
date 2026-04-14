using UnityEngine;

public class Cell : MonoBehaviour
{
    public bool collapsed;
    public Tile[] tileOptions;
    public bool isMainPath;
    public Vector2Int gridPos;

    public void CreateCell(bool collapseState, Tile[] tiles)
    {
        collapsed = collapseState;
        tileOptions = tiles;
    }

    public void RecreateCell(Tile[] tiles)
    {
        tileOptions = tiles;
    }


}
