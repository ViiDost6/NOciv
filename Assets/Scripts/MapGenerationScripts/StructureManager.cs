using UnityEngine;
using System.Collections.Generic;

public class StructureManager : MonoBehaviour
{
    public MapGenerator mapGenerator;
    
    [Header("Prefabs")]
    public GameObject playerTowerPrefab;
    public GameObject enemyTowerPrefab;
    public GameObject resourcePrefab;

    [Header("Generation Settings")]
    [Range(1, 10)] public int maxTowersPerPlayer = 3;
    [Range(1, 50)] public int cornerRadiusPercent = 10;
    [Range(1, 100)] public int maxGenerationAttempts = 10;
    [Range(1, 20)] public int minResources = 5;
    [Range(1, 20)] public int maxResources = 10;
    [Range(1, 50)] public int resourceBorderMargin = 10;
    
    // Listas de datos
    private List<GameObject> placedStructures = new List<GameObject>();
    public List<Vector2Int> PlayerTowerPositions { get; private set; } = new List<Vector2Int>();
    public List<Vector2Int> EnemyTowerPositions { get; private set; } = new List<Vector2Int>();
    public List<Vector2Int> ResourcePositions { get; private set; } = new List<Vector2Int>();
    
    void Awake()
    {
        // Al iniciar, intentamos detectar si ya hay estructuras en la escena
        // Esto es útil si las colocaste manualmente o si la generación ocurrió antes
        ScanStructuresInScene();
    }

    public void ScanStructuresInScene()
    {
        // Si ya tenemos datos, no hace falta escanear (evita duplicados)
        if (PlayerTowerPositions.Count > 0 || EnemyTowerPositions.Count > 0) return;

        Debug.Log("StructureManager: Escaneando estructuras existentes en la escena...");

        // Iteramos sobre todos los hijos del StructureManager
        foreach (Transform child in transform)
        {
            ParseStructure(child.gameObject);
        }
        
        Debug.Log($"Escaner completado. Detectados: {PlayerTowerPositions.Count} Torres Jugador, {EnemyTowerPositions.Count} Torres Enemigas, {ResourcePositions.Count} Recursos.");
    }

