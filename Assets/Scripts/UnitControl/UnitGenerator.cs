using UnityEngine;
using System.Collections.Generic;
public class UnitGenerator : MonoBehaviour
{
    [System.Serializable]
    public class UnitSpawnData
    {
        public GameObject unitPrefab;
        public Vector2Int gridPosition;
        public bool isPlayerUnit;
        public int attackRange;
        public int movesTotal;
        // El resto de variables
    }

    public List<UnitSpawnData> unitsToSpawn = new List<UnitSpawnData>();
    public MapGenerator map;

    public void GenerateUnits()
    {
        GameObject unitContainer = GameObject.Find("UnitGenerator");

        for (int i = unitContainer.transform.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(unitContainer.transform.GetChild(i).gameObject);
        }

        foreach(var data in unitsToSpawn)
        {
            TileData tile = null;

            TileData[] allTiles = Object.FindObjectsByType<TileData>(FindObjectsSortMode.None);
            string searchName = $"Hex_{data.gridPosition.x}_{data.gridPosition.y}";

            foreach (TileData t in allTiles)
            {
                if (t.gameObject.name.StartsWith(searchName))
                {
                    tile = t;
                    break;
                }
            }

            GameObject newUnit = Instantiate(data.unitPrefab, unitContainer.transform);
            newUnit.name = $"Unit_{data.gridPosition.x}_{data.gridPosition.y}";
            newUnit.transform.position = new Vector3(tile.transform.position.x, tile.transform.position.y, -1);

            Unit u = newUnit.GetComponent<Unit>();
            if(u != null)
            {
                u.isPlayerUnit = data.isPlayerUnit;
                u.currentTile = tile;
                tile.hasUnit = true;
                u.attackRange = data.attackRange;
                u.movesTotal = data.movesTotal;
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
