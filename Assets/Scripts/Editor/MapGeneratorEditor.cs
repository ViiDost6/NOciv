using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(MapGenerator))]
public class MapGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        MapGenerator generator = (MapGenerator)target;
        GUILayout.Space(10);
        if (GUILayout.Button("Generar Mapa"))
        {
            generator.GenerateMap();
        }
    }
}