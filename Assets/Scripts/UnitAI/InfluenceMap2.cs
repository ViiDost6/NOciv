using UnityEngine;
using System.Collections.Generic;

public class InfluenceMap2 : MonoBehaviour
{
    [Header("Debug Visualization")]
    public bool showGizmos = true;
    [Range(0f, 1f)] public float gizmoOpacity = 0.5f;
    [Range(0.1f, 2f)] public float gizmoScale = 1.0f;

    private float[,] threatMap;
    private float[,] allyMap;
    private float[,] desireMap;
    
    private MapGenerator mapGenerator;
    private StructureManager structureManager;

    private HashSet<Vector2Int> allStructurePositions = new HashSet<Vector2Int>();
    
    // Cache de enemigos para la defensa activa
    private List<Unit> cachedEnemyUnits = new List<Unit>();

    // Cache de órdenes
    private List<Vector2Int> cachedCommanderTargets;
    private float cachedPriority;
    private float cachedAllyBias;
    private float cachedEnemyBias;

    public void Initialize(MapGenerator mapGen, StructureManager structMan)
    {
        mapGenerator = mapGen;
        structureManager = structMan;
        int h = mapGen.mapHeight; int w = mapGen.mapWidth;
        threatMap = new float[h, w]; allyMap = new float[h, w]; desireMap = new float[h, w];
        RefreshStructureCache();
    }

    private void RefreshStructureCache()
    {
        allStructurePositions.Clear();
        if (structureManager == null) return;
        allStructurePositions.UnionWith(structureManager.PlayerTowerPositions);
        allStructurePositions.UnionWith(structureManager.EnemyTowerPositions);
        allStructurePositions.UnionWith(structureManager.ResourcePositions);
    }

    // --- CÁLCULO DE MAPAS ---

    public void CalculateThreatMap(List<Unit> enemyUnits)
    {
        cachedEnemyUnits = enemyUnits; // Guardamos referencia para la Defensa Activa
        ClearMap(threatMap);
        
        foreach (var enemy in enemyUnits)
        {
            if (enemy == null) continue;
            float threatValue = enemy.damage * 1.2f; 
            int effectiveRange = enemy.movesTotal + enemy.attackRange;
            AddInfluenceBFS(threatMap, enemy.currentTile.gridPosition, threatValue, effectiveRange);
        }
    }

    public void CalculateAllyMap(List<Unit> allyUnits)
    {
        ClearMap(allyMap);
        foreach (var ally in allyUnits)
        {
            if (ally == null) continue;
            float powerValue = ally.damage * 1.0f;
            int effectiveRange = ally.movesTotal + ally.attackRange;
            AddInfluenceBFS(allyMap, ally.currentTile.gridPosition, powerValue, effectiveRange);
        }
    }

    public void SetStrategicGoals(List<Vector2Int> commanderTargets, float priority, float allyBias = 1.0f, float enemyBias = 1.0f)
    {
        cachedCommanderTargets = commanderTargets;
        cachedPriority = priority;
        cachedAllyBias = allyBias;
        cachedEnemyBias = enemyBias;
        InternalCalculateDesireMap();
    }

    public void RefreshDesireMap() => InternalCalculateDesireMap();

    private void InternalCalculateDesireMap()
    {
        ClearMap(desireMap);
        int globalRange = Mathf.CeilToInt(Mathf.Sqrt(threatMap.GetLength(0)*threatMap.GetLength(0) + threatMap.GetLength(1)*threatMap.GetLength(1))) + 5;

        // 1. Órdenes Directas
        if(cachedCommanderTargets != null)
        {
            foreach(var target in cachedCommanderTargets)
            {
                if (IsTargetHostileOrNeutral(target))
                    AddInfluenceBFS(desireMap, target, cachedPriority, globalRange); 
            }
        }

        // 2. Lógica Ambiental y DEFENSA ACTIVA
        foreach(var pos in allStructurePositions)
        {
            Building b = GetBuildingAt(pos);
            if (b != null) EvaluateStructureInfluence(pos, b, cachedAllyBias, cachedEnemyBias);
        }
    }

