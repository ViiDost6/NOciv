using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Utils; 

[RequireComponent(typeof(Unit))]
[RequireComponent(typeof(BehaviourTreeRunner))]
public class AIUnitController : MonoBehaviour
{
    public TileData obj; 
    private Unit unit;
    private BehaviourTreeRunner btRunner;
    public bool IsBusy { get; private set; }

    [Header("Personality")]
    public float fearFactor = 1.0f;
    public float motivationFactor = 1.0f;
    [Range(0f, 1f)] public float independence = 0.3f; 

    [Header("Debug")]
    public bool DebugMode = true; 

    [Header("Analysis Cache")]
    public TacticalAnalysis currentAnalysis; 

    [System.Serializable]
    public struct TacticalAnalysis {
        public TileData bestStrategicMove; 
        public float strategicScore;
        public TileData bestLocalOpportunity; 
        public float localScore;
        public bool isLocalOptionBetter; 
    }

    private InfluenceMap2 influenceMap;
    private MapGenerator mapGenerator;

    void Awake() {
        unit = GetComponent<Unit>();
        btRunner = GetComponent<BehaviourTreeRunner>();
    }

    void Start() {
        influenceMap = FindObjectOfType<InfluenceMap2>();
        mapGenerator = FindObjectOfType<MapGenerator>();
        if (unit.unitType == Unit.UnitType.HeavyInfantry) { fearFactor = 0.5f; independence = 0.2f; }
        if (unit.unitType == Unit.UnitType.Artillery) { fearFactor = 2.0f; independence = 0.1f; }
    }

    public void ResetBehavior() {
        IsBusy = false; StopAllCoroutines(); 
        if (btRunner != null && btRunner.runningTree != null)
            foreach (var node in btRunner.runningTree.nodes) node.ResetState();
        currentAnalysis = new TacticalAnalysis();
    }

    public NodeState ExecuteTree() {
        if (!IsBusy && unit.movesLeftThisTurn <= 0 && unit.hasAttackedThisTurn) 
            return NodeState.Failure;
        return btRunner.RunTree();
    }

    // --- RECONOCIMIENTO TÁCTICO V11 ---
    public void PerformTacticalRecon()
    {
        currentAnalysis = new TacticalAnalysis();
        if (unit.movesLeftThisTurn <= 0) return;

        List<TileData> candidates = GetReachableTilesBFS(unit.currentTile, unit.movesLeftThisTurn);
        float maxStratScore = -float.MaxValue;
        float maxLocalScore = -float.MaxValue;

        Vector2Int distantObjective = Vector2Int.zero;
        bool hasGlobalTarget = false;
        
        if(influenceMap != null) {
            distantObjective = influenceMap.GetNearestHighDesirePoint(unit.currentTile.gridPosition);
            if (distantObjective != unit.currentTile.gridPosition) hasGlobalTarget = true;
        }

        bool isCamping = false;
        if (unit.currentTile.currentBuilding != null && unit.currentTile.currentBuilding.hasBeenClaimed == (unit.isPlayerUnit ? 1 : 2)) {
            if (influenceMap != null && influenceMap.GetThreatAt(unit.currentTile.gridPosition) <= 0.1f) isCamping = true;
        }

        foreach(var tile in candidates)
        {
            if (tile.hasUnit && tile != unit.currentTile) continue;

            float stratScore = CalculateStrategicScore(tile);
            if (tile == unit.currentTile) stratScore -= isCamping ? 500.0f : 0.5f;
            
            if (hasGlobalTarget && tile != unit.currentTile) {
                float currentDist = Vector2Int.Distance(unit.currentTile.gridPosition, distantObjective);
                float newDist = Vector2Int.Distance(tile.gridPosition, distantObjective);
                if (newDist < currentDist) stratScore += 2.0f;
            }

            if (stratScore > maxStratScore) {
                maxStratScore = stratScore;
                currentAnalysis.bestStrategicMove = tile;
            }

            float localScore = CalculateLocalScore(tile);
            float distLocal = Mathf.Abs(tile.gridPosition.x - unit.currentTile.gridPosition.x) + 
                              Mathf.Abs(tile.gridPosition.y - unit.currentTile.gridPosition.y);
            localScore -= (distLocal * 0.5f);
            if (tile == unit.currentTile) localScore -= isCamping ? 500.0f : 0.5f;

            if (localScore > maxLocalScore) {
                maxLocalScore = localScore;
                currentAnalysis.bestLocalOpportunity = tile;
            }
        }

        if ((currentAnalysis.bestStrategicMove == unit.currentTile || maxStratScore < 0.1f) && candidates.Count > 1)
        {
            List<TileData> roamingOptions = new List<TileData>();
            foreach(var c in candidates) if(c != unit.currentTile && !c.hasUnit) roamingOptions.Add(c);
            
            if(roamingOptions.Count > 0)
            {
                if (hasGlobalTarget) {
                    TileData targetTile = mapGenerator.GetTileAtPosition(distantObjective);
                    if (targetTile != null) {
                        List<TileData> path = CalculatePath(unit.currentTile, targetTile);
                        TileData nextStep = FindFurthestReachableOnPath(path, unit.movesLeftThisTurn);
                        if (nextStep != null && nextStep != unit.currentTile) {
                            currentAnalysis.bestStrategicMove = nextStep; maxStratScore = 5.0f;
                        }
                    }
                } else {
                    currentAnalysis.bestStrategicMove = roamingOptions[Random.Range(0, roamingOptions.Count)];
                    maxStratScore = 1.0f;
                }
            }
        }

        currentAnalysis.strategicScore = maxStratScore;
        currentAnalysis.localScore = maxLocalScore;
        float threshold = (1.0f - independence) + 0.5f; 
        currentAnalysis.isLocalOptionBetter = (maxLocalScore > maxStratScore * threshold);
    }

