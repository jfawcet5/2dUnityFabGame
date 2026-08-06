using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace BeyProject.UI
{
    /// <summary>
    /// Persistent room-name banner: fades in, holds, fades out. Unscaled time so it still
    /// plays correctly regardless of Time.timeScale.
    /// </summary>
    public class RoomTitleUI : MonoBehaviour
    {
        public static RoomTitleUI Instance { get; private set; }

        [SerializeField] private Text titleText;
        [SerializeField] private float visibleSeconds = 1.6f;
        [SerializeField] private float fadeSeconds = 0.4f;

        private Coroutine current;

        private void Awake()
        {
            Instance = this;
            SetAlpha(0f);
        }

        public void Show(string roomName)
        {
            if (titleText == null || string.IsNullOrEmpty(roomName))
            {
                return;
            }

            titleText.text = roomName;

            if (current != null)
            {
                StopCoroutine(current);
            }
            current = StartCoroutine(ShowRoutine());
        }

        private IEnumerator ShowRoutine()
        {
            yield return Fade(0f, 1f);
            yield return new WaitForSecondsRealtime(visibleSeconds);
            yield return Fade(1f, 0f);
            current = null;
        }

        private IEnumerator Fade(float from, float to)
        {
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
            if (titleText == null)
            {
                return;
            }

            Color color = titleText.color;
            color.a = alpha;
            titleText.color = color;
        }
    }
}
