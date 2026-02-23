using UnityEngine;
using JohnnyGame.Data;
using JohnnyGame.Debugging;

namespace JohnnyGame.Simulation
{
    /// <summary>
    /// Each tick, Johnny gathers from the nearest resource node of each type
    /// within <see cref="gatherRadius"/> tiles of the spawn point.
    /// Nodes deplete as they are harvested.
    /// </summary>
    public sealed class WorkAssignment : MonoBehaviour
    {
        [SerializeField] public float gatherRadius = 8f;

        private IslandGrid    _grid;
        private ResourceStore _store;

        // ── Setup ──────────────────────────────────────────────────────────

        public void Initialize(IslandGrid grid, ResourceStore store)
        {
            _grid  = grid;
            _store = store;
        }

        // ── Tick ───────────────────────────────────────────────────────────

        public void OnTick()
        {
            if (_grid == null || _store == null) return;

            // Find the nearest non-empty node per resource type within radius, then harvest.
            // Linear scan — node count is small.
            var nodes = _grid.Nodes;
            float spawnX = _grid.SpawnX;
            float spawnY = _grid.SpawnY;

            // Simple insertion-style single-pass: find nearest non-empty node per type
            var nearest = new System.Collections.Generic.Dictionary<string, int>();
            var nearestDist = new System.Collections.Generic.Dictionary<string, float>();

            for (int i = 0; i < nodes.Length; i++)
            {
                var n = nodes[i];
                if (n.amount <= 0f) continue;

                float dx   = n.x - spawnX;
                float dy   = n.y - spawnY;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                if (dist > gatherRadius) continue;

                if (!nearestDist.TryGetValue(n.resourceId, out float best) || dist < best)
                {
                    nearest[n.resourceId]     = i;
                    nearestDist[n.resourceId] = dist;
                }
            }

            // Harvest from nearest node of each type
            foreach (var kvp in nearest)
            {
                string resId = kvp.Key;
                var node     = nodes[kvp.Value];
                float mult   = _store.GetGatherMultiplier(resId);
                float yield  = _store.GetBaseGatherRate(resId) * mult;
                float actual = Mathf.Min(yield, node.amount);

                node.amount -= actual;
                _store.Add(resId, actual);
            }
        }

    }
}
