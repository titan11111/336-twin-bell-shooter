// TbBazooka.cs
// ため撃ちのバズーカ：加速しながら上昇、煙と炎の尾。最初に当たった所で大爆発し、周囲の敵をまとめて巻き込む。
using System.Collections.Generic;
using UnityEngine;

public sealed class TbBazooka : MonoBehaviour
{
    public const float BlastRadius = 2.4f;
    float speed = 4f, trail;

    void Update()
    {
        float dt = Time.deltaTime;
        speed = Mathf.Min(22f, speed + 45f * dt);
        transform.position += Vector3.up * speed * dt;

        trail -= dt;
        if (trail <= 0f)
        {
            trail = 0.015f;
            var tail = transform.position + Vector3.down * 0.7f;
            var f = TbFx.Spawn(TbArt.Soft(), tail, 28);
            f.vel = new Vector2(Random.Range(-0.6f, 0.6f), -2f); f.drag = 2f; f.life = 0.22f;
            f.startScale = 0.8f; f.endScale = 0.2f; f.c0 = new Color(1f, 0.85f, 0.4f, 0.9f); f.c1 = new Color(1f, 0.3f, 0.1f, 0f);
            var s = TbFx.Spawn(TbArt.Soft(), tail, 27);
            s.vel = new Vector2(Random.Range(-1f, 1f), -0.5f); s.drag = 1.5f; s.life = 0.6f;
            s.startScale = 0.5f; s.endScale = 1.3f; s.c0 = new Color(0.75f, 0.75f, 0.8f, 0.45f); s.c1 = new Color(0.85f, 0.85f, 0.9f, 0f);
        }

        if (transform.position.y > TbShooterGame.HalfH + 1.5f) { Destroy(gameObject); return; }

        var boss = TbBoss.Current;
        if (boss != null && boss.Contains(transform.position, 0.3f)) { Explode(); return; }
        foreach (var e in TbEnemy.All)
        {
            if (e == null || e.Dead) continue;
            float r = e.Radius + 0.4f;
            if ((e.transform.position - transform.position).sqrMagnitude < r * r) { Explode(); return; }
        }
    }

    void Explode()
    {
        var pos = transform.position;
        TbFx.Explosion(pos, 1.8f);
        foreach (var e in new List<TbEnemy>(TbEnemy.All))
            if (e != null && !e.Dead && (e.transform.position - pos).sqrMagnitude < BlastRadius * BlastRadius)
                e.Hit(pos, 6);
        var boss = TbBoss.Current;
        if (boss != null && (boss.transform.position - pos).sqrMagnitude < (BlastRadius + 1.8f) * (BlastRadius + 1.8f))
            boss.Hit(pos, 12);
        Destroy(gameObject);
    }
}
