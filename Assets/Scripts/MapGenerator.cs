using UnityEngine;
using System.Collections.Generic;

public class MapGenerator : MonoBehaviour
{
    public int mapHeight = 10;
    public int mapWidth = 10;
    public List<GameObject> terrainPrefabs;
    
    private int[,] mapMatrix;

    public void GenerateMap()
    {
        ClearMap();
        GenerateMapMatrix(mapHeight, mapWidth);
        MapDrawing();
    }

    void GenerateMapMatrix(int height, int width)
    {
        mapMatrix = new int[height, width];
        for (int i = 0; i < height; i++)
        {
            for (int j = 0; j < width; j++)
            {
                mapMatrix[i, j] = 0;
            }
        }
    }

    void MapDrawing()
    {
        if (terrainPrefabs == null || terrainPrefabs.Count == 0)
        {
            Debug.LogError("No terrain prefabs assigned!");
            return;
        }

        for (int row = 0; row < mapHeight; row++)
        {
            for (int col = 0; col < mapWidth; col++)
            {
                float xPos = col * 1.7f;
                float yPos = row * 0.5f;
                
                if (row % 2 == 1)
                {
                    xPos += 0.85f;
                }

                Vector3 position = new Vector3(xPos, yPos, 0);
                GameObject tile = Instantiate(terrainPrefabs[0], position, Quaternion.identity);
                tile.transform.SetParent(this.transform);
                tile.name = $"Hex_{row}_{col}";
            }
        }
    }
    
    void ClearMap()
    {
        while (transform.childCount > 0)
        {
            DestroyImmediate(transform.GetChild(0).gameObject);
        }
    }
}