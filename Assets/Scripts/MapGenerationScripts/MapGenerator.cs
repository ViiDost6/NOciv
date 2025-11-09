using UnityEngine;
using System.Collections.Generic;

public class MapGenerator : MonoBehaviour
{
    public int mapHeight = 10;
    public int mapWidth = 10;
    public List<GameObject> terrainPrefabs;
    
    private int[,] mapMatrix;
    private Dictionary<Vector2Int, TileData> tileGrid = new Dictionary<Vector2Int, TileData>();

    public void GenerateMap()
    {
        ClearMap();
        MapGeneratorRules rules = GetComponent<MapGeneratorRules>();
        if (rules != null) rules.GenerateMapWithRules();
        else
        {
            GenerateEmptyMap();
            MapDrawing();
        }
        ConnectAllNeighbors();
    }

    public void SetMapData(int[,] newMapData)
    {
        if (newMapData.GetLength(0) == mapHeight && newMapData.GetLength(1) == mapWidth)
        {
            mapMatrix = newMapData;
            MapDrawing();
            ConnectAllNeighbors();
        }
    }

    void GenerateEmptyMap()
    {
        mapMatrix = new int[mapHeight, mapWidth];
        for (int row = 0; row < mapHeight; row++)
            for (int col = 0; col < mapWidth; col++)
                mapMatrix[row, col] = 0;
    }

    void MapDrawing()
    {
        if (terrainPrefabs == null || terrainPrefabs.Count == 0) return;

        tileGrid.Clear();

        for (int row = 0; row < mapHeight; row++)
        {
            for (int col = 0; col < mapWidth; col++)
            {
                float xPos = col * 1.7f;
                float yPos = row * 0.5f;
                if (row % 2 == 1) xPos += 0.85f;

                int tileType = mapMatrix[row, col];
                if (tileType >= 0 && tileType < terrainPrefabs.Count && terrainPrefabs[tileType] != null)
                {
                    Vector3 position = new Vector3(xPos, yPos, 0);
                    GameObject tile = Instantiate(terrainPrefabs[tileType], position, Quaternion.identity);
                    tile.transform.SetParent(transform);
                    tile.name = $"Hex_{row}_{col}_Type{tileType}";
                    
                    ApplyTileData(tile, tileType, row, col);
                    tileGrid[new Vector2Int(row, col)] = tile.GetComponent<TileData>();
                }
            }
        }
    }
    
    void ApplyTileData(GameObject tile, int tileType, int row, int col)
    {
        MapGeneratorRules rules = GetComponent<MapGeneratorRules>();
        if (rules != null)
        {
            var rule = rules.tileRules.Find(r => r.tileType == tileType);
            if (rule != null)
            {
                TileData data = tile.GetComponent<TileData>();
                if (data == null) data = tile.AddComponent<TileData>();
                data.Initialize(rule.tileType, rule.walkable, rule.movementWeight, row, col);
            }
        }
    }
    
    void ConnectAllNeighbors()
    {
        foreach (var tile in tileGrid.Values)
            ConnectNeighbors(tile.gridPosition);
    }
    
    void ConnectNeighbors(Vector2Int pos)
    {
        TileData currentTile = tileGrid[pos];
        foreach (Vector2Int neighborPos in GetExactNeighborPositions(pos))
        {
            if (tileGrid.TryGetValue(neighborPos, out TileData neighbor))
                currentTile.AddNeighbor(neighbor);
        }
    }
    
    List<Vector2Int> GetExactNeighborPositions(Vector2Int pos)
    {
        int row = pos.x;
        int col = pos.y;
        List<Vector2Int> neighbors = new List<Vector2Int>();

        int rowParity = row % 2;

        // Patrón basado en tus ejemplos
        neighbors.Add(new Vector2Int(row + 2, col));        // Norte
        neighbors.Add(new Vector2Int(row - 2, col));        // Sur
        neighbors.Add(new Vector2Int(row + 1, col + rowParity));     // Noreste
        neighbors.Add(new Vector2Int(row - 1, col + rowParity));     // Sureste
        neighbors.Add(new Vector2Int(row + 1, col - (1 - rowParity))); // Noroeste
        neighbors.Add(new Vector2Int(row - 1, col - (1 - rowParity))); // Suroeste

        List<Vector2Int> validNeighbors = new List<Vector2Int>();
        foreach (Vector2Int neighbor in neighbors)
        {
            if (neighbor.x >= 0 && neighbor.x < mapHeight && neighbor.y >= 0 && neighbor.y < mapWidth)
                validNeighbors.Add(neighbor);
        }

        return validNeighbors;
    }
    
    void ClearMap()
    {
        tileGrid.Clear();
        while (transform.childCount > 0)
            DestroyImmediate(transform.GetChild(0).gameObject);
    }
    
    public TileData GetTileAtPosition(Vector2Int position)
    {
        return tileGrid.TryGetValue(position, out TileData tile) ? tile : null;
    }
}