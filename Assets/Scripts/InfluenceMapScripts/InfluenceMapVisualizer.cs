using UnityEngine;
using System.Collections.Generic;

public class InfluenceMapVisualizer : MonoBehaviour
{
    public InfluenceMap influenceMap;
    public GameObject influenceTilePrefab;
    
    private List<GameObject> visualTiles = new List<GameObject>();

    public void GenerateVisualization()
    {
        ClearVisualization();
        
        if (influenceMap == null || influenceTilePrefab == null || influenceMap.mapGenerator == null)
        {
            Debug.LogError("InfluenceVisualizer: Missing references");
            return;
        }

        // Asegurar que el influence map este generado
        if (influenceMap.GetInfluenceAt(Vector2Int.zero) == 0f)
        {
            influenceMap.GenerateInfluenceMap();
        }

        // Obtener rango de influencias
        influenceMap.GetInfluenceRange(out float minInfluence, out float maxInfluence);
        
        Debug.Log($"InfluenceVisualizer: Range {minInfluence:F2} to {maxInfluence:F2}");

        // Crear tiles visuales
        Dictionary<Vector2Int, TileData> tileGrid = influenceMap.mapGenerator.GetTileGrid();
        if (tileGrid == null) 
        {
            Debug.LogError("InfluenceVisualizer: No tile grid found");
            return;
        }
        
        int tilesCreated = 0;
        foreach (var kvp in tileGrid)
        {
            if (CreateHeatmapTile(kvp.Key, kvp.Value.transform.position, minInfluence, maxInfluence))
            {
                tilesCreated++;
            }
        }
        
        Debug.Log($"InfluenceVisualizer: Created {tilesCreated} tiles");
    }
    
    bool CreateHeatmapTile(Vector2Int gridPos, Vector3 worldPosition, float minInfluence, float maxInfluence)
    {
        if (influenceTilePrefab == null) return false;
        
        GameObject visualTile = Instantiate(influenceTilePrefab);
        visualTile.name = $"Heatmap_{gridPos.x}_{gridPos.y}";
        visualTile.transform.position = new Vector3(worldPosition.x, worldPosition.y, worldPosition.z - 0.1f);
        visualTile.transform.SetParent(transform);
        
        ApplyHeatmapColor(visualTile, gridPos, minInfluence, maxInfluence);
        visualTiles.Add(visualTile);
        
        return true;
    }
    
    void ApplyHeatmapColor(GameObject visualTile, Vector2Int gridPos, float minInfluence, float maxInfluence)
    {
        float influence = influenceMap.GetInfluenceAt(gridPos);
        Color color = GetHeatmapColor(influence, minInfluence, maxInfluence);
        
        SpriteRenderer spriteRenderer = visualTile.GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            spriteRenderer.color = color;
        }
        else
        {
            Renderer renderer = visualTile.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = color;
            }
        }
    }
    
    Color GetHeatmapColor(float influence, float minInfluence, float maxInfluence)
    {
        float normalized = Mathf.InverseLerp(minInfluence, maxInfluence, influence);
        
        // Verde (bajo) -> Amarillo (medio) -> Rojo (alto)
        if (normalized < 0.5f)
        {
            return Color.Lerp(Color.green, Color.yellow, normalized * 2f);
        }
        else
        {
            return Color.Lerp(Color.yellow, Color.red, (normalized - 0.5f) * 2f);
        }
    }
    
    public void UpdateVisualization()
    {
        if (visualTiles.Count == 0) 
        {
            GenerateVisualization();
            return;
        }
        
        influenceMap.GetInfluenceRange(out float minInfluence, out float maxInfluence);
        
        int tilesUpdated = 0;
        foreach (GameObject visualTile in visualTiles)
        {
            if (visualTile != null)
            {
                string[] nameParts = visualTile.name.Split('_');
                if (nameParts.Length == 3)
                {
                    Vector2Int gridPos = new Vector2Int(int.Parse(nameParts[1]), int.Parse(nameParts[2]));
                    ApplyHeatmapColor(visualTile, gridPos, minInfluence, maxInfluence);
                    tilesUpdated++;
                }
            }
        }
        
        Debug.Log($"InfluenceVisualizer: Updated {tilesUpdated} tiles");
    }
    
    public void ClearVisualization()
    {
        foreach (GameObject tile in visualTiles)
        {
            if (tile != null) DestroyImmediate(tile);
        }
        visualTiles.Clear();
    }
}