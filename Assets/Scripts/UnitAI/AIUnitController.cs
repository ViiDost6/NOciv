using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Tilemaps;
using Utils;
using System.IO;

[RequireComponent(typeof(Unit))]
[RequireComponent(typeof(BehaviourTreeRunner))]
public class AIUnitController : MonoBehaviour
{
    private Unit unit;
    private BehaviourTreeRunner btRunner;
    private InfluenceMap2 influenceMap;
    
    // Variable pública para que TurnManager sepa si estamos ocupados
    public bool IsBusy { get; private set; }

    // Factores de personalidad
    public float fearFactor = 1.0f;
    public float motivationFactor = 1.0f;

    void Awake()
    {
        unit = GetComponent<Unit>();
        btRunner = GetComponent<BehaviourTreeRunner>();
    }

    void Start()
    {
        influenceMap = FindFirstObjectByType<InfluenceMap2>();
        
        if (unit.unitType == Unit.UnitType.HeavyInfantry) fearFactor = 0.5f;
        if (unit.unitType == Unit.UnitType.Artillery) fearFactor = 2.0f;
    }

    public NodeState ExecuteTree()
    {
        // Si ya estamos haciendo algo, no ejecutar el árbol
        if (IsBusy) return NodeState.Running;

        if (unit.movesLeftThisTurn <= 0 && unit.hasAttackedThisTurn) 
            return NodeState.Failure;

        return btRunner.RunTree();
    }

    // --- ACCIONES CON CORRECCIÓN DE TIEMPO ---

    public void PerformMove(TileData targetTile)
    {
        // BLOQUEO INMEDIATO: Evita que el TurnManager crea que terminamos antes de empezar
        IsBusy = true; 
        StartCoroutine(MoveRoutine(targetTile));
    }

    public void PerformAttack(Unit target)
    {
        // BLOQUEO INMEDIATO
        IsBusy = true;
        StartCoroutine(AttackRoutine(target));
    }

    private IEnumerator MoveRoutine(TileData tile)
    {
        // Lógica de datos (instantánea)
        unit.currentTile.hasUnit = false;
        unit.currentTile = tile;
        tile.hasUnit = true;
        unit.movesLeftThisTurn -= 1; 

        // Lógica visual (animación)
        Vector3 startPos = unit.transform.position;
        Vector3 endPos = new Vector3(tile.transform.position.x, tile.transform.position.y, -1); // Asegurar Z correcto
        
        float duration = 0.5f; // Tiempo que tarda el movimiento
        float elapsed = 0f;

        while(elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            // Usamos SmoothStep para un movimiento más suave
            unit.transform.position = Vector3.Lerp(startPos, endPos, Mathf.SmoothStep(0, 1, t));
            yield return null;
        }
        if(tile.currentBuilding != null)
        {
            tile.currentBuilding.hasBeenClaimed = unit.isPlayerUnit ? 1 : 2;
            tile.currentBuilding.UpdateState();
        }

        unit.transform.position = endPos;
        
        // Pequeña pausa extra para que se note la llegada
        yield return new WaitForSeconds(0.1f);
        
        IsBusy = false; // LIBERAMOS la unidad
    }

    private IEnumerator AttackRoutine(Unit target)
    {
        Vector3 originalPos = transform.position;
        Vector3 targetPos = target.transform.position;
        Vector3 attackPos = (originalPos + targetPos) / 2f;
        
        // Fase 1: Ir hacia el enemigo
        float duration = 0.2f;
        float elapsed = 0f;
        while(elapsed < duration) 
        { 
            elapsed += Time.deltaTime;
            unit.transform.position = Vector3.Lerp(originalPos, attackPos, elapsed/duration); 
            yield return null;
        }

        // Fase 2: Impacto (Daño)
        UnitManager.Instance.Attack(unit, target);
        
        // Efecto de sacudida o pausa de impacto
        yield return new WaitForSeconds(0.1f);

        // Fase 3: Volver
        elapsed = 0f;
        while(elapsed < duration) 
        { 
            elapsed += Time.deltaTime;
            unit.transform.position = Vector3.Lerp(attackPos, originalPos, elapsed/duration); 
            yield return null;
        }
        
        unit.transform.position = originalPos;
        IsBusy = false; // LIBERAMOS la unidad
    }

    // --- LÓGICA DE DECISIÓN (Sin cambios importantes) ---

    public Unit GetBestTargetInRange()
    {
        if (unit.hasAttackedThisTurn) return null;

        List<Unit> enemies = GetEnemiesInRange();
        Unit bestTarget = null;
        float bestScore = -1f;

        foreach(var enemy in enemies)
        {
            float score = CalculateAttackScore(enemy);
            if (score > bestScore)
            {
                bestScore = score;
                bestTarget = enemy;
            }
        }
        return bestTarget;
    }

