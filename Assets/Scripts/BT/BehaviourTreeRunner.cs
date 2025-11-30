using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[System.Serializable]
public class TaskBinding
{
    public string taskName;
    public UnityEvent action;
}

public class BehaviourTreeRunner : MonoBehaviour
{
    public BehaviourTree treeAsset;

    [HideInInspector]
    public BehaviourTree runningTree { get; private set; }
    
    public List<TaskBinding> taskBindings;
    private Dictionary<string, UnityEvent> taskLookup;

    void Start()
    {
        if (treeAsset == null)
        {
            Debug.Log($"No hay BehaviourTree asignado en {gameObject.name}.", this);
            enabled = false;
            return;
        }
        runningTree = treeAsset.Clone();

        taskLookup = new Dictionary<string, UnityEvent>();
        foreach (var binding in taskBindings)
        {
            taskLookup[binding.taskName] = binding.action;
        }
    }

    // --- CAMBIO: Eliminado Update() ---
    
    // Método público para ejecutar el árbol manualmente UNA vez.
    // Devuelve el estado final (Success/Failure/Running)
    public NodeState RunTree()
    {
        if (runningTree != null)
        {
            // Evaluamos una vez
            NodeState newState = runningTree.rootNode.Evaluate(this.gameObject);
            runningTree.treeState = newState;
            return newState;
        }
        return NodeState.Failure;
    }

    public NodeState ExecuteTask(string taskName)
    {
        if (taskLookup.TryGetValue(taskName, out UnityEvent action))
        {
            action.Invoke();
            return NodeState.Success;
        }
        else
        {
            Debug.LogWarning($"Tarea '{taskName}' no encontrada en {gameObject.name}");
            return NodeState.Failure;
        }
    }
}