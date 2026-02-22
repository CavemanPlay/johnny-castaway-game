using JohnnyGame.Core;

namespace JohnnyGame.Simulation
{
    /// <summary>
    /// Deterministic RNG backed by System.Random. Fully implements IRng.
    /// </summary>
    public sealed class SeededRng : IRng
    {
        private readonly System.Random _random;

        public int Seed { get; }

        public SeededRng(int seed)
        {
            Seed = seed;
            _random = new System.Random(seed);
        }

        public int NextInt(int minInclusive, int maxExclusive)
            => _random.Next(minInclusive, maxExclusive);

        public float NextFloat(float min, float max)
            => min + (float)_random.NextDouble() * (max - min);

        public bool NextBool(float trueProbability = 0.5f)
            => (float)_random.NextDouble() < trueProbability;
    }
}