    public struct PathNode
    {
        public TileData parent;
        public float carriedWeight;
        public PathNode(TileData parent, float carriedWeight)
        {
            this.parent = parent;
            this.carriedWeight = carriedWeight;
        }
    }

    public List<TileData> CalculatePath(TileData origin, TileData destination)
    {
        Dictionary<TileData,PathNode> graphNotes = new();
        PriorityQueue<TileData, float> pQueue = new();

        graphNotes.Add(origin, new(null, 0));
        pQueue.Enqueue(origin, 0);

        while(pQueue.Count > 0)
        {
            TileData current = pQueue.Dequeue();
            PathNode extraData = graphNotes[current];

            if(current == destination) break;

            foreach(TileData neighbour in current.neighbors)
            {
                float threatWeight = influenceMap.GetThreatAt(neighbour.gridPosition);
                if(!graphNotes.ContainsKey(neighbour) || graphNotes[neighbour].carriedWeight > extraData.carriedWeight + threatWeight)
                {
                    PathNode pn = new(current, threatWeight + extraData.carriedWeight);
                    graphNotes[neighbour] = pn;
                    pQueue.Enqueue(neighbour, threatWeight);
                }
            }
        }


        return ReconstructPath(graphNotes, destination);
        
    }

    List<TileData> ReconstructPath(Dictionary<TileData, PathNode> dct, TileData destination)
    {
        List<TileData> path = new();
        TileData current = destination;
        while(current != null)
        {
            path.Add(current);
            current = dct[current].parent;
        }

        path.Reverse();
        return path;
    }

    private float CalculateAttackScore(Unit enemy)
    {
        float score = 10f;
        float damage = unit.damage;
        if (enemy.hasArmor && !unit.hasPiercing) damage -= 1;
        if (enemy.health <= damage) score += 50f;

        if (unit.unitType == Unit.UnitType.Infantry && enemy.unitType == Unit.UnitType.HeavyInfantry) score += 5;
        if (unit.unitType == Unit.UnitType.Artillery && enemy.unitType == Unit.UnitType.Infantry) score += 10;
        if (unit.unitType == Unit.UnitType.HeavyInfantry && enemy.unitType == Unit.UnitType.Artillery) score += 10;
        
        return score;
    }

    public TileData GetBestTacticalMovePosition()
    {
        if (unit.movesLeftThisTurn <= 0) return null;

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

    private List<Unit> GetEnemiesInRange()
    {
        List<Unit> enemies = new List<Unit>();
        var allUnits = TurnManager.Instance.GetAllUnits(true); 
        foreach(var enemy in allUnits)
        {
            if(enemy == null) continue;
            int dist = Mathf.Abs(unit.currentTile.gridPosition.x - enemy.currentTile.gridPosition.x) + 
                       Mathf.Abs(unit.currentTile.gridPosition.y - enemy.currentTile.gridPosition.y); // Aprox hex
            
            // Comprobación de rango real (usando Grid si es posible, o aprox)
            // Para hex grid, la distancia Axial o Cúbica es mejor, pero Manhattan sirve para tests
            if (dist <= unit.attackRange) enemies.Add(enemy);
        }
        return enemies;
    }

    private List<TileData> GetReachableTiles()
    {
        // Replicamos la lógica BFS de UnitManager para encontrar tiles
        // NOTA: Idealmente esto debería estar en un método estático helper en UnitManager
        // para no duplicar código.
        
        List<TileData> inRange = new List<TileData>();
        if(unit.currentTile == null) return inRange;

        Queue<(TileData tile, int level)> queue = new Queue<(TileData tile, int level)>();
        HashSet<TileData> visited = new HashSet<TileData>();

        queue.Enqueue((unit.currentTile, 0));
        visited.Add(unit.currentTile);

        int maxMoves = 1; // La IA evalúa moverse paso a paso

        while (queue.Count > 0)
        {
            var (tile, level) = queue.Dequeue();
            if (level > 0) inRange.Add(tile);
            if (level >= maxMoves) continue;

            foreach (TileData neighbor in tile.neighbors)
            {
                if (!visited.Contains(neighbor) && neighbor.walkable && !neighbor.hasUnit)
                {
                    visited.Add(neighbor);
                    queue.Enqueue((neighbor, level + 1));
                }
            }
        }
        return inRange;
    }

    public TileData GetObjective()
    {
        //influenceMap.
        throw new System.NotImplementedException();
    }
}