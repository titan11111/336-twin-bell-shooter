// TbPlayer.cs
// 自機：十字キー/キーボードで移動、ゲーム画面をなぞると指の移動量だけ移動。
// A（またはゲーム画面タッチ）で連射。A/Bを押し続けるとため → 離すとバズーカ発射（反動で少し下がる）。
// 敵・敵弾に当たると残機-1、2秒無敵（点滅）。
using UnityEngine;

public sealed class TbPlayer : MonoBehaviour
{
    public float speed = 6.5f;
    public float fireInterval = 0.09f;
    public float dragGain = 1.2f;
    public const float Size = 1.0f;
    public const float HitRadius = 0.26f;      // 当たり判定は見た目より小さく（シューティングの定番）
    const float ChargeStart = 0.2f, ChargeFull = 1.1f;

    public bool Alive { get; private set; } = true;
    public static bool God;                  // エディタ確認用の無敵（9キーのボス確認モード）

    float fireTimer, bank, flickerT, charge, invuln, recoil;
    Vector2 vel;
    bool leftGun, fullNotified;
    int seenReset;
    Transform flameL, flameR;
    SpriteRenderer body, orb;

    void Start()
    {
        body = GetComponent<SpriteRenderer>();
        TbShooterGame.AddShadow(transform, TbArt.Ship().body, 0.85f);
        flameL = TbShooterGame.MakeSprite("FlameL", TbArt.Flame(), transform.position, 34, transform).transform;
        flameR = TbShooterGame.MakeSprite("FlameR", TbArt.Flame(), transform.position, 34, transform).transform;
        flameL.localPosition = new Vector3(-0.09f, -0.6f, 0f);
        flameR.localPosition = new Vector3(0.09f, -0.6f, 0f);
        orb = TbShooterGame.MakeSprite("ChargeOrb", TbArt.Soft(), transform.position, 36, transform);
        orb.transform.localPosition = new Vector3(0f, 0.85f, 0f);
        orb.enabled = false;
        if (TbControls.I != null) seenReset = TbControls.I.ResetCount;
    }

