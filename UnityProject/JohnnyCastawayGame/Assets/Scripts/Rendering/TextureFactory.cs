using UnityEngine;

namespace JohnnyGame.Rendering
{
    /// <summary>
    /// Generates all Texture2D assets at runtime — no imported sprites needed.
    ///
    /// Visual target: classic Johnny Castaway screensaver (1992).
    /// Style: bold saturated pixel-art; FilterMode.Point on everything.
    ///
    /// Palette derived directly from the reference screenshot:
    ///   - Cyan sky with fluffy cloud
    ///   - Deep navy ocean with horizontal wave banding + foam crests
    ///   - Round golden-sand island, wet-rim halo, starfish + shell decorations
    ///   - Tall palm, sitting Johnny, campfire, submerged rock
    /// </summary>
    public static class TextureFactory
    {
        // ── Palette ────────────────────────────────────────────────────────

        // Sky
        static readonly Color32 SkyTop  = C("38BAEE");
        static readonly Color32 SkyMid  = C("70D4F8");
        static readonly Color32 SkyBot  = C("B2EAF8");
        static readonly Color32 CloudW  = C("FFFFFF");
        static readonly Color32 CloudG  = C("D0E8F8");
        static readonly Color32 CloudSh = C("A8C8E0");

        // Ocean — deep navy banding matching reference
        static readonly Color32 OceanDeep2 = C("0E1A4A");
        static readonly Color32 OceanDeep  = C("162568");
        static readonly Color32 OceanDark  = C("1E3282");
        static readonly Color32 OceanMid   = C("2A469E");
        static readonly Color32 OceanBrght = C("3860BE");
        static readonly Color32 OceanFoam  = C("8AB0E8");
        static readonly Color32 OceanCrest = C("C0D4F8");

        // Sand — golden tones matching reference
        static readonly Color32 SandBrite = C("EDD060");
        static readonly Color32 SandMain  = C("D4A830");
        static readonly Color32 SandDark  = C("A87020");
        static readonly Color32 SandWet   = C("7A5010");
        static readonly Color32 SandEdge  = C("503208");

        // Palm
        static readonly Color32 TrunkMain = C("7A4E28");
        static readonly Color32 TrunkLt   = C("A8723A");
        static readonly Color32 TrunkDk   = C("3E1E08");
        static readonly Color32 FrondMain = C("287018");
        static readonly Color32 FrondLt   = C("40A020");
        static readonly Color32 FrondDk   = C("145008");
        static readonly Color32 FrondTip  = C("60C030");
        static readonly Color32 CoconutC  = C("603818");

        // Johnny
        static readonly Color32 SkinMain  = C("D4A265");
        static readonly Color32 SkinDk    = C("A07248");
        static readonly Color32 SkinLt    = C("E8C090");
        static readonly Color32 HatMain   = C("D4B060");
        static readonly Color32 HatDk     = C("907030");
        static readonly Color32 HatBand   = C("60401A");
        static readonly Color32 ShirtW    = C("F0E8C8");
        static readonly Color32 ShirtSh   = C("C0B090");
        static readonly Color32 BeardBr   = C("7A5230");
        static readonly Color32 BeardGy   = C("A08060");
        static readonly Color32 EyeC      = C("201008");

        // Fire
        static readonly Color32 FireRed = C("FF2808");
        static readonly Color32 FireOrg = C("FF7808");
        static readonly Color32 FireYel = C("FFCC18");
        static readonly Color32 Ember   = C("801808");
        static readonly Color32 Log     = C("502808");

        // Rock
        static readonly Color32 RockDk    = C("282828");
        static readonly Color32 RockMid   = C("404040");
        static readonly Color32 RockLt    = C("686868");
        static readonly Color32 RockSheen = C("909090");

        // Dock
        static readonly Color32 DockBr   = C("6A4018");
        static readonly Color32 DockLt   = C("8A5820");
        static readonly Color32 DockDk   = C("402808");
        static readonly Color32 DockRope = C("C8A060");

