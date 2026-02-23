using System.Collections.Generic;
using UnityEngine;
using JohnnyGame.Core;

namespace JohnnyGame.Rendering
{
    /// <summary>
    /// Renders the full island scene using procedurally-generated pixel-art sprites.
    /// Auto-created as a child of GameRoot — no scene setup required.
    ///
    /// Layer order (back to front):
    ///   Sky (-200) → OceanBase (-180) → OceanWaves (-160) → Rock (-120)
    ///   Island (-100) → Dock (-90) → Palm (-80) → Campfire (-60)
    ///   Johnny (-40)
    /// </summary>
    [DefaultExecutionOrder(-50)]
    public class IslandSceneView : MonoBehaviour
    {
        // ── Scene objects ──────────────────────────────────────────────────
        Camera     _cam;
        GameObject _skyGO, _oceanBaseGO, _oceanWavesGO;
        GameObject _islandGO, _rockGO, _palmGO, _campfireGO, _johnnyGO, _dockGO;

        // ── Renderers used for animation ───────────────────────────────────
        MeshRenderer   _oceanMR;
        SpriteRenderer _fireSR, _johnnySR, _palmSR;

        // ── Animation sprites ──────────────────────────────────────────────
        Sprite[] _fireSprites   = new Sprite[3];
        Sprite[] _johnnySprites = new Sprite[4];

        // ── Timers / state ─────────────────────────────────────────────────
        float _fireTimer, _johnnyTimer;
        int   _fireIdx, _johnnySeqIdx;

        // Johnny idle sequence: neutral, look-l, neutral, look-r, neutral, wave, neutral, neutral
        static readonly int[] IdleSeq = { 0, 1, 0, 2, 0, 3, 0, 0 };

        bool  _storming;
        float _stormTime;
        Color _skyColorNormal;
        static readonly Color _skyColorStorm = new Color(0.22f, 0.30f, 0.45f);

        const float FireFPS   = 0.10f;
        const float IdleGap   = 3.5f;
        const float WaveSpeed = 0.04f;
        const float PalmSway  = 3f;    // max degrees
        const float PalmSpeed = 2.3f;  // radians/second

        // ── Debris system ──────────────────────────────────────────────────

        class DebrisItem
        {
            public GameObject    go;
            public SpriteRenderer sr;
            public float          speed;  // world units/sec (positive=right, negative=left)
            public float          baseY;
            public float          phase;  // random phase for bobbing
        }

        readonly List<DebrisItem> _debris = new List<DebrisItem>();
        float _debrisTimer;
        float _nextSpawn;
        readonly Sprite[] _debrisSprites = new Sprite[3]; // types 0, 1, 2

        const float DebrisSpawnMin = 6f;
        const float DebrisSpawnMax = 16f;

        // Subtle GUI style for "[Click]" hints — built lazily
        GUIStyle _hintStyle;

        // ── Public events / API ────────────────────────────────────────────

        /// <summary>Fired when the player clicks a debris item.</summary>
        public System.Action OnDebrisCollected;

        // ── Lifecycle ──────────────────────────────────────────────────────

        void Awake()
        {
            SetupCamera();
            GenerateAndBuildScene();
            BuildDebrisSprites();
        }

        void Start()
        {
            _nextSpawn = Random.Range(3f, 8f);
        }

        void Update()
        {
            AnimateFire();
            AnimateJohnny();
            AnimateOcean();
            AnimatePalm();
            if (_storming) TickStorm();
            UpdateDebris();
        }

        void OnGUI()
        {
            DrawDebrisHints();
        }

        // ── Public API ─────────────────────────────────────────────────────

        /// <summary>Triggers a 5-second storm visual effect.</summary>
        public void NotifyStorm()
        {
            _storming  = true;
            _stormTime = 5f;
        }

        /// <summary>Shows or hides the dock sprite.</summary>
        public void SetDockBuilt(bool built)
        {
            if (_dockGO != null) _dockGO.SetActive(built);
        }

