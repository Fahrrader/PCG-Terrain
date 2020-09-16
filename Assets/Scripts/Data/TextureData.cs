using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu]
public class TextureData : UpdatableData
{
    public Color[] colors;
    [Range(0,1)]
    public float[] startHeights;
    [Range(0,1)]
    public float[] blends;
    
    private float savedMinHeight;
    private float savedMaxHeight;
    
    public void ApplyMaterial(Material material)
    {
        material.SetInt("color_count", colors.Length);
        material.SetColorArray("colors", colors);
        material.SetFloatArray("start_heights", startHeights);
        material.SetFloatArray("blends", blends);
        
        UpdateMeshHeights(material, savedMinHeight, savedMaxHeight);
    }

    public void UpdateMeshHeights(Material material, float minHeight, float maxHeight)
    {
        savedMinHeight = minHeight;
        savedMaxHeight = maxHeight;
        
        material.SetFloat("min_height", minHeight);
        material.SetFloat("max_height", maxHeight);
    }
}