    private void ParseStructure(GameObject obj)
    {
        // Intentamos deducir qué es y dónde está basándonos en el nombre
        // Formato esperado: "player_Tower_X_Y", "enemy_Tower_X_Y", "Resource_X_Y"
        
        string[] parts = obj.name.Split('_');
        if (parts.Length < 4) return; // Nombre no válido

        if (int.TryParse(parts[2], out int x) && int.TryParse(parts[3], out int y))
        {
            Vector2Int pos = new Vector2Int(x, y);
            
            // Reconstruir lista de estructuras colocadas
            if (!placedStructures.Contains(obj)) placedStructures.Add(obj);

            // --- FIX: Vincular con el TileData si el mapa ya existe ---
            if (mapGenerator != null)
            {
                TileData tile = mapGenerator.GetTileAtPosition(pos);
                if (tile != null)
                {
                    Building b = obj.GetComponent<Building>();
                    if (b != null)
                    {
                        tile.currentBuilding = b;
                        // Forzar actualización visual por si acaso
                        b.UpdateState();
                    }
                }
            }
            // -----------------------------------------------------------

            if (obj.name.ToLower().Contains("player"))
            {
                if (!PlayerTowerPositions.Contains(pos)) PlayerTowerPositions.Add(pos);
            }
            else if (obj.name.ToLower().Contains("enemy"))
            {
                if (!EnemyTowerPositions.Contains(pos)) EnemyTowerPositions.Add(pos);
            }
            else if (obj.name.Contains("Resource"))
            {
                if (!ResourcePositions.Contains(pos)) ResourcePositions.Add(pos);
            }
        }
    }

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
                Debug.Log($"Estructuras generadas exitosamente en intento {attempt + 1}");
                return;
            }
            
            ClearStructures();
        }
        
        Debug.LogWarning("No se pudo generar estructuras conectadas tras varios intentos");
    }
    
    void GeneratePlayerTowers()
    {
        int cornerRadius = CalculateCornerRadius();
        List<Vector2Int> cornerPositions = GetCornerPositions(0, 0, cornerRadius);
        PlaceTowersInCorner(cornerPositions, playerTowerPrefab, "player", PlayerTowerPositions);
    }
    
    void GenerateEnemyTowers()
    {
        int maxRow = mapGenerator.mapHeight - 1;
        int maxCol = mapGenerator.mapWidth - 1;
        int cornerRadius = CalculateCornerRadius();
        List<Vector2Int> cornerPositions = GetCornerPositions(maxRow, maxCol, cornerRadius);
        PlaceTowersInCorner(cornerPositions, enemyTowerPrefab, "enemy", EnemyTowerPositions);
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
            ResourcePositions.Add(position);
        }
    }
    
    // --- Helpers de Posicionamiento ---

    List<Vector2Int> GetValidResourcePositions()
    {
        List<Vector2Int> validPositions = new List<Vector2Int>();
        int margin = resourceBorderMargin;
        
        if (mapGenerator == null) return validPositions;

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
        TileData tile = mapGenerator.GetTileAtPosition(position);
        if (tile == null || tile.tileType != 0) return false;
        
        int cornerRadius = CalculateCornerRadius();
        if ((position.x <= cornerRadius && position.y <= cornerRadius) ||
            (position.x >= mapGenerator.mapHeight - 1 - cornerRadius && position.y >= mapGenerator.mapWidth - 1 - cornerRadius))
            return false;
        
        if (IsPositionOccupied(position)) return false;
            
        return true;
    }
    
    bool IsPositionOccupied(Vector2Int position)
    {
        return PlayerTowerPositions.Contains(position) || 
               EnemyTowerPositions.Contains(position) || 
               ResourcePositions.Contains(position);
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
    
    // --- Lógica de Conexión ---

    bool AreAllStructuresConnected()
    {
        if (PlayerTowerPositions.Count == 0 || EnemyTowerPositions.Count == 0 || ResourcePositions.Count == 0)
            return false;
        
        if (!AreAllTowersConnected()) return false;
        
        // Verificar recursos (simplificado)
        return true; 
    }
    
    bool AreAllTowersConnected()
    {
        // Simplificado para el ejemplo: asume conexión si hay camino
        return true;
    }
    
    // --- Utilidades ---

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
        if (mapGenerator == null) return false;
        if (position.x < 0 || position.x >= mapGenerator.mapHeight || 
            position.y < 0 || position.y >= mapGenerator.mapWidth)
            return false;
        
        TileData tile = mapGenerator.GetTileAtPosition(position);
        if (tile == null || tile.tileType != 0) return false;
            
        return true;
    }
    
    void PlaceTower(GameObject towerPrefab, Vector2Int position, string owner)
    {
        TileData tile = mapGenerator.GetTileAtPosition(position);
        if (tile == null) return;
        
        Vector3 worldPosition = tile.transform.position;
        GameObject tower = Instantiate(towerPrefab, worldPosition, Quaternion.identity);
        tower.transform.SetParent(transform);
        tower.name = $"{owner}_Tower_{position.x}_{position.y}"; // Nombre clave para el parseo
        
        // --- FIX CRÍTICO: Asignar Building al TileData ---
        Building b = tower.GetComponent<Building>();
        if (b != null)
        {
            tile.currentBuilding = b;
            b.isBase = true;
            
            // Asignar dueño inicial
            if (owner == "player") b.hasBeenClaimed = 1;
            else if (owner == "enemy") b.hasBeenClaimed = 2;
            else b.hasBeenClaimed = 0; 

            b.UpdateState(); // Actualizar sprite
        }
        // ------------------------------------------------

        placedStructures.Add(tower);
    }
    
    void PlaceResource(GameObject resourcePrefab, Vector2Int position)
    {
        TileData tile = mapGenerator.GetTileAtPosition(position);
        if (tile == null) return;
        
        Vector3 worldPosition = tile.transform.position;
        GameObject resource = Instantiate(resourcePrefab, worldPosition, Quaternion.identity);
        resource.transform.SetParent(transform);
        resource.name = $"Resource_{position.x}_{position.y}"; // Nombre clave para el parseo
        
        // --- FIX CRÍTICO: Asignar Building al TileData ---
        Building b = resource.GetComponent<Building>();
        if (b != null)
        {
            tile.currentBuilding = b;
            b.isBase = false;
            b.hasBeenClaimed = 0; // Neutral
            b.UpdateState();
        }
        // ------------------------------------------------

        placedStructures.Add(resource);
    }
    
    public void ClearStructures()
    {
        foreach (GameObject structure in placedStructures)
        {
            if (structure != null) 
            {
                // Intentar limpiar la referencia en el tile
                // Esto es un "best effort" ya que no tenemos la posición guardada explícitamente en la lista
                // Pero es buena práctica.
                Building b = structure.GetComponent<Building>();
                if (b != null)
                {
                    // Si el mapa sigue existiendo, busca tiles con este edificio
                    // (Opcional, consume rendimiento, pero asegura limpieza)
                }
                DestroyImmediate(structure);
            }
        }
        
        // También limpiar referencias en el mapa si es posible
        if (mapGenerator != null)
        {
            foreach(var tile in mapGenerator.GetTileGrid().Values)
            {
                if (tile.currentBuilding != null) tile.currentBuilding = null;
            }
        }

        placedStructures.Clear();
        PlayerTowerPositions.Clear();
        EnemyTowerPositions.Clear();
        ResourcePositions.Clear();
    }
}
// using UnityEngine;
// using System.Collections.Generic;

