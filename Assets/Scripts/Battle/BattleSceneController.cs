using System.Collections;
using BeyProject.Core;
using UnityEngine;
using UnityEngine.UI;

namespace BeyProject.Battle
{
    /// <summary>
    /// Placeholder only. Shows who you're fighting and a way back to exploration.
    /// This is the seam where the real battle system (still undecided - turn-based vs
    /// real-time physics vs hybrid) will get built later.
    /// </summary>
    public class BattleSceneController : MonoBehaviour
    {
        [SerializeField] private Text opponentLabel;
        [SerializeField] private Button returnButton;
        [SerializeField] private Image fadeOverlay;
        [SerializeField] private float fadeInSeconds = 0.6f;

        private void Start()
        {
            BattleContext context = GameManager.Instance != null ? GameManager.Instance.PendingBattle : null;

            if (opponentLabel != null)
            {
                string name = context != null ? context.opponentDisplayName : "Unknown Bey";
                opponentLabel.text = $"Battle vs. {name}\n\nBattle System Not Yet Implemented";
            }

            if (returnButton != null)
            {
                returnButton.onClick.AddListener(OnReturnPressed);
            }

            if (fadeOverlay != null)
            {
                StartCoroutine(FadeIn());
            }
        }

        // fadeOverlay starts fully opaque (black), covering the message, and fades to
        // transparent - a fade INTO the placeholder screen, not a fade-out of it.
        private IEnumerator FadeIn()
        {
            Color color = fadeOverlay.color;
            color.a = 1f;
            fadeOverlay.color = color;

            float elapsed = 0f;
            while (elapsed < fadeInSeconds)
            {
                elapsed += Time.deltaTime;
                color.a = Mathf.Lerp(1f, 0f, elapsed / fadeInSeconds);
                fadeOverlay.color = color;
                yield return null;
            }

            color.a = 0f;
            fadeOverlay.color = color;
        }

        private void OnReturnPressed()
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayUIClick();
            }

            if (GameManager.Instance != null)
            {
                GameManager.Instance.ReturnFromBattle();
            }
        }
    }
}
