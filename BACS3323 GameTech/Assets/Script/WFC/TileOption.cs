using UnityEngine;

public class TileOption
{
    public WFCTile tile;
    public int rotationIndex; // 0, 1, 2, 3 (对应 0, 90, 180, 270 度)
    public Connectivity connectivity;

    public TileOption(WFCTile t, int r)
    {
        tile = t;
        rotationIndex = r;
        connectivity = t.GetRotatedConnectivity(r);
    }
}

public struct Connectivity { public bool up, down, left, right; }
