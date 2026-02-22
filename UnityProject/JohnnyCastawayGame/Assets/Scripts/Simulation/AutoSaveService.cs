using System;
using UnityEngine;
using JohnnyGame.Core;
using JohnnyGame.Data;
using JohnnyGame.Debugging;

namespace JohnnyGame.Simulation
{
    /// <summary>
    /// Counts ticks and calls the save service every N ticks.
    /// Call <see cref="ForceSave"/> to trigger an immediate save (e.g. on quit).
    /// </summary>
    public sealed class AutoSaveService : MonoBehaviour
    {
        private ISave             _save;
        private Func<SaveData>    _collectData;
        private int               _autosaveEveryN = 10;
        private int               _ticksSinceSave;

        // ── Setup ──────────────────────────────────────────────────────────

        public void Initialize(ISave save, Func<SaveData> collectData, int autosaveEveryN)
        {
            _save            = save;
            _collectData     = collectData;
            _autosaveEveryN  = Mathf.Max(1, autosaveEveryN);
            _ticksSinceSave  = 0;
        }

        // ── Tick ───────────────────────────────────────────────────────────

        public void OnTick()
        {
            _ticksSinceSave++;
            if (_ticksSinceSave >= _autosaveEveryN)
            {
                ForceSave();
                _ticksSinceSave = 0;
            }
        }

        // ── Public API ─────────────────────────────────────────────────────

        public void ForceSave()
        {
            if (_save == null || _collectData == null) return;
            _save.Save(_collectData());
        }
    }
}
