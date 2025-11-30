using UnityEngine;
using System.Collections.Generic;

public class CommanderAI : MonoBehaviour
{
    public enum SituationState { ATTACK, DEFENSE, EXPLORE }
    
    [Header("AI Settings")]
    public float defenseThreshold = -3.0f;  // Nivel de influencia para defensa
    public float attackThreshold = 4.0f;    // Nivel de influencia para ataque
    public float resourceSafetyMargin = 2.0f;
    
    [Header("Unit Generation")]
    public int unitGenerationCost = 5;
    public int currentResources = 10;
    
    // Referencias a sistemas
    private InfluenceMap influenceMap;
    private StructureManager structureManager;
    
    // Estado actual de la IA
    private SituationState currentGlobalOrder;
    private float overallThreatLevel;
    private float overallOpportunityLevel;
    
    void Start()
    {
        influenceMap = FindObjectOfType<InfluenceMap>();
        structureManager = FindObjectOfType<StructureManager>();
        
        if (influenceMap == null)
            Debug.LogError("CommanderAI: No se encontró InfluenceMap en la escena");
        if (structureManager == null)
            Debug.LogError("CommanderAI: No se encontró StructureManager en la escena");
    }
    
    public void StartAITurn()
    {
        Debug.Log("=== INICIANDO TURNO DE IA COMANDANTE ===");
        
        // 1. Recibir y analizar el mapa de influencias
        AnalyzeGlobalSituation();
        
        // 2. Analizar situación de peligro y recursos
        AssessStrategicSituation();
        
        // 3. Generar nuevas unidades si es posible
        GenerateUnitsIfPossible();
        
        // 4. Determinar y enviar la orden global
        DetermineGlobalOrder();
        
        Debug.Log($"=== ORDEN GLOBAL: {currentGlobalOrder} ===");
    }
    
    void AnalyzeGlobalSituation()
    {
        if (influenceMap == null) return;
        
        // Calcular nivel de amenaza general alrededor de nuestras bases
        CalculateOverallThreatLevel();
        
        // Calcular nivel de oportunidad general alrededor de bases enemigas
        CalculateOverallOpportunityLevel();
        
        Debug.Log($"Análisis global - Amenaza: {overallThreatLevel:F2}, Oportunidad: {overallOpportunityLevel:F2}");
    }
    
    void CalculateOverallThreatLevel()
    {
        overallThreatLevel = 0f;
        int sampleCount = 0;
        
        // Analizar influencia alrededor de todas nuestras bases
        foreach (Vector2Int basePos in structureManager.PlayerTowerPositions)
        {
            // Influencia directa en la base
            float baseInfluence = influenceMap.GetInfluenceAt(basePos);
            overallThreatLevel += baseInfluence;
            sampleCount++;
            
            // Influencia en área circundante (radio 3)
            List<Vector2Int> surrounding = GetSurroundingPositions(basePos, 3);
            foreach (Vector2Int pos in surrounding)
            {
                float influence = influenceMap.GetInfluenceAt(pos);
                overallThreatLevel += influence;
                sampleCount++;
            }
        }
        
        if (sampleCount > 0)
            overallThreatLevel /= sampleCount;
    }
    
    void CalculateOverallOpportunityLevel()
    {
        overallOpportunityLevel = 0f;
        int sampleCount = 0;
        
        // Analizar influencia alrededor de bases enemigas
        foreach (Vector2Int enemyBase in structureManager.EnemyTowerPositions)
        {
            // Influencia directa en base enemiga
            float baseInfluence = influenceMap.GetInfluenceAt(enemyBase);
            overallOpportunityLevel += baseInfluence;
            sampleCount++;
            
            // Influencia en área circundante
            List<Vector2Int> surrounding = GetSurroundingPositions(enemyBase, 2);
            foreach (Vector2Int pos in surrounding)
            {
                float influence = influenceMap.GetInfluenceAt(pos);
                overallOpportunityLevel += influence;
                sampleCount++;
            }
        }
        
        if (sampleCount > 0)
            overallOpportunityLevel /= sampleCount;
    }
    
    void AssessStrategicSituation()
    {
        // Evaluar si tenemos recursos para generar unidades
        bool canGenerateUnits = currentResources >= unitGenerationCost;
        
        Debug.Log($"Situación estratégica - Recursos: {currentResources}, Puede generar: {canGenerateUnits}");
    }
    
