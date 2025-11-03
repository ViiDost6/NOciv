using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;
using System.Collections.Generic;
using UnityEditor;
using System.Linq;
using UnityEngine;
using System;
using UnityEditor.UIElements;
using System.Reflection;
using UnityEngine.Events;

public class BehaviourTreeGraphView : GraphView
{
    public BehaviourTree tree;
    private SerializedObject serializedTree;
    // Mapa de GUID -> NodeView para consulta rápida
    private Dictionary<string, NodeView> nodeViewLookup = new Dictionary<string, NodeView>();
    private bool needsStateClear = true; // Flag para limpiar al salir de Play Mode
    public BehaviourTreeGraphView(BehaviourTree tree)
    {
        this.tree = tree;
        this.serializedTree = new SerializedObject(tree);
        // Carga el Stylesheet directamente en el GraphView
        /* var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/Editor/BehaviourTreeEditor.uss");
        if (styleSheet == null)
        {
            Debug.LogError("¡Error! Stylesheet no encontrado en 'Assets/Editor/BehaviourTreeEditor.uss' (desde GraphView).");
        }
        else
        {
            // Añádelo a la lista de 'styleSheets' (en plural) del GraphView
            styleSheets.Add(styleSheet);
        } */
        //Configuración visual
        this.AddManipulator(new ContentZoomer()); // Zoom
        this.AddManipulator(new ContentDragger()); //Mover fondo
        this.AddManipulator(new SelectionDragger()); //Mover nodos
        this.AddManipulator(new RectangleSelector()); //Seleccionar en rectángulo

        //Rejilla
        var grid = new GridBackground();
        Insert(0, grid);
        grid.StretchToParentSize();

        PopulateGraph();

        graphViewChanged += OnGraphViewChanged;

        //AddElement(CreateNode());
    }

