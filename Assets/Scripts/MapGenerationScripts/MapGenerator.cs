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
        
        // Buscar el componente de reglas y usarlo
        MapGeneratorRules rules = GetComponent<MapGeneratorRules>();
        if (rules != null)
        {
            rules.GenerateMapWithRules();
        }
        else
        {
            // Fallback: generar mapa vacío
            GenerateEmptyMap();
            MapDrawing();
        }
    }

    public void SetMapData(int[,] newMapData)
    {
        if (newMapData.GetLength(0) == mapHeight && newMapData.GetLength(1) == mapWidth)
        {
            mapMatrix = newMapData;
            MapDrawing();
        }
        else
        {
            Debug.LogError("Map data dimensions don't match!");
        }
    }

    void GenerateEmptyMap()
    {
        mapMatrix = new int[mapHeight, mapWidth];
        
        for (int row = 0; row < mapHeight; row++)
        {
            for (int col = 0; col < mapWidth; col++)
            {
                mapMatrix[row, col] = 0;
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

        if (mapMatrix == null)
        {
            Debug.LogError("No map data to draw!");
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
                
                int tileType = mapMatrix[row, col];
                if (tileType >= 0 && tileType < terrainPrefabs.Count && terrainPrefabs[tileType] != null)
                {
                    GameObject tile = Instantiate(terrainPrefabs[tileType], position, Quaternion.identity);
                    tile.transform.SetParent(this.transform);
                    tile.name = $"Hex_{row}_{col}_Type{tileType}";
                }
                else
                {
                    Debug.LogWarning($"Invalid tile type {tileType} at position ({row}, {col})");
                }
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