using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;
using System.Collections.Generic;
using UnityEditor;
using System.Linq;
using UnityEngine;
using System;
using UnityEditor.UIElements;
using System.Reflection;

public class BehaviourTreeGraphView : GraphView
{
    public BehaviourTree tree;
    private SerializedObject serializedTree;
    private Dictionary<string, NodeView> nodeViewLookup = new Dictionary<string, NodeView>();
    private bool needsStateClear = true; 

    public BehaviourTreeGraphView(BehaviourTree tree)
    {
        this.tree = tree;
        this.serializedTree = new SerializedObject(tree);

        this.AddManipulator(new ContentZoomer()); 
        this.AddManipulator(new ContentDragger()); 
        this.AddManipulator(new SelectionDragger()); 
        this.AddManipulator(new RectangleSelector()); 

        var grid = new GridBackground();
        Insert(0, grid);
        grid.StretchToParentSize();

        PopulateGraph();

        graphViewChanged += OnGraphViewChanged;

        // Delegados para Copy/Paste
        serializeGraphElements = SerializeGraphElementsImpl;
        unserializeAndPaste = UnserializeAndPasteImpl;
    }

    private void PopulateGraph()
    {
        serializedTree.Update(); 
        SerializedProperty nodesProperty = serializedTree.FindProperty("nodes");
        Dictionary<Node, NodeView> nodeViewMap = new();
        nodeViewLookup.Clear();

        for (int i = 0; i < nodesProperty.arraySize; i++)
        {
            SerializedProperty nodeProp = nodesProperty.GetArrayElementAtIndex(i);
            Node nodeData = nodeProp.managedReferenceValue as Node;
            if (nodeData == null) continue;

            NodeView nodeView = CreateNodeView(nodeData, nodeProp);
            nodeView.SetPosition(new Rect(nodeData.position, Vector2.zero));

            if (!string.IsNullOrEmpty(nodeData.guid)) nodeViewLookup[nodeData.guid] = nodeView;

            AddElement(nodeView);
            nodeViewMap.Add(nodeData, nodeView);
        }

        for (int i = 0; i < nodesProperty.arraySize; i++)
        {
            SerializedProperty nodeProp = nodesProperty.GetArrayElementAtIndex(i);
            Node nodeData = nodeProp.managedReferenceValue as Node;
            if (nodeData == null) continue;
            if (!nodeViewMap.TryGetValue(nodeData, out NodeView parentView)) continue;

            if (nodeData is CompositeNode composite)
            {
                foreach (Node child in composite.children)
                {
                    if (child != null && nodeViewMap.TryGetValue(child, out NodeView childView))
                    {
                        Edge edge = parentView.outputPort.ConnectTo(childView.inputPort);
                        AddElement(edge);
                    }
                }
            }
            else if (nodeData is DecoratorNode decorator)
            {
                if (decorator.child != null && nodeViewMap.TryGetValue(decorator.child, out NodeView childView))
                {
                    Edge edge = parentView.outputPort.ConnectTo(childView.inputPort);
                    AddElement(edge);
                }
            }
        }
    }

    private NodeView CreateNodeView(Node node, SerializedProperty nodeProperty)
    {
        var nodeView = new NodeView(node, nodeProperty);
        nodeView.Bind(serializedTree);
        return nodeView;
    }

    public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
    {
        base.BuildContextualMenu(evt);
        var types = TypeCache.GetTypesDerivedFrom<Node>().Where(t => !t.IsAbstract && t.IsSubclassOf(typeof(Node)));
        foreach (var type in types)
        {
            if (type == typeof(RootNode)) continue;
            evt.menu.AppendAction($"[Add]/{type.Name}", (a) => CreateNode(type, evt.localMousePosition));
        }
    }

    private void CreateNode(Type type, Vector2 position)
    {
        Node node = Activator.CreateInstance(type) as Node;
        node.guid = System.Guid.NewGuid().ToString();
        node.position = position; 

        serializedTree.Update();
        SerializedProperty nodesProp = serializedTree.FindProperty("nodes");
        nodesProp.InsertArrayElementAtIndex(nodesProp.arraySize);
        SerializedProperty newNodeProp = nodesProp.GetArrayElementAtIndex(nodesProp.arraySize - 1);
        newNodeProp.managedReferenceValue = node;

        EditorUtility.SetDirty(tree);
        serializedTree.ApplyModifiedProperties();

        NodeView nodeView = CreateNodeView(node, newNodeProp);
        nodeView.SetPosition(new Rect(position, Vector2.zero)); 
        AddElement(nodeView);
    }

