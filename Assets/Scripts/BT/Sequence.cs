using UnityEngine;

[System.Serializable]
public class Sequence : CompositeNode
{
    //Hijo en ejecución
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
                case NodeState.Failure: // Falló el hijo, se sale
                    state = NodeState.Failure;
                    runningChildIndex = 0; // Reset para próxima
                    return state;
                case NodeState.Success: // Éxito, seguirá el bucle
                    continue;
            }
        }
        state = NodeState.Success;
        runningChildIndex = 0; //Reset para la próxima
        return state;
    }
}