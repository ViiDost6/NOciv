using UnityEngine;
using System.Collections.Generic;
using System.Linq;

// --- CONDICIONES ---

[System.Serializable]
public class CheckHealthNode : Node
{
    [ShowInEditor] public int threshold = 3;
    
    public override NodeState Evaluate(GameObject agent)
    {
        Unit unit = agent.GetComponent<Unit>();
        return (unit != null && unit.health <= threshold) ? NodeState.Success : NodeState.Failure;
    }
}

[System.Serializable]
public class CheckAttackOpportunityNode : Node
{
    public override NodeState Evaluate(GameObject agent)
    {
        Unit unit = agent.GetComponent<Unit>();
        AIBlackboard blackboard = agent.GetComponent<AIBlackboard>();
        
        if (unit == null || blackboard == null || unit.currentTile == null) return NodeState.Failure;

        Unit bestTarget = null;
        float bestScore = -100f;

        foreach (var neighbor in unit.currentTile.neighbors)
        {
            if (neighbor.hasUnit)
            {
                Unit target = FindUnitOnTile(neighbor);
                if (target != null && target.isPlayerUnit != unit.isPlayerUnit)
                {
                    // Protección por si no existe el Helper
                    int rps = (AIActionsHelper.Instance != null) ? AIActionsHelper.Instance.GetRPSMatchup(unit, target) : 0;
                    
                    float score = 10f + (rps * 5f) + unit.damage - target.health;

                    if (rps == -1 && target.health > unit.damage) score -= 50f;

                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestTarget = target;
                    }
                }
            }
        }

        if (bestTarget != null && bestScore > 0)
        {
            blackboard.SetData("TargetEnemy", bestTarget);
            return NodeState.Success;
        }

        return NodeState.Failure;
    }

    private Unit FindUnitOnTile(TileData tile)
    {
        Unit[] allUnits = Object.FindObjectsByType<Unit>(FindObjectsSortMode.None);
        foreach(var u in allUnits) if (u.currentTile == tile) return u;
        return null;
    }
}

// --- ACCIONES ---

[System.Serializable]
public class AttackTargetNode : Node
{
    public override NodeState Evaluate(GameObject agent)
    {
        Unit unit = agent.GetComponent<Unit>();
        AIBlackboard blackboard = agent.GetComponent<AIBlackboard>();

        if (unit == null || blackboard == null) return NodeState.Failure;

        Unit target = blackboard.GetData<Unit>("TargetEnemy");
        if (target != null && target.gameObject != null && target.health > 0)
        {
            // Intentamos usar el helper, si no, aplicamos daño simple aquí
            if (AIActionsHelper.Instance != null)
            {
                AIActionsHelper.Instance.AttackUnit(unit, target);
            }
            else
            {
                target.health -= unit.damage;
                if(target.health <= 0) target.Death();
                unit.movesLeftThisTurn = 0;
            }

            blackboard.ClearData("TargetEnemy");
            return NodeState.Success;
        }
        return NodeState.Failure;
    }
}

// --- NODO DE MOVIMIENTO INTELIGENTE (CON DEBUG Y MOVIMIENTO DIRECTO) ---
[System.Serializable]
public class TacticalMoveNode : Node
{
    // Activa esto en el Inspector del nodo para ver los logs
    [ShowInEditor] public bool debugMode = true; 

