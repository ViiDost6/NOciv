using UnityEngine;
using System.Collections.Generic;

public class InfluenceMap : MonoBehaviour
{
    public MapGenerator mapGenerator;
    [Range(1, 50)] public int maxInfluenceDistance = 10;
    
    // Capas de influencia
    private float[,] baseInfluence;      // Influencia base del terreno
    private float[,] objectiveInfluence; // Influencia de objetivos
    private float[,] menaceInfluence;    // Influencia de amenazas
    private float[,] combinedInfluence;  // Influencia combinada final
    
    private List<Vector2Int> objectivePositions = new List<Vector2Int>();
    private List<Vector2Int> menacePositions = new List<Vector2Int>();
    
    // Pesos para combinar las influencias
    [Header("Influence Weights")]
    public float baseWeight = 0.3f;
    public float objectiveWeight = 1.0f;
    public float menaceWeight = 1.0f;
    
    [Header("Influence Intensity Settings")]
    [Range(1, 20)] public float maxSourceIntensity = 10f;
    
    [Header("Terrain Propagation Settings")]
    public bool useTerrainPropagation = true;
    public float difficultTerrainPenalty = 0.3f;
    public float favorableTerrainBonus = 1.5f;
    
    public void GenerateInfluenceMap()
    {
        if (mapGenerator == null)
        {
            Debug.LogError("InfluenceMap: MapGenerator is null");
            return;
        }
        
        Debug.Log($"InfluenceMap: Starting generation with distance {maxInfluenceDistance}");
        
        FindAllObjectivesAndMenaces();
        InitializeInfluenceLayers();
        
        CalculateBaseInfluence();
        CalculateObjectiveInfluence();
        CalculateMenaceInfluence();
        CombineAllInfluences();
        
        Debug.Log($"InfluenceMap: Complete - {objectivePositions.Count} objectives, {menacePositions.Count} menaces");
    }
    
    void FindAllObjectivesAndMenaces()
    {
        objectivePositions.Clear();
        menacePositions.Clear();
        
        // Buscar objetivos en escena
        GameObject[] objectives = GameObject.FindGameObjectsWithTag("Objective");
        foreach (GameObject obj in objectives)
        {
            Vector2Int? pos = GetObjectPosition(obj);
            if (pos.HasValue)
            {
                objectivePositions.Add(pos.Value);
                Debug.Log($"InfluenceMap: Objective at {pos.Value}");
            }
        }
        
        // Buscar amenazas en escena
        GameObject[] menaces = GameObject.FindGameObjectsWithTag("Menace");
        foreach (GameObject obj in menaces)
        {
            Vector2Int? pos = GetObjectPosition(obj);
            if (pos.HasValue)
            {
                menacePositions.Add(pos.Value);
                Debug.Log($"InfluenceMap: Menace at {pos.Value}");
            }
        }
    }
    
    Vector2Int? GetObjectPosition(GameObject obj)
    {
        if (obj == null) return null;
        
        // Buscar TileData en el objeto
        TileData tileData = obj.GetComponent<TileData>();
        if (tileData == null) tileData = obj.GetComponentInChildren<TileData>();
        if (tileData == null) tileData = obj.GetComponentInParent<TileData>();
        
        if (tileData != null) return tileData.gridPosition;
        
        // Buscar tile mas cercano como fallback
        return FindClosestTilePosition(obj.transform.position);
    }
    
    Vector2Int? FindClosestTilePosition(Vector3 worldPos)
    {
        Dictionary<Vector2Int, TileData> tileGrid = mapGenerator.GetTileGrid();
        if (tileGrid == null) return null;
        
        float closestDistance = float.MaxValue;
        Vector2Int? closestPos = null;
        
        foreach (var kvp in tileGrid)
        {
            if (kvp.Value == null) continue;
            
            float distance = Vector3.Distance(worldPos, kvp.Value.transform.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestPos = kvp.Key;
            }
        }
        
        return closestPos;
    }
    
