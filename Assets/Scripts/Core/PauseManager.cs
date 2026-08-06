using BeyProject.UI;
using UnityEngine;

namespace BeyProject.Core
{
    /// <summary>
    /// Owns pause state, Time.timeScale, and the Escape-key toggle. Lives on the persistent
    /// PersistentSystems root, which is never deactivated - unlike the old PauseMenuController,
    /// which ran this same logic in Update() on the Pause panel GameObject and then deactivated
    /// that very GameObject in Awake, permanently killing its own Update before the first frame
    /// and making pause unreachable. PauseMenuController is now a pure view: ShowPanel()/
    /// HidePanel() only.
    /// </summary>
    public class PauseManager : MonoBehaviour
    {
        public static PauseManager Instance { get; private set; }

        public bool IsPaused { get; private set; }

        private void Awake()
        {
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void Update()
        {
            if (!Input.GetKeyDown(KeyCode.Escape))
            {
                return;
            }

            if (IsPaused)
            {
                Resume();
            }
            else
            {
                TryPause();
            }
        }

        /// <summary>Returns false if another modal (dialogue/inventory) currently holds the input lock.</summary>
        public bool TryPause()
        {
            if (IsPaused)
            {
                return true;
            }

            if (!UIInputLock.TryAcquire(this))
            {
                return false;
            }

            IsPaused = true;
            Time.timeScale = 0f;
            PauseMenuController.Instance?.ShowPanel();
            return true;
        }

        public void Resume()
        {
            if (!IsPaused)
            {
                return;
            }

            IsPaused = false;
            Time.timeScale = 1f;
            PauseMenuController.Instance?.HidePanel();
            UIInputLock.Release(this);
        }
    }
}