    private void EvaluateStructureInfluence(Vector2Int pos, Building b, float allyBias, float enemyBias)
    {
        // A. ESTRUCTURAS ALIADAS (Team 2)
        if (b.hasBeenClaimed == 2) 
        {
            // Detectamos amenaza "potencial" sobre la base (gracias al threatMap predictivo)
            float localThreat = GetThreatAt(pos);
            
            if (localThreat > 0)
            {
                // --- DEFENSA ACTIVA (INTERCEPCIÓN) ---
                // En lugar de llamar a las tropas a la base, marcamos a los invasores.
                bool intrudersTagged = false;
                float interceptionRange = 10.0f; // Radio de radar de la base

                foreach(var enemy in cachedEnemyUnits)
                {
                    if (enemy == null) continue;
                    
                    float dist = Vector2Int.Distance(pos, enemy.currentTile.gridPosition);
                    
                    // Si el enemigo está cerca de la base amenazada...
                    if (dist <= interceptionRange)
                    {
                        // ...Generamos un deseo MUY ALTO sobre SU cabeza.
                        // "Mata a este tipo que amenaza mi base".
                        float eliminationPriority = (30.0f + localThreat) * allyBias;
                        
                        // Rango medio (8) para atraer unidades cercanas a interceptar
                        AddInfluenceBFS(desireMap, enemy.currentTile.gridPosition, eliminationPriority, 8);
                        intrudersTagged = true;
                    }
                }

                // Fallback: Si hay amenaza (mapa rojo) pero no encuentro la unidad (ej: error distancia),
                // entonces sí protejo la base como último recurso.
                if (!intrudersTagged)
                {
                    float defenseUrgency = (20.0f + localThreat) * allyBias;
                    AddInfluenceBFS(desireMap, pos, defenseUrgency, 10);
                }
            }
            else
            {
                // Sin amenaza: Patrulla nula para fomentar dispersión
                AddInfluenceBFS(desireMap, pos, 0.0f, 1);
            }
        }
        // B. ENEMIGO/NEUTRAL
        else 
        {
            float localDefense = GetThreatAt(pos); 
            float baseAttraction = 0f;
            int range = 0;

            if (b.isBase) { baseAttraction = 25.0f * enemyBias; range = 25; }
            else { 
                float neutralityBonus = (b.hasBeenClaimed == 0) ? 5.0f : 0f;
                baseAttraction = (15.0f + neutralityBonus) * enemyBias; 
                range = 18; 
            }
            
            float fearFactor = 1.0f / Mathf.Max(0.1f, enemyBias);
            float finalAttraction = baseAttraction - (localDefense * fearFactor);
            if (finalAttraction < 2.0f) finalAttraction = 2.0f;

            AddInfluenceBFS(desireMap, pos, finalAttraction, range);
        }
    }

    // ... (Resto de métodos Helpers, Análisis, BFS y Gizmos se mantienen igual) ...
    // ... COPIA EL RESTO DE MÉTODOS DE LA VERSIÓN ANTERIOR AQUÍ PARA COMPLETAR ...
    
