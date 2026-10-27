// TbArt.cs
// 画像ファイルを使わず、コードで「立体っぽい」スプライトを描く（SVGと同じく数式で形を作り、さらに光と影を計算）。
// 左上から光が当たる想定で、楕円体ごとに陰影・ハイライト・輪郭を付ける。
using UnityEngine;

public static class TbArt
{
    static readonly Vector3 LightDir = new Vector3(-0.45f, 0.6f, 0.66f).normalized;
    static readonly Vector3 Half = (new Vector3(-0.45f, 0.6f, 0.66f).normalized + Vector3.forward).normalized;
    static float edgePx = 32f; // [-1,1]座標で1あたり何ピクセルか（輪郭のなめらかさ用）

    public delegate Color PixelFn(float x, float y);

    // 敵・自機用：本体スプライトと、被弾時に光らせる白シルエットを同時に作る
    public struct Pair { public Sprite body, flash; }

    static Color[] Paint(int w, int h, PixelFn fn)
    {
        edgePx = Mathf.Min(w, h) * 0.5f;
        var px = new Color[w * h];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                px[y * w + x] = fn((x + 0.5f) / w * 2f - 1f, (y + 0.5f) / h * 2f - 1f);
        return px;
    }

    static Sprite ToSprite(Color[] px, int w, int h, float ppu)
    {
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;
        tex.SetPixels(px); tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), ppu);
    }

    public static Sprite Make(int w, int h, float ppu, PixelFn fn) { return ToSprite(Paint(w, h, fn), w, h, ppu); }

    static Pair MakePair(int w, int h, float ppu, PixelFn fn)
    {
        var px = Paint(w, h, fn);
        var white = new Color[px.Length];
        for (int i = 0; i < px.Length; i++) white[i] = new Color(1f, 1f, 1f, px[i].a);
        return new Pair { body = ToSprite(px, w, h, ppu), flash = ToSprite(white, w, h, ppu) };
    }

    // 重ね塗り（上に src を置く）
    public static Color Over(Color dst, Color src)
    {
        float a = src.a + dst.a * (1f - src.a);
        if (a <= 0.0001f) return new Color(0, 0, 0, 0);
        float r = (src.r * src.a + dst.r * dst.a * (1f - src.a)) / a;
        float g = (src.g * src.a + dst.g * dst.a * (1f - src.a)) / a;
        float b = (src.b * src.a + dst.b * dst.a * (1f - src.a)) / a;
        return new Color(r, g, b, a);
    }

    // 陰影付きの楕円体（立体の球・カプセル風）
    public static Color Ellip(float x, float y, float cx, float cy, float rx, float ry, Color col, float shine = 0.6f)
    {
        float dx = (x - cx) / rx, dy = (y - cy) / ry;
        float r = Mathf.Sqrt(dx * dx + dy * dy);
        if (r >= 1f) return new Color(0, 0, 0, 0);
        float alpha = Mathf.Clamp01((1f - r) * Mathf.Min(rx, ry) * edgePx * 0.9f);
        float nz = Mathf.Sqrt(Mathf.Max(0f, 1f - r * r));
        var n = new Vector3(dx, dy, nz).normalized;
        float diff = Mathf.Max(0f, Vector3.Dot(n, LightDir));
        float spec = Mathf.Pow(Mathf.Max(0f, Vector3.Dot(n, Half)), 28f) * shine;
        float shade = 0.32f + 0.85f * diff;
        var c = new Color(col.r * shade + spec, col.g * shade + spec, col.b * shade + spec, alpha);
        if (r > 0.88f) { c.r *= 0.45f; c.g *= 0.45f; c.b *= 0.5f; } // アニメ風の輪郭
        return c;
    }

    static Pair ship, drone, dart, boss;
    static Sprite bullet, enemyBullet, bazooka, flame, soft, ring, spark, cloud, sky;
    static Sprite dpad, dpadBase, btnA, btnB, btnSmall, body, bar;

    public static Color Mul(Color c, float k) { return new Color(c.r * k, c.g * k, c.b * k, c.a); }

    // 角丸の箱（ふち＝上は明るく下は暗い、で立体ボタン風）
    static float BoxDist(float X, float Y, float hw, float hh, float r)
    {
        float qx = Mathf.Abs(X) - hw + r, qy = Mathf.Abs(Y) - hh + r;
        float ox = Mathf.Max(qx, 0f), oy = Mathf.Max(qy, 0f);
        return Mathf.Sqrt(ox * ox + oy * oy) + Mathf.Min(Mathf.Max(qx, qy), 0f) - r; // 内側が負
    }

    public static Sprite Panel(int w, int h, float ppu, float radiusPx, Color col, float bevelPx)
    {
        return Make(w, h, ppu, (x, y) =>
        {
            float X = x * w * 0.5f, Y = y * h * 0.5f;
            float d = BoxDist(X, Y, w * 0.5f - 1f, h * 0.5f - 1f, radiusPx);
            if (d > 0.5f) return new Color(0, 0, 0, 0);
            float a = Mathf.Clamp01(0.5f - d);
            float e = -d;
            float k = 0.92f + 0.12f * y;
            if (e < bevelPx) k *= Mathf.Lerp(y > 0f ? 1.35f : 0.6f, 1f, e / bevelPx);
            var c = Mul(col, k); c.a = a;
            return c;
        });
    }

    // 十字キー（明るいグレーの立体十字）
    public static Sprite DPad()
    {
        if (dpad != null) return dpad;
        const int N = 160;
        return dpad = Make(N, N, 52f, (x, y) =>
        {
            float X = x * N * 0.5f, Y = y * N * 0.5f;
            float d = Mathf.Min(BoxDist(X, Y, 78f, 26f, 10f), BoxDist(X, Y, 26f, 78f, 10f));
            if (d > 0.5f) return new Color(0, 0, 0, 0);
            float a = Mathf.Clamp01(0.5f - d), e = -d;
            float k = 0.95f + 0.12f * y;
            if (e < 6f) k *= Mathf.Lerp((x - y) < 0f ? 1.25f : 0.6f, 1f, e / 6f);
            float r = Mathf.Sqrt(X * X + Y * Y);
            if (r < 14f) k *= 0.8f + 0.2f * r / 14f; // 中央のくぼみ
            var c = Mul(new Color(0.78f, 0.82f, 0.9f), k); c.a = a;
            return c;
        });
    }

    // 十字キーの台座（くぼんだ円）
    public static Sprite DPadBase()
    {
        if (dpadBase != null) return dpadBase;
        return dpadBase = Make(128, 128, 32f, (x, y) =>
        {
            float r = Mathf.Sqrt(x * x + y * y);
            if (r > 1f) return new Color(0, 0, 0, 0);
            float a = Mathf.Clamp01((1f - r) * 64f);
            float k = 0.75f - 0.25f * y * r; // 内側にくぼんで見える（上が暗く下が明るい）
            if (r > 0.92f) k = 1.1f;
            return new Color(0.1f * k + 0.05f, 0.13f * k + 0.05f, 0.2f * k + 0.07f, a);
        });
    }

    static Sprite RoundButton(Color col)
    {
        return Make(128, 128, 96f, (x, y) =>
        {
            var c = Ellip(x, y, 0f, -0.05f, 0.97f, 0.97f, Mul(col, 0.35f), 0.2f); // 側面（影）
            c = Over(c, Ellip(x, y, 0f, 0.04f, 0.86f, 0.86f, col, 1.0f));
            return c;
        });
    }

    public static Sprite ButtonA() { return btnA != null ? btnA : btnA = RoundButton(new Color(1f, 0.55f, 0.15f)); }
    public static Sprite ButtonB() { return btnB != null ? btnB : btnB = RoundButton(new Color(0.15f, 0.78f, 0.82f)); }
    public static Sprite RedButton() { return ButtonA(); }

    public static Sprite SmallButton()
    {
        if (btnSmall != null) return btnSmall;
        return btnSmall = Panel(120, 40, 100f, 19f, new Color(0.32f, 0.36f, 0.48f), 5f);
    }

    // オリジナル・コントローラー本体：濃紺のボディ＋上辺のティールのライン
    public static Sprite ControllerBody()
    {
        if (body != null) return body;
        var baseCol = new Color(0.16f, 0.19f, 0.29f);
        var inner = Panel(360, 192, 40f, 40f, baseCol, 8f);
        var src = inner.texture.GetPixels();
        for (int yy = 0; yy < 192; yy++)
            for (int xx = 0; xx < 360; xx++)
            {
                int i = yy * 360 + xx;
                if (src[i].a <= 0f) continue;
                if (yy > 176 && yy < 184) { var c = new Color(0.2f, 0.85f, 0.85f, src[i].a); src[i] = c; } // アクセントライン
            }
        inner.texture.SetPixels(src); inner.texture.Apply();
        return body = inner;
    }
    public static Sprite GoldPlate() { return ControllerBody(); }
    public static Sprite BlackSlot() { return SmallButton(); }
    public static Sprite Bar() { return bar != null ? bar : bar = Panel(64, 16, 64f, 6f, Color.white, 3f); }

    // 回転した楕円体（翼など斜めの部品用）
    public static Color EllipRot(float x, float y, float cx, float cy, float rx, float ry, float deg, Color col, float shine = 0.6f)
    {
        float r = -deg * Mathf.Deg2Rad, c = Mathf.Cos(r), s = Mathf.Sin(r);
        float dx = x - cx, dy = y - cy;
        return Ellip(dx * c - dy * s, dx * s + dy * c, 0f, 0f, rx, ry, col, shine);
    }

    // 自機：オリジナルの矢じり型ファイター「スワロー」
    // 白い細身の胴体・赤い機首・オレンジの後退翼・2枚の尾翼・ティールのキャノピー・2基のノズル
    public static Pair Ship()
    {
        if (ship.body != null) return ship;
        ship = MakePair(96, 96, 72f, (x, y) =>
        {
            var c = new Color(0, 0, 0, 0);
            var wing = new Color(1f, 0.55f, 0.15f);
            var fin = new Color(0.85f, 0.4f, 0.12f);
            c = Over(c, EllipRot(x, y, -0.27f, -0.6f, 0.26f, 0.09f, 40f, fin, 0.6f));
            c = Over(c, EllipRot(x, y, 0.27f, -0.6f, 0.26f, 0.09f, -40f, fin, 0.6f));
            c = Over(c, EllipRot(x, y, -0.46f, -0.18f, 0.58f, 0.15f, 28f, wing, 0.7f));
            c = Over(c, EllipRot(x, y, 0.46f, -0.18f, 0.58f, 0.15f, -28f, wing, 0.7f));
            c = Over(c, Ellip(x, y, -0.13f, -0.74f, 0.09f, 0.13f, new Color(0.3f, 0.32f, 0.38f), 0.9f));
            c = Over(c, Ellip(x, y, 0.13f, -0.74f, 0.09f, 0.13f, new Color(0.3f, 0.32f, 0.38f), 0.9f));
            c = Over(c, Ellip(x, y, 0f, -0.05f, 0.21f, 0.82f, new Color(0.93f, 0.95f, 1f), 0.9f));
            c = Over(c, Ellip(x, y, 0f, 0.66f, 0.11f, 0.22f, new Color(0.95f, 0.25f, 0.22f), 1.0f));
            c = Over(c, Ellip(x, y, 0f, 0.22f, 0.1f, 0.24f, new Color(0.2f, 0.85f, 0.9f), 1.5f));
            return c;
        });
        return ship;
    }

    // 敵A：赤いまんまるドローン（リング＋大きな目）
    public static Pair Drone()
    {
        if (drone.body != null) return drone;
        drone = MakePair(96, 96, 80f, (x, y) =>
        {
            var c = new Color(0, 0, 0, 0);
            c = Over(c, Ellip(x, y, 0f, 0f, 0.95f, 0.3f, new Color(0.55f, 0.45f, 0.75f), 0.7f));
            c = Over(c, Ellip(x, y, 0f, 0.06f, 0.62f, 0.62f, new Color(1f, 0.3f, 0.32f), 0.9f));
            c = Over(c, Ellip(x, y, 0f, -0.1f, 0.27f, 0.2f, new Color(1f, 0.95f, 0.5f), 1.0f));
            c = Over(c, Ellip(x, y, 0f, -0.13f, 0.1f, 0.1f, new Color(0.1f, 0.05f, 0.1f), 1.2f));
            return c;
        });
        return drone;
    }

    // 敵B：緑のダーツ型（下向きに突っ込んでくる）
    public static Pair Dart()
    {
        if (dart.body != null) return dart;
        dart = MakePair(96, 96, 85f, (x, y) =>
        {
            var c = new Color(0, 0, 0, 0);
            c = Over(c, Ellip(x, y, 0f, 0.25f, 0.92f, 0.2f, new Color(0.2f, 0.65f, 0.35f), 0.6f));
            c = Over(c, Ellip(x, y, 0f, 0f, 0.33f, 0.88f, new Color(0.35f, 0.95f, 0.45f), 0.8f));
            c = Over(c, Ellip(x, y, 0f, -0.38f, 0.14f, 0.2f, new Color(1f, 0.55f, 0.2f), 1.2f));
            return c;
        });
        return dart;
    }

    // ボス：雑魚の約10倍（面積）の要塞メカ。両腕の砲台・装甲帯・赤いコア・ガラスドーム
    public static Pair Boss()
    {
        if (boss.body != null) return boss;
        boss = MakePair(192, 160, 48f, (x, y) =>
        {
            var c = new Color(0, 0, 0, 0);
            c = Over(c, Ellip(x, y, -0.74f, -0.08f, 0.26f, 0.55f, new Color(0.62f, 0.62f, 0.7f), 0.8f));
            c = Over(c, Ellip(x, y, 0.74f, -0.08f, 0.26f, 0.55f, new Color(0.62f, 0.62f, 0.7f), 0.8f));
            c = Over(c, Ellip(x, y, -0.74f, -0.62f, 0.1f, 0.22f, new Color(0.25f, 0.22f, 0.3f), 1.0f));
            c = Over(c, Ellip(x, y, 0.74f, -0.62f, 0.1f, 0.22f, new Color(0.25f, 0.22f, 0.3f), 1.0f));
            c = Over(c, Ellip(x, y, 0f, 0.02f, 0.62f, 0.78f, new Color(0.52f, 0.32f, 0.78f), 0.8f));
            c = Over(c, Ellip(x, y, 0f, -0.18f, 0.66f, 0.24f, new Color(0.8f, 0.76f, 0.86f), 0.7f));
            c = Over(c, Ellip(x, y, 0f, -0.68f, 0.2f, 0.18f, new Color(0.3f, 0.25f, 0.35f), 0.6f));
            c = Over(c, Ellip(x, y, 0f, 0.1f, 0.25f, 0.28f, new Color(1f, 0.18f, 0.22f), 1.6f));
            c = Over(c, Ellip(x, y, 0f, 0.6f, 0.28f, 0.2f, new Color(0.55f, 0.95f, 1f), 1.4f));
            return c;
        });
        return boss;
    }

    // 敵弾：マゼンタの縁＋白い芯。爆発（オレンジ）や空（青）に埋もれない色
    public static Sprite EnemyBullet()
    {
        if (enemyBullet != null) return enemyBullet;
        return enemyBullet = Make(32, 32, 80f, (x, y) =>
        {
            float r = Mathf.Sqrt(x * x + y * y);
            if (r > 1f) return new Color(0, 0, 0, 0);
            if (r > 0.82f) return new Color(0.25f, 0f, 0.2f, Mathf.Clamp01((1f - r) * 12f));
            if (r > 0.5f) return new Color(1f, 0.2f, 0.75f, 1f);
            return Color.Lerp(Color.white, new Color(1f, 0.6f, 0.9f), r / 0.5f);
        });
    }

    // 光る弾（白い芯＋色つきのにじみ）
    static Sprite Glow(int w, int h, float ppu, float halfLenPx, float corePx, float glowPx, Color glow)
    {
        return Make(w, h, ppu, (x, y) =>
        {
            float X = x * w * 0.5f, Y = y * h * 0.5f;
            float dy = Mathf.Max(0f, Mathf.Abs(Y) - halfLenPx);
            float d = Mathf.Sqrt(X * X + dy * dy);
            float core = Mathf.Clamp01(1f - d / corePx);
            float g = Mathf.Exp(-d * d / (glowPx * glowPx));
            var c = Color.Lerp(glow, Color.white, core);
            c.a = Mathf.Max(core, g * 0.9f);
            return c;
        });
    }

    public static Sprite Bullet() { return bullet != null ? bullet : bullet = Glow(24, 64, 64f, 16f, 3.2f, 6f, new Color(1f, 0.75f, 0.15f)); }
    public static Sprite Bazooka() { return bazooka != null ? bazooka : bazooka = Glow(40, 96, 64f, 22f, 7f, 11f, new Color(1f, 0.35f, 0.1f)); }
    public static Sprite Flame() { return flame != null ? flame : flame = Glow(24, 48, 80f, 8f, 3f, 6f, new Color(0.3f, 0.7f, 1f)); }
    public static Sprite Spark() { return spark != null ? spark : spark = Glow(12, 40, 64f, 10f, 2f, 3.5f, new Color(1f, 0.8f, 0.3f)); }

    public static Sprite Soft()
    {
        if (soft != null) return soft;
        return soft = Make(64, 64, 64f, (x, y) =>
        {
            float r = Mathf.Sqrt(x * x + y * y);
            float a = Mathf.Clamp01(1f - r); a *= a;
            return new Color(1f, 1f, 1f, a);
        });
    }

    public static Sprite Ring()
    {
        if (ring != null) return ring;
        return ring = Make(128, 128, 128f, (x, y) =>
        {
            float r = Mathf.Sqrt(x * x + y * y);
            float a = Mathf.Exp(-((r - 0.82f) * (r - 0.82f)) / 0.004f);
            return new Color(1f, 1f, 1f, a);
        });
    }

    // ふんわり雲（上側が明るい）
    public static Sprite Cloud()
    {
        if (cloud != null) return cloud;
        Vector3[] blobs = {
            new Vector3(-0.55f, -0.2f, 0.42f), new Vector3(-0.15f, 0.05f, 0.55f),
            new Vector3(0.3f, -0.05f, 0.48f), new Vector3(0.65f, -0.25f, 0.33f), new Vector3(0.05f, -0.35f, 0.45f) };
        return cloud = Make(160, 80, 40f, (x, y) =>
        {
            float dens = 0f;
            foreach (var b in blobs)
            {
                float dx = (x - b.x) / b.z, dy = (y - b.y) * 0.5f / b.z * 1.6f;
                dens = Mathf.Max(dens, 1f - Mathf.Sqrt(dx * dx + dy * dy));
            }
            float a = Mathf.SmoothStep(0f, 0.25f, dens);
            float lit = 0.8f + 0.2f * Mathf.Clamp01(y + 0.6f);
            return new Color(lit, lit, Mathf.Min(1f, lit + 0.05f), a);
        });
    }

    // 空のグラデーション背景
    public static Sprite Sky()
    {
        if (sky != null) return sky;
        return sky = Make(4, 128, 8f, (x, y) =>
        {
            float t = (y + 1f) * 0.5f;
            return Color.Lerp(new Color(0.55f, 0.82f, 1f), new Color(0.12f, 0.32f, 0.82f), t);
        });
    }
}
