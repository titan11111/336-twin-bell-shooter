// TbBoss.cs
// 60秒で登場するボス（雑魚の約10倍の大きさ＝面積、HP220）。5パターンを順番に繰り返して自機に襲いかかる。
// 1 SWEEP SHOT：左右に往復しながら自機狙い3WAY
// 2 RING BURST：中央で全方位リング弾
// 3 CRUSH DIVE：赤く点滅して予告 → 自機めがけて急降下 → 着地で全方位弾 → 戻る
// 4 SPIRAL STORM：回転する2本のらせん弾
// 5 ZIGZAG RUSH：ジグザグに迫りながら扇状弾
using UnityEngine;

public sealed class TbBoss : MonoBehaviour
{
    public static TbBoss Current;
    public const int MaxHp = 220;
    static readonly string[] Names = { "1/5 SWEEP SHOT", "2/5 RING BURST", "3/5 CRUSH DIVE", "4/5 SPIRAL STORM", "5/5 ZIGZAG RUSH" };
    static readonly float[] Lengths = { 5.5f, 5f, 4.2f, 5f, 5.5f };
    const float RX = 1.85f, RY = 1.35f; // 当たり判定の楕円

    public bool Dying { get; private set; }
    public float Hp01 => Mathf.Clamp01(hp / (float)MaxHp);
    public string PatternName => entering ? "" : Names[pattern];

    int hp = MaxHp, pattern, diveStep;
    float pt, fireT, angle, flashTimer, dieT, dieFx;
    bool entering = true;
    float homeY, diveX;
    SpriteRenderer flash, body;

    public void Setup(TbArt.Pair art)
    {
        Current = this;
        homeY = TbShooterGame.HalfH - 1.9f;
        body = GetComponent<SpriteRenderer>();
        TbShooterGame.AddShadow(transform, art.body, 0.85f);
        flash = TbShooterGame.MakeSprite("Flash", art.flash, transform.position, 6, transform);
        flash.enabled = false;
        flash.color = new Color(1f, 1f, 1f, 0.45f);
        if (TbShooterGame.Instance != null) TbShooterGame.Instance.AddShake(0.3f);
    }

    public bool Contains(Vector3 p, float pad)
    {
        if (Dying || entering) return false;
        var d = p - transform.position;
        float x = d.x / (RX + pad), y = d.y / (RY + pad);
        return x * x + y * y < 1f;
    }

    public void Hit(Vector3 from, int dmg)
    {
        if (Dying || entering) return;
        hp -= dmg;
        if (flashTimer < -0.08f || dmg > 1) flashTimer = 0.04f;
        TbFx.HitSpark(from);
        TbShooterGame.Hits++;
        if (hp <= 0) { Dying = true; dieT = 0f; }
    }

