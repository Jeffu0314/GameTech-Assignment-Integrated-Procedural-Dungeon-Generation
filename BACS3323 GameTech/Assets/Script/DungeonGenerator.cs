using UnityEngine;

public class DungeonGenerator : MonoBehaviour
{
    public int width;
    public int height;

    DungeonData dungeon;

    void Start()
    {
        GenerateDungeon();
    }

    void GenerateDungeon()
    {
        WFC_Generator wfc = new WFC_Generator();

        RoomType[,] dungeon = wfc.Generate(width, height);

        PrintDungeon(dungeon);
    }

    void PrintDungeon(RoomType[,] dungeon)
    {
        int w = dungeon.GetLength(0);
        int h = dungeon.GetLength(1);

        for (int y = 0; y < h; y++)
        {
            string row = "";

            for (int x = 0; x < w; x++)
            {
                row += dungeon[x, y].ToString()[0] + " ";
            }

            Debug.Log(row);
        }
    }
}
