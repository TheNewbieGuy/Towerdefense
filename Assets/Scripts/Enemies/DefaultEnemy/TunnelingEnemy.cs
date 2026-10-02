using System.Collections;
using UnityEngine;
using TowerDefense.Core;
using TowerDefense.Waves;
using TowerDefense.World.Data;
using TowerDefense.World.Generation;
using TowerDefense.Towers;

namespace TowerDefense.World.Tunneling
{
    public enum EnemyTargetType
    {
        Core,
        NearestTurret
    }

    public class TunnelingEnemy : MonoBehaviour
    {
        [Header("Enemy Health & Combat")]
        [SerializeField] private float maxHealth = 50f;
        [SerializeField] private float attackDamage = 10f;
        [SerializeField] private float attackIntervalSeconds = 1.5f;

        [Header("Targeting Logic")]
        [SerializeField] private EnemyTargetType targetType = EnemyTargetType.Core;
        [SerializeField] private bool canMergeTunnels = true;
        [SerializeField] private LayerMask obstacleLayers;
        [SerializeField] private EnemyTunnelingProfile defaultProfile;

        [SerializeField] private float _currentHealth;
        private bool _isDead;

        private WorldGenerator _worldGenerator;
        private VoxelGrid _grid;
        private EnemyTunnelingProfile _profile;
        private TunnelNetwork _tunnelNetwork;
        public float CurrentHealth => _currentHealth;
        public float MaxHealth => maxHealth;
        private Vector2Int _currentVoxel;
        private Vector2Int _targetVoxel;
        private int _lockedZ;

        private float _noiseSeed;
        private bool _isTunnelling;

        private TunnelPath _myPath;
        private bool _isFollowingTunnel;
        private TunnelPath _followedPath;
        private int _followIndex;

        private DefenceCore _targetCore;
        private TurretHealth _targetTurret;
        private Vector2Int _lastMoveDirection = Vector2Int.right;
        private bool _movingRight = true;
        private float? _forcedMoveInterval = null;

        public void ApplySlowEffect(float fixedMoveInterval)
        {
            _forcedMoveInterval = fixedMoveInterval;
        }

        public void RemoveSlowEffect()
        {
            _forcedMoveInterval = null;
        }

        public float GetCurrentMoveInterval()
        {
            if (_forcedMoveInterval.HasValue)
            {
                return _forcedMoveInterval.Value;
            }
            return _profile != null ? _profile.moveIntervalSeconds : 0.2f;
        }

        public void SetStats(float hp, float dmg, float attackInterval, EnemyTargetType type, bool allowMerging = true)
        {
            maxHealth = hp;
            _currentHealth = hp; 
            attackDamage = dmg;
            attackIntervalSeconds = attackInterval;
            targetType = type;
            canMergeTunnels = allowMerging && (targetType != EnemyTargetType.NearestTurret);
        }

        private void Awake()
        {
            _currentHealth = maxHealth;
        }

        public void TakeDamage(float damage)
        {
            if (_isDead) return;

            _currentHealth -= damage;
            Debug.Log($"[{gameObject.name}] Enemy taking damage! Current HP: {_currentHealth}/{maxHealth}", this);

            if (_currentHealth <= 0f)
            {
                Die();
            }
        }

        private void Die()
        {
            if (_isDead) return;
            _isDead = true;
            _isTunnelling = false;

            if (_targetTurret != null)
            {
                _targetTurret.ReleaseClaim(this);
                _targetTurret = null;
            }

            if (TryGetComponent<EnemyDeathNotifier>(out var notifier))
            {
                notifier.NotifyDied();
            }

            WaveManager waveManager = FindFirstObjectByType<WaveManager>();
            if (waveManager != null)
            {
                waveManager.ReportEnemyDeath();
            }

            Destroy(gameObject);
        }

        private void OnTriggerEnter(Collider other)
        {
            CheckAndHandleImpact(other.gameObject);
        }

        private void OnCollisionEnter(Collision collision)
        {
            CheckAndHandleImpact(collision.gameObject);
        }

