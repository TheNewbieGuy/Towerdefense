using UnityEngine;
using TowerDefense.World.Tunneling;

namespace TowerDefense.Towers
{
    public class TurretHealth : MonoBehaviour
    {
        [SerializeField] private float maxHealth = 100f;
        [SerializeField] private float _currentHealth;
        private bool _isDestroyed = false;

        public float CurrentHealth => _currentHealth;
        public float MaxHealth => maxHealth;

        public bool IsDestroyed => _isDestroyed;

        public TunnelingEnemy ClaimedBy { get; private set; }

        public bool TryClaim(TunnelingEnemy enemy)
        {
            if (ClaimedBy == null || ClaimedBy == enemy)
            {
                ClaimedBy = enemy;
                return true;
            }
            return false;
        }

        public void ReleaseClaim(TunnelingEnemy enemy)
        {
            if (ClaimedBy == enemy)
            {
                ClaimedBy = null;
            }
        }

        private void Awake()
        {
            _currentHealth = maxHealth;
        }

        public void TakeDamage(float amount)
        {
            if (_isDestroyed) return;

            _currentHealth -= amount;
            Debug.Log($"Turret took {amount} damage! Current HP: {_currentHealth}/{maxHealth}", this);

            if (_currentHealth <= 0)
            {
                DestroyTurret();
            }
        }

        private void DestroyTurret()
        {
            if (_isDestroyed) return;
            _isDestroyed = true;

            Debug.Log("Turret destroyed by enemies!", this);
            Destroy(gameObject);
        }
    }
}