        // Debris
        static readonly Color32 DebrisWd   = C("8A6030");
        static readonly Color32 DebrisWdLt = C("B08040");
        static readonly Color32 DebrisGl   = C("2A7038");
        static readonly Color32 DebrisGlLt = C("40A050");

        // Decorations
        static readonly Color32 StarOr = C("E86820");
        static readonly Color32 StarDk = C("A84010");
        static readonly Color32 ShellW = C("F0EAD0");
        static readonly Color32 ShellG = C("C8B898");

        static readonly Color32 Clear = new Color32(0, 0, 0, 0);

        // ── Helpers ────────────────────────────────────────────────────────

        static Color32 C(string hex)
        {
            byte r = System.Convert.ToByte(hex.Substring(0, 2), 16);
            byte g = System.Convert.ToByte(hex.Substring(2, 2), 16);
            byte b = System.Convert.ToByte(hex.Substring(4, 2), 16);
            return new Color32(r, g, b, 255);
        }

        static Color32 Lerp(Color32 a, Color32 b, float t)
            => Color32.Lerp(a, b, Mathf.Clamp01(t));

        static Texture2D NewTex(int w, int h, bool repeat = false)
        {
            var t = new Texture2D(w, h, TextureFormat.RGBA32, false);
            t.filterMode = FilterMode.Point;
            t.wrapMode   = repeat ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
            return t;
        }

        static void S(Color32[] px, int tw, int th, int x, int y, Color32 c)
        {
            if ((uint)x >= (uint)tw || (uint)y >= (uint)th) return;
            px[y * tw + x] = c;
        }

        static void Box(Color32[] px, int tw, int th, int x, int y, int w, int h, Color32 c)
        {
            for (int py = y; py < y + h; py++)
            for (int px2 = x; px2 < x + w; px2++)
                S(px, tw, th, px2, py, c);
        }

        static void DrawLine(Color32[] px, int tw, int th,
                             int x0, int y0, int x1, int y1,
                             Color32 c, int thick = 0)
        {
            int steps = Mathf.Max(Mathf.Abs(x1 - x0), Mathf.Abs(y1 - y0), 1);
            for (int i = 0; i <= steps; i++)
            {
                float f  = (float)i / steps;
                int   bx = Mathf.RoundToInt(Mathf.Lerp(x0, x1, f));
                int   by = Mathf.RoundToInt(Mathf.Lerp(y0, y1, f));
                for (int oy = -thick; oy <= thick; oy++)
                for (int ox = -thick; ox <= thick; ox++)
                    S(px, tw, th, bx + ox, by + oy, c);
            }
        }

        static void Ellipse(Color32[] px, int tw, int th,
                            float cx, float cy, float rx, float ry, Color32 c)
        {
            int x0 = Mathf.Max(0, Mathf.FloorToInt(cx - rx));
            int x1 = Mathf.Min(tw - 1, Mathf.CeilToInt(cx + rx));
            int y0 = Mathf.Max(0, Mathf.FloorToInt(cy - ry));
            int y1 = Mathf.Min(th - 1, Mathf.CeilToInt(cy + ry));
            for (int y = y0; y <= y1; y++)
            for (int x = x0; x <= x1; x++)
            {
                float fx = (x - cx) / rx, fy = (y - cy) / ry;
                if (fx * fx + fy * fy <= 1f) S(px, tw, th, x, y, c);
            }
        }

