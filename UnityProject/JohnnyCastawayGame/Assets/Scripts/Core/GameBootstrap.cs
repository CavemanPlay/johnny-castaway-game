using UnityEngine;

namespace JohnnyGame.Core
{
    /// <summary>
    /// Ensures a GameRoot exists in every scene at runtime, even without manual scene setup.
    /// Fires after the scene has loaded; if GameRoot is already present (from inspector), this
    /// is a no-op. The GameRoot's own Awake self-bootstraps all child services.
    /// </summary>
    public static class GameBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureGameRoot()
        {
            if (GameRoot.Instance != null) return;

            var go = new GameObject("[GameRoot]");
            go.AddComponent<GameRoot>();
            // GameRoot.Awake fires immediately, sets Instance, and creates all child services.
        }
    }
}
