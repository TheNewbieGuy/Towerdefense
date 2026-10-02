using System.Collections.Generic;
using UnityEngine;
using TowerDefense.World.Data;
using TowerDefense.World.Generation;

namespace TowerDefense.World.Rendering
{
   
    [RequireComponent(typeof(WorldGenerator))]
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    public class VoxelWorldRenderer : MonoBehaviour
    {
        [Tooltip("Optional. If left empty, a simple double-sided material is generated automatically.")]
        [SerializeField] private Material voxelMaterial;

        private WorldGenerator _worldGenerator;
        private MeshFilter _meshFilter;
        private Mesh _mesh;

        private readonly List<Vector3> _vertices = new List<Vector3>();
        private readonly List<int> _triangles = new List<int>();
        private MeshCollider _meshCollider;
        private void Awake()
        {
            _worldGenerator = GetComponent<WorldGenerator>();
            _meshFilter = GetComponent<MeshFilter>();
            var meshRenderer = GetComponent<MeshRenderer>();
    
            _meshCollider = GetComponent<MeshCollider>();
            if (_meshCollider == null) 
            {
                _meshCollider = gameObject.AddComponent<MeshCollider>();
            }

            _mesh = new Mesh { name = "VoxelWorldMesh" };
            _mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            _meshFilter.sharedMesh = _mesh;

            meshRenderer.sharedMaterial = voxelMaterial != null ? voxelMaterial : CreateDefaultMaterial();
        }

        private void OnEnable()
        {
            _worldGenerator.OnWorldGenerated += RenderWorld;
            _worldGenerator.OnTerrainModified += RenderWorld;
        }

        private void OnDisable()
        {
            _worldGenerator.OnWorldGenerated -= RenderWorld;
            _worldGenerator.OnTerrainModified -= RenderWorld;
        }

        private void RenderWorld(VoxelGrid grid)
        {
            _vertices.Clear();
            _triangles.Clear();

            float voxelSize = _worldGenerator.Config.voxelSize;
            float h = voxelSize / 2f;

            for (int x = 0; x < grid.Width; x++)
            {
                for (int y = 0; y < grid.Height; y++)
                {
                    for (int z = 0; z < grid.Depth; z++)
                    {
                        if (!grid.IsSolid(x, y, z))
                            continue;

                        Vector3 center = _worldGenerator.VoxelToWorld(new Vector3Int(x, y, z));
                        AddExposedFaces(grid, x, y, z, center, h);
                    }
                }
            }

            _mesh.Clear();
            _mesh.SetVertices(_vertices);
            _mesh.SetTriangles(_triangles, 0);
            _mesh.RecalculateNormals();
            _mesh.RecalculateBounds();
            _meshCollider.sharedMesh = null; 
            _meshCollider.sharedMesh = _mesh;
        }

        private void AddExposedFaces(VoxelGrid grid, int x, int y, int z, Vector3 c, float h)
        {
            if (!grid.IsSolid(x + 1, y, z))
                AddQuad(c + new Vector3(h, -h, -h), c + new Vector3(h, h, -h), c + new Vector3(h, h, h), c + new Vector3(h, -h, h));

            if (!grid.IsSolid(x - 1, y, z))
                AddQuad(c + new Vector3(-h, -h, h), c + new Vector3(-h, h, h), c + new Vector3(-h, h, -h), c + new Vector3(-h, -h, -h));

            if (!grid.IsSolid(x, y + 1, z))
                AddQuad(c + new Vector3(-h, h, -h), c + new Vector3(-h, h, h), c + new Vector3(h, h, h), c + new Vector3(h, h, -h));

            if (!grid.IsSolid(x, y - 1, z))
                AddQuad(c + new Vector3(-h, -h, h), c + new Vector3(-h, -h, -h), c + new Vector3(h, -h, -h), c + new Vector3(h, -h, h));

            if (!grid.IsSolid(x, y, z + 1))
                AddQuad(c + new Vector3(-h, -h, h), c + new Vector3(h, -h, h), c + new Vector3(h, h, h), c + new Vector3(-h, h, h));

            if (!grid.IsSolid(x, y, z - 1))
                AddQuad(c + new Vector3(h, -h, -h), c + new Vector3(-h, -h, -h), c + new Vector3(-h, h, -h), c + new Vector3(h, h, -h));
        }

        private void AddQuad(Vector3 v0, Vector3 v1, Vector3 v2, Vector3 v3)
        {
            int start = _vertices.Count;
            _vertices.Add(v0);
            _vertices.Add(v1);
            _vertices.Add(v2);
            _vertices.Add(v3);

            _triangles.Add(start + 0);
            _triangles.Add(start + 1);
            _triangles.Add(start + 2);
            _triangles.Add(start + 0);
            _triangles.Add(start + 2);
            _triangles.Add(start + 3);
        }

        private Material CreateDefaultMaterial()
        {
           
            var shader = Shader.Find("Sprites/Default");
            return new Material(shader) { color = new Color(0.45f, 0.30f, 0.15f) }; // dirt brown
        }
    }
}