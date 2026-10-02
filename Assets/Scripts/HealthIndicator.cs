using UnityEngine;
using TowerDefense.Towers;

namespace TowerDefense.World.Tunneling
{
    [RequireComponent(typeof(Renderer))]
    public class HealthCubeIndicator : MonoBehaviour
    {
        [Header("Color Settings")]
        [SerializeField] private Color fullHealthColor = Color.green;
        [SerializeField] private Color zeroHealthColor = Color.red;

        private Renderer _renderer;
        private MaterialPropertyBlock _propBlock;

        private TurretHealth _turretHealth;
        private TunnelingEnemy _enemyHealth;

        private void Awake()
        {
            _renderer = GetComponent<Renderer>();
            _propBlock = new MaterialPropertyBlock();

            
            _turretHealth = GetComponentInParent<TurretHealth>();
            _enemyHealth = GetComponentInParent<TunnelingEnemy>();
        }

        private void Update()
        {
            float healthPercent = GetHealthPercent();
            
            Color currentColor = Color.Lerp(zeroHealthColor, fullHealthColor, healthPercent);

            _renderer.GetPropertyBlock(_propBlock);
            _propBlock.SetColor("_Color", currentColor); 
            _propBlock.SetColor("_BaseColor", currentColor); 
            _renderer.SetPropertyBlock(_propBlock);
        }

        private float GetHealthPercent()
        {
            if (_turretHealth != null && _turretHealth.MaxHealth > 0f)
            {
                return Mathf.Clamp01(_turretHealth.CurrentHealth / _turretHealth.MaxHealth);
            }

            if (_enemyHealth != null && _enemyHealth.MaxHealth > 0f)
            {
                return Mathf.Clamp01(_enemyHealth.CurrentHealth / _enemyHealth.MaxHealth);
            }

            return 1f;
        }
    }
}