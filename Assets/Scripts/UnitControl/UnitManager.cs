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

    public class PathNode
    {
        public TileData parent;
        public int cost;

        public PathNode(TileData parent, int cost)
        {
            this.parent = parent;
            this.cost = cost;
        }
    }

    private Dictionary<TileData, PathNode> lastPathMap = new Dictionary<TileData, PathNode>();

    public List<TileData> CalculateReachableTilesWithPaths(TileData startTile, int movementPoints)
    {
        lastPathMap.Clear();
        List<TileData> reachable = new List<TileData>();

        // Cola para BFS modificado (tile, coste acumulado)
        Queue<(TileData tile, int cost)> queue = new Queue<(TileData tile, int cost)>();
        queue.Enqueue((startTile, 0));
        lastPathMap[startTile] = new PathNode(null, 0);

        while(queue.Count > 0)
        {
            var (current, costSoFar) = queue.Dequeue();

            foreach(TileData neighbor in current.neighbors)
            {
                if(!neighbor.walkable) continue;

                int stepCost = 1;
                if (currentUnitSelected != null && currentUnitSelected.unitType == Unit.UnitType.Artillery &&
                (current.tileType == 2 || neighbor.tileType == 2))
                {
                    stepCost = 2;
                    if (movementPoints - costSoFar == 1) // Permitir moverse si queda 1 punto
                        stepCost = 1;
                }

                int newCost = costSoFar + stepCost;

                if(newCost > movementPoints) continue;

                if(!lastPathMap.ContainsKey(neighbor) || newCost < lastPathMap[neighbor].cost)
                {
                    lastPathMap[neighbor] = new PathNode(current, newCost);
                    queue.Enqueue((neighbor, newCost));
                    reachable.Add(neighbor);
                }
            }
        }

        return reachable;
    }

    public List<TileData> ReconstructPath(TileData target)
    {
        List<TileData> path = new List<TileData>();
        TileData current = target;

        while(current != null)
        {
            path.Add(current);
            if(lastPathMap.ContainsKey(current))
                current = lastPathMap[current].parent;
            else
                current = null;
        }

        path.Reverse();
        return path;
    }

    public IEnumerator MoveUnitThroughPath(Unit unit, List<TileData> path)
    {
        if(unit == null || path == null || path.Count == 0) yield break;

        foreach(TileData tile in path)
        {
            // Calcular coste del movimiento actual
            int stepCost = 1;
            if(unit.unitType == Unit.UnitType.Artillery && (unit.currentTile.tileType == 2 || tile.tileType == 2))
            {
                stepCost = 2;
                if(unit.movesLeftThisTurn == 1) stepCost = 1;
            }
            if(tile != unit.currentTile) unit.movesLeftThisTurn -= stepCost;

            if(unit.movesLeftThisTurn < 0) unit.movesLeftThisTurn = 0;

            // Actualizar posición de la unidad
            Vector3 startPos = unit.transform.position;
            Vector3 endPos = new Vector3(tile.transform.position.x, tile.transform.position.y, -1);
            float duration = 0.3f; // Ajusta velocidad de movimiento
            float elapsed = 0f;

            AudioManager.Instance.PlaySFX(AudioManager.Instance.moveClip, 1.0f);

            while(elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                unit.transform.position = Vector3.Lerp(startPos, endPos, t);
                yield return null;
            }
            unit.transform.position = endPos;

            // Actualizar tile de la unidad
            unit.currentTile.hasUnit = false;
            unit.currentTile = tile;
            tile.hasUnit = true;

            // ---- CLAIM DE EDIFICIOS ----
            if (tile.currentBuilding != null)
            {
                int team = unit.isPlayerUnit ? 1 : 2;

                if (tile.currentBuilding.hasBeenClaimed != team)
                {
                    tile.currentBuilding.hasBeenClaimed = team;
                    if(unit.isPlayerUnit) AudioManager.Instance.PlaySFX(AudioManager.Instance.capturePlayerClip, 1.0f);
                    else AudioManager.Instance.PlaySFX(AudioManager.Instance.captureAIClip, 1.0f);

                    tile.currentBuilding.UpdateState();

                    // Actualizar contadores si es una base
                    if (tile.currentBuilding.isBase)
                    {
                        if (team == 1)
                        {
                            playerBaseCount++;
                            aiBaseCount--;
                        }
                        else 
                        {
                            aiBaseCount++;
                            playerBaseCount--;
                        }
                    }

                    if(aiBaseCount <= 0)
                    {
                        TurnManager.Instance.EndGame(true);
                    }
                    else if(playerBaseCount <= 0)
                    {
                        TurnManager.Instance.EndGame(false);
                    }
                }
            }

            // Mantener outline desactivado en cada paso
            tile.SetOutline(false);
        }

        // Recalcular tiles alcanzables después del movimiento
        if(currentState == State.SelectingMovement)
        {
            currentUnitSelected.reachableTiles = CalculateReachableTilesWithPaths(unit.currentTile, unit.movesLeftThisTurn);
            foreach(TileData t in currentUnitSelected.reachableTiles)
            {
                t.SetOutline(true, Color.darkGray);
            }
        }
        UpdateButtonVisual();
    }

    public IEnumerator AI_MoveAlongFullPath(Unit unit, List<TileData> fullPath)
    {
        if (unit == null || fullPath == null || fullPath.Count == 0) yield break;

        foreach (TileData nextTile in fullPath)
        {
            if (unit.movesLeftThisTurn <= 0) yield break;
            if(nextTile == unit.currentTile) continue;

            int stepCost = 1;
            if (unit.unitType == Unit.UnitType.Artillery && (unit.currentTile.tileType == 2 || nextTile.tileType == 2))
            {
                stepCost = 2;
                if (unit.movesLeftThisTurn == 1) stepCost = 1;
            }
            unit.movesLeftThisTurn -= stepCost;

            // Si el destino no tiene unidad enemiga -> Intentar atacar a cualquier unidad a rango
            // Si el destino tiene unidad enemiga -> Comprobar si está a rango, si es así atacar y parar el movimiento

            AudioManager.Instance.PlaySFX(AudioManager.Instance.moveClip, 1.0f);

            Vector3 startPos = unit.transform.position;
            Vector3 endPos = new Vector3(nextTile.transform.position.x, nextTile.transform.position.y, -1);

            float duration = 0.3f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                unit.transform.position = Vector3.Lerp(startPos, endPos, t);
                yield return null;
            }

            unit.transform.position = endPos;

            unit.currentTile.hasUnit = false;
            unit.currentTile = nextTile;
            nextTile.hasUnit = true;

            if (nextTile.currentBuilding != null)
            {
                int team = unit.isPlayerUnit ? 1 : 2;

                if (nextTile.currentBuilding.hasBeenClaimed != team)
                {
                    nextTile.currentBuilding.hasBeenClaimed = team;

                    if (unit.isPlayerUnit)
                        AudioManager.Instance.PlaySFX(AudioManager.Instance.capturePlayerClip, 1.0f);
                    else
                        AudioManager.Instance.PlaySFX(AudioManager.Instance.captureAIClip, 1.0f);

                    nextTile.currentBuilding.UpdateState();

                    if (nextTile.currentBuilding.isBase)
                    {
                        if (team == 1)
                        {
                            playerBaseCount++;
                            aiBaseCount--;
                        }
                        else
                        {
                            aiBaseCount++;
                            playerBaseCount--;
                        }
                    }

                    if (aiBaseCount <= 0)
                    {
                        TurnManager.Instance.EndGame(true);
                        yield break;
                    }
                    else if (playerBaseCount <= 0)
                    {
                        TurnManager.Instance.EndGame(false);
                        yield break;
                    }
                }
            }
        }
        // Intentar atacar a cualquier unidad enemiga a rango una vez terminado el movimiento
    }


    private void HandleHover()
    {
        Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        RaycastHit2D hit = Physics2D.Raycast(mousePos, Vector2.zero);
        Unit unitHit = hit.collider != null ? hit.collider.GetComponent<Unit>() : null;

        if (unitHit == currentUnitHover) return;

        if (currentUnitHover != null && currentUnitHover != currentUnitSelected) currentUnitHover.SetOutline(false);

        currentUnitHover = unitHit;

        if (currentUnitHover != null && IsSelectable(currentUnitHover) && currentUnitHover != currentUnitSelected)
        {
            currentUnitHover.SetOutline(true);
            AudioManager.Instance.PlaySFX(AudioManager.Instance.hoverClip, 1.0f);
        }
    }

    private void UpdateNoSelection()
    {
        HandleHover();

        if (EventSystem.current.IsPointerOverGameObject()) return;

        if (currentUnitHover != null && IsSelectable(currentUnitHover) && Input.GetMouseButtonDown(0))
        {
            currentUnitSelected = currentUnitHover;
            AudioManager.Instance.PlaySFX(AudioManager.Instance.playerUnitSelect, 1.0f);
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
            currentState = State.UnitSelected;
            ToggleAttackRange(false);
        }
        else
        {
            if(currentState == State.SelectingMovement) 
            {
                ToggleMovementRange(false);
            }
            currentState = State.SelectingAttack;
            ToggleAttackRange(true);
        }

        UpdateButtonVisual();
    }

    public void ToggleAttackRange(bool show)
    {
        if (show)
        {
            foreach (TileData t in currentUnitSelected.shownAttackTiles)
                t.SetOutline(false);

            currentUnitSelected.shownAttackTiles.Clear();

            int bonusRange = (currentUnitSelected.currentTile.tileType == 2) ? 1 : 0;
            int finalRange = currentUnitSelected.attackRange + bonusRange;
            currentUnitSelected.attackableTiles = CalculateTilesInRange(currentUnitSelected.currentTile, finalRange);

            currentUnitSelected.shownAttackTiles.AddRange(currentUnitSelected.attackableTiles);

            foreach (TileData tile in currentUnitSelected.attackableTiles) tile.SetOutline(true, Color.red);
        }
        else
        {
            foreach (TileData tile in currentUnitSelected.shownAttackTiles)
                tile.SetOutline(false);

            currentUnitSelected.shownAttackTiles.Clear();
        }
    }

    public void ToggleMovementRange(bool show)
    {
        if (show)
        {
            // Limpiar los que estuvieran antes
            foreach (TileData t in currentUnitSelected.shownMoveTiles)
                t.SetOutline(false);

            currentUnitSelected.shownMoveTiles.Clear();

            // Calcular el rango desde la casilla actual
            currentUnitSelected.reachableTiles = CalculateReachableTilesWithPaths(currentUnitSelected.currentTile, currentUnitSelected.movesLeftThisTurn);

            // Guardar los tiles mostrados
            currentUnitSelected.shownMoveTiles.AddRange(currentUnitSelected.reachableTiles);

            // Mostrar outline
            foreach (TileData t in currentUnitSelected.reachableTiles)
            {
                t.SetOutline(true, Color.darkGray);
                currentUnitSelected.shownMoveTiles.Add(t);
            }
        }
        else
        {
            // Ocultar SOLO los tiles mostrados antes
            foreach (TileData tile in currentUnitSelected.shownMoveTiles)
                tile.SetOutline(false);

            currentUnitSelected.shownMoveTiles.Clear();
        }
    }

    private void ToggleMoveMode()
    {
        if (currentState == State.SelectingMovement)
        {
            currentState = State.UnitSelected;
            ToggleMovementRange(false);
        }
        else
        {
            if(currentState == State.SelectingAttack) 
            {
                ToggleAttackRange(false);
            }
            currentState = State.SelectingMovement;
            ToggleMovementRange(true);
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
        switch(attacker.unitType)
        {
            case Unit.UnitType.Infantry:
                AudioManager.Instance.PlaySFX(AudioManager.Instance.infantryAttack, 1.0f);
                break;
            case Unit.UnitType.HeavyInfantry:
                AudioManager.Instance.PlaySFX(AudioManager.Instance.heavyInfantryAttack, 1.0f);
                break;
            case Unit.UnitType.Artillery:
                AudioManager.Instance.PlaySFX(AudioManager.Instance.artilleryAttack, 1.0f);
                break;
        }

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
        if(currentTileHover != null && currentUnitSelected.reachableTiles.Contains(currentTileHover))
        {
            currentTileHover.SetOutline(true, Color.darkGray);
            currentTileHover.outline.GetComponent<SpriteRenderer>().sortingOrder = -2;
        }
        else if (currentTileHover != null)
        {
            currentTileHover.outline.GetComponent<SpriteRenderer>().sortingOrder = -2;
            currentTileHover.SetOutline(false);
        }

        currentTileHover = tileHit;
        currentUnitSelected.reachableTiles = CalculateReachableTilesWithPaths(currentUnitSelected.currentTile, currentUnitSelected.movesLeftThisTurn);
        if (tileHit == null || currentUnitSelected == null) return;

        // Comprobar si el tile está en los alcanzables
        if (currentUnitSelected.reachableTiles.Contains(tileHit) && !tileHit.hasUnit)
        {
            tileHit.SetOutline(true, Color.black);
            tileHit.outline.GetComponent<SpriteRenderer>().sortingOrder = -1;

            if (Input.GetMouseButtonDown(0))
            {
                ToggleMovementRange(false);

                List<TileData> path = ReconstructPath(tileHit);
                StartCoroutine(MoveUnitThroughPath(currentUnitSelected, path));
                
                currentState = State.UnitSelected;
                UpdateButtonVisual();
            }
        }
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
        else return !unit.isPlayerUnit && currentUnitSelected.attackableTiles.Contains(unit.currentTile);
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
