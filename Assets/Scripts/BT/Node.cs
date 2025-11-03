using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public enum NodeState
{
    Running, // En ejecución
    Success, // Terminado con éxito
    Failure // Fallido
}

[System.Serializable]
public abstract class Node
{
    public string nodeName;
    [HideInInspector]
    public string guid;
    // Guardamos la posición del nodo en el grafo
    [HideInInspector] public Vector2 position;
    // Almacena el estado para que los nodos que estén en ejecución (Running) puedan continuar
    protected NodeState state;
    public NodeState GetState() { return state; }

    public virtual void ResetState()
    {
        state = NodeState.Failure; // Un estado "inactivo" por defecto
    }
    // Método principal que evalua el árbol
    public abstract NodeState Evaluate(GameObject agent);
}

[System.Serializable]
public abstract class CompositeNode : Node
{
    // Esta lista será la que tu GUI (GraphView) modificará
    // [SerializeReference] es útil aquí si no usas ScriptableObjects por nodo.
    [SerializeReference]
    public List<Node> children = new List<Node>();
}

[System.Serializable]
public abstract class DecoratorNode : Node
{
    // Guarda referencia de su único hijo
    [SerializeReference]
    public Node child;
}

[System.Serializable]
public class CallMethodNode : Node
{
    [ShowInEditor]
    public string taskName; // E.g., "Attack"

    public override NodeState Evaluate(GameObject agent)
    {
        // 1. Encontrar el Runner en el agente
        BehaviourTreeRunner runner = agent.GetComponent<BehaviourTreeRunner>();
        if (runner == null)
        {
            Debug.LogError($"CallMethodNode: No se encontró BehaviourTreeRunner en {agent.name}");
            state = NodeState.Failure;
            return state;
        }

        // 2. Pedirle al Runner que ejecute la tarea
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
    [ShowInEditor]
    public string message;

    public override NodeState Evaluate(GameObject agent)
    {
        Debug.Log(message);
        state = NodeState.Success; // Siempre tiene éxito instantáneamente
        return state;
    }
}