    // --- COPY / PASTE IMPL ---
    string SerializeGraphElementsImpl(IEnumerable<GraphElement> elements)
    {
        var elementsList = elements.ToList();
        var nodes = elementsList.OfType<NodeView>().Select(x => x.node).ToList();
        if (nodes.Count == 0) return null;

        BehaviourTree container = ScriptableObject.CreateInstance<BehaviourTree>();
        container.nodes = new List<Node>();
        Dictionary<Node, Node> originalToCloneMap = new Dictionary<Node, Node>();

        foreach (var original in nodes)
        {
            Node clone = CopyNode(original);
            container.nodes.Add(clone);
            originalToCloneMap[original] = clone;
        }

        foreach (var original in nodes)
        {
            Node clone = originalToCloneMap[original];
            if (original is CompositeNode compositeOriginal && clone is CompositeNode compositeClone)
            {
                compositeClone.children = new List<Node>();
                foreach (var child in compositeOriginal.children)
                {
                    if (child != null && originalToCloneMap.ContainsKey(child))
                        compositeClone.children.Add(originalToCloneMap[child]);
                }
            }
            else if (original is DecoratorNode decoratorOriginal && clone is DecoratorNode decoratorClone)
            {
                if (decoratorOriginal.child != null && originalToCloneMap.ContainsKey(decoratorOriginal.child))
                    decoratorClone.child = originalToCloneMap[decoratorOriginal.child];
            }
        }
        string data = EditorJsonUtility.ToJson(container);
        UnityEngine.Object.DestroyImmediate(container);
        return data;
    }

    void UnserializeAndPasteImpl(string operationName, string data)
    {
        BehaviourTree container = ScriptableObject.CreateInstance<BehaviourTree>();
        EditorJsonUtility.FromJsonOverwrite(data, container);
        if (container.nodes == null || container.nodes.Count == 0) return;

        serializedTree.Update();
        SerializedProperty nodesProp = serializedTree.FindProperty("nodes");
        Dictionary<string, NodeView> pastedNodeViews = new Dictionary<string, NodeView>();
        Vector2 pasteOffset = new Vector2(30, 30);

        foreach (var node in container.nodes)
        {
            node.guid = System.Guid.NewGuid().ToString();
            node.position += pasteOffset;
            nodesProp.InsertArrayElementAtIndex(nodesProp.arraySize);
            SerializedProperty newNodeProp = nodesProp.GetArrayElementAtIndex(nodesProp.arraySize - 1);
            newNodeProp.managedReferenceValue = node;

            NodeView nodeView = CreateNodeView(node, newNodeProp);
            nodeView.SetPosition(new Rect(node.position, Vector2.zero));
            AddElement(nodeView);
            AddToSelection(nodeView);
            pastedNodeViews[node.guid] = nodeView;
        }

        EditorUtility.SetDirty(tree);
        serializedTree.ApplyModifiedProperties();

        foreach (var node in container.nodes)
        {
            if (!pastedNodeViews.TryGetValue(node.guid, out NodeView parentView)) continue;
            if (node is CompositeNode composite)
            {
                foreach (var child in composite.children)
                {
                    if (child != null && pastedNodeViews.TryGetValue(child.guid, out NodeView childView))
                    {
                        Edge edge = parentView.outputPort.ConnectTo(childView.inputPort);
                        AddElement(edge);
                    }
                }
            }
            else if (node is DecoratorNode decorator)
            {
                if (decorator.child != null && pastedNodeViews.TryGetValue(decorator.child.guid, out NodeView childView))
                {
                    Edge edge = parentView.outputPort.ConnectTo(childView.inputPort);
                    AddElement(edge);
                }
            }
        }
        UnityEngine.Object.DestroyImmediate(container);
    }

    private Node CopyNode(Node original)
    {
        string json = JsonUtility.ToJson(original);
        Node clone = Activator.CreateInstance(original.GetType()) as Node;
        JsonUtility.FromJsonOverwrite(json, clone);
        return clone;
    }

