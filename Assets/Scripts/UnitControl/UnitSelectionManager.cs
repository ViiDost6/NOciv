using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class UnitSelectionManager : MonoBehaviour
{
    public static UnitSelectionManager Instance;
    public GameObject unitActionUIPrefab;

    private enum State { NoSelection, UnitSelected, SelectingMovement, SelectingAttack }

    private Unit currentHover = null;
    private TileData currentTileHover = null;
    private Unit currentSelected = null;
    private GameObject currentUI = null;
    private State currentState = State.NoSelection;

    private Button attackBtn;
    private Button moveBtn;

    private void Awake()
    {
        Instance = this;
    }

    void Update()
    {
        switch (currentState)
        {
            case State.NoSelection:       UpdateNoSelection();       break;
            case State.UnitSelected:      UpdateUnitSelected();      break;
            case State.SelectingMovement: UpdateSelectingMovement(); break;
            case State.SelectingAttack:   UpdateSelectingAttack();   break;
        }
    }

    private void HandleHover()
    {
        Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        RaycastHit2D hit = Physics2D.Raycast(mousePos, Vector2.zero);
        Unit unitHit = hit.collider != null ? hit.collider.GetComponent<Unit>() : null;

        if (unitHit == currentHover) return;

        if (currentHover != null && currentHover != currentSelected) currentHover.SetOutline(false);

        currentHover = unitHit;

        if (currentHover != null && IsSelectable(currentHover) && currentHover != currentSelected) currentHover.SetOutline(true);
    }

    private void UpdateNoSelection()
    {
        HandleHover();

        if (EventSystem.current.IsPointerOverGameObject()) return;

        if (currentHover != null && IsSelectable(currentHover) && Input.GetMouseButtonDown(0))
        {
            currentSelected = currentHover;
            currentState = State.UnitSelected;
        }
    }

    private void UpdateUnitSelected()
    {
        HandleHover();

        if (EventSystem.current.IsPointerOverGameObject()) return;

        if ((currentHover == null || currentHover == currentSelected) && Input.GetMouseButtonDown(0))
        {
            currentSelected.SetOutline(false);
            currentSelected = null;
            DestroyUI();
            currentState = State.NoSelection;
            return;
        }

        if (currentHover != null && IsSelectable(currentHover) && Input.GetMouseButtonDown(0))
        {
            currentSelected.SetOutline(false);
            currentSelected = currentHover;
            currentSelected.SetOutline(true);
            DestroyUI();
        }

        if (currentUI == null && currentSelected != null) CreateUIForSelected();
    }

    private void CreateUIForSelected()
    {
        currentUI = Instantiate(unitActionUIPrefab, currentSelected.transform);

        currentSelected.reachableTiles = CalculateTilesInRange(currentSelected.currentTile, currentSelected.attackRange);

        Transform canvas = currentUI.transform.Find("Canvas");
        if (canvas == null)
        {
            Debug.LogError("UI prefab sin Canvas");
            return;
        }

        attackBtn = canvas.Find("AttackButton")?.GetComponent<Button>();
        moveBtn = canvas.Find("MoveButton")?.GetComponent<Button>();

        if (attackBtn != null)
        {
            attackBtn.onClick.RemoveAllListeners();
            attackBtn.onClick.AddListener(() => ToggleAttackMode());
        }

        if (moveBtn != null)
        {
            moveBtn.onClick.RemoveAllListeners();
            moveBtn.onClick.AddListener(() => ToggleMoveMode());
        }

        UpdateButtonVisual();
    }

    private void ToggleAttackMode()
    {
        if (currentState == State.SelectingAttack)
        {
            currentSelected.attackRangeIndicator.SetActive(false);
            currentState = State.UnitSelected;
        }
        else
        {
            currentSelected.attackRangeIndicator.SetActive(true);
            currentState = State.SelectingAttack;
        }

        UpdateButtonVisual();
    }

    private void ToggleMoveMode()
    {
        if (currentState == State.SelectingMovement) currentState = State.UnitSelected;
        else
        {
            if(currentState == State.SelectingAttack) currentSelected.attackRangeIndicator.SetActive(false);
            currentState = State.SelectingMovement;
        }

        UpdateButtonVisual();
    }

    private void UpdateButtonVisual()
    {
        if (attackBtn != null)
        {
            bool active = currentState == State.SelectingAttack;
            ColorBlock cb = attackBtn.colors;
            cb.normalColor = active ? Color.red : Color.white;
            attackBtn.colors = cb;
            Image img = attackBtn.GetComponent<Image>();
            if (img != null) img.color = cb.normalColor;
        }

        if (moveBtn != null)
        {
            bool active = currentState == State.SelectingMovement;
            ColorBlock cb = moveBtn.colors;
            cb.normalColor = active ? Color.grey : Color.white;
            moveBtn.colors = cb;
            Image img = moveBtn.GetComponent<Image>();
            if (img != null) img.color = cb.normalColor;
        }
    }

    private void UpdateSelectingAttack()
    {
        if (EventSystem.current.IsPointerOverGameObject()) return;

        Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        RaycastHit2D hit = Physics2D.Raycast(mousePos, Vector2.zero);
        Unit enemyHit = hit.collider != null ? hit.collider.GetComponent<Unit>() : null;

        if (currentHover != null && currentHover != currentSelected) currentHover.SetOutline(false);

        currentHover = enemyHit;

        if (currentHover == null || currentHover.isPlayerUnit) return;

        if (IsSelectable(currentHover))
        {
            currentHover.SetOutline(true);

            if (Input.GetMouseButtonDown(0))
            {
                Debug.Log($"Atacar unidad: {currentHover.name}");

                // Implementar ataque

                currentSelected.attackRangeIndicator.SetActive(false);
                currentState = State.UnitSelected;
                UpdateButtonVisual();
                currentHover.SetOutline(false);
            }
        }
    }

    private void UpdateSelectingMovement()
    {
        if (EventSystem.current.IsPointerOverGameObject()) return;

        Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        RaycastHit2D hit = Physics2D.Raycast(mousePos, Vector2.zero);
        TileData tileHit = hit.collider != null ? hit.collider.GetComponent<TileData>() : null;

        // Limpiar outline anterior
        if (currentTileHover != null)
            currentTileHover.SetOutline(false);

        currentTileHover = tileHit;

        if (tileHit == null || currentSelected == null) return;

        // Comprobar si el tile está en los alcanzables
        if (currentSelected.currentTile.neighbors.Contains(tileHit) && !tileHit.hasUnit)
        {
            tileHit.SetOutline(true);

            if (Input.GetMouseButtonDown(0))
            {
                MoveUnitToTile(currentSelected, tileHit);
                currentState = State.UnitSelected;
                UpdateButtonVisual();

                // Recalcular tiles de movimiento si quieres mostrar de nuevo
                currentSelected.reachableTiles = CalculateTilesInRange(tileHit, currentSelected.attackRange);
            }
        }
    }

    private void MoveUnitToTile(Unit unit, TileData tile)
    {
        if (unit == null || tile == null) return;

        unit.transform.position = new Vector3(tile.transform.position.x, tile.transform.position.y, -1);
        unit.currentTile.hasUnit = false;
        unit.currentTile = tile;
        tile.hasUnit = true;
        tile.SetOutline(false);
    }

    private List<TileData> CalculateTilesInRange(TileData startTile, int range)
    {
        List<TileData> inRange = new List<TileData>();
        HashSet<TileData> visited = new HashSet<TileData>();
        Queue<(TileData tile, int level)> queue = new Queue<(TileData tile, int level)>();

        queue.Enqueue((startTile, 0));
        visited.Add(startTile);

        while (queue.Count > 0)
        {
            var (tile, level) = queue.Dequeue();

            if (level > 0)
                inRange.Add(tile);

            if (level >= range)
                continue;

            foreach (TileData neighbor in tile.neighbors)
            {
                if (!visited.Contains(neighbor) && neighbor.walkable)
                {
                    visited.Add(neighbor);
                    queue.Enqueue((neighbor, level + 1));
                }
            }
        }

        return inRange;
    }

    public bool IsSelectable(Unit unit)
    {
        if(currentState != State.SelectingAttack) return unit.isPlayerUnit;
        else return !unit.isPlayerUnit && currentSelected.reachableTiles.Contains(unit.currentTile);
    }

    private void DestroyUI()
    {
        if (currentUI != null)
        {
            if (attackBtn != null) attackBtn.onClick.RemoveAllListeners();
            if (moveBtn != null) moveBtn.onClick.RemoveAllListeners();

            Destroy(currentUI);
            currentUI = null;
        }

        attackBtn = null;
        moveBtn = null;
    }
}
