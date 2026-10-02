using System.Collections;
using UnityEngine;
using TMPro;

namespace TowerDefense.UI
{
    public class WaveBannerUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private TextMeshProUGUI waveText;

        [Header("Timing Settings")]
        [SerializeField] private float fadeDuration = 0.5f;
        [SerializeField] private float holdDuration = 1.0f;

        private void Awake()
        {
            if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
            
            // Ensure invisible and non-blocking at start
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.blocksRaycasts = false;
            }
        }

        public IEnumerator PlayWaveBannerRoutine(int waveNumber)
        {
            if (waveText != null)
            {
                waveText.text = $"WAVE {waveNumber}";
            }

            canvasGroup.blocksRaycasts = true;

            // Fade to Black
            float timer = 0f;
            while (timer < fadeDuration)
            {
                timer += Time.deltaTime;
                canvasGroup.alpha = Mathf.Lerp(0f, 1f, timer / fadeDuration);
                yield return null;
            }
            canvasGroup.alpha = 1f;

            // Hold screen on black with text
            yield return new WaitForSeconds(holdDuration);

            // Fade back in to world
            timer = 0f;
            while (timer < fadeDuration)
            {
                timer += Time.deltaTime;
                canvasGroup.alpha = Mathf.Lerp(1f, 0f, timer / fadeDuration);
                yield return null;
            }
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
        }
    }
}