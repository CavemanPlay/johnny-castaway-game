using System.Collections.Generic;
using UnityEngine;
using JohnnyGame.Data;
using JohnnyGame.Debugging;

namespace JohnnyGame.Simulation
{
    /// <summary>
    /// Manages live resource amounts. Applies passive income and decay each tick.
    /// Upgrade bonuses are added via <see cref="AddIncomeBonus"/> and
    /// <see cref="SetGatherMultiplier"/>.
    /// </summary>
    public sealed class ResourceStore : MonoBehaviour
    {
        private ResourceDefinitionSO[]          _defs;
        private Dictionary<string, float>       _amounts    = new();
        private Dictionary<string, float>       _incomeBonuses = new();
        private Dictionary<string, float>       _gatherMult = new();

        // ── Initialisation ─────────────────────────────────────────────────

        public void Initialize(ResourceDefinitionSO[] defs, Dictionary<string, float> initialAmounts)
        {
            _defs = defs;
            _amounts.Clear();
            _incomeBonuses.Clear();
            _gatherMult.Clear();

            foreach (var def in defs)
            {
                float start = initialAmounts != null && initialAmounts.TryGetValue(def.resourceId, out float v)
                              ? v : def.startingAmount;
                _amounts[def.resourceId]      = Mathf.Clamp(start, 0f, def.maxAmount);
                _incomeBonuses[def.resourceId] = 0f;
                _gatherMult[def.resourceId]   = 1f;
            }
        }

        // ── Tick ───────────────────────────────────────────────────────────

        public void OnTick()
        {
            if (_defs == null) return;
            foreach (var def in _defs)
            {
                _incomeBonuses.TryGetValue(def.resourceId, out float bonus);
                float income = def.baseIncomePerTick + bonus;
                float delta  = income - def.decayPerTick;
                AddInternal(def.resourceId, delta, def.maxAmount);
            }
        }

        // ── Public API ─────────────────────────────────────────────────────

        public float Get(string resourceId)
            => _amounts.TryGetValue(resourceId, out float v) ? v : 0f;

        public void Add(string resourceId, float amount)
        {
            float max = GetMax(resourceId);
            AddInternal(resourceId, amount, max);
        }

        public bool TrySpend(string resourceId, float amount)
        {
            if (Get(resourceId) < amount) return false;
            _amounts[resourceId] -= amount;
            return true;
        }

        public float GetGatherMultiplier(string resourceId)
            => _gatherMult.TryGetValue(resourceId, out float m) ? m : 1f;

        // ── Upgrade hooks ──────────────────────────────────────────────────

        public void AddIncomeBonus(string resourceId, float bonus)
        {
            if (_incomeBonuses.ContainsKey(resourceId))
                _incomeBonuses[resourceId] += bonus;
        }

        public void SetGatherMultiplier(string resourceId, float mult)
        {
            if (_gatherMult.ContainsKey(resourceId))
                _gatherMult[resourceId] = mult;
        }

        /// <summary>Returns the base gather rate per tick for the given resource definition.</summary>
        public float GetBaseGatherRate(string resourceId)
        {
            if (_defs != null)
                foreach (var d in _defs)
                    if (d.resourceId == resourceId) return d.gatherRatePerTick;
            return 1f;
        }

        // ── Save/load helpers ──────────────────────────────────────────────

        /// <summary>Writes current amounts into a PlayerData for serialisation.</summary>
        public void FlushToPlayerData(PlayerData player)
        {
            player.food  = Get("resource.food");
            player.wood  = Get("resource.wood");
            player.scrap = Get("resource.scrap");
        }

        // ── Private helpers ────────────────────────────────────────────────

        private float GetMax(string resourceId)
        {
            if (_defs != null)
                foreach (var d in _defs)
                    if (d.resourceId == resourceId)
                        return d.maxAmount;
            return 999f;
        }

        private void AddInternal(string id, float delta, float max)
        {
            if (!_amounts.TryGetValue(id, out float cur)) return;
            _amounts[id] = Mathf.Clamp(cur + delta, 0f, max);
        }
    }
}
