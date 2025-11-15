using UnityEngine;
using UnityEditor;
using System.CodeDom.Compiler;

[CustomEditor(typeof(UnitGenerator))]
public class UnitGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        UnitGenerator unitGenerator = (UnitGenerator)target;

        GUILayout.Space(10);

        if (GUILayout.Button("Generar Unidades"))
        {
            if(unitGenerator.map == null) Debug.LogError("Asigna un MapGenerator al UnitGenerator antes de generar unidades.");
            else unitGenerator.GenerateUnits();
        }
    }
}
