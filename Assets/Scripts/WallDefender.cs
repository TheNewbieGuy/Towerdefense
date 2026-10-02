using UnityEngine;
using TowerDefense.Towers;
using TowerDefense.World.Tunneling;

namespace TowerDefense.Defenders
{
    [RequireComponent(typeof(TurretHealth))]
    [RequireComponent(typeof(Collider))]
    public class WallDefender : MonoBehaviour
    {
        private TurretHealth _turretHealth;

        private void Awake()
        {
            _turretHealth = GetComponent<TurretHealth>();
        }

        private void OnTriggerEnter(Collider other)
        {
            HandleEnemyCollision(other.gameObject);
        }

        private void OnCollisionEnter(Collision collision)
        {
            HandleEnemyCollision(collision.gameObject);
        }

        private void HandleEnemyCollision(GameObject enemyObject)
        {
            if (_turretHealth != null && _turretHealth.IsDestroyed) return;

            TunnelingEnemy enemy = enemyObject.GetComponentInParent<TunnelingEnemy>();
            if (enemy == null) return;

        }
    }
}