    void GenerateUnitsIfPossible()
    {
        if (currentResources < unitGenerationCost)
        {
            Debug.Log($"Recursos insuficientes para generar unidades: {currentResources}/{unitGenerationCost}");
            return;
        }
        
        // Encontrar la base más segura para generar unidades
        Vector2Int? safestBase = GetSafestBase();
        
        if (safestBase.HasValue)
        {
            GenerateUnitAtBase(safestBase.Value);
            currentResources -= unitGenerationCost;
            Debug.Log($"Unidad generada en base {safestBase.Value}. Recursos restantes: {currentResources}");
        }
        else
        {
            Debug.Log("No se encontró base segura para generar unidades");
        }
    }
    
    Vector2Int? GetSafestBase()
    {
        Vector2Int? safestBase = null;
        float highestSafety = float.MinValue;
        
        foreach (Vector2Int basePos in structureManager.PlayerTowerPositions)
        {
            float safetyScore = CalculateBaseSafety(basePos);
            
            if (safetyScore > highestSafety)
            {
                highestSafety = safetyScore;
                safestBase = basePos;
            }
        }
        
        return safestBase;
    }
    
    float CalculateBaseSafety(Vector2Int basePos)
    {
        float safety = 0f;
        int sampleCount = 0;
        
        // Evaluar influencia en y alrededor de la base
        List<Vector2Int> checkArea = GetSurroundingPositions(basePos, 2);
        checkArea.Add(basePos); // Incluir la base misma
        
        foreach (Vector2Int pos in checkArea)
        {
            float influence = influenceMap.GetInfluenceAt(pos);
            safety += influence;
            sampleCount++;
        }
        
        return sampleCount > 0 ? safety / sampleCount : 0f;
    }
    
    void GenerateUnitAtBase(Vector2Int basePosition)
    {
        // Lógica para generar una unidad en la base especificada
        Debug.Log($"Generando unidad en base {basePosition}");
        // GameObject newUnit = Instantiate(unitPrefab, GetWorldPosition(basePosition), Quaternion.identity);
    }
    
    void DetermineGlobalOrder()
    {
        // Lógica de decisión basada en el análisis global
        if (overallThreatLevel < defenseThreshold)
        {
            // Amenaza alta - modo DEFENSA
            currentGlobalOrder = SituationState.DEFENSE;
            Debug.Log("Orden: DEFENSA - Amenaza alta detectada");
        }
        else if (overallOpportunityLevel > attackThreshold && currentResources >= unitGenerationCost)
        {
            // Oportunidad de ataque y recursos suficientes - modo ATAQUE
            currentGlobalOrder = SituationState.ATTACK;
            Debug.Log("Orden: ATAQUE - Oportunidad favorable detectada");
        }
        else
        {
            // Situación neutral - modo EXPLORACIÓN
            currentGlobalOrder = SituationState.EXPLORE;
            Debug.Log("Orden: EXPLORACIÓN - Situación neutral");
        }
        
        // Enviar la orden global a todas las unidades
        BroadcastGlobalOrder(currentGlobalOrder);
    }
    
    void BroadcastGlobalOrder(SituationState order)
    {
        // Aquí se enviaría la orden a todas las unidades del jugador
        Debug.Log($"=== COMANDANTE ORDENA: {order} A TODAS LAS UNIDADES ===");
        
        // Ejemplo de implementación:
        // foreach (GameObject unit in GetAllPlayerUnits())
        // {
        //     UnitAI unitAI = unit.GetComponent<UnitAI>();
        //     if (unitAI != null)
        //     {
        //         unitAI.ReceiveGlobalOrder(order);
        //     }
        // }
    }
    
    // Métodos auxiliares
    List<Vector2Int> GetSurroundingPositions(Vector2Int center, int radius)
    {
        List<Vector2Int> positions = new List<Vector2Int>();
        
        for (int x = -radius; x <= radius; x++)
        {
            for (int y = -radius; y <= radius; y++)
            {
                Vector2Int pos = new Vector2Int(center.x + x, center.y + y);
                
                // Verificar límites del mapa
                if (pos.x >= 0 && pos.x < GetMapHeight() && 
                    pos.y >= 0 && pos.y < GetMapWidth())
                {
                    positions.Add(pos);
                }
            }
        }
        
        return positions;
    }
    
    int GetMapHeight()
    {
        return structureManager != null ? structureManager.mapGenerator.mapHeight : 10;
    }
    
    int GetMapWidth()
    {
        return structureManager != null ? structureManager.mapGenerator.mapWidth : 10;
    }
    
    // Método público para obtener la orden actual (para debugging)
    public SituationState GetCurrentGlobalOrder()
    {
        return currentGlobalOrder;
    }
    
    // Método público para obtener métricas de la IA (para debugging)
    public void GetAIMetrics(out float threat, out float opportunity)
    {
        threat = overallThreatLevel;
        opportunity = overallOpportunityLevel;
    }
}