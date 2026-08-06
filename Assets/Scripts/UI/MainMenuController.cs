using BeyProject.Core;
using UnityEngine;
using UnityEngine.UI;

namespace BeyProject.UI
{
    public class MainMenuController : MonoBehaviour
    {
        [SerializeField] private Button newGameButton;
        [SerializeField] private Button continueButton;
        [SerializeField] private Button quitButton;

        [SerializeField] private string startingRoomScene = "Lobby";
        [SerializeField] private string startingSpawnPointId = "lobby_start";
        [SerializeField] private Vector2 startingFallbackPosition;

        private void Start()
        {
            if (newGameButton != null)
            {
                newGameButton.onClick.AddListener(OnNewGame);
            }

            if (continueButton != null)
            {
                continueButton.onClick.AddListener(OnContinue);
                continueButton.interactable = SaveSystem.Instance != null && SaveSystem.Instance.HasSaveFile();
            }

            if (quitButton != null)
            {
                quitButton.onClick.AddListener(OnQuit);
            }
        }

        private void OnNewGame()
        {
            AudioManager.Instance?.PlayUIClick();
            SaveSystem.Instance?.ResetToDefaults();
            GameManager.Instance?.TravelToRoom(startingRoomScene, startingSpawnPointId, startingFallbackPosition);
        }

        private void OnContinue()
        {
            AudioManager.Instance?.PlayUIClick();
            SaveSystem.Instance?.LoadFromDisk();
        }

        private void OnQuit()
        {
            AudioManager.Instance?.PlayUIClick();
            Application.Quit();
        }
    }
}