    // Versión abreviada de los métodos estándar para que compile si copias-pegas:
    private Building GetBuildingAt(Vector2Int pos) {
        if (mapGenerator == null) return null;
        TileData tile = mapGenerator.GetTileAtPosition(pos);
        return tile != null ? tile.currentBuilding : null;
    }
    private bool IsTargetHostileOrNeutral(Vector2Int pos) {
        Building b = GetBuildingAt(pos);
        if (b != null && b.hasBeenClaimed == 2) return false;
        return true;
    }
    public float GetGlobalTension() { float t = 0; for(int x=0; x<threatMap.GetLength(0); x++) for(int y=0; y<threatMap.GetLength(1); y++) t += threatMap[x,y] * allyMap[x,y]; return Mathf.Clamp01(t/500f); }
    public float GetTerritorialDominance() { int m = 0; for(int x=0; x<threatMap.GetLength(0); x++) for(int y=0; y<threatMap.GetLength(1); y++) if(allyMap[x,y] > threatMap[x,y]) m++; return (float)m / threatMap.Length; }
    public float GetAverageThreatAtPositions(List<Vector2Int> p) { float t = 0; foreach(var pos in p) if(IsInBounds(pos)) t += threatMap[pos.x, pos.y]; return p.Count > 0 ? t / p.Count : 0; }
    public float GetEnemyExposure(List<Vector2Int> enemyBases) { float def = 0; foreach(var pos in enemyBases) if(IsInBounds(pos)) def += threatMap[pos.x, pos.y]; return enemyBases.Count > 0 ? 1.0f - Mathf.Clamp01((def/enemyBases.Count)/8.0f) : 1.0f; }
    public List<Vector2Int> GetWeakestTargets(List<Vector2Int> pts, float thresh) { List<Vector2Int> w = new List<Vector2Int>(); foreach(var p in pts) if(IsTargetHostileOrNeutral(p) && GetThreatAt(p) <= thresh) w.Add(p); return w; }
    private void AddInfluenceBFS(float[,] map, Vector2Int center, float value, int range) {
        if (!IsInBounds(center)) return;
        Queue<Vector2Int> q = new Queue<Vector2Int>(); Dictionary<Vector2Int, int> dists = new Dictionary<Vector2Int, int>(); q.Enqueue(center); dists[center] = 0; map[center.x, center.y] += value;
        while (q.Count > 0) {
            Vector2Int curr = q.Dequeue(); int d = dists[curr]; if (d >= range) continue;
            foreach (Vector2Int n in GetHexNeighbors(curr)) {
                if (!IsInBounds(n) || dists.ContainsKey(n)) continue; dists[n] = d + 1; q.Enqueue(n);
                float percent = 1f - ((float)(d+1) / range); float influence = value * percent; 
                if (influence > 0.01f) map[n.x, n.y] += influence;
            }
        }
    }
    private List<Vector2Int> GetHexNeighbors(Vector2Int pos) {
        int r = pos.x; int c = pos.y; int p = r & 1;
        return new List<Vector2Int> { new Vector2Int(r+2, c), new Vector2Int(r-2, c), new Vector2Int(r+1, c+p), new Vector2Int(r-1, c+p), new Vector2Int(r+1, c-(1-p)), new Vector2Int(r-1, c-(1-p)) };
    }
    public float GetThreatAt(Vector2Int pos) => IsInBounds(pos) ? threatMap[pos.x, pos.y] : 0f;
    public float GetDesireAt(Vector2Int pos) => IsInBounds(pos) ? desireMap[pos.x, pos.y] : 0f;
    public float GetAllyPowerAt(Vector2Int pos) => IsInBounds(pos) ? allyMap[pos.x, pos.y] : 0f;
    public Vector2Int GetNearestHighDesirePoint(Vector2Int origin) {
        Vector2Int best = origin; float minDist = float.MaxValue; bool found = false;
        int h = desireMap.GetLength(0); int w = desireMap.GetLength(1);
        for(int x=0; x<h; x++) for(int y=0; y<w; y++) if(desireMap[x,y] > 2.0f) { float d = Mathf.Abs(x-origin.x)+Mathf.Abs(y-origin.y); if(d < minDist) { minDist = d; best = new Vector2Int(x,y); found = true; } }
        if(!found) { foreach(var pos in allStructurePositions) { if(IsTargetHostileOrNeutral(pos)) { float d = Mathf.Abs(pos.x-origin.x)+Mathf.Abs(pos.y-origin.y); if(d < minDist) { minDist = d; best = pos; }}}}
        return best;
    }
    private void ClearMap(float[,] map) { System.Array.Clear(map, 0, map.Length); }
    private bool IsInBounds(Vector2Int pos) => pos.x >= 0 && pos.x < threatMap.GetLength(0) && pos.y >= 0 && pos.y < threatMap.GetLength(1);
#if UNITY_EDITOR
    void OnDrawGizmos() {
        if (!showGizmos || threatMap == null) return;
        int h = threatMap.GetLength(0); int w = threatMap.GetLength(1);
        for (int i = 0; i < h; i++) { for (int j = 0; j < w; j++) {
            float xPos = j * 1.7f; float yPos = i * 0.5f; if (i % 2 == 1) xPos += 0.85f;
            Vector3 pos = new Vector3(xPos, yPos, 0);
            float ally = allyMap[i, j]; float threat = threatMap[i, j]; float desire = desireMap[i, j];
            if (ally > 0.1f) { Gizmos.color = new Color(0, 0.5f, 1f, 0.3f); Gizmos.DrawCube(pos + Vector3.forward * 0.1f, new Vector3(1f, 0.8f, 0.1f) * gizmoScale); }
            if (threat > 0.1f) { Gizmos.color = new Color(1f, 0f, 0f, 0.3f); Gizmos.DrawSphere(pos + Vector3.back * 0.5f, 0.3f * gizmoScale); }
            if (desire > 0.1f) { float height = Mathf.Clamp(desire * 0.1f, 0.2f, 3.0f); Gizmos.color = Color.Lerp(Color.yellow, Color.green, Mathf.Clamp01(desire/20f)); Vector3 top = pos + Vector3.back * height; Gizmos.DrawLine(pos, top); Gizmos.DrawCube(top, Vector3.one * 0.2f * gizmoScale); }
        }}
    }
#endif
}