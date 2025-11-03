using UnityEngine;

[System.Serializable]
public class Inverter : DecoratorNode
{
    public override NodeState Evaluate(GameObject agent)
    {
        if (child == null)
        {
            state = NodeState.Failure;
            return state;
        }

        NodeState childState = child.Evaluate(agent);
        switch(childState)
        {
            case NodeState.Running: // Se mantiene porque está indeterminado
                state = NodeState.Running;
                break;
            case NodeState.Failure: // Se invierte
                state = NodeState.Success;
                break;
            case NodeState.Success: // Se invierte
                state = NodeState.Failure;
                break;
        }
        return state;
    }
}