    public override NodeState Evaluate(GameObject agent)
    {
        Unit unit = agent.GetComponent<Unit>();
        AIBlackboard blackboard = agent.GetComponent<AIBlackboard>();
        InfluenceMap map = Object.FindFirstObjectByType<InfluenceMap>();

        // 1. CHEQUEOS DE SEGURIDAD
        if (unit == null) { if(debugMode) Debug.LogError($"{agent.name}: Error - Unit component missing"); return NodeState.Failure; }
        if (map == null) { if(debugMode) Debug.LogError($"{agent.name}: Error - InfluenceMap not found in scene"); return NodeState.Failure; }
        if (unit.currentTile == null) { if(debugMode) Debug.LogError($"{agent.name}: Error - Unit is not on a tile"); return NodeState.Failure; }
        
        if (unit.movesLeftThisTurn <= 0) 
        {
            return NodeState.Failure; 
        }

        // 2. OBTENER ORDEN
        GlobalOrder globalOrder = GlobalOrder.ADVANCE;
        if (blackboard != null && blackboard.HasData("GlobalOrder"))
            globalOrder = blackboard.GetData<GlobalOrder>("GlobalOrder");
        else if (debugMode)
            Debug.LogWarning($"{agent.name}: No GlobalOrder in Blackboard, defaulting to ADVANCE");

        // 3. CONFIGURAR FACTORES
        float strategicFactor = 1.0f; 
        float aggressionBias = 0f;    

        switch (globalOrder)
        {
            case GlobalOrder.ALL_OUT_ATTACK: strategicFactor = 0.8f; aggressionBias = 1.0f; break;
            case GlobalOrder.ADVANCE:        strategicFactor = 1.0f; aggressionBias = 0.5f; break;
            case GlobalOrder.DEFEND_BASE:    strategicFactor = 1.2f; aggressionBias = -0.5f; break;
            case GlobalOrder.RETREAT:        strategicFactor = 2.0f; aggressionBias = -1.0f; break;
        }

        Vector2Int currentPos = unit.currentTile.gridPosition;
        Vector2Int bestMove = currentPos;
        float bestScore = -9999f;
        
        if(debugMode) Debug.Log($"<color=cyan>EVALUANDO MOVIMIENTO {agent.name}</color> | Orden: {globalOrder} | Pos: {currentPos}");

        // 4. EVALUAR VECINOS
        int neighborsChecked = 0;
        foreach (var neighbor in unit.currentTile.neighbors)
        {
            neighborsChecked++;
            if (!neighbor.walkable || neighbor.hasUnit) continue;

            float score = 0f;
            float mapInf = map.GetInfluenceAt(neighbor.gridPosition);
            score += (mapInf * aggressionBias) * strategicFactor;

            // RPS Check
            foreach(var adjacent in neighbor.neighbors)
            {
                if(adjacent.hasUnit)
                {
                    Unit otherUnit = FindUnitOnTile(adjacent);
                    if(otherUnit != null && otherUnit.isPlayerUnit != unit.isPlayerUnit)
                    {
                        int rps = (AIActionsHelper.Instance != null) ? AIActionsHelper.Instance.GetRPSMatchup(unit, otherUnit) : 0;
                        if (rps == 1) score += 0.8f; 
                        else if (rps == -1) score -= 2.0f; 
                        else score -= 0.2f;
                    }
                }
            }

            score += Random.Range(0f, 0.05f);

            if(debugMode) Debug.Log($" -> Vecino {neighbor.gridPosition}: Inf={mapInf:F2}, Score={score:F2}");

            if (score > bestScore)
            {
                bestScore = score;
                bestMove = neighbor.gridPosition;
            }
        }

        if(neighborsChecked == 0 && debugMode) Debug.LogError($"{agent.name}: Error - Unit has 0 neighbors linked!");

        // 5. EJECUTAR (MOVIMIENTO DIRECTO)
        if (bestMove != currentPos)
        {
            TileData targetTile = map.mapGenerator.GetTileAtPosition(bestMove);
            if (targetTile != null)
            {
                // A. Actualizar Posición Visual (Forzamos Z = -1 para que se vea sobre el mapa)
                unit.transform.position = new Vector3(targetTile.transform.position.x, targetTile.transform.position.y, -1f);
                
                // B. Actualizar Datos Lógicos del Mapa
                if (unit.currentTile != null) unit.currentTile.hasUnit = false;
                unit.currentTile = targetTile;
                targetTile.hasUnit = true;
                
                // C. Consumir Recurso
                unit.movesLeftThisTurn--;
                
                if(debugMode) Debug.Log($"<color=green>MOVIMIENTO ACEPTADO:</color> {agent.name} -> {bestMove} (Coste: 1, Restan: {unit.movesLeftThisTurn})");
                return NodeState.Success;
            }
        }

        if(debugMode) Debug.LogWarning($"{agent.name}: No se encontró mejor movimiento que quedarse quieto. BestScore: {bestScore}");
        return NodeState.Failure;
    }

    private Unit FindUnitOnTile(TileData tile)
    {
        Unit[] allUnits = Object.FindObjectsByType<Unit>(FindObjectsSortMode.None);
        foreach(var u in allUnits) if (u.currentTile == tile) return u;
        return null;
    }
}