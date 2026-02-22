using System.Collections.Generic;
using JohnnyGame.Core;
using JohnnyGame.Data;
using JohnnyGame.Debugging;
using UnityEngine;

namespace JohnnyGame.Simulation
{
    /// <summary>
    /// Generates a seeded island: terrain via distance-from-center + Perlin noise,
    /// resource nodes placed per terrain type at a configurable density.
    /// Implements IWorldGen.
    /// </summary>
    public sealed class IslandGenerator : IWorldGen
    {
        private readonly RunConfigSO _config;

        public IslandGenerator(RunConfigSO config)
        {
            _config = config;
        }

        public WorldData Generate(int seed)
        {
            int W = _config.islandWidth;
            int H = _config.islandHeight;

            var world = new WorldData
            {
                seed        = seed,
                biomeId     = "biome.tropical",
                width       = W,
                height      = H,
                spawnX      = W / 2,
                spawnY      = H / 2,
                terrainFlat = new TerrainType[W * H],
            };

            // Perlin offset derived from seed for cheap "seeded" noise
            float noiseOffX = (seed % 1000) / 10f;
            float noiseOffY = (seed / 1000 % 1000) / 10f;
            const float noiseFreq = 0.3f;

            float cx = W * 0.5f;
            float cy = H * 0.5f;
            float maxDist = Mathf.Min(cx, cy);

            for (int y = 0; y < H; y++)
            {
                for (int x = 0; x < W; x++)
                {
                    float dx = (x - cx) / maxDist;
                    float dy = (y - cy) / maxDist;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);

                    float noise = Mathf.PerlinNoise(x * noiseFreq + noiseOffX,
                                                    y * noiseFreq + noiseOffY);
                    // noise perturbs the distance
                    float effective = dist - (noise - 0.5f) * 0.3f;

                    TerrainType t;
                    if      (effective > 1.0f) t = TerrainType.Ocean;
                    else if (effective > 0.75f) t = TerrainType.Beach;
                    else if (noise > 0.65f)    t = TerrainType.Rocky;
                    else if (noise > 0.40f)    t = TerrainType.Forest;
                    else                        t = TerrainType.Clearing;

                    world.terrainFlat[y * W + x] = t;
                }
            }

            world.nodes = PlaceNodes(world, seed);

            GameLogger.Log(GameLogger.Category.WorldGen,
                $"Island generated: seed={seed}, size={W}×{H}, nodes={world.nodes.Length}");
            return world;
        }

        private ResourceNodeData[] PlaceNodes(WorldData world, int seed)
        {
            var rng   = new SeededRng(seed ^ 0xBEEF);
            var nodes = new List<ResourceNodeData>();

            int W = world.width;
            int H = world.height;

            for (int y = 0; y < H; y++)
            {
                for (int x = 0; x < W; x++)
                {
                    var terrain = world.terrainFlat[y * W + x];
                    string resourceId = TerrainToResource(terrain);
                    if (resourceId == null) continue;
                    if (!rng.NextBool(_config.resourceDensity)) continue;

                    nodes.Add(new ResourceNodeData
                    {
                        resourceId = resourceId,
                        x          = x,
                        y          = y,
                        maxAmount  = 50f,
                        amount     = rng.NextFloat(20f, 50f),
                    });
                }
            }

            return nodes.ToArray();
        }

        private static string TerrainToResource(TerrainType t) => t switch
        {
            TerrainType.Forest   => "resource.wood",
            TerrainType.Beach    => "resource.food",
            TerrainType.Rocky    => "resource.scrap",
            _                    => null,
        };
    }
}
