using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FalloffGenerator : MonoBehaviour
{
    public static float[,] GenerateFalloffMap(int size)
    {
        var map = new float[size, size];

        for (var i = 0; i < size; i++)
        {
            for (var j = 0; j < size; j++)
            {
                var x = i / (float) size * 2 - 1;
                var y = j / (float) size * 2 - 1;

                var value = Mathf.Max(Mathf.Abs(x), Mathf.Abs(y));
                map[i, j] = Evaluate(value);
            }            
        }

        return map;
    }

    private static float Evaluate(float value)
    {
        var scale = 1f;
        var slope = 3f;
        var offset = 2.2f;
        var a = Mathf.Pow(value, slope);

        return scale * a / (a + Mathf.Pow(offset - offset * value, slope));
    }
}
