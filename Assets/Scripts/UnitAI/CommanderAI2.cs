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
        influenceMap = FindFirstObjectByType<InfluenceMap2>();
        structureManager = FindFirstObjectByType<StructureManager>();
        turnManager = FindFirstObjectByType<TurnManager>();
        
        // Inicializar mapas
        influenceMap.Initialize(FindFirstObjectByType<MapGenerator>(), structureManager);
    }
    
    // Llamado por TurnManager al inicio del turno
    public void PrepareTurn()
    {
        // 1. Calcular Mapa de Amenazas (Dónde está el jugador)
        List<Unit> playerUnits = turnManager.GetAllUnits(true); // true = jugador
        influenceMap.CalculateThreatMap(playerUnits);

        // 2. Decidir Estado Global
        DecideGlobalStrategy(playerUnits);

        // 3. Establecer Mapa de Deseos (A dónde ir)
        SetGoals();
    }

    void DecideGlobalStrategy(List<Unit> playerUnits)
    {
        // Lógica simple: Si tenemos menos unidades, defender. Si tenemos más, atacar.
        int myUnitsCount = turnManager.GetAllUnits(false).Count;
        int enemyUnitsCount = playerUnits.Count;

        if (myUnitsCount > enemyUnitsCount * 1.5f)
            currentGlobalOrder = SituationState.ATTACK;
        else if (myUnitsCount < enemyUnitsCount * 0.8f)
            currentGlobalOrder = SituationState.DEFENSE;
        else
            currentGlobalOrder = SituationState.EXPLORE; // O capturar recursos
            
        Debug.Log($"Commander Strategy: {currentGlobalOrder}");
    }

    void SetGoals()
    {
        List<Vector2Int> targets = new List<Vector2Int>();
        float priority = 10f;

        if (currentGlobalOrder == SituationState.ATTACK)
        {
            // Objetivos: Torres del jugador
            foreach(var pos in structureManager.PlayerTowerPositions)
            {
                targets.Add(pos);
            }
            priority = 20f;
        }
        else if (currentGlobalOrder == SituationState.DEFENSE)
        {
            // Objetivos: Mis propias torres (protegerlas)
            foreach(var pos in structureManager.EnemyTowerPositions) // EnemyTowerPositions son las de la IA
            {
                targets.Add(pos);
            }
            // Y recursos cercanos
            priority = 15f;
        }
        else // EXPLORE
        {
            // Objetivos: Recursos neutrales no capturados
            foreach(var pos in structureManager.ResourcePositions)
            {
                targets.Add(pos);
            }
            priority = 10f;
        }

        influenceMap.SetStrategicGoals(targets, priority);
    }
}