using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class MatchInitializer : MonoBehaviour
{
    [Header("Dependencies")]
    public MapGenerator mapGenerator;
    public StructureManager structureManager;
    public UnitGenerator unitGenerator;
    public TurnManager turnManager;

    [Header("Game Settings")]
    public bool generateOnStart = true;

    void Start()
    {
        if (generateOnStart)
        {
            StartCoroutine(InitializeMatchRoutine());
        }
    }

    // Usamos corrutina para asegurar que Unity procese cada paso (útil si hay operaciones pesadas)
    private IEnumerator InitializeMatchRoutine()
    {
        Debug.Log("--- INICIANDO GENERACIÓN DE PARTIDA ---");

        // 1. Generar Mapa
        if (mapGenerator != null)
        {
            mapGenerator.GenerateMap();
            Debug.Log("1. Mapa Generado.");
        }
        yield return null;

        // 2. Generar Estructuras
        if (structureManager != null)
        {
            structureManager.GenerateAllStructures();
            Debug.Log($"2. Estructuras Generadas.");
        }
        yield return null;

        // 3. Generar Unidades
        if (unitGenerator != null && structureManager != null)
        {
            unitGenerator.ClearUnits();
            SpawnSquadInTowers(structureManager.PlayerTowerPositions, true);
            SpawnSquadInTowers(structureManager.EnemyTowerPositions, false);
            Debug.Log("3. Unidades Generadas.");
        }
        yield return null;

        // 4. Inicializar Turnos (y recuento de bases)
        if (turnManager != null)
        {
            turnManager.turno = 1;
            turnManager.IniciarTurno(); // Ahora llama al recuento de bases internas
            Debug.Log("4. Partida Comenzada.");
        }
    }

    private void SpawnSquadInTowers(List<Vector2Int> towerPositions, bool isPlayer)
    {
        Unit.UnitType[] squadTemplate = { 
            Unit.UnitType.Infantry, 
            Unit.UnitType.HeavyInfantry, 
            Unit.UnitType.Artillery 
        };

        int squadIndex = 0;

        foreach (var pos in towerPositions)
        {
            Unit.UnitType typeToSpawn = squadTemplate[squadIndex % squadTemplate.Length];
            unitGenerator.SpawnUnitAtPosition(pos, isPlayer, typeToSpawn);
            squadIndex++;
        }
    }
}