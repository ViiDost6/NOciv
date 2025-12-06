using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(Unit))]
[RequireComponent(typeof(BehaviourTreeRunner))]
public class AIUnitController : MonoBehaviour
{
    public TileData obj;
    private Unit unit;
    private BehaviourTreeRunner btRunner;
    
    // Bandera para que el Nodo sepa cuándo esperar
    public bool IsBusy { get; private set; }

    [Header("Personality")]
    public float fearFactor = 1.0f;
    public float motivationFactor = 1.0f;

    [Header("Debug Tools")]
    public bool debugMode = true;

    private InfluenceMap2 influenceMap;

    void Awake()
    {
        unit = GetComponent<Unit>();
        btRunner = GetComponent<BehaviourTreeRunner>();
    }

    void Start()
    {
        influenceMap = FindObjectOfType<InfluenceMap2>();
        
        // Configuración básica de personalidad
        if (unit.unitType == Unit.UnitType.HeavyInfantry) fearFactor = 0.5f;
        if (unit.unitType == Unit.UnitType.Artillery) fearFactor = 2.0f;
    }

    public NodeState ExecuteTree()
    {
        if (IsBusy) return NodeState.Running; // Si estamos moviendo, devolvemos Running
        if (unit.movesLeftThisTurn <= 0 && unit.hasAttackedThisTurn) return NodeState.Failure;
        return btRunner.RunTree();
    }

    // --- LÓGICA DE MOVIMIENTO DE IA (Transferida desde UnitManager) ---

    // Llamado por el Nodo MoveToStrategicPosition
    public void MoveAlongPath(List<TileData> path)
    {
        if (path == null || path.Count == 0) return;
        
        IsBusy = true;
        StartCoroutine(MoveUnitAlongPathRoutine(path));
    }

    private IEnumerator MoveUnitAlongPathRoutine(List<TileData> path)
    {
        Debug.Log($"[AI] Iniciando movimiento. Pasos: {path.Count}");

        foreach (TileData nextTile in path)
        {
            // Ignoramos la casilla actual
            if (nextTile == unit.currentTile) continue;
            
            // 1. Comprobación de movimiento
            if (unit.movesLeftThisTurn <= 0) break;

            // 2. Cálculo de Costes
            int stepCost = 1;
            // Regla: Artillería en montañas (TileType 2) cuesta más
            if (unit.unitType == Unit.UnitType.Artillery && (unit.currentTile.tileType == 2 || nextTile.tileType == 2))
            {
                stepCost = 2;
                // Excepción: Si le queda 1 movimiento, permitimos el último paso
                if (unit.movesLeftThisTurn == 1) stepCost = 1;
            }
            
            unit.movesLeftThisTurn -= stepCost;
            if (unit.movesLeftThisTurn < 0) unit.movesLeftThisTurn = 0;

            // 3. Audio
            if(AudioManager.Instance != null) 
                AudioManager.Instance.PlaySFX(AudioManager.Instance.moveClip, 1.0f);

            // 4. Animación
            Vector3 startPos = unit.transform.position;
            Vector3 endPos = new Vector3(nextTile.transform.position.x, nextTile.transform.position.y, -1);

            float duration = 0.3f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                unit.transform.position = Vector3.Lerp(startPos, endPos, Mathf.SmoothStep(0, 1, t));
                yield return null; 
            }

            unit.transform.position = endPos;

            // 5. Actualización de casilla
            unit.currentTile.hasUnit = false;
            unit.currentTile = nextTile;
            nextTile.hasUnit = true;

            // 6. Lógica de Captura de Edificios
            if (nextTile.currentBuilding != null)
            {
                int team = unit.isPlayerUnit ? 1 : 2; // 1 = Jugador, 2 = IA

                if (nextTile.currentBuilding.hasBeenClaimed != team)
                {
                    nextTile.currentBuilding.hasBeenClaimed = team;

                    // Sonidos de captura
                    if (AudioManager.Instance != null)
                    {
                        if (unit.isPlayerUnit)
                            AudioManager.Instance.PlaySFX(AudioManager.Instance.capturePlayerClip, 1.0f);
                        else
                            AudioManager.Instance.PlaySFX(AudioManager.Instance.captureAIClip, 1.0f);
                    }

                    nextTile.currentBuilding.UpdateState();

                    // Gestión de Bases (Usando UnitManager.Instance)
                    if (nextTile.currentBuilding.isBase)
                    {
                        if (team == 1)
                        {
                            UnitManager.Instance.playerBaseCount++;
                            UnitManager.Instance.aiBaseCount--;
                        }
                        else
                        {
                            UnitManager.Instance.aiBaseCount++;
                            UnitManager.Instance.playerBaseCount--;
                        }
                    }

                    // Condiciones de Victoria/Derrota
                    if (UnitManager.Instance.aiBaseCount <= 0)
                    {
                        if(TurnManager.Instance != null) TurnManager.Instance.EndGame(true);
                        yield break;
                    }
                    else if (UnitManager.Instance.playerBaseCount <= 0)
                    {
                        if(TurnManager.Instance != null) TurnManager.Instance.EndGame(false);
                        yield break;
                    }
                }
            }
            
            // Pequeña pausa entre pasos
            yield return null; 
        }

