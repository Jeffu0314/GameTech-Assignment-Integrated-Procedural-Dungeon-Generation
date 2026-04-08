using System.Collections.Generic;
using UnityEngine;

public class WFC_Cell
{
    public bool collapsed;
    public List<WFCTile> possibleTiles;

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
