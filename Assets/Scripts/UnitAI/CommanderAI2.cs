using UnityEngine;
using System.Collections.Generic;

public class CommanderAI2 : MonoBehaviour
{
    public enum SituationState { ATTACK, DEFENSE, EXPLORE }
    
    [Header("AI Settings")]
    public SituationState currentGlobalOrder;
    
    private InfluenceMap2 influenceMap;
    private StructureManager structureManager;
    private TurnManager turnManager;

    void Start()
    {
        influenceMap = FindObjectOfType<InfluenceMap2>();
        structureManager = FindObjectOfType<StructureManager>();
        turnManager = FindObjectOfType<TurnManager>();
        
        // Nos aseguramos de que el mapa de influencia tenga referencias
        if(influenceMap != null)
        {
            influenceMap.Initialize(FindObjectOfType<MapGenerator>(), structureManager);
        }
    }
    
    public void PrepareTurn()
    {
        Debug.Log("--- Commander AI: Preparando Turno ---");

        if (structureManager == null)
        {
            Debug.LogError("CommanderAI: CRÍTICO - StructureManager no encontrado.");
            return;
        }

        // 0. Asegurarnos de que el StructureManager tiene datos actualizados
        structureManager.ScanStructuresInScene();

        // 1. Calcular Mapa de Amenazas
        List<Unit> playerUnits = turnManager.GetAllUnits(true); 
        influenceMap.CalculateThreatMap(playerUnits);

        // 2. Decidir Estrategia
        DecideGlobalStrategy(playerUnits);

        // 3. Establecer Mapa de Deseos
        SetGoals();
    }

    void DecideGlobalStrategy(List<Unit> playerUnits)
    {
        int myUnitsCount = turnManager.GetAllUnits(false).Count;
        int enemyUnitsCount = playerUnits.Count;
        
        Debug.Log($"Commander Stats: Mis Unidades ({myUnitsCount}) vs Enemigos ({enemyUnitsCount})");

        if (myUnitsCount > enemyUnitsCount * 1.2f) // Un poco más agresivo
            currentGlobalOrder = SituationState.ATTACK;
        else if (myUnitsCount < enemyUnitsCount * 0.8f)
            currentGlobalOrder = SituationState.DEFENSE;
        else
            currentGlobalOrder = SituationState.EXPLORE;
            
        Debug.Log($"Commander Orden Global: <color=yellow>{currentGlobalOrder}</color>");
    }

    void SetGoals()
    {
        List<Vector2Int> targets = new List<Vector2Int>();
        float priority = 10f;
        string debugTargetType = "";

        if (currentGlobalOrder == SituationState.ATTACK)
        {
            // Atacar Torres del Jugador
            debugTargetType = "Torres Jugador";
            foreach(var pos in structureManager.PlayerTowerPositions)
            {
                targets.Add(pos);
            }
            priority = 20f;
        }
        else if (currentGlobalOrder == SituationState.DEFENSE)
        {
            // Defender Mis Torres
            debugTargetType = "Mis Torres (Enemigas)";
            foreach(var pos in structureManager.EnemyTowerPositions) 
            {
                targets.Add(pos);
            }
            priority = 15f;
        }
        else 
        {
            // Explorar Recursos
            debugTargetType = "Recursos";
            foreach(var pos in structureManager.ResourcePositions)
            {
                targets.Add(pos);
            }
            priority = 10f;
        }

        Debug.Log($"Commander: Estableciendo objetivos. Tipo: {debugTargetType}. Cantidad encontrada: {targets.Count}");

        if (targets.Count == 0)
        {
            Debug.LogWarning("CommanderAI: ¡No se encontraron objetivos para la orden actual! Revisa StructureManager.");
        }

        influenceMap.SetStrategicGoals(targets, priority);
    }
}