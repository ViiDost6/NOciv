using UnityEngine;
using System.Collections.Generic;

public class CommanderAI2 : MonoBehaviour
{
    // Estado actual de la FSM
    private CommanderState currentState;
    
    // Referencias públicas para que los Estados las usen (Contexto compartido)
    public StructureManager structureManager;
    public TurnManager turnManager;
    private InfluenceMap2 influenceMap;

    [Header("Debug Info")]
    [SerializeField] private string currentStateName; // Para ver en el inspector

    void Start()
    {
        influenceMap = FindObjectOfType<InfluenceMap2>();
        structureManager = FindObjectOfType<StructureManager>();
        turnManager = FindObjectOfType<TurnManager>();

        if (influenceMap != null)
            influenceMap.Initialize(FindObjectOfType<MapGenerator>(), structureManager);

        // Estado inicial por defecto
        ChangeState(new ExploreState(this));
    }

    // --- AQUÍ ESTÁ LA LÓGICA CENTRALIZADA ---
    public void PrepareTurn()
    {
        Debug.Log("--- Commander AI: Inicio de Turno (FSM) ---");

        // PASO 1: Tareas Comunes (Centralizadas)
        // No importa en qué estado estemos, SIEMPRE hay que hacer esto
        UpdateGlobalData();

        // PASO 2: Transiciones
        // Preguntamos al estado actual si quiere cambiar
        CommanderState nextState = currentState.CheckTransitions();
        if (nextState != null && nextState != currentState)
        {
            ChangeState(nextState);
        }

        // PASO 3: Ejecución Específica
        // Delegamos la decisión estratégica al estado
        currentState.UpdateStrategy();
    }

    private void UpdateGlobalData()
    {
        // 1. Escanear estructuras (si se destruyó alguna el turno anterior)
        structureManager.ScanStructuresInScene();

        // 2. Actualizar Mapa de Amenazas
        // Esto es caro computacionalmente, así que lo hacemos UNA vez aquí
        // y todos los estados se benefician del mapa ya calculado.
        List<Unit> playerUnits = turnManager.GetAllUnits(true); 
        influenceMap.CalculateThreatMap(playerUnits);
        
        Debug.Log("[Central] Mapas de Influencia y Estructuras actualizados.");
    }

    private void ChangeState(CommanderState newState)
    {
        if (currentState != null) currentState.Exit();
        
        currentState = newState;
        currentStateName = currentState.GetType().Name; // Update visual inspector
        
        currentState.Enter();
    }

    // --- Helpers para los Estados ---
    // Métodos públicos que simplifican la vida a las clases State
    
    public InfluenceMap2 GetInfluenceMap() => influenceMap;
    
    public int GetMyUnitCount() => turnManager.GetAllUnits(false).Count;
    
    public int GetEnemyUnitCount() => turnManager.GetAllUnits(true).Count;
}