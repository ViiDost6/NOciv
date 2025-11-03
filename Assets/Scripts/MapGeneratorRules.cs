using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class TileRule
{
    public int tileType;
    [Range(0f, 1f)] public float threshold = 0.5f;
    [Range(1f, 999f)] public float movementWeight = 1f;
    public bool walkable = true;
}

public class MapGeneratorRules : MonoBehaviour
{
    public MapGenerator mapGenerator;
    public List<TileRule> tileRules;
    
    [Header("Configuración de Ruido")]
    [Range(0, 1000000)] public int seed = 0;
    [Range(0.01f, 0.5f)] public float globalNoiseScale = 0.1f;
    [Range(1, 8)] public int octaves = 3;
    [Range(0f, 1f)] public float persistence = 0.5f;
    [Range(1f, 4f)] public float lacunarity = 2f;
    
    public void GenerateMapWithRules()
    {
        if (mapGenerator == null) 
        {
            mapGenerator = GetComponent<MapGenerator>();
            if (mapGenerator == null) return;
        }

        // Generar semilla aleatoria
        seed = Random.Range(0, 1000000);
        
        // Generar matriz del mapa
        int[,] mapData = GenerateMapMatrix();
        
        // Pasar los datos al MapGenerator
        mapGenerator.SetMapData(mapData);
    }

    int[,] GenerateMapMatrix()
    {
        int height = mapGenerator.mapHeight;
        int width = mapGenerator.mapWidth;
        int[,] mapMatrix = new int[height, width];

        Random.InitState(seed);
        float offsetX = Random.Range(-10000f, 10000f);
        float offsetY = Random.Range(-10000f, 10000f);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                // Coordenadas para ruido (sin ajuste hexagonal)
                float noiseX = (x + offsetX) * globalNoiseScale;
                float noiseY = (y + offsetY) * globalNoiseScale;
                
                float noiseValue = GenerateOctaveNoise(noiseX, noiseY);
                mapMatrix[y, x] = SelectTileFromNoise(noiseValue);
            }
        }

        return mapMatrix;
    }

    float GenerateOctaveNoise(float x, float y)
    {
        float value = 0f;
        float amplitude = 1f;
        float frequency = 1f;
        float maxValue = 0f;

        for (int i = 0; i < octaves; i++)
        {
            float perlinValue = Mathf.PerlinNoise(x * frequency, y * frequency);
            value += perlinValue * amplitude;
            
            maxValue += amplitude;
            amplitude *= persistence;
            frequency *= lacunarity;
        }

        return value / maxValue;
    }

    int SelectTileFromNoise(float noiseValue)
    {
        // Ordenar reglas por threshold (mayor a menor)
        List<TileRule> sortedRules = new List<TileRule>(tileRules);
        sortedRules.Sort((a, b) => b.threshold.CompareTo(a.threshold));

        // Encontrar la regla apropiada
        foreach (var rule in sortedRules)
        {
            if (noiseValue >= rule.threshold)
            {
                return rule.tileType;
            }
        }

        // Fallback a la primera regla caminable
        foreach (var rule in tileRules)
        {
            if (rule.walkable) return rule.tileType;
        }

        return 0;
    }
}