        static void Frond(Color32[] px, int tw, int th,
                          int ox, int oy, float angleDeg, int length)
        {
            float rad = angleDeg * Mathf.Deg2Rad;
            float dx  = Mathf.Sin(rad), dy = Mathf.Cos(rad);

            for (int i = 2; i < length; i++)
            {
                float f     = (float)i / length;
                float droop = f * f * 14f;
                int   fx    = ox + Mathf.RoundToInt(dx * i);
                int   fy    = oy + Mathf.RoundToInt(dy * i - droop);
                int   hw    = Mathf.Max(1, Mathf.RoundToInt(Mathf.Lerp(4f, 0.5f, f)));

                float perpX = -dy, perpY = dx;
                for (int j = -hw; j <= hw; j++)
                {
                    int ppx = fx + Mathf.RoundToInt(perpX * j);
                    int ppy = fy + Mathf.RoundToInt(perpY * j);
                    if ((uint)ppx >= (uint)tw || (uint)ppy >= (uint)th) continue;

                    Color32 col = Mathf.Abs(j) == hw ? FrondDk
                                : f > 0.85f          ? FrondTip
                                : f < 0.4f           ? FrondMain
                                                     : FrondLt;
                    int idx = ppy * tw + ppx;
                    if (px[idx].a < 128) px[idx] = col;
                }
            }
        }

        static void DrawFlame(Color32[] px, int tw, int th,
                              int cx, int baseY, int hw, int height, int frame)
        {
            for (int y = baseY; y < baseY + height; y++)
            {
                float f   = (float)(y - baseY) / height;
                float w2  = (1f - f) * hw;
                float sw  = Mathf.Sin(f * 5.8f + frame * 1.7f) * 1.8f;
                int   x0  = Mathf.RoundToInt(cx + sw - w2);
                int   x1  = Mathf.RoundToInt(cx + sw + w2);
                for (int x = x0; x <= x1; x++)
                {
                    Color32 col;
                    if      (f < 0.22f) col = FireRed;
                    else if (f < 0.52f) col = FireOrg;
                    else if (f < 0.78f) col = FireYel;
                    else                col = Lerp(FireYel, new Color32(255, 255, 200, 0),
                                                   (f - 0.78f) * 4.5f);
                    S(px, tw, th, x, y, col);
                }
            }
        }

        static void Starfish(Color32[] px, int tw, int th, int cx, int cy, int r)
        {
            for (int arm = 0; arm < 5; arm++)
            {
                float angle = arm * 72f * Mathf.Deg2Rad - Mathf.PI * 0.5f;
                for (int i = 0; i <= r; i++)
                {
                    float f  = (float)i / r;
                    int   fx = cx + Mathf.RoundToInt(Mathf.Cos(angle) * i);
                    int   fy = cy + Mathf.RoundToInt(Mathf.Sin(angle) * i);
                    int   hw = Mathf.Max(1, Mathf.RoundToInt(Mathf.Lerp(2.5f, 0.5f, f)));
                    float pa = angle + Mathf.PI * 0.5f;
                    for (int d = -hw; d <= hw; d++)
                    {
                        int bx = fx + Mathf.RoundToInt(Mathf.Cos(pa) * d);
                        int by = fy + Mathf.RoundToInt(Mathf.Sin(pa) * d);
                        S(px, tw, th, bx, by, Mathf.Abs(d) >= hw ? StarDk : StarOr);
                    }
                }
            }
        }

        static void Shell(Color32[] px, int tw, int th, int cx, int cy)
        {
            for (int rib = 0; rib < 5; rib++)
            {
                float angle = (-50f + rib * 25f) * Mathf.Deg2Rad;
                for (int i = 0; i < 6; i++)
                {
                    int bx = cx + Mathf.RoundToInt(Mathf.Cos(angle) * i);
                    int by = cy + Mathf.RoundToInt(Mathf.Sin(angle) * i);
                    S(px, tw, th, bx, by, i < 3 ? ShellG : ShellW);
                }
            }
        }

        static void CloudBubble(Color32[] px, int tw, int th,
                                int cx, int cy, int rw, int rh)
        {
            int x0 = Mathf.Max(0, cx - rw), x1 = Mathf.Min(tw - 1, cx + rw);
            int y0 = Mathf.Max(0, cy - rh), y1 = Mathf.Min(th - 1, cy + rh);
            for (int y = y0; y <= y1; y++)
            for (int x = x0; x <= x1; x++)
            {
                float fx = (float)(x - cx) / rw, fy = (float)(y - cy) / rh;
                float d  = fx * fx + fy * fy;
                if (d > 1f) continue;
                px[y * tw + x] = d > 0.7f ? CloudSh : d > 0.35f ? CloudG : CloudW;
            }
        }

