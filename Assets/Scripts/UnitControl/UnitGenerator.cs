using UnityEngine;
using System.Collections.Generic;
public class UnitGenerator : MonoBehaviour
{
    public GameObject unitInfantryPrefab;
    public GameObject unitHeavyInfantryPrefab;
    public GameObject unitArtilleryPrefab;

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
                    unitPrefab = unitInfantryPrefab;
                    break;
                case Unit.UnitType.HeavyInfantry:
                    unitPrefab = unitHeavyInfantryPrefab;
                    break;
                case Unit.UnitType.Artillery:
                    unitPrefab = unitArtilleryPrefab;
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
                tile.hasUnit = true;

                u.outline = newUnit.transform.Find("Outline").gameObject;
                u.attackRangeIndicator = newUnit.transform.Find("AttackRange").gameObject;

                if(!u.isPlayerUnit) u.GetComponent<SpriteRenderer>().color = Color.red;

                switch(u.attackRange)
                {
                    case 1:
                        u.attackRangeIndicator.transform.localScale = new Vector3(4.85f, 4.85f, 1f);
                        break;
                    case 2:
                        u.attackRangeIndicator.transform.localScale = new Vector3(7.5f, 7.7f, 1f);
                        break;
                    case 3:
                        u.transform.Find("AttackRange").localScale = new Vector3(10.3f, 10.6f, 1f);
                        break;
                }
            }
        }
    }
}
