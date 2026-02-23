using UnityEngine;

namespace JohnnyGame.Rendering
{
    /// <summary>
    /// Generates all pixel-art Texture2D assets at runtime.
    /// Style: Johnny Castaway screensaver — warm 2D cartoon, not realistic.
    /// Palette: tropical blues, golden sand, deep greens, warm skin tones.
    /// </summary>
    public static class TextureFactory
    {
        // ── Palette ────────────────────────────────────────────────────────
        static readonly Color32 SkyTop      = C("5EC8F0");
        static readonly Color32 SkyBot      = C("A8E0F8");
        static readonly Color32 CloudWhite  = C("FFFFFF");
        static readonly Color32 CloudGrey   = C("D8EEF8");

        static readonly Color32 OceanDeep   = C("1B3F8A");
        static readonly Color32 OceanMid    = C("2B60B5");
        static readonly Color32 OceanLight  = C("4A90CC");
        static readonly Color32 OceanFoam   = C("C8EAFF");

        static readonly Color32 SandLight   = C("F5D278");
        static readonly Color32 SandMain    = C("D4A835");
        static readonly Color32 SandDark    = C("A87820");
        static readonly Color32 SandWet     = C("8A5F18");

        static readonly Color32 TrunkMain   = C("7A4E28");
        static readonly Color32 TrunkLight  = C("A8723A");
        static readonly Color32 TrunkDark   = C("4E2E12");

        static readonly Color32 FrondMain   = C("2A7A1A");
        static readonly Color32 FrondLight  = C("4AAA28");
        static readonly Color32 FrondDark   = C("1A5010");
        static readonly Color32 CoconutCol  = C("6B3A1F");

        static readonly Color32 SkinMain    = C("D4A265");
        static readonly Color32 SkinDark    = C("A0724A");
        static readonly Color32 HatMain     = C("D4B56A");
        static readonly Color32 HatDark     = C("A08042");
        static readonly Color32 ShirtMain   = C("F0E8C8");
        static readonly Color32 ShirtDark   = C("C0B898");
        static readonly Color32 BeardBrown  = C("7A5230");
        static readonly Color32 BeardGrey   = C("A88260");
        static readonly Color32 EyeColor    = C("28180E");

        static readonly Color32 FireRed     = C("FF3A0A");
        static readonly Color32 FireOrange  = C("FF8A10");
        static readonly Color32 FireYellow  = C("FFCC20");
        static readonly Color32 Ember       = C("8A2808");
        static readonly Color32 LogColor    = C("5A3010");

        static readonly Color32 Clear       = new Color32(0, 0, 0, 0);

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

        // Pixel setters on flat array (row-major, bottom=row0 in Unity)
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

        // ── Sky (256×160) ──────────────────────────────────────────────────

        public static Texture2D CreateSky(int w = 256, int h = 160)
        {
            var tex = NewTex(w, h);
            var px  = new Color32[w * h];

            // Gradient: warm bottom → bright top
            for (int y = 0; y < h; y++)
            {
                float f = (float)y / (h - 1);
                Color32 col = Lerp(SkyBot, SkyTop, f * f);
                for (int x = 0; x < w; x++)
                    px[y * w + x] = col;
            }

            // Clouds
            DrawCloud(px, w, h, w / 5,      h * 4 / 5, 52, 22);
            DrawCloud(px, w, h, w * 3 / 5,  h * 5 / 6, 40, 18);
            DrawCloud(px, w, h, w * 13 / 16, h * 3 / 4, 28, 13);

            tex.SetPixels32(px);
            tex.Apply();
            return tex;
        }

        static void DrawCloud(Color32[] px, int tw, int th, int cx, int cy, int rw, int rh)
        {
            CloudBubble(px, tw, th, cx,          cy,          rw,      rh);
            CloudBubble(px, tw, th, cx + rw / 2, cy + rh / 4, rw * 3/4, rh * 3/4);
            CloudBubble(px, tw, th, cx - rw / 3, cy + rh / 5, rw / 2,   rh / 2);
        }

        static void CloudBubble(Color32[] px, int tw, int th, int cx, int cy, int rw, int rh)
        {
            int x0 = Mathf.Max(0, cx - rw), x1 = Mathf.Min(tw - 1, cx + rw);
            int y0 = Mathf.Max(0, cy - rh), y1 = Mathf.Min(th - 1, cy + rh);
            for (int y = y0; y <= y1; y++)
            for (int x = x0; x <= x1; x++)
            {
                float fx = (float)(x - cx) / rw, fy = (float)(y - cy) / rh;
                float d = fx * fx + fy * fy;
                if (d > 1f) continue;
                px[y * tw + x] = d > 0.65f ? CloudGrey : CloudWhite;
            }
        }

