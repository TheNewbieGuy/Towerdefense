using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TowerDefense.Waves;

namespace TowerDefense.Towers
{
    [RequireComponent(typeof(TurretHealth))]
    public class ResourceGeneratorDefender : MonoBehaviour
    {
        [Header("Resource Economy (Sunflower Engine)")]
        [SerializeField] private float generationIntervalSeconds = 2.5f;

        [SerializeField] private int goldGeneratedPerCycle = 10;

        [Header("Cap Settings")]
        [SerializeField] private int maxActiveGeneratorsCap = 5;

        private static readonly List<ResourceGeneratorDefender> ActiveGeneratorsRegistry = new List<ResourceGeneratorDefender>();

        private WaveManager _waveManager;
        private TurretHealth _turretHealth;

        private void Awake()
        {
            _waveManager = FindFirstObjectByType<WaveManager>();
            _turretHealth = GetComponent<TurretHealth>();
        }

        private void OnEnable()
        {
            if (!ActiveGeneratorsRegistry.Contains(this))
            {
                ActiveGeneratorsRegistry.Add(this);
            }
        }

        private void OnDisable()
        {
            if (ActiveGeneratorsRegistry.Contains(this))
            {
                ActiveGeneratorsRegistry.Remove(this);
            }
        }

        private void Start()
        {
            StartCoroutine(GenerateGoldRoutine());
        }

        public bool IsGeneratingGold()
        {
            int index = ActiveGeneratorsRegistry.IndexOf(this);
            return index >= 0 && index < maxActiveGeneratorsCap;
        }

        private IEnumerator GenerateGoldRoutine()
        {
            var wait = new WaitForSeconds(generationIntervalSeconds);

            while (_turretHealth == null || !_turretHealth.IsDestroyed)
            {
                yield return wait;

                if (IsGeneratingGold() && _waveManager != null)
                {
                    _waveManager.AddGold(goldGeneratedPerCycle);
                }
                else if (!IsGeneratingGold())
                {
                }
            }
        }
    }
}