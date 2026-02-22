using UnityEngine;

namespace JohnnyGame.Data
{
    /// <summary>
    /// Defines a purchasable upgrade. Create via Assets → Create → JohnnyGame → Upgrade Definition.
    /// </summary>
    [CreateAssetMenu(menuName = "JohnnyGame/Upgrade Definition", fileName = "UpgradeDef_New")]
    public class UpgradeDefinitionSO : ScriptableObject
    {
        [Header("Identity")]
        public string upgradeId = "upgrade.unnamed";
        public string displayName = "Unknown Upgrade";
        [TextArea] public string description;

        [Header("Cost")]
        public float costWood;
        public float costFood;
        public float costScrap;

        [Header("Effect")]
        // Supported effectTypes: "gather_multiplier", "food_income", "wood_income", "scrap_income"
        public string effectType;
        public float effectValue;

        // ID of the resource this effect targets (if applicable), e.g. "resource.wood"
        public string effectTargetResourceId;
    }
}
