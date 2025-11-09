using UnityEngine;
using System.Collections.Generic;

public class StructureManager : MonoBehaviour
{
    [Header("Referencias")]
    public MapGenerator mapGenerator;
    
    [Header("Torres de Base")]
    public GameObject playerTowerPrefab;
    public GameObject enemyTowerPrefab;
    [Range(1, 10)] public int maxTowersPerPlayer = 3;
    [Range(1, 50)] public int cornerRadiusPercent = 10;
    [Range(1, 100)] public int maxGenerationAttempts = 10;
    
    [Header("Puntos de Recursos")]
    public GameObject resourcePrefab;
    [Range(1, 20)] public int minResources = 5;
    [Range(1, 20)] public int maxResources = 10;
    [Range(1, 50)] public int resourceBorderMargin = 10;
    
    private List<GameObject> placedStructures = new List<GameObject>();
    private List<Vector2Int> playerTowerPositions = new List<Vector2Int>();
    private List<Vector2Int> enemyTowerPositions = new List<Vector2Int>();
    private List<Vector2Int> resourcePositions = new List<Vector2Int>();
    
    public void GenerateAllStructures()
    {
        ClearStructures();
        
        for (int attempt = 0; attempt < maxGenerationAttempts; attempt++)
        {
            GeneratePlayerTowers();
            GenerateEnemyTowers();
            GenerateResources();
            
            if (AreAllStructuresConnected())
            {
                Debug.Log($"Estructuras generadas exitosamente en el intento {attempt + 1}");
                return;
            }
            
            ClearStructures();
        }
        
        Debug.LogWarning("No se pudo generar estructuras conectadas después de " + maxGenerationAttempts + " intentos");
    }
    
    void GeneratePlayerTowers()
    {
        int cornerRadius = CalculateCornerRadius();
        List<Vector2Int> cornerPositions = GetCornerPositions(0, 0, cornerRadius);
        PlaceTowersInCorner(cornerPositions, playerTowerPrefab, "player", playerTowerPositions);
    }
    
    void GenerateEnemyTowers()
    {
        int maxRow = mapGenerator.mapHeight - 1;
        int maxCol = mapGenerator.mapWidth - 1;
        int cornerRadius = CalculateCornerRadius();
        
        List<Vector2Int> cornerPositions = GetCornerPositions(maxRow, maxCol, cornerRadius);
        PlaceTowersInCorner(cornerPositions, enemyTowerPrefab, "enemy", enemyTowerPositions);
    }
    
    void GenerateResources()
    {
        int resourcesToPlace = Random.Range(minResources, maxResources + 1);
        List<Vector2Int> validPositions = GetValidResourcePositions();
        
        ShufflePositions(validPositions);
        
        for (int i = 0; i < Mathf.Min(resourcesToPlace, validPositions.Count); i++)
        {
            Vector2Int position = validPositions[i];
            PlaceResource(resourcePrefab, position);
            resourcePositions.Add(position);
        }
    }
    
    List<Vector2Int> GetValidResourcePositions()
    {
        List<Vector2Int> validPositions = new List<Vector2Int>();
        int margin = resourceBorderMargin;
        
        for (int row = margin; row < mapGenerator.mapHeight - margin; row++)
        {
            for (int col = margin; col < mapGenerator.mapWidth - margin; col++)
            {
                Vector2Int position = new Vector2Int(row, col);
                if (IsPositionValidForResource(position))
                    validPositions.Add(position);
            }
        }
        
        return validPositions;
    }
    
    bool IsPositionValidForResource(Vector2Int position)
    {
        // Verificar terreno tipo 0
        TileData tile = mapGenerator.GetTileAtPosition(position);
        if (tile == null || tile.tileType != 0)
            return false;
        
        // Verificar que no esté en área de torres
        int cornerRadius = CalculateCornerRadius();
        if ((position.x <= cornerRadius && position.y <= cornerRadius) || // Esquina inferior izquierda
            (position.x >= mapGenerator.mapHeight - 1 - cornerRadius && position.y >= mapGenerator.mapWidth - 1 - cornerRadius)) // Esquina superior derecha
            return false;
        
        // Verificar que no hay estructura en esa posición
        if (IsPositionOccupied(position))
            return false;
            
        return true;
    }
    
    bool IsPositionOccupied(Vector2Int position)
    {
        return playerTowerPositions.Contains(position) || 
               enemyTowerPositions.Contains(position) || 
               resourcePositions.Contains(position);
    }
    
    void PlaceTowersInCorner(List<Vector2Int> positions, GameObject towerPrefab, string owner, List<Vector2Int> towerList)
    {
        List<Vector2Int> validPositions = new List<Vector2Int>();
        
        foreach (Vector2Int pos in positions)
        {
            if (IsPositionValidForTower(pos))
                validPositions.Add(pos);
        }
        
        ShufflePositions(validPositions);
        
        int towersToPlace = Mathf.Min(maxTowersPerPlayer, validPositions.Count);
        for (int i = 0; i < towersToPlace; i++)
        {
            Vector2Int position = validPositions[i];
            PlaceTower(towerPrefab, position, owner);
            towerList.Add(position);
        }
    }
    
