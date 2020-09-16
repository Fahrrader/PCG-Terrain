using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu]
public class NoiseData : UpdatableData
{
    public Noise.NormalizeMode normalizationMode;
    public float noiseScale;
    
    public int numberOfOctaves;
    [Range(0,1)]
    public float persistence;
    public float lacunarity;
    
    public int seed;
    public Vector2 offset;
    
    #if UNITY_EDITOR
    protected override void OnValidate()
    {
        if (noiseScale <= 0)
            noiseScale = 0.000001f;
        if (lacunarity < 1)
            lacunarity = 1;
        if (numberOfOctaves < 0)
            numberOfOctaves = 0;
        
        base.OnValidate();
    }
    #endif
}