        private void CheckAndHandleImpact(GameObject targetObject)
        {
            if (_isDead) return;

            TurretHealth turret = targetObject.GetComponentInParent<TurretHealth>();
            if (turret != null && !turret.IsDestroyed)
            {
                if (targetType == EnemyTargetType.NearestTurret)
                {
                    ExplodeOnTurret(turret);
                }
                else
                {
                    _targetTurret = turret;
                }
                return;
            }

            if (targetObject.TryGetComponent<DefenceCore>(out var core))
            {
                if (core != null && !core.IsDestroyed)
                {
                    core.TakeDamage(attackDamage);
                }
                Die();
            }
        }

        private void ExplodeOnTurret(TurretHealth turret)
        {
            if (_isDead) return;

            if (turret != null && !turret.IsDestroyed)
            {
                Debug.Log($"[Hunter Impact] '{gameObject.name}' exploded on '{turret.gameObject.name}', dealing {attackDamage} damage!");
                turret.TakeDamage(attackDamage);
            }

            Die();
        }

        public void Initialize(
            WorldGenerator generator,
            Vector3Int startVoxel,
            Vector3Int targetVoxel,
            TunnelNetwork tunnelNetwork,
            EnemyTunnelingProfile profile = null)
        {
            CommonSetup(generator, startVoxel, targetVoxel, tunnelNetwork, profile);
            CarveAt(_currentVoxel);
            StartTunnelling();
        }

        public void InitializeFollowing(
            WorldGenerator generator,
            Vector3Int startVoxel,
            Vector3Int targetVoxel,
            TunnelNetwork tunnelNetwork,
            TunnelPath pathToFollow,
            int startIndex,
            EnemyTunnelingProfile profile = null)
        {
            CommonSetup(generator, startVoxel, targetVoxel, tunnelNetwork, profile);
            
            if (CanMerge())
            {
                BeginFollowing(pathToFollow, startIndex);
            }

            CarveAt(_currentVoxel);
            StartTunnelling();
        }

        private void CommonSetup(
            WorldGenerator generator,
            Vector3Int startVoxel,
            Vector3Int targetVoxel,
            TunnelNetwork tunnelNetwork,
            EnemyTunnelingProfile profile)
        {
            _worldGenerator = generator;
            _grid = generator.Grid;
            _tunnelNetwork = tunnelNetwork;

            _currentVoxel = new Vector2Int(startVoxel.x, startVoxel.y);
            _lockedZ = startVoxel.z;
            _noiseSeed = Random.Range(0f, 1000f);
            _profile = ResolveProfile(profile);

            int centerX = _grid.Width / 2;
            _movingRight = _currentVoxel.x < centerX;
            _lastMoveDirection = new Vector2Int(_movingRight ? 1 : -1, 0);

            UpdateTargetVoxel();

            _myPath = _tunnelNetwork.RegisterNewPath();
            _myPath.Voxels.Add(_currentVoxel);

            transform.position = _worldGenerator.VoxelToWorld(CurrentVoxel3D());
        }

        private bool CanMerge()
        {
            if (!canMergeTunnels) return false;
            if (_profile != null && !_profile.allowTunnelMerging) return false;
            return true;
        }

        private void UpdateTargetVoxel()
        {
            if (targetType == EnemyTargetType.NearestTurret)
            {
                Vector3Int turretTarget = FindNearestValidTurretVoxel();
                _targetVoxel = new Vector2Int(turretTarget.x, turretTarget.y);
            }
            else
            {
                int centerX = _grid.Width / 2;
                _targetVoxel = new Vector2Int(centerX, _currentVoxel.y);
                _targetCore = FindFirstObjectByType<DefenceCore>();
            }
        }

        private Vector3Int FindNearestValidTurretVoxel()
        {
            TurretHealth[] turrets = FindObjectsByType<TurretHealth>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);

            TurretHealth closest = null;
            float closestDist = float.MaxValue;
            Vector3 myPos = transform.position;

            foreach (var turret in turrets)
            {
                if (turret == null || turret.IsDestroyed) continue;

                if (turret.ClaimedBy != null && turret.ClaimedBy != this)
                    continue;

                float dist = Vector3.Distance(myPos, turret.transform.position);

                if (dist < closestDist)
                {
                    closestDist = dist;
                    closest = turret;
                }
            }

            if (_targetTurret != null && _targetTurret != closest)
            {
                _targetTurret.ReleaseClaim(this);
            }

