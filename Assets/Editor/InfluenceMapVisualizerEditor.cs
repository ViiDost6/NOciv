using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(InfluenceMapVisualizer))]
public class InfluenceMapVisualizerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        
        InfluenceMapVisualizer visualizer = (InfluenceMapVisualizer)target;
        
        GUILayout.Space(10);
        
        if (GUILayout.Button("Generar Visualización"))
        {
            visualizer.GenerateVisualization();
        }
        
        if (GUILayout.Button("Actualizar Colores"))
        {
            visualizer.UpdateVisualization();
        }
        
        if (GUILayout.Button("Limpiar Visualización"))
        {
            visualizer.ClearVisualization();
        }
        
        GUILayout.Space(10);
        EditorGUILayout.HelpBox("Asigna un prefab hexágono en 'Influence Tile Prefab'", MessageType.Info);
        EditorGUILayout.LabelField("Leyenda:", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("🔵 Azul: Influencia negativa (evitar)");
        EditorGUILayout.LabelField("⚪ Blanco: Influencia neutra");
        EditorGUILayout.LabelField("🔴 Rojo: Influencia positiva (buscar)");
    }
}