    private GraphViewChange OnGraphViewChanged(GraphViewChange graphViewChange)
    {
        serializedTree.Update();
        if (graphViewChange.elementsToRemove != null)
        {
            SerializedProperty nodesProperty = serializedTree.FindProperty("nodes");
            foreach (var element in graphViewChange.elementsToRemove)
            {
                if (element is NodeView nodeView)
                {
                    for (int i = 0; i < nodesProperty.arraySize; i++)
                    {
                        if (nodesProperty.GetArrayElementAtIndex(i).managedReferenceValue == nodeView.node)
                        {
                            nodesProperty.DeleteArrayElementAtIndex(i);
                            break;
                        }
                    }
                }
                if (element is Edge edge)
                {
                    NodeView parentView = edge.output.node as NodeView;
                    NodeView childView = edge.input.node as NodeView;
                    for (int i = 0; i < nodesProperty.arraySize; i++)
                    {
                        SerializedProperty nodeProp = nodesProperty.GetArrayElementAtIndex(i);
                        if (nodeProp.managedReferenceValue == parentView.node)
                        {
                            if (parentView.node is CompositeNode)
                            {
                                SerializedProperty childrenProp = nodeProp.FindPropertyRelative("children");
                                for (int j = 0; j < childrenProp.arraySize; j++)
                                {
                                    if (childrenProp.GetArrayElementAtIndex(j).managedReferenceValue == childView.node)
                                    {
                                        childrenProp.DeleteArrayElementAtIndex(j);
                                        break;
                                    }
                                }
                            }
                            else if (parentView.node is DecoratorNode)
                            {
                                SerializedProperty childProp = nodeProp.FindPropertyRelative("child");
                                if (childProp.managedReferenceValue == childView.node) childProp.managedReferenceValue = null;
                            }
                            break;
                        }
                    }
                }
            }
        }

        if (graphViewChange.edgesToCreate != null)
        {
            SerializedProperty nodesProperty = serializedTree.FindProperty("nodes");
            foreach (var edge in graphViewChange.edgesToCreate)
            {
                NodeView parentView = edge.output.node as NodeView;
                NodeView childView = edge.input.node as NodeView;
                for (int i = 0; i < nodesProperty.arraySize; i++)
                {
                    SerializedProperty nodeProp = nodesProperty.GetArrayElementAtIndex(i);
                    if (nodeProp.managedReferenceValue == parentView.node)
                    {
                        if (parentView.node is CompositeNode)
                        {
                            SerializedProperty childrenProp = nodeProp.FindPropertyRelative("children");
                            childrenProp.InsertArrayElementAtIndex(childrenProp.arraySize);
                            childrenProp.GetArrayElementAtIndex(childrenProp.arraySize - 1).managedReferenceValue = childView.node;
                        }
                        else if (parentView.node is DecoratorNode)
                        {
                            SerializedProperty childProp = nodeProp.FindPropertyRelative("child");
                            childProp.managedReferenceValue = childView.node;
                        }
                        break;
                    }
                }
            }
        }
        EditorUtility.SetDirty(tree);
        serializedTree.ApplyModifiedProperties();
        return graphViewChange;
    }

    public void UpdateNodeStates()
    {
        needsStateClear = true; 
        BehaviourTreeRunner runner = null;
        if (Selection.activeGameObject) runner = Selection.activeGameObject.GetComponent<BehaviourTreeRunner>();
        if (runner == null || runner.treeAsset != this.tree)
        {
            var runners = UnityEngine.Object.FindObjectsByType<BehaviourTreeRunner>(FindObjectsSortMode.None);
            runner = runners.FirstOrDefault(r => r.treeAsset == this.tree);
        }
        BehaviourTree runningTreeInstance = (runner != null) ? runner.runningTree : null;
        foreach (var guid in nodeViewLookup.Keys)
        {
            NodeView nodeView = nodeViewLookup[guid];
            nodeView.UpdateState(runningTreeInstance);
        }
    }

    public void ClearNodeStates()
    {
        if (!needsStateClear) return;
        foreach (var guid in nodeViewLookup.Keys)
        {
            NodeView nodeView = nodeViewLookup[guid];
            nodeView.ClearState(); 
        }
        needsStateClear = false;
    }

    public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
    {
        var compatiblePorts = new List<Port>();
        ports.ForEach(port =>
        {
            if (port == startPort) return;
            if (port.node == startPort.node) return;
            if (port.direction == startPort.direction) return;
            if (port.portType != startPort.portType) return;
            compatiblePorts.Add(port);
        });
        return compatiblePorts;
    }

