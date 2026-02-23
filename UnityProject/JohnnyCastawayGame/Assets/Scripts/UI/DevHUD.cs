using System.Collections.Generic;
using UnityEngine;
using JohnnyGame.Core;
using JohnnyGame.Data;

namespace JohnnyGame.UI
{
    /// <summary>
    /// Game HUD — resources, upgrades shop, escape controls, and debug tools.
    /// Toggle with backtick (`).
    ///
    /// Visual style: tropical wood/parchment — warm amber panels, golden headers.
    ///
    /// Panel layout:
    ///   Left  (10,10)  — Resources + Escape + Status
    ///   Right (10,top) — Upgrade Shop
    ///   Bottom-left    — Debug strip
    /// </summary>
    public sealed class DevHUD : MonoBehaviour
    {
        // ── State ──────────────────────────────────────────────────────────
        private bool     _visible = true;
        private GameRoot _root;

        // ── Cached styles (built once in Awake) ────────────────────────────
        private GUIStyle _panelStyle;
        private GUIStyle _titleStyle;
        private GUIStyle _headerStyle;
        private GUIStyle _labelStyle;
        private GUIStyle _dimLabel;
        private GUIStyle _warnLabel;
        private GUIStyle _btnStyle;
        private GUIStyle _dimBtn;
        private GUIStyle _wonLabel;
        private bool     _stylesBuilt;

        // Panel background textures
        private Texture2D _panelTex;
        private Texture2D _barBgTex;
        private Texture2D _barFillTex;
        private Texture2D _barFill2Tex;   // red (for low food)
        private Texture2D _btnTex;
        private Texture2D _btnHoverTex;

        // Storm flash
        private const int StormFlashTicks = 4;

        // ── Palette ────────────────────────────────────────────────────────
        static readonly Color PanelBg    = new Color(0.10f, 0.18f, 0.22f, 0.88f);
        static readonly Color Gold       = new Color(0.95f, 0.80f, 0.30f);
        static readonly Color LightTan   = new Color(0.95f, 0.90f, 0.75f);
        static readonly Color DimTan     = new Color(0.65f, 0.60f, 0.45f);
        static readonly Color GreenTrop  = new Color(0.25f, 0.85f, 0.45f);
        static readonly Color RedWarn    = new Color(1.00f, 0.30f, 0.20f);
        static readonly Color OrangeWarn = new Color(1.00f, 0.65f, 0.10f);
        static readonly Color BtnBg      = new Color(0.20f, 0.35f, 0.28f, 0.92f);
        static readonly Color BtnHover   = new Color(0.28f, 0.48f, 0.36f, 0.95f);
        static readonly Color BarBg      = new Color(0.08f, 0.12f, 0.15f, 1f);
        static readonly Color BarGreen   = new Color(0.20f, 0.75f, 0.35f, 1f);
        static readonly Color BarRed     = new Color(0.85f, 0.18f, 0.10f, 1f);

        // ── Lifecycle ──────────────────────────────────────────────────────

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
            EnsureStyles();
            if (_root == null) { DrawNoRoot(); return; }

            DrawStatusPanel();
            DrawUpgradePanel();
            DrawDebugStrip();
        }

        // ── Lazy style builder ─────────────────────────────────────────────

        private void EnsureStyles()
        {
            if (_stylesBuilt) return;
            _stylesBuilt = true;

            _panelTex    = SolidTex(PanelBg);
            _barBgTex    = SolidTex(BarBg);
            _barFillTex  = SolidTex(BarGreen);
            _barFill2Tex = SolidTex(BarRed);
            _btnTex      = SolidTex(BtnBg);
            _btnHoverTex = SolidTex(BtnHover);

            _panelStyle = new GUIStyle(GUI.skin.box)
            {
                normal  = { background = _panelTex, textColor = LightTan },
                border  = new RectOffset(4, 4, 4, 4),
                padding = new RectOffset(8, 8, 8, 8),
            };

            _titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize  = 13,
                fontStyle = FontStyle.Bold,
                normal    = { textColor = Gold },
                alignment = TextAnchor.MiddleCenter,
            };

