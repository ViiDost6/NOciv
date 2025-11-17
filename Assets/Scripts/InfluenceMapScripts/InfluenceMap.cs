using UnityEngine;
using System.Collections.Generic;

public class InfluenceMap : MonoBehaviour
{
    public MapGenerator mapGenerator;
    public StructureManager structureManager;
    [Range(0.1f, 5f)] public float baseInfluenceDecay = 1.0f;
    [Range(1, 50)] public int maxInfluenceDistance = 20; // Aumentar distancia máxima
    
    private float[,] playerInfluence;
    private float[,] enemyInfluence;
    private float[,] resourceInfluence;
    private float[,] strategicInfluence;
    private float[,] combinedInfluence;
    
    private Dictionary<string, float> layerWeights = new Dictionary<string, float>()
    {
        {"player", 2.0f},
        {"enemy", -3.0f},
        {"resource", 1.5f},
        {"strategic", 1.0f},
        {"terrain", 0.5f}
    };
    
    public void GenerateInfluenceMap()
    {
        Debug.Log("INFLUENCE MAP: Starting influence map generation");
        
        if (structureManager == null)
        {
            Debug.LogError("INFLUENCE MAP: StructureManager is null");
            return;
        }
        
        if (mapGenerator == null)
        {
            Debug.LogError("INFLUENCE MAP: MapGenerator is null");
            return;
        }
        
        Debug.Log("INFLUENCE MAP: Map dimensions: " + mapGenerator.mapWidth + "x" + mapGenerator.mapHeight);
        Debug.Log("INFLUENCE MAP: Player towers: " + structureManager.PlayerTowerPositions.Count);
        Debug.Log("INFLUENCE MAP: Enemy towers: " + structureManager.EnemyTowerPositions.Count);
        Debug.Log("INFLUENCE MAP: Resources: " + structureManager.ResourcePositions.Count);
        
        InitializeMaps();
        CalculatePlayerInfluence();
        CalculateEnemyInfluence();
        CalculateResourceInfluence();
        CalculateStrategicInfluence();
        CalculateTerrainInfluence();
        CombineAllInfluences();
        
        Debug.Log("INFLUENCE MAP: Influence maps generated successfully");
        DebugInfluenceStats();
    }
    
    void DebugInfluenceStats()
    {
        if (combinedInfluence == null) return;
        
        float minInfluence = float.MaxValue;
        float maxInfluence = float.MinValue;
        int positiveTiles = 0;
        int negativeTiles = 0;
        int neutralTiles = 0;
        
        for (int y = 0; y < mapGenerator.mapHeight; y++)
        {
            for (int x = 0; x < mapGenerator.mapWidth; x++)
            {
                float influence = combinedInfluence[y, x];
                minInfluence = Mathf.Min(minInfluence, influence);
                maxInfluence = Mathf.Max(maxInfluence, influence);
                
                if (influence > 1f) positiveTiles++;
                else if (influence < -1f) negativeTiles++;
                else neutralTiles++;
            }
        }
        
        Debug.Log("INFLUENCE MAP: Influence statistics:");
        Debug.Log("INFLUENCE MAP:   Min: " + minInfluence.ToString("F2"));
        Debug.Log("INFLUENCE MAP:   Max: " + maxInfluence.ToString("F2"));
        Debug.Log("INFLUENCE MAP:   Positive tiles: " + positiveTiles);
        Debug.Log("INFLUENCE MAP:   Negative tiles: " + negativeTiles);
        Debug.Log("INFLUENCE MAP:   Neutral tiles: " + neutralTiles);
    }
    
    void InitializeMaps()
    {
        int height = mapGenerator.mapHeight;
        int width = mapGenerator.mapWidth;
        
        playerInfluence = new float[height, width];
        enemyInfluence = new float[height, width];
        resourceInfluence = new float[height, width];
        strategicInfluence = new float[height, width];
        combinedInfluence = new float[height, width];
        
        Debug.Log("INFLUENCE MAP: Maps initialized: " + width + "x" + height);
    }
    
    void CalculatePlayerInfluence()
    {
        Debug.Log("INFLUENCE MAP: Calculating player influence from " + structureManager.PlayerTowerPositions.Count + " towers");
        foreach (Vector2Int towerPos in structureManager.PlayerTowerPositions)
        {
            PropagateInfluence(towerPos, 100f, playerInfluence, true);
        }
    }
    
