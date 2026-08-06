using BeyProject.UI;
using UnityEngine;

namespace BeyProject.Core
{
    /// <summary>
    /// Same fix as PauseManager, for the same reason: InventoryUI's own GameObject is the
    /// panel it deactivates when closed, so the "I" toggle can't live in InventoryUI's own
    /// Update() without permanently killing itself after the first close. This lives on the
    /// always-active PersistentSystems root instead and drives InventoryUI purely as a view.
    /// </summary>
    public class InventoryManager : MonoBehaviour
    {
        public static InventoryManager Instance { get; private set; }

        public bool IsOpen { get; private set; }

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
            if (!Input.GetKeyDown(KeyCode.I))
            {
                return;
            }

            if (IsOpen)
            {
                Close();
            }
            else
            {
                TryOpen();
            }
        }

        /// <summary>Returns false if another modal (dialogue/pause) currently holds the input lock.</summary>
        public bool TryOpen()
        {
            if (IsOpen)
            {
                return true;
            }

            if (!UIInputLock.TryAcquire(this))
            {
                return false;
            }

            IsOpen = true;
            InventoryUI.Instance?.ShowPanel();
            return true;
        }

        public void Close()
        {
            if (!IsOpen)
            {
                return;
            }

            IsOpen = false;
            InventoryUI.Instance?.HidePanel();
            UIInputLock.Release(this);
        }
    }
}