        static void DrawCloud(Color32[] px, int tw, int th,
                              int cx, int cy, int rw, int rh)
        {
            CloudBubble(px, tw, th, cx,              cy,              rw,       rh);
            CloudBubble(px, tw, th, cx + rw / 2,     cy + rh / 4,    rw * 3/4, rh * 3/4);
            CloudBubble(px, tw, th, cx - rw * 2 / 5, cy + rh / 5,   rw / 2,   rh / 2);
            CloudBubble(px, tw, th, cx + rw * 3 / 4, cy + rh * 2/5, rw / 2,   rh * 2/3);
        }

        // ── Sky (256×160) ──────────────────────────────────────────────────

        public static Texture2D CreateSky(int w = 256, int h = 160)
        {
            var tex = NewTex(w, h);
            var px  = new Color32[w * h];

            for (int y = 0; y < h; y++)
            {
                float f   = (float)y / (h - 1);
                Color32 c = f < 0.4f ? Lerp(SkyBot, SkyMid, f / 0.4f)
                                     : Lerp(SkyMid, SkyTop, (f - 0.4f) / 0.6f);
                for (int x = 0; x < w; x++)
                    px[y * w + x] = c;
            }

            // Main cloud cluster — upper right (matches reference position)
            DrawCloud(px, w, h, w * 11 / 16, h * 4 / 5, 54, 24);
            // Small secondary cloud — upper left
            DrawCloud(px, w, h, w / 6,       h * 9 / 11, 30, 14);

            tex.SetPixels32(px);
            tex.Apply();
            return tex;
        }

        // ── Ocean base (static dark gradient behind wave layer) ────────────

        public static Texture2D CreateOceanBase(int w = 128, int h = 64)
        {
            var tex = NewTex(w, h, repeat: false);
            var px  = new Color32[w * h];

            for (int y = 0; y < h; y++)
            {
                float f   = (float)y / (h - 1);
                Color32 c = Lerp(OceanDeep2, OceanDark, f * f);
                for (int x = 0; x < w; x++)
                    px[y * w + x] = c;
            }

            tex.SetPixels32(px);
            tex.Apply();
            return tex;
        }

        // ── Ocean waves (tiling, UV-animated) — THE key visual upgrade ─────
        //
        // 8-row band cycle creates horizontal wave banding exactly like the
        // reference screenshot: dark trough → mid body → bright crest → foam.
        // Per-column sine offset breaks up perfectly straight lines so the
        // bands read as rolling waves rather than static stripes.

        public static Texture2D CreateOcean(int w = 128, int h = 64)
        {
            var tex = NewTex(w, h, repeat: true);
            var px  = new Color32[w * h];

            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                int   bandGroup = y / 8;
                float undulate  = Mathf.Sin(x * 0.11f + bandGroup * 0.85f) * 1.8f;
                int   band      = (int)Mathf.Repeat(y + undulate, 8f);

                Color32 col;
                switch (band)
                {
                    case 0: case 1: col = OceanDeep2;  break;
                    case 2: case 3: col = OceanDeep;   break;
                    case 4: case 5: col = OceanMid;    break;
                    case 6:         col = OceanBrght;  break;
                    default:
                        float foam = Mathf.Sin(x * 0.38f + bandGroup * 1.65f);
                        col = foam > 0.55f ? OceanCrest : OceanFoam;
                        break;
                }

                // Semi-transparent so the OceanBase gradient shows through slightly
                col.a        = 220;
                px[y * w + x] = col;
            }

            tex.SetPixels32(px);
            tex.Apply();
            return tex;
        }

        // ── Island (256×160, RGBA) ─────────────────────────────────────────

