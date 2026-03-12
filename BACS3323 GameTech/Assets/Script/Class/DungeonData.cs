using UnityEngine;

public class DungeonData
{
    //total size of the dungeon
    public int width;
    public int height;

    public DungeonCell[,] grid;

    public DungeonData(int w, int h)
    {
        width = w;
        height = h;

        grid = new DungeonCell[w, h];

        for(int i = 0; i < w; i++)
        {
            for(int j = 0; j < h; j++)
            {
                grid[i, j] = new DungeonCell(i, j);
            }
        }
    }
}
