using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TowerDefense.Core;
using TowerDefense.Towers;
using TowerDefense.World.Data;
using TowerDefense.World.Generation;
using TowerDefense.World.Tunneling;

namespace TowerDefense.Waves
{
    [System.Serializable]
    public struct EnemyTypeConfig
    {
        public string name;
        public TunnelingEnemy prefab;
        public EnemyTunnelingProfile profile;
        public float health;
        public float attackDamage;
        public float attackInterval;
        public EnemyTargetType targetType;
        public int pointCost;
        public int killGoldReward;
        public float baseWeight;
    }

    [RequireComponent(typeof(WorldGenerator))]
    public class WaveManager : MonoBehaviour
    {
        [Header("Enemy Configurations")]
        [SerializeField]
        private EnemyTypeConfig standardEnemy = new EnemyTypeConfig
        {
            name = "Standard Enemy",
            health = 50f,
            attackDamage = 10f,
            attackInterval = 1.5f,
            targetType = EnemyTargetType.Core,
            pointCost = 10,
            killGoldReward = 15,
            baseWeight = 70f
        };

        [SerializeField]
        private EnemyTypeConfig fastEnemy = new EnemyTypeConfig
        {
            name = "Fast Enemy",
            health = 20f,
            attackDamage = 5f,
            attackInterval = 1.0f,
            targetType = EnemyTargetType.Core,
            pointCost = 18,
            killGoldReward = 20,
            baseWeight = 20f
        };

        [SerializeField]
        private EnemyTypeConfig turretHunterEnemy = new EnemyTypeConfig
        {
            name = "Turret Hunter Enemy",
            health = 60f,
            attackDamage = 20f,
            attackInterval = 1.2f,
            targetType = EnemyTargetType.NearestTurret,
            pointCost = 25,
            killGoldReward = 30,
            baseWeight = 10f
        };

        [Header("Economy System (Gold)")]
        [SerializeField] private int startingGold = 150;
        [SerializeField] private int waveCompletionBaseGold = 100;
        [SerializeField] private int maxGoldCap = 1000;
        private int _currentGold;

        [Header("Procedural Wave Budget & Scaling")]
        [SerializeField] private float baseWaveBudget = 40f;
        [SerializeField] private float budgetGrowthMultiplier = 1.15f;
        [SerializeField] private float enemyHealthScalingPerWave = 1.08f;
        [SerializeField] private float baseSpawnInterval = 1.5f;
        [SerializeField] private float minSpawnIntervalFloor = 0.1f;

        [Header("DDA Settings")]
        [SerializeField] private bool enableDDA = true;

        [Space(5)]
        [Min(1)][SerializeField] private int surgeMinWave = 10;
        [SerializeField] private float surgeThreshold = 1.4f;
        [Range(0f, 1f)][SerializeField] private float surgeBudgetBonusRatio = 0.20f;
        [SerializeField] private float surgeHunterWeightBonus = 20f;

        [Space(5)]
        [SerializeField] private float recoveryThreshold = 0.6f;
        [Range(0f, 1f)][SerializeField] private float recoveryBudgetReductionRatio = 0.15f;

        [Header("Performance Rating Multipliers")]
        [SerializeField] private float coreHpWeight = 1.0f;
        [SerializeField] private float defenderSurvivalWeight = 0.5f;
        [SerializeField] private float goldBankWeight = 0.3f;

        [Header("Spawn Position Settings")]
        [SerializeField] private bool randomizeSpawnSide = true;
        [SerializeField] private bool spawnOnLeftSide = true;
        [SerializeField] private int spawnEdgeMargin = 3;

        [Header("Existing Tunnels")]
        [Range(0f, 1f)]
        [SerializeField] private float joinExistingTunnelChance = 0.25f;

        [Header("UI Transition Reference")]
        [SerializeField] private TowerDefense.UI.WaveBannerUI waveBannerUI;

        private WorldGenerator _worldGenerator;
        private TunnelNetwork _tunnelNetwork;
        private DefenceCore _defenceCore;

        private int _currentWave = 0;
        private int _activeEnemiesCount = 0;
        private float _adaptiveBudgetModifier = 0f;
        private bool _isSurgeActive = false;

        private int _totalDefendersPlacedCount = 0;
        private int _activeResourceGeneratorsCount = 0;

        public int CurrentWave => _currentWave;
        public int CurrentGold => _currentGold;
        public TunnelNetwork Network => _tunnelNetwork;

        private void Awake()
        {
            _worldGenerator = GetComponent<WorldGenerator>();
            _defenceCore = FindFirstObjectByType<DefenceCore>();
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
            _currentWave = 0;
            _currentGold = startingGold;
            _activeEnemiesCount = 0;
            _adaptiveBudgetModifier = 0f;
            _isSurgeActive = false;
            _totalDefendersPlacedCount = 0;
            _activeResourceGeneratorsCount = 0;

            StopAllCoroutines();
            StartNextWave();
        }

        public void AddGold(int amount)
        {
            _currentGold = Mathf.Min(maxGoldCap, _currentGold + amount);
        }