    void Update()
    {
        float dt = Time.deltaTime;
        var p = transform.position;
        var pl = TbShooterGame.Player;
        Vector3 plPos = pl != null && pl.Alive ? pl.transform.position : new Vector3(0, -TbShooterGame.HalfH + 1.5f, 0);

        flashTimer -= dt;
        flash.enabled = flashTimer > 0f;

        if (Dying) { UpdateDying(dt); return; }

        if (entering)
        {
            p.y = Mathf.MoveTowards(p.y, homeY, dt * 2.2f);
            transform.position = p;
            if (Mathf.Abs(p.y - homeY) < 0.01f) { entering = false; pattern = 0; pt = 0f; }
            return;
        }

        pt += dt;
        fireT -= dt;
        var muzzle = p + new Vector3(0, -1.1f, 0);
        switch (pattern)
        {
            case 0: // SWEEP SHOT
                p.x = Mathf.Sin(pt * 1.3f) * 2.5f;
                p.y = Mathf.MoveTowards(p.y, homeY, dt * 4f);
                if (fireT <= 0f) { fireT = 0.6f; for (int i = -1; i <= 1; i++) TbEnemyBullet.Aimed(muzzle, 4.4f, i * 16f); }
                break;
            case 1: // RING BURST
                p.x = Mathf.MoveTowards(p.x, 0f, dt * 4f);
                p.y = Mathf.MoveTowards(p.y, homeY - 0.6f, dt * 3f);
                if (fireT <= 0f)
                {
                    fireT = 0.75f; angle += 11f;
                    for (int i = 0; i < 16; i++) TbEnemyBullet.Fire(p, TbEnemyBullet.Rotate(Vector2.down, angle + i * 22.5f) * 3.3f);
                }
                break;
            case 2: // CRUSH DIVE
                if (diveStep == 0)
                {
                    diveX = plPos.x;
                    p.x = Mathf.MoveTowards(p.x, Mathf.Clamp(diveX, -2.6f, 2.6f), dt * 5f) + Random.Range(-0.05f, 0.05f);
                    flashTimer = Mathf.Repeat(pt * 10f, 1f) > 0.5f ? 0.02f : 0f;
                    body.color = Color.Lerp(Color.white, new Color(1f, 0.4f, 0.4f), Mathf.Repeat(pt * 5f, 1f));
                    if (pt > 1.0f) { diveStep = 1; body.color = Color.white; }
                }
                else if (diveStep == 1)
                {
                    p.y = Mathf.MoveTowards(p.y, -TbShooterGame.HalfH + 1.6f, dt * 13f);
                    if (p.y <= -TbShooterGame.HalfH + 1.61f)
                    {
                        diveStep = 2;
                        if (TbShooterGame.Instance != null) TbShooterGame.Instance.AddShake(0.45f);
                        TbFx.HitSpark(p + Vector3.down * 1.2f);
                        for (int i = 0; i < 12; i++) TbEnemyBullet.Fire(p, TbEnemyBullet.Rotate(Vector2.up, i * 30f) * 3f);
                    }
                }
                else p.y = Mathf.MoveTowards(p.y, homeY, dt * 5f);
                break;
            case 3: // SPIRAL STORM
                p.x = Mathf.MoveTowards(p.x, 0f, dt * 3f);
                p.y = Mathf.MoveTowards(p.y, homeY - 0.3f, dt * 3f);
                if (fireT <= 0f)
                {
                    fireT = 0.1f; angle += 17f;
                    TbEnemyBullet.Fire(p, TbEnemyBullet.Rotate(Vector2.down, angle) * 3.4f);
                    TbEnemyBullet.Fire(p, TbEnemyBullet.Rotate(Vector2.down, angle + 180f) * 3.4f);
                }
                break;
            case 4: // ZIGZAG RUSH
                p.x = Mathf.Sin(pt * 3.2f) * 2.6f;
                p.y = homeY - Mathf.Sin(Mathf.Clamp01(pt / Lengths[4]) * Mathf.PI) * 4.2f;
                if (fireT <= 0f) { fireT = 0.45f; for (int i = -2; i <= 2; i++) TbEnemyBullet.Fire(muzzle, TbEnemyBullet.Rotate(Vector2.down, i * 13f) * 4f); }
                break;
        }
        transform.position = p;
        transform.rotation = Quaternion.Euler(0, 0, Mathf.Sin(Time.time * 2f) * 3f);

        if (pt > Lengths[pattern])
        {
            pattern = (pattern + 1) % 5; pt = 0f; fireT = 0.4f; diveStep = 0; body.color = Color.white;
        }

        // 本体への体当たり
        if (pl != null && pl.Alive)
        {
            var d = plPos - p;
            float x = d.x / (RX * 0.85f), y = d.y / (RY * 0.85f);
            if (x * x + y * y < 1f) pl.Damage();
        }
    }

    void UpdateDying(float dt)
    {
        dieT += dt; dieFx -= dt;
        transform.position += new Vector3(Random.Range(-0.06f, 0.06f), -dt * 0.5f, 0f);
        if (dieFx <= 0f)
        {
            dieFx = 0.14f;
            var off = new Vector3(Random.Range(-RX, RX), Random.Range(-RY, RY), 0f);
            TbFx.Explosion(transform.position + off, Random.Range(0.7f, 1.2f));
        }
        if (dieT > 2.2f)
        {
            TbFx.Explosion(transform.position, 3f);
            if (TbShooterGame.Instance != null) TbShooterGame.Instance.OnBossDefeated();
            Current = null;
            Destroy(gameObject);
        }
    }

    void OnDestroy() { if (Current == this) Current = null; }
}
