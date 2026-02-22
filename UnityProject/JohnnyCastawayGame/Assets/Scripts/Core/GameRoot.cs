using System.Collections.Generic;
using UnityEngine;
using JohnnyGame.Data;
using JohnnyGame.Debugging;
using JohnnyGame.Simulation;

namespace JohnnyGame.Core
{
    /// <summary>
    /// Entry point and composition root.
    /// Owns the state machine, wires all services, and drives the tick loop.
    /// Add this MonoBehaviour to the Bootstrap scene (set Execution Order to -100).
    ///
    /// Inspector setup (drag SOs and child MonoBehaviours in Unity):
    ///   _config          → RunConfig SO
    ///   _resourceDefs    → array of ResourceDefinition SOs (food, wood, scrap)
    ///   _upgradeDefs     → array of UpgradeDefinition SOs
    ///   _tickService     → TickService MonoBehaviour
    ///   _resourceStore   → ResourceStore MonoBehaviour
    ///   _workAssignment  → WorkAssignment MonoBehaviour
    ///   _upgradeManager  → UpgradeManager MonoBehaviour
    ///   _autoSave        → AutoSaveService MonoBehaviour
    ///   _gridRenderer    → DebugGridRenderer MonoBehaviour (optional)
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public sealed class GameRoot : MonoBehaviour
    {
        // ── Inspector references ───────────────────────────────────────────
        [Header("Config")]
        [SerializeField] private RunConfigSO           _config;
        [SerializeField] private ResourceDefinitionSO[] _resourceDefs;
        [SerializeField] private UpgradeDefinitionSO[]  _upgradeDefs;

        [Header("Services (child MonoBehaviours)")]
        [SerializeField] private TickService      _tickService;
        [SerializeField] private ResourceStore    _resourceStore;
        [SerializeField] private WorkAssignment   _workAssignment;
        [SerializeField] private UpgradeManager   _upgradeManager;
        [SerializeField] private AutoSaveService  _autoSave;
        [SerializeField] private DebugGridRenderer _gridRenderer;

        // ── Runtime state ──────────────────────────────────────────────────
        private GameState  _state = GameState.Boot;
        private RunData    _runData;
        private IslandGrid _island;
        private ISave      _save;

        // ── Singleton ──────────────────────────────────────────────────────
        public static GameRoot Instance { get; private set; }

        // ── Unity lifecycle ────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            GameLogger.Log(GameLogger.Category.Core, "GameRoot awakened.");

            _save = new JsonSave();

            // Wire TickService auto-tick callback
            _tickService._onAutoTickInternal = () => _tickService.Tick(_runData);
            _tickService.OnTick += HandleTick;
        }

        private void Start()
        {
            if (_save.HasSave())
                LoadGame();
            else
                StartNewRun();
        }

        private void Update()
        {
            if (_state != GameState.Running) return;

            // Speed controls
            if (_config != null && _config.speedMultipliers != null)
            {
                if (Input.GetKeyDown(KeyCode.Alpha1) && _config.speedMultipliers.Length > 0)
                    SetSpeed(0);
                if (Input.GetKeyDown(KeyCode.Alpha2) && _config.speedMultipliers.Length > 1)
                    SetSpeed(1);
                if (Input.GetKeyDown(KeyCode.Alpha3) && _config.speedMultipliers.Length > 2)
                    SetSpeed(2);
            }

            if (Input.GetKeyDown(KeyCode.F5))
                ManualTick();

            if (Input.GetKeyDown(KeyCode.F6))
                DumpState();

            if (Input.GetKeyDown(KeyCode.Escape))
                TransitionTo(GameState.Pause);
        }

        private void OnApplicationQuit()
        {
            _autoSave?.ForceSave();
        }

        // ── Run management ─────────────────────────────────────────────────

