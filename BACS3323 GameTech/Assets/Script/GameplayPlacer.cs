using UnityEngine;

public class GameplayPlacer
{
    public void Place(DungeonLayout layout, float difficulty)
    {
        foreach (var room in layout.rooms)
        {
            float enemyWeight = room.depth * difficulty;
            float treasureWeight = 1f - enemyWeight;

            float r = Random.value;

            if (r < enemyWeight)
                room.content = "Enemy";
            else
                room.content = "Treasure";
        }
    }
}