        IsBusy = false;
        Debug.Log("[AI] Movimiento finalizado");
    }

    // --- ACCIONES DE COMBATE ---
    
    public void PerformAttack(Unit target)
    {
        IsBusy = true;
        StartCoroutine(AttackRoutine(target));
    }

    private IEnumerator AttackRoutine(Unit target)
    {
        // ... (Tu lógica visual de ataque) ...
        Vector3 originalPos = transform.position;
        Vector3 targetPos = target.transform.position;
        Vector3 attackPos = (originalPos + targetPos) / 2f;
        
        float duration = 0.2f;
        float elapsed = 0f;
        while(elapsed < duration) { elapsed += Time.deltaTime; unit.transform.position = Vector3.Lerp(originalPos, attackPos, elapsed/duration); yield return null; }

        UnitManager.Instance.Attack(unit, target);
        
        yield return new WaitForSeconds(0.1f);

        elapsed = 0f;
        while(elapsed < duration) { elapsed += Time.deltaTime; unit.transform.position = Vector3.Lerp(attackPos, originalPos, elapsed/duration); yield return null; }
        
        unit.transform.position = originalPos;
        IsBusy = false;
    }

    // --- UTILS (Pathfinding y Selección) ---

    public TileData GetBestTacticalMovePosition()
    {
        if (unit.movesLeftThisTurn <= 0) return null;
        if (influenceMap == null) return null;

        List<TileData> candidates = GetReachableTiles();
        TileData bestTile = null;
        float bestScore = -float.MaxValue;

        foreach(var tile in candidates)
        {
            if (tile.hasUnit && tile != unit.currentTile) continue;

            float threat = influenceMap.GetThreatAt(tile.gridPosition);
            float desire = influenceMap.GetDesireAt(tile.gridPosition);
            
            float score = (desire * motivationFactor) - (threat * fearFactor);
            score += Random.Range(0f, 0.5f);

            if (score > bestScore)
            {
                bestScore = score;
                bestTile = tile;
            }
        }
        
        if (bestTile == unit.currentTile) return null;
        return bestTile;
    }

    public List<TileData> CalculatePath(TileData start, TileData end)
    {
        if (start == null || end == null) return new List<TileData>();

        Queue<TileData> queue = new Queue<TileData>();
        Dictionary<TileData, TileData> cameFrom = new Dictionary<TileData, TileData>();
        
        queue.Enqueue(start);
        cameFrom[start] = null;

        bool found = false;

        while (queue.Count > 0)
        {
            TileData current = queue.Dequeue();
            if (current == end) { found = true; break; }

            foreach (TileData neighbor in current.neighbors)
            {
                if (neighbor.walkable && !cameFrom.ContainsKey(neighbor))
                {
                    if (neighbor.hasUnit && neighbor != end) continue;
                    cameFrom[neighbor] = current;
                    queue.Enqueue(neighbor);
                }
            }
        }

        if (!found) return new List<TileData>();

        List<TileData> path = new List<TileData>();
        TileData curr = end;
        while (curr != start)
        {
            path.Add(curr);
            curr = cameFrom[curr];
        }
        path.Add(start);
        path.Reverse();
        return path;
    }

    private List<TileData> GetReachableTiles()
    {
        // BFS simple de 1 nivel (o rango de movimiento) para decidir el próximo paso
        List<TileData> inRange = new List<TileData>();
        if(unit.currentTile == null) return inRange;

        foreach (TileData neighbor in unit.currentTile.neighbors)
        {
            if (neighbor.walkable && !neighbor.hasUnit)
                inRange.Add(neighbor);
        }
        return inRange;
    }
    
    public Unit GetBestTargetInRange()
    {
        if (unit.hasAttackedThisTurn) return null;
        // ... (Lógica de búsqueda de enemigos ya implementada antes)
        // Por brevedad, asumimos que usas la lógica previa o la de UnitManager
        var allUnits = TurnManager.Instance.GetAllUnits(true); 
        Unit best = null; float maxScore = -1;

        foreach(var enemy in allUnits) {
             // Distancia simple
             int dist = Mathf.Abs(unit.currentTile.gridPosition.x - enemy.currentTile.gridPosition.x) + 
                        Mathf.Abs(unit.currentTile.gridPosition.y - enemy.currentTile.gridPosition.y);
             if (dist <= unit.attackRange) {
                 float score = 10; // Simplificado
                 if (score > maxScore) { maxScore = score; best = enemy; }
             }
        }
        return best;
    }
}