    void CalculateEnemyInfluence()
    {
        Debug.Log("INFLUENCE MAP: Calculating enemy influence from " + structureManager.EnemyTowerPositions.Count + " towers");
        foreach (Vector2Int towerPos in structureManager.EnemyTowerPositions)
        {
            PropagateInfluence(towerPos, -80f, enemyInfluence, true);
        }
    }
    
    void CalculateResourceInfluence()
    {
        Debug.Log("INFLUENCE MAP: Calculating resource influence from " + structureManager.ResourcePositions.Count + " resources");
        foreach (Vector2Int resourcePos in structureManager.ResourcePositions)
        {
            PropagateInfluence(resourcePos, 60f, resourceInfluence, false);
        }
    }
    
    void CalculateStrategicInfluence()
    {
        Debug.Log("INFLUENCE MAP: Calculating strategic influence");
        for (int y = 0; y < mapGenerator.mapHeight; y++)
        {
            for (int x = 0; x < mapGenerator.mapWidth; x++)
            {
                if (x >= 0 && x < mapGenerator.mapWidth && y >= 0 && y < mapGenerator.mapHeight)
                {
                    Vector2Int pos = new Vector2Int(x, y);
                    float strategicValue = CalculatePositionStrategicValue(pos);
                    strategicInfluence[y, x] = strategicValue;
                }
            }
        }
    }
    
    void CalculateTerrainInfluence()
    {
        Debug.Log("INFLUENCE MAP: Calculating terrain influence");
        for (int y = 0; y < mapGenerator.mapHeight; y++)
        {
            for (int x = 0; x < mapGenerator.mapWidth; x++)
            {
                if (x >= 0 && x < mapGenerator.mapWidth && y >= 0 && y < mapGenerator.mapHeight)
                {
                    TileData tile = mapGenerator.GetTileAtPosition(new Vector2Int(x, y));
                    if (tile != null)
                    {
                        float terrainWeight = GetTerrainInfluence(tile.tileType);
                        combinedInfluence[y, x] += terrainWeight * layerWeights["terrain"];
                    }
                }
            }
        }
    }
    
    void PropagateInfluence(Vector2Int source, float baseStrength, float[,] influenceMap, bool usePathfinding)
    {
        int tilesInfluenced = 0;
        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        Dictionary<Vector2Int, float> influenceValues = new Dictionary<Vector2Int, float>();
        
        queue.Enqueue(source);
        influenceValues[source] = baseStrength;
        
        while (queue.Count > 0)
        {
            Vector2Int current = queue.Dequeue();
            float currentInfluence = influenceValues[current];
            
            if (current.x >= 0 && current.x < mapGenerator.mapWidth && 
                current.y >= 0 && current.y < mapGenerator.mapHeight)
            {
                // MEJORA: Sumar en lugar de reemplazar para influencias superpuestas
                influenceMap[current.y, current.x] += currentInfluence;
                tilesInfluenced++;
            }
            
            // MEJORA: Reducir el umbral mínimo para propagar más lejos
            if (Mathf.Abs(currentInfluence) < 1f) continue;
            
            TileData currentTile = mapGenerator.GetTileAtPosition(current);
            if (currentTile != null)
            {
                foreach (TileData neighbor in currentTile.neighbors)
                {
                    Vector2Int neighborPos = neighbor.gridPosition;
                    
                    if (neighborPos.x >= 0 && neighborPos.x < mapGenerator.mapWidth && 
                        neighborPos.y >= 0 && neighborPos.y < mapGenerator.mapHeight &&
                        !influenceValues.ContainsKey(neighborPos))
                    {
                        // MEJORA: Permitir propagación a través de tiles no walkables con mayor penalización
                        float distance = Vector2Int.Distance(source, neighborPos);
                        if (distance <= maxInfluenceDistance)
                        {
                            float decay = CalculateInfluenceDecay(current, neighborPos, usePathfinding, neighbor.walkable);
                            float neighborInfluence = currentInfluence * decay;
                            
                            // MEJORA: Aplicar penalización adicional por tiles no walkables
                            if (!neighbor.walkable)
                            {
                                neighborInfluence *= 0.3f; // Reducir influencia en tiles no walkables
                            }
                            
                            influenceValues[neighborPos] = neighborInfluence;
                            queue.Enqueue(neighborPos);
                        }
                    }
                }
            }
        }
        
        Debug.Log("INFLUENCE MAP: Propagation from " + source + ": " + tilesInfluenced + " tiles influenced");
    }
    
