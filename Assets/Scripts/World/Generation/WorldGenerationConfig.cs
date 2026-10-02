using UnityEngine;

namespace TowerDefense.World.Generation
{
    
    [CreateAssetMenu(
        fileName = "WorldGenerationConfig",
        menuName = "TowerDefense/World Generation Config")]
    public class WorldGenerationConfig : ScriptableObject
    {
        [Header("Grid Dimensions (in voxels)")]
        [Min(1)] public int width = 64;
        [Min(1)] public int height = 32;
        [Min(1)] public int depth = 64;

        [Header("World Space")]
        [Min(0.01f)] public float voxelSize = 1f;

        [Header("Perlin Noise Settings")]
        public float noiseScale = 12f;

        [Range(0f, 1f)] public float baseHeightRatio = 0.4f;

        [Range(0f, 1f)] public float heightAmplitudeRatio = 0.25f;

        [Header("Seed")]
        public int seed = 0;

        [Header("Core / Objective Placement")]
        [Range(0f, 1f)] public float coreDepthRatio = 0f;

        [Range(0f, 1f)] public float coreAxisMinHeightRatio = 0.15f;
        [Range(0f, 1f)] public float coreAxisMaxHeightRatio = 0.4f;

        [Range(0f, 1f)] public float coreAxisBottomCushionRatio = 0.05f;
        [Range(0f, 1f)] public float coreAxisTopCushionRatio = 0.05f;
    }
}