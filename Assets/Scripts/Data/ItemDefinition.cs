using UnityEngine;

namespace BeyProject.Data
{
    /// <summary>
    /// Identity + display data for a collectible/key item, plus chipModule (see
    /// ChipModuleStats) when category == ChipModule.
    /// </summary>
    [CreateAssetMenu(fileName = "ItemDefinition", menuName = "Bey Project/Item Definition")]
    public class ItemDefinition : ScriptableObject
    {
        public string id = "unnamed_item";
        public string displayName = "Item";
        public Color color = Color.yellow;

        [TextArea]
        public string description = "";
        public ItemCategory category = ItemCategory.Component;
        public bool isKeyItem = false;
        public Sprite icon;

        [Header("Chip Module (only used when category == ChipModule)")]
        public ChipModuleStats chipModule = new ChipModuleStats();
    }
}