    // MEJORA: Añadir parámetro para tiles no walkables
    float CalculateInfluenceDecay(Vector2Int from, Vector2Int to, bool usePathfinding, bool isWalkable = true)
    {
        float distance = Vector2Int.Distance(from, to);
        
        // MEJORA: Usar decaimiento más suave
        float baseDecay = Mathf.Exp(-distance / (baseInfluenceDecay * 2f)); // Reducir tasa de decaimiento
        
        if (usePathfinding && !isWalkable)
        {
            baseDecay *= 0.3f; // Penalización adicional para tiles no walkables
        }
        else if (usePathfinding)
        {
            TileData toTile = mapGenerator.GetTileAtPosition(to);
            if (toTile != null && toTile.weight > 1.0f)
            {
                baseDecay *= 0.7f;
            }
        }
        
        return baseDecay;
    }
    
    float CalculatePositionStrategicValue(Vector2Int pos)
    {
        TileData tile = mapGenerator.GetTileAtPosition(pos);
        if (tile == null) return 0f;
        
        float value = 0f;
        
        // MEJORA: Valor estratégico basado en conectividad
        int walkableNeighbors = 0;
        foreach (TileData neighbor in tile.neighbors)
        {
            if (neighbor.walkable) walkableNeighbors++;
        }
        
        // MEJORA: Valorar más los puntos de choke (pocas conexiones) y cruces (muchas conexiones)
        if (walkableNeighbors <= 2)
            value += 40f; // Puntos de choke - muy valiosos
        else if (walkableNeighbors >= 6)
            value += 25f; // Intersecciones - valiosas
        else if (walkableNeighbors >= 4)
            value += 15f; // Puntos normales
        
        // MEJORA: Valorar proximidad a recursos
        float minResourceDist = float.MaxValue;
        foreach (Vector2Int resourcePos in structureManager.ResourcePositions)
        {
            float dist = Vector2Int.Distance(pos, resourcePos);
            if (dist < minResourceDist) minResourceDist = dist;
        }
        
        if (minResourceDist < 10f)
        {
            value += Mathf.Max(0, 25f - minResourceDist * 2f);
        }
        
        return value;
    }
    
    float GetTerrainInfluence(int tileType)
    {
        switch (tileType)
        {
            case 0: return 1.0f;  // Tierra - neutral
            case 1: return 0.5f;  // Hierba - ligeramente positiva
            case 2: return -0.5f; // Agua - ligeramente negativa
            case 3: return -1.0f; // Montaña - muy negativa
            default: return 0f;
        }
    }
    
    void CombineAllInfluences()
    {
        for (int y = 0; y < mapGenerator.mapHeight; y++)
        {
            for (int x = 0; x < mapGenerator.mapWidth; x++)
            {
                combinedInfluence[y, x] = 
                    playerInfluence[y, x] * layerWeights["player"] +
                    enemyInfluence[y, x] * layerWeights["enemy"] +
                    resourceInfluence[y, x] * layerWeights["resource"] +
                    strategicInfluence[y, x] * layerWeights["strategic"] +
                    combinedInfluence[y, x]; // Terrain influence ya añadido
            }
        }
    }
    
    // ... resto de métodos sin cambios
    public float GetInfluenceAt(Vector2Int position)
    {
        if (combinedInfluence == null) 
        {
            Debug.LogWarning("INFLUENCE MAP: InfluenceMap has not been generated");
            return 0f;
        }
        
        if (position.x < 0 || position.x >= mapGenerator.mapWidth || 
            position.y < 0 || position.y >= mapGenerator.mapHeight)
        {
            return float.MinValue;
        }
        
        return combinedInfluence[position.y, position.x];
    }
    
    public Vector2Int GetBestMoveFrom(Vector2Int currentPosition)
    {
        TileData currentTile = mapGenerator.GetTileAtPosition(currentPosition);
        if (currentTile == null) return currentPosition;
        
        Vector2Int bestMove = currentPosition;
        float bestInfluence = GetInfluenceAt(currentPosition);
        
        foreach (TileData neighbor in currentTile.neighbors)
        {
            if (neighbor.walkable)
            {
                float influence = GetInfluenceAt(neighbor.gridPosition);
                if (influence > bestInfluence)
                {
                    bestInfluence = influence;
                    bestMove = neighbor.gridPosition;
                }
            }
        }
        
        return bestMove;
    }
    
    public void UpdateDynamicInfluence(Vector2Int position, float influenceChange)
    {
        if (position.x >= 0 && position.x < mapGenerator.mapWidth && 
            position.y >= 0 && position.y < mapGenerator.mapHeight)
        {
            combinedInfluence[position.y, position.x] += influenceChange;
        }
    }
}