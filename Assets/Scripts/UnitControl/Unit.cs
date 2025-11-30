using System.Collections.Generic;
using UnityEngine;

public class Unit : MonoBehaviour
{
    public enum UnitType {Infantry, HeavyInfantry, Artillery}
    public UnitType unitType;
    public int cost;
    public int movesTotal;
    public int attackRange;
    public int health;
    public int damage;
    public bool hasPiercing;
    public bool hasArmor;

    public GameObject outline;
    public GameObject attackRangeIndicator;
    public bool isPlayerUnit;
    public List<TileData> reachableTiles = new List<TileData>();
    public int movesLeftThisTurn;
    public bool hasAttackedThisTurn;

    public TileData currentTile;

    public void SetOutline(bool state)
    {
        if(outline == null) outline = transform.Find("Outline").gameObject;
        outline.SetActive(state);
    }

    public void Death()
    {
        currentTile.hasUnit = false;
        Destroy(gameObject);
    }
}