        public void StartNewRun()
        {
            int seed = System.Environment.TickCount;

            _runData = new RunData
            {
                tick   = 0,
                player = new PlayerData
                {
                    food  = _config != null ? _config.startingFood  : 10f,
                    wood  = _config != null ? _config.startingWood  : 5f,
                    scrap = _config != null ? _config.startingScrap : 0f,
                },
                world = new WorldData { seed = seed },
            };

            var gen    = new IslandGenerator(_config);
            _runData.world = gen.Generate(seed);

            BootstrapServices(isNewRun: true);
            TransitionTo(GameState.Running);
        }

        public void LoadGame()
        {
            var saveData = _save.Load();
            if (saveData == null) { StartNewRun(); return; }

            _runData = saveData.run;
            BootstrapServices(isNewRun: false);
            TransitionTo(GameState.Running);
        }

        // ── State machine ──────────────────────────────────────────────────

        public void TransitionTo(GameState next)
        {
            GameLogger.Log(GameLogger.Category.Core, $"State: {_state} -> {next}");
            _state = next;

            switch (next)
            {
                case GameState.Running:
                    Time.timeScale = 1f;
                    _tickService.SetRunning(true);
                    break;
                case GameState.Pause:
                    Time.timeScale = 0f;
                    _tickService.SetRunning(false);
                    break;
                case GameState.GameOver:
                case GameState.Won:
                    _tickService.SetRunning(false);
                    _autoSave.ForceSave();
                    break;
                case GameState.Exit:
                    Application.Quit();
                    break;
            }
        }

        // ── Dev commands ───────────────────────────────────────────────────

        public void ManualTick()
        {
            if (_runData == null) return;
            _tickService.Tick(_runData);
        }

        public void DumpState()
        {
            StateSnapshot.Dump(this);
        }

        public void ToggleGridDebug()
        {
            if (_gridRenderer != null)
                _gridRenderer.SetVisible(!_gridRenderer.IsVisible);
        }

        // ── Upgrade API (called from UI in later milestones) ───────────────

        public bool TryBuyUpgrade(string upgradeId) => _upgradeManager.TryBuy(upgradeId);

        // ── Public read-only state ─────────────────────────────────────────

        public GameState CurrentState    => _state;
        public int       CurrentTick     => _tickService != null ? _tickService.CurrentTick : 0;
        public int       Seed            => _runData?.world?.seed ?? 0;
        public float     SpeedMultiplier => _tickService != null ? _tickService.SpeedMultiplier : 1f;
        public int       UpgradeCount    => _upgradeManager != null ? _upgradeManager.PurchasedCount : 0;
        public IslandGrid CurrentIsland  => _island;

        public float GetResource(string id) => _resourceStore != null ? _resourceStore.Get(id) : 0f;

        // ── Private helpers ────────────────────────────────────────────────

        private void BootstrapServices(bool isNewRun)
        {
            // Build runtime island grid
            _island = new IslandGrid(_runData.world);

            // Seed resource store
            var initial = new Dictionary<string, float>
            {
                ["resource.food"]  = _runData.player.food,
                ["resource.wood"]  = _runData.player.wood,
                ["resource.scrap"] = _runData.player.scrap,
            };
            _resourceStore.Initialize(_resourceDefs, initial);
            _workAssignment.Initialize(_island, _resourceStore);
            _upgradeManager.Initialize(_resourceStore, _workAssignment, _upgradeDefs,
                                       isNewRun ? null : new List<string>());

            _autoSave.Initialize(_save, BuildSaveData, _config != null ? _config.autosaveEveryNTicks : 10);
            _tickService.ResetTick(_runData.tick);

            if (_gridRenderer != null)
                _gridRenderer.SetGrid(_island);
        }

        private void HandleTick()
        {
            _resourceStore.OnTick();
            _workAssignment.OnTick();
            _autoSave.OnTick();
        }

        private SaveData BuildSaveData()
        {
            // Sync live resource values back into RunData before saving
            _resourceStore.FlushToPlayerData(_runData.player);
            return new SaveData { run = _runData };
        }

        private void SetSpeed(int index)
        {
            float mult = _config.speedMultipliers[index];
            _tickService.SetSpeed(mult);
            GameLogger.Log(GameLogger.Category.Core, $"Speed set to {mult}x");
        }
    }
}
