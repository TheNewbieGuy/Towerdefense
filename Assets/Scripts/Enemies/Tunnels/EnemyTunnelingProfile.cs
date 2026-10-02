using UnityEngine;

namespace TowerDefense.World.Tunneling
{
    [CreateAssetMenu(
        fileName = "EnemyTunnelingProfile",
        menuName = "TowerDefense/Enemy Tunneling Profile")]
    public class EnemyTunnelingProfile : ScriptableObject
    {
        [Header("Speed")]
        [Min(0.01f)] public float moveIntervalSeconds = 0.5f;

        [Header("Tunnel Size")]
        [Min(0)] public int tunnelRadius = 2;
        [Min(1)] public int tunnelDepth = 3;

        [Header("Wiggliness")]
        [Range(0f, 2f)] public float curveNoiseWeight = 0.5f;
        [Min(0f)] public float curveNoiseSpeed = 0.3f;

        [Header("Steering")]
        [Min(0f)] public float targetPullWeight = 1f;
        [Min(0f)] public float gravityWeight = 0.4f;

        [Header("World Bounds")]
        [Min(0f)] public float floorCushion = 3f;
        [Min(0f)] public float floorRepulsionWeight = 1.5f;

        [Header("Tunnel Seeking & Spawning")]
        public bool allowTunnelMerging = true;

        public bool mustSpawnInExistingTunnel = false;

        [Range(0f, 100f)] public float existingTunnelSpawnChance = 25f;

        [Range(0f, 1f)] public float tunnelSeekingChance = 0.01f;
        [Min(0f)] public float tunnelSeekingDetectionRadius = 3f;

        [Header("Tunnel Merging")]
        [Range(0f, 1f)] public float mergeChance = 0.02f;
        [Min(0f)] public float opportunisticMergeDetectionRadius = 0.5f;
        [Min(0f)] public float forcedMergeDetectionRadius = 3f;
        [Min(0f)] public float minMergeSeparation = 1.5f;
    }
}