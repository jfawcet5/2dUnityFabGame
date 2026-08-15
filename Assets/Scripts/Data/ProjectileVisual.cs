using UnityEngine;

namespace BeyProject.Data
{
    /// <summary>
    /// A pluggable projectile look: a looping sprite-sheet animation (or a single static sprite
    /// if frames has one entry). Assign a module's sliced sprite sheet here and reference the
    /// asset from ItemDefinition.chipModule.projectileVisual - no code changes needed to swap
    /// art later.
    /// </summary>
    [CreateAssetMenu(fileName = "ProjectileVisual", menuName = "Bey Project/Projectile Visual")]
    public class ProjectileVisual : ScriptableObject
    {
        public Sprite[] frames;
        public float secondsPerFrame = 0.06f;
    }
}
