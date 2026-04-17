using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class DungeonConfig
{
    public int size = 8;
    public int seed = 100;
    public float cellSpacing = 10f;

    [Range(0f, 1f)]
    public float difficulty = 0.5f;

    public bool enableBranches = true;

    public int numCombatRooms = 5;
    public int numBonusRooms = 2;
    public int numEliteRooms = 1;

    public bool bossRequired = true;
}