    void Update()
    {
        var c = TbControls.I;
        if (c == null || Time.timeScale == 0f) return;
        float dt = Time.deltaTime;

        // ----- 移動 -----
        // なめらか移動：十字キー/キーは加速・減速つき、指ドラッグは指の動きにそのまま追従
        vel = Vector2.MoveTowards(vel, c.Move * speed, (c.Move == Vector2.zero ? 45f : 32f) * dt);
        var p = transform.position + (Vector3)(vel * dt) + c.DragDelta * dragGain;
        recoil = Mathf.MoveTowards(recoil, 0f, dt * 6f);
        p.y -= recoil * dt * 6f;
        p.x = Mathf.Clamp(p.x, -TbShooterGame.HalfW + 0.55f, TbShooterGame.HalfW - 0.55f);
        p.y = Mathf.Clamp(p.y, -TbShooterGame.HalfH + 0.6f, TbShooterGame.HalfH - 0.9f);
        float dragX = dt > 0f ? Mathf.Clamp(c.DragDelta.x / (speed * dt), -1f, 1f) : 0f;
        transform.position = p;

        bank = Mathf.Lerp(bank, Mathf.Clamp(vel.x / speed + dragX, -1f, 1f), dt * 10f);
        transform.rotation = Quaternion.Euler(0, 0, -bank * 14f);
        transform.localScale = new Vector3((1f - Mathf.Abs(bank) * 0.22f) * Size, Size, 1f);

        flickerT += dt * 40f;
        float boost = c.Move.y > 0f ? 0.35f : 0f;
        flameL.localScale = new Vector3(1f, 0.85f + Mathf.PerlinNoise(flickerT, 0f) * 0.6f + boost, 1f);
        flameR.localScale = new Vector3(1f, 0.85f + Mathf.PerlinNoise(0f, flickerT) * 0.6f + boost, 1f);

        // 無敵中は点滅
        if (invuln > 0f) { invuln -= dt; body.enabled = invuln <= 0f || Mathf.Repeat(invuln * 12f, 1f) > 0.4f; }

        // ----- 連射 -----
        fireTimer -= dt;
        if ((c.AHeld || c.GameTouchHeld) && fireTimer <= 0f)
        {
            fireTimer = fireInterval;
            leftGun = !leftGun;
            var muzzle = transform.position + new Vector3(leftGun ? -0.2f : 0.2f, 0.7f, 0f);
            var b = TbShooterGame.MakeSprite("Bullet", TbArt.Bullet(), muzzle, 30);
            b.transform.localScale = new Vector3(1.4f, 1.1f, 1f);
            b.gameObject.AddComponent<TbBullet>();
            TbFx.Muzzle(muzzle);
        }

        // ----- ため撃ち（A/B長押し → 離すとバズーカ） -----
        if (c.ResetCount != seenReset) { seenReset = c.ResetCount; charge = 0f; } // アプリ切替などで中断
        bool holding = c.AHeld || c.BHeld;
        if (holding) charge += dt;
        else
        {
            if (charge >= ChargeFull) FireBazooka();
            charge = 0f; fullNotified = false;
        }
        float k = Mathf.Clamp01((charge - ChargeStart) / (ChargeFull - ChargeStart));
        bool full = charge >= ChargeFull;
        c.SetCharge(k, full);
        orb.enabled = charge > ChargeStart;
        if (orb.enabled)
        {
            float pulse = 1f + Mathf.Sin(Time.time * (full ? 30f : 12f)) * 0.12f;
            orb.transform.localScale = Vector3.one * (0.35f + 0.9f * k) * pulse;
            orb.color = full ? Color.Lerp(new Color(0.5f, 1f, 1f), Color.white, Mathf.Repeat(Time.time * 8f, 1f)) : new Color(1f, 0.6f + 0.3f * k, 0.2f, 0.85f);
            if (Random.value < 0.5f) // 吸い込まれる光の粒
            {
                var a = Random.value * Mathf.PI * 2f;
                var from = orb.transform.position + new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0) * 1.1f;
                var q = TbFx.Spawn(TbArt.Spark(), from, 36);
                q.vel = (orb.transform.position - from) * 6f; q.drag = 0; q.life = 0.16f; q.alignToVel = true;
                q.startScale = 0.5f; q.endScale = 0.1f; q.c0 = new Color(1f, 0.9f, 0.5f, 0.9f); q.c1 = new Color(1f, 1f, 1f, 0f);
            }
            if (full && !fullNotified)
            {
                fullNotified = true;
                var r = TbFx.Spawn(TbArt.Ring(), orb.transform.position, 36);
                r.life = 0.25f; r.startScale = 1.6f; r.endScale = 0.3f; r.drag = 0;
                r.c0 = new Color(0.6f, 1f, 1f, 1f); r.c1 = new Color(1f, 1f, 1f, 0f);
            }
        }
    }

    void FireBazooka()
    {
        var pos = transform.position + new Vector3(0f, 0.9f, 0f);
        var g = TbShooterGame.MakeSprite("Bazooka", TbArt.Bazooka(), pos, 31);
        g.transform.localScale = new Vector3(1.3f, 1.3f, 1f);
        g.gameObject.AddComponent<TbBazooka>();
        recoil = 1.2f; // 反動
        var game = TbShooterGame.Instance;
        if (game != null) { game.AddShake(0.25f); game.HitStop(0.03f); }
        var f = TbFx.Spawn(TbArt.Soft(), pos, 32);
        f.life = 0.15f; f.startScale = 2.2f; f.endScale = 0.6f; f.drag = 0;
        f.c0 = new Color(1f, 0.95f, 0.7f, 1f); f.c1 = new Color(1f, 0.4f, 0.1f, 0f);
        for (int i = 0; i < 8; i++)
        {
            var s = TbFx.Spawn(TbArt.Soft(), pos + Vector3.down * 0.6f, 29);
            s.vel = new Vector2(Random.Range(-3f, 3f), Random.Range(-4f, -1f)); s.drag = 3f; s.life = 0.5f;
            s.startScale = 0.6f; s.endScale = 1.4f; s.c0 = new Color(0.7f, 0.7f, 0.75f, 0.5f); s.c1 = new Color(0.8f, 0.8f, 0.85f, 0f);
        }
    }

    public void Damage()
    {
        if (!Alive || invuln > 0f || God) return;
        var game = TbShooterGame.Instance;
        TbFx.Explosion(transform.position, 0.9f);
        game.Lives--;
        charge = 0f;
        if (game.Lives <= 0)
        {
            Alive = false;
            game.OnPlayerDead();
            gameObject.SetActive(false);
        }
        else invuln = 2f;
    }

    public bool Hits(Vector3 pos, float radius)
    {
        float r = radius + HitRadius;
        return Alive && invuln <= 0f && (pos - transform.position).sqrMagnitude < r * r;
    }
}
