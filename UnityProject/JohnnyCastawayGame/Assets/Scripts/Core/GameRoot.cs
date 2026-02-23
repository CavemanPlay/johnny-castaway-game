using System.Collections.Generic;
using UnityEngine;
using JohnnyGame.Data;
using JohnnyGame.Debugging;
using JohnnyGame.Rendering;
using JohnnyGame.Simulation;
using JohnnyGame.UI;

namespace JohnnyGame.Core
{
    /// <summary>
    /// Entry point, singleton, and composition root.
    ///
    /// ZERO SETUP REQUIRED: GameBootstrap.cs creates this automatically on Play.
    /// If you want to configure values, assign [SerializeField] fields in the inspector.
    /// If left empty, sensible defaults are created at runtime.
    ///
    /// Keyboard shortcuts (during Running state):
    ///   1 / 2 / 3  — set speed 1×/2×/4×
    ///   E          — attempt island escape (costs 15 wood + 15 food)
    ///   F5         — manual tick
    ///   F6         — dump state to console
    ///   Escape     — toggle pause
    ///   `          — toggle DevHUD
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public sealed class GameRoot : MonoBehaviour
    {
        // ── Inspector refs (optional — all auto-created if null) ───────────
        [Header("Config (auto-created if unassigned)")]
        [SerializeField] private RunConfigSO            _config;
        [SerializeField] private ResourceDefinitionSO[] _resourceDefs;
        [SerializeField] private UpgradeDefinitionSO[]  _upgradeDefs;

        [Header("Services (auto-created as children if unassigned)")]
        [SerializeField] private TickService       _tickService;
        [SerializeField] private ResourceStore     _resourceStore;
        [SerializeField] private WorkAssignment    _workAssignment;
        [SerializeField] private UpgradeManager    _upgradeManager;
        [SerializeField] private AutoSaveService   _autoSave;
        [SerializeField] private DebugGridRenderer _gridRenderer;
        [SerializeField] private DevHUD            _devHUD;
        [SerializeField] private IslandSceneView   _sceneView;

        // ── Runtime state ──────────────────────────────────────────────────
        private GameState  _state = GameState.Boot;
        private RunData    _runData;
        private IslandGrid _island;
        private ISave      _save;
        private int        _lastStormTick = -999;

        // ── Singleton ──────────────────────────────────────────────────────
        public static GameRoot Instance { get; private set; }

        // ── Unity lifecycle ────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            GameLogger.Log(GameLogger.Category.Core, "GameRoot awakened.");

            EnsureConfig();
            EnsureServices();

            _save = new JsonSave();

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
            if (_state == GameState.Running)
            {
                HandleSpeedKeys();

                if (Input.GetKeyDown(KeyCode.E))          TryEscape();
                if (Input.GetKeyDown(KeyCode.F5))         ManualTick();
                if (Input.GetKeyDown(KeyCode.F6))         DumpState();
                if (Input.GetKeyDown(KeyCode.Escape))     TransitionTo(GameState.Pause);
            }
            else if (_state == GameState.Pause)
            {
                if (Input.GetKeyDown(KeyCode.Escape))     TransitionTo(GameState.Running);
            }
        }

        private void OnApplicationQuit() => _autoSave?.ForceSave();

        // ── Run management ─────────────────────────────────────────────────

        public void StartNewRun()
        {
            int seed = System.Environment.TickCount;

            _runData = new RunData
            {
                tick   = 0,
                player = new PlayerData
                {
                    food  = _config.startingFood,
                    wood  = _config.startingWood,
                    scrap = _config.startingScrap,
                },
                world = new WorldData { seed = seed },
            };

            _runData.world = new IslandGenerator(_config).Generate(seed);

            BootstrapServices(isNewRun: true);
            TransitionTo(GameState.Running);
        }

