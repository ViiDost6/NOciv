using UnityEngine;
using System.Collections.Generic;

public class CommanderAI2 : MonoBehaviour
{
    private CommanderState currentState;
    
    public StructureManager structureManager;
    public TurnManager turnManager;
    private InfluenceMap2 influenceMap;

    [Header("Debug Info")]
    [SerializeField] private string currentStateName;
    
    // --- MÉTRICAS DE INTELIGENCIA ---
    [Header("Strategic Metrics (0.0 - 1.0)")]
    [SerializeField] private float tension;          
    [SerializeField] private float vulnerability;    
    [SerializeField] private float dominance;        
    [SerializeField] private float economicSafety;   
    [SerializeField] private float enemyExposure; // NUEVO: ¿Qué tan desprotegido está el rival?

    // Getters
    public float Tension => tension;
    public float Vulnerability => vulnerability;
    public float Dominance => dominance;
    public float EconomicSafety => economicSafety;
    public float EnemyExposure => enemyExposure;

    void Start()
    {
        influenceMap = FindObjectOfType<InfluenceMap2>();
        structureManager = FindObjectOfType<StructureManager>();
        turnManager = FindObjectOfType<TurnManager>();

        if (influenceMap != null)
            influenceMap.Initialize(FindObjectOfType<MapGenerator>(), structureManager);

        ChangeState(new ExploreState(this));
    }

    public void PrepareTurn()
    {
        Debug.Log("--- Commander AI: Inicio de Turno (Análisis Estratégico) ---");

        UpdateGlobalData();

        CommanderState nextState = currentState.CheckTransitions();
        if (nextState != null && nextState.GetType() != currentState.GetType())
        {
            ChangeState(nextState);
        }

        currentState.UpdateStrategy();
    }

    private void UpdateGlobalData()
    {
        structureManager.ScanStructuresInScene();

        List<Unit> enemyUnits = turnManager.GetAllUnits(true); 
        List<Unit> myUnits = turnManager.GetAllUnits(false);   

        influenceMap.CalculateThreatMap(enemyUnits);
        influenceMap.CalculateAllyMap(myUnits);

        // --- CÁLCULO DE MÉTRICAS ---
        
        tension = influenceMap.GetGlobalTension();

        float threatOnBases = influenceMap.GetAverageThreatAtPositions(structureManager.EnemyTowerPositions);
        vulnerability = Mathf.Clamp01(threatOnBases / 5.0f);

        dominance = influenceMap.GetTerritorialDominance();

        float threatOnResources = influenceMap.GetAverageThreatAtPositions(structureManager.ResourcePositions);
        economicSafety = 1.0f - Mathf.Clamp01(threatOnResources / 3.0f);

        // NUEVO: Analizar bases del jugador humano para ver si están solas
        enemyExposure = influenceMap.GetEnemyExposure(structureManager.PlayerTowerPositions);

        Debug.Log($"[Intel] Tension:{tension:F1} | Vuln:{vulnerability:F1} | Exp:{enemyExposure:F1} | Dom:{dominance:F1}");
    }

    private void ChangeState(CommanderState newState)
    {
        if (currentState != null) currentState.Exit();
        currentState = newState;
        currentStateName = currentState.GetType().Name;
        currentState.Enter();
    }

    public InfluenceMap2 GetInfluenceMap() => influenceMap;
    public int GetMyUnitCount() => turnManager.GetAllUnits(false).Count;
    public int GetEnemyUnitCount() => turnManager.GetAllUnits(true).Count;
}