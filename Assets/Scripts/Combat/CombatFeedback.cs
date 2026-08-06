using BeyProject.Overworld;
using UnityEngine;

namespace BeyProject.Combat
{
    /// <summary>
    /// One place every combat "juice" effect is spawned from, so hit/death/explosion feel is
    /// tuned in a single file rather than scattered across Projectile/EnemyBase/BossEnemy.
    /// Everything is built from TransientEffect - no particle systems, no prefab assets,
    /// matching how the rest of this project generates its visuals at runtime.
    /// </summary>
    public static class CombatFeedback
    {
        /// <summary>Small spark burst where a projectile connected.</summary>
        public static void Impact(Vector3 position, Color color)
        {
            TransientEffect.Spawn(position, new Color(1f, 1f, 1f, 0.9f), 0.35f, 0.05f, 0.12f);

            for (int i = 0; i < 4; i++)
            {
                float angle = Random.Range(0f, Mathf.PI * 2f);
                var drift = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * Random.Range(1.5f, 3f);
                TransientEffect.Spawn(position, color, 0.16f, 0.02f, Random.Range(0.15f, 0.28f), true, drift);
            }
        }

        /// <summary>Bigger ring + shards when something is destroyed.</summary>
        public static void Death(Vector3 position, Color color)
        {
            TransientEffect.Spawn(position, new Color(color.r, color.g, color.b, 0.7f), 0.4f, 1.6f, 0.32f);

            for (int i = 0; i < 10; i++)
            {
                float angle = (Mathf.PI * 2f / 10f) * i + Random.Range(-0.2f, 0.2f);
                var drift = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * Random.Range(2f, 4.5f);
                TransientEffect.Spawn(position, color, 0.22f, 0.02f, Random.Range(0.25f, 0.45f), false, drift);
            }

            CameraFollow2D.RequestShake(0.16f, 0.09f);
        }

        /// <summary>Brief flash at the muzzle when the player fires.</summary>
        public static void MuzzleFlash(Vector3 position, Vector2 direction, Color color, float sizeMultiplier)
        {
            Vector3 offset = (Vector3)(direction.normalized * 0.25f);
            TransientEffect.Spawn(position + offset, new Color(color.r, color.g, color.b, 0.85f),
                0.34f * sizeMultiplier, 0.05f, 0.07f);
        }

        /// <summary>Expanding blast ring - paired with ExplosiveObject's radius damage.</summary>
        public static void Explosion(Vector3 position, float radius, Color color)
        {
            TransientEffect.Spawn(position, new Color(1f, 0.95f, 0.7f, 0.9f), 0.5f, radius * 2f, 0.28f);
            TransientEffect.Spawn(position, new Color(color.r, color.g, color.b, 0.75f), 0.3f, radius * 1.5f, 0.42f);

            for (int i = 0; i < 12; i++)
            {
                float angle = (Mathf.PI * 2f / 12f) * i;
                var drift = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius * Random.Range(1.5f, 2.5f);
                TransientEffect.Spawn(position, color, 0.28f, 0.03f, Random.Range(0.3f, 0.5f), false, drift);
            }

            CameraFollow2D.RequestShake(0.3f, 0.22f);
        }

        /// <summary>Telegraph pulse - a wind-up tell before a dash or a boss attack.</summary>
        public static void Telegraph(Vector3 position, Color color, float size)
        {
            TransientEffect.Spawn(position, new Color(color.r, color.g, color.b, 0.5f), size * 0.4f, size, 0.35f);
        }
    }
}
