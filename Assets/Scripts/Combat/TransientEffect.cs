using BeyProject.Core;
using UnityEngine;

namespace BeyProject.Combat
{
    /// <summary>
    /// A short-lived sprite that scales and fades out, then destroys itself. Runtime-
    /// instantiated with no prefab asset, same philosophy as Projectile.Spawn - all the
    /// combat VFX in CombatFeedback are built out of these, so there's exactly one place
    /// that owns effect lifetime instead of a coroutine per effect type.
    /// </summary>
    public class TransientEffect : MonoBehaviour
    {
        private float lifetime;
        private float age;
        private float startScale;
        private float endScale;
        private Color startColor;
        private Vector2 drift;
        private SpriteRenderer spriteRenderer;

        public static TransientEffect Spawn(Vector3 position, Color color, float startScale, float endScale,
            float lifetime, bool circle = true, Vector2 drift = default, int sortingOrder = 7)
        {
            var go = new GameObject("CombatEffect", typeof(SpriteRenderer), typeof(TransientEffect));
            go.transform.position = position;
            go.transform.localScale = Vector3.one * startScale;

            SpriteRenderer renderer = go.GetComponent<SpriteRenderer>();
            renderer.sprite = circle ? PlaceholderSprite.SharedCircle() : PlaceholderSprite.SharedSquare();
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;

            TransientEffect effect = go.GetComponent<TransientEffect>();
            effect.spriteRenderer = renderer;
            effect.startColor = color;
            effect.startScale = startScale;
            effect.endScale = endScale;
            effect.lifetime = Mathf.Max(0.01f, lifetime);
            effect.drift = drift;

            return effect;
        }

        private void Update()
        {
            age += Time.deltaTime;
            float t = Mathf.Clamp01(age / lifetime);

            transform.localScale = Vector3.one * Mathf.Lerp(startScale, endScale, t);
            transform.position += (Vector3)(drift * Time.deltaTime);

            Color color = startColor;
            color.a = startColor.a * (1f - t);
            spriteRenderer.color = color;

            if (t >= 1f)
            {
                Destroy(gameObject);
            }
        }
    }
}
