using UnityEngine;
using System.Collections.Generic;
public class UnitGenerator : MonoBehaviour
{
    [System.Serializable]
    public class UnitSpawnData
    {
        public GameObject unitPrefab;
        public Vector2Int gridPosition;
        public bool isPlayerUnit = true;
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
            newUnit.transform.position = tile.transform.position;

            Unit u = newUnit.GetComponent<Unit>();
            if(u != null)
            {
                u.isPlayerUnit = data.isPlayerUnit;
                u.currentTile = tile;
                u.outline = u.outline = newUnit.transform.Find("Outline").gameObject;
                if(!u.isPlayerUnit) u.GetComponent<SpriteRenderer>().color = Color.red;
            }
        }
    }
}
