using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TowerDefense.World.Tunneling;

namespace TowerDefense.Defenders
{
    [RequireComponent(typeof(SphereCollider))]
    public class SlowFieldDefender : MonoBehaviour
    {
        [Header("Slow Field Settings")]
        [SerializeField] private float effectRadius = 4f;
        [SerializeField] private float durationSeconds = 5f;
        
        [SerializeField] private float slowedMoveInterval = 0.8f;

        private SphereCollider _sphereCollider;
        private HashSet<TunnelingEnemy> _affectedEnemies = new HashSet<TunnelingEnemy>();

        private void Awake()
        {
            _sphereCollider = GetComponent<SphereCollider>();
            _sphereCollider.isTrigger = true;
            _sphereCollider.radius = effectRadius;
        }

        private void Start()
        {
            StartCoroutine(LifetimeRoutine());
        }

        private void OnTriggerEnter(Collider other)
        {
            TunnelingEnemy enemy = other.GetComponentInParent<TunnelingEnemy>();
            if (enemy != null && !_affectedEnemies.Contains(enemy))
            {
                _affectedEnemies.Add(enemy);
                enemy.ApplySlowEffect(slowedMoveInterval);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            TunnelingEnemy enemy = other.GetComponentInParent<TunnelingEnemy>();
            if (enemy != null && _affectedEnemies.Contains(enemy))
            {
                _affectedEnemies.Remove(enemy);
                enemy.RemoveSlowEffect();
            }
        }

        private IEnumerator LifetimeRoutine()
        {
            yield return new WaitForSeconds(durationSeconds);

            foreach (var enemy in _affectedEnemies)
            {
                if (enemy != null)
                {
                    enemy.RemoveSlowEffect();
                }
            }
            _affectedEnemies.Clear();

            Destroy(gameObject);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0f, 0.7f, 1f, 0.3f);
            Gizmos.DrawSphere(transform.position, effectRadius);
        }
    }
}