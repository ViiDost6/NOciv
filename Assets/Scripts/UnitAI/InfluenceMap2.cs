using UnityEngine;
using System.Collections.Generic;

public class InfluenceMap2 : MonoBehaviour
{
    // Capa 0: Amenaza (Donde están los enemigos y su rango de ataque)
    // Capa 1: Aliado (Donde están los aliados)
    // Capa 2: Deseo (Donde el Comandante quiere que vayamos)
    private float[,] threatMap;
    private float[,] allyMap;
    private float[,] desireMap;
    
    private MapGenerator mapGenerator;
    private StructureManager structureManager;

    public void Initialize(MapGenerator mapGen, StructureManager structMan)
    {
        mapGenerator = mapGen;
        structureManager = structMan;
        threatMap = new float[mapGen.mapHeight, mapGen.mapWidth];
        allyMap = new float[mapGen.mapHeight, mapGen.mapWidth];
        desireMap = new float[mapGen.mapHeight, mapGen.mapWidth];
    }

    public void CalculateThreatMap(List<Unit> enemyUnits)
    {
        ClearMap(threatMap);
        
        foreach (var enemy in enemyUnits)
        {
            if (enemy == null) continue;
            
            // La amenaza es mayor en la posición del enemigo y decae con la distancia
            // Calculamos basado en su daño y rango
            float threatValue = enemy.damage * 1.5f; 
            AddInfluence(threatMap, enemy.currentTile.gridPosition, threatValue, enemy.attackRange + 1, 0.5f);
        }
    }

    public void CalculateAllyMap(List<Unit> allyUnits)
    {
        ClearMap(allyMap);
        
        foreach (var ally in allyUnits)
        {
            if (ally == null) continue;
            
            // La amenaza es mayor en la posición del enemigo y decae con la distancia
            // Calculamos basado en su daño y rango
            float threatValue = ally.damage * 1.5f; 
            AddInfluence(allyMap, ally.currentTile.gridPosition, threatValue, ally.attackRange + 1, 0.5f);
        }
    }

    // El Comandante usa esto para decir "QUIERO ESTA ZONA"
    public void SetStrategicGoals(List<Vector2Int> targets, float priority)
    {
        ClearMap(desireMap);
        foreach(var target in targets)
        {
            // Creamos un gradiente de atracción hacia el objetivo
            AddInfluence(desireMap, target, priority, 15, 0.8f);
        }
    }

    // El comandante 
    public void AddStrategicGoals(List<Vector2Int> targets, float priority)
    {
        
    }

    public float GetInfluenceAroundPoint(Vector2Int origin, int radius )
    {
        return 0;
    }

    public float GetTotalMapControlRatio()
    {

        return 0;
        
    }

    // Algoritmo de propagación de influencia (Flood fill ponderado)
    private void AddInfluence(float[,] map, Vector2Int center, float value, int range, float decay)
    {
        int h = map.GetLength(0);
        int w = map.GetLength(1);

        for (int x = -range; x <= range; x++)
        {
            for (int y = -range; y <= range; y++)
            {
                int r = center.x + x;
                int c = center.y + y;

                if (r >= 0 && r < h && c >= 0 && c < w)
                {
                    float dist = Mathf.Abs(x) + Mathf.Abs(y); // Distancia Manhattan
                    if (dist <= range)
                    {
                        float influence = value / (1 + (dist * decay));
                        map[r, c] += influence;
                    }
                }
            }
        }
    }

    public float GetThreatAt(Vector2Int pos) => IsInBounds(pos) ? threatMap[pos.x, pos.y] : 100f;
    public float GetDesireAt(Vector2Int pos) => IsInBounds(pos) ? desireMap[pos.x, pos.y] : -100f;

    // Fórmula mágica de navegación: Coste = Terreno + (Amenaza * Miedo) - (Deseo * Motivación)
    public float GetMoveCost(Vector2Int pos, float fearFactor, float motivationFactor)
    {
        if (!IsInBounds(pos)) return float.MaxValue;

        // Asumimos que MapGenerator puede dar el coste base del terreno
        TileData tile = mapGenerator.GetTileAtPosition(pos);
        float terrainCost = tile != null ? tile.weight : 1f;

        float threat = threatMap[pos.x, pos.y];
        float desire = desireMap[pos.x, pos.y];

        return terrainCost + (threat * fearFactor) - (desire * motivationFactor);
    }

    private void ClearMap(float[,] map)
    {
        System.Array.Clear(map, 0, map.Length);
    }

    private bool IsInBounds(Vector2Int pos)
    {
        return pos.x >= 0 && pos.x < threatMap.GetLength(0) && 
               pos.y >= 0 && pos.y < threatMap.GetLength(1);
    }

    // Debug visual para ver qué piensa la IA
    void OnDrawGizmosSelected()
    {
        if (desireMap == null) return;
        
        // Dibuja esferas: Rojas (Amenaza), Verdes (Deseo)
        for (int i = 0; i < desireMap.GetLength(0); i++)
        {
            for (int j = 0; j < desireMap.GetLength(1); j++)
            {
                if (desireMap[i, j] > 0.1f)
                {
                    Gizmos.color = new Color(0, 1, 0, 0.5f);
                    Gizmos.DrawSphere(new Vector3(j * 1.7f + (i%2)*0.85f, i * 0.5f, 0), desireMap[i,j] * 0.1f);
                }
                 if (threatMap[i, j] > 0.1f)
                {
                    Gizmos.color = new Color(1, 0, 0, 0.5f);
                    Gizmos.DrawSphere(new Vector3(j * 1.7f + (i%2)*0.85f, i * 0.5f, 0), threatMap[i,j] * 0.1f);
                }
            }
        }
    }
}