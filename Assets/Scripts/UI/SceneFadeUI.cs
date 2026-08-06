using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace BeyProject.UI
{
    /// <summary>
    /// Persistent full-screen black overlay used for room/battle scene transitions.
    /// Uses unscaled time so a fade started right as Pause sets Time.timeScale = 0
    /// (e.g. quitting to the main menu) still completes normally.
    /// </summary>
    public class SceneFadeUI : MonoBehaviour
    {
        public static SceneFadeUI Instance { get; private set; }

        [SerializeField] private Image fadeImage;
        [SerializeField] private float fadeSeconds = 0.35f;

        private void Awake()
        {
            Instance = this;
            SetAlpha(0f);
        }

        public IEnumerator FadeOut() => Fade(0f, 1f);
        public IEnumerator FadeIn() => Fade(1f, 0f);

        private IEnumerator Fade(float from, float to)
        {
            if (fadeImage == null)
            {
                yield break;
            }

            SetAlpha(from);
            float elapsed = 0f;
            while (elapsed < fadeSeconds)
            {
                elapsed += Time.unscaledDeltaTime;
                SetAlpha(Mathf.Lerp(from, to, elapsed / fadeSeconds));
                yield return null;
            }
            SetAlpha(to);
        }

        private void SetAlpha(float alpha)
        {
            if (fadeImage == null)
            {
                return;
            }

            Color color = fadeImage.color;
            color.a = alpha;
            fadeImage.color = color;
        }
    }
}
