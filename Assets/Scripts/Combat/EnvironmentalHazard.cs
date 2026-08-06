using System.Collections.Generic;
using BeyProject.Core;
using UnityEngine;

namespace BeyProject.Combat
{
    public enum HazardType
    {
        ElectricFloor,
        SteamVent,
        FireTrap,
        RotatingLaser
    }

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
    public class EnvironmentalHazard : MonoBehaviour
    {
        [SerializeField] private HazardType hazardType = HazardType.ElectricFloor;
        [SerializeField] private float damagePerTick = 7f;
        [SerializeField] private float tickIntervalSeconds = 0.6f;
        [SerializeField] private float cycleSeconds = 3f;
        [SerializeField] private float activePhaseSeconds = 1.4f;
        [SerializeField] private float rotationDegreesPerSecond = 45f;
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Color activeColor = new Color(0.5f, 0.85f, 1f, 0.85f);
        [SerializeField] private Color idleColor = new Color(0.5f, 0.85f, 1f, 0.18f);

        private readonly List<Collider2D> occupants = new List<Collider2D>();
        private float nextTickTime;

        /// <summary>
        /// Fire traps burn constantly; the rotating laser is always lethal but sweeps in and
        /// out of the player's position; electric floors and steam vents pulse on a cycle so
        /// there's a readable safe window.
        /// </summary>
        public bool IsActivePhase
        {
            get
            {
                switch (hazardType)
                {
                    case HazardType.FireTrap:
                    case HazardType.RotatingLaser:
                        return true;
                    default:
                        return (Time.time % Mathf.Max(cycleSeconds, 0.01f)) < activePhaseSeconds;
                }
            }
        }

        private void Awake()
        {
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
            }

            if (spriteRenderer != null && spriteRenderer.sprite == null)
            {
                spriteRenderer.sprite = PlaceholderSprite.SharedSquare();
            }
        }

        private void Update()
        {
            if (hazardType == HazardType.RotatingLaser)
            {
                transform.Rotate(0f, 0f, rotationDegreesPerSecond * Time.deltaTime);
            }

            bool active = IsActivePhase;

            if (spriteRenderer != null)
            {
                // Pulse the idle state slightly so a dormant hazard still reads as dangerous.
                Color target = active ? activeColor : idleColor;
                if (!active)
                {
                    target.a *= 0.75f + 0.25f * Mathf.Sin(Time.time * 4f);
                }
                spriteRenderer.color = target;
            }

            if (!active || Time.time < nextTickTime)
            {
                return;
            }

            nextTickTime = Time.time + tickIntervalSeconds;
            DamageOccupants();
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

                damageable.TakeDamage(damagePerTick, occupant.transform.position);
            }
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
