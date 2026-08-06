using UnityEngine;

namespace BeyProject.Overworld
{
    /// <summary>
    /// Plain script-based smooth follow, no Cinemachine dependency for now. Also owns screen
    /// shake: the shake offset is applied on top of the smoothed follow position rather than
    /// by moving the camera itself, so a shake never fights SmoothDamp or leaves the camera
    /// permanently displaced if the effect is interrupted mid-transition.
    /// </summary>
    public class CameraFollow2D : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private float smoothTime = 0.15f;
        [SerializeField] private Vector3 offset = new Vector3(0f, 0f, -10f);

        private static CameraFollow2D active;

        private Vector3 velocity;
        private float shakeTimeRemaining;
        private float shakeDuration;
        private float shakeMagnitude;

        private void OnEnable()
        {
            active = this;
        }

        private void OnDisable()
        {
            if (active == this)
            {
                active = null;
            }
        }

        /// <summary>
        /// Shakes whichever follow camera is currently live. Static so combat code can request
        /// a shake without holding a reference - each room builds its own camera, so a cached
        /// reference would dangle across scene transitions.
        /// </summary>
        public static void RequestShake(float duration, float magnitude)
        {
            if (active != null)
            {
                active.Shake(duration, magnitude);
            }
        }

        public void Shake(float duration, float magnitude)
        {
            // Take the stronger of the two rather than stacking, so a burst of simultaneous
            // hits reads as one solid impact instead of shaking the screen off its axis.
            if (duration <= shakeTimeRemaining && magnitude <= shakeMagnitude)
            {
                return;
            }

            shakeDuration = Mathf.Max(duration, 0.01f);
            shakeTimeRemaining = shakeDuration;
            shakeMagnitude = magnitude;
        }

        private void LateUpdate()
        {
            if (target == null)
            {
                var player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                {
                    target = player.transform;
                }
                else
                {
                    return;
                }
            }

            Vector3 desiredPosition = target.position + offset;
            transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref velocity, smoothTime);

            if (shakeTimeRemaining > 0f)
            {
                shakeTimeRemaining -= Time.deltaTime;
                float falloff = Mathf.Clamp01(shakeTimeRemaining / shakeDuration);
                Vector2 jitter = Random.insideUnitCircle * (shakeMagnitude * falloff);
                transform.position += new Vector3(jitter.x, jitter.y, 0f);
            }
        }

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
        }
    }
}
