using System.Collections.Generic;
using UnityEngine;

public class WFC_Cell
{
    public List<RoomType> possibleStates;
    public bool collapsed;

    public WFC_Cell(List<RoomType> states)
    {
        possibleStates = new List<RoomType>(states);
        collapsed = false;
    }

    public int Entropy()
    {
        return possibleStates.Count;
    }
}
