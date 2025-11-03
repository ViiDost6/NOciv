using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class TileRule
{
    public int tileType;
    [Range(0f, 1f)] public float minProbability = 0f;
    [Range(0f, 1f)] public float maxProbability = 1f;
    [Range(0, 1000)] public int maxCount = 1000;
    public bool requiredForConnectivity = false;
    [Range(0.1f, 5f)] public float baseWeight = 1f;
    public bool walkable = true;
}

public class MapGeneratorRules : MonoBehaviour
{
    public MapGenerator mapGenerator;
    public List<TileRule> tileRules;
    
    private Dictionary<int, int> tileCounts;
    private Dictionary<int, int> tileTargets;
    private int[,] mapMatrix;
    private int mapHeight;
    private int mapWidth;

    void Start()
    {
        if (mapGenerator == null)
            mapGenerator = GetComponent<MapGenerator>();
    }

    public void GenerateMapWithRules()
    {
        if (mapGenerator == null) return;

        InitializeTileCounts();
        mapHeight = mapGenerator.mapHeight;
        mapWidth = mapGenerator.mapWidth;
        mapMatrix = new int[mapHeight, mapWidth];
        
        CalculateTileTargets();
        GenerateMapWithBalancedDistribution();
        ApplyConnectivityRules();
        
        mapGenerator.SetMapData(mapMatrix);
        ApplyTileDataToMap();
    }

    void InitializeTileCounts()
    {
        tileCounts = new Dictionary<int, int>();
        tileTargets = new Dictionary<int, int>();
        foreach (var rule in tileRules)
        {
            tileCounts[rule.tileType] = 0;
            tileTargets[rule.tileType] = 0;
        }
    }

    void CalculateTileTargets()
    {
        int totalTiles = mapHeight * mapWidth;
        float totalProbability = 0f;

        foreach (var rule in tileRules)
        {
            float avgProbability = (rule.minProbability + rule.maxProbability) / 2f;
            totalProbability += avgProbability;
        }

        foreach (var rule in tileRules)
        {
            float avgProbability = (rule.minProbability + rule.maxProbability) / 2f;
            float proportion = avgProbability / totalProbability;
            int targetCount = Mathf.RoundToInt(totalTiles * proportion);
            
            int maxAllowed = rule.maxCount == 1000 ? totalTiles : rule.maxCount;
            tileTargets[rule.tileType] = Mathf.Min(targetCount, maxAllowed);
        }
    }

    void GenerateMapWithBalancedDistribution()
    {
        List<Vector2Int> allPositions = new List<Vector2Int>();
        for (int y = 0; y < mapHeight; y++)
        {
            for (int x = 0; x < mapWidth; x++)
            {
                allPositions.Add(new Vector2Int(x, y));
            }
        }

        ShufflePositions(allPositions);

        foreach (Vector2Int pos in allPositions)
        {
            int tileType = SelectTileTypeWithBalance(pos);
            mapMatrix[pos.y, pos.x] = tileType;
            tileCounts[tileType]++;
        }
    }

    void ShufflePositions(List<Vector2Int> positions)
    {
        for (int i = 0; i < positions.Count; i++)
        {
            int randomIndex = Random.Range(i, positions.Count);
            Vector2Int temp = positions[i];
            positions[i] = positions[randomIndex];
            positions[randomIndex] = temp;
        }
    }

    int SelectTileTypeWithBalance(Vector2Int pos)
    {
        List<int> availableTypes = new List<int>();
        List<float> weights = new List<float>();

        foreach (var rule in tileRules)
        {
            if (tileCounts[rule.tileType] < tileTargets[rule.tileType])
            {
                availableTypes.Add(rule.tileType);
                
                float baseProbability = Random.Range(rule.minProbability, rule.maxProbability);
                float balanceWeight = CalculateBalanceWeight(rule.tileType);
                float clusterWeight = CalculateClusterWeight(rule.tileType, pos, rule.baseWeight);
                
                float finalWeight = baseProbability * balanceWeight * clusterWeight;
                weights.Add(finalWeight);
            }
        }

        if (availableTypes.Count == 0) return 0;

        float totalWeight = 0f;
        foreach (float weight in weights) totalWeight += weight;

        float randomValue = Random.Range(0f, totalWeight);
        float currentWeight = 0f;

        for (int i = 0; i < availableTypes.Count; i++)
        {
            currentWeight += weights[i];
            if (randomValue <= currentWeight)
            {
                return availableTypes[i];
            }
        }

        return availableTypes[0];
    }

