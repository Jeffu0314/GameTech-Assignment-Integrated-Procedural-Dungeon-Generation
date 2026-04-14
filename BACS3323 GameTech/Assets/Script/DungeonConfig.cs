using UnityEngine;

[System.Serializable]
public class DungeonConfig
{
    public int width = 5;
    public int height = 5;

    public int mainPathLength = 8;

    public int combatRooms = 3;
    public int eliteRooms = 1;
    public int bonusRooms = 1;

    public float branchProbability = 0.3f;
}
