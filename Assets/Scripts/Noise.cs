using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Noise
{
    public enum NormalizeMode
    {
        Local,
        Global
    }
    
    public static float[,] GenerateNoise(int width, int height, float scale, int seed, 
        int octavesNum, float persistence, float lacunarity, Vector2 offset, NormalizeMode normalizeMode)
    {
        float[,] noiseMap = new float[width, height];

        var rng = new System.Random(seed);
        var octaveOffsets = new Vector2[octavesNum];

        var amplitude = 1f;
        var frequency = 1f;
        var maxPossibleHeight = 1f / (1 - persistence);
        
        for (var i = 0; i < octavesNum; i++)
        {
            var offsetX = rng.Next(-100000, 100000) + offset.x;
            var offsetY = rng.Next(-100000, 100000) - offset.y;
            octaveOffsets[i] = new Vector2(offsetX, offsetY);

            //maxPossibleHeight += amplitude;
            amplitude *= persistence;
        }

        if (scale <= 0)
            scale = 0.000001f;

        var maxLocalNoiseHeight = float.MinValue;
        var minLocalNoiseHeight = float.MaxValue;

        var halfWidth = width * .5f;
        var halfHeight = height * .5f;

        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                amplitude = 1;
                frequency = 1;
                float noiseHeight = 0;

                for (var i = 0; i < octavesNum; i++)
                {
                    var sampleX = (x - halfWidth + octaveOffsets[i].x) / scale * frequency;
                    var sampleY = (y - halfHeight + octaveOffsets[i].y) / scale * frequency;

                    var perlinNoise = Mathf.PerlinNoise(sampleX, sampleY) * 2 - 1;
                    noiseHeight += perlinNoise * amplitude;

                    amplitude *= persistence;
                    frequency *= lacunarity;
                }

                noiseMap[x, y] = noiseHeight;

                if (noiseHeight < minLocalNoiseHeight)
                    minLocalNoiseHeight = noiseHeight;
                else if (noiseHeight > maxLocalNoiseHeight)
                    maxLocalNoiseHeight = noiseHeight;
            }
        }

        
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                if (normalizeMode == NormalizeMode.Local)
                    noiseMap[x, y] = Mathf.InverseLerp(minLocalNoiseHeight, maxLocalNoiseHeight, noiseMap[x, y]);
                else
                {
                    var normalizedHeight = (noiseMap[x, y] + 1) / maxPossibleHeight;
                    noiseMap[x, y] = Mathf.Clamp(normalizedHeight, 0, int.MaxValue);
                }
            }
        }

        return noiseMap;
    }
}
