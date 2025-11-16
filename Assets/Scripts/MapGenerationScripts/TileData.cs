using UnityEngine;
using System.Collections.Generic;

public class TileData : MonoBehaviour
{
    public int tileType;
    public bool walkable;
    public bool canHide;
    public float weight;
    public List<TileData> neighbors = new List<TileData>();
    public Vector2Int gridPosition;
    public GameObject outline;
    public bool hasUnit = false;
    
    public void Initialize(int type, bool canWalk, float cost, int row, int col)
    {
        tileType = type;
        walkable = canWalk;
        weight = cost;
        gridPosition = new Vector2Int(row, col);
    }
    
    public void AddNeighbor(TileData neighbor)
    {
        if (!neighbors.Contains(neighbor))
            neighbors.Add(neighbor);
    }

    public void SetOutline(bool state)
    {
        if(outline == null) outline = transform.Find("Outline").gameObject;
        outline.SetActive(state);
    }
}