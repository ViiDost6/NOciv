using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "New Behaviour Tree", menuName = "AI/Behaviour Tree")]
public class BehaviourTree : ScriptableObject
{
    [SerializeReference]
    public RootNode rootNode;
    [System.NonSerialized]
    public NodeState treeState = NodeState.Running;

    // Lista de nodos del árbol. (necesario para el editor)
    [SerializeReference, HideInInspector]
    public List<Node> nodes = new List<Node>();

    [System.NonSerialized]
    private Dictionary<string, Node> nodeLookup;

    public BehaviourTree Clone()
    {
        // Se crea copia profunda con Instantiate,con todos los campos con [SerializeReference]
        BehaviourTree newTree = Instantiate(this);
        newTree.name = this.name + " (Instance)"; // Se añade Instance al final para correctamente diferenciarlo de ser necesario
        newTree.nodeLookup = new Dictionary<string, Node>();
        newTree.nodes.ForEach(n =>
        {
            if (n != null)
            {
                newTree.nodeLookup[n.guid] = n;
                n.ResetState(); // Resetea el estado al clonar
            }
        });
        // Opcional: dejar el árbol reseteado si no lo estuviera
        newTree.treeState = NodeState.Running;
        return newTree;
    }

    // Método para que el editor consulte el estado de un nodo
    public NodeState GetNodeState(string guid)
    {
        if (nodeLookup != null && nodeLookup.TryGetValue(guid, out Node node))
        {
            return node.GetState();
        }
        return NodeState.Failure; // Estado por defecto si no se encuentra
    }

    private void Reset()
    {
        rootNode = new RootNode
        {
            guid = System.Guid.NewGuid().ToString()
        };

        nodes = new List<Node>
        {
            rootNode
        };

        // Opcional: Dale un nombre para depuración
        //rootNode.name = "Root"; 
    }
}