            if (closest != null && closest.TryClaim(this))
            {
                _targetTurret = closest;

                float voxelSize = _worldGenerator.Config.voxelSize;
                Vector3 turretPos = closest.transform.position;

                int vx = Mathf.Clamp(Mathf.FloorToInt(turretPos.x / voxelSize), 0, _grid.Width - 1);
                int vy = Mathf.Clamp(Mathf.FloorToInt(turretPos.y / voxelSize), 0, _grid.Height - 1);
                int vz = Mathf.Clamp(Mathf.FloorToInt(turretPos.z / voxelSize), 0, _grid.Depth - 1);

                return new Vector3Int(vx, vy, vz);
            }

            _targetTurret = null;
            _targetCore = FindFirstObjectByType<DefenceCore>();
            int coreX = _grid.Width / 2;
            return new Vector3Int(coreX, _currentVoxel.y, _lockedZ);
        }

        private EnemyTunnelingProfile ResolveProfile(EnemyTunnelingProfile overrideProfile)
        {
            if (overrideProfile != null) return overrideProfile;
            if (defaultProfile != null) return defaultProfile;

            return ScriptableObject.CreateInstance<EnemyTunnelingProfile>();
        }

        private void StartTunnelling()
        {
            if (_isTunnelling) return;
            _isTunnelling = true;
            StartCoroutine(TunnelLoop());
        }

        private IEnumerator TunnelLoop()
        {
            while (!_isDead)
            {
                if (targetType == EnemyTargetType.NearestTurret)
                {
                    UpdateTargetVoxel();
                }

                if (_targetTurret == null && HasReachedCore())
                {
                    yield return StartCoroutine(AttackCoreRoutine());
                    yield break;
                }

                if (_targetTurret != null && !_targetTurret.IsDestroyed && HasReachedTurret())
                {
                    if (targetType == EnemyTargetType.NearestTurret)
                    {
                        ExplodeOnTurret(_targetTurret);
                        yield break;
                    }
                    else
                    {
                        yield return StartCoroutine(AttackTurretRoutine());
                        _targetTurret = null;
                    }
                }

                if (IsPathBlockedAhead(out bool hitTarget))
                {
                    if (hitTarget)
                    {
                        if (_targetTurret != null)
                        {
                            if (targetType == EnemyTargetType.NearestTurret)
                            {
                                ExplodeOnTurret(_targetTurret);
                                yield break;
                            }
                            else
                            {
                                yield return StartCoroutine(AttackTurretRoutine());
                                _targetTurret = null;
                            }
                        }
                        else if (_targetCore != null)
                        {
                            yield return StartCoroutine(AttackCoreRoutine());
                            yield break;
                        }
                    }

                    yield return new WaitForSeconds(GetCurrentMoveInterval());
                    continue;
                }

                Step();
        
                yield return new WaitForSeconds(GetCurrentMoveInterval());
            }
        }

        private bool HasReachedCore()
        {
            if (_targetCore == null || _targetCore.IsDestroyed) return false;

            float voxelSize = _worldGenerator.Config.voxelSize;
            float dx = Mathf.Abs(transform.position.x - _targetCore.transform.position.x);

            return dx <= _targetCore.Radius + voxelSize * 0.5f + 0.01f;
        }

        private bool HasReachedTurret()
        {
            if (_targetTurret == null || _targetTurret.IsDestroyed) return false;

            float voxelSize = _worldGenerator.Config.voxelSize;
            float dist = Vector3.Distance(transform.position, _targetTurret.transform.position);

            return dist <= voxelSize * 1.5f;
        }

        private bool IsPathBlockedAhead(out bool hitTarget)
        {
            hitTarget = false;
            float checkDistance = _worldGenerator.Config.voxelSize * 1.5f;
            Vector3 rayOrigin = transform.position;
            Vector3 rayDirection = new Vector3(_lastMoveDirection.x, _lastMoveDirection.y, 0f).normalized;

            if (Physics.Raycast(rayOrigin, rayDirection, out RaycastHit hit, checkDistance, obstacleLayers))
            {
                TurretHealth turret = hit.collider.GetComponentInParent<TurretHealth>();
                if (turret != null && !turret.IsDestroyed)
                {
                    _targetTurret = turret;
                    hitTarget = true;
                    return true;
                }

                if (hit.collider.TryGetComponent<DefenceCore>(out DefenceCore core))
                {
                    _targetCore = core;
                    hitTarget = true;
                    return true;
                }

                if (hit.collider.TryGetComponent<TunnelingEnemy>(out _))
                {
                    return true;
                }
            }

            return false;
        }