    private void PopulateGraph()
    {
        serializedTree.Update(); // Sincroniza el objeto serializado

        SerializedProperty nodesProperty = serializedTree.FindProperty("nodes");

        Dictionary<Node, NodeView> nodeViewMap = new();
        nodeViewLookup.Clear();

        // --- BUCLE 1: Crear todos los NodeViews ---
        for (int i = 0; i < nodesProperty.arraySize; i++)
        {
            SerializedProperty nodeProp = nodesProperty.GetArrayElementAtIndex(i);

            // Leemos el nodo desde la 'SerializedProperty'
            // en lugar de 'tree.nodes[i]'
            Node nodeData = nodeProp.managedReferenceValue as Node;

            if (nodeData == null) continue; // Seguridad por si algo salió mal

            NodeView nodeView = CreateNodeView(nodeData, nodeProp);
            nodeView.SetPosition(new Rect(nodeData.position, Vector2.zero));

            if (!string.IsNullOrEmpty(nodeData.guid))
            {
                nodeViewLookup[nodeData.guid] = nodeView;
            }

            AddElement(nodeView);
            nodeViewMap.Add(nodeData, nodeView);
        }

        // --- BUCLE 2: Conectar todos los Edges ---
        // También iteramos usando las SerializedProperties para seguridad
        for (int i = 0; i < nodesProperty.arraySize; i++)
        {
            SerializedProperty nodeProp = nodesProperty.GetArrayElementAtIndex(i);
            Node nodeData = nodeProp.managedReferenceValue as Node;

            if (nodeData == null) continue;

            // Busca el NodeView que acabamos de crear
            if (!nodeViewMap.TryGetValue(nodeData, out NodeView parentView))
            {
                continue;
            }

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
        // TODO: AÑADIR CAMPOS DE TEXTO EDITABLES
        nodeView.Bind(serializedTree);
        return nodeView;
    }

    public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
    {
        base.BuildContextualMenu(evt);

        // Obtenemos dinámicamente tipos de nodos
        var types = TypeCache.GetTypesDerivedFrom<Node>()
            .Where(t => !t.IsAbstract && t.IsSubclassOf(typeof(Node)));

        foreach (var type in types)
        {
            if (type == typeof(RootNode)) continue;
            evt.menu.AppendAction($"[Add]/{type.Name}", (a) =>
            {
                CreateNode(type, evt.localMousePosition);
            });
        }
    }

    private void CreateNode(Type type, Vector2 position)
    {
        // 1. Crear la instancia de DATOS
        Node node = Activator.CreateInstance(type) as Node;
        node.guid = System.Guid.NewGuid().ToString();
        node.position = position; // Guardar la posición inicial

        // 2. Añadirlo al SCRIPTABLE OBJECT (Forma correcta)
        serializedTree.Update();
        SerializedProperty nodesProp = serializedTree.FindProperty("nodes");

        // Añadimos un nuevo elemento al final de la lista 'nodes'
        nodesProp.InsertArrayElementAtIndex(nodesProp.arraySize);
        SerializedProperty newNodeProp = nodesProp.GetArrayElementAtIndex(nodesProp.arraySize - 1);

        // Asignamos nuestra instancia de nodo a esa nueva ranura
        newNodeProp.managedReferenceValue = node;

        EditorUtility.SetDirty(tree);
        serializedTree.ApplyModifiedProperties();

        // 3. Crear la instancia VISUAL
        NodeView nodeView = CreateNodeView(node, newNodeProp);
        nodeView.SetPosition(new Rect(position, new Vector2(100, 150))); // Posición visual
        AddElement(nodeView);
    }

    private GraphViewChange OnGraphViewChanged(GraphViewChange graphViewChange)
    {
        serializedTree.Update();

        // --- MANEJAR BORRADOS ---
        if (graphViewChange.elementsToRemove != null)
        {
            SerializedProperty nodesProperty = serializedTree.FindProperty("nodes");

            foreach (var element in graphViewChange.elementsToRemove)
            {
                // A. Borrar un NODO
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

                // B. Borrar una CONEXIÓN (Edge)
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
                                if (childProp.managedReferenceValue == childView.node)
                                {
                                    childProp.managedReferenceValue = null;
                                }
                            }
                            break;
                        }
                    }
                }
            }
        }

        // --- MANEJAR CREACIÓN DE CONEXIONES ---
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

    // Método llamado por OnInspectorUpdate de la ventana
    public void UpdateNodeStates()
    {
        needsStateClear = true; // Marcar para que se limpie al salir de Play Mode

        // 1. Encontrar el 'Runner' en la escena.
        //    (Una forma simple es buscar el que está seleccionado)
        BehaviourTreeRunner runner = null;
        if (Selection.activeGameObject)
        {
            runner = Selection.activeGameObject.GetComponent<BehaviourTreeRunner>();
        }

        // Si el runner seleccionado no es el de este árbol, busca en toda la escena
        // (Esto puede ser lento, pero es más robusto)
        if (runner == null || runner.treeAsset != this.tree)
        {
            var runners = UnityEngine.Object.FindObjectsByType<BehaviourTreeRunner>(FindObjectsSortMode.None);
            runner = runners.FirstOrDefault(r => r.treeAsset == this.tree);
        }

        // 2. Obtener su árbol clonado (el de runtime)
        BehaviourTree runningTreeInstance = (runner != null) ? runner.runningTree : null;

        // 3. Iterar todos los nodos VISUALES y actualizar su estado
        foreach (var guid in nodeViewLookup.Keys)
        {
            NodeView nodeView = nodeViewLookup[guid];
            nodeView.UpdateState(runningTreeInstance);
        }
    }

    // Método llamado por OnInspectorUpdate cuando no se está en Play Mode
    public void ClearNodeStates()
    {
        if (!needsStateClear) return;

        foreach (var guid in nodeViewLookup.Keys)
        {
            NodeView nodeView = nodeViewLookup[guid];
            nodeView.ClearState(); // Llama al método que quita las clases USS
        }
        needsStateClear = false;
    }

    // Este método es llamado por GraphView cuando empiezas a
    // arrastrar una conexión (Edge) desde un puerto.
    public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
    {
        // 1. Creamos una lista para guardar los puertos compatibles
        var compatiblePorts = new List<Port>();

        // 2. Iteramos sobre TODOS los puertos existentes en el grafo
        ports.ForEach(port =>
        {
            // 3. Definimos las reglas de compatibilidad

            // Regla 1: No conectar un puerto consigo mismo
            if (port == startPort) return;

            // Regla 2: No conectar puertos del mismo nodo
            if (port.node == startPort.node) return;

            // Regla 3: No conectar puertos con la misma dirección
            // (Input no se puede conectar a Input)
            if (port.direction == startPort.direction) return;

            // Regla 4: (Opcional pero recomendada)
            // No conectar tipos de datos incompatibles.
            // (Como nosotros usamos 'typeof(bool)' para todos, esto funcionará)
            if (port.portType != startPort.portType) return;

            // Si pasa todas las reglas, es un puerto válido
            compatiblePorts.Add(port);
        });

        // 4. Devolvemos la lista de puertos a los que SÍ se puede conectar
        return compatiblePorts;
    }

    public class NodeView : UnityEditor.Experimental.GraphView.Node
    {
        public Node node;
        private SerializedProperty nodeProperty;
        public Port inputPort;
        public Port outputPort;
        private NodeState lastState; // Para evitar aplicar estilos innecesariamente

        public NodeView(Node node, SerializedProperty nodeProperty)
        {
            this.node = node;
            this.nodeProperty = nodeProperty;

            string defaultName;
            if (node is RootNode)
            {
                defaultName = "ROOT";
                capabilities = capabilities & ~Capabilities.Deletable;
            }
            else
            {
                defaultName = node.GetType().Name;
            }

            // Si 'nodeName' no está vacío, úsalo. Si no, usa el 'defaultName'.
            this.title = string.IsNullOrEmpty(node.nodeName) ? defaultName : node.nodeName;

            CreateInputPorts();
            CreateOutputPorts();
            CreatePropertyFields(nodeProperty);
            // Registra un callback que se dispara cuando la geometría
            // (incluida la posición) del nodo cambia.
            RegisterCallback<GeometryChangedEvent>(OnNodeMoved);
        }

        private void OnNodeMoved(GeometryChangedEvent evt)
        {
            // Esto se puede disparar por zoom o cambios de tamaño,
            // así que nos aseguramos de que la posición realmente haya cambiado.
            if (evt.newRect.position == node.position)
            {
                return;
            }

            // 1. Guardar la nueva posición en el objeto de DATOS
            node.position = evt.newRect.position;

            // 2. Marcar el asset como sucio
            EditorUtility.SetDirty(nodeProperty.serializedObject.targetObject);
        }

        // EN: BehaviourTreeGraphView.cs -> CLASE ANIDADA NodeView

        private void CreatePropertyFields(SerializedProperty nodeProperty)
        {
            // --- 1. MANEJO ESPECIAL PARA 'nodeName' (Tu código, está bien) ---
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

            // --- 2. MANEJO AUTOMÁTICO (¡CON LA CORRECCIÓN!) ---
            var fields = node.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance);
            foreach (var field in fields)
            {
                if (field.GetCustomAttribute<ShowInEditorAttribute>() == null)
                {
                    continue; // Saltar campos sin el atributo
                }

                SerializedProperty prop = nodeProperty.FindPropertyRelative(field.Name);
                if (prop == null)
                {
                    continue; // Saltar si no se encuentra la propiedad
                }

                PropertyField propField = new PropertyField(prop);
                propField.Bind(nodeProperty.serializedObject);

                // Añadimos el "SetDirty" para que se guarden los cambios
                propField.RegisterValueChangeCallback(evt =>
                {
                    EditorUtility.SetDirty(nodeProperty.serializedObject.targetObject);
                });

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
            // Decorators tienen 1 salida
            if (node is DecoratorNode)
            {
                outputPort = Port.Create<Edge>(Orientation.Vertical, Direction.Output, Port.Capacity.Single, typeof(bool));
                outputPort.portName = "Out";
                outputContainer.Add(outputPort);
            }
            // Composites tienen múltiples salidas
            else if (node is CompositeNode)
            {
                outputPort = Port.Create<Edge>(Orientation.Vertical, Direction.Output, Port.Capacity.Multi, typeof(bool));
                outputPort.portName = "Out";
                outputContainer.Add(outputPort);
            }
        }

        // Método llamado por el GraphView para actualizar el color
        public void UpdateState(BehaviourTree runningTreeInstance)
        {
            // Si no hay un árbol ejecutándose (ej. el agente no está seleccionado)
            if (runningTreeInstance == null)
            {
                ClearState();
                return;
            }

            NodeState currentState = runningTreeInstance.GetNodeState(node.guid);

            // Si el estado no ha cambiado, no hacemos nada (eficiencia)
            if (currentState == lastState) return;

            lastState = currentState;

            // Limpia estilos anteriores
            ClearState();

            // Aplica el estilo nuevo
            switch (currentState)
            {
                case NodeState.Running:
                    style.backgroundColor = new StyleColor(new Color(0.2f, 0.4f, 0.6f)); // Azul
                    break;
                case NodeState.Success:
                    style.backgroundColor = new StyleColor(new Color(0.2f, 0.6f, 0.2f)); // Verde
                    break;
                case NodeState.Failure:
                    style.backgroundColor = new StyleColor(new Color(0.6f, 0.2f, 0.2f)); // Rojo
                    break;
            }
        }

        // Limpia todos los estilos de estado
        public void ClearState()
        {
            lastState = NodeState.Failure; // Resetea el estado de caché

            // Resetea el color de fondo al valor por defecto
            style.backgroundColor = new StyleColor(StyleKeyword.Null);

            RemoveFromClassList("running");
            RemoveFromClassList("success");
            RemoveFromClassList("failure");
        }
    }
}