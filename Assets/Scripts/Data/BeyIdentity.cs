using UnityEngine;

namespace BeyProject.Data
{
    /// <summary>
    /// Minimal identity for an overworld opponent, just enough to tell placeholder
    /// bladers/beys apart and pass context into a battle. Stats/moves intentionally
    /// live in whatever battle system gets built later, not here.
    /// </summary>
    [CreateAssetMenu(fileName = "BeyIdentity", menuName = "Bey Project/Bey Identity")]
    public class BeyIdentity : ScriptableObject
    {
        public string id = "unnamed_bey";
        public string displayName = "Wild Bey";
        public Color color = Color.red;
    }
}
