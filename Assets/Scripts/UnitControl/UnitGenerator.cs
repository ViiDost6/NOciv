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

    public void GenerateUnits()
    {
        GameObject unitContainer = GameObject.FindWithTag("UnitGenerator");

        for (int i = unitContainer.transform.childCount - 1; i >= 0; i--)
        {
            unitContainer.transform.GetChild(i).GetComponent<Unit>().currentTile.hasUnit = false;
            DestroyImmediate(unitContainer.transform.GetChild(i).gameObject);
        }

        foreach(var data in unitsToSpawn)
        {
            TileData tile = null;

            TileData[] allTiles = Object.FindObjectsByType<TileData>(FindObjectsSortMode.None);
            string searchName = $"Hex_{data.gridPosition.x}_{data.gridPosition.y}_";

            foreach (TileData t in allTiles)
            {
                if (t.gameObject.name.StartsWith(searchName))
                {
                    tile = t;
                    break;
                }
            }

            GameObject unitPrefab = null;
            switch(data.unitType)
            {
                case Unit.UnitType.Infantry:
                    if(data.isPlayerUnit)
                        unitPrefab = unitInfantryPrefab;
                    else
                        unitPrefab = enemyInfantryPrefab;
                    break;
                case Unit.UnitType.HeavyInfantry:
                    if(data.isPlayerUnit)
                        unitPrefab = unitHeavyInfantryPrefab;
                    else
                        unitPrefab = enemyHeavyInfantryPrefab;
                    break;
                case Unit.UnitType.Artillery:
                    if(data.isPlayerUnit)
                        unitPrefab = unitArtilleryPrefab;
                    else
                        unitPrefab = enemyArtilleryPrefab;
                    break;
            }
            GameObject newUnit = Instantiate(unitPrefab, unitContainer.transform);
            newUnit.name = $"{data.unitType}_{data.gridPosition.x}_{data.gridPosition.y}";
            newUnit.transform.position = new Vector3(tile.transform.position.x, tile.transform.position.y, -1);

            Unit u = newUnit.GetComponent<Unit>();
            if(u != null)
            {
                u.isPlayerUnit = data.isPlayerUnit;
                u.currentTile = tile;
                u.movesLeftThisTurn = u.movesTotal;
                u.hasAttackedThisTurn = false;
                tile.hasUnit = true;

                u.outline = newUnit.transform.Find("Outline").gameObject;
    
            }
        }
    }
}
