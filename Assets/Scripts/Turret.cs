using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TowerDefense.World.Tunneling;

namespace TowerDefense.Towers
{
    public class Turret : MonoBehaviour
    {
        [Header("Targeting Mode")]
        [SerializeField] private bool targetOnlyClosest = true;
        [SerializeField] private float range = 8f;
        [SerializeField] private LayerMask enemyLayer;

        [Header("Burst Interval Settings")]
        [SerializeField] private float laserActiveDuration = 2.0f;
        [SerializeField] private float laserInactiveDuration = 1.5f;

        [Header("Damage Settings")]
        [SerializeField] private float damagePerTick = 10f;
        [SerializeField] private float damageInterval = 0.25f;

        [Header("Laser Setup")]
        [SerializeField] private Transform firePoint;
        [SerializeField] private Material laserMaterial;
        [SerializeField] private float laserWidth = 0.15f;
        [SerializeField] private float cameraZOffset = -0.5f; 

        private LineRenderer _myLineRenderer;
        private List<EnemyHealth> _currentTargets = new List<EnemyHealth>();
        private bool _isLaserActive = false;

        private void Awake()
        {
            _myLineRenderer = GetComponent<LineRenderer>();
            if (_myLineRenderer == null)
            {
                _myLineRenderer = gameObject.AddComponent<LineRenderer>();
            }

            _myLineRenderer.positionCount = 2;
            _myLineRenderer.startWidth = laserWidth;
            _myLineRenderer.endWidth = laserWidth;
            _myLineRenderer.enabled = false;

            if (laserMaterial != null)
            {
                _myLineRenderer.material = laserMaterial;
            }
        }

        private void Start()
        {
            StartCoroutine(LaserBurstCycleRoutine());
        }

        private void Update()
        {
            FindTargets();

            if (_isLaserActive && _currentTargets.Count > 0)
            {
                UpdateLaserVisuals();
            }
            else
            {
                DisableLaser();
            }
        }

        private void FindTargets()
        {
            _currentTargets.Clear();

            Collider[] hits = Physics.OverlapSphere(transform.position, range);
            if (hits.Length == 0) return;

            float nearestDistance = float.MaxValue;
            EnemyHealth nearestEnemy = null;

            foreach (var hit in hits)
            {
                if (hit.GetComponentInParent<EnemyHealth>() is EnemyHealth enemy)
                {
                    Vector2 turretPos2D = new Vector2(transform.position.x, transform.position.y);
                    Vector2 enemyPos2D = new Vector2(enemy.transform.position.x, enemy.transform.position.y);
                    float dist2D = Vector2.Distance(turretPos2D, enemyPos2D);

                    if (dist2D <= range)
                    {
                        if (dist2D < nearestDistance)
                        {
                            nearestDistance = dist2D;
                            nearestEnemy = enemy;
                        }
                    }
                }
            }

            if (nearestEnemy != null)
            {
                _currentTargets.Add(nearestEnemy);
            }
        }

        private IEnumerator LaserBurstCycleRoutine()
        {
            var activeWait = new WaitForSeconds(laserActiveDuration);
            var inactiveWait = new WaitForSeconds(laserInactiveDuration);

            while (true)
            {
                _isLaserActive = false;
                DisableLaser();
                yield return inactiveWait;

                _isLaserActive = true;
                Coroutine damageCoroutine = StartCoroutine(DamageIntervalRoutine());
                
                yield return activeWait;

                if (damageCoroutine != null)
                {
                    StopCoroutine(damageCoroutine);
                }
            }
        }

        private IEnumerator DamageIntervalRoutine()
        {
            var tickWait = new WaitForSeconds(damageInterval);

            while (_isLaserActive)
            {
                if (_currentTargets.Count > 0 && _currentTargets[0] != null)
                {
                    _currentTargets[0].TakeDamage(damagePerTick);
                }

                yield return tickWait;
            }
        }

        private void UpdateLaserVisuals()
        {
            if (_currentTargets.Count == 0 || _currentTargets[0] == null)
            {
                DisableLaser();
                return;
            }

            Vector3 origin = firePoint != null ? firePoint.position : transform.position;
            Vector3 targetPos = _currentTargets[0].transform.position;

            Vector3 offsetOrigin = origin + new Vector3(0f, 0f, cameraZOffset);
            Vector3 offsetTarget = targetPos + new Vector3(0f, 0f, cameraZOffset);

            _myLineRenderer.enabled = true;
            _myLineRenderer.SetPosition(0, offsetOrigin);
            _myLineRenderer.SetPosition(1, offsetTarget);
        }

        private void DisableLaser()
        {
            if (_myLineRenderer != null)
            {
                _myLineRenderer.enabled = false;
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, range);
        }
    }
}