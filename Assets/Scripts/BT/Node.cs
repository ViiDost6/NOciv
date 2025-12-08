using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

// Estado Inactive añadido para limpiar el debug visual
public enum NodeState
{
    Inactive, 
    Running, 
    Success, 
    Failure 
}

[System.Serializable]
public abstract class Node
{
    public string nodeName;
    [HideInInspector] public string guid;
    [HideInInspector] public Vector2 position;
    
    // Inicializamos en Inactive para que no empiece con colores
    protected NodeState state = NodeState.Inactive; 
    public NodeState GetState() { return state; }

    public virtual void ResetState()
    {
        // Al resetear, volvemos a estado neutro
        state = NodeState.Inactive; 
    }

    public abstract NodeState Evaluate(GameObject agent);
}

[System.Serializable]
public abstract class CompositeNode : Node
{
    [SerializeReference]
    public List<Node> children = new List<Node>();
}

[System.Serializable]
public abstract class DecoratorNode : Node
{
    [SerializeReference]
    public Node child;
}

[System.Serializable]
public class CallMethodNode : Node
{
    [ShowInEditor]
    public string taskName; 

    public override NodeState Evaluate(GameObject agent)
    {
        BehaviourTreeRunner runner = agent.GetComponent<BehaviourTreeRunner>();
        if (runner == null)
        {
            Debug.LogError($"CallMethodNode: No BehaviourTreeRunner in {agent.name}");
            state = NodeState.Failure;
            return state;
        }
        state = runner.ExecuteTask(taskName);
        return state;
    }
}

[System.Serializable]
public class RootNode : DecoratorNode
{
    public override NodeState Evaluate(GameObject agent)
    {
        if (child == null)
        {
            state = NodeState.Failure;
            return state;
        }
        state = child.Evaluate(agent);
        return state;
    }
}

[System.Serializable]
public class DebugLogNode : Node
{
    [ShowInEditor] public string message;

    public override NodeState Evaluate(GameObject agent)
    {
        Debug.Log(message);
        state = NodeState.Success; 
        return state;
    }
}