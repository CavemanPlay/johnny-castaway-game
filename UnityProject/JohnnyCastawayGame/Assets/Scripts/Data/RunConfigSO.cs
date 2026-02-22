using UnityEngine;

namespace JohnnyGame.Data
{
    /// <summary>
    /// Runtime configuration for a single run. Create via Assets → Create → JohnnyGame → Run Config.
    /// Wire this into GameRoot's inspector field.
    /// </summary>
    [CreateAssetMenu(menuName = "JohnnyGame/Run Config", fileName = "RunConfig")]
    public class RunConfigSO : ScriptableObject
    {
        [Header("Tick")]
        public float tickIntervalSeconds = 1f;
        public int autosaveEveryNTicks = 10;
        public float[] speedMultipliers = { 1f, 2f, 4f };

        [Header("Island Generation")]
        public int islandWidth = 20;
        public int islandHeight = 20;
        [Range(0f, 1f)] public float resourceDensity = 0.15f;

        [Header("Starting Resources")]
        public float startingFood = 10f;
        public float startingWood = 5f;
        public float startingScrap = 0f;
    }
}
