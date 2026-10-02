using System.Collections;
using UnityEngine;
using TowerDefense.World.Data;
using TowerDefense.World.Generation;

namespace TowerDefense.World.Tunneling
{
   
    [RequireComponent(typeof(WorldGenerator))]
    public class EnemySpawner : MonoBehaviour
    {
        [SerializeField] private TunnelingEnemy enemyPrefab;

        [SerializeField] private EnemyTunnelingProfile tunnelingProfile;

        [Header("Wave")]
        [Min(1)] [SerializeField] private int enemyCount = 1;
        [Min(0f)] [SerializeField] private float spawnInterval = 1.5f;

        [Header("Spawn Position")]
        [SerializeField] private bool randomizeSpawnSide = true;
        [SerializeField] private bool spawnOnLeftSide = true;
        [SerializeField] private int spawnEdgeMargin = 3;

        [Header("Existing Tunnels")]
        [Range(0f, 1f)] [SerializeField] private float joinExistingTunnelChance = 0.25f;

        private WorldGenerator _worldGenerator;
        private TunnelNetwork _tunnelNetwork;

        private void Awake()
        {
            _worldGenerator = GetComponent<WorldGenerator>();
        }

        private void OnEnable()
        {
            _worldGenerator.OnWorldGenerated += HandleWorldGenerated;
        }

        private void OnDisable()
        {
            _worldGenerator.OnWorldGenerated -= HandleWorldGenerated;
        }

        private void HandleWorldGenerated(VoxelGrid grid)
        {
            _tunnelNetwork = new TunnelNetwork(); 
            StopAllCoroutines(); 
            StartCoroutine(SpawnWaveCoroutine(grid));
        }

        private IEnumerator SpawnWaveCoroutine(VoxelGrid grid)
        {
            for (int i = 0; i < enemyCount; i++)
            {
                SpawnOneEnemy(grid);

                bool isLastEnemy = i == enemyCount - 1;
                if (!isLastEnemy && spawnInterval > 0f)
                {
                    yield return new WaitForSeconds(spawnInterval);
                }
            }
        }

        private void SpawnOneEnemy(VoxelGrid grid)
        {
            TunnelingEnemy enemy = enemyPrefab != null
                ? Instantiate(enemyPrefab)
                : CreateFallbackEnemy();
            enemy.name = "TunnelingEnemy";

            Vector3Int targetVoxel = _worldGenerator.GetRandomCoreAxisPosition();

            bool tryJoinExisting = Random.value < joinExistingTunnelChance;
            if (tryJoinExisting && _tunnelNetwork.TryGetRandomJoinablePoint(out TunnelPath joinPath, out int joinIndex))
            {
                Vector2Int joinVoxel2D = joinPath.Voxels[joinIndex];
                Vector3Int joinVoxel = new Vector3Int(joinVoxel2D.x, joinVoxel2D.y, 0);
                enemy.InitializeFollowing(_worldGenerator, joinVoxel, targetVoxel, _tunnelNetwork, joinPath, joinIndex, tunnelingProfile);
            }
            else
            {
                Vector3Int spawnVoxel = FindEdgeEmbeddedSpawnPosition(grid);
                enemy.Initialize(_worldGenerator, spawnVoxel, targetVoxel, _tunnelNetwork, tunnelingProfile);
            }
        }

       
        private Vector3Int FindEdgeEmbeddedSpawnPosition(VoxelGrid grid)
        {
            bool spawnOnLeft = randomizeSpawnSide ? Random.value < 0.5f : spawnOnLeftSide;
            int x = spawnOnLeft ? spawnEdgeMargin : grid.Width - 1 - spawnEdgeMargin;
            int z = 0;

            int surfaceHeight = FindSurfaceHeight(grid, x, z);
            int spawnY = Random.Range(0, surfaceHeight + 1); 

            return new Vector3Int(x, spawnY, z);
        }

        private int FindSurfaceHeight(VoxelGrid grid, int x, int z)
        {
            for (int y = grid.Height - 1; y >= 0; y--)
            {
                if (grid.IsSolid(x, y, z))
                    return y;
            }
            return 0;
        }

        private TunnelingEnemy CreateFallbackEnemy()
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.transform.localScale = Vector3.one * 0.8f;
            return go.AddComponent<TunnelingEnemy>();
        }
    }
}