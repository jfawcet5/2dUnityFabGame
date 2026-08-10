using BeyProject.Core;
using BeyProject.Player;
using UnityEngine;

namespace BeyProject.Combat
{
    /// <summary>
    /// Shared player/enemy projectile - runtime-instantiated (no prefab asset), same
    /// philosophy as PlaceholderSprite.CreateSquare. isPlayerOwned decides which side it can
    /// damage: player-owned bolts hurt enemies/turrets/boss/props, enemy bolts hurt the player.
    /// Either way it dies on anything solid, so walls and cover actually stop shots.
    /// </summary>
    public class Projectile : MonoBehaviour
    {
        private const float Lifespan = 3f;
        private const float HomingTurnDegreesPerSecond = 220f;

        private Vector2 direction;
        private float speed;
        private float damage;
        private bool isPlayerOwned;
        private bool homing;
        private float age;
        private Color tint;
        private Rigidbody2D body;

        public static Projectile Spawn(Vector3 position, Vector2 direction, float speed, float damage,
            bool isPlayerOwned, bool homing, float sizeMultiplier, Color color)
        {
            var go = new GameObject("Projectile", typeof(Rigidbody2D), typeof(CircleCollider2D), typeof(SpriteRenderer), typeof(Projectile));
            go.transform.position = position;
            go.transform.localScale = Vector3.one * Mathf.Max(0.2f, 0.35f * sizeMultiplier);

            Rigidbody2D body = go.GetComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            body.freezeRotation = true;

            CircleCollider2D collider = go.GetComponent<CircleCollider2D>();
            collider.isTrigger = true;
            collider.radius = 0.5f;

            SpriteRenderer renderer = go.GetComponent<SpriteRenderer>();
            renderer.sprite = PlaceholderSprite.SharedCircle();
            renderer.color = color;
            renderer.sortingOrder = 6;

            Projectile projectile = go.GetComponent<Projectile>();
            projectile.direction = direction.sqrMagnitude > 0f ? direction.normalized : Vector2.up;
            projectile.speed = speed;
            projectile.damage = damage;
            projectile.isPlayerOwned = isPlayerOwned;
            projectile.homing = homing;
            projectile.tint = color;
            projectile.body = body;

            return projectile;
        }

        private void FixedUpdate()
        {
            if (homing)
            {
                Transform target = FindNearestTargetTransform();
                if (target != null)
                {
                    Vector2 toTarget = ((Vector2)target.position - (Vector2)transform.position).normalized;
                    Vector3 rotated = Vector3.RotateTowards(direction, toTarget,
                        HomingTurnDegreesPerSecond * Mathf.Deg2Rad * Time.fixedDeltaTime, 0f);
                    direction = ((Vector2)rotated).normalized;
                }
            }

            body.velocity = direction * speed;
        }

        private void Update()
        {
            age += Time.deltaTime;
            if (age >= Lifespan)
            {
                Destroy(gameObject);
            }
        }

        private Transform FindNearestTargetTransform()
        {
            Transform nearest = null;
            float nearestDistance = float.MaxValue;

            if (isPlayerOwned)
            {
                foreach (var enemy in FindObjectsOfType<EnemyBase>())
                {
                    float distance = Vector2.Distance(transform.position, enemy.transform.position);
                    if (distance < nearestDistance)
                    {
                        nearestDistance = distance;
                        nearest = enemy.transform;
                    }
                }

                foreach (var boss in FindObjectsOfType<BossEnemy>())
                {
                    float distance = Vector2.Distance(transform.position, boss.transform.position);
                    if (distance < nearestDistance)
                    {
                        nearestDistance = distance;
                        nearest = boss.transform;
                    }
                }
            }
            else
            {
                var player = FindObjectOfType<PlayerHealth>();
                if (player != null)
                {
                    nearest = player.transform;
                }
            }

            return nearest;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            var damageable = other.GetComponent<IDamageable>();

            bool canDamage = CanDamage(damageable);

            if (damageable != null && canDamage)
            {
                damageable.TakeDamage(damage, transform.position);
                Impact();
                return;
            }
            else if (damageable != null && !canDamage)
            {
                return;
            }

            // Solid geometry (walls, cover) stops the shot. Non-damaging trigger volumes
            // - open doors, item pickups, hazard zones - are passed straight through.
            if (!other.isTrigger)
            {
                Impact();
            }
        }

        /// <summary>
        /// Friendly fire rules: player bolts never hurt the player, enemy bolts never hurt
        /// other enemies. Explosive props are deliberately damageable by BOTH sides, so a
        /// stray enemy shot can set off a barrel standing next to its own allies.
        /// </summary>
        private bool CanDamage(IDamageable damageable)
        {
            if (damageable is ExplosiveObject)
            {
                return true;
            }

            if (damageable is CoverObject)
            {
                return true;
            }

            bool targetIsPlayer = damageable is PlayerHealth;
            return isPlayerOwned != targetIsPlayer;
        }

        private void Impact()
        {
            CombatFeedback.Impact(transform.position, tint);
            Destroy(gameObject);
        }
    }
}