            _headerStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize  = 11,
                fontStyle = FontStyle.Bold,
                normal    = { textColor = Gold },
            };

            _labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                normal   = { textColor = LightTan },
            };

            _dimLabel = new GUIStyle(_labelStyle)
            {
                normal = { textColor = DimTan },
            };

            _warnLabel = new GUIStyle(_labelStyle)
            {
                fontSize  = 12,
                fontStyle = FontStyle.Bold,
                normal    = { textColor = OrangeWarn },
            };

            _wonLabel = new GUIStyle(_labelStyle)
            {
                fontSize  = 14,
                fontStyle = FontStyle.Bold,
                normal    = { textColor = Gold },
                alignment = TextAnchor.MiddleCenter,
            };

            _btnStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 11,
                normal   = { background = _btnTex,      textColor = LightTan },
                hover    = { background = _btnHoverTex, textColor = Color.white },
                active   = { background = _btnHoverTex, textColor = Color.white },
            };

            _dimBtn = new GUIStyle(_btnStyle)
            {
                normal = { background = _panelTex, textColor = DimTan },
                hover  = { background = _panelTex, textColor = DimTan },
            };
        }

        static Texture2D SolidTex(Color c)
        {
            var t = new Texture2D(2, 2);
            t.SetPixels(new[] { c, c, c, c });
            t.Apply();
            return t;
        }

        // ── Status panel (left) ────────────────────────────────────────────

        private void DrawStatusPanel()
        {
            var state = _root.CurrentState;

            GUILayout.BeginArea(new Rect(10, 10, 268, 340), _panelStyle);

            GUILayout.Label("~ JOHNNY CASTAWAY ~  [` hide]", _titleStyle);
            GUILayout.Space(2);

            // Game state badge
            Color stateCol = StateColor(state);
            var stateLbl = new GUIStyle(_labelStyle) { normal = { textColor = stateCol }, fontStyle = FontStyle.Bold };
            GUILayout.Label($"  {state}", stateLbl);

            // ── Win / Game Over screens ─────────────────────────────────
            if (state == GameState.Won)
            {
                GUILayout.Space(8);
                GUILayout.Label("YOU ESCAPED THE ISLAND!", _wonLabel);
                GUILayout.Space(4);
                GUILayout.Label("Johnny waves goodbye from the rescue boat.", _dimLabel);
                GUILayout.Space(8);
                if (GUILayout.Button("New Island  [N]", _btnStyle))
                    _root.StartNewRun();
                GUILayout.EndArea();
                return;
            }

            if (state == GameState.GameOver)
            {
                GUILayout.Space(8);
                var goLbl = new GUIStyle(_wonLabel) { normal = { textColor = RedWarn } };
                GUILayout.Label("GAME OVER", goLbl);
                GUILayout.Space(8);
                if (GUILayout.Button("Try Again", _btnStyle))
                    _root.StartNewRun();
                GUILayout.EndArea();
                return;
            }

            GUILayout.Space(6);
            Divider();

            // ── Resources ──────────────────────────────────────────────
            GUILayout.Label("RESOURCES", _headerStyle);

            float food  = _root.GetResource("resource.food");
            float wood  = _root.GetResource("resource.wood");
            float scrap = _root.GetResource("resource.scrap");

            GUILayout.Label($"  Fish / Food    {food:F1}", _labelStyle);
            GUILayout.Label($"  Palm Wood      {wood:F1}", _labelStyle);
            GUILayout.Label($"  Scrap Metal    {scrap:F1}", _labelStyle);

            // Storm warning
            int ticksSinceStorm = _root.CurrentTick - _root.LastStormTick;
            if (ticksSinceStorm >= 0 && ticksSinceStorm < StormFlashTicks)
            {
                GUILayout.Space(3);
                GUILayout.Label("!! STORM — food supplies raided !!", _warnLabel);
            }

            GUILayout.Space(6);
            Divider();

            // ── Escape ─────────────────────────────────────────────────
            GUILayout.Label("ESCAPE PROGRESS", _headerStyle);
            float esc = _root.EscapeProgress;
            StyledBar(esc, food < 5f);
            GUILayout.Label($"  {esc * 100f:F0}%  —  10 attempts needed", _dimLabel);

            bool canEscape = wood >= 15f && food >= 15f;
            GUIStyle escBtn = canEscape ? _btnStyle : _dimBtn;
            if (GUILayout.Button("Attempt Escape  [E]  (15W + 15F)", escBtn) && canEscape)
                _root.TryEscape();

            GUILayout.Space(6);
            Divider();

            // ── Speed / tick info ───────────────────────────────────────
            GUILayout.Label($"  Tick {_root.CurrentTick}   Speed {_root.SpeedMultiplier:F0}x  [1/2/3]",
                            _dimLabel);

            if (state == GameState.Pause)
            {
                GUILayout.Space(2);
                var pauseLbl = new GUIStyle(_warnLabel) { normal = { textColor = Color.yellow } };
                GUILayout.Label("  PAUSED — [Esc] to resume", pauseLbl);
            }

            GUILayout.EndArea();
        }

        // ── Upgrade shop (right of status panel) ──────────────────────────

        private void DrawUpgradePanel()
        {
            var defs = _root.UpgradeDefs;
            if (defs == null || defs.Length == 0) return;

            int panelH = 28 + defs.Length * 66;
            GUILayout.BeginArea(new Rect(288, 10, 238, panelH), _panelStyle);

            GUILayout.Label("UPGRADES", _headerStyle);
            GUILayout.Space(2);

            foreach (var def in defs)
            {
                if (def == null) continue;
                bool owned = _root.IsUpgradeOwned(def.upgradeId);

                if (owned)
                {
                    var ownedLbl = new GUIStyle(_labelStyle) { normal = { textColor = GreenTrop } };
                    GUILayout.Label($"[OK] {def.displayName}", ownedLbl);
                    GUILayout.Label($"     {def.description}", _dimLabel);
                }
                else
                {
                    GUILayout.Label(def.displayName, _labelStyle);
                    GUILayout.Label($"     {def.description}", _dimLabel);
                    bool affordable = CanAfford(def);
                    if (GUILayout.Button($"Buy  [{CostString(def)}]", affordable ? _btnStyle : _dimBtn)
                        && affordable)
                        _root.TryBuyUpgrade(def.upgradeId);
                }
                GUILayout.Space(3);
            }

            GUILayout.EndArea();
        }

        // ── Debug strip (bottom) ───────────────────────────────────────────

        private void DrawDebugStrip()
        {
            GUILayout.BeginArea(new Rect(10, 360, 516, 40), _panelStyle);
            GUILayout.BeginHorizontal();
            GUILayout.Label("DEBUG:", _dimLabel);
            if (GUILayout.Button("Tick [F5]",    _btnStyle, GUILayout.Width(80)))  _root.ManualTick();
            if (GUILayout.Button("Dump [F6]",    _btnStyle, GUILayout.Width(80)))  _root.DumpState();
            if (GUILayout.Button("Grid Overlay", _btnStyle, GUILayout.Width(100))) _root.ToggleGridDebug();
            if (GUILayout.Button("Seed: " + _root.Seed, _dimBtn, GUILayout.Width(130))) { /* info only */ }
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        // ── Helpers ────────────────────────────────────────────────────────

        private void DrawNoRoot()
        {
            GUILayout.BeginArea(new Rect(10, 10, 260, 50), _panelStyle);
            GUILayout.Label("HUD — no GameRoot found", _dimLabel);
            GUILayout.EndArea();
        }

        private void Divider()
        {
            var r = GUILayoutUtility.GetRect(0, 1, GUILayout.ExpandWidth(true));
            GUI.color = new Color(0.5f, 0.45f, 0.25f, 0.8f);
            GUI.DrawTexture(r, Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUILayout.Space(4);
        }

        private void StyledBar(float t, bool danger = false)
        {
            t = Mathf.Clamp01(t);
            Rect full = GUILayoutUtility.GetRect(0f, 12f, GUILayout.ExpandWidth(true));
            GUI.DrawTexture(full, _barBgTex);
            if (t > 0f)
            {
                var fill = new Rect(full.x, full.y, full.width * t, full.height);
                GUI.DrawTexture(fill, danger ? _barFill2Tex : _barFillTex);
            }
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
            GameState.Running  => GreenTrop,
            GameState.Pause    => Color.yellow,
            GameState.Won      => new Color(1f, 0.9f, 0.2f),
            GameState.GameOver => RedWarn,
            _                  => Color.white,
        };
    }
}
