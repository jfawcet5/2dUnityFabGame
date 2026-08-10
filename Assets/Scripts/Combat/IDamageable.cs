using UnityEngine;

namespace BeyProject.Combat
{
    /// <summary>
    /// Anything that can be shot. The two-argument form carries where the hit came from so
    /// receivers can apply knockback; the one-argument form is kept for sources with no
    /// meaningful direction (hazards, explosions centred on the target, contact damage) and
    /// implementers forward it to the two-arg form with their own position, i.e. no knockback.
    /// </summary>
    public interface IDamageable
    {
        void TakeDamage(float amount);
        void TakeDamage(float amount, Vector2 hitFromPosition);
        void TakeDamage(float amount, bool bypassInvulnerability = false);
    }
}
