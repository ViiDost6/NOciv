using System.Collections.Generic;
using UnityEngine;

// --- CONDICIONES ---

[System.Serializable]
public class CheckCanAttackNode : Node
{
    public override NodeState Evaluate(GameObject agent)
    {
        AIUnitController ctrl = agent.GetComponent<AIUnitController>();
        Unit u = agent.GetComponent<Unit>();
        if (u.hasAttackedThisTurn) { state = NodeState.Failure; return state; }
        Unit target = ctrl.GetBestTargetInRange();
        state = (target != null) ? NodeState.Success : NodeState.Failure;
        return state;
    }
}

[System.Serializable]
public class CheckCanMoveNode : Node
{
    public override NodeState Evaluate(GameObject agent)
    {
        Unit u = agent.GetComponent<Unit>();
        state = (u.movesLeftThisTurn > 0) ? NodeState.Success : NodeState.Failure;
        return state;
    }
}

[System.Serializable]
public class CheckBetterLocalObjectiveNode : Node
{
    public override NodeState Evaluate(GameObject agent)
    {
        AIUnitController ctrl = agent.GetComponent<AIUnitController>();
        if (ctrl.currentAnalysis.isLocalOptionBetter && ctrl.currentAnalysis.bestLocalOpportunity != null)
            state = NodeState.Success;
        else
            state = NodeState.Failure;
        return state;
    }
}

// --- ACCIONES ---

[System.Serializable]
public class PerformReconNode : Node
{
    public override NodeState Evaluate(GameObject agent)
    {
        AIUnitController ctrl = agent.GetComponent<AIUnitController>();
        ctrl.PerformTacticalRecon();
        state = NodeState.Success;
        return state;
    }
}

[System.Serializable]
public class AttackBestTargetNode : Node
{
    private bool waitingForAttack = false;
    public override NodeState Evaluate(GameObject agent)
    {
        AIUnitController ctrl = agent.GetComponent<AIUnitController>();
        if (state != NodeState.Running) waitingForAttack = false;
        if (waitingForAttack)
        {
            if (!ctrl.IsBusy) { waitingForAttack = false; state = NodeState.Success; return NodeState.Success; }
            state = NodeState.Running; return NodeState.Running;
        }
        if (ctrl.IsBusy) { state = NodeState.Running; return NodeState.Running; }

        Unit target = ctrl.GetBestTargetInRange();
        if (target != null)
        {
            ctrl.PerformAttack(target);
            waitingForAttack = true;
            state = NodeState.Running;
        }
        else state = NodeState.Failure;
        return state;
    }
}

[System.Serializable]
public class MoveToStrategicPositionNode : Node
{
    private bool waitingForMove = false;
    public override NodeState Evaluate(GameObject agent)
    {
        AIUnitController ctrl = agent.GetComponent<AIUnitController>();
        Unit u = agent.GetComponent<Unit>();
        if (state != NodeState.Running) waitingForMove = false;
        if (waitingForMove)
        {
            if (!ctrl.IsBusy) { waitingForMove = false; state = NodeState.Success; return NodeState.Success; }
            state = NodeState.Running; return NodeState.Running;
        }
        if (ctrl.IsBusy) { state = NodeState.Running; return NodeState.Running; }
        if (u.movesLeftThisTurn <= 0) { state = NodeState.Failure; return NodeState.Failure; }

        TileData targetTile = ctrl.currentAnalysis.bestStrategicMove;
        if (targetTile == null) targetTile = ctrl.GetBestTacticalMovePosition();

        // FIX: Si ya estamos ahí, es un éxito (nos hemos posicionado bien), no un fallo
        if (targetTile == u.currentTile)
        {
            state = NodeState.Success;
            return NodeState.Success;
        }

        if (targetTile != null)
        {
            List<TileData> path = ctrl.CalculatePath(u.currentTile, targetTile);
            if (path != null && path.Count > 0)
            {
                ctrl.MoveAlongPath(path);
                waitingForMove = true;
                state = NodeState.Running;
                return NodeState.Running;
            }
        }
        state = NodeState.Failure;
        return state;
    }
}

[System.Serializable]
public class MoveToLocalObjectiveNode : Node
{
    private bool waitingForMove = false;
    public override NodeState Evaluate(GameObject agent)
    {
        AIUnitController ctrl = agent.GetComponent<AIUnitController>();
        Unit u = agent.GetComponent<Unit>();
        if (state != NodeState.Running) waitingForMove = false;
        if (waitingForMove)
        {
            if (!ctrl.IsBusy) { waitingForMove = false; state = NodeState.Success; return NodeState.Success; }
            state = NodeState.Running; return NodeState.Running;
        }
        if (ctrl.IsBusy) { state = NodeState.Running; return NodeState.Running; }
        if (u.movesLeftThisTurn <= 0) { state = NodeState.Failure; return NodeState.Failure; }

        TileData targetTile = ctrl.currentAnalysis.bestLocalOpportunity;

        // FIX: Éxito si ya estamos en posición de disparo/captura local
        if (targetTile == u.currentTile)
        {
            state = NodeState.Success;
            return NodeState.Success;
        }

        if (targetTile != null)
        {
            List<TileData> path = ctrl.CalculatePath(u.currentTile, targetTile);
            if (path != null && path.Count > 0)
            {
                ctrl.MoveAlongPath(path);
                waitingForMove = true;
                state = NodeState.Running;
                return NodeState.Running;
            }
        }
        state = NodeState.Failure;
        return state;
    }
}