    private TileData FindFurthestReachableOnPath(List<TileData> fullPath, int moves) {
        if (fullPath == null || fullPath.Count == 0) return null;
        TileData bestReach = null; int costSoFar = 0;
        foreach (TileData t in fullPath) {
            if (t == unit.currentTile) continue; 
            int stepCost = (int)t.weight; if (unit.unitType == Unit.UnitType.Artillery && t.tileType == 2) stepCost = 2;
            if (costSoFar + stepCost <= moves) { costSoFar += stepCost; bestReach = t; } else break; 
        }
        return bestReach;
    }

    private float CalculateStrategicScore(TileData tile) {
        float desire = influenceMap != null ? influenceMap.GetDesireAt(tile.gridPosition) : 0;
        float threat = influenceMap != null ? influenceMap.GetThreatAt(tile.gridPosition) : 0;
        return (desire * motivationFactor) - (threat * fearFactor);
    }

    private float CalculateLocalScore(TileData tile) {
        float score = 0;
        float threat = influenceMap != null ? influenceMap.GetThreatAt(tile.gridPosition) : 0;
        float healthPct = (float)unit.health / unit.maxHealth;
        
        float currentBravery = (healthPct > 0.7f) ? 0.3f : (healthPct < 0.4f ? 2.5f : 1.0f);
        float effectiveFear = fearFactor * currentBravery;
        
        score -= threat * effectiveFear; 

        if (HasFriendlyNeighbor(tile)) score -= 2.0f;

        if (tile.currentBuilding != null) {
            int team = unit.isPlayerUnit ? 1 : 2;
            if (tile.currentBuilding.hasBeenClaimed != team) score += tile.currentBuilding.isBase ? 100.0f : 50.0f; 
        }

        if (!unit.hasAttackedThisTurn)
        {
            Unit bestVictim = GetBestTargetFromPosition(tile);
            if (bestVictim != null)
            {
                float attackScore = 40.0f; 
                float damagePct = (float)unit.damage / bestVictim.maxHealth;
                if (damagePct > 0.4f) attackScore += 30.0f;

                if (bestVictim.health <= unit.damage) 
                {
                    attackScore += 120.0f; 
                    if (healthPct > 0.3f) score += 20.0f; 
                }
                else
                {
                    if (healthPct < 0.5f) attackScore -= 30.0f;
                }
                score += attackScore;
            }
        }

        if (tile.tileType == 2 && threat > 0.1f) score += 5.0f;
        return score;
    }

