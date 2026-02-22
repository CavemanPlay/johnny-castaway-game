namespace JohnnyGame.Data
{
    [System.Serializable]
    public class PlayerData
    {
        // ID strategy: "category.name" strings (stable across schema versions)
        public string playerId = "player.default";

        // Resources — driven by TickSim
        public float food;
        public float wood;
        public float scrap;

        // Escape progress — 0..1, reaches 1 to trigger Won state
        public float escapeProgress;
    }
}