// public class StructureManager : MonoBehaviour
// {
//     public MapGenerator mapGenerator;
    
//     [Header("Prefabs")]
//     public GameObject playerTowerPrefab;
//     public GameObject enemyTowerPrefab;
//     public GameObject resourcePrefab;

//     [Header("Generation Settings")]
//     [Range(1, 10)] public int maxTowersPerPlayer = 3;
//     [Range(1, 50)] public int cornerRadiusPercent = 10;
//     [Range(1, 100)] public int maxGenerationAttempts = 10;
//     [Range(1, 20)] public int minResources = 5;
//     [Range(1, 20)] public int maxResources = 10;
//     [Range(1, 50)] public int resourceBorderMargin = 10;
    
//     // Listas de datos
//     private List<GameObject> placedStructures = new List<GameObject>();
//     public List<Vector2Int> PlayerTowerPositions { get; private set; } = new List<Vector2Int>();
//     public List<Vector2Int> EnemyTowerPositions { get; private set; } = new List<Vector2Int>();
//     public List<Vector2Int> ResourcePositions { get; private set; } = new List<Vector2Int>();
    
//     void Awake()
//     {
//         // Al iniciar, intentamos detectar si ya hay estructuras en la escena
//         // Esto es útil si las colocaste manualmente o si la generación ocurrió antes
//         ScanStructuresInScene();
//     }

//     public void ScanStructuresInScene()
//     {
//         // Si ya tenemos datos, no hace falta escanear (evita duplicados)
//         if (PlayerTowerPositions.Count > 0 || EnemyTowerPositions.Count > 0) return;

//         Debug.Log("StructureManager: Escaneando estructuras existentes en la escena...");

//         // Iteramos sobre todos los hijos del StructureManager
//         foreach (Transform child in transform)
//         {
//             ParseStructure(child.gameObject);
//         }
        
//         Debug.Log($"Escaner completado. Detectados: {PlayerTowerPositions.Count} Torres Jugador, {EnemyTowerPositions.Count} Torres Enemigas, {ResourcePositions.Count} Recursos.");
//     }

//     private void ParseStructure(GameObject obj)
//     {
//         // Intentamos deducir qué es y dónde está basándonos en el nombre
//         // Formato esperado: "player_Tower_X_Y", "enemy_Tower_X_Y", "Resource_X_Y"
        
//         string[] parts = obj.name.Split('_');
//         if (parts.Length < 4) return; // Nombre no válido

//         if (int.TryParse(parts[2], out int x) && int.TryParse(parts[3], out int y))
//         {
//             Vector2Int pos = new Vector2Int(x, y);
            
//             // Reconstruir lista de estructuras colocadas
//             if (!placedStructures.Contains(obj)) placedStructures.Add(obj);

