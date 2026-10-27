// TbEnemyBullet.cs
// 敵弾：マゼンタ＋白芯で、爆発や雲に埋もれないよう一番手前に描く。自機の小さな当たり判定で判定。
using UnityEngine;

public sealed class TbEnemyBullet : MonoBehaviour
{
    public Vector2 vel;
    const float Radius = 0.13f;

    public static void Fire(Vector3 pos, Vector2 vel)
    {
        var sr = TbShooterGame.MakeSprite("EnemyBullet", TbArt.EnemyBullet(), pos, 40);
        sr.transform.localScale = new Vector3(1.1f, 1.1f, 1f);
        sr.gameObject.AddComponent<TbEnemyBullet>().vel = vel;
    }

    public static void Aimed(Vector3 from, float speed, float spreadDeg = 0f)
    {
        var pl = TbShooterGame.Player;
        Vector2 dir = pl != null && pl.Alive ? (Vector2)(pl.transform.position - from).normalized : Vector2.down;
        Fire(from, Rotate(dir, spreadDeg) * speed);
    }

    public static Vector2 Rotate(Vector2 v, float deg)
    {
        float r = deg * Mathf.Deg2Rad, c = Mathf.Cos(r), s = Mathf.Sin(r);
        return new Vector2(v.x * c - v.y * s, v.x * s + v.y * c);
    }

    void Update()
    {
        transform.position += (Vector3)(vel * Time.deltaTime);
        var p = transform.position;
        if (Mathf.Abs(p.x) > TbShooterGame.HalfW + 1f || Mathf.Abs(p.y) > TbShooterGame.HalfH + 1f) { Destroy(gameObject); return; }
        var pl = TbShooterGame.Player;
        if (pl != null && pl.Hits(p, Radius)) { pl.Damage(); Destroy(gameObject); }
    }
}
