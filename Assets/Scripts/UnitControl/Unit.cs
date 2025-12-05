using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Unit : MonoBehaviour
{
    public enum UnitType {Infantry, HeavyInfantry, Artillery}
    public UnitType unitType;
    public int cost;
    public int movesTotal;
    public int attackRange;
    public int health;
    public int maxHealth;
    public int damage;
    public bool hasPiercing;
    public bool hasArmor;

    public GameObject outline;
    public bool isPlayerUnit;
    public int movesLeftThisTurn;
    public bool hasAttackedThisTurn;

    public TileData currentTile;
    public List<TileData> attackableTiles = new List<TileData>();
    public List<TileData> reachableTiles = new List<TileData>();
    public List<TileData> shownAttackTiles = new List<TileData>();
    public List<TileData> shownMoveTiles = new List<TileData>();

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

    public void UpdateHealthUI()
    {
        Transform healthTransform = GetChildByName("Health");
        Image healthBar = healthTransform.GetComponent<Image>();

        healthBar.fillAmount = (float)health / maxHealth;
    }

    public Transform GetChildByName(string childName)
    {
        foreach (Transform t in GetComponentsInChildren<Transform>(true))
        {
            if (t.name == childName)
                return t;
        }
        return null;
    }
}