        public bool TrySpendGold(int amount)
        {
            if (_currentGold >= amount)
            {
                _currentGold -= amount;
                return true;
            }
            return false;
        }

        public void RegisterPlacedDefender(DefenderType type)
        {
            _totalDefendersPlacedCount++;
            if (type == DefenderType.ResourceGenerator)
            {
                _activeResourceGeneratorsCount++;
            }
        }

        public void StartNextWave()
        {
            StartCoroutine(StartNextWaveRoutine());
        }

        private IEnumerator StartNextWaveRoutine()
        {
            _currentWave++;

            // Grant completion gold for Wave 1+, not on initial start
            if (_currentWave > 1)
            {
                AddGold(waveCompletionBaseGold);
            }

            // Trigger Fade Out -> Show Wave Text -> Fade In Screen
            if (waveBannerUI != null)
            {
                yield return StartCoroutine(waveBannerUI.PlayWaveBannerRoutine(_currentWave));
            }

            float rawBudget = baseWaveBudget * Mathf.Pow(budgetGrowthMultiplier, _currentWave - 1);
            float finalBudget = Mathf.Max(20f, rawBudget + _adaptiveBudgetModifier);

            List<EnemyTypeConfig> spawnList = GenerateWaveCompositionFromBudget(finalBudget);

            _activeEnemiesCount = spawnList.Count;
            Debug.Log($"--- STARTING WAVE {_currentWave} (Budget: {finalBudget:F1}, Surge: {_isSurgeActive}, Enemies: {spawnList.Count}, Gold: {_currentGold}g) ---");

            StartCoroutine(SpawnWaveCoroutine(spawnList));
        }

        private List<EnemyTypeConfig> GenerateWaveCompositionFromBudget(float budget)
        {
            List<EnemyTypeConfig> queue = new List<EnemyTypeConfig>();
            float remainingBudget = budget;

            float stdWeight, fastWeight, hunterWeight;

            if (_currentWave == 1)
            {
                stdWeight = 100f; fastWeight = 0f; hunterWeight = 0f;
            }
            else if (_currentWave <= 4)
            {
                stdWeight = 80f; fastWeight = 20f; hunterWeight = 0f;
            }
            else if (_currentWave <= 8)
            {
                stdWeight = 50f; fastWeight = 30f; hunterWeight = 20f;
            }
            else
            {
                stdWeight = 30f; fastWeight = 30f; hunterWeight = 40f;
            }

            int currentGeneratorsAlive = FindObjectsByType<ResourceGeneratorDefender>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).Length;

            if (currentGeneratorsAlive >= 7)
            {
                hunterWeight += 15f;
                Debug.Log($"[WaveManager] Resource Spike Trigger Active! ({currentGeneratorsAlive} Generators Alive -> +15% Hunter Weight)");
            }

            if (_isSurgeActive && _currentWave > 1)
            {
                hunterWeight += surgeHunterWeightBonus;
                Debug.Log($"[DDA Loop] Aggressive Surge boosting Hunter Spawn Weight by +{surgeHunterWeightBonus}!");
            }

            int safetyCounter = 0;
            while (remainingBudget >= standardEnemy.pointCost && safetyCounter < 500)
            {
                safetyCounter++;
                EnemyTypeConfig selected = PickWeightedEnemyConfig(stdWeight, fastWeight, hunterWeight);

                if (remainingBudget >= selected.pointCost)
                {
                    queue.Add(selected);
                    remainingBudget -= selected.pointCost;
                }
                else
                {
                    queue.Add(standardEnemy);
                    remainingBudget -= standardEnemy.pointCost;
                }
            }

            return queue;
        }

        private EnemyTypeConfig PickWeightedEnemyConfig(float wStd, float wFast, float wHunter)
        {
            float total = wStd + wFast + wHunter;
            if (total <= 0f) return standardEnemy;

            float r = Random.Range(0f, total);
            if (r < wStd) return standardEnemy;
            r -= wStd;

            if (r < wFast) return fastEnemy;
            return turretHunterEnemy;
        }

        private IEnumerator SpawnWaveCoroutine(List<EnemyTypeConfig> spawnList)
        {
            VoxelGrid grid = _worldGenerator.Grid;
            float currentInterval = Mathf.Max(minSpawnIntervalFloor, baseSpawnInterval - (_currentWave * 0.04f));

            for (int i = 0; i < spawnList.Count; i++)
            {
                SpawnOneEnemy(grid, spawnList[i]);

                bool isLastEnemy = i == spawnList.Count - 1;
                if (!isLastEnemy && currentInterval > 0f)
                {
                    yield return new WaitForSeconds(currentInterval);
                }
            }
        }

