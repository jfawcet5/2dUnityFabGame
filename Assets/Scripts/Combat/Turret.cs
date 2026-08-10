using System;
using BeyProject.Core;
using UnityEngine;

namespace BeyProject.Combat
{
    /// <summary>
    /// Stationary automated defense: tracks the player, telegraphs, then fires. Destructible,
    /// and separate from EnemyBase precisely because it never moves - mixing "does it chase?"
    /// into EnemyBase's enum branch would have made that component's Update read as two
    /// unrelated behaviours stapled together. Forces target prioritisation: it keeps shooting
    /// while the player deals with the chasers.
    /// </summary>
    public class Turret : MonoBehaviour, IDamageable
    {
        [SerializeField] private string turretId;
        [SerializeField] private float maxHealth = 45f;
        [SerializeField] private float range = 7.5f;
        [SerializeField] private float fireIntervalSeconds = 1.8f;
        [SerializeField] private float telegraphSeconds = 0.4f;
        [SerializeField] private float projectileSpeed = 5.5f;
        [SerializeField] private float projectileDamage = 9f;
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Color fallbackColor = new Color(0.75f, 0.3f, 0.55f);
        [SerializeField] private HitFlash hitFlash;

        public event Action Destroyed;

        private float currentHealth;
        private float nextFireTime;
        private bool telegraphing;
        private Transform player;
        private Color baseColor;
        private Transform barrel;

        private void Awake()
        {
            if (!string.IsNullOrEmpty(turretId) &&
                SaveSystem.Instance != null && SaveSystem.Instance.HasFlag(DestroyedFlag))
            {
                Destroy(gameObject);
                return;
            }

            currentHealth = maxHealth;

            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
            }

            if (spriteRenderer != null)
            {
                if (spriteRenderer.sprite == null)
                {
                    spriteRenderer.sprite = PlaceholderSprite.CreateSquare(fallbackColor);
                }

                baseColor = spriteRenderer.color;
            }

            if (hitFlash == null)
            {
                hitFlash = GetComponent<HitFlash>();
            }

            hitFlash?.SetBaseColor(baseColor);

            // A short barrel stub so the player can read which way it's pointing.
            var barrelGO = new GameObject("Barrel", typeof(SpriteRenderer));
            barrelGO.transform.SetParent(transform, false);
            barrelGO.transform.localScale = new Vector3(0.55f, 0.18f, 1f);
            SpriteRenderer barrelRenderer = barrelGO.GetComponent<SpriteRenderer>();
            barrelRenderer.sprite = PlaceholderSprite.SharedSquare();
            barrelRenderer.color = new Color(0.2f, 0.2f, 0.25f);
            barrelRenderer.sortingOrder = (spriteRenderer != null ? spriteRenderer.sortingOrder : 5) + 1;
            barrel = barrelGO.transform;

            nextFireTime = Time.time + fireIntervalSeconds;
        }

        private string DestroyedFlag => $"turret_destroyed_{turretId}";

        public bool GetIsDestroyed()
        {
            return !string.IsNullOrEmpty(turretId) && SaveSystem.Instance != null && SaveSystem.Instance.HasFlag(DestroyedFlag);
        }

        private void Start()
        {
            GameObject playerGO = GameObject.FindGameObjectWithTag("Player");
            if (playerGO != null)
            {
                player = playerGO.transform;
            }
        }

        private void Update()
        {
            if (player == null)
            {
                return;
            }

            Vector2 toPlayer = (Vector2)player.position - (Vector2)transform.position;
            float distance = toPlayer.magnitude;

            if (barrel != null && distance > 0.01f)
            {
                float angle = Mathf.Atan2(toPlayer.y, toPlayer.x) * Mathf.Rad2Deg;
                barrel.localPosition = (Vector3)(toPlayer.normalized * 0.3f);
                barrel.localRotation = Quaternion.Euler(0f, 0f, angle);
            }

            if (distance > range)
            {
                return;
            }

            if (!telegraphing && Time.time >= nextFireTime - telegraphSeconds)
            {
                telegraphing = true;
                CombatFeedback.Telegraph(transform.position, new Color(1f, 0.4f, 0.4f), 1.1f);
            }

            if (Time.time >= nextFireTime)
            {
                Fire(toPlayer.normalized);
                telegraphing = false;
                nextFireTime = Time.time + fireIntervalSeconds;
            }
        }

        private void Fire(Vector2 direction)
        {
            Vector3 spawn = transform.position + (Vector3)(direction * 0.45f);
            CombatFeedback.MuzzleFlash(spawn, direction, new Color(1f, 0.5f, 0.5f), 1f);
            Projectile.Spawn(spawn, direction, projectileSpeed, projectileDamage,
                isPlayerOwned: false, homing: false, sizeMultiplier: 1f, color: new Color(1f, 0.45f, 0.5f));
        }

        public void TakeDamage(float amount)
        {
            TakeDamage(amount, transform.position);
        }

        public void TakeDamage(float amount, Vector2 hitFromPosition)
        {
            currentHealth -= amount;
            hitFlash?.Flash();

            if (currentHealth <= 0f)
            {
                if (!string.IsNullOrEmpty(turretId))
                {
                    SaveSystem.Instance?.SetFlag(DestroyedFlag);
                }

                CombatFeedback.Death(transform.position, baseColor);
                Destroyed?.Invoke();
                Destroy(gameObject);
            }
        }

        public void TakeDamage(float amount, bool bypassInvulnerability = false)
        {
            throw new NotImplementedException();
        }
    }
}
