using System.Collections.Generic;
using UnityEngine;

public class DungeonAPI
{
    WFCGenerator wfc = new WFCGenerator();
    DungeonValidator validator = new DungeonValidator();
    GameplayPlacer placer = new GameplayPlacer();

    public DungeonResult GenerateDungeon(DungeonConfig config)
    {
        Dictionary<Vector2Int, Tile> layout;

        do
        {
            layout = wfc.Generate(config.size, config.seed);

        } while (!validator.Validate(layout, config.size));

        var content = placer.Place(layout, config.difficulty);

        return new DungeonResult
        {
            layout = layout,
            content = content
        };
    }
}


