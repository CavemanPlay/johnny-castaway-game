using UnityEngine;
using JohnnyGame.Core;

namespace JohnnyGame.Rendering
{
    /// <summary>
    /// Renders the island scene using procedurally-generated pixel-art sprites.
    /// Auto-created as a child of GameRoot — no scene setup required.
    ///
    /// Layer order (back→front):
    ///   Sky quad  (-200)  →  Ocean quad  (-150)  →  Island  (-100)
    ///   Palm  (-80)  →  Campfire  (-60)  →  Johnny  (-40)
    /// </summary>
    [DefaultExecutionOrder(-50)]
    public class IslandSceneView : MonoBehaviour
    {
        // ── Scene objects ──────────────────────────────────────────────────
        Camera     _cam;
        GameObject _skyGO, _oceanGO, _islandGO, _palmGO, _campfireGO, _johnnyGO;

        // ── Renderers used for animation ───────────────────────────────────
        MeshRenderer   _oceanMR;
        SpriteRenderer _fireSR, _johnnySR;

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
        Color _skyColorStorm = new Color(0.22f, 0.32f, 0.48f);

        const float FireFPS   = 0.11f;
        const float IdleGap   = 3.5f;
        const float WaveSpeed = 0.055f;
        const float PalmSway  = 2.8f;   // max degrees
        const float PalmSpeed = 2.3f;   // radians/second

        // ── Lifecycle ──────────────────────────────────────────────────────

        void Awake()
        {
            SetupCamera();
            GenerateAndBuildScene();
        }

        void Update()
        {
            AnimateFire();
            AnimateJohnny();
            AnimateOcean();
            AnimatePalm();
            if (_storming) TickStorm();
        }

        // ── Public API ─────────────────────────────────────────────────────

        /// <summary>Called by GameRoot when a storm event occurs.</summary>
        public void NotifyStorm()
        {
            _storming  = true;
            _stormTime = 5f;
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
            _cam.orthographic     = true;
            _cam.orthographicSize = 4f;      // 8 world-units tall
            _cam.transform.position = new Vector3(0f, 0f, -10f);
            _cam.clearFlags      = CameraClearFlags.SolidColor;
            _cam.backgroundColor = new Color(0.37f, 0.78f, 0.94f);
            _cam.nearClipPlane   = 0.1f;
            _cam.farClipPlane    = 20f;
            _skyColorNormal      = _cam.backgroundColor;
        }

        // ── Scene construction ─────────────────────────────────────────────

        void GenerateAndBuildScene()
        {
            // ── Sky gradient (static quad behind everything)
            _skyGO = Quad("Sky", 0f, 0.5f, 15f, 9f,
                          TextureFactory.CreateSky(256, 160), -200);

            // ── Ocean (tiling, UV-animated)
            var oceanTex = TextureFactory.CreateOcean(64, 32);
            oceanTex.wrapMode = TextureWrapMode.Repeat;
            _oceanGO = Quad("Ocean", 0f, -2.0f, 15f, 5.5f, oceanTex, -150);
            _oceanMR = _oceanGO.GetComponent<MeshRenderer>();
            _oceanMR.material.mainTextureScale = new Vector2(6f, 4f);

            // ── Island
            _islandGO = SprObj("Island",
                                TextureFactory.CreateIsland(128, 80),
                                0f, -1.2f, -100, ppu: 32f);

            // ── Palm tree (pivot at base)
            _palmGO = SprObj("PalmTree",
                              TextureFactory.CreatePalmTree(80, 120),
                              0.65f, -0.25f, -80, ppu: 28f,
                              pivotY: 0f);

            // ── Campfire (animated)
            var fireTex = TextureFactory.CreateCampfireFrames();
            for (int i = 0; i < 3; i++)
                _fireSprites[i] = MakeSprite(fireTex[i], 0.5f, 0f, 10f);

            _campfireGO = SprObj("Campfire", fireTex[0], -0.9f, -0.95f, -60, ppu: 10f, pivotY: 0f);
            _fireSR     = _campfireGO.GetComponent<SpriteRenderer>();

            // ── Johnny (animated)
            var johnTex = TextureFactory.CreateJohnnyFrames();
            for (int i = 0; i < 4; i++)
                _johnnySprites[i] = MakeSprite(johnTex[i], 0.5f, 0f, 13f);

            _johnnyGO = SprObj("Johnny", johnTex[0], -0.35f, -1.05f, -40, ppu: 13f, pivotY: 0f);
            _johnnySR = _johnnyGO.GetComponent<SpriteRenderer>();
        }

        // ── Factory helpers ────────────────────────────────────────────────

        /// <summary>Creates a full-screen (or sized) quad using MeshRenderer + Sprites/Default.</summary>
        GameObject Quad(string name, float x, float y, float w, float h,
                        Texture2D tex, int sortOrder)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform);
            go.transform.localPosition = new Vector3(x, y, 0f);

            var mesh = new Mesh { name = name };
            mesh.vertices  = new[]
            {
                new Vector3(-w/2, -h/2), new Vector3(w/2, -h/2),
                new Vector3(-w/2,  h/2), new Vector3(w/2,  h/2)
            };
            mesh.uv        = new[] { Vector2.zero, Vector2.right, Vector2.up, Vector2.one };
            mesh.triangles = new[] { 0, 2, 1, 2, 3, 1 };
            mesh.RecalculateNormals();

            go.AddComponent<MeshFilter>().mesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            // "Sprites/Default" is included in Unity URP projects for legacy compatibility
            mr.material = new Material(Shader.Find("Sprites/Default")) { mainTexture = tex };
            mr.sortingOrder = sortOrder;
            return go;
        }

        /// <summary>Creates a sprite GameObject with bottom-center pivot.</summary>
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
            if (_fireSR) _fireSR.sprite = _fireSprites[_fireIdx];
        }

        void AnimateJohnny()
        {
            _johnnyTimer += Time.deltaTime;
            if (_johnnyTimer < IdleGap) return;
            _johnnyTimer      = 0f;
            _johnnySeqIdx     = (_johnnySeqIdx + 1) % IdleSeq.Length;
            if (_johnnySR) _johnnySR.sprite = _johnnySprites[IdleSeq[_johnnySeqIdx]];
        }

        void AnimateOcean()
        {
            if (_oceanMR == null) return;
            float spd = _storming ? WaveSpeed * 2.8f : WaveSpeed;
            float t   = Time.time * spd;
            _oceanMR.material.mainTextureOffset = new Vector2(t, t * 0.35f);
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
            if (_stormTime <= 0f) { _stormTime = 0f; _storming = false; }

            float intensity = Mathf.Clamp01(_stormTime / 5f);
            float flash     = Mathf.Abs(Mathf.Sin(Time.time * 4.5f));
            _cam.backgroundColor = Color.Lerp(_skyColorNormal, _skyColorStorm, intensity * flash * 0.65f);

            if (!_storming)
                _cam.backgroundColor = _skyColorNormal;
        }
    }
}