//             if (obj.name.ToLower().Contains("player"))
//             {
//                 if (!PlayerTowerPositions.Contains(pos)) PlayerTowerPositions.Add(pos);
//             }
//             else if (obj.name.ToLower().Contains("enemy"))
//             {
//                 if (!EnemyTowerPositions.Contains(pos)) EnemyTowerPositions.Add(pos);
//             }
//             else if (obj.name.Contains("Resource"))
//             {
//                 if (!ResourcePositions.Contains(pos)) ResourcePositions.Add(pos);
//             }
//         }
//     }

//     public void GenerateAllStructures()
//     {
//         ClearStructures();
        
//         for (int attempt = 0; attempt < maxGenerationAttempts; attempt++)
//         {
//             GeneratePlayerTowers();
//             GenerateEnemyTowers();
//             GenerateResources();
            
//             if (AreAllStructuresConnected())
//             {
//                 Debug.Log($"Estructuras generadas exitosamente en intento {attempt + 1}");
//                 return;
//             }
            
//             ClearStructures();
//         }
        
//         Debug.LogWarning("No se pudo generar estructuras conectadas tras varios intentos");
//     }
    
//     void GeneratePlayerTowers()
//     {
//         int cornerRadius = CalculateCornerRadius();
//         List<Vector2Int> cornerPositions = GetCornerPositions(0, 0, cornerRadius);
//         PlaceTowersInCorner(cornerPositions, playerTowerPrefab, "player", PlayerTowerPositions);
//     }
    
//     void GenerateEnemyTowers()
//     {
//         int maxRow = mapGenerator.mapHeight - 1;
//         int maxCol = mapGenerator.mapWidth - 1;
//         int cornerRadius = CalculateCornerRadius();
//         List<Vector2Int> cornerPositions = GetCornerPositions(maxRow, maxCol, cornerRadius);
//         PlaceTowersInCorner(cornerPositions, enemyTowerPrefab, "enemy", EnemyTowerPositions);
//     }
    
//     void GenerateResources()
//     {
//         int resourcesToPlace = Random.Range(minResources, maxResources + 1);
//         List<Vector2Int> validPositions = GetValidResourcePositions();
//         ShufflePositions(validPositions);
        
//         for (int i = 0; i < Mathf.Min(resourcesToPlace, validPositions.Count); i++)
//         {
//             Vector2Int position = validPositions[i];
//             PlaceResource(resourcePrefab, position);
//             ResourcePositions.Add(position);
//         }
//     }
    
//     // --- Helpers de Posicionamiento ---

//     List<Vector2Int> GetValidResourcePositions()
//     {
//         List<Vector2Int> validPositions = new List<Vector2Int>();
//         int margin = resourceBorderMargin;
        
//         if (mapGenerator == null) return validPositions;

//         for (int row = margin; row < mapGenerator.mapHeight - margin; row++)
//         {
//             for (int col = margin; col < mapGenerator.mapWidth - margin; col++)
//             {
//                 Vector2Int position = new Vector2Int(row, col);
//                 if (IsPositionValidForResource(position))
//                     validPositions.Add(position);
//             }
//         }
//         return validPositions;
//     }
    
//     bool IsPositionValidForResource(Vector2Int position)
//     {
//         TileData tile = mapGenerator.GetTileAtPosition(position);
//         if (tile == null || tile.tileType != 0) return false;
        
//         int cornerRadius = CalculateCornerRadius();
//         if ((position.x <= cornerRadius && position.y <= cornerRadius) ||
//             (position.x >= mapGenerator.mapHeight - 1 - cornerRadius && position.y >= mapGenerator.mapWidth - 1 - cornerRadius))
//             return false;
        
//         if (IsPositionOccupied(position)) return false;
            
//         return true;
//     }
    
//     bool IsPositionOccupied(Vector2Int position)
//     {
//         return PlayerTowerPositions.Contains(position) || 
//                EnemyTowerPositions.Contains(position) || 
//                ResourcePositions.Contains(position);
//     }
    
//     void PlaceTowersInCorner(List<Vector2Int> positions, GameObject towerPrefab, string owner, List<Vector2Int> towerList)
//     {
//         List<Vector2Int> validPositions = new List<Vector2Int>();
        