    void InitializeInfluenceLayers()
    {
        int height = mapGenerator.mapHeight;
        int width = mapGenerator.mapWidth;
        
        baseInfluence = new float[height, width];
        objectiveInfluence = new float[height, width];
        menaceInfluence = new float[height, width];
        combinedInfluence = new float[height, width];
    }
    
    void CalculateBaseInfluence()
    {
        Debug.Log("InfluenceMap: Calculating base terrain influence");
        
        for (int x = 0; x < mapGenerator.mapHeight; x++)
        {
            for (int y = 0; y < mapGenerator.mapWidth; y++)
            {
                Vector2Int pos = new Vector2Int(x, y);
                TileData tile = mapGenerator.GetTileAtPosition(pos);
                
                if (tile != null)
                {
                    // Valor base del terreno considerando tipo y weight
                    float terrainValue = GetTerrainBaseValue(tile.tileType, tile.weight);
                    baseInfluence[x, y] = terrainValue;
                    
                    // Bonus por conectividad con tiles vecinos
                    float connectivityBonus = CalculateConnectivityBonus(tile);
                    baseInfluence[x, y] += connectivityBonus;
                    
                    // Bonus por proximidad al centro del mapa
                    float centerBonus = CalculateCenterBonus(pos);
                    baseInfluence[x, y] += centerBonus;
                }
            }
        }
    }
    
    float GetTerrainBaseValue(int tileType, float tileWeight)
    {
        float baseValueFromType = 0f;
        
        switch (tileType)
        {
            case 0: baseValueFromType = 1.0f; break;  // Tierra
            case 1: baseValueFromType = 1.2f; break;  // Hierba
            case 2: baseValueFromType = 0.5f; break;  // Agua
            case 3: baseValueFromType = 1.5f; break;  // Montaña
            default: baseValueFromType = 1.0f; break;
        }
        
        // Weight 0 = indeseable, Weight 1 = neutral, Weight >1 = deseable
        float weightContribution = (tileWeight - 1.0f) * 2.0f;
        
        return baseValueFromType + weightContribution;
    }
    
    float CalculateConnectivityBonus(TileData tile)
    {
        int walkableNeighbors = 0;
        foreach (TileData neighbor in tile.neighbors)
        {
            if (neighbor.walkable) walkableNeighbors++;
        }
        
        // Puntos de choke (2-3 conexiones) son estrategicamente valiosos
        if (walkableNeighbors >= 2 && walkableNeighbors <= 3)
            return 0.8f;
        // Intersecciones (4+ conexiones) tambien son valiosas
        else if (walkableNeighbors >= 4)
            return 0.5f;
        
        return 0f;
    }
    
    float CalculateCenterBonus(Vector2Int pos)
    {
        float centerX = mapGenerator.mapWidth / 2f;
        float centerY = mapGenerator.mapHeight / 2f;
        float distToCenter = Vector2.Distance(new Vector2(pos.x, pos.y), new Vector2(centerX, centerY));
        float maxDist = Mathf.Max(centerX, centerY);
        
        // Bonus decreciente segun distancia al centro
        return 0.5f * (1f - distToCenter / maxDist);
    }
    
    void CalculateObjectiveInfluence()
    {
        Debug.Log("InfluenceMap: Calculating objective influence");
        
        // Resetear capa de objetivos
        for (int x = 0; x < mapGenerator.mapHeight; x++)
        {
            for (int y = 0; y < mapGenerator.mapWidth; y++)
            {
                objectiveInfluence[x, y] = 0f;
            }
        }
        
        // Propagar desde cada objetivo
        foreach (Vector2Int objectivePos in objectivePositions)
        {
            PropagateInfluenceLinearStep(objectivePos, maxSourceIntensity, objectiveInfluence, true);
        }
    }
    
    void CalculateMenaceInfluence()
    {
        Debug.Log("InfluenceMap: Calculating menace influence");
        
        // Resetear capa de amenazas
        for (int x = 0; x < mapGenerator.mapHeight; x++)
        {
            for (int y = 0; y < mapGenerator.mapWidth; y++)
            {
                menaceInfluence[x, y] = 0f;
            }
        }
        
        // Propagar desde cada amenaza
        foreach (Vector2Int menacePos in menacePositions)
        {
            PropagateInfluenceLinearStep(menacePos, maxSourceIntensity, menaceInfluence, false);
        }
    }
    