        public static Texture2D CreateIsland(int w = 256, int h = 160)
        {
            var tex = NewTex(w, h);
            var px  = new Color32[w * h];
            for (int i = 0; i < px.Length; i++) px[i] = Clear;

            float cx = w * 0.50f, cy = h * 0.46f;
            float rx = w * 0.44f, ry = h * 0.40f;

            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float fx = (x - cx) / rx, fy = (y - cy) / ry;
                float d  = Mathf.Sqrt(fx * fx + fy * fy);
                if (d >= 1f) continue;

                float noise = Mathf.PerlinNoise(x * 0.14f + 0.5f, y * 0.14f + 0.5f);

                Color32 col;
                if      (d < 0.25f) col = SandBrite;
                else if (d < 0.50f) col = Lerp(SandBrite, SandMain,  (d - 0.25f) / 0.25f);
                else if (d < 0.68f) col = SandMain;
                else if (d < 0.80f) col = Lerp(SandMain, SandDark,   (d - 0.68f) / 0.12f);
                else if (d < 0.90f) col = SandWet;
                else                col = SandEdge;

                if (d > 0.30f && d < 0.75f && noise > 0.62f)
                    col = Lerp(col, SandDark, (noise - 0.62f) * 0.9f);

                if (d > 0.93f)
                    col.a = (byte)Mathf.RoundToInt(
                                Mathf.Lerp(255f, 0f, (d - 0.93f) / 0.07f));

                px[y * w + x] = col;
            }

            // Baked decorations
            Starfish(px, w, h, (int)(w * 0.70f), (int)(h * 0.52f), 5);
            Starfish(px, w, h, (int)(w * 0.32f), (int)(h * 0.38f), 4);
            Shell(px, w, h, (int)(w * 0.62f), (int)(h * 0.42f));
            var pebble = C("907060");
            Box(px, w, h, (int)(w * 0.47f), (int)(h * 0.44f), 3, 2, pebble);

