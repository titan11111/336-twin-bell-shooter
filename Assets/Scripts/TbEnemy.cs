// TbEnemy.cs
// 敵：ドローン（揺れながら降下・HP3・たまに自機狙い弾）とダーツ（自機の方へ突っ込む・HP2）。
// 被弾で白く光ってノックバック、HP0で爆発。自機に体当たりするとダメージ。
using System.Collections.Generic;
using UnityEngine;

public sealed class TbEnemy : MonoBehaviour
{
    public enum Kind { Drone, Dart }
    public static readonly List<TbEnemy> All = new List<TbEnemy>();
    public const float Scale = 1.0f;

    public Kind kind;
    public float Radius { get; private set; }
    public bool Dead { get; private set; }

    int hp;
    float speed, baseX, t, flashTimer, fireTimer;
    Vector2 knock;
    SpriteRenderer flash;

    void OnEnable() { All.Add(this); }
    void OnDisable() { All.Remove(this); }

    public void Setup(Kind k, TbArt.Pair art)
    {
        kind = k;
        hp = k == Kind.Dart ? 2 : 3;
        speed = k == Kind.Dart ? 3.0f : 1.8f;
        Radius = k == Kind.Dart ? 0.48f : 0.58f;
        baseX = transform.position.x;
        t = Random.value * 6f;
        fireTimer = Random.Range(1.2f, 3f);
        TbShooterGame.AddShadow(transform, art.body, 0.8f);
        flash = TbShooterGame.MakeSprite("Flash", art.flash, transform.position, 6, transform);
        flash.enabled = false;
    }

    public void Hit(Vector3 from, int damage)
    {
        if (Dead) return;
        hp -= damage;
        flashTimer = 0.07f;
        knock += new Vector2((transform.position.x - from.x) * 3f, 2.8f); // ノックバック：当たった側の反対・上へ押し返す
        TbFx.HitSpark(from + Vector3.up * 0.15f);
        var game = TbShooterGame.Instance;
        if (game != null) game.AddShake(0.05f);
        if (hp <= 0)
        {
            Dead = true;
            if (game != null) game.AddScore(kind == Kind.Dart ? 150 : 100);
            TbFx.Explosion(transform.position, kind == Kind.Dart ? 0.75f : 0.9f);
            Destroy(gameObject);
        }
    }

    void Update()
    {
        float dt = Time.deltaTime;
        t += dt;
        var p = transform.position;
        var pl = TbShooterGame.Player;

        if (kind == Kind.Drone)
        {
            baseX += knock.x * dt;
            p.x = baseX + Mathf.Sin(t * 2f) * 1.1f;
            transform.rotation = Quaternion.Euler(0, 0, Mathf.Sin(t * 3f) * 10f + knock.x * 4f);
            fireTimer -= dt;
            if (fireTimer <= 0f)
            {
                fireTimer = Random.Range(2.2f, 3.5f);
                if (p.y < TbShooterGame.HalfH - 0.3f && p.y > -1f) TbEnemyBullet.Aimed(p + Vector3.down * 0.4f, 3.6f);
            }
        }
        else
        {
            float target = pl != null && pl.Alive ? pl.transform.position.x : p.x;
            p.x = Mathf.MoveTowards(p.x, target, dt * 0.8f) + knock.x * dt;
            transform.rotation = Quaternion.Euler(0, 0, (target - p.x) * 6f);
        }
        p.y += (-speed + knock.y) * dt;
        knock = Vector2.MoveTowards(knock, Vector2.zero, dt * 12f);
        p.x = Mathf.Clamp(p.x, -TbShooterGame.HalfW + 0.45f, TbShooterGame.HalfW - 0.45f);
        transform.position = p;

        flashTimer -= dt;
        flash.enabled = flashTimer > 0f;
        float s = (flashTimer > 0f ? 1.15f : 1f) * Scale;
        transform.localScale = new Vector3(s, s, 1f);

        // 体当たり（ノックバック後の位置で判定）
        if (pl != null && pl.Hits(p, Radius * 0.75f)) { pl.Damage(); Hit(p, 99); return; }

        if (p.y < -TbShooterGame.HalfH - 1.5f) Destroy(gameObject);
    }
}
