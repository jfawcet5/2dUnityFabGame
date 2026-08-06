using UnityEngine;

namespace BeyProject.Core
{
    /// <summary>
    /// Persistent audio hub. Clip slots are left unassigned for now - there's no real audio
    /// asset pipeline yet (see RoomAmbience for per-room BGM), so every Play* method no-ops
    /// if its clip is null rather than logging/erroring. Call sites are wired up correctly
    /// now so dropping in real clips later needs zero code changes.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [SerializeField] private AudioSource bgmSource;
        [SerializeField] private AudioSource sfxSource;

        public AudioClip footstepClip;
        public AudioClip itemPickupClip;
        public AudioClip doorClip;
        public AudioClip uiClickClip;
        public AudioClip lockedClip;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }

            Instance = this;

            if (bgmSource == null)
            {
                bgmSource = gameObject.AddComponent<AudioSource>();
                bgmSource.loop = true;
            }

            if (sfxSource == null)
            {
                sfxSource = gameObject.AddComponent<AudioSource>();
                sfxSource.loop = false;
            }
        }

        public void PlayBGM(AudioClip clip)
        {
            if (clip == null || bgmSource == null)
            {
                return;
            }

            if (bgmSource.clip == clip && bgmSource.isPlaying)
            {
                return;
            }

            bgmSource.clip = clip;
            bgmSource.Play();
        }

        public void PlayFootstepIfSet() => PlayOneShot(footstepClip);
        public void PlayItemPickup() => PlayOneShot(itemPickupClip);
        public void PlayDoor() => PlayOneShot(doorClip);
        public void PlayUIClick() => PlayOneShot(uiClickClip);
        public void PlayLocked() => PlayOneShot(lockedClip);

        private void PlayOneShot(AudioClip clip)
        {
            if (clip == null || sfxSource == null)
            {
                return;
            }

            sfxSource.PlayOneShot(clip);
        }
    }
}
