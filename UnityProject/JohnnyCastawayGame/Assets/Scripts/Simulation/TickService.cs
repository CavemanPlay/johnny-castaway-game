using System;
using UnityEngine;
using JohnnyGame.Core;
using JohnnyGame.Data;
using JohnnyGame.Debugging;

namespace JohnnyGame.Simulation
{
    /// <summary>
    /// Drives the discrete tick simulation. Uses an accumulator against
    /// unscaledDeltaTime × speedMultiplier so ticks are independent of Time.timeScale.
    /// Subscribe to <see cref="OnTick"/> to receive tick callbacks.
    /// Add this MonoBehaviour to the Bootstrap scene.
    /// </summary>
    public sealed class TickService : MonoBehaviour, ITickSim
    {
        [SerializeField] private RunConfigSO _config;

        // ── State ──────────────────────────────────────────────────────────
        private float _accumulator;
        private float _speedMultiplier = 1f;
        private bool  _running;

        public int   CurrentTick     { get; private set; }
        public float SpeedMultiplier => _speedMultiplier;

        /// <summary>Fired once per simulation tick. Subscribe in GameRoot.Awake.</summary>
        public event Action OnTick;

        // ── ITickSim ───────────────────────────────────────────────────────

        /// <summary>
        /// Advances the simulation by one tick. Called automatically by the
        /// accumulator and also available for manual ticking (e.g. F5 in GameRoot).
        /// </summary>
        public void Tick(RunData run)
        {
            CurrentTick++;
            run.tick = CurrentTick;
            GameLogger.Log(GameLogger.Category.Sim, $"Tick {CurrentTick}");
            OnTick?.Invoke();
        }

        // ── Public API ─────────────────────────────────────────────────────

        public void SetRunning(bool running) => _running = running;

        public void SetSpeed(float multiplier) => _speedMultiplier = multiplier;

        /// <summary>Injects config at runtime (used by GameRoot self-bootstrap).</summary>
        public void SetConfig(RunConfigSO config) => _config = config;

        public void ResetTick(int tick = 0)
        {
            CurrentTick = tick;
            _accumulator = 0f;
        }

        // ── Unity lifecycle ────────────────────────────────────────────────

        private void Update()
        {
            if (!_running || _config == null) return;

            float interval = _config.tickIntervalSeconds;
            if (interval <= 0f) interval = 1f;

            _accumulator += Time.unscaledDeltaTime * _speedMultiplier;

            // Cap to avoid spiral of death after long pauses
            float maxAccumulation = interval * 5f;
            if (_accumulator > maxAccumulation)
                _accumulator = maxAccumulation;

            // GameRoot.Update() drives manual ticks via ITickSim.Tick;
            // here we only fire auto ticks and let GameRoot handle the RunData pass.
            while (_accumulator >= interval)
            {
                _accumulator -= interval;
                // Signal that a tick is due — GameRoot._onAutoTick handles RunData
                _onAutoTickInternal?.Invoke();
            }
        }

        // Internal callback so GameRoot can provide RunData without coupling TickService to it
        internal Action _onAutoTickInternal;
    }
}
