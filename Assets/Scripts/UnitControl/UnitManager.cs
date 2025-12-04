using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using System.Collections;

public class UnitManager : MonoBehaviour
{
    public static UnitManager Instance;
    public TurnManager turnManager;
    public GameObject unitActionUIPrefab;
    public StructureManager structureManager;

    public enum State { NoSelection, UnitSelected, SelectingMovement, SelectingAttack }

    private Unit currentUnitHover = null;
    private TileData currentTileHover = null;
    public Unit currentUnitSelected = null;
    private GameObject currentUI = null;
    public State currentState = State.NoSelection;

    private Button attackBtn;
    private Button moveBtn;

    public int playerBaseCount = 0;
    public int aiBaseCount = 0;

    private void Awake()
    {
        Instance = this;
    }

    void Update()
    {
        if(turnManager.currentTurnState != TurnManager.TurnState.PlayerTurn) return;
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

        if (unitHit == currentUnitHover) return;

        if (currentUnitHover != null && currentUnitHover != currentUnitSelected) currentUnitHover.SetOutline(false);

        currentUnitHover = unitHit;

        if (currentUnitHover != null && IsSelectable(currentUnitHover) && currentUnitHover != currentUnitSelected) currentUnitHover.SetOutline(true);
    }

    private void UpdateNoSelection()
    {
        HandleHover();

        if (EventSystem.current.IsPointerOverGameObject()) return;

        if (currentUnitHover != null && IsSelectable(currentUnitHover) && Input.GetMouseButtonDown(0))
        {
            currentUnitSelected = currentUnitHover;
            currentState = State.UnitSelected;
        }
    }

    private void UpdateUnitSelected()
    {
        HandleHover();

        if (EventSystem.current.IsPointerOverGameObject()) return;

        if ((currentUnitHover == null || currentUnitHover == currentUnitSelected) && Input.GetMouseButtonDown(0))
        {
            currentUnitSelected.SetOutline(false);
            currentUnitSelected = null;
            DestroyUI();
            if(currentUnitHover != null) currentUnitHover.SetOutline(true);
            currentState = State.NoSelection;
            return;
        }

        if (currentUnitHover != null && IsSelectable(currentUnitHover) && Input.GetMouseButtonDown(0))
        {
            currentUnitSelected.SetOutline(false);
            currentUnitSelected = currentUnitHover;
            currentUnitSelected.SetOutline(true);
            DestroyUI();
        }

        if (currentUI == null && currentUnitSelected != null) CreateUIForSelected();
    }

    private void CreateUIForSelected()
    {
        currentUI = Instantiate(unitActionUIPrefab, currentUnitSelected.transform);

        currentUnitSelected.reachableTiles = CalculateTilesInRange(currentUnitSelected.currentTile, currentUnitSelected.attackRange);

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

        moveBtn.GetComponentInChildren<TMP_Text>().text = $"Moves: {currentUnitSelected.movesLeftThisTurn}";
        UpdateButtonVisual();
    }

    private void ToggleAttackMode()
    {
        if (currentState == State.SelectingAttack)
        {
            currentState = State.UnitSelected;
            ToggleAttackRange(false);
        }
        else
        {
            currentState = State.SelectingAttack;
            ToggleAttackRange(true);
        }

        UpdateButtonVisual();
    }

    public void ToggleAttackRange(bool show)
    {
        List<TileData> attackableTiles = CalculateTilesInRange(currentUnitSelected.currentTile, currentUnitSelected.attackRange);
        foreach (TileData tile in attackableTiles)
        {
            if(show) tile.SetOutline(show, Color.red);
            else tile.SetOutline(false, Color.black);
        }
    }

    private void ToggleMoveMode()
    {
        if (currentState == State.SelectingMovement) currentState = State.UnitSelected;
        else
        {
            if(currentState == State.SelectingAttack) 
            {
                List<TileData> attackableTiles = CalculateTilesInRange(currentUnitSelected.currentTile, currentUnitSelected.attackRange);
                foreach (TileData tile in attackableTiles)
                {
                    tile.SetOutline(false, Color.black);
                }
            }
            currentState = State.SelectingMovement;
        }

        UpdateButtonVisual();
    }

