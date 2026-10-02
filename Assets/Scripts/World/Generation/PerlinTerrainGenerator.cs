using UnityEngine;
using TowerDefense.World.Data;

namespace TowerDefense.World.Generation
{
    
    public class PerlinTerrainGenerator
    {
        private readonly WorldGenerationConfig _config;
        private readonly float _seedOffsetX;
        private readonly float _seedOffsetZ;

        public PerlinTerrainGenerator(WorldGenerationConfig config)
        {
            _config = config;

            var rng = new System.Random(config.seed);
            _seedOffsetX = (float)rng.NextDouble() * 10000f;
            _seedOffsetZ = (float)rng.NextDouble() * 10000f;
        }

        public void Generate(VoxelGrid grid)
        {
            for (int x = 0; x < grid.Width; x++)
            {
                for (int z = 0; z < grid.Depth; z++)
                {
                    int surfaceHeight = CalculateSurfaceHeight(x, z);
                    FillColumn(grid, x, z, surfaceHeight);
                }
            }
        }

        private int CalculateSurfaceHeight(int x, int z)
        {
            float sampleX = (x + _seedOffsetX) / _config.noiseScale;
            float sampleZ = (z + _seedOffsetZ) / _config.noiseScale;

            float noiseValue = Mathf.PerlinNoise(sampleX, sampleZ); 
            float signedNoise = noiseValue * 2f - 1f; 

            int baseHeight = Mathf.RoundToInt(_config.height * _config.baseHeightRatio);
            int amplitude = Mathf.RoundToInt(_config.height * _config.heightAmplitudeRatio);

            int surfaceHeight = baseHeight + Mathf.RoundToInt(signedNoise * amplitude);

            return Mathf.Clamp(surfaceHeight, 0, _config.height - 1);
        }

        private void FillColumn(VoxelGrid grid, int x, int z, int surfaceHeight)
        {
            for (int y = 0; y <= surfaceHeight; y++)
            {
                grid.SetVoxel(x, y, z, VoxelType.Dirt);
            }
        }
    }
}