        // ── Camera ─────────────────────────────────────────────────────────

        void SetupCamera()
        {
            _cam = Camera.main;
            if (_cam == null)
            {
                var cgo = new GameObject("MainCamera") { tag = "MainCamera" };
                _cam = cgo.AddComponent<Camera>();
                if (FindObjectOfType<AudioListener>() == null)
                    cgo.AddComponent<AudioListener>();
            }
            _cam.orthographic       = true;
            _cam.orthographicSize   = 4f;          // 8 world-units tall
            _cam.transform.position = new Vector3(0f, 0f, -10f);
            _cam.clearFlags         = CameraClearFlags.SolidColor;
            _cam.backgroundColor    = new Color(0.35f, 0.76f, 0.94f);
            _cam.nearClipPlane      = 0.1f;
            _cam.farClipPlane       = 20f;
            _skyColorNormal         = _cam.backgroundColor;
        }

        // ── Scene construction ─────────────────────────────────────────────

        void GenerateAndBuildScene()
        {
            // ── Sky: full-screen static quad (camera is 8 units tall, ~14.2 wide at 16:9)
            _skyGO = Quad("Sky", 0f, 0f, 15f, 9f,
                          TextureFactory.CreateSky(256, 160), -200);

            // ── Ocean base: static dark gradient, covers y=-4 to y=1 (height=5, centre=-1.5)
            var oceanBaseTex = TextureFactory.CreateOceanBase();
            _oceanBaseGO = Quad("OceanBase", 0f, -1.5f, 15f, 5f, oceanBaseTex, -180);

            // ── Ocean waves: tiling UV-animated quad over base
            var oceanTex = TextureFactory.CreateOcean(128, 64);
            oceanTex.wrapMode = TextureWrapMode.Repeat;
            _oceanWavesGO = Quad("OceanWaves", 0f, -1.5f, 15f, 5f, oceanTex, -160);
            _oceanMR = _oceanWavesGO.GetComponent<MeshRenderer>();
            _oceanMR.material.mainTextureScale = new Vector2(4f, 1.2f);

            // ── Rock: partially submerged boulder to the left
            // 56×44 at ppu=22 → 2.54×2.0 wu, bottom at y=-2.2, top at y=-0.2 (above waterline)
            _rockGO = SprObj("Rock",
                             TextureFactory.CreateRock(),
                             -5.2f, -2.2f, -120, ppu: 22f, pivotY: 0f);

            // ── Island: large oval
            // 256×160 at ppu=36 → 7.11×4.44 wu; place bottom at y=-4.2 so sandy top ≈ y=-0.4
            _islandGO = SprObj("Island",
                               TextureFactory.CreateIsland(256, 160),
                               0f, -4.2f, -100, ppu: 36f, pivotY: 0f);

            // ── Dock: hidden until built
            // 72×48 at ppu=34 → 2.12×1.41 wu
            _dockGO = SprObj("Dock",
                             TextureFactory.CreateDock(),
                             3.8f, -1.4f, -90, ppu: 34f, pivotY: 0f);
            _dockGO.SetActive(false);

            // ── Palm tree: right side of island
            // 96×144 at ppu=42 → 2.28×3.43 wu
            _palmGO = SprObj("PalmTree",
                             TextureFactory.CreatePalmTree(96, 144),
                             1.8f, -1.2f, -80, ppu: 42f, pivotY: 0f);
            _palmSR = _palmGO.GetComponent<SpriteRenderer>();

            // ── Campfire: animated 3 frames
            // 24×28 at ppu=46 → 0.52×0.61 wu
            var fireTex = TextureFactory.CreateCampfireFrames();
            for (int i = 0; i < 3; i++)
                _fireSprites[i] = MakeSprite(fireTex[i], 0.5f, 0f, 46f);
            _campfireGO = SprObj("Campfire", fireTex[0], -1.4f, -1.6f, -60, ppu: 46f, pivotY: 0f);
            _fireSR     = _campfireGO.GetComponent<SpriteRenderer>();

            // ── Johnny: animated 4 frames
            // 48×72 at ppu=46 → 1.04×1.56 wu
            var johnTex = TextureFactory.CreateJohnnyFrames();
            for (int i = 0; i < 4; i++)
                _johnnySprites[i] = MakeSprite(johnTex[i], 0.5f, 0f, 46f);
            _johnnyGO = SprObj("Johnny", johnTex[0], -0.4f, -1.5f, -40, ppu: 46f, pivotY: 0f);
            _johnnySR = _johnnyGO.GetComponent<SpriteRenderer>();
        }