    void PropagateInfluenceLinearStep(Vector2Int source, float maxStrength, float[,] influenceLayer, bool isPositive)
    {
        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        Dictionary<Vector2Int, int> distances = new Dictionary<Vector2Int, int>();
        
        queue.Enqueue(source);
        distances[source] = 0;
        
        while (queue.Count > 0)
        {
            Vector2Int current = queue.Dequeue();
            int currentDistance = distances[current];
            
            // Calcular influencia lineal para esta distancia
            float influenceAtDistance = CalculateLinearInfluence(maxStrength, currentDistance);
            
            // Aplicar factor de terreno si esta habilitado
            float finalInfluence = useTerrainPropagation ? 
                influenceAtDistance * GetTerrainPropagationFactor(current, isPositive) : 
                influenceAtDistance;
                
            influenceLayer[current.x, current.y] += isPositive ? finalInfluence : -finalInfluence;
            
            // Detener propagacion si se alcanzo distancia maxima
            if (currentDistance >= maxInfluenceDistance) continue;
            
            TileData currentTile = mapGenerator.GetTileAtPosition(current);
            if (currentTile != null)
            {
                foreach (TileData neighbor in currentTile.neighbors)
                {
                    Vector2Int neighborPos = neighbor.gridPosition;
                    
                    if (!distances.ContainsKey(neighborPos) && neighbor.walkable)
                    {
                        distances[neighborPos] = currentDistance + 1;
                        queue.Enqueue(neighborPos);
                    }
                }
            }
        }
    }
    
    float CalculateLinearInfluence(float maxStrength, int distance)
    {
        if (distance > maxInfluenceDistance) return 0f;
        
        // Decaimiento lineal: (maxDistance - distance) / maxDistance * maxStrength
        float linearFactor = (float)(maxInfluenceDistance - distance) / maxInfluenceDistance;
        return linearFactor * maxStrength;
    }
    
    float GetTerrainPropagationFactor(Vector2Int position, bool isPositiveInfluence)
    {
        TileData tile = mapGenerator.GetTileAtPosition(position);
        if (tile == null) return 1.0f;
        
        float factor = 1.0f;
        
        // Terreno no transitable reduce drasticamente la propagacion
        if (!tile.walkable)
        {
            factor *= 0.1f;
        }
        else
        {
            // Modificar factor segun tipo de terreno
            switch (tile.tileType)
            {
                case 0: // Tierra - neutral
                    factor = 1.0f;
                    break;
                case 1: // Hierba - favorable para influencia positiva
                    factor = isPositiveInfluence ? favorableTerrainBonus : 1.0f;
                    break;
                case 2: // Agua - dificil para cualquier influencia
                    factor = difficultTerrainPenalty;
                    break;
                case 3: // Montaña - favorable para positiva, dificil para negativa
                    factor = isPositiveInfluence ? favorableTerrainBonus : difficultTerrainPenalty;
                    break;
            }
            
            // Solo objetivos usan el weight del terreno para propagacion
            // Amenazas ignoran el weight y se propagan uniformemente
            if (isPositiveInfluence)
            {
                factor *= tile.weight;
            }
        }
        
        return factor;
    }
    
    void CombineAllInfluences()
    {
        Debug.Log("InfluenceMap: Combining all influence layers");
        
        for (int x = 0; x < mapGenerator.mapHeight; x++)
        {
            for (int y = 0; y < mapGenerator.mapWidth; y++)
            {
                // Combinar capas con sus pesos respectivos
                combinedInfluence[x, y] = 
                    (baseInfluence[x, y] * baseWeight) +
                    (objectiveInfluence[x, y] * objectiveWeight) +
                    (menaceInfluence[x, y] * menaceWeight);
                
                // Limitar valores extremos
                combinedInfluence[x, y] = Mathf.Clamp(combinedInfluence[x, y], -maxSourceIntensity, maxSourceIntensity);
            }
        }
    }
    
