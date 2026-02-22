using UnityEngine;

namespace JohnnyGame.Data
{
    /// <summary>
    /// Defines a single resource type. Create via Assets → Create → JohnnyGame → Resource Definition.
    /// </summary>
    [CreateAssetMenu(menuName = "JohnnyGame/Resource Definition", fileName = "ResourceDef_New")]
    public class ResourceDefinitionSO : ScriptableObject
    {
        [Header("Identity")]
        public string resourceId = "resource.unnamed";
        public string displayName = "Unknown";

        [Header("Economy")]
        public float startingAmount = 0f;
        public float maxAmount = 999f;
        public float baseIncomePerTick = 0f;
        public float decayPerTick = 0f;

        [Header("Node Yield")]
        // Amount gathered per tick from a resource node (before multipliers)
        public float gatherRatePerTick = 1f;
        // Max amount stored in a single node
        public float nodeMaxAmount = 50f;
    }
}
