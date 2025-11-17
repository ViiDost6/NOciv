using UnityEngine;
using System.Collections.Generic;

public class InfluenceMapVisualizer : MonoBehaviour
{
    public InfluenceMap influenceMap;
    public GameObject influenceTilePrefab;
    
    private List<GameObject> visualTiles = new List<GameObject>();

    public void GenerateVisualization()
    {
        Debug.Log("INFLUENCE VISUALIZER: Starting influence map visualization");
        
        ClearVisualization();
        
        if (influenceMap == null)
        {
            Debug.LogError("INFLUENCE VISUALIZER: InfluenceMap is null");
            return;
        }
        
        if (influenceTilePrefab == null)
        {
            Debug.LogError("INFLUENCE VISUALIZER: InfluenceTilePrefab is null");
            return;
        }
        
        MapGenerator mapGenerator = influenceMap.mapGenerator;
        if (mapGenerator == null)
        {
            Debug.LogError("INFLUENCE VISUALIZER: MapGenerator is null");
            return;
        }

        // Primero, encontrar el rango de influencias para normalizar
        float minInfluence = float.MaxValue;
        float maxInfluence = float.MinValue;
        
        for (int y = 0; y < mapGenerator.mapHeight; y++)
        {
            for (int x = 0; x < mapGenerator.mapWidth; x++)
            {
                float influence = influenceMap.GetInfluenceAt(new Vector2Int(x, y));
                minInfluence = Mathf.Min(minInfluence, influence);
                maxInfluence = Mathf.Max(maxInfluence, influence);
            }
        }
        
        Debug.Log($"INFLUENCE VISUALIZER: Influence range: {minInfluence:F2} to {maxInfluence:F2}");

        Dictionary<Vector2Int, TileData> tileGrid = mapGenerator.GetTileGrid();
        
        if (tileGrid == null)
        {
            Debug.LogError("INFLUENCE VISUALIZER: tileGrid is null");
            return;
        }
        
        Debug.Log("INFLUENCE VISUALIZER: Generating heatmap for " + tileGrid.Count + " tiles");
        
        int tilesCreated = 0;
        
        foreach (var kvp in tileGrid)
        {
            Vector2Int gridPos = kvp.Key;
            TileData originalTile = kvp.Value;
            
            if (originalTile == null) continue;
            
            CreateHeatmapTile(gridPos, originalTile.transform.position, minInfluence, maxInfluence);
            tilesCreated++;
        }
        
        Debug.Log("INFLUENCE VISUALIZER: Heatmap completed: " + tilesCreated + " tiles created");
    }
    
    void CreateHeatmapTile(Vector2Int gridPos, Vector3 worldPosition, float minInfluence, float maxInfluence)
    {
        // Instanciar el prefab
        GameObject visualTile = Instantiate(influenceTilePrefab);
        visualTile.name = "Heatmap_" + gridPos.x + "_" + gridPos.y;
        
        // Posicionar en la misma ubicación que el tile original
        Vector3 visualPos = new Vector3(worldPosition.x, worldPosition.y, worldPosition.z - 0.1f);
        visualTile.transform.position = visualPos;
        visualTile.transform.SetParent(transform);
        
        // Aplicar color de mapa de calor
        ApplyHeatmapColor(visualTile, gridPos, minInfluence, maxInfluence);
        
        visualTiles.Add(visualTile);
    }
    
