using System.Collections.Generic;
using UnityEngine;

public class DungeonAPI
{
    public static Dictionary<Vector2Int, Tile> GenerateDungeon(
        DungeonConfig config,
        Tile[] tileObjects,
        Tile startTile,
        Tile bossTile,
        Tile emptyTile
    )
    {
        WFCGenerator wfc = new WFCGenerator();

        wfc.tileObjects = tileObjects;
        wfc.startTile = startTile;
        wfc.bossTile = bossTile;
        wfc.emptyTile = emptyTile;

        var layout = wfc.Generate(
            config.size,
            config.seed,
            config.difficulty,
            config.enableBranches
        );

        // Step 2 会在这里加内容分配
        return layout;
    }
}