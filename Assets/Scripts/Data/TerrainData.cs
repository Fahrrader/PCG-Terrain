using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu]
public class TerrainData : UpdatableData
{
    public float uniformScale = 2.5f; 
    
    public float meshHeightMultiplier;
    public AnimationCurve meshHeightCurve;

    public bool useFlatShading;
    public bool applyFalloff;

    public float minHeight => uniformScale * meshHeightMultiplier * meshHeightCurve.Evaluate(0);
    public float maxHeight => uniformScale * meshHeightMultiplier * meshHeightCurve.Evaluate(1);
}