    void ApplyHeatmapColor(GameObject visualTile, Vector2Int gridPos, float minInfluence, float maxInfluence)
    {
        float influence = influenceMap.GetInfluenceAt(gridPos);
        Color color = GetHeatmapColor(influence, minInfluence, maxInfluence);
        
        // Aplicar color al SpriteRenderer si existe
        SpriteRenderer spriteRenderer = visualTile.GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            spriteRenderer.color = color;
        }
        else
        {
            // Si no tiene SpriteRenderer, intentar con el Renderer normal
            Renderer renderer = visualTile.GetComponent<Renderer>();
            if (renderer != null)
            {
                Material newMaterial = new Material(renderer.sharedMaterial);
                newMaterial.color = color;
                renderer.sharedMaterial = newMaterial;
            }
        }
    }
    
    Color GetHeatmapColor(float influence, float minInfluence, float maxInfluence)
    {
        // Normalizar la influencia al rango [0, 1]
        float normalizedInfluence = 0f;
        
        if (Mathf.Abs(maxInfluence - minInfluence) > 0.001f)
        {
            normalizedInfluence = Mathf.InverseLerp(minInfluence, maxInfluence, influence);
        }
        
        // Mapa de calor INVERTIDO: Rojo (bajo/negativo) -> Amarillo (medio) -> Verde (alto/positivo)
        Color color;
        
        if (normalizedInfluence < 0.5f)
        {
            // Rojo a Amarillo: 0.0-0.5
            float t = normalizedInfluence * 2f; // Convertir a [0,1] dentro de este segmento
            color = Color.Lerp(
                new Color(1f, 0f, 0f, 0.8f),     // Rojo (influencia negativa)
                new Color(1f, 1f, 0f, 0.8f),     // Amarillo (neutral)
                t
            );
        }
        else
        {
            // Amarillo a Verde: 0.5-1.0
            float t = (normalizedInfluence - 0.5f) * 2f; // Convertir a [0,1] dentro de este segmento
            color = Color.Lerp(
                new Color(1f, 1f, 0f, 0.8f),     // Amarillo (neutral)
                new Color(0f, 1f, 0f, 0.8f),     // Verde (influencia positiva)
                t
            );
        }
        
        return color;
    }
    
    // Versión alternativa con más colores (opcional)
    Color GetHeatmapColorAdvanced(float influence, float minInfluence, float maxInfluence)
    {
        // Normalizar la influencia al rango [0, 1]
        float normalizedInfluence = 0f;
        
        if (Mathf.Abs(maxInfluence - minInfluence) > 0.001f)
        {
            normalizedInfluence = Mathf.InverseLerp(minInfluence, maxInfluence, influence);
        }
        
        // Mapa de calor invertido con más transiciones:
        // Rojo -> Naranja-rojo -> Naranja -> Amarillo -> Verde
        if (normalizedInfluence < 0.25f)
        {
            // Rojo oscuro a Naranja-rojo
            float t = normalizedInfluence * 4f;
            return Color.Lerp(
                new Color(0.8f, 0f, 0f, 0.8f),     // Rojo oscuro (muy negativo)
                new Color(1f, 0.3f, 0f, 0.8f),     // Naranja-rojo
                t
            );
        }
        else if (normalizedInfluence < 0.5f)
        {
            // Naranja-rojo a Naranja
            float t = (normalizedInfluence - 0.25f) * 4f;
            return Color.Lerp(
                new Color(1f, 0.3f, 0f, 0.8f),     // Naranja-rojo
                new Color(1f, 0.6f, 0f, 0.8f),     // Naranja
                t
            );
        }
        else if (normalizedInfluence < 0.75f)
        {
            // Naranja a Amarillo
            float t = (normalizedInfluence - 0.5f) * 4f;
            return Color.Lerp(
                new Color(1f, 0.6f, 0f, 0.8f),     // Naranja
                new Color(1f, 1f, 0f, 0.8f),       // Amarillo
                t
            );
        }
        else
        {
            // Amarillo a Verde
            float t = (normalizedInfluence - 0.75f) * 4f;
            return Color.Lerp(
                new Color(1f, 1f, 0f, 0.8f),       // Amarillo
                new Color(0f, 0.8f, 0f, 0.8f),     // Verde (muy positivo)
                t
            );
        }
    }
    
    public void UpdateVisualization()
    {
        Debug.Log("INFLUENCE VISUALIZER: Updating heatmap colors");
        
        // Recalcular rangos
        float minInfluence = float.MaxValue;
        float maxInfluence = float.MinValue;
        MapGenerator mapGenerator = influenceMap.mapGenerator;
        
        for (int y = 0; y < mapGenerator.mapHeight; y++)
        {
            for (int x = 0; x < mapGenerator.mapWidth; x++)
            {
                float influence = influenceMap.GetInfluenceAt(new Vector2Int(x, y));
                minInfluence = Mathf.Min(minInfluence, influence);
                maxInfluence = Mathf.Max(maxInfluence, influence);
            }
        }
        
        int tilesUpdated = 0;
        
        foreach (GameObject visualTile in visualTiles)
        {
            if (visualTile != null)
            {
                string[] nameParts = visualTile.name.Split('_');
                if (nameParts.Length >= 3)
                {
                    int x = int.Parse(nameParts[1]);
                    int y = int.Parse(nameParts[2]);
                    Vector2Int gridPos = new Vector2Int(x, y);
                    
                    // Actualizar el color
                    ApplyHeatmapColor(visualTile, gridPos, minInfluence, maxInfluence);
                    tilesUpdated++;
                }
            }
        }
        
        Debug.Log("INFLUENCE VISUALIZER: Heatmap updated for " + tilesUpdated + " tiles");
    }
    
    public void ClearVisualization()
    {
        Debug.Log("INFLUENCE VISUALIZER: Clearing heatmap (" + visualTiles.Count + " tiles)");
        foreach (GameObject visualTile in visualTiles)
        {
            if (visualTile != null)
            {
                DestroyImmediate(visualTile);
            }
        }
        visualTiles.Clear();
    }
    
    // Método para debuggear los colores
    public void DebugColorRanges()
    {
        if (influenceMap == null || influenceMap.mapGenerator == null) return;
        
        float minInfluence = float.MaxValue;
        float maxInfluence = float.MinValue;
        MapGenerator mapGenerator = influenceMap.mapGenerator;
        
        for (int y = 0; y < mapGenerator.mapHeight; y++)
        {
            for (int x = 0; x < mapGenerator.mapWidth; x++)
            {
                float influence = influenceMap.GetInfluenceAt(new Vector2Int(x, y));
                minInfluence = Mathf.Min(minInfluence, influence);
                maxInfluence = Mathf.Max(maxInfluence, influence);
            }
        }
        
        Debug.Log("HEATMAP COLOR RANGES (INVERTED):");
        Debug.Log($"Min influence: {minInfluence:F2} -> Red (Negative)");
        Debug.Log($"Max influence: {maxInfluence:F2} -> Green (Positive)");
        Debug.Log($"Neutral (0.5): {minInfluence + (maxInfluence - minInfluence) * 0.5f:F2} -> Yellow");
    }
}