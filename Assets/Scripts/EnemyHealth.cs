using UnityEngine;

namespace TowerDefense.World.Tunneling
{
    public class EnemyHealth : MonoBehaviour
    {
        private TunnelingEnemy _tunnelingEnemy;

        private void Awake()
        {
            _tunnelingEnemy = GetComponent<TunnelingEnemy>();
        }

        public void TakeDamage(float amount)
        {
            if (_tunnelingEnemy != null)
            {
                _tunnelingEnemy.TakeDamage(amount);
            }
        }
    }
}