            tex.SetPixels32(px);
            tex.Apply();
            return tex;
        }

        // ── Palm tree (96×144, RGBA) ───────────────────────────────────────

        public static Texture2D CreatePalmTree(int w = 96, int h = 144)
        {
            var tex = NewTex(w, h);
            var px  = new Color32[w * h];
            for (int i = 0; i < px.Length; i++) px[i] = Clear;

            int frondRootY = h - 52;
            int baseX      = w / 2;

            for (int y = 0; y < frondRootY; y++)
            {
                float f     = (float)y / frondRootY;
                int   cx2   = baseX + Mathf.RoundToInt(Mathf.Sin(f * Mathf.PI * 1.2f) * 6f);
                int   halfW = Mathf.RoundToInt(Mathf.Lerp(5f, 2.5f, f));
                bool  ring  = (y % 10) < 4;

                for (int dx = -halfW; dx <= halfW; dx++)
                {
                    Color32 col;
                    if      (Mathf.Abs(dx) >= halfW) col = TrunkDk;
                    else if (ring)                   col = Lerp(TrunkDk, TrunkMain, 0.55f);
                    else if (dx >= 1)                col = TrunkLt;
                    else                             col = TrunkMain;
                    S(px, w, h, cx2 + dx, y, col);
                }
            }

            // Coconut cluster near frond root
            int ftx = baseX + 5, fty = frondRootY;
            Ellipse(px, w, h, ftx - 4, fty + 2, 3.5f, 3f, CoconutC);
            Ellipse(px, w, h, ftx + 2, fty + 2, 3.5f, 3f, CoconutC);
            Ellipse(px, w, h, ftx - 1, fty + 5, 3.5f, 3f, CoconutC);
            Ellipse(px, w, h, ftx + 5, fty + 5, 3.5f, 3f, CoconutC);

            // Seven fronds
            Frond(px, w, h, ftx, fty, -52f, 52);
            Frond(px, w, h, ftx, fty,  46f, 55);
            Frond(px, w, h, ftx, fty, -26f, 58);
            Frond(px, w, h, ftx, fty,  22f, 56);
            Frond(px, w, h, ftx, fty,  -6f, 60);
            Frond(px, w, h, ftx, fty, -72f, 40);
            Frond(px, w, h, ftx, fty,  68f, 42);

            tex.SetPixels32(px);
            tex.Apply();
            return tex;
        }

        // ── Campfire frames (24×28, 3 frames) ─────────────────────────────

        public static Texture2D[] CreateCampfireFrames()
            => new[] { CampfireFrame(0), CampfireFrame(1), CampfireFrame(2) };

        static Texture2D CampfireFrame(int frame)
        {
            int W = 24, H = 28;
            var tex = NewTex(W, H);
            var px  = new Color32[W * H];
            for (int i = 0; i < px.Length; i++) px[i] = Clear;

            DrawLine(px, W, H, 2,  4, 20, 8, Log, 1);
            DrawLine(px, W, H, 20, 4, 2,  8, Log, 1);

            Color32 emb = frame % 2 == 0 ? Ember : Lerp(Ember, FireRed, 0.3f);
            Box(px, W, H, 6, 3, 12, 4, emb);

            switch (frame)
            {
                case 0: DrawFlame(px, W, H, 12, 5,  6, 18, 0); break;
                case 1: DrawFlame(px, W, H, 13, 5,  7, 14, 1);
                        DrawFlame(px, W, H, 10, 7,  3,  8, 1); break;
                case 2: DrawFlame(px, W, H, 11, 5,  5, 16, 2);
                        DrawFlame(px, W, H, 15, 6,  4, 12, 2); break;
            }

            tex.SetPixels32(px);
            tex.Apply();
            return tex;
        }

        // ── Johnny frames (48×72, 4 frames) ───────────────────────────────
        // 0: neutral sitting  1: look-left  2: look-right  3: wave

        public static Texture2D[] CreateJohnnyFrames()
            => new[] { JohnnyFrame(0), JohnnyFrame(1), JohnnyFrame(2), JohnnyFrame(3) };

        static Texture2D JohnnyFrame(int frame)
        {
            int W = 48, H = 72;
            var tex = NewTex(W, H);
            var px  = new Color32[W * H];
            for (int i = 0; i < px.Length; i++) px[i] = Clear;

            void F(int x, int y, int w, int h, Color32 c) => Box(px, W, H, x, y, w, h, c);
            void P(int x, int y, Color32 c)               => S(px, W, H, x, y, c);
            void E(int x, int y, float rx, float ry, Color32 c)
                => Ellipse(px, W, H, x, y, rx, ry, c);

            int cx = W / 2; // 24

            // ── Crossed legs ────────────────────────────────────────────
            F(2,   0, 18, 11, ShirtSh);
            F(28,  0, 18, 11, ShirtW);
            F(5,   7, 12,  8, ShirtSh);
            F(30,  7, 12,  8, ShirtW);
            F(2,   0,  6,  4, C("2A1808"));   // left shoe
            F(40,  0,  6,  4, C("2A1808"));   // right shoe
            E(cx - 9,  9, 5f, 4f, C("D8C8A8"));
            E(cx + 9,  9, 5f, 4f, ShirtW);

            // ── Torso ────────────────────────────────────────────────────
            F(cx - 10, 14, 20, 22, ShirtW);
            F(cx - 10, 14,  2, 22, ShirtSh);
            F(cx +  8, 14,  2, 22, ShirtSh);
            F(cx -  9, 20, 18,  2, C("8A5A28")); // belt
            F(cx -  2, 20,  4,  2, C("C88828")); // buckle

            // ── Arms ─────────────────────────────────────────────────────
            bool wave = frame == 3;
            F(cx - 18, 20, 8, 6, SkinMain);
            F(cx - 18, 20, 2, 6, SkinDk);
            int ry2 = wave ? 28 : 22;
            F(cx + 10, ry2, 8, 6, SkinMain);
            F(cx + 16, ry2, 2, 6, SkinDk);
            if (wave)
            {
                F(cx + 11, 44, 6, 8, SkinMain);
                F(cx + 13, 50, 5, 4, SkinLt);
            }

            // ── Neck ─────────────────────────────────────────────────────
            F(cx - 3, 36, 6, 6, SkinMain);

            // ── Head ─────────────────────────────────────────────────────
            E(cx, 48, 8f, 9f, SkinMain);
            for (int by = 39; by <= 57; by++)
            {
                S(px, W, H, cx - 8, by, SkinDk);
                S(px, W, H, cx - 7, by, Lerp(SkinDk, SkinMain, 0.4f));
            }

            // ── Beard ────────────────────────────────────────────────────
            F(cx - 6, 40, 12, 5, BeardBr);
            F(cx - 5, 42, 10, 5, BeardGy);
            F(cx - 4, 47,  8, 3, BeardGy);

            // ── Eyes ─────────────────────────────────────────────────────
            int eShift = frame == 1 ? -2 : frame == 2 ? 2 : 0;
            F(cx - 5 + eShift, 48, 3, 3, EyeC);
            F(cx + 2 + eShift, 48, 3, 3, EyeC);
            P(cx - 4 + eShift, 50, new Color32(255, 255, 255, 200));
            P(cx + 3 + eShift, 50, new Color32(255, 255, 255, 200));

            // ── Hat brim ─────────────────────────────────────────────────
            F(cx - 11, 55, 22, 4, HatMain);
            F(cx - 11, 55,  2, 4, HatDk);
            F(cx +  9, 55,  2, 4, HatDk);
            F(cx -  8, 56, 16, 1, HatBand);

            // ── Hat crown ────────────────────────────────────────────────
            F(cx -  7, 59, 14,  9, HatMain);
            F(cx -  7, 59,  2,  9, HatDk);
            F(cx +  5, 59,  2,  9, HatDk);
            F(cx -  6, 67, 12,  2, HatDk);

            tex.SetPixels32(px);
            tex.Apply();
            return tex;
        }

        // ── Rock (56×44, RGBA) — partially submerged boulder ──────────────

        public static Texture2D CreateRock(int w = 56, int h = 44)
        {
            var tex = NewTex(w, h);
            var px  = new Color32[w * h];
            for (int i = 0; i < px.Length; i++) px[i] = Clear;

            float cx = w * 0.50f, cy = h * 0.52f;
            float rx = w * 0.42f, ry = h * 0.36f;

            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float noise = Mathf.PerlinNoise(x * 0.28f + 1.5f, y * 0.28f + 2.1f);
                float fx    = (x - cx) / rx, fy = (y - cy) / ry;
                float d     = Mathf.Sqrt(fx * fx + fy * fy) + (noise - 0.5f) * 0.22f;
                if (d >= 1f) continue;

                Color32 col;
                if      (d < 0.45f) col = RockMid;
                else if (d < 0.72f) col = Lerp(RockMid, RockDk, (d - 0.45f) / 0.27f);
                else                col = RockDk;

                // Sheen on visual top (high y in Unity texture = visual top)
                float topFrac = (y - (cy - ry * 0.4f)) / (ry * 0.5f);
                if (topFrac > 0f && topFrac < 1f && d < 0.55f)
                    col = Lerp(col, topFrac < 0.25f ? RockSheen : RockLt,
                               (1f - topFrac) * 0.7f);

                // Submerged bottom fades to ocean colour (low y = visual bottom)
                float subFrac = (float)y / (h * 0.30f);
                if (subFrac < 1f)
                    col = Lerp(OceanMid, col, Mathf.Clamp01(subFrac));

                px[y * w + x] = col;
            }

            tex.SetPixels32(px);
            tex.Apply();
            return tex;
        }

        // ── Debris items ───────────────────────────────────────────────────
        // type 0 = wooden plank (36×10)
        // type 1 = glass bottle  (12×22)
        // type 2 = coconut       (18×18)

        public static Texture2D CreateDebrisItem(int type)
        {
            return type switch { 0 => DebrisPlank(), 1 => DebrisBottle(), _ => DebrisCoconut() };
        }

        static Texture2D DebrisPlank()
        {
            int W = 36, H = 10;
            var tex = NewTex(W, H);
            var px  = new Color32[W * H];
            for (int i = 0; i < px.Length; i++) px[i] = Clear;

            Box(px, W, H, 1, 2, W - 2, H - 4, DebrisWd);
            for (int gx = 4; gx < W - 4; gx += 6)
                DrawLine(px, W, H, gx, 2, gx + 1, H - 3, DebrisWdLt);
            S(px, W, H, 3,     H / 2, C("181008"));
            S(px, W, H, W - 4, H / 2, C("181008"));

            tex.SetPixels32(px);
            tex.Apply();
            return tex;
        }

        static Texture2D DebrisBottle()
        {
            int W = 12, H = 22;
            var tex = NewTex(W, H);
            var px  = new Color32[W * H];
            for (int i = 0; i < px.Length; i++) px[i] = Clear;

            Ellipse(px, W, H, W / 2f, 7f, 4.5f, 7f, DebrisGl);
            Box(px, W, H, W / 2 - 1, 14, 3, 6, DebrisGl);
            Box(px, W, H, W / 2 - 2, 19, 5, 3, C("808020"));
            for (int by = 3; by < 14; by++)
                S(px, W, H, W / 2 - 3, by, DebrisGlLt);

            tex.SetPixels32(px);
            tex.Apply();
            return tex;
        }

        static Texture2D DebrisCoconut()
        {
            int W = 18, H = 18;
            var tex = NewTex(W, H);
            var px  = new Color32[W * H];
            for (int i = 0; i < px.Length; i++) px[i] = Clear;

            Ellipse(px, W, H, W / 2f, H / 2f, 7.5f, 7.5f, CoconutC);
            Ellipse(px, W, H, W / 2f + 2f, H / 2f + 2f, 3f, 3f,
                    Lerp(CoconutC, C("A06028"), 0.5f));
            S(px, W, H, W/2 - 2, H/2 + 1, TrunkDk);
            S(px, W, H, W/2,     H/2 + 2, TrunkDk);
            S(px, W, H, W/2 + 2, H/2 + 1, TrunkDk);

            tex.SetPixels32(px);
            tex.Apply();
            return tex;
        }

        // ── Dock / pier (72×48, RGBA) ──────────────────────────────────────

        public static Texture2D CreateDock(int w = 72, int h = 48)
        {
            var tex = NewTex(w, h);
            var px  = new Color32[w * h];
            for (int i = 0; i < px.Length; i++) px[i] = Clear;

            int topY  = (int)(h * 0.60f);
            int[] postX = { w / 6, w / 2, w * 5 / 6 };

            foreach (int px2 in postX)
            {
                Box(px, w, h, px2 - 2, 0, 4, topY + 4, DockDk);
                DrawLine(px, w, h, px2 + 1, 2, px2 + 1, topY + 2, DockLt);
            }

            for (int py = 8; py < topY; py += 9)
            for (int bx = postX[0] - 2; bx <= postX[2] + 2; bx++)
            {
                float n   = Mathf.PerlinNoise(bx * 0.35f, py * 0.35f);
                Color32 c = n > 0.6f ? DockLt : DockBr;
                S(px, w, h, bx, py,     c);
                S(px, w, h, bx, py + 1, DockDk);
            }

            // Top platform deck
            for (int py = topY; py < topY + 6; py++)
            for (int bx = 2; bx < w - 2; bx++)
            {
                float n = Mathf.PerlinNoise(bx * 0.22f + 3f, py * 0.8f);
                S(px, w, h, bx, py, n > 0.55f ? DockLt : DockBr);
            }
            for (int bx = 2; bx < w - 2; bx++)
                S(px, w, h, bx, topY, DockDk);

            // Rope
            DrawLine(px, w, h, postX[0], topY + 5, postX[1], topY + 3, DockRope);
            DrawLine(px, w, h, postX[1], topY + 3, postX[2], topY + 5, DockRope);

            tex.SetPixels32(px);
            tex.Apply();
            return tex;
        }
    }
}
