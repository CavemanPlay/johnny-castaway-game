using UnityEngine;
using JohnnyGame.Core;

namespace JohnnyGame.UI
{
    /// <summary>
    /// Developer overlay HUD. Shows live game state, resources, and speed.
    /// Toggle visibility with backtick (`). See Docs/Debug.md for full usage.
    /// </summary>
    public sealed class DevHUD : MonoBehaviour
    {
        private bool     _visible = true;
        private GameRoot _root;

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

            GUILayout.BeginArea(new Rect(10, 10, 280, 230), GUI.skin.box);
            GUILayout.Label("DEV HUD  [` to toggle]");
            GUILayout.Space(4);

            if (_root != null)
            {
                GUILayout.Label($"State:     {_root.CurrentState}");
                GUILayout.Label($"Tick:      {_root.CurrentTick}");
                GUILayout.Label($"Seed:      {_root.Seed}");
                GUILayout.Label($"Speed:     {_root.SpeedMultiplier}x  [1/2/3]");
                GUILayout.Label($"Upgrades:  {_root.UpgradeCount}");
                GUILayout.Space(4);
                GUILayout.Label($"Food:      {_root.GetResource("resource.food"):F1}");
                GUILayout.Label($"Wood:      {_root.GetResource("resource.wood"):F1}");
                GUILayout.Label($"Scrap:     {_root.GetResource("resource.scrap"):F1}");
                GUILayout.Space(4);

                if (GUILayout.Button("Manual Tick  [F5]"))
                    _root.ManualTick();

                if (GUILayout.Button("Dump State   [F6]"))
                    _root.DumpState();

                if (GUILayout.Button("Toggle Grid Debug"))
                    _root.ToggleGridDebug();
            }
            else
            {
                GUILayout.Label("(no GameRoot found in scene)");
            }

            GUILayout.EndArea();
        }
    }
}
