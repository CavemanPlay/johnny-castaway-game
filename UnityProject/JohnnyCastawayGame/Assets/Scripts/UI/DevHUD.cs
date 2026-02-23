using System.Collections.Generic;
using UnityEngine;
using JohnnyGame.Core;
using JohnnyGame.Data;

namespace JohnnyGame.UI
{
    /// <summary>
    /// Full game HUD — resources, upgrades shop, escape controls, and debug tools.
    /// Toggle with backtick (`).
    ///
    /// Panel layout:
    ///   Left  (10,10)   — Resources + Status
    ///   Right (300,10)  — Upgrade Shop
    ///   Bottom-left     — Debug tools
    /// </summary>
    public sealed class DevHUD : MonoBehaviour
    {
        private bool     _visible = true;
        private GameRoot _root;

        // Storm flash — show warning for 3 ticks after a storm
        private const int StormFlashTicks = 4;

        private void Awake()
        {
            _root = FindFirstObjectByType<GameRoot>();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.BackQuote))
                _visible = !_visible;
        }

        private void OnGUI()
        {
            if (!_visible) return;
            if (_root == null) { DrawNoRoot(); return; }

            DrawStatusPanel();
            DrawUpgradePanel();
            DrawDebugPanel();
        }

        // ── Status panel (left) ────────────────────────────────────────────

        private void DrawStatusPanel()
        {
            var state = _root.CurrentState;
            int panelH = state == GameState.Running || state == GameState.Pause ? 280 : 180;

            GUILayout.BeginArea(new Rect(10, 10, 270, panelH), GUI.skin.box);

            // Title
            GUI.color = Color.cyan;
            GUILayout.Label("JOHNNY CASTAWAY  [` toggle]");
            GUI.color = Color.white;

            // State
            GUI.color = StateColor(state);
            GUILayout.Label($"State:  {state}");
            GUI.color = Color.white;

            if (state == GameState.Won)
            {
                GUI.color = Color.yellow;
                GUILayout.Label("YOU ESCAPED THE ISLAND!");
                GUILayout.Label("Press [N] to start a new island.");
                GUI.color = Color.white;
                if (GUILayout.Button("New Island  [N]"))
                    _root.StartNewRun();
                GUILayout.EndArea();
                return;
            }

            if (state == GameState.GameOver)
            {
                GUI.color = new Color(1f, 0.4f, 0.4f);
                GUILayout.Label("GAME OVER");
                GUI.color = Color.white;
                if (GUILayout.Button("Try Again"))
                    _root.StartNewRun();
                GUILayout.EndArea();
                return;
            }

            GUILayout.Space(4);

            // Resources
            GUI.color = new Color(0.9f, 0.85f, 0.5f);
            GUILayout.Label("── RESOURCES ──");
            GUI.color = Color.white;

            float food  = _root.GetResource("resource.food");
            float wood  = _root.GetResource("resource.wood");
            float scrap = _root.GetResource("resource.scrap");

            GUILayout.Label($"Food:     {food:F1}");
            GUILayout.Label($"Wood:     {wood:F1}");
            GUILayout.Label($"Scrap:    {scrap:F1}");

            GUILayout.Space(4);

            // Storm warning
            int ticksSinceStorm = _root.CurrentTick - _root.LastStormTick;
            if (ticksSinceStorm >= 0 && ticksSinceStorm < StormFlashTicks)
            {
                GUI.color = new Color(1f, 0.6f, 0.2f);
                GUILayout.Label("⚡ STORM! Food supplies damaged!");
                GUI.color = Color.white;
            }

            GUILayout.Space(4);

            // Escape
            GUI.color = new Color(0.5f, 1f, 0.5f);
            GUILayout.Label("── ESCAPE ──");
            GUI.color = Color.white;

            float esc = _root.EscapeProgress;
            DrawProgressBar(esc, Color.green);
            GUILayout.Label($"Progress: {esc * 100f:F0}%  (need 10 attempts)");

            bool canEscape = wood >= 15f && food >= 15f;
            GUI.color = canEscape ? Color.white : Color.gray;
            if (GUILayout.Button("Attempt Escape  [E]  (15W + 15F)"))
                _root.TryEscape();
            GUI.color = Color.white;

            GUILayout.Space(4);

            // Sim info
            GUILayout.Label($"Tick:   {_root.CurrentTick}   Speed: {_root.SpeedMultiplier}×  [1/2/3]");
            GUILayout.Label($"Seed:   {_root.Seed}");

            if (state == GameState.Pause)
            {
                GUI.color = Color.yellow;
                GUILayout.Label("PAUSED — press [Esc] to resume");
                GUI.color = Color.white;
            }

            GUILayout.EndArea();
        }

        // ── Upgrade shop panel (right) ─────────────────────────────────────

        private void DrawUpgradePanel()
        {
            var defs = _root.UpgradeDefs;
            if (defs == null || defs.Length == 0) return;

            int panelH = 30 + defs.Length * 62;
            GUILayout.BeginArea(new Rect(290, 10, 240, panelH), GUI.skin.box);

            GUI.color = new Color(1f, 0.8f, 0.4f);
            GUILayout.Label("── UPGRADES ──");
            GUI.color = Color.white;

            foreach (var def in defs)
            {
                if (def == null) continue;
                bool owned = _root.IsUpgradeOwned(def.upgradeId);

                if (owned)
                {
                    GUI.color = Color.green;
                    GUILayout.Label($"✓ {def.displayName}");
                    GUI.color = Color.gray;
                    GUILayout.Label($"   {def.description}");
                    GUI.color = Color.white;
                }
                else
                {
                    GUILayout.Label($"{def.displayName}");
                    GUI.color = Color.gray;
                    GUILayout.Label($"   {def.description}");
                    GUI.color = Color.white;

                    bool affordable = CanAfford(def);
                    GUI.color = affordable ? Color.white : Color.gray;
                    string cost = CostString(def);
                    if (GUILayout.Button($"Buy  [{cost}]"))
                        _root.TryBuyUpgrade(def.upgradeId);
                    GUI.color = Color.white;
                }

                GUILayout.Space(2);
            }

            GUILayout.EndArea();
        }

        // ── Debug panel (bottom-left) ──────────────────────────────────────

        private void DrawDebugPanel()
        {
            GUILayout.BeginArea(new Rect(10, 300, 270, 100), GUI.skin.box);

            GUI.color = Color.gray;
            GUILayout.Label("── DEBUG ──");
            GUI.color = Color.white;

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Manual Tick [F5]")) _root.ManualTick();
            if (GUILayout.Button("Dump [F6]"))        _root.DumpState();
            GUILayout.EndHorizontal();

            if (GUILayout.Button("Toggle Island Grid"))
                _root.ToggleGridDebug();

            GUILayout.EndArea();
        }

        // ── Helpers ────────────────────────────────────────────────────────

        private void DrawNoRoot()
        {
            GUILayout.BeginArea(new Rect(10, 10, 260, 60), GUI.skin.box);
            GUILayout.Label("DEV HUD — no GameRoot found");
            GUILayout.EndArea();
        }

        private void DrawProgressBar(float t, Color fill)
        {
            t = Mathf.Clamp01(t);
            Rect full = GUILayoutUtility.GetRect(0f, 16f, GUILayout.ExpandWidth(true));
            Rect bar  = new Rect(full.x, full.y, full.width * t, full.height);
            GUI.color = new Color(0.2f, 0.2f, 0.2f);
            GUI.DrawTexture(full, Texture2D.whiteTexture);
            GUI.color = fill;
            if (t > 0f) GUI.DrawTexture(bar, Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        private bool CanAfford(UpgradeDefinitionSO def)
            =>  _root.GetResource("resource.wood")  >= def.costWood
             && _root.GetResource("resource.food")  >= def.costFood
             && _root.GetResource("resource.scrap") >= def.costScrap;

        private static string CostString(UpgradeDefinitionSO def)
        {
            var parts = new List<string>();
            if (def.costWood  > 0) parts.Add($"{def.costWood}W");
            if (def.costFood  > 0) parts.Add($"{def.costFood}F");
            if (def.costScrap > 0) parts.Add($"{def.costScrap}S");
            return parts.Count > 0 ? string.Join("+", parts) : "free";
        }

        private static Color StateColor(GameState s) => s switch
        {
            GameState.Running  => Color.green,
            GameState.Pause    => Color.yellow,
            GameState.Won      => new Color(1f, 0.9f, 0.2f),
            GameState.GameOver => new Color(1f, 0.3f, 0.3f),
            _                  => Color.white,
        };
    }
}