        private void SpawnOneEnemy(VoxelGrid grid, EnemyTypeConfig config)
        {
            TunnelingEnemy enemy = config.prefab != null
                ? Instantiate(config.prefab)
                : CreateFallbackEnemy();

            enemy.name = config.name;

            float scaledHealth = config.health * Mathf.Pow(enemyHealthScalingPerWave, _currentWave - 1);
            enemy.SetStats(scaledHealth, config.attackDamage, config.attackInterval, config.targetType);

            Vector3Int targetVoxel = _worldGenerator.GetRandomCoreAxisPosition();

            EnemyTunnelingProfile profile = config.profile;

            float spawnChance = profile != null
                ? profile.existingTunnelSpawnChance / 100f
                : joinExistingTunnelChance;

            bool mustJoinTunnel = profile != null && profile.mustSpawnInExistingTunnel;
            bool tryJoinExisting = mustJoinTunnel || (Random.value < spawnChance);

            if (tryJoinExisting && _tunnelNetwork != null && _tunnelNetwork.TryGetRandomStartPoint(out TunnelPath joinPath, out int joinIndex))
            {
                Vector2Int joinVoxel2D = joinPath.Voxels[0];
                Vector3Int joinVoxel = new Vector3Int(joinVoxel2D.x, joinVoxel2D.y, _worldGenerator.CoreZ);
                enemy.InitializeFollowing(_worldGenerator, joinVoxel, targetVoxel, _tunnelNetwork, joinPath, joinIndex, config.profile);
            }
            else
            {
                Vector3Int spawnVoxel = FindEdgeEmbeddedSpawnPosition(grid);
                enemy.Initialize(_worldGenerator, spawnVoxel, targetVoxel, _tunnelNetwork, config.profile);
            }
        }

        private Vector3Int FindEdgeEmbeddedSpawnPosition(VoxelGrid grid)
        {
            bool spawnOnLeft = randomizeSpawnSide ? Random.value < 0.5f : spawnOnLeftSide;
            int x = spawnOnLeft ? spawnEdgeMargin : grid.Width - 1 - spawnEdgeMargin;
            int z = _worldGenerator.CoreZ;

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

        public void ReportEnemyDeath(int goldReward = 15)
        {
            if (this == null || !gameObject.activeInHierarchy) return;

            AddGold(goldReward);
            _activeEnemiesCount--;

            if (_activeEnemiesCount <= 0)
            {
                _activeEnemiesCount = 0;
                StopAllCoroutines();

                EvaluatePerformanceAndAdjustBudget();

                Debug.Log($"Wave {_currentWave} CLEARED! Starting Wave {_currentWave + 1}...");
                StartNextWave();
            }
        }

        private void EvaluatePerformanceAndAdjustBudget()
        {
            if (!enableDDA)
            {
                _adaptiveBudgetModifier = 0f;
                _isSurgeActive = false;
                return;
            }

            if (_defenceCore == null)
            {
                _defenceCore = FindFirstObjectByType<DefenceCore>();
            }

            float coreHpRatio = _defenceCore != null ? (_defenceCore.CurrentHealth / _defenceCore.MaxHealth) : 1f;

            TurretHealth[] activeTurrets = FindObjectsByType<TurretHealth>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            float aliveRatio = _totalDefendersPlacedCount > 0 ? ((float)activeTurrets.Length / _totalDefendersPlacedCount) : 1f;

            float bankRatio = Mathf.Clamp01((float)_currentGold / maxGoldCap);

            // Performance Rating P formula using customizable Inspector weights
            float performanceP = (coreHpRatio * coreHpWeight) + (aliveRatio * defenderSurvivalWeight) + (bankRatio * goldBankWeight);
            Debug.Log($"[DDA Loop] Wave Performance Rating P = {performanceP:F2} (CoreHP: {coreHpRatio:F2}, AliveRatio: {aliveRatio:F2}, BankRatio: {bankRatio:F2})");

            float baseNextBudget = baseWaveBudget * Mathf.Pow(budgetGrowthMultiplier, _currentWave);

            if (performanceP >= surgeThreshold)
            {
                if (_currentWave >= surgeMinWave)
                {
                    _isSurgeActive = true;
                    _adaptiveBudgetModifier = baseNextBudget * surgeBudgetBonusRatio;  
                    Debug.Log($"[DDA Loop] Aggressive Surge Triggered! (P: {performanceP:F2} >= {surgeThreshold}, Wave: {_currentWave} >= {surgeMinWave}) -> Next Wave Budget +{surgeBudgetBonusRatio * 100f:F0}% ({_adaptiveBudgetModifier:+0.0;-0.0})");
                }
                else
                {
                    _isSurgeActive = false;
                    _adaptiveBudgetModifier = 0f;
                    Debug.Log($"[DDA Loop] Performance high (P: {performanceP:F2} >= {surgeThreshold}), but Surge locked until Wave {surgeMinWave} (Current: Wave {_currentWave}).");
                }
            }
            else if (performanceP <= recoveryThreshold)
            {
                _isSurgeActive = false;
                _adaptiveBudgetModifier = -baseNextBudget * recoveryBudgetReductionRatio; 
                Debug.Log($"[DDA Loop] ️ Recovery Assistance Triggered! (P: {performanceP:F2} <= {recoveryThreshold}) -> Next Wave Budget -{recoveryBudgetReductionRatio * 100f:F0}% ({_adaptiveBudgetModifier:+0.0;-0.0})");
            }
            else
            {
                _isSurgeActive = false;
                _adaptiveBudgetModifier = 0f;
                Debug.Log($"[DDA Loop] Normal Scaling (P: {performanceP:F2} within balanced bounds).");
            }
        }
    }
}