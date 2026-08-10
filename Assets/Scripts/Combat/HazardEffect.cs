using System.Collections.Generic;
using BeyProject.Core;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace BeyProject.Combat
{
    /// <summary>
    /// One component, branching on hazardType - the same "flat fields + enum" convention
    /// EnemyBase and WorldAction already use, rather than four near-identical scripts.
    /// Hazards damage everything standing in them, enemies included, so kiting a chaser
    /// across an electric floor is a real tactic and not just a tax on the player.
    ///
    /// Occupants are tracked on enter/exit and damaged on a shared tick rather than in
    /// OnTriggerStay: Stay fires per-collider per-physics-step, which would make damage
    /// silently depend on how many colliders a target happens to have.
    ///
    /// Deliberately NOT IDamageable: hazards are scenery. If they were damageable,
    /// Projectile would stop dead on the first floor panel it crossed.
    /// </summary>
    public class HazardEffect : MonoBehaviour
    {
        [SerializeField] private string hazardId;
        [SerializeField] private float damagePerTick = 7f;
        [SerializeField] private float tickInterval = 1f;
        private float tickTimer;
        public bool bypassInvulnerability = false;
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Tilemap tileRenderer;
        [SerializeField] private Color activeColor = new Color(0.5f, 0.85f, 1f, 0.85f);
        [SerializeField] private Color idleColor = new Color(0.5f, 0.85f, 1f, 0.18f);

        private readonly List<Collider2D> occupants = new List<Collider2D>();

        private void Awake()
        {
            if (SaveSystem.Instance != null && SaveSystem.Instance.HasFlag($"hazard_disabled_{hazardId}"))
            {
                Destroy(gameObject);
                return;
            }

            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
            }

            if (spriteRenderer != null && spriteRenderer.sprite == null)
            {
                spriteRenderer.sprite = PlaceholderSprite.SharedSquare();
            }

            if (tileRenderer == null)
            {
                tileRenderer = GetComponent<Tilemap>();
            }
        }

        private void Update()
        {
            tickTimer += Time.deltaTime;

            if (tickTimer >= tickInterval)
            {
                DamageOccupants();
                tickTimer -= tickInterval;
            }
        }

        private void DamageOccupants()
        {
            for (int i = occupants.Count - 1; i >= 0; i--)
            {
                Collider2D occupant = occupants[i];
                if (occupant == null)
                {
                    occupants.RemoveAt(i);
                    continue;
                }

                var damageable = occupant.GetComponent<IDamageable>();
                if (damageable == null)
                {
                    continue;
                }

                damageable.TakeDamage(damagePerTick, bypassInvulnerability);
            }
        }

        public string GetHazardId()
        {
            return this.hazardId;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.GetComponent<IDamageable>() != null && !occupants.Contains(other))
            {
                occupants.Add(other);
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            occupants.Remove(other);
        }
    }
}