        private IEnumerator AttackTurretRoutine()
        {
            var attackWait = new WaitForSeconds(attackIntervalSeconds);
            while (!_isDead && _targetTurret != null && !_targetTurret.IsDestroyed)
            {
                _targetTurret.TakeDamage(attackDamage);
                yield return attackWait;
            }
        }

        private IEnumerator AttackCoreRoutine()
        {
            var attackWait = new WaitForSeconds(attackIntervalSeconds);
            while (!_isDead && _targetCore != null && !_targetCore.IsDestroyed)
            {
                _targetCore.TakeDamage(attackDamage);
                yield return attackWait;
            }
            Die();
        }

        private void Step()
        {
            if (_isFollowingTunnel && CanMerge())
            {
                StepFollowing();
            }
            else
            {
                _isFollowingTunnel = false;
                StepDigging();
            }
        }

        private void StepFollowing()
        {
            if (_followedPath == null || _followIndex >= _followedPath.Voxels.Count)
            {
                _isFollowingTunnel = false;
                _myPath.IsFollowing = false;
                StepDigging();
                return;
            }

            Vector2Int nextVoxel = _followedPath.Voxels[_followIndex];
            _followIndex++;

            MoveTo(nextVoxel);
        }

        private void StepDigging()
        {
            if (CanMerge() && Random.value < _profile.tunnelSeekingChance)
            {
                if (_tunnelNetwork.TryFindNearbyPath(
                    _currentVoxel, _myPath.Id, _profile.tunnelSeekingDetectionRadius,
                    out TunnelPath seekTarget, out int seekIndex))
                {
                    if (!IsTooCloseToPathOwner(seekTarget, _currentVoxel))
                    {
                        BeginFollowing(seekTarget, seekIndex);
                        return;
                    }
                }
            }

            Vector2 steering = CalculateSteeringDirection();
            Vector2Int stepOffset = DirectionToVoxelStep(steering);

            if (stepOffset == Vector2Int.zero)
            {
                stepOffset = DirectionToVoxelStep(_targetVoxel - _currentVoxel);
            }

            Vector2Int intendedVoxel = _currentVoxel + stepOffset;

            if (CanMerge() && TryHandleTunnelInteraction(intendedVoxel))
                return;

            intendedVoxel.x = Mathf.Clamp(intendedVoxel.x, 0, _grid.Width - 1);
            intendedVoxel.y = Mathf.Clamp(intendedVoxel.y, 0, _grid.Height - 1);

            MoveTo(intendedVoxel);
        }

        private bool TryHandleTunnelInteraction(Vector2Int intendedVoxel)
        {
            bool wouldLeaveGrid =
                intendedVoxel.x < 0 || intendedVoxel.x >= _grid.Width ||
                intendedVoxel.y < 0 || intendedVoxel.y >= _grid.Height;

            bool wouldHitOtherTunnel = _tunnelNetwork.IsPartOfOtherPath(
                intendedVoxel, _myPath.Id, out TunnelPath collidedPath, out int collidedIndex);

            if (wouldLeaveGrid || wouldHitOtherTunnel)
            {
                if (wouldHitOtherTunnel)
                {
                    if (IsTooCloseToPathOwner(collidedPath, _currentVoxel)) return true;
                    BeginFollowing(collidedPath, collidedIndex);
                    return true;
                }

                if (_tunnelNetwork.TryFindNearbyPath(_currentVoxel, _myPath.Id, _profile.forcedMergeDetectionRadius, out TunnelPath nearbyPath, out int nearbyIndex))
                {
                    if (IsTooCloseToPathOwner(nearbyPath, _currentVoxel)) return true;
                    BeginFollowing(nearbyPath, nearbyIndex);
                    return true;
                }

                return true;
            }

            if (_tunnelNetwork.TryFindNearbyPath(_currentVoxel, _myPath.Id, _profile.opportunisticMergeDetectionRadius, out TunnelPath nearbyOpportunity, out int nearbyOpportunityIndex))
            {
                bool tooClose = IsTooCloseToPathOwner(nearbyOpportunity, _currentVoxel);
                if (!tooClose && Random.value < _profile.mergeChance)
                {
                    BeginFollowing(nearbyOpportunity, nearbyOpportunityIndex);
                    return true;
                }
            }

            return false;
        }

