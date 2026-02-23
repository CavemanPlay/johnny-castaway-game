using System.Collections.Generic;
using UnityEngine;
using JohnnyGame.Data;
using JohnnyGame.Debugging;

namespace JohnnyGame.Simulation
{
    /// <summary>
    /// Manages purchased upgrades and applies their effects to ResourceStore / WorkAssignment.
    /// </summary>
    public sealed class UpgradeManager : MonoBehaviour
    {
        [SerializeField] private UpgradeDefinitionSO[] _allUpgrades;

        private readonly List<string> _purchased = new();
        private ResourceStore   _store;
        private WorkAssignment  _work;

        // ── Setup ──────────────────────────────────────────────────────────

        public void Initialize(ResourceStore store, WorkAssignment work,
                               UpgradeDefinitionSO[] upgrades, List<string> alreadyOwned = null)
        {
            _store = store;
            _work  = work;
            if (upgrades != null) _allUpgrades = upgrades;

            _purchased.Clear();
            if (alreadyOwned != null)
            {
                foreach (var id in alreadyOwned)
                    ApplyById(id);
            }
        }

        // ── Public API ─────────────────────────────────────────────────────

        public int PurchasedCount => _purchased.Count;

        public bool IsOwned(string upgradeId) => _purchased.Contains(upgradeId);

        public bool TryBuy(string upgradeId)
        {
            if (IsOwned(upgradeId)) return false;
            var def = FindDef(upgradeId);
            if (def == null)
            {
                GameLogger.LogWarning(GameLogger.Category.Sim, $"Unknown upgrade: {upgradeId}");
                return false;
            }

            // Check all costs first (atomic — avoids partial deduction)
            if (_store.Get("resource.wood")  < def.costWood)  return false;
            if (_store.Get("resource.food")  < def.costFood)  return false;
            if (_store.Get("resource.scrap") < def.costScrap) return false;

            _store.TrySpend("resource.wood",  def.costWood);
            _store.TrySpend("resource.food",  def.costFood);
            _store.TrySpend("resource.scrap", def.costScrap);

            ApplyEffect(def);
            _purchased.Add(upgradeId);
            GameLogger.Log(GameLogger.Category.Sim, $"Upgrade purchased: {upgradeId}");
            return true;
        }

        public IReadOnlyList<string> PurchasedIds => _purchased;

        // ── Internals ──────────────────────────────────────────────────────

        private void ApplyById(string id)
        {
            var def = FindDef(id);
            if (def != null) { ApplyEffect(def); _purchased.Add(id); }
        }

        private void ApplyEffect(UpgradeDefinitionSO def)
        {
            switch (def.effectType)
            {
                case "gather_multiplier":
                    _store.SetGatherMultiplier(def.effectTargetResourceId, def.effectValue);
                    break;
                case "food_income":
                    _store.AddIncomeBonus("resource.food", def.effectValue);
                    break;
                case "wood_income":
                    _store.AddIncomeBonus("resource.wood", def.effectValue);
                    break;
                case "scrap_income":
                    _store.AddIncomeBonus("resource.scrap", def.effectValue);
                    break;
                default:
                    GameLogger.LogWarning(GameLogger.Category.Sim,
                        $"Unknown effect type '{def.effectType}' on upgrade {def.upgradeId}");
                    break;
            }
        }

        private UpgradeDefinitionSO FindDef(string id)
        {
            if (_allUpgrades == null) return null;
            foreach (var d in _allUpgrades)
                if (d != null && d.upgradeId == id) return d;
            return null;
        }
    }
}