        // ── Ocean (64×32, tiling) ──────────────────────────────────────────

        public static Texture2D CreateOcean(int w = 64, int h = 32)
        {
            var tex = NewTex(w, h, repeat: true);
            var px  = new Color32[w * h];

            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float fx = (float)x / w, fy = (float)y / h;
                float wave1 = Mathf.Sin(fx * 6.28f + fy * 3.0f) * 0.5f + 0.5f;
                float wave2 = Mathf.Sin(fx * 4.2f  - fy * 2.1f + 1.5f) * 0.5f + 0.5f;
                float wave  = (wave1 + wave2) * 0.5f;

                Color32 col;
                if      (wave > 0.82f) col = Lerp(OceanLight, OceanFoam,  (wave - 0.82f) / 0.18f);
                else if (wave > 0.55f) col = Lerp(OceanMid,   OceanLight, (wave - 0.55f) / 0.27f);
                else                   col = Lerp(OceanDeep,  OceanMid,   wave / 0.55f);

                px[y * w + x] = col;
            }

            tex.SetPixels32(px);
            tex.Apply();
            return tex;
        }

        // ── Island (128×80, alpha outside oval) ───────────────────────────

        public static Texture2D CreateIsland(int w = 128, int h = 80)
        {
            var tex = NewTex(w, h);
            var px  = new Color32[w * h];
            for (int i = 0; i < px.Length; i++) px[i] = Clear;

            float cx = w * 0.50f, cy = h * 0.42f;
            float rx = w * 0.44f, ry = h * 0.40f;

            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float fx = (x - cx) / rx, fy = (y - cy) / ry;
                float d = fx * fx + fy * fy;
                if (d > 1f) continue;

                float edge = 1f - d;
                Color32 col;
                if      (edge < 0.06f) col = SandWet;
                else if (edge < 0.18f) col = SandDark;
                else if (edge < 0.55f) col = SandMain;
                else                   col = SandLight;

                // Subtle Perlin noise variation
                float n = Mathf.PerlinNoise(x * 0.18f, y * 0.18f);
                if (n > 0.65f && edge > 0.1f)
                    col = Lerp(col, SandDark, (n - 0.65f) * 0.5f);

                px[y * w + x] = col;
            }

