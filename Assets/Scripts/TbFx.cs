// TbFx.cs
// 演出の出し方まとめ：爆発・被弾の火花・発射炎。画面揺れとヒットストップはTbShooterGameに依頼する。
using UnityEngine;

public static class TbFx
{
    public static TbParticle Spawn(Sprite sprite, Vector3 pos, int order)
    {
        var g = new GameObject("Fx");
        g.transform.SetParent(TbShooterGame.World, false);
        g.transform.position = pos;
        var sr = g.AddComponent<SpriteRenderer>();
        sr.sprite = sprite; sr.sortingOrder = order;
        return g.AddComponent<TbParticle>();
    }

    static Vector2 Rand(float min, float max)
    {
        float a = Random.value * Mathf.PI * 2f;
        return new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * Random.Range(min, max);
    }

    public static void Explosion(Vector3 pos, float size)
    {
        var game = TbShooterGame.Instance;
        if (game != null) { game.AddShake(0.35f * size); game.HitStop(0.045f); }

        // 1) 白い閃光
        var f = Spawn(TbArt.Soft(), pos, 30);
        f.life = 0.16f; f.startScale = 2.2f * size; f.endScale = 4.5f * size; f.drag = 0;
        f.c0 = new Color(1f, 1f, 0.9f, 1f); f.c1 = new Color(1f, 0.8f, 0.3f, 0f);

        // 2) 衝撃波リング
        var r = Spawn(TbArt.Ring(), pos, 29);
        r.life = 0.35f; r.startScale = 0.4f * size; r.endScale = 3.6f * size; r.drag = 0;
        r.c0 = new Color(1f, 1f, 1f, 0.9f); r.c1 = new Color(1f, 0.7f, 0.3f, 0f);

        // 3) 炎の玉
        for (int i = 0; i < 16; i++)
        {
            var p = Spawn(TbArt.Soft(), pos + (Vector3)Rand(0f, 0.3f * size), 27);
            p.vel = Rand(1.5f, 6f) * size; p.drag = 4f; p.life = Random.Range(0.35f, 0.6f);
            p.startScale = Random.Range(0.9f, 1.6f) * size; p.endScale = 0.2f;
            p.c0 = Color.Lerp(new Color(1f, 0.95f, 0.5f, 0.85f), new Color(1f, 0.5f, 0.1f, 0.85f), Random.value);
            p.c1 = new Color(0.6f, 0.1f, 0.05f, 0f);
        }
        // 4) 火花（速く飛んで細長く伸びる）
        for (int i = 0; i < 14; i++)
        {
            var p = Spawn(TbArt.Spark(), pos, 28);
            p.vel = Rand(6f, 13f) * size; p.drag = 3f; p.life = Random.Range(0.25f, 0.45f);
            p.startScale = 1f; p.endScale = 0.3f; p.alignToVel = true;
            p.c0 = Color.white; p.c1 = new Color(1f, 0.6f, 0.2f, 0f);
        }
        // 5) 煙（ゆっくり上へ）
        for (int i = 0; i < 6; i++)
        {
            var p = Spawn(TbArt.Soft(), pos + (Vector3)Rand(0f, 0.4f * size), 26);
            p.vel = Rand(0.3f, 1.2f) + Vector2.up * 0.6f; p.drag = 1f; p.life = Random.Range(0.7f, 1.1f);
            p.startScale = 1.2f * size; p.endScale = 2.4f * size;
            p.c0 = new Color(0.35f, 0.33f, 0.38f, 0.38f); p.c1 = new Color(0.5f, 0.5f, 0.55f, 0f);
        }
        // 6) 破片（重力で落ちながら回転）
        for (int i = 0; i < 8; i++)
        {
            var p = Spawn(TbShooterGame.Square(), pos, 27);
            p.vel = Rand(3f, 8f) * size + Vector2.up * 2f; p.drag = 0.8f; p.gravity = 14f;
            p.life = Random.Range(0.6f, 0.9f); p.spin = Random.Range(-720f, 720f);
            p.startScale = Random.Range(0.12f, 0.22f); p.endScale = 0.08f;
            p.c0 = new Color(0.25f, 0.22f, 0.3f, 1f); p.c1 = new Color(0.2f, 0.2f, 0.2f, 0f);
        }
    }

    public static void HitSpark(Vector3 pos)
    {
        var f = Spawn(TbArt.Soft(), pos, 28);
        f.life = 0.09f; f.startScale = 0.9f; f.endScale = 1.4f; f.drag = 0;
        f.c0 = new Color(1f, 1f, 0.85f, 1f); f.c1 = new Color(1f, 0.8f, 0.3f, 0f);
        for (int i = 0; i < 5; i++)
        {
            var p = Spawn(TbArt.Spark(), pos, 28);
            p.vel = new Vector2(Random.Range(-5f, 5f), Random.Range(1f, 7f)); p.drag = 5f;
            p.life = 0.2f; p.startScale = 0.7f; p.endScale = 0.2f; p.alignToVel = true;
            p.c0 = Color.white; p.c1 = new Color(1f, 0.7f, 0.2f, 0f);
        }
    }

    public static void Muzzle(Vector3 pos)
    {
        var f = Spawn(TbArt.Soft(), pos, 12);
        f.life = 0.06f; f.startScale = 0.55f; f.endScale = 0.2f; f.drag = 0;
        f.c0 = new Color(1f, 0.95f, 0.6f, 0.95f); f.c1 = new Color(1f, 0.6f, 0.2f, 0f);
    }

    public static void Trail(Vector3 pos)
    {
        var p = Spawn(TbArt.Soft(), pos, 7);
        p.life = 0.12f; p.startScale = 0.28f; p.endScale = 0.05f; p.drag = 0;
        p.c0 = new Color(1f, 0.7f, 0.2f, 0.4f); p.c1 = new Color(1f, 0.4f, 0.1f, 0f);
    }
}
