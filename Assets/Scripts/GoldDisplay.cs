using UnityEngine;
using TMPro;
using TowerDefense.Waves;

namespace TowerDefense.UI
{
    public class GoldUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI goldText;

        [Header("Formatting")]
        [SerializeField] private string suffix = "g";

        [Header("Manager Reference")]
        [SerializeField] private WaveManager waveManager;

        private int _lastDisplayedGold = -1;

        private void Awake()
        {
            if (goldText == null)
            {
                goldText = GetComponent<TextMeshProUGUI>();
            }

            if (waveManager == null)
            {
                waveManager = FindFirstObjectByType<WaveManager>();
            }
        }

        private void Update()
        {
            if (waveManager == null || goldText == null) return;

            int currentGold = waveManager.CurrentGold;
            if (currentGold != _lastDisplayedGold)
            {
                _lastDisplayedGold = currentGold;
                goldText.text = $"{currentGold}{suffix}";
            }
        }
    }
}