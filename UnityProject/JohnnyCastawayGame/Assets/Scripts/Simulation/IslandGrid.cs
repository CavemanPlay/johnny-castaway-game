using JohnnyGame.Data;

namespace JohnnyGame.Simulation
{
    /// <summary>
    /// Runtime representation of the island. Wraps WorldData arrays for convenient 2D access.
    /// </summary>
    public sealed class IslandGrid
    {
        public int Width { get; }
        public int Height { get; }
        public int SpawnX { get; }
        public int SpawnY { get; }

        private readonly TerrainType[] _terrainFlat;
        // Live mutable copy of nodes (amounts deplete during play)
        public ResourceNodeData[] Nodes { get; }

        public IslandGrid(WorldData world)
        {
            Width  = world.width;
            Height = world.height;
            SpawnX = world.spawnX;
            SpawnY = world.spawnY;
            _terrainFlat = world.terrainFlat;
            // Deep-copy nodes so saves aren't modified during play
            Nodes = new ResourceNodeData[world.nodes.Length];
            for (int i = 0; i < world.nodes.Length; i++)
            {
                var src = world.nodes[i];
                Nodes[i] = new ResourceNodeData
                {
                    resourceId = src.resourceId,
                    x          = src.x,
                    y          = src.y,
                    amount     = src.amount,
                    maxAmount  = src.maxAmount,
                };
            }
        }

        public TerrainType GetTerrain(int x, int y) => _terrainFlat[y * Width + x];

        public bool InBounds(int x, int y) => x >= 0 && x < Width && y >= 0 && y < Height;
    }
}
