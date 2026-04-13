using System.Collections.Generic;
using UnityEngine;
using static WFCTile;

public class WFC_Cell
{
    public bool collapsed;
    public bool locked;

    public List<WFCTile> possibleTiles;

    public RoomType assignedType = RoomType.Corridor;

    public WFC_Cell(List<WFCTile> allTiles)
    {
        collapsed = false;
        possibleTiles = new List<WFCTile>(allTiles);
    }

    public int Entropy()
    {
        return possibleTiles.Count;
    }


}