        public void LoadGame()
        {
            var saveData = _save.Load();

            // Reject saves missing island data (pre-M1 or corrupt)
            if (saveData == null
                || saveData.run == null
                || saveData.run.world == null
                || saveData.run.world.terrainFlat == null
                || saveData.run.world.terrainFlat.Length == 0)
            {
                GameLogger.LogWarning(GameLogger.Category.Save, "Save invalid or pre-M1 — starting fresh.");
                _save.DeleteSave();
                StartNewRun();
                return;
            }

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
                case GameState.Won:
                case GameState.GameOver:
                    Time.timeScale = 0f;
                    _tickService.SetRunning(false);
                    _autoSave?.ForceSave();
                    break;
                case GameState.Exit:
                    Application.Quit();
                    break;
            }
        }

        // ── Player actions ─────────────────────────────────────────────────

        /// <summary>Attempt to escape. Costs 15 wood + 15 food, adds 10% progress.</summary>
        public bool TryEscape()
        {
            if (_state != GameState.Running || _runData == null) return false;

            const float woodCost = 15f, foodCost = 15f;
            if (_resourceStore.Get("resource.wood") < woodCost ||
                _resourceStore.Get("resource.food") < foodCost)
            {
                GameLogger.Log(GameLogger.Category.Core,
                    "Escape attempt failed — need 15 wood + 15 food.");
                return false;
            }

            _resourceStore.TrySpend("resource.wood", woodCost);
            _resourceStore.TrySpend("resource.food", foodCost);
            _runData.player.escapeProgress = Mathf.Min(1f, _runData.player.escapeProgress + 0.1f);

            GameLogger.Log(GameLogger.Category.Core,
                $"Escape attempt! Progress: {_runData.player.escapeProgress * 100f:F0}%");

            if (_runData.player.escapeProgress >= 1f)
            {
                GameLogger.Log(GameLogger.Category.Core, "YOU ESCAPED THE ISLAND!");
                TransitionTo(GameState.Won);
            }

            return true;
        }

        public bool TryBuyUpgrade(string upgradeId) => _upgradeManager?.TryBuy(upgradeId) ?? false;

        // ── Dev commands ───────────────────────────────────────────────────

        public void ManualTick()
        {
            if (_runData == null) return;
            _tickService.Tick(_runData);
        }

        public void DumpState() => StateSnapshot.Dump(this);

        public void ToggleGridDebug()
        {
            if (_gridRenderer != null)
                _gridRenderer.SetVisible(!_gridRenderer.IsVisible);
        }

        // ── Public read-only state ─────────────────────────────────────────

        public GameState CurrentState    => _state;
        public int       CurrentTick     => _tickService != null ? _tickService.CurrentTick : 0;
        public int       Seed            => _runData?.world?.seed ?? 0;
        public float     SpeedMultiplier => _tickService != null ? _tickService.SpeedMultiplier : 1f;
        public int       UpgradeCount    => _upgradeManager != null ? _upgradeManager.PurchasedCount : 0;
        public IslandGrid CurrentIsland  => _island;
        public float     EscapeProgress  => _runData?.player.escapeProgress ?? 0f;
        public int       LastStormTick   => _lastStormTick;

        public float GetResource(string id) => _resourceStore?.Get(id) ?? 0f;

        public UpgradeDefinitionSO[] UpgradeDefs => _upgradeDefs;
        public bool IsUpgradeOwned(string id)    => _upgradeManager?.IsOwned(id) ?? false;

        // ── Private helpers ────────────────────────────────────────────────

        private void EnsureConfig()
        {
            if (_config == null)            _config       = BuildDefaultRunConfig();
            if (_resourceDefs == null || _resourceDefs.Length == 0)
                _resourceDefs = BuildDefaultResourceDefs();
            if (_upgradeDefs == null || _upgradeDefs.Length == 0)
                _upgradeDefs  = BuildDefaultUpgradeDefs();
        }

        private void EnsureServices()
        {
            if (_tickService    == null) _tickService    = CreateChild<TickService>("TickService");
            _tickService.SetConfig(_config);

            if (_resourceStore  == null) _resourceStore  = CreateChild<ResourceStore>("ResourceStore");
            if (_workAssignment == null) _workAssignment = CreateChild<WorkAssignment>("WorkAssignment");
            if (_upgradeManager == null) _upgradeManager = CreateChild<UpgradeManager>("UpgradeManager");
            if (_autoSave       == null) _autoSave       = CreateChild<AutoSaveService>("AutoSaveService");
            if (_gridRenderer   == null) _gridRenderer   = CreateChild<DebugGridRenderer>("DebugGridRenderer");
            if (_devHUD         == null) _devHUD         = CreateChild<DevHUD>("DevHUD");
            if (_sceneView      == null) _sceneView      = CreateChild<IslandSceneView>("IslandSceneView");
        }

        private T CreateChild<T>(string childName) where T : MonoBehaviour
        {
            var go = new GameObject(childName);
            go.transform.SetParent(transform);
            return go.AddComponent<T>();
        }

        private void BootstrapServices(bool isNewRun)
        {
            _island = new IslandGrid(_runData.world);

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
            _autoSave.Initialize(_save, BuildSaveData, _config.autosaveEveryNTicks);
            _tickService.ResetTick(_runData.tick);

            _gridRenderer.SetGrid(_island);

            GameLogger.Log(GameLogger.Category.Core,
                $"Services bootstrapped. Island {_island.Width}×{_island.Height}, {_island.Nodes.Length} nodes.");
        }

        private void HandleTick()
        {
            _resourceStore.OnTick();
            _workAssignment.OnTick();

            // Storm hazard: ~5% chance per tick
            if (UnityEngine.Random.value < 0.05f)
            {
                float dmg = UnityEngine.Random.Range(3f, 10f);
                _resourceStore.TrySpend("resource.food", dmg);
                _lastStormTick = CurrentTick;
                _sceneView?.NotifyStorm();
                GameLogger.LogWarning(GameLogger.Category.Sim, $"Storm! Lost {dmg:F1} food.");
            }

            _autoSave.OnTick();
        }

        private void HandleSpeedKeys()
        {
            if (_config.speedMultipliers == null || _config.speedMultipliers.Length == 0) return;
            if (Input.GetKeyDown(KeyCode.Alpha1) && _config.speedMultipliers.Length > 0) SetSpeed(0);
            if (Input.GetKeyDown(KeyCode.Alpha2) && _config.speedMultipliers.Length > 1) SetSpeed(1);
            if (Input.GetKeyDown(KeyCode.Alpha3) && _config.speedMultipliers.Length > 2) SetSpeed(2);
        }

        private void SetSpeed(int index)
        {
            float mult = _config.speedMultipliers[index];
            _tickService.SetSpeed(mult);
            GameLogger.Log(GameLogger.Category.Core, $"Speed: {mult}×");
        }

        private SaveData BuildSaveData()
        {
            _resourceStore.FlushToPlayerData(_runData.player);
            _runData.tick = CurrentTick;
            return new SaveData { run = _runData };
        }

        // ── Default SO factories ───────────────────────────────────────────

        private static RunConfigSO BuildDefaultRunConfig()
        {
            var c = ScriptableObject.CreateInstance<RunConfigSO>();
            c.tickIntervalSeconds  = 1f;
            c.autosaveEveryNTicks  = 10;
            c.speedMultipliers     = new[] { 1f, 2f, 4f };
            c.islandWidth          = 20;
            c.islandHeight         = 20;
            c.resourceDensity      = 0.15f;
            c.startingFood         = 10f;
            c.startingWood         = 5f;
            c.startingScrap        = 0f;
            return c;
        }

        private static ResourceDefinitionSO[] BuildDefaultResourceDefs()
        {
            return new[]
            {
                MakeResDef("resource.food",  "Food",  10f, gatherRate: 1.5f, income: 0.05f),
                MakeResDef("resource.wood",  "Wood",   5f, gatherRate: 1.0f, income: 0f),
                MakeResDef("resource.scrap", "Scrap",  0f, gatherRate: 0.5f, income: 0f),
            };
        }

        private static ResourceDefinitionSO MakeResDef(string id, string display, float start,
                                                        float gatherRate = 1f, float income = 0f)
        {
            var d = ScriptableObject.CreateInstance<ResourceDefinitionSO>();
            d.resourceId        = id;
            d.displayName       = display;
            d.startingAmount    = start;
            d.maxAmount         = 9999f;
            d.baseIncomePerTick = income;
            d.decayPerTick      = 0f;
            d.gatherRatePerTick = gatherRate;
            d.nodeMaxAmount     = 50f;
            return d;
        }

        private static UpgradeDefinitionSO[] BuildDefaultUpgradeDefs()
        {
            return new[]
            {
                MakeUpgrade("upgrade.better-axe",    "Better Axe",
                            "Wood gather rate ×2",
                            costWood: 10f,
                            effectType: "gather_multiplier", effectValue: 2f,
                            target: "resource.wood"),

                MakeUpgrade("upgrade.fish-trap",     "Fish Trap",
                            "+1 food per tick (passive)",
                            costWood: 5f, costScrap: 2f,
                            effectType: "food_income", effectValue: 1f),

                MakeUpgrade("upgrade.debris-sifter", "Debris Sifter",
                            "+0.5 scrap per tick (passive)",
                            costFood: 8f,
                            effectType: "scrap_income", effectValue: 0.5f),
            };
        }

        private static UpgradeDefinitionSO MakeUpgrade(string id, string display, string desc,
                                                        float costWood = 0, float costFood = 0,
                                                        float costScrap = 0,
                                                        string effectType = "", float effectValue = 0,
                                                        string target = "")
        {
            var d = ScriptableObject.CreateInstance<UpgradeDefinitionSO>();
            d.upgradeId              = id;
            d.displayName            = display;
            d.description            = desc;
            d.costWood               = costWood;
            d.costFood               = costFood;
            d.costScrap              = costScrap;
            d.effectType             = effectType;
            d.effectValue            = effectValue;
            d.effectTargetResourceId = target;
            return d;
        }
    }
}
