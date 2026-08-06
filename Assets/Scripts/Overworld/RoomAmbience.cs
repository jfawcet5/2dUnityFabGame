using BeyProject.Core;
using UnityEngine;

namespace BeyProject.Overworld
{
    /// <summary>
    /// One per room scene - an obvious place to drop a real background music clip later
    /// without touching AudioManager. No-op if left unassigned.
    /// </summary>
    public class RoomAmbience : MonoBehaviour
    {
        [SerializeField] private AudioClip roomBgm;

        private void Start()
        {
            if (roomBgm != null && AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayBGM(roomBgm);
            }
        }
    }
}
