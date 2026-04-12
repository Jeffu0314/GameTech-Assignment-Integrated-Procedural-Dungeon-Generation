using System.Collections.Generic;
using UnityEngine;

public class WFC_Cell
{
    public bool collapsed;
    public List<TileOption> possibleOptions;

    public WFC_Cell(List<WFCTile> allTiles)
    {
        collapsed = false;
        possibleOptions = new List<TileOption>();
        foreach (var t in allTiles)
        {
            // 每个 Tile 都有 4 种旋转可能
            for (int r = 0; r < 4; r++)
            {
                possibleOptions.Add(new TileOption(t, r));
            }
        }
    }
}