    private Unit GetBestTargetFromPosition(TileData pos) {
        int range = unit.attackRange; if (pos.tileType == 2) range += 1;
        var enemies = TurnManager.Instance.GetAllUnits(!unit.isPlayerUnit);
        Unit bestVictim = null; float bestVal = -1f;

        foreach(var enemy in enemies) {
            if(enemy == null) continue;
            int dist = Mathf.Abs(pos.gridPosition.x - enemy.currentTile.gridPosition.x) + 
                       Mathf.Abs(pos.gridPosition.y - enemy.currentTile.gridPosition.y);
            
            if (dist <= range) {
                float val = 1.0f - ((float)enemy.health / enemy.maxHealth);
                if (val > bestVal) { bestVal = val; bestVictim = enemy; }
            }
        }
        return bestVictim;
    }

    private bool HasFriendlyNeighbor(TileData tile) {
        foreach(var n in tile.neighbors) {
            if (n.hasUnit && n.GetComponentInChildren<Unit>() != null && !n.GetComponentInChildren<Unit>().isPlayerUnit) return true;
        }
        return false;
    }

    public TileData GetBestTacticalMovePosition() {
        PerformTacticalRecon();
        return currentAnalysis.isLocalOptionBetter ? currentAnalysis.bestLocalOpportunity : currentAnalysis.bestStrategicMove;
    }

    public List<TileData> CalculatePath(TileData start, TileData end) {
        if (start == end || start == null || end == null) return new List<TileData>();
        var frontier = new PriorityQueue<TileData, float>();
        frontier.Enqueue(start, 0);
        var cameFrom = new Dictionary<TileData, TileData>();
        var costSoFar = new Dictionary<TileData, float>();
        cameFrom[start] = null; costSoFar[start] = 0;
        while (frontier.Count > 0) {
            TileData current = frontier.Dequeue();
            if (current == end) break;
            foreach (var neighbor in current.neighbors) {
                if (!neighbor.walkable) continue;
                if (neighbor.hasUnit && neighbor != end && neighbor != start) continue;
                float newCost = costSoFar[current] + neighbor.weight;
                if (!costSoFar.ContainsKey(neighbor) || newCost < costSoFar[neighbor]) {
                    costSoFar[neighbor] = newCost;
                    float priority = newCost + (Mathf.Abs(neighbor.gridPosition.x - end.gridPosition.x) + Mathf.Abs(neighbor.gridPosition.y - end.gridPosition.y));
                    frontier.Enqueue(neighbor, priority);
                    cameFrom[neighbor] = current;
                }
            }
        }
        if (!cameFrom.ContainsKey(end)) return null;
        var path = new List<TileData>(); TileData curr = end;
        while (curr != start) { path.Add(curr); curr = cameFrom[curr]; }
        path.Reverse();
        return path;
    }

    public void MoveAlongPath(List<TileData> path) {
        if (path == null || path.Count == 0 || IsBusy) return;
        IsBusy = true; StartCoroutine(MoveUnitAlongPathRoutine(path));
    }

    private IEnumerator MoveUnitAlongPathRoutine(List<TileData> path) {
        foreach (TileData nextTile in path) {
            if (nextTile == unit.currentTile) continue;
            float dist = Vector3.Distance(unit.transform.position, nextTile.transform.position);
            if (dist > 3.0f) { IsBusy = false; yield break; }
            int stepCost = 1;
            if (unit.unitType == Unit.UnitType.Artillery && (unit.currentTile.tileType == 2 || nextTile.tileType == 2)) {
                stepCost = 2; if (unit.movesLeftThisTurn == 1) stepCost = 1;
            }
            if (unit.movesLeftThisTurn < stepCost) break;
            unit.movesLeftThisTurn -= stepCost;
            if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX(AudioManager.Instance.moveClip, 1.0f);
            Vector3 startPos = unit.transform.position; Vector3 endPos = nextTile.transform.position; endPos.z = startPos.z;
            float duration = 0.3f; float elapsed = 0f;
            while (elapsed < duration) {
                elapsed += Time.deltaTime; unit.transform.position = Vector3.Lerp(startPos, endPos, elapsed / duration); yield return null; 
            }
            unit.transform.position = endPos;
            unit.currentTile.hasUnit = false; unit.currentTile = nextTile; nextTile.hasUnit = true;
            
            HandleCapture(nextTile);

            if (UnitManager.Instance.aiBaseCount <= 0 || UnitManager.Instance.playerBaseCount <= 0) yield break;
            yield return null; 
        }
        IsBusy = false;
    }

