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
    
    [Header("Configuración Principal")]
    [Range(0, 1000000)] public int seed = 0;
    [Range(0.01f, 0.5f)] public float globalNoiseScale = 0.1f;
    [Range(1, 8)] public int octaves = 3;
    [Range(0f, 1f)] public float persistence = 0.5f;
    [Range(1f, 4f)] public float lacunarity = 2f;
    
    [Header("Separación de Biomas")]
    [Range(0.1f, 2f)] public float biomeSeparation = 1.0f;
    [Range(0f, 1f)] public float forestClusterStrength = 0.7f;
    [Range(0f, 1f)] public float mountainClusterStrength = 0.8f;
    [Range(0f, 1f)] public float waterClusterStrength = 0.9f;
    
    public void GenerateMapWithRules()
    {
        if (mapGenerator == null) mapGenerator = GetComponent<MapGenerator>();
        if (mapGenerator == null) return;

        seed = Random.Range(0, 1000000);
        int[,] mapData = GenerateMapMatrix();
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

        // Generar máscaras de influencia para cada bioma
        float[,] waterMask = GenerateBiomeMask(height, width, offsetX, offsetY, 1.5f, waterClusterStrength);
        float[,] mountainMask = GenerateBiomeMask(height, width, offsetX + 1000f, offsetY + 1000f, 1.2f, mountainClusterStrength);
        float[,] forestMask = GenerateBiomeMask(height, width, offsetX + 2000f, offsetY + 2000f, 1.0f, forestClusterStrength);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float noiseX = (x + offsetX) * globalNoiseScale;
                float noiseY = (y + offsetY) * globalNoiseScale;
                float baseNoise = GenerateOctaveNoise(noiseX, noiseY);
                
                // Aplicar máscaras de biomas
                float waterValue = waterMask[y, x];
                float mountainValue = mountainMask[y, x];
                float forestValue = forestMask[y, x];
                
                mapMatrix[y, x] = SelectTileWithBiomes(baseNoise, waterValue, mountainValue, forestValue);
            }
        }

        return mapMatrix;
    }

    float[,] GenerateBiomeMask(int height, int width, float offsetX, float offsetY, float scaleMultiplier, float clusterStrength)
    {
        float[,] mask = new float[height, width];
        float scale = globalNoiseScale * scaleMultiplier * biomeSeparation;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float noiseX = (x + offsetX) * scale;
                float noiseY = (y + offsetY) * scale;
                float value = GenerateOctaveNoise(noiseX, noiseY);
                
                // Aplicar fuerza de agrupamiento
                mask[y, x] = Mathf.Pow(value, clusterStrength);
            }
        }

        return mask;
    }

    int SelectTileWithBiomes(float baseNoise, float waterValue, float mountainValue, float forestValue)
    {
        // Agua - más agrupada
        if (waterValue > 0.7f && baseNoise > 0.6f)
            return FindTileTypeByThreshold(0.8f);
        
        // Montañas - moderadamente agrupadas
        if (mountainValue > 0.6f && baseNoise > 0.5f && baseNoise <= 0.8f)
            return FindTileTypeByThreshold(0.6f);
        
        // Bosques - algo agrupados
        if (forestValue > 0.5f && baseNoise > 0.3f && baseNoise <= 0.6f)
            return FindTileTypeByThreshold(0.4f);
        
        // Praderas - distribución base
        return FindTileTypeByThreshold(0.0f);
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

    int FindTileTypeByThreshold(float threshold)
    {
        List<TileRule> sortedRules = new List<TileRule>(tileRules);
        sortedRules.Sort((a, b) => b.threshold.CompareTo(a.threshold));

        foreach (var rule in sortedRules)
            if (threshold >= rule.threshold)
                return rule.tileType;

        foreach (var rule in tileRules)
            if (rule.walkable) return rule.tileType;

        return 0;
    }
}