using UnityEngine;
using UnityEngine.UI;
using TowerDefense.Core;

namespace TowerDefense.UI
{
    [RequireComponent(typeof(Slider))]
    public class CoreHealthBar : MonoBehaviour
    {
        [SerializeField] private DefenceCore core;
        private Slider _slider;

        private void Awake()
        {
            _slider = GetComponent<Slider>();
            
            if (core == null)
            {
                core = FindFirstObjectByType<DefenceCore>();
            }
        }

        private void OnEnable()
        {
            if (core != null)
            {
                core.OnHealthChanged += UpdateHealthBar;
            }
        }

        private void OnDisable()
        {
            if (core != null)
            {
                core.OnHealthChanged -= UpdateHealthBar;
            }
        }

        private void Start()
        {
            if (core != null)
            {
                UpdateHealthBar(core.CurrentHealth, core.MaxHealth);
            }
        }

        private void UpdateHealthBar(float currentHealth, float maxHealth)
        {
            if (_slider != null)
            {
                _slider.value = currentHealth / maxHealth;
            }
        }
    }
}