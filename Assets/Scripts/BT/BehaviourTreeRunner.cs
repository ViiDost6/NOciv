using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

// Esta clase 'conecta' un nombre (string) a un UnityEvent
[System.Serializable]
public class TaskBinding
{
    public string taskName;
    public UnityEvent action;
}

public class BehaviourTreeRunner : MonoBehaviour
{
    //Plantilla asignada desde el inspector
    public BehaviourTree treeAsset;

    //Copia que se ejecutará
    [HideInInspector]
    public BehaviourTree runningTree { get; private set; }
    
    // Esta lista aparecerá en el Inspector de tu AgenteIA
    public List<TaskBinding> taskBindings;

    // Un diccionario para búsqueda rápida en runtime
    private Dictionary<string, UnityEvent> taskLookup;

    void Start()
    {
        if (treeAsset == null)
        {
            Debug.Log($"No hay BehaviourTree asignado en {gameObject.name}.", this);
            enabled = false;
            return;
        }
        // Se clona el árbol desde el asset puesto en el inspector
        runningTree = treeAsset.Clone();

        // Construir el diccionario al empezar
        taskLookup = new Dictionary<string, UnityEvent>();
        foreach (var binding in taskBindings)
        {
            taskLookup[binding.taskName] = binding.action;
        }
    }

    void Update()
    {
        //Si el árbol no es nulo, vamos comprobándolo
        if (runningTree != null)
        {
            // Comprueba si el árbol (clonado) SIGUE en ejecución
            if (runningTree.treeState == NodeState.Running)
            {
                // Solo si está en 'Running', evalúa el árbol
                NodeState newState = runningTree.rootNode.Evaluate(this.gameObject);

                // Actualiza el estado general del árbol
                runningTree.treeState = newState;
            }
        }

    }

    // El método que el CallMethodNode buscará
    public NodeState ExecuteTask(string taskName)
    {
        if (taskLookup.TryGetValue(taskName, out UnityEvent action))
        {
            action.Invoke();
            return NodeState.Success;
        }
        else
        {
            Debug.LogWarning($"Tarea '{taskName}' no encontrada en el BehaviourTreeRunner de {gameObject.name}");
            return NodeState.Failure;
        }
    }
}