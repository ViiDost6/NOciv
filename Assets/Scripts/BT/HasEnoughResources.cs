using UnityEngine;

public class HasEnoughResources : Node
{
    [ShowInEditor]
    public int cost = 0;
    public override NodeState Evaluate(GameObject agent)
    {
        EnemyStats es = agent.GetComponent<EnemyStats>();
        if (es.currency < cost) state = NodeState.Failure;
        else state = NodeState.Success;
        
        return state;
    }
}