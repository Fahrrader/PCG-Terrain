using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EndlessTerrain : MonoBehaviour
{
    private const float ViewerMoveThresholdForChunkUpdate = 25f;
    private const float SqrViewerMoveThresholdForChunkUpdate =
        ViewerMoveThresholdForChunkUpdate * ViewerMoveThresholdForChunkUpdate;
    private const float ColliderGenerationDistThreshold = 5f; 

    public int colliderLOD;
    public LODInfo[] detailLevels;
    private static float _maxViewDist;
    
    public Transform viewer;
    public Material mapMaterial;

    private static Vector2 _viewerPosition;
    private Vector2 _viewerPositionOld;
    private static MapGenerator _mapGenerator;
    private int _chunkSize;
    private int _chunksVisibleInViewDist;

    private Dictionary<Vector2, TerrainChunk> _terrainChunks = new Dictionary<Vector2, TerrainChunk>();
    private static List<TerrainChunk> _visibleChunks = new List<TerrainChunk>();

    private void Start()
    {
        _mapGenerator = FindObjectOfType<MapGenerator>();
        _maxViewDist = detailLevels[detailLevels.Length - 1].visibleDistThreshold;
        _chunkSize = _mapGenerator.MapChunkSize - 1;
        _chunksVisibleInViewDist = Mathf.RoundToInt(_maxViewDist / _chunkSize);
        
        _viewerPositionOld = _viewerPosition;
        UpdateVisibleChunks();
    }

    private void Update()
    {
        _viewerPosition = new Vector2(viewer.position.x, viewer.position.z) / _mapGenerator.terrainData.uniformScale;

        if (_viewerPosition != _viewerPositionOld)
        {
            foreach (var chunk in _visibleChunks)
            {
                chunk.UpdateCollisionMesh();
            }
        }

        if ((_viewerPositionOld - _viewerPosition).sqrMagnitude > SqrViewerMoveThresholdForChunkUpdate)
        {
            _viewerPositionOld = _viewerPosition;
            UpdateVisibleChunks();
        }
    }

    private void UpdateVisibleChunks()
    {
        var alreadyUpdatedChunkCoords = new HashSet<Vector2>();
        for (var i = _visibleChunks.Count - 1; i >= 0; i--)
        {
            alreadyUpdatedChunkCoords.Add(_visibleChunks[i].coord);
            _visibleChunks[i].UpdateChunk();
        }

        var currentChunkCoordX = Mathf.RoundToInt(_viewerPosition.x / _chunkSize);
        var currentChunkCoordY = Mathf.RoundToInt(_viewerPosition.y / _chunkSize);

        for (var yOffset = -_chunksVisibleInViewDist; yOffset <= _chunksVisibleInViewDist; yOffset++)
        {
            for (var xOffset = -_chunksVisibleInViewDist; xOffset <= _chunksVisibleInViewDist; xOffset++)
            {
                var viewedChunkCoord = new Vector2(currentChunkCoordX + xOffset, currentChunkCoordY + yOffset);
                if (alreadyUpdatedChunkCoords.Contains(viewedChunkCoord)) continue;
                if (_terrainChunks.ContainsKey(viewedChunkCoord))
                {
                    _terrainChunks[viewedChunkCoord].UpdateChunk();
                }
                else
                {
                    _terrainChunks.Add(viewedChunkCoord, new TerrainChunk(viewedChunkCoord, _chunkSize, detailLevels, colliderLOD, transform, mapMaterial));
                }
            }
        }
    }

    public class TerrainChunk
    {
        public Vector2 coord;
        
        private GameObject _meshObject;
        private Vector2 _position;
        private Bounds _bounds;

        private MapData _mapData;
        private bool _mapDataReceived;

        private MeshRenderer _meshRenderer;
        private MeshFilter _meshFilter;
        private MeshCollider _meshCollider;

        private LODInfo[] _detailLevels;
        private int _colliderLod;
        private LODMesh[] _lodMeshes;
        private int _previousLODIndex = -1;
        private bool _hasSetCollider;

        public TerrainChunk(Vector2 coord, int size, LODInfo[] detailLevels, int colliderLod, Transform parent, Material material)
        {
            this.coord = coord;
            _detailLevels = detailLevels;
            _colliderLod = colliderLod;
            
            _position = coord * size;
            _bounds = new Bounds(_position, Vector2.one * size);
            var positionV3 = new Vector3(_position.x, 0, _position.y);

            _meshObject = new GameObject("Terrain Chunk");
            _meshRenderer = _meshObject.AddComponent<MeshRenderer>();
            _meshFilter = _meshObject.AddComponent<MeshFilter>();
            _meshCollider = _meshObject.AddComponent<MeshCollider>();
            
            _meshRenderer.material = material;
            
            _meshObject.transform.position = positionV3 * _mapGenerator.terrainData.uniformScale;
            _meshObject.transform.parent = parent;
            _meshObject.transform.localScale = Vector3.one * _mapGenerator.terrainData.uniformScale;
            
            SetVisible(false);
            
            _lodMeshes = new LODMesh[detailLevels.Length];
            for (var i = 0; i < detailLevels.Length; i++)
            {
                _lodMeshes[i] = new LODMesh(detailLevels[i].lod, UpdateChunk);
            }
            
            _mapGenerator.RequestMapData(_position, OnMapDataReceived);
        }

        private void OnMapDataReceived(MapData mapData)
        {
            _mapData = mapData;
            _mapDataReceived = true;

            var texture = TextureGenerator.GetTextureFromColorMap(mapData.colorMap, 
                _mapGenerator.MapChunkSize, _mapGenerator.MapChunkSize);
            _meshRenderer.material.mainTexture = texture;
            
            UpdateChunk();
        }

        public void UpdateChunk()
        {
            if (!_mapDataReceived) return;
            
            var viewerDistFromNearestEdge = Mathf.Sqrt(_bounds.SqrDistance(_viewerPosition));
            
            var wasVisible = IsVisible();
            var visible = viewerDistFromNearestEdge <= _maxViewDist;
            
            if (visible)
            {
                var lodIndex = 0;
                
                for (var i = 0; i < _detailLevels.Length - 1; i++)
                    if (viewerDistFromNearestEdge > _detailLevels[i].visibleDistThreshold)
                        lodIndex = i + 1;
                    else break;
                
                if (lodIndex != _previousLODIndex)
                {
                    var lodMesh = _lodMeshes[lodIndex];
                    if (lodMesh.hasMesh)
                    {
                        _previousLODIndex = lodIndex;
                        _meshFilter.mesh = lodMesh.mesh;
                    }
                    else if (!lodMesh.hasRequestedMesh)
                    {
                        lodMesh.RequestMesh(_mapData);
                    }
                }
                
                _visibleChunks.Add(this);
            }

            if (wasVisible != visible)
            {
                if (visible)
                    _visibleChunks.Add(this);
                else
                    _visibleChunks.Remove(this);
                SetVisible(visible);
            }
        }

        public void UpdateCollisionMesh()
        {
            if (_hasSetCollider) return;
            
            var sqrDistFromViewerToEdge = _bounds.SqrDistance(_viewerPosition);

            if (sqrDistFromViewerToEdge < _detailLevels[_colliderLod].SqrVisibleDistThreshold)
                if (!_lodMeshes[_colliderLod].hasRequestedMesh)
                    _lodMeshes[_colliderLod].RequestMesh(_mapData);
            
            if (sqrDistFromViewerToEdge < ColliderGenerationDistThreshold * ColliderGenerationDistThreshold)
            {
                if (_lodMeshes[_colliderLod].hasMesh)
                {
                    _meshCollider.sharedMesh = _lodMeshes[_colliderLod].mesh;
                    _hasSetCollider = true;
                }
            }
        }

        public void SetVisible(bool visible)
        {
            _meshObject.SetActive(visible);
        }

        public bool IsVisible()
        {
            return _meshObject.activeSelf;
        }
    }

    private class LODMesh
    {
        public Mesh mesh;
        public bool hasRequestedMesh;
        public bool hasMesh;
        private int LOD;
        private Action updateCallback;

        public LODMesh(int lod, Action updateCallback)
        {
            this.LOD = lod;
            this.updateCallback = updateCallback;
        }

        private void OnMeshDataReceived(MeshData meshData)
        {
            mesh = meshData.CreateMesh();
            hasMesh = true;
            updateCallback();
        } 

        public void RequestMesh(MapData mapData)
        {
            hasRequestedMesh = true;
            _mapGenerator.RequestMeshData(mapData, LOD, OnMeshDataReceived);
        }
    }

    [Serializable]
    public struct LODInfo
    {
        public int lod;
        public float visibleDistThreshold;

        public float SqrVisibleDistThreshold => visibleDistThreshold * visibleDistThreshold;
    }
}
