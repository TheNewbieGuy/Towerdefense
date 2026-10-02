using UnityEngine;
using UnityEngine.UI;
using TowerDefense.Towers;
using TowerDefense.Waves;

namespace TowerDefense.UI
{
    [RequireComponent(typeof(Slider))]
    public class DefenderCostBar : MonoBehaviour
    {
        [Header("Defender Assignment")]
        [SerializeField] private DefenderType defenderType;

        [Header("Manager References")]
        [SerializeField] private WaveManager waveManager;
        [SerializeField] private TurretPlacementManager placementManager;

        private Slider _slider;

        private void Awake()
        {
            _slider = GetComponent<Slider>();

            if (waveManager == null) waveManager = FindFirstObjectByType<WaveManager>();
            if (placementManager == null) placementManager = FindFirstObjectByType<TurretPlacementManager>();
        }

        private void Start()
        {
            if (_slider != null)
            {
                _slider.minValue = 0f;
                _slider.maxValue = 1f;
            }
        }

        [Header("Optional Visual Styling")]
        [SerializeField] private Image fillImage;
        [SerializeField] private Color chargingColor = new Color(0.8f, 0.8f, 0.2f, 1f); 
        [SerializeField] private Color readyColor = new Color(0.2f, 0.9f, 0.2f, 1f);    

        private void Update()
        {
            if (_slider == null || waveManager == null) return;

            int cost = GetDefenderCost(defenderType);
            if (cost <= 0) return;

            float ratio = Mathf.Clamp01((float)waveManager.CurrentGold / cost);
            _slider.value = ratio;

            if (fillImage != null)
            {
                fillImage.color = ratio >= 1f ? readyColor : chargingColor;
            }
        }

        private int GetDefenderCost(DefenderType type)
        {
            return type switch
            {
                DefenderType.StandardTurret => 175,      
                DefenderType.Wall => 25,
                DefenderType.SlowField => 75,
                DefenderType.ResourceGenerator => 50,
                _ => 100
            };
        }
    }
}