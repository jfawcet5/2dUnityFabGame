using UnityEngine;

namespace BeyProject.Data
{
    /// <summary>Flat list of every UnlockRule in the game, parallel to how ItemDatabase wraps
    /// allItems - the single asset UnlockManager references.</summary>
    [CreateAssetMenu(fileName = "UnlockRuleSet", menuName = "Bey Project/Unlock Rule Set")]
    public class UnlockRuleSet : ScriptableObject
    {
        public UnlockRule[] rules = new UnlockRule[0];
    }
}
