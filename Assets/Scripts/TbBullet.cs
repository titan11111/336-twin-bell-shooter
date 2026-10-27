// TbBullet.cs
// 自機の弾：光る弾がまっすぐ上へ。軌跡を残し、敵やボスに当たったらノックバック＋火花。
using UnityEngine;

public sealed class TbBullet : MonoBehaviour
{
    public float speed = 18f;
    float trailTimer;

    void Update()
    {
        transform.position += Vector3.up * speed * Time.deltaTime;
        if (transform.position.y > TbShooterGame.HalfH + 1f) { Destroy(gameObject); return; }

        trailTimer -= Time.deltaTime;
        if (trailTimer <= 0f) { trailTimer = 0.03f; TbFx.Trail(transform.position - Vector3.up * 0.3f); }

        var boss = TbBoss.Current;
        if (boss != null && boss.Contains(transform.position, 0.1f))
        {
            boss.Hit(transform.position, 1);
            Destroy(gameObject);
            return;
        }
        foreach (var e in TbEnemy.All)
        {
            if (e == null || e.Dead) continue;
            float r = e.Radius + 0.12f;
            if ((e.transform.position - transform.position).sqrMagnitude < r * r)
            {
                TbShooterGame.Hits++;
                e.Hit(transform.position, 1);
                Destroy(gameObject);
                return;
            }
        }
    }
}