    float CalculateBalanceWeight(int tileType)
    {
        int currentCount = tileCounts[tileType];
        int targetCount = tileTargets[tileType];
        
        if (currentCount >= targetCount) return 0.01f;
        
        float progress = (float)currentCount / targetCount;
        return 1.0f + (1.0f - progress) * 2.0f;
    }

    float CalculateClusterWeight(int tileType, Vector2Int pos, float baseWeight)
    {
        List<Vector2Int> neighbors = GetHexNeighbors(pos);
        int sameTypeNeighbors = 0;
        int totalValidNeighbors = 0;

        foreach (Vector2Int neighbor in neighbors)
        {
            if (neighbor.x >= 0 && neighbor.x < mapWidth && neighbor.y >= 0 && neighbor.y < mapHeight)
            {
                totalValidNeighbors++;
                if (mapMatrix[neighbor.y, neighbor.x] == tileType)
                {
                    sameTypeNeighbors++;
                }
            }
        }

        if (totalValidNeighbors == 0) return 1.0f;

        float clusterFactor = (float)sameTypeNeighbors / totalValidNeighbors;
        
        // BASE WEIGHT controla el agrupamiento:
        // baseWeight = 0.1 → Evita agrupamiento (×0.1)
        // baseWeight = 1.0 → Neutral (×1.0)  
        // baseWeight = 5.0 → Favorece mucho agrupamiento (×5.0)
        return 1.0f + (clusterFactor * baseWeight * 2.0f);
    }

    List<Vector2Int> GetHexNeighbors(Vector2Int pos)
    {
        List<Vector2Int> neighbors = new List<Vector2Int>();
        int x = pos.x;
        int y = pos.y;

        bool isOddRow = y % 2 == 1;

        Vector2Int[] hexOffsets = isOddRow ? 
            new Vector2Int[] {
                new Vector2Int(0, -1),
                new Vector2Int(1, -1),
                new Vector2Int(1, 0),
                new Vector2Int(0, 1),
                new Vector2Int(1, 1),
                new Vector2Int(-1, 0)
            } :
            new Vector2Int[] {
                new Vector2Int(-1, -1),
                new Vector2Int(0, -1),
                new Vector2Int(1, 0),
                new Vector2Int(-1, 1),
                new Vector2Int(0, 1),
                new Vector2Int(-1, 0)
            };

        foreach (Vector2Int offset in hexOffsets)
        {
            neighbors.Add(new Vector2Int(x + offset.x, y + offset.y));
        }

        return neighbors;
    }

    void ApplyConnectivityRules()
    {
        foreach (var rule in tileRules)
        {
            if (rule.requiredForConnectivity && tileCounts[rule.tileType] == 0)
            {
                ForceTilePlacement(rule.tileType);
            }
        }
    }

    void ForceTilePlacement(int tileType)
    {
        for (int y = 1; y < mapHeight - 1; y += 2)
        {
            for (int x = 1; x < mapWidth - 1; x += 2)
            {
                if (mapMatrix[y, x] != tileType)
                {
                    int oldType = mapMatrix[y, x];
                    mapMatrix[y, x] = tileType;
                    tileCounts[tileType]++;
                    tileCounts[oldType]--;
                    return;
                }
            }
        }
    }

    void ApplyTileDataToMap()
    {
        foreach (Transform child in mapGenerator.transform)
        {
            TileData tileData = child.GetComponent<TileData>();
            if (tileData != null)
            {
                TileRule rule = tileRules.Find(r => r.tileType == tileData.tileType);
                if (rule != null)
                {
                    tileData.walkable = rule.walkable;
                    tileData.weight = rule.baseWeight;
                }
            }
        }
    }
}