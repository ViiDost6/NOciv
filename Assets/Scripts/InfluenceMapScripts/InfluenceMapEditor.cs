using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(InfluenceMap))]
public class InfluenceMapEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        
        InfluenceMap influenceMap = (InfluenceMap)target;
        
        GUILayout.Space(15);
        
        if (GUILayout.Button("Generar Mapa de Influencias"))
        {
            influenceMap.GenerateInfluenceMap();
        }
        
        if (GUILayout.Button("Debug: Mostar Valores"))
        {
            DebugInfluenceValues();
        }
    }
    
    void DebugInfluenceValues()
    {
        InfluenceMap influenceMap = (InfluenceMap)target;
        // Mostrar valores de influencia en posiciones clave
    }
}