using System.Collections.Generic;
using UnityEngine;

public class Unit : MonoBehaviour
{
    public GameObject outline;
    public GameObject attackRangeIndicator;
    public bool isPlayerUnit = true;
    public int attackRange = 2;
    public List<TileData> reachableTiles = new List<TileData>();
    public int movesTotal = 3;
    public int movesLeftThisTurn;

    public TileData currentTile;

    public void SetOutline(bool state)
    {
        if(outline == null) outline = transform.Find("Outline").gameObject;
        outline.SetActive(state);
    }
}