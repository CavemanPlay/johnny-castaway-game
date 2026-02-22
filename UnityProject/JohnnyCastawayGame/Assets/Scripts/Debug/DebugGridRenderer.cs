using UnityEngine;
using JohnnyGame.Data;
using JohnnyGame.Simulation;

namespace JohnnyGame.Debugging
{
    /// <summary>
    /// Draws a coloured terrain grid and resource-node dots using OnGUI.
    /// Toggle via the DevHUD "Grid Debug" button or call <see cref="SetVisible"/>.
    /// Attach to any GameObject in the Bootstrap scene.
    /// </summary>
    public sealed class DebugGridRenderer : MonoBehaviour
    {
        [SerializeField] private int   cellPx  = 14;
        [SerializeField] private int   offsetX = 10;
        [SerializeField] private int   offsetY = 210; // below DevHUD

        private bool       _visible;
        private IslandGrid _grid;

        private static readonly Color32 ColOcean    = new Color32( 30,  80, 180, 255);
        private static readonly Color32 ColBeach    = new Color32(230, 210, 130, 255);
        private static readonly Color32 ColClearing = new Color32(150, 200, 100, 255);
        private static readonly Color32 ColForest   = new Color32( 34, 120,  34, 255);
        private static readonly Color32 ColRocky    = new Color32(120, 100,  80, 255);
        private static readonly Color32 ColSpawn    = new Color32(255, 255,   0, 255);
        private static readonly Color32 ColNode     = new Color32(255,  60,  60, 255);

        private Texture2D _pixel;

        private void Awake()
        {
            _pixel = new Texture2D(1, 1);
            _pixel.SetPixel(0, 0, Color.white);
            _pixel.Apply();
        }

        public void SetVisible(bool visible) => _visible = visible;
        public bool IsVisible => _visible;

        public void SetGrid(IslandGrid grid) => _grid = grid;

        private void OnGUI()
        {
            if (!_visible || _grid == null) return;

            GUI.color = Color.white;
            for (int y = 0; y < _grid.Height; y++)
            {
                for (int x = 0; x < _grid.Width; x++)
                {
                    var terrain = _grid.GetTerrain(x, y);
                    GUI.color = TerrainColor(terrain);
                    GUI.DrawTexture(CellRect(x, y), _pixel);
                }
            }

            // Spawn marker
            GUI.color = ColSpawn;
            GUI.DrawTexture(CellRect(_grid.SpawnX, _grid.SpawnY), _pixel);

            // Resource node dots (centre quarter of cell)
            GUI.color = ColNode;
            int dot = Mathf.Max(2, cellPx / 3);
            int dotOff = (cellPx - dot) / 2;
            foreach (var node in _grid.Nodes)
            {
                if (node.amount <= 0f) continue;
                int px = offsetX + node.x * cellPx + dotOff;
                int py = offsetY + node.y * cellPx + dotOff;
                GUI.DrawTexture(new Rect(px, py, dot, dot), _pixel);
            }

            GUI.color = Color.white;
        }

        private Rect CellRect(int x, int y)
            => new Rect(offsetX + x * cellPx, offsetY + y * cellPx, cellPx - 1, cellPx - 1);

        private static Color TerrainColor(TerrainType t) => t switch
        {
            TerrainType.Ocean    => ColOcean,
            TerrainType.Beach    => ColBeach,
            TerrainType.Clearing => ColClearing,
            TerrainType.Forest   => ColForest,
            TerrainType.Rocky    => ColRocky,
            _                    => Color.magenta,
        };
    }
}