//         foreach (Vector2Int pos in positions)
//         {
//             if (IsPositionValidForTower(pos))
//                 validPositions.Add(pos);
//         }
        
//         ShufflePositions(validPositions);
        
//         int towersToPlace = Mathf.Min(maxTowersPerPlayer, validPositions.Count);
//         for (int i = 0; i < towersToPlace; i++)
//         {
//             Vector2Int position = validPositions[i];
//             PlaceTower(towerPrefab, position, owner);
//             towerList.Add(position);
//         }
//     }
    
//     // --- Lógica de Conexión ---

//     bool AreAllStructuresConnected()
//     {
//         if (PlayerTowerPositions.Count == 0 || EnemyTowerPositions.Count == 0 || ResourcePositions.Count == 0)
//             return false;
        
//         if (!AreAllTowersConnected()) return false;
        
//         // Verificar recursos (simplificado)
//         return true; 
//     }
    
//     bool AreAllTowersConnected()
//     {
//         // Simplificado para el ejemplo: asume conexión si hay camino
//         return true;
//     }
    
//     // --- Utilidades ---

//     public int CalculateCornerRadius()
//     {
//         if (mapGenerator == null) return 0;
//         int minDimension = Mathf.Min(mapGenerator.mapHeight, mapGenerator.mapWidth);
//         float radius = minDimension * (cornerRadiusPercent / 100f);
//         return Mathf.RoundToInt(radius);
//     }
    
//     List<Vector2Int> GetCornerPositions(int centerRow, int centerCol, int radius)
//     {
//         List<Vector2Int> positions = new List<Vector2Int>();
//         for (int row = centerRow - radius; row <= centerRow + radius; row++)
//         {
//             for (int col = centerCol - radius; col <= centerCol + radius; col++)
//             {
//                 positions.Add(new Vector2Int(row, col));
//             }
//         }
//         return positions;
//     }
    
//     void ShufflePositions(List<Vector2Int> positions)
//     {
//         for (int i = 0; i < positions.Count; i++)
//         {
//             int randomIndex = Random.Range(i, positions.Count);
//             Vector2Int temp = positions[i];
//             positions[i] = positions[randomIndex];
//             positions[randomIndex] = temp;
//         }
//     }
    
//     bool IsPositionValidForTower(Vector2Int position)
//     {
//         if (mapGenerator == null) return false;
//         if (position.x < 0 || position.x >= mapGenerator.mapHeight || 
//             position.y < 0 || position.y >= mapGenerator.mapWidth)
//             return false;
        
//         TileData tile = mapGenerator.GetTileAtPosition(position);
//         if (tile == null || tile.tileType != 0) return false;
            
//         return true;
//     }
    
//     void PlaceTower(GameObject towerPrefab, Vector2Int position, string owner)
//     {
//         TileData tile = mapGenerator.GetTileAtPosition(position);
//         if (tile == null) return;
        
//         Vector3 worldPosition = tile.transform.position;
//         GameObject tower = Instantiate(towerPrefab, worldPosition, Quaternion.identity);
//         tower.transform.SetParent(transform);
//         tower.name = $"{owner}_Tower_{position.x}_{position.y}"; // Nombre clave para el parseo
        
//         placedStructures.Add(tower);
//     }
    
//     void PlaceResource(GameObject resourcePrefab, Vector2Int position)
//     {
//         TileData tile = mapGenerator.GetTileAtPosition(position);
//         if (tile == null) return;
        
//         Vector3 worldPosition = tile.transform.position;
//         GameObject resource = Instantiate(resourcePrefab, worldPosition, Quaternion.identity);
//         resource.transform.SetParent(transform);
//         resource.name = $"Resource_{position.x}_{position.y}"; // Nombre clave para el parseo
        
//         placedStructures.Add(resource);
//     }
    
//     public void ClearStructures()
//     {
//         foreach (GameObject structure in placedStructures)
//         {
//             if (structure != null) DestroyImmediate(structure);
//         }
//         placedStructures.Clear();
//         PlayerTowerPositions.Clear();
//         EnemyTowerPositions.Clear();
//         ResourcePositions.Clear();
//     }
// }