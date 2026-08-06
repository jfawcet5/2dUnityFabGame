using BeyProject.UI;
using UnityEngine;

namespace BeyProject.Overworld
{
    /// <summary>
    /// One per room scene - shows the room's display name via the persistent RoomTitleUI
    /// whenever the scene loads (every entry, not just the first).
    /// </summary>
    public class RoomIntro : MonoBehaviour
    {
        [SerializeField] private string roomDisplayName;

        private void Start()
        {
            if (RoomTitleUI.Instance != null)
            {
                RoomTitleUI.Instance.Show(roomDisplayName);
            }
        }
    }
}