    public float GetInfluenceAt(Vector2Int position)
    {
        if (combinedInfluence == null) return 0f;
        if (position.x < 0 || position.x >= mapGenerator.mapHeight || 
            position.y < 0 || position.y >= mapGenerator.mapWidth) return 0f;
        
        return combinedInfluence[position.x, position.y];
    }
    
    public void GetInfluenceRange(out float min, out float max)
    {
        min = 0f;
        max = 0f;
        if (combinedInfluence == null) return;
        
        min = float.MaxValue;
        max = float.MinValue;
        
        for (int x = 0; x < mapGenerator.mapHeight; x++)
        {
            for (int y = 0; y < mapGenerator.mapWidth; y++)
            {
                float influence = combinedInfluence[x, y];
                min = Mathf.Min(min, influence);
                max = Mathf.Max(max, influence);
            }
        }
        
        // Rango minimo para visualizacion
        if (Mathf.Abs(max - min) < 1f)
        {
            min = -5f;
            max = 5f;
        }
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
                
                // Considerar weight del tile en la decision de movimiento
                float weightBonus = (neighbor.weight - 1.0f) * 2.0f;
                float adjustedInfluence = influence + weightBonus;
                
                if (adjustedInfluence > bestInfluence)
                {
                    bestInfluence = adjustedInfluence;
                    bestMove = neighbor.gridPosition;
                }
            }
        }
        
        return bestMove;
    }
    
    // Metodos para debug y testing
    public void DebugInfluenceBreakdown(Vector2Int position)
    {
        if (baseInfluence == null || objectiveInfluence == null || menaceInfluence == null) return;
        
        TileData tile = mapGenerator.GetTileAtPosition(position);
        string terrainInfo = tile != null ? $"Type: {tile.tileType}, Weight: {tile.weight}, Walkable: {tile.walkable}" : "No tile data";
        
        Debug.Log($"Influence breakdown at {position}:");
        Debug.Log($"  Terrain: {terrainInfo}");
        Debug.Log($"  Base: {baseInfluence[position.x, position.y]:F2} (x{baseWeight})");
        Debug.Log($"  Objective: {objectiveInfluence[position.x, position.y]:F2} (x{objectiveWeight})");
        Debug.Log($"  Menace: {menaceInfluence[position.x, position.y]:F2} (x{menaceWeight})");
        Debug.Log($"  Combined: {GetInfluenceAt(position):F2}");
    }
    
    public void DebugTerrainWeights()
    {
        Debug.Log("Terrain weights debug:");
        
        Dictionary<Vector2Int, TileData> tileGrid = mapGenerator.GetTileGrid();
        if (tileGrid == null) return;
        
        foreach (var kvp in tileGrid)
        {
            TileData tile = kvp.Value;
            if (tile != null)
            {
                float baseValue = GetTerrainBaseValue(tile.tileType, tile.weight);
                Debug.Log($"Tile at {kvp.Key}: Type={tile.tileType}, Weight={tile.weight}, BaseValue={baseValue:F2}");
            }
        }
    }
    
    public void AddTestObjectives()
    {
        // Agregar objetivos de prueba si no hay ninguno
        if (objectivePositions.Count == 0)
        {
            AddObjectiveManually(new Vector2Int(2, 2));
            AddObjectiveManually(new Vector2Int(5, 7));
        }
        
        if (menacePositions.Count == 0)
        {
            AddMenaceManually(new Vector2Int(8, 3));
            AddMenaceManually(new Vector2Int(3, 8));
        }
        
        GenerateInfluenceMap();
    }
    
    public void AddObjectiveManually(Vector2Int position)
    {
        if (!objectivePositions.Contains(position))
        {
            objectivePositions.Add(position);
            Debug.Log($"InfluenceMap: Manually added objective at {position}");
        }
    }
    
    public void AddMenaceManually(Vector2Int position)
    {
        if (!menacePositions.Contains(position))
        {
            menacePositions.Add(position);
            Debug.Log($"InfluenceMap: Manually added menace at {position}");
        }
    }
}