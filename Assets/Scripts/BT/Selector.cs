using UnityEngine;

[System.Serializable]
public class Selector : CompositeNode
{
    private int runningChildIndex = 0;
    public override NodeState Evaluate(GameObject agent)
    {
        if (state != NodeState.Running)
        {
            runningChildIndex = 0;
        }

        for (int i = runningChildIndex; i < children.Count; i++)
        {
            runningChildIndex = i;
            Node child = children[i];
            switch (child.Evaluate(agent))
            {
                case NodeState.Running: // Sigue en marcha
                    state = NodeState.Running;
                    return state;
                case NodeState.Success: // Tuvo éxito
                    state = NodeState.Success;
                    runningChildIndex = 0;
                    return state;
                case NodeState.Failure:
                    continue;
            }
        }
        state = NodeState.Failure;
        runningChildIndex = 0;
        return state;
    }
}