            tex.SetPixels32(px);
            tex.Apply();
            return tex;
        }

        // ── Palm tree (80×120, alpha background) ──────────────────────────

        public static Texture2D CreatePalmTree(int w = 80, int h = 120)
        {
            var tex = NewTex(w, h);
            var px  = new Color32[w * h];
            for (int i = 0; i < px.Length; i++) px[i] = Clear;

            int trunkBaseX = w / 2;
            int frondRootY = h - 42;   // y where fronds start

            // Trunk (gently curved, tapers from base to top)
            for (int y = 0; y < frondRootY; y++)
            {
                float f    = (float)y / frondRootY;
                int   halfW = Mathf.RoundToInt(Mathf.Lerp(4.5f, 2.5f, f));
                int   curvX = trunkBaseX + Mathf.RoundToInt(Mathf.Sin(f * Mathf.PI * 1.3f) * 5f);
                bool  ring  = (y % 9) < 3;

                for (int dx = -halfW; dx <= halfW; dx++)
                {
                    int bx = curvX + dx;
                    Color32 col;
                    if      (Mathf.Abs(dx) >= halfW) col = TrunkDark;
                    else if (ring)                   col = Lerp(TrunkDark, TrunkMain, 0.5f);
                    else if (dx > 0)                 col = TrunkLight;
                    else                             col = TrunkMain;
                    S(px, w, h, bx, y, col);
                }
            }

            // Coconuts near frond root
            int ftx = trunkBaseX + 4, fty = frondRootY;
            S(px, w, h, ftx - 3, fty + 1, CoconutCol);
            S(px, w, h, ftx - 2, fty + 2, CoconutCol);
            S(px, w, h, ftx + 4, fty + 1, CoconutCol);
            S(px, w, h, ftx + 5, fty + 2, CoconutCol);

            // Fronds radiating from top
            Frond(px, w, h, ftx, fty, -42f, 46);
            Frond(px, w, h, ftx, fty,  42f, 50);
            Frond(px, w, h, ftx, fty, -16f, 52);
            Frond(px, w, h, ftx, fty,  16f, 52);
            Frond(px, w, h, ftx, fty, -68f, 36);
            Frond(px, w, h, ftx, fty,  68f, 38);
            Frond(px, w, h, ftx, fty,   4f, 54);

            tex.SetPixels32(px);
            tex.Apply();
            return tex;
        }

        static void Frond(Color32[] px, int tw, int th, int ox, int oy, float angleDeg, int length)
        {
            float rad = angleDeg * Mathf.Deg2Rad;
            float dx  = Mathf.Sin(rad), dy = Mathf.Cos(rad);

            for (int i = 2; i < length; i++)
            {
                float f     = (float)i / length;
                float droop = f * f * 9f;            // gravity droop
                int   px2   = ox + Mathf.RoundToInt(dx * i);
                int   py    = oy + Mathf.RoundToInt(dy * i - droop);
                int   hw    = Mathf.Max(1, Mathf.RoundToInt(Mathf.Lerp(3.5f, 0.5f, f)));

                float perpX = -dy, perpY = dx;
                for (int j = -hw; j <= hw; j++)
                {
                    int ppx = px2 + Mathf.RoundToInt(perpX * j);
                    int ppy = py  + Mathf.RoundToInt(perpY * j);
                    if ((uint)ppx >= (uint)tw || (uint)ppy >= (uint)th) continue;

                    Color32 col;
                    if      (Mathf.Abs(j) == hw) col = FrondDark;
                    else if (f < 0.4f)           col = FrondMain;
                    else                         col = FrondLight;

                    int idx = ppy * tw + ppx;
                    if (px[idx].a < 128) px[idx] = col;   // don't overwrite overlapping fronds
                }
            }
        }

        // ── Campfire frames (20×22, alpha background) ─────────────────────

        public static Texture2D[] CreateCampfireFrames()
            => new[] { CampfireFrame(0), CampfireFrame(1), CampfireFrame(2) };

        static Texture2D CampfireFrame(int frame)
        {
            int w = 20, h = 22;
            var tex = NewTex(w, h);
            var px  = new Color32[w * h];
            for (int i = 0; i < px.Length; i++) px[i] = Clear;

            // Logs (X shape)
            DrawLine(px, w, h, 2, 3, 17, 7, LogColor, 1);
            DrawLine(px, w, h, 17, 3, 2, 7, LogColor, 1);

            // Embers (flicker between frames)
            Color32 emberCol = frame % 2 == 0 ? Ember : Lerp(Ember, FireRed, 0.35f);
            for (int ex = 6; ex < 14; ex++)
            for (int ey = 3; ey < 6; ey++)
                S(px, w, h, ex, ey, emberCol);

            // Flames
            float sway = Mathf.Sin(frame * 2.1f) * 1.8f;
            int   fcx  = Mathf.RoundToInt(10f + sway);
            DrawFlame(px, w, h, fcx, 5, 7, 15, frame);

            tex.SetPixels32(px);
            tex.Apply();
            return tex;
        }

        static void DrawLine(Color32[] px, int tw, int th,
                             int x0, int y0, int x1, int y1,
                             Color32 col, int thick)
        {
            int steps = Mathf.Max(Mathf.Abs(x1 - x0), Mathf.Abs(y1 - y0), 1);
            for (int i = 0; i <= steps; i++)
            {
                float f = (float)i / steps;
                int bx = Mathf.RoundToInt(Mathf.Lerp(x0, x1, f));
                int by = Mathf.RoundToInt(Mathf.Lerp(y0, y1, f));
                for (int oy = -thick; oy <= thick; oy++)
                for (int ox = -thick; ox <= thick; ox++)
                    S(px, tw, th, bx + ox, by + oy, col);
            }
        }

        static void DrawFlame(Color32[] px, int tw, int th,
                              int cx, int baseY, int maxHalfW, int height, int frame)
        {
            for (int y = baseY; y < baseY + height; y++)
            {
                float f   = (float)(y - baseY) / height;
                float hw  = (1f - f) * maxHalfW;
                float sw  = Mathf.Sin(f * 6.28f + frame * 1.8f) * 1.5f;
                int   x0  = Mathf.RoundToInt(cx + sw - hw);
                int   x1  = Mathf.RoundToInt(cx + sw + hw);
                for (int x = x0; x <= x1; x++)
                {
                    Color32 col;
                    if      (f < 0.25f) col = FireRed;
                    else if (f < 0.55f) col = FireOrange;
                    else if (f < 0.80f) col = FireYellow;
                    else                col = Lerp(FireYellow, new Color32(255, 255, 200, 0), (f - 0.80f) * 5f);
                    S(px, tw, th, x, y, col);
                }
            }
        }

        // ── Johnny frames (32×48, alpha background) ───────────────────────
        // Frame 0: sitting neutral  1: looking left  2: looking right  3: waving

        public static Texture2D[] CreateJohnnyFrames()
            => new[] { JohnnyFrame(0), JohnnyFrame(1), JohnnyFrame(2), JohnnyFrame(3) };

        static Texture2D JohnnyFrame(int frame)
        {
            int W = 32, H = 48;
            var tex = NewTex(W, H);
            var px  = new Color32[W * H];
            for (int i = 0; i < px.Length; i++) px[i] = Clear;

            // Local helpers
            void F(int x, int y, int w, int h, Color32 c) => Box(px, W, H, x, y, w, h, c);
            void P(int x, int y, Color32 c)               => S(px, W, H, x, y, c);

            int cx = W / 2; // 16

            // ── Crossed legs (sitting) ──────────────────────────────────
            F(2,  0, 12, 8, ShirtDark);     // left leg
            F(18, 0, 12, 8, ShirtMain);     // right leg
            F(5,  5,  8, 5, ShirtDark);     // left knee overlap
            F(19, 5,  8, 5, ShirtMain);     // right knee overlap

            // Feet / shoes (darker)
            F(2,  0, 4, 3, C("3A2810"));
            F(26, 0, 4, 3, C("3A2810"));

            // ── Torso ───────────────────────────────────────────────────
            F(cx - 7, 9, 14, 16, ShirtMain);
            F(cx - 7, 9,  2, 16, ShirtDark);   // left shadow
            F(cx + 5, 9,  2, 16, ShirtDark);   // right shadow

            // Belt line
            F(cx - 6, 13, 12, 1, C("8A5A28"));

            // ── Arms ────────────────────────────────────────────────────
            int waveOff = (frame == 3) ? 7 : 0;
            F(cx - 12, 14, 5, 4, SkinMain);              // left arm
            F(cx + 8,  14 - waveOff, 4, 4, SkinMain);    // right arm (raised when waving)
            if (frame == 3)
                F(cx + 9, 8, 4, 6, SkinMain);            // waving hand raised

            // ── Neck ────────────────────────────────────────────────────
            F(cx - 2, 25, 4, 4, SkinMain);

            // ── Head ────────────────────────────────────────────────────
            F(cx - 5, 29, 10, 10, SkinMain);
            F(cx - 5, 29,  2, 10, SkinDark);  // left shadow
            F(cx + 3, 29,  2, 10, SkinDark);  // right shadow

            // ── Beard ───────────────────────────────────────────────────
            F(cx - 4, 29, 8, 4, BeardBrown);
            F(cx - 3, 31, 6, 3, BeardGrey);

            // ── Eyes (shift for look-left / look-right) ─────────────────
            int eShift = (frame == 1) ? -1 : (frame == 2) ? 1 : 0;
            F(cx - 3 + eShift, 36, 2, 2, EyeColor);
            F(cx + 1 + eShift, 36, 2, 2, EyeColor);
            // Eye shine
            P(cx - 3 + eShift + 1, 37, new Color32(255, 255, 255, 200));
            P(cx + 1 + eShift + 1, 37, new Color32(255, 255, 255, 200));

            // ── Hat brim ────────────────────────────────────────────────
            F(cx - 7, 39, 14, 3, HatMain);
            F(cx - 7, 39,  1, 3, HatDark);   // left edge shadow
            F(cx + 6, 39,  1, 3, HatDark);   // right edge shadow

            // ── Hat crown ───────────────────────────────────────────────
            F(cx - 4, 42, 8, 6, HatMain);
            F(cx - 4, 42, 2, 6, HatDark);
            F(cx + 3, 42, 1, 6, HatDark);
            F(cx - 3, 47, 6, 1, HatDark);   // top edge

            tex.SetPixels32(px);
            tex.Apply();
            return tex;
        }
    }
}
