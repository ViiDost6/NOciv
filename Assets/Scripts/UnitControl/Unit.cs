using UnityEngine;
using UnityEngine.Tilemaps;

public class Unit : MonoBehaviour
{
    public GameObject outline;
    public bool isPlayerUnit = true;

    public TileData currentTile;

    public void SetOutline(bool state)
    {
        outline.SetActive(state);
    }

    public bool IsSelectable()
    {
        return isPlayerUnit; // Añadir condiciones más adelante
    }
}