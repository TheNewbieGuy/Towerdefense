using System;
using UnityEngine;
using TowerDefense.World.Data;
using TowerDefense.World.Generation;

namespace TowerDefense.Core
{
    [RequireComponent(typeof(Collider))]
    public class DefenceCore : MonoBehaviour
    {
        [Header("Core Dimensions")]

        [Min(0.1f)]
        [SerializeField] private float radius = 2f;

        [SerializeField] private bool spanFullWorldHeight = true;

        [Header("Placement")]

        [SerializeField] private Vector3 worldOffset = Vector3.zero;

        [Header("Health")]

        [Min(1f)]
        [SerializeField] private float maxHealth = 100f;

        [Header("References")]

        [SerializeField] private WorldGenerator worldGenerator;

        private float _currentHealth;
        private bool _isDestroyed;

        public float MaxHealth => maxHealth;

        public float CurrentHealth => _currentHealth;

        public bool IsDestroyed => _isDestroyed;

        public float Radius => radius;

        public event Action<float, float> OnHealthChanged;

        public event Action OnCoreDestroyed;

        private void Awake()
        {
            if (worldGenerator == null)
            {
                worldGenerator = FindFirstObjectByType<WorldGenerator>();
            }

            ResetHealth();
        }

        private void OnEnable()
        {
            if (worldGenerator != null)
            {
                worldGenerator.OnWorldGenerated += HandleWorldGenerated;
            }
        }

        private void OnDisable()
        {
            if (worldGenerator != null)
            {
                worldGenerator.OnWorldGenerated -= HandleWorldGenerated;
            }
        }

        private void Start()
        {
            if (worldGenerator == null)
            {
                Debug.LogError(
                    $"{name} could not find a WorldGenerator.",
                    this);

                return;
            }

            if (worldGenerator.Grid != null)
            {
                PositionCore(worldGenerator.Grid);
            }
        }

        public void ResetHealth()
        {
            _currentHealth = maxHealth;
            _isDestroyed = false;

            OnHealthChanged?.Invoke(_currentHealth, maxHealth);
        }

        public void TakeDamage(float damage)
        {
            if (_isDestroyed) return;
            if (damage <= 0f) return;

            _currentHealth = Mathf.Max(0f, _currentHealth - damage);
            OnHealthChanged?.Invoke(_currentHealth, maxHealth);

            if (_currentHealth <= 0f)
            {
                HandleDestroyed();
            }
        }

        public void Heal(float amount)
        {
            if (_isDestroyed)
                return;

            if (amount <= 0f)
                return;

            _currentHealth = Mathf.Min(
                maxHealth,
                _currentHealth + amount);

            OnHealthChanged?.Invoke(_currentHealth, maxHealth);
        }

        private void HandleDestroyed()
        {
            if (_isDestroyed)
                return;

            _isDestroyed = true;

            Debug.Log("The Defence Core has been destroyed!", this);

            OnCoreDestroyed?.Invoke();
            Destroy(gameObject);
        }

        private void HandleWorldGenerated(VoxelGrid grid)
        {
            PositionCore(grid);
        }

        private void PositionCore(VoxelGrid grid)
        {
            if (grid == null)
                return;

            float voxelSize = worldGenerator.Config.voxelSize;

            float worldWidth = grid.Width * voxelSize;
            float worldHeight = grid.Height * voxelSize;

            float centerX = worldWidth * 0.5f;
            float centerZ = worldGenerator.CoreZ * voxelSize;

            float coreHeight = spanFullWorldHeight
                ? worldHeight
                : transform.localScale.y * 1f;

            transform.position = new Vector3(
                centerX,
                coreHeight * 1f,
                centerZ) + worldOffset;

            transform.localScale = new Vector3(
                radius * 2f,
                coreHeight * 1f,
                radius * 2f);
        }
    }
}