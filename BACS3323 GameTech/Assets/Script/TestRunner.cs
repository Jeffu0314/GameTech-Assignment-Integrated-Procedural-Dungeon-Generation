using UnityEngine;

public class TestRunner : MonoBehaviour
{
    void Start()
    {
        DungeonAPI api = new DungeonAPI();

        var config = new DungeonConfig
        {
            size = 8,
            seed = 123,
            difficulty = 0.5f
        };

        var dungeon = api.GenerateDungeon(config);

        Debug.Log("Dungeon Generated!");
    }
}
