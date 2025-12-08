using UnityEngine;
using System.Collections.Generic;

public class CommanderAI2 : MonoBehaviour
{
    private CommanderState currentState;
    
    public StructureManager structureManager;
    public TurnManager turnManager;
    public UnitGenerator unitGenerator; 

    private InfluenceMap2 influenceMap;

    [Header("Debug Info")]
    [SerializeField] private string currentStateName;
    
    [Header("Strategic Metrics (0.0 - 1.0)")]
    [SerializeField] private float tension;          
    [SerializeField] private float vulnerability;    
    [SerializeField] private float dominance;        
    [SerializeField] private float economicSafety;   
    [SerializeField] private float enemyExposure; 

    public float Tension => tension;
    public float Vulnerability => vulnerability;
    public float Dominance => dominance;
    public float EconomicSafety => economicSafety;
    public float EnemyExposure => enemyExposure;

    private const int COST_INFANTRY = 50;
    private const int COST_HEAVY = 100;
    private const int COST_ARTILLERY = 150;

    void Start()
    {
        influenceMap = FindObjectOfType<InfluenceMap2>();
        structureManager = FindObjectOfType<StructureManager>();
        turnManager = FindObjectOfType<TurnManager>();
        unitGenerator = FindObjectOfType<UnitGenerator>();

        if (influenceMap != null)
            influenceMap.Initialize(FindObjectOfType<MapGenerator>(), structureManager);

        ChangeState(new ExploreState(this));
    }

    public void PrepareTurn()
    {
        Debug.Log("--- Commander AI: Inicio de Turno ---");
        UpdateGlobalData();
        ManageEconomyAndRecruit();

        CommanderState nextState = currentState.CheckTransitions();
        if (nextState != null && nextState.GetType() != currentState.GetType())
        {
            ChangeState(nextState);
        }
        currentState.UpdateStrategy();
    }

    // --- RECLUTAMIENTO ---
    private void ManageEconomyAndRecruit()
    {
        int budget = turnManager.aiResources;
        if (budget < COST_INFANTRY) return;

        Debug.Log($"[Commander Economy] Presupuesto: {budget}.");

        List<Vector2Int> towers = new List<Vector2Int>(structureManager.EnemyTowerPositions);
        Shuffle(towers);

        foreach (Vector2Int towerPos in towers)
        {
            Unit.UnitType desiredUnit = DecideUnitToBuy(budget);
            int unitCost = GetUnitCost(desiredUnit);

            if (budget < unitCost) 
            {
                Debug.Log($"[Commander Economy] Ahorrando para {desiredUnit}");
                break; 
            }

            Vector2Int spawnPos = FindValidSpawnPosition(towerPos);
            
            if (spawnPos != new Vector2Int(-1, -1))
            {
                unitGenerator.SpawnUnitAtPosition(spawnPos, false, desiredUnit);
                
                // --- NUEVO: Sonido de Spawn ---
                if(AudioManager.Instance != null)
                    AudioManager.Instance.PlaySFX(AudioManager.Instance.commanderSpawnUnit, 1.0f);
                
                turnManager.aiResources -= unitCost;
                budget -= unitCost;
                
                Debug.Log($"[Commander Economy] COMPRA: {desiredUnit}. Restante: {budget}");
            }
        }
        turnManager.aiResources = budget;
    }

    private Unit.UnitType DecideUnitToBuy(int currentBudget)
    {
        if (vulnerability > 0.3f || (tension > 0.6f && dominance < 0.4f))
            return Unit.UnitType.Infantry;

        if (economicSafety > 0.6f && dominance > 0.5f)
        {
            if (currentBudget >= COST_ARTILLERY) return Unit.UnitType.Artillery;
            return Unit.UnitType.HeavyInfantry;
        }

        float rnd = Random.value;
        if (rnd < 0.5f) return Unit.UnitType.Infantry;       
        if (rnd < 0.8f) return Unit.UnitType.HeavyInfantry;  
        return Unit.UnitType.Artillery;                      
    }

    private int GetUnitCost(Unit.UnitType type)
    {
        switch (type)
        {
            case Unit.UnitType.Infantry: return COST_INFANTRY;
            case Unit.UnitType.HeavyInfantry: return COST_HEAVY;
            case Unit.UnitType.Artillery: return COST_ARTILLERY;
            default: return COST_INFANTRY;
        }
    }

    private Vector2Int FindValidSpawnPosition(Vector2Int center)
    {
        MapGenerator mapGen = structureManager.mapGenerator;
        
        TileData centerTile = mapGen.GetTileAtPosition(center);
        if (IsTileFree(centerTile)) return center;

        if (centerTile != null)
        {
            List<TileData> neighbors = new List<TileData>(centerTile.neighbors);
            Shuffle(neighbors); 

            foreach (var neighbor in neighbors)
            {
                if (IsTileFree(neighbor)) return neighbor.gridPosition;
            }
        }
        return new Vector2Int(-1, -1); 
    }

    private bool IsTileFree(TileData tile)
    {
        return tile != null && tile.walkable && !tile.hasUnit;
    }

    private void Shuffle<T>(List<T> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int rnd = Random.Range(i, list.Count);
            T temp = list[i];
            list[i] = list[rnd];
            list[rnd] = temp;
        }
    }

    private void UpdateGlobalData()
    {
        structureManager.ScanStructuresInScene();

        List<Unit> enemyUnits = turnManager.GetAllUnits(true); 
        List<Unit> myUnits = turnManager.GetAllUnits(false);   

        influenceMap.CalculateThreatMap(enemyUnits);
        influenceMap.CalculateAllyMap(myUnits);
        
        tension = influenceMap.GetGlobalTension();
        
        float incomingThreat = influenceMap.GetMaxThreatInRadius(structureManager.EnemyTowerPositions, 6);
        vulnerability = Mathf.Clamp01(incomingThreat / 20.0f); 

        dominance = influenceMap.GetTerritorialDominance();

        float threatOnResources = influenceMap.GetAverageThreatAtPositions(structureManager.ResourcePositions);
        economicSafety = 1.0f - Mathf.Clamp01(threatOnResources / 10.0f);

        enemyExposure = influenceMap.GetEnemyExposure(structureManager.PlayerTowerPositions);

        Debug.Log($"[Intel] Tension:{tension:F2} | Vuln:{vulnerability:F2} | EcoSafe:{economicSafety:F2}");
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