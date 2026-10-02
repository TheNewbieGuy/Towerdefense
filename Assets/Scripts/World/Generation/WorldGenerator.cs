using UnityEngine;
using TowerDefense.World.Data;

namespace TowerDefense.World.Generation
{
    public class WorldGenerator : MonoBehaviour
    {
        [SerializeField] private WorldGenerationConfig config;
        
        [Header("Runtime Seed Options")]
        [SerializeField] private bool useRandomSeed = true;

        public VoxelGrid Grid { get; private set; }
        public WorldGenerationConfig Config => config;

        public event System.Action<VoxelGrid> OnWorldGenerated;
        public event System.Action<VoxelGrid> OnTerrainModified;

        private void Start()
        {
            GenerateWorld();
        }

        [ContextMenu("Regenerate World")]
        public void GenerateWorld()
        {
            if (config == null)
            {
                Debug.LogError("WorldGenerator has no WorldGenerationConfig assigned.", this);
                return;
            }

            // Assign a random seed at runtime if enabled
            if (useRandomSeed)
            {
                config.seed = Random.Range(int.MinValue, int.MaxValue);
            }

            Grid = new VoxelGrid(config.width, config.height, config.depth);

            var terrainGenerator = new PerlinTerrainGenerator(config);
            terrainGenerator.Generate(Grid);

            OnWorldGenerated?.Invoke(Grid);
        }

        public void NotifyTerrainModified()
        {
            OnTerrainModified?.Invoke(Grid);
        }

        public Vector3 VoxelToWorld(Vector3Int voxelPos)
        {
            return new Vector3(voxelPos.x, voxelPos.y, voxelPos.z) * config.voxelSize;
        }

        public int CenterZ => Grid != null ? Grid.Depth / 2 : 0;

        public int CoreZ
        {
            get
            {
                if (Grid == null || config == null) return 0;
                int z = Mathf.RoundToInt((Grid.Depth - 1) * config.coreDepthRatio);
                return Mathf.Clamp(z, 0, Grid.Depth - 1);
            }
        }

        public Vector3Int GetCorePosition()
        {
            if (Grid == null || config == null)
                return Vector3Int.zero;

            float midRatio = (config.coreAxisMinHeightRatio + config.coreAxisMaxHeightRatio) / 2f;
            int coreY = Mathf.RoundToInt(Grid.Height * midRatio);
            return new Vector3Int(Grid.Width / 2, coreY, CoreZ);
        }

        public Vector3Int GetRandomCoreAxisPosition()
        {
            if (Grid == null || config == null)
                return Vector3Int.zero;

            int minY = Mathf.RoundToInt(Grid.Height * config.coreAxisMinHeightRatio);
            int maxY = Mathf.RoundToInt(Grid.Height * config.coreAxisMaxHeightRatio);

            int bottomCushionY = Mathf.RoundToInt(Grid.Height * config.coreAxisBottomCushionRatio);
            minY = Mathf.Max(minY, bottomCushionY);

            int topCushionY = Grid.Height - 1 - Mathf.RoundToInt(Grid.Height * config.coreAxisTopCushionRatio);
            maxY = Mathf.Min(maxY, topCushionY);

            if (maxY < minY)
            {
                (minY, maxY) = (maxY, minY); 
            }

            int y = Random.Range(minY, maxY + 1);
            return new Vector3Int(Grid.Width / 2, y, CoreZ);
        }
    }
}