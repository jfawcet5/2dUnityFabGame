using System.Collections;
using BeyProject.Core;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BeyProject.UI
{
    /// <summary>
    /// Pure view for the pause screen. PauseManager owns pause state, Time.timeScale, the
    /// Escape-key toggle, and UIInputLock ownership - it calls ShowPanel()/HidePanel() here.
    /// Buttons call back into PauseManager rather than toggling anything locally, so there is
    /// exactly one place pause state can change.
    /// </summary>
    public class PauseMenuController : MonoBehaviour
    {
        public static PauseMenuController Instance { get; private set; }

        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button saveButton;
        [SerializeField] private Button quitButton;
        [SerializeField] private Text savedFeedbackText;

        private void Awake()
        {
            Instance = this;

            if (resumeButton != null)
            {
                resumeButton.onClick.AddListener(OnResumePressed);
            }

            if (saveButton != null)
            {
                saveButton.onClick.AddListener(OnSavePressed);
            }

            if (quitButton != null)
            {
                quitButton.onClick.AddListener(OnQuitPressed);
            }

            if (panelRoot != null)
            {
                panelRoot.SetActive(false);
            }

            if (savedFeedbackText != null)
            {
                savedFeedbackText.gameObject.SetActive(false);
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void ShowPanel()
        {
            if (panelRoot != null)
            {
                panelRoot.SetActive(true);
            }
        }

        public void HidePanel()
        {
            if (panelRoot != null)
            {
                panelRoot.SetActive(false);
            }
        }

        private void OnResumePressed()
        {
            AudioManager.Instance?.PlayUIClick();
            PauseManager.Instance?.Resume();
        }

        private void OnSavePressed()
        {
            AudioManager.Instance?.PlayUIClick();
            SaveSystem.Instance?.Save();

            if (savedFeedbackText != null)
            {
                StartCoroutine(FlashSaved());
            }
        }

        private IEnumerator FlashSaved()
        {
            savedFeedbackText.gameObject.SetActive(true);
            yield return new WaitForSecondsRealtime(1.2f);
            savedFeedbackText.gameObject.SetActive(false);
        }

        private void OnQuitPressed()
        {
            AudioManager.Instance?.PlayUIClick();
            PauseManager.Instance?.Resume();
            SceneManager.LoadScene("MainMenu");
        }
    }
}