        private bool IsTooCloseToPathOwner(TunnelPath otherPath, Vector2Int myPosition)
        {
            if (otherPath.Voxels.Count == 0) return false;
            Vector2Int ownerCurrentPosition = otherPath.Voxels[otherPath.Voxels.Count - 1];
            return Vector2.Distance(myPosition, ownerCurrentPosition) < _profile.minMergeSeparation;
        }

        private void BeginFollowing(TunnelPath pathToFollow, int atIndex)
        {
            if (!CanMerge()) return;

            _isFollowingTunnel = true;
            _myPath.IsFollowing = true;
            _followedPath = pathToFollow;
            _followIndex = Mathf.Clamp(atIndex + 1, 0, pathToFollow.Voxels.Count);
        }

        private void MoveTo(Vector2Int voxel)
        {
            Vector2Int moveDelta = voxel - _currentVoxel;
            if (moveDelta != Vector2Int.zero)
            {
                _lastMoveDirection = moveDelta;
            }

            CarveAt(voxel);
            _currentVoxel = voxel;
            _myPath.Voxels.Add(_currentVoxel);
            transform.position = _worldGenerator.VoxelToWorld(CurrentVoxel3D());
        }

        private Vector2 CalculateSteeringDirection()
        {
            Vector2 towardTarget = ((Vector2)(_targetVoxel - _currentVoxel)).normalized;
            Vector2 gravity = Vector2.down;
            Vector2 curveNoise = CalculateCurveNoise();
            Vector2 floorRepulsion = CalculateFloorRepulsion();

            return towardTarget * _profile.targetPullWeight
                 + gravity * _profile.gravityWeight
                 + curveNoise * _profile.curveNoiseWeight
                 + floorRepulsion * _profile.floorRepulsionWeight;
        }

        private Vector2 CalculateFloorRepulsion()
        {
            if (_profile.floorCushion <= 0f) return Vector2.zero;

            float distanceFromFloor = _currentVoxel.y;
            if (distanceFromFloor >= _profile.floorCushion) return Vector2.zero;

            float strength = 1f - (distanceFromFloor / _profile.floorCushion);
            return Vector2.up * strength;
        }

        private Vector2 CalculateCurveNoise()
        {
            float t = Time.time * _profile.curveNoiseSpeed;
            float noiseY = Mathf.PerlinNoise(_noiseSeed, t) * 2f - 1f;
            return new Vector2(0f, noiseY);
        }

        private Vector2Int DirectionToVoxelStep(Vector2 direction)
        {
            if (direction.sqrMagnitude < 0.0001f) return Vector2Int.zero;
            return new Vector2Int(SignStep(direction.x), SignStep(direction.y));
        }

        private int SignStep(float value)
        {
            const float deadZone = 0.25f;
            if (value > deadZone) return 1;
            if (value < -deadZone) return -1;
            return 0;
        }

        private void CarveAt(Vector2Int voxel)
        {
            if (_grid == null) return;

            float radiusXY = _profile != null ? _profile.tunnelRadius : 1f;
            float radiusZ = _profile != null ? _profile.tunnelDepth : 3f;
            TunnelCarver.CarveRoundedVolume(_grid, voxel.x, voxel.y, _lockedZ, radiusXY, radiusZ);
            _worldGenerator.NotifyTerrainModified();
        }

        private Vector3Int CurrentVoxel3D()
        {
            return new Vector3Int(_currentVoxel.x, _currentVoxel.y, _lockedZ);
        }

        private void OnDrawGizmos()
        {
            if (!Application.isPlaying) return;

            if (_targetTurret != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(transform.position, _targetTurret.transform.position);
                Gizmos.DrawWireSphere(_targetTurret.transform.position, 0.5f);
            }
            else if (_targetCore != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(transform.position, _targetCore.transform.position);
            }
        }
    }
}