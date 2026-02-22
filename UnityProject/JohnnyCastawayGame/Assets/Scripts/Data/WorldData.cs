namespace JohnnyGame.Data
{
    [System.Serializable]
    public class WorldData
    {
        public int seed;

        // ID strategy: "category.name" (e.g. "biome.tropical", "biome.storm-belt")
        public string biomeId = "biome.tropical";

        // Island layout
        public int width;
        public int height;
        public int spawnX;
        public int spawnY;

        // Flat terrain grid indexed as [y * width + x], values are TerrainType enum ints
        public TerrainType[] terrainFlat;

        // Resource nodes placed on the island
        public ResourceNodeData[] nodes;
    }
}