    public void UpdateButtonVisual()
    {
        if (attackBtn != null)
        {
            if(currentUnitSelected.hasAttackedThisTurn) attackBtn.interactable = false;
            bool active = currentState == State.SelectingAttack;
            ColorBlock cb = attackBtn.colors;
            cb.normalColor = active ? Color.red : Color.white;
            attackBtn.colors = cb;
            Image img = attackBtn.GetComponent<Image>();
            if (img != null) img.color = cb.normalColor;
        }

        if (moveBtn != null)
        {
            if(currentUnitSelected.movesLeftThisTurn <= 0) moveBtn.interactable = false;
            bool active = currentState == State.SelectingMovement;
            ColorBlock cb = moveBtn.colors;
            cb.normalColor = active ? Color.grey : Color.white;
            moveBtn.colors = cb;
            moveBtn.GetComponentInChildren<TMP_Text>().text = $"Moves: {currentUnitSelected.movesLeftThisTurn}";
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

        if (currentUnitHover != null && currentUnitHover != currentUnitSelected) currentUnitHover.SetOutline(false);

        currentUnitHover = enemyHit;

        if (currentUnitHover == null || currentUnitHover.isPlayerUnit) return;

        if (IsSelectable(currentUnitHover))
        {
            currentUnitHover.SetOutline(true);

            if (Input.GetMouseButtonDown(0))
            {
                Attack(currentUnitSelected, currentUnitHover);

                ToggleAttackMode();
                UpdateButtonVisual();
                currentUnitHover.SetOutline(false);
            }
        }
    }

    public void Attack(Unit attacker, Unit defender)
    {
        if(defender.hasArmor && !attacker.hasPiercing)
        {
            defender.health -= attacker.damage - 1;
        }
        else
        {
            defender.health -= attacker.damage;
        }

        attacker.hasAttackedThisTurn = true;

        defender.UpdateHealthUI();

        if(defender.health <= 0) defender.Death();
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

        if (tileHit == null || currentUnitSelected == null) return;

        // Comprobar si el tile está en los alcanzables
        if (currentUnitSelected.currentTile.neighbors.Contains(tileHit) && !tileHit.hasUnit)
        {
            tileHit.SetOutline(true);

            if (Input.GetMouseButtonDown(0))
            {
                MoveUnitToTile(currentUnitSelected, tileHit);
                currentState = State.UnitSelected;
                UpdateButtonVisual();

                // Recalcular tiles de movimiento si quieres mostrar de nuevo
                currentUnitSelected.reachableTiles = CalculateTilesInRange(tileHit, currentUnitSelected.attackRange);
            }
        }
    }

    private void MoveUnitToTile(Unit unit, TileData tile)
    {
        if (unit == null || tile == null) return;

        if(unit.unitType == Unit.UnitType.Artillery && (tile.tileType == 2 || unit.currentTile.tileType == 2)) unit.movesLeftThisTurn -= 2;
        else unit.movesLeftThisTurn--;

        if(unit.currentTile.tileType == 2) unit.attackRange--;
        if(tile.tileType == 2) unit.attackRange++;

        unit.currentTile.hasUnit = false;
        unit.currentTile = tile;
        tile.hasUnit = true;
        tile.SetOutline(false);

        if(unit.movesLeftThisTurn < 0) unit.movesLeftThisTurn = 0;

        if(tile.currentBuilding != null)
        {
            if(unit.isPlayerUnit && tile.currentBuilding.hasBeenClaimed == 1){}
            else if(!unit.isPlayerUnit && tile.currentBuilding.hasBeenClaimed == 2){}
            else
            {
                
                if(tile.currentBuilding.isBase)
                {
                    if(unit.isPlayerUnit)
                    {
                        playerBaseCount++;
                        aiBaseCount--;
                        tile.currentBuilding.hasBeenClaimed = 1;
                        if(aiBaseCount <= 0) turnManager.EndGame(true);
                    }
                    else
                    {
                        aiBaseCount++;
                        playerBaseCount--;
                        tile.currentBuilding.hasBeenClaimed = 2;
                        if(playerBaseCount <= 0) turnManager.EndGame(false);
                    }
                }
                else
                {
                    if(unit.isPlayerUnit)
                    {
                        if(tile.currentBuilding.hasBeenClaimed == 2) turnManager.aiResourceBuildings--;
                        tile.currentBuilding.hasBeenClaimed = 1;
                        turnManager.playerResourceBuildings++;
                    }
                    else
                    {
                        if(tile.currentBuilding.hasBeenClaimed == 1) turnManager.playerResourceBuildings--;
                        tile.currentBuilding.hasBeenClaimed = 2;
                        turnManager.aiResourceBuildings++;
                    } 
                } 

                tile.currentBuilding.UpdateState();          
            }
        }
        Vector3 endPos = new Vector3(tile.transform.position.x, tile.transform.position.y, -1);
        StartCoroutine(MovementCoroutine(unit, endPos));
    }

    private IEnumerator MovementCoroutine(Unit unit, Vector3 endPos)
    {
        Vector3 startPos = unit.transform.position;
        float duration = 0.5f;
        float elapsed = 0f;

        while(elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            unit.transform.position = Vector3.Lerp(startPos, endPos, t);
            yield return null;
        }
        unit.transform.position = endPos;
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
        else return !unit.isPlayerUnit && currentUnitSelected.reachableTiles.Contains(unit.currentTile);
    }

    public void DestroyUI()
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
