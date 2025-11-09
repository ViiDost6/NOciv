using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

public class BehaviourTreeEditorWindow : EditorWindow
{
    private BehaviourTreeGraphView graphView; // vista del grafo
    private BehaviourTree tree; // Referencia al asset que se está editando

    [OnOpenAsset(1)]
    public static bool OnOpenAsset(int instanceID, int line)
    {
        if (Selection.activeObject is BehaviourTree)
        {
            OpenWindow(Selection.activeObject as BehaviourTree);
            return true;
        }
        return false;
    }

    public static void OpenWindow(BehaviourTree tree)
    {
        BehaviourTreeEditorWindow window = GetWindow<BehaviourTreeEditorWindow>();
        window.titleContent = new GUIContent(tree.name);
        window.tree = tree;
        window.CreateGUI();
    }

    // para construir la GUI con UIElements
    public void CreateGUI()
    {
        VisualElement root = rootVisualElement;
        root.Clear();

        if (tree == null)
        {
            var label = new Label("No hay Behaviour Tree seleccionado. Haz doble clic en uno de la vista de proyecto.");
            root.Add(label);
            return;
        }

        var toolbar = new Toolbar();

        var assetNameField = new TextField("Tree Name:")
        {
            value = tree.name
        };
        assetNameField.RegisterValueChangedCallback(evt => tree.name = evt.newValue);
        toolbar.Add(assetNameField);

        var saveButton = new Button(() => { SaveTree(); });
        toolbar.Add(saveButton);

        root.Add(toolbar);
        graphView = new BehaviourTreeGraphView(tree)
        {
            name = "Behaviour Tree Graph"
        };
        graphView.StretchToParentSize();
        root.Add(graphView);

    }

    // Este método es llamado por Unity constantemente en el Editor
    private void OnInspectorUpdate()
    {
        // Si el GraphView existe y el juego está en Play Mode...
        if (graphView != null && Application.isPlaying)
        {
            // ...le decimos que actualice los estados de los nodos
            graphView.UpdateNodeStates();
        }
        else if (graphView != null)
        {
            // Si no estamos en Play Mode, nos aseguramos
            // de que todos los nodos estén limpios (sin color)
            graphView.ClearNodeStates();
        }
    }

    private void SaveTree()
    {
        Debug.Log($"Guardando el árbol como: {tree.name}.asset");
        //AssetDatabase.CreateAsset(, $"Assets/{treeAssetName}.asset");
    }
}