    public class NodeView : UnityEditor.Experimental.GraphView.Node
    {
        public Node node;
        private SerializedProperty nodeProperty;
        public Port inputPort;
        public Port outputPort;
        private NodeState lastState = NodeState.Inactive; // Inicializamos en Inactive

        public NodeView(Node node, SerializedProperty nodeProperty)
        {
            this.node = node;
            this.nodeProperty = nodeProperty;
            string defaultName = (node is RootNode) ? "ROOT" : node.GetType().Name;
            if (node is RootNode) capabilities = capabilities & ~Capabilities.Deletable;
            this.title = string.IsNullOrEmpty(node.nodeName) ? defaultName : node.nodeName;

            CreateInputPorts();
            CreateOutputPorts();
            CreatePropertyFields(nodeProperty);
            RegisterCallback<GeometryChangedEvent>(OnNodeMoved);
        }

        private void OnNodeMoved(GeometryChangedEvent evt)
        {
            if (evt.newRect.position == node.position) return;
            node.position = evt.newRect.position;
            EditorUtility.SetDirty(nodeProperty.serializedObject.targetObject);
        }

        private void CreatePropertyFields(SerializedProperty nodeProperty)
        {
            SerializedProperty nameProp = nodeProperty.FindPropertyRelative("nodeName");
            if (nameProp != null)
            {
                TextField nameField = new TextField("Name");
                nameField.SetValueWithoutNotify(nameProp.stringValue);
                nameField.RegisterValueChangedCallback(evt =>
                {
                    string newName = evt.newValue;
                    string defaultName = node is RootNode ? "ROOT" : node.GetType().Name;
                    title = string.IsNullOrEmpty(newName) ? defaultName : newName;
                    node.nodeName = newName;
                    EditorUtility.SetDirty(nodeProperty.serializedObject.targetObject);
                });
                extensionContainer.Add(nameField);
            }

            var fields = node.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance);
            foreach (var field in fields)
            {
                if (field.GetCustomAttribute<ShowInEditorAttribute>() == null) continue;
                SerializedProperty prop = nodeProperty.FindPropertyRelative(field.Name);
                if (prop == null) continue;
                PropertyField propField = new PropertyField(prop);
                propField.Bind(nodeProperty.serializedObject);
                propField.RegisterValueChangeCallback(evt => EditorUtility.SetDirty(nodeProperty.serializedObject.targetObject));
                extensionContainer.Add(propField);
            }
        }

        private void CreateInputPorts()
        {
            if (node is RootNode) return;
            inputPort = Port.Create<Edge>(Orientation.Vertical, Direction.Input, Port.Capacity.Single, typeof(bool));
            inputPort.portName = "In";
            inputContainer.Add(inputPort);
        }
        private void CreateOutputPorts()
        {
            if (node is DecoratorNode)
            {
                outputPort = Port.Create<Edge>(Orientation.Vertical, Direction.Output, Port.Capacity.Single, typeof(bool));
                outputPort.portName = "Out";
                outputContainer.Add(outputPort);
            }
            else if (node is CompositeNode)
            {
                outputPort = Port.Create<Edge>(Orientation.Vertical, Direction.Output, Port.Capacity.Multi, typeof(bool));
                outputPort.portName = "Out";
                outputContainer.Add(outputPort);
            }
        }

        public void UpdateState(BehaviourTree runningTreeInstance)
        {
            if (runningTreeInstance == null) { ClearState(); return; }

            NodeState currentState = runningTreeInstance.GetNodeState(node.guid);
            if (currentState == lastState) return;

            lastState = currentState;
            ClearState();

            switch (currentState)
            {
                case NodeState.Running:
                    style.backgroundColor = new StyleColor(new Color(0.2f, 0.4f, 0.6f)); // Azul
                    AddToClassList("running");
                    break;
                case NodeState.Success:
                    style.backgroundColor = new StyleColor(new Color(0.2f, 0.6f, 0.2f)); // Verde
                    AddToClassList("success");
                    break;
                case NodeState.Failure:
                    style.backgroundColor = new StyleColor(new Color(0.6f, 0.2f, 0.2f)); // Rojo
                    AddToClassList("failure");
                    break;
                case NodeState.Inactive:
                    // Color por defecto (Gris) - Se limpia en ClearState()
                    break;
            }
        }

        public void ClearState()
        {
            // Reset visual
            style.backgroundColor = new StyleColor(StyleKeyword.Null);
            RemoveFromClassList("running");
            RemoveFromClassList("success");
            RemoveFromClassList("failure");
        }
    }
}