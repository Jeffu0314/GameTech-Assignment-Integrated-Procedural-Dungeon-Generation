using System.Collections.Generic;
using UnityEngine;

public class GameplayPlacer
{
    public Dictionary<Vector2Int, string> Place(Dictionary<Vector2Int, Tile> layout,
        float difficulty)
    {
        Dictionary<Vector2Int, string> content = new();

        foreach (var kv in layout)
        {
            float r = Random.value;

            float enemyChance = difficulty;
            float treasureChance = 1f - difficulty;

            if (r < enemyChance)
                content[kv.Key] = "Enemy";
            else
                content[kv.Key] = "Treasure";
        }

        return content;
    }
}