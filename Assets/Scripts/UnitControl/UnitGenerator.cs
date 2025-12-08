using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class UnitGenerator : MonoBehaviour
{
    public GameObject unitInfantryPrefab;
    public GameObject unitHeavyInfantryPrefab;
    public GameObject unitArtilleryPrefab;
    public GameObject enemyInfantryPrefab;
    public GameObject enemyHeavyInfantryPrefab;
    public GameObject enemyArtilleryPrefab;

    [System.Serializable]
    public class UnitSpawnData
    {
        public Vector2Int gridPosition;
        public bool isPlayerUnit;
        public Unit.UnitType unitType;
    }

    public List<UnitSpawnData> unitsToSpawn = new List<UnitSpawnData>();
    public MapGenerator map;

    // --- MÉTODO EXISTENTE (Para spawn manual desde editor) ---
    public void GenerateUnits()
    {
        ClearUnits();
        foreach(var data in unitsToSpawn)
        {
            SpawnUnitAtPosition(data.gridPosition, data.isPlayerUnit, data.unitType);
        }
    }

    public void ClearUnits()
    {
        GameObject unitContainer = GameObject.FindWithTag("UnitGenerator");
        if (unitContainer == null) return;

        for (int i = unitContainer.transform.childCount - 1; i >= 0; i--)
        {
            Unit u = unitContainer.transform.GetChild(i).GetComponent<Unit>();
            if(u != null && u.currentTile != null) u.currentTile.hasUnit = false;
            DestroyImmediate(unitContainer.transform.GetChild(i).gameObject);
        }
    }

    // --- NUEVO MÉTODO PÚBLICO (Para el MatchInitializer) ---
    public void SpawnUnitAtPosition(Vector2Int gridPos, bool isPlayer, Unit.UnitType type)
    {
        if (map == null) map = FindObjectOfType<MapGenerator>();
        
        TileData tile = map.GetTileAtPosition(gridPos);
        if (tile == null)
        {
            Debug.LogError($"No se puede spawnear unidad en {gridPos}: Tile no existe.");
            return;
        }

        if (tile.hasUnit)
        {
            Debug.LogWarning($"Ya hay una unidad en {gridPos}. Saltando spawn.");
            return;
        }

        GameObject unitContainer = GameObject.FindWithTag("UnitGenerator");
        if (unitContainer == null)
        {
            unitContainer = new GameObject("UnitContainer");
            unitContainer.tag = "UnitGenerator";
        }

        GameObject prefab = GetPrefab(type, isPlayer);
        if (prefab == null) return;

        GameObject newUnit = Instantiate(prefab, unitContainer.transform);
        newUnit.name = $"{type}_{(isPlayer?"Player":"Enemy")}_{gridPos.x}_{gridPos.y}";
        // Ajustamos Z a -1 para que se renderice sobre el tile
        newUnit.transform.position = new Vector3(tile.transform.position.x, tile.transform.position.y, -1);

        Unit u = newUnit.GetComponent<Unit>();
        if(u != null)
        {
            u.isPlayerUnit = isPlayer;
            u.currentTile = tile;
            //u.movesTotal = GetMovesByType(type); // Asegurar stats correctos
            u.movesLeftThisTurn = u.movesTotal;
            u.hasAttackedThisTurn = false;
            
            // Vincular tile
            tile.hasUnit = true;
            u.outline = newUnit.transform.Find("Outline")?.gameObject;
        }
    }

    private GameObject GetPrefab(Unit.UnitType type, bool isPlayer)
    {
        switch(type)
        {
            case Unit.UnitType.Infantry: return isPlayer ? unitInfantryPrefab : enemyInfantryPrefab;
            case Unit.UnitType.HeavyInfantry: return isPlayer ? unitHeavyInfantryPrefab : enemyHeavyInfantryPrefab;
            case Unit.UnitType.Artillery: return isPlayer ? unitArtilleryPrefab : enemyArtilleryPrefab;
            default: return null;
        }
    }

    private int GetMovesByType(Unit.UnitType type)
    {
        // Valores por defecto si el prefab no los tiene bien configurados
        switch(type)
        {
            case Unit.UnitType.Infantry: return 3;
            case Unit.UnitType.HeavyInfantry: return 2;
            case Unit.UnitType.Artillery: return 2;
            default: return 3;
        }
    }
}




// using UnityEngine;
// using System.Collections.Generic;
// using System.Collections;
// public class UnitGenerator : MonoBehaviour
// {
//     public GameObject unitInfantryPrefab;
//     public GameObject unitHeavyInfantryPrefab;
//     public GameObject unitArtilleryPrefab;
//     public GameObject enemyInfantryPrefab;
//     public GameObject enemyHeavyInfantryPrefab;
//     public GameObject enemyArtilleryPrefab;

//     [System.Serializable]
//     public class UnitSpawnData
//     {
//         public Vector2Int gridPosition;
//         public bool isPlayerUnit;
//         public Unit.UnitType unitType;
//     }

//     public List<UnitSpawnData> unitsToSpawn = new List<UnitSpawnData>();
//     public MapGenerator map;

//     public void GenerateUnits()
//     {
//         GameObject unitContainer = GameObject.FindWithTag("UnitGenerator");

//         for (int i = unitContainer.transform.childCount - 1; i >= 0; i--)
//         {
//             unitContainer.transform.GetChild(i).GetComponent<Unit>().currentTile.hasUnit = false;
//             DestroyImmediate(unitContainer.transform.GetChild(i).gameObject);
//         }

//         foreach(var data in unitsToSpawn)
//         {
//             TileData tile = null;

//             TileData[] allTiles = Object.FindObjectsByType<TileData>(FindObjectsSortMode.None);
//             string searchName = $"Hex_{data.gridPosition.x}_{data.gridPosition.y}_";

//             foreach (TileData t in allTiles)
//             {
//                 if (t.gameObject.name.StartsWith(searchName))
//                 {
//                     tile = t;
//                     break;
//                 }
//             }

//             GameObject unitPrefab = null;
//             switch(data.unitType)
//             {
//                 case Unit.UnitType.Infantry:
//                     if(data.isPlayerUnit)
//                         unitPrefab = unitInfantryPrefab;
//                     else
//                         unitPrefab = enemyInfantryPrefab;
//                     break;
//                 case Unit.UnitType.HeavyInfantry:
//                     if(data.isPlayerUnit)
//                         unitPrefab = unitHeavyInfantryPrefab;
//                     else
//                         unitPrefab = enemyHeavyInfantryPrefab;
//                     break;
//                 case Unit.UnitType.Artillery:
//                     if(data.isPlayerUnit)
//                         unitPrefab = unitArtilleryPrefab;
//                     else
//                         unitPrefab = enemyArtilleryPrefab;
//                     break;
//             }
//             GameObject newUnit = Instantiate(unitPrefab, unitContainer.transform);
//             newUnit.name = $"{data.unitType}_{data.gridPosition.x}_{data.gridPosition.y}";
//             newUnit.transform.position = new Vector3(tile.transform.position.x, tile.transform.position.y, -1);

//             Unit u = newUnit.GetComponent<Unit>();
//             if(u != null)
//             {
//                 u.isPlayerUnit = data.isPlayerUnit;
//                 u.currentTile = tile;
//                 u.movesLeftThisTurn = u.movesTotal;
//                 u.hasAttackedThisTurn = false;
//                 tile.hasUnit = true;

//                 u.outline = newUnit.transform.Find("Outline").gameObject;
    
//             }
//         }
//     }
// }
