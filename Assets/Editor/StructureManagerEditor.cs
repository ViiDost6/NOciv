using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(StructureManager))]
public class StructureManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        
        StructureManager manager = (StructureManager)target;
        
        if (manager.mapGenerator != null)
        {
            GUILayout.Space(10);
            EditorGUILayout.LabelField("Información", EditorStyles.miniBoldLabel);
            int calculatedRadius = manager.CalculateCornerRadius();
            EditorGUILayout.LabelField($"Radio esquinas: {calculatedRadius} casillas");
            EditorGUILayout.LabelField($"Margen recursos: {manager.resourceBorderMargin} casillas");
        }
        
        GUILayout.Space(15);
        
        EditorGUILayout.HelpBox("Genera torres y recursos conectados por caminos transitables", MessageType.Info);
        
        if (GUILayout.Button("Generar Todas las Estructuras"))
        {
            manager.GenerateAllStructures();
        }
        
        if (GUILayout.Button("Limpiar Estructuras"))
        {
            manager.ClearStructures();
        }
    }
}