    private void HandleCapture(TileData tile) {
        if (tile.currentBuilding != null) {
            int team = unit.isPlayerUnit ? 1 : 2;
            if (tile.currentBuilding.hasBeenClaimed != team) {
                tile.currentBuilding.hasBeenClaimed = team;
                if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX(AudioManager.Instance.captureAIClip, 1.0f);
                tile.currentBuilding.UpdateState();
                
                // Actualizar mapas de influencia
                if (influenceMap != null) influenceMap.RefreshDesireMap();
                
                // FIX: Actualizar economía global INMEDIATAMENTE
                // Esto hará que la UI del jugador muestre la pérdida de ingresos al instante
                if (TurnManager.Instance != null) TurnManager.Instance.RecalculateEconomy();

                if (tile.currentBuilding.isBase) {
                    if (team == 1) { UnitManager.Instance.playerBaseCount++; UnitManager.Instance.aiBaseCount--; }
                    else { UnitManager.Instance.aiBaseCount++; UnitManager.Instance.playerBaseCount--; }
                    if (UnitManager.Instance.aiBaseCount <= 0) TurnManager.Instance.EndGame(true);
                    else if (UnitManager.Instance.playerBaseCount <= 0) TurnManager.Instance.EndGame(false);
                }
            }
        }
    }

    public void PerformAttack(Unit target) { IsBusy = true; StartCoroutine(AttackRoutine(target)); }
    private IEnumerator AttackRoutine(Unit target) {
        if (target == null) { IsBusy = false; yield break; }
        Vector3 start = transform.position; Vector3 end = target.transform.position;
        float t = 0; while(t < 1) { t += Time.deltaTime * 5; transform.position = Vector3.Lerp(start, Vector3.Lerp(start, end, 0.4f), t); yield return null; }
        UnitManager.Instance.Attack(unit, target);
        yield return new WaitForSeconds(0.1f);
        t = 0; while(t < 1) { t += Time.deltaTime * 5; transform.position = Vector3.Lerp(Vector3.Lerp(start, end, 0.4f), start, t); yield return null; }
        transform.position = start; IsBusy = false;
    }

    private List<TileData> GetReachableTilesBFS(TileData start, int maxMoves) {
        List<TileData> reachable = new List<TileData>();
        Queue<(TileData, int)> queue = new Queue<(TileData, int)>();
        HashSet<TileData> visited = new HashSet<TileData>();
        queue.Enqueue((start, 0)); visited.Add(start); reachable.Add(start);
        while (queue.Count > 0) {
            var (current, cost) = queue.Dequeue();
            foreach (var neighbor in current.neighbors) {
                if (!neighbor.walkable || visited.Contains(neighbor)) continue;
                if (neighbor.hasUnit) continue; 
                int stepCost = neighbor.weight > 1 ? (int)neighbor.weight : 1; 
                if (unit.unitType == Unit.UnitType.Artillery && (current.tileType == 2 || neighbor.tileType == 2)) stepCost = 2;
                int newCost = cost + stepCost;
                if (newCost <= maxMoves) { visited.Add(neighbor); reachable.Add(neighbor); queue.Enqueue((neighbor, newCost)); }
            }
        }
        return reachable;
    }
    
    public Unit GetBestTargetInRange() {
        if (unit.hasAttackedThisTurn) return null;
        int range = unit.attackRange; if (unit.currentTile.tileType == 2) range += 1;
        Unit best = null; float minHealth = 999f;
        foreach(var enemy in TurnManager.Instance.GetAllUnits(true)) {
             if(enemy == null) continue;
             int dist = Mathf.Abs(unit.currentTile.gridPosition.x - enemy.currentTile.gridPosition.x) + 
                        Mathf.Abs(unit.currentTile.gridPosition.y - enemy.currentTile.gridPosition.y);
             if (dist <= range && enemy.health < minHealth) { minHealth = enemy.health; best = enemy; }
        }
        return best;
    }
}