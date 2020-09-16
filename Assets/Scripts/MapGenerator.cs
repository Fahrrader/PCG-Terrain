using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class MapGenerator : MonoBehaviour
{
    public enum DrawMode
    {
        NoiseMap,
        ColorMap,
        Mesh,
        ShadedMesh,
        Falloff
    }
 
    public DrawMode drawMode;

    public TerrainData terrainData;
    public NoiseData noiseData;
    public TextureData textureData;

    public Material terrainMaterial;

    [Range(0, 6)]
    public int editorLevelOfDetail;
    public bool autoUpdate;

    public TerrainType[] regions;

    private float[,] falloffMap;

    private Queue<MapThreadInfo<MapData>> mapDataThreadInfoQueue = new Queue<MapThreadInfo<MapData>>();
    private Queue<MapThreadInfo<MeshData>> meshDataThreadInfoQueue = new Queue<MapThreadInfo<MeshData>>();

    private void Awake()
    {
        textureData.ApplyMaterial(terrainMaterial);
        textureData.UpdateMeshHeights(terrainMaterial, terrainData.minHeight, terrainData.maxHeight);
        falloffMap = FalloffGenerator.GenerateFalloffMap(MapChunkSize);
    }

    private void OnValuesUpdated()
    {
        if (!Application.isPlaying)
        {
            DrawMapInEditor();
        }
    }

    private void OnTextureValuesUpdated()
    {
        textureData.ApplyMaterial(terrainMaterial);
    }

    private void OnValidate()
    {
        if (terrainData != null)
        {
            terrainData.OnValuesUpdated -= OnValuesUpdated;
            terrainData.OnValuesUpdated += OnValuesUpdated;            
        }
        
        if (noiseData != null)
        {
            noiseData.OnValuesUpdated -= OnValuesUpdated;
            noiseData.OnValuesUpdated += OnValuesUpdated;            
        }
        
        if (textureData != null)
        {
            textureData.OnValuesUpdated -= OnTextureValuesUpdated;
            textureData.OnValuesUpdated += OnTextureValuesUpdated;
        }
    }

    private void Update()
    {
        if (mapDataThreadInfoQueue.Count > 0)
            for (var i = 0; i < mapDataThreadInfoQueue.Count; i++)
            {
                var threadInfo = mapDataThreadInfoQueue.Dequeue();
                threadInfo.callback(threadInfo.parameter);
            }
        if (meshDataThreadInfoQueue.Count > 0)
            for (var i = 0; i < meshDataThreadInfoQueue.Count; i++)
            {
                var threadInfo = meshDataThreadInfoQueue.Dequeue();
                threadInfo.callback(threadInfo.parameter);
            }
    }
    
    public int MapChunkSize => terrainData.useFlatShading ? 95 : 239;

    public void DrawMapInEditor()
    {
        textureData.UpdateMeshHeights(terrainMaterial, terrainData.minHeight, terrainData.maxHeight);
        var mapData = GenerateMapData(Vector2.zero);

        var display = FindObjectOfType<MapDisplay>();
        switch (drawMode)
        {
            case DrawMode.NoiseMap:
                display.DrawTexture(TextureGenerator.GetTextureFromHeightMap(mapData.heightMap));
                break;
            case DrawMode.ColorMap:
                display.DrawTexture(TextureGenerator.GetTextureFromColorMap(mapData.colorMap, MapChunkSize, MapChunkSize));
                break;
            case DrawMode.Mesh:
                display.DrawMesh(
                    MeshGenerator.GenerateTerrainMesh(mapData.heightMap, terrainData.meshHeightMultiplier, 
                        terrainData.meshHeightCurve, editorLevelOfDetail, terrainData.useFlatShading), 
                    TextureGenerator.GetTextureFromColorMap(mapData.colorMap, MapChunkSize, MapChunkSize));
                break;
            case DrawMode.ShadedMesh:
                display.DrawMesh(
                    MeshGenerator.GenerateTerrainMesh(mapData.heightMap, terrainData.meshHeightMultiplier,
                        terrainData.meshHeightCurve, editorLevelOfDetail, terrainData.useFlatShading));
                break;
            case DrawMode.Falloff:
                display.DrawTexture(TextureGenerator.GetTextureFromHeightMap(FalloffGenerator.GenerateFalloffMap(MapChunkSize)));
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private MapData GenerateMapData(Vector2 center)
    {
        //if (applyFalloff && falloffMap == null)
        falloffMap = FalloffGenerator.GenerateFalloffMap(MapChunkSize + 2);
        
        var noiseMap = Noise.GenerateNoise(MapChunkSize + 2, MapChunkSize + 2, 
            noiseData.noiseScale, noiseData.seed, noiseData.numberOfOctaves, noiseData.persistence, noiseData.lacunarity, 
            center + noiseData.offset, noiseData.normalizationMode);
        
        if (terrainData.applyFalloff)
            for (var y = 0; y < MapChunkSize + 2; y++)
            for (var x = 0; x < MapChunkSize + 2; x++)
                noiseMap[x, y] = Mathf.Clamp(noiseMap[x, y] - falloffMap[x, y], 0, 1);

        var colorMap = new Color[MapChunkSize * MapChunkSize];
        
        for (var y = 0; y < MapChunkSize; y++)
        {
            for (var x = 0; x < MapChunkSize; x++)
            {
                var currentHeight = noiseMap[x, y];
                for (var i = 0; i < regions.Length; i++)
                {
                    if (currentHeight >= regions[i].height)
                    {
                        colorMap[y * MapChunkSize + x] = regions[i].color;
                    }
                    else break;
                }
            }
        }

        return new MapData(noiseMap, colorMap);
    }

    public void RequestMapData(Vector2 center, Action<MapData> callback)
    {
        void ThreadStart()
        {
            MapDataThread(center, callback);
        }

        new Thread(ThreadStart).Start();
    }

    private void MapDataThread(Vector2 center, Action<MapData> callback)
    {
        var mapData = GenerateMapData(center);
        lock (mapDataThreadInfoQueue)
        {
            mapDataThreadInfoQueue.Enqueue(new MapThreadInfo<MapData>(callback, mapData));
        }
    }

    public void RequestMeshData(MapData mapData, int lod, Action<MeshData> callback)
    {
        void ThreadStart()
        {
            MeshDataThread(mapData, lod, callback);
        }

        new Thread(ThreadStart).Start();
    }

    private void MeshDataThread(MapData mapData, int lod, Action<MeshData> callback)
    {
        var meshData = MeshGenerator.GenerateTerrainMesh(mapData.heightMap, terrainData.meshHeightMultiplier, 
            terrainData.meshHeightCurve, lod, terrainData.useFlatShading);
        lock (meshDataThreadInfoQueue)
        {
            meshDataThreadInfoQueue.Enqueue(new MapThreadInfo<MeshData>(callback, meshData));
        }
    }

    private readonly struct MapThreadInfo<T>
    {
        public readonly Action<T> callback;
        public readonly T parameter;

        public MapThreadInfo(Action<T> callback, T parameter)
        {
            this.callback = callback;
            this.parameter = parameter;
        }
    }
}

[Serializable]
public struct TerrainType
{
    public string name;
    public float height;
    public Color color;
}

public struct MapData
{
    public float[,] heightMap;
    public Color[] colorMap;

    public MapData(float[,] heightMap, Color[] colorMap)
    {
        this.heightMap = heightMap;
        this.colorMap = colorMap;
    }
}