        void BuildDebrisSprites()
        {
            for (int i = 0; i < 3; i++)
            {
                var tex = TextureFactory.CreateDebrisItem(i);
                _debrisSprites[i] = MakeSprite(tex, 0.5f, 0.5f, 26f);
            }
        }

        // ── Factory helpers ────────────────────────────────────────────────

        GameObject Quad(string name, float x, float y, float w, float h,
                        Texture2D tex, int sortOrder)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform);
            go.transform.localPosition = new Vector3(x, y, 0f);

            var mesh = new Mesh { name = name };
            mesh.vertices = new[]
            {
                new Vector3(-w * 0.5f, -h * 0.5f, 0f),
                new Vector3( w * 0.5f, -h * 0.5f, 0f),
                new Vector3(-w * 0.5f,  h * 0.5f, 0f),
                new Vector3( w * 0.5f,  h * 0.5f, 0f)
            };
            mesh.uv        = new[] { Vector2.zero, Vector2.right, Vector2.up, Vector2.one };
            mesh.triangles = new[] { 0, 2, 1, 2, 3, 1 };
            mesh.RecalculateNormals();

            go.AddComponent<MeshFilter>().mesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.material     = new Material(Shader.Find("Sprites/Default")) { mainTexture = tex };
            mr.sortingOrder = sortOrder;
            return go;
        }

        GameObject SprObj(string name, Texture2D tex,
                          float x, float y, int sortOrder,
                          float ppu, float pivotY = 0f)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform);
            go.transform.localPosition = new Vector3(x, y, 0f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite       = MakeSprite(tex, 0.5f, pivotY, ppu);
            sr.sortingOrder = sortOrder;
            return go;
        }

        static Sprite MakeSprite(Texture2D tex, float pivotX, float pivotY, float ppu)
            => Sprite.Create(tex,
                             new Rect(0, 0, tex.width, tex.height),
                             new Vector2(pivotX, pivotY),
                             ppu, 0, SpriteMeshType.FullRect);

        // ── Animations ─────────────────────────────────────────────────────

        void AnimateFire()
        {
            _fireTimer += Time.deltaTime;
            if (_fireTimer < FireFPS) return;
            _fireTimer = 0f;
            _fireIdx   = (_fireIdx + 1) % 3;
            if (_fireSR != null) _fireSR.sprite = _fireSprites[_fireIdx];
        }

        void AnimateJohnny()
        {
            _johnnyTimer += Time.deltaTime;
            if (_johnnyTimer < IdleGap) return;
            _johnnyTimer  = 0f;
            _johnnySeqIdx = (_johnnySeqIdx + 1) % IdleSeq.Length;
            if (_johnnySR != null) _johnnySR.sprite = _johnnySprites[IdleSeq[_johnnySeqIdx]];
        }

        void AnimateOcean()
        {
            if (_oceanMR == null) return;
            float speed = _storming ? WaveSpeed * 2f : WaveSpeed;
            float t     = Time.time * speed;
            _oceanMR.material.mainTextureOffset = new Vector2(t, t * 0.06f);
        }

        void AnimatePalm()
        {
            if (_palmGO == null) return;
            float swayMult = _storming ? 2.5f : 1f;
            float angle    = Mathf.Sin(Time.time * PalmSpeed) * PalmSway * swayMult;
            _palmGO.transform.localRotation = Quaternion.Euler(0f, 0f, angle);
        }

        void TickStorm()
        {
            _stormTime -= Time.deltaTime;
            bool wasStorming = _storming;
            if (_stormTime <= 0f) { _stormTime = 0f; _storming = false; }

            float t = Mathf.Clamp01(_stormTime / 5f);
            _cam.backgroundColor = Color.Lerp(_skyColorNormal, _skyColorStorm, t);
            if (!_storming && wasStorming)
                _cam.backgroundColor = _skyColorNormal;
        }

        // ── Debris system ──────────────────────────────────────────────────

        void UpdateDebris()
        {
            // Spawn
            _debrisTimer += Time.deltaTime;
            if (_debrisTimer >= _nextSpawn && _debris.Count < 4)
            {
                _debrisTimer = 0f;
                _nextSpawn   = Random.Range(DebrisSpawnMin, DebrisSpawnMax);
                SpawnDebris();
            }

            // Move and cull off-screen items
            for (int i = _debris.Count - 1; i >= 0; i--)
            {
                var item = _debris[i];
                if (item.go == null) { _debris.RemoveAt(i); continue; }

                var pos = item.go.transform.localPosition;
                pos.x += item.speed * Time.deltaTime;
                pos.y  = item.baseY + Mathf.Sin(Time.time * 1.8f + item.phase) * 0.10f;
                item.go.transform.localPosition = pos;

                if (Mathf.Abs(pos.x) > 10f)
                {
                    Destroy(item.go);
                    _debris.RemoveAt(i);
                }
            }

            // Click to collect
            if (Input.GetMouseButtonDown(0))
            {
                Vector3 worldPos = _cam.ScreenToWorldPoint(Input.mousePosition);
                worldPos.z = 0f;

                for (int i = _debris.Count - 1; i >= 0; i--)
                {
                    var item = _debris[i];
                    if (item.go == null) continue;

                    if (Vector3.Distance(item.go.transform.position, worldPos) < 0.7f)
                    {
                        Destroy(item.go);
                        _debris.RemoveAt(i);
                        OnDebrisCollected?.Invoke();
                        break;
                    }
                }
            }
        }

        void SpawnDebris()
        {
            int   side   = Random.value < 0.5f ? -1 : 1;
            float spawnX = side * 9.5f;
            float spawnY = Random.Range(-2.8f, -1.6f);
            float speed  = Random.Range(0.28f, 0.52f) * -side; // move toward opposite side
            int   type   = Random.Range(0, 3);

            var go = new GameObject("Debris_" + type);
            go.transform.SetParent(transform);
            go.transform.localPosition = new Vector3(spawnX, spawnY, 0f);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite       = _debrisSprites[type];
            sr.sortingOrder = -70;

            _debris.Add(new DebrisItem
            {
                go    = go,
                sr    = sr,
                speed = speed,
                baseY = spawnY,
                phase = Random.Range(0f, Mathf.PI * 2f),
            });
        }

        void DrawDebrisHints()
        {
            if (_cam == null) return;

            // Build style lazily (can only access GUI.skin from OnGUI)
            if (_hintStyle == null)
            {
                _hintStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize  = 9,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter,
                    normal    = { textColor = new Color(1f, 0.95f, 0.6f, 0.85f) },
                };
            }

            foreach (var item in _debris)
            {
                if (item.go == null) continue;

                // Only show hint when debris is near the island (collectible zone)
                float wx = item.go.transform.position.x;
                if (wx < -4.5f || wx > 4.5f) continue;

                Vector3 screenPos = _cam.WorldToScreenPoint(item.go.transform.position);
                float guiX = screenPos.x;
                float guiY = Screen.height - screenPos.y - 22f;
                GUI.Label(new Rect(guiX - 22f, guiY, 44f, 18f), "[Click]", _hintStyle);
            }
        }
    }
}