    bool AreAllStructuresConnected()
    {
        if (playerTowerPositions.Count == 0 || enemyTowerPositions.Count == 0 || resourcePositions.Count == 0)
            return false;
        
        // Verificar conexión entre todas las torres
        if (!AreAllTowersConnected()) return false;
        
        // Verificar que todos los recursos son accesibles desde al menos una torre
        foreach (Vector2Int resourcePos in resourcePositions)
        {
            bool resourceAccessible = false;
            
            foreach (Vector2Int playerTower in playerTowerPositions)
            {
                if (AreTowersConnected(playerTower, resourcePos))
                {
                    resourceAccessible = true;
                    break;
                }
            }
            
            if (!resourceAccessible)
            {
                foreach (Vector2Int enemyTower in enemyTowerPositions)
                {
                    if (AreTowersConnected(enemyTower, resourcePos))
                    {
                        resourceAccessible = true;
                        break;
                    }
                }
            }
            
            if (!resourceAccessible) return false;
        }
        
        return true;
    }
    
    bool AreAllTowersConnected()
    {
        // Verificar que todas las torres player están conectadas entre sí
        for (int i = 1; i < playerTowerPositions.Count; i++)
        {
            if (!AreTowersConnected(playerTowerPositions[0], playerTowerPositions[i]))
                return false;
        }
        
        // Verificar que todas las torres enemy están conectadas entre sí
        for (int i = 1; i < enemyTowerPositions.Count; i++)
        {
            if (!AreTowersConnected(enemyTowerPositions[0], enemyTowerPositions[i]))
                return false;
        }
        
        // Verificar que al menos una torre player está conectada con una torre enemy
        foreach (Vector2Int playerTower in playerTowerPositions)
        {
            foreach (Vector2Int enemyTower in enemyTowerPositions)
            {
                if (AreTowersConnected(playerTower, enemyTower))
                    return true;
            }
        }
        
        return false;
    }
    
    bool AreTowersConnected(Vector2Int start, Vector2Int end)
    {
        Queue<Vector2Int> openSet = new Queue<Vector2Int>();
        HashSet<Vector2Int> closedSet = new HashSet<Vector2Int>();
        
        openSet.Enqueue(start);
        closedSet.Add(start);
        
        while (openSet.Count > 0)
        {
            Vector2Int current = openSet.Dequeue();
            
            if (current == end)
                return true;
            
            TileData currentTile = mapGenerator.GetTileAtPosition(current);
            if (currentTile == null) continue;
            
            foreach (TileData neighbor in currentTile.neighbors)
            {
                if (!closedSet.Contains(neighbor.gridPosition) && neighbor.walkable)
                {
                    openSet.Enqueue(neighbor.gridPosition);
                    closedSet.Add(neighbor.gridPosition);
                }
            }
        }
        
        return false;
    }
    
    public int CalculateCornerRadius()
    {
        if (mapGenerator == null) return 0;
        int minDimension = Mathf.Min(mapGenerator.mapHeight, mapGenerator.mapWidth);
        float radius = minDimension * (cornerRadiusPercent / 100f);
        return Mathf.RoundToInt(radius);
    }
    
    List<Vector2Int> GetCornerPositions(int centerRow, int centerCol, int radius)
    {
        List<Vector2Int> positions = new List<Vector2Int>();
        
        for (int row = centerRow - radius; row <= centerRow + radius; row++)
        {
            for (int col = centerCol - radius; col <= centerCol + radius; col++)
            {
                positions.Add(new Vector2Int(row, col));
            }
        }
        
        return positions;
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
    
    bool IsPositionValidForTower(Vector2Int position)
    {
        if (position.x < 0 || position.x >= mapGenerator.mapHeight || 
            position.y < 0 || position.y >= mapGenerator.mapWidth)
            return false;
        
        TileData tile = mapGenerator.GetTileAtPosition(position);
        if (tile == null || tile.tileType != 0)
            return false;
            
        return true;
    }
    
    void PlaceTower(GameObject towerPrefab, Vector2Int position, string owner)
    {
        TileData tile = mapGenerator.GetTileAtPosition(position);
        if (tile == null) return;
        
        Vector3 worldPosition = tile.transform.position;
        GameObject tower = Instantiate(towerPrefab, worldPosition, Quaternion.identity);
        tower.transform.SetParent(transform);
        tower.name = $"{owner}_Tower_{position.x}_{position.y}";
        
        placedStructures.Add(tower);
    }
    
    void PlaceResource(GameObject resourcePrefab, Vector2Int position)
    {
        TileData tile = mapGenerator.GetTileAtPosition(position);
        if (tile == null) return;
        
        Vector3 worldPosition = tile.transform.position;
        GameObject resource = Instantiate(resourcePrefab, worldPosition, Quaternion.identity);
        resource.transform.SetParent(transform);
        resource.name = $"Resource_{position.x}_{position.y}";
        
        placedStructures.Add(resource);
    }
    
    public void ClearStructures()
    {
        foreach (GameObject structure in placedStructures)
        {
            if (structure != null)
                DestroyImmediate(structure);
        }
        placedStructures.Clear();
        playerTowerPositions.Clear();
        enemyTowerPositions.Clear();
        resourcePositions.Clear();
    }
    
    public int GetStructureCount()
    {
        return placedStructures.Count;
    }
}