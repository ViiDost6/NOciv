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
        
        if (u.hasAttackedThisTurn) 
        {
            state = NodeState.Failure;
            return state;
        }

        Unit target = ctrl.GetBestTargetInRange();
        if (target != null)
        {
            // Guardamos el objetivo en un Blackboard temporal o propiedad del controlador
            // Para simplificar, asumimos que el siguiente nodo de Acción volverá a pedir "GetBestTarget"
            // o lo guardamos en una variable estática temporal (no ideal para paralelo, ok para secuencial)
            state = NodeState.Success;
        }
        else
        {
            state = NodeState.Failure;
        }
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

// --- ACCIONES ---

[System.Serializable]
public class AttackBestTargetNode : Node
{
    public override NodeState Evaluate(GameObject agent)
    {
        AIUnitController ctrl = agent.GetComponent<AIUnitController>();
        Unit target = ctrl.GetBestTargetInRange();
        
        if (target != null)
        {
            ctrl.PerformAttack(target);
            state = NodeState.Success;
        }
        else
        {
            state = NodeState.Failure;
        }
        return state;
    }
}

[System.Serializable]
public class MoveToStrategicPositionNode : Node
{
    public override NodeState Evaluate(GameObject agent)
    {
        AIUnitController ctrl = agent.GetComponent<AIUnitController>();
        Unit u = agent.GetComponent<Unit>();
        TileData objective = ctrl.GetObjective(); 
        List<TileData> path = ctrl.CalculatePath(u.currentTile,objective);
        
        UnitManager.Instance
        //if(llamada) return NodeState.Success;
        return NodeState.Failure;
    }
}