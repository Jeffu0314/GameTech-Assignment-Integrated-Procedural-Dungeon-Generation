using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;

public class WaveFunctionCollapse : MonoBehaviour
{
    public int dimensions;
    public Tile[] tileObjects;
    public List<Cell> gridComponenets;
    public Cell cellObj;

    public Tile backupTile;

    private int iteration;

    public float cellSpacing = 10f;

    private void Awake()
    {
        GenerateAdjacencyRules();
        gridComponenets = new List<Cell>();
        InitializeGrid();

    }

    void InitializeGrid()
    {
        for(int y = 0; y < dimensions; y++)
        {
            for(int x = 0; x < dimensions; x++)
            {
                Cell newCell = Instantiate(cellObj, new Vector3(x * cellSpacing, 0, y * cellSpacing), Quaternion.identity);
                newCell.CreateCell(false, tileObjects);
                gridComponenets.Add(newCell);
            }
        }

        StartCoroutine(CheckEntropy());
    }

    IEnumerator CheckEntropy()
    {
        List<Cell> tempGrid = new List<Cell>(gridComponenets);
        tempGrid.RemoveAll(c => c.collapsed);
        tempGrid.Sort((a, b) => a.tileOptions.Length - b.tileOptions.Length);
        tempGrid.RemoveAll(a => a.tileOptions.Length != tempGrid[0].tileOptions.Length);
        yield return new WaitForSeconds(0.125f);

        CollapseCell(tempGrid);
    }

    void CollapseCell(List<Cell> tempGrid)
    {
        int randIndex = UnityEngine.Random.Range(0, tempGrid.Count);

        Cell cellToCollapse = tempGrid[randIndex];

        cellToCollapse.collapsed = true;

        try
        {
            Tile selectedTile = cellToCollapse.tileOptions[UnityEngine.Random.Range(0, cellToCollapse.tileOptions.Length)];
            cellToCollapse.tileOptions = new Tile[] { selectedTile };
        }
        catch
        {
            Debug.Log("No tile options left, using backup tile");
            Tile selectedTile = backupTile;
            cellToCollapse.tileOptions = new Tile[] { selectedTile };
        }

        Tile foundTile = cellToCollapse.tileOptions[0];
        Instantiate(foundTile, cellToCollapse.transform.position, foundTile.transform.rotation);
    
        UpdateGeneration();
    }

    void UpdateGeneration()
    {
        List<Cell> newGenerationCell = new List<Cell>(gridComponenets);

        for(int y = 0; y < dimensions; y++)
        {
            for(int x = 0; x < dimensions; x++) 
            {
                var index = x + y * dimensions;

                if (gridComponenets[index].collapsed) 
                {
                    newGenerationCell[index] = gridComponenets[index];
                }
                else
                {
                    List<Tile> options = new List<Tile>();
                    foreach(Tile t in tileObjects)
                    {
                        options.Add(t);
                    }

                    if(y > 0)
                    {
                        Cell up = gridComponenets[x + (y - 1) * dimensions];
                        List<Tile> validOptions = new List<Tile>();

                        foreach(Tile possibleOptions in up.tileOptions)
                        {
                            var validOption = Array.FindIndex(tileObjects, obj => obj == possibleOptions);
                            var valid = tileObjects[validOption].downNeighbours;

                            validOptions = validOptions.Concat(valid).ToList();
                        }

                        CheckValidity(options, validOptions);
                    }

                    if (x < dimensions - 1)
                    {
                        Cell right = gridComponenets[x + 1 + y * dimensions];
                        List<Tile> validOptions = new List<Tile>();

                        foreach (Tile possibleOptions in right.tileOptions)
                        {
                            var validOption = Array.FindIndex(tileObjects, obj => obj == possibleOptions);
                            var valid = tileObjects[validOption].leftNeighbours;

                            validOptions = validOptions.Concat(valid).ToList();
                        }

                        CheckValidity(options, validOptions);
                    }

                    if (y < dimensions - 1)
                    {
                        Cell down = gridComponenets[x + (1 + y) * dimensions];
                        List<Tile> validOptions = new List<Tile>();

                        foreach (Tile possibleOptions in down.tileOptions)
                        {
                            var validOption = Array.FindIndex(tileObjects, obj => obj == possibleOptions);
                            var valid = tileObjects[validOption].upNeighbours;

                            validOptions = validOptions.Concat(valid).ToList();
                        }

                        CheckValidity(options, validOptions);
                    }

                    if (x > 0)
                    {
                        Cell left = gridComponenets[x - 1 + y * dimensions];
                        List<Tile> validOptions = new List<Tile>();

                        foreach (Tile possibleOptions in left.tileOptions)
                        {
                            var validOption = Array.FindIndex(tileObjects, obj => obj == possibleOptions);
                            var valid = tileObjects[validOption].rightNeighbours;

                            validOptions = validOptions.Concat(valid).ToList();
                        }

                        CheckValidity(options, validOptions);
                    }

                    Tile[] newTileList = new Tile[options.Count];

                    for(int i = 0; i < options.Count; i++)
                    {
                        newTileList[i] = options[i];
                    }

                    newGenerationCell[index].RecreateCell(newTileList);

                }
            }
        }

        gridComponenets = newGenerationCell;

        if(iteration < dimensions * dimensions)
        {
            StartCoroutine(CheckEntropy());
        }
    }

    void CheckValidity(List<Tile> optionList, List<Tile> validOption)
    {
        for(int x = optionList.Count - 1; x >= 0; x--)
        { 
            var element = optionList[x];

            if (!validOption.Contains(element))
            {
                optionList.RemoveAt(x);
            }
        }
    }

    void GenerateAdjacencyRules()
    {
        foreach (Tile tile in tileObjects)
        {
            List<Tile> upList = new List<Tile>();
            List<Tile> downList = new List<Tile>();
            List<Tile> leftList = new List<Tile>();
            List<Tile> rightList = new List<Tile>();

            foreach (Tile other in tileObjects)
            {
                if (tile.up == other.down)
                    upList.Add(other);

                if (tile.down == other.up)
                    downList.Add(other);

                if (tile.left == other.right)
                    leftList.Add(other);

                if (tile.right == other.left)
                    rightList.Add(other);
            }

            tile.upNeighbours = upList.ToArray();
            tile.downNeighbours = downList.ToArray();
            tile.leftNeighbours = leftList.ToArray();
            tile.rightNeighbours = rightList.ToArray();
        }
    }
}
