// TbParticle.cs
// 爆発の火花・煙・破片など、1つ1つの粒の動き（速度・減速・重力・拡大縮小・色変化）
using UnityEngine;

public sealed class TbParticle : MonoBehaviour
{
    public Vector2 vel;
    public float drag = 2f, gravity = 0f, life = 0.5f, startScale = 1f, endScale = 0f, spin = 0f;
    public Color c0 = Color.white, c1 = new Color(1, 1, 1, 0);
    public bool alignToVel;
    float t;
    SpriteRenderer sr;

    void Awake() { sr = GetComponent<SpriteRenderer>(); }

    void Update()
    {
        t += Time.deltaTime;
        float k = t / life;
        if (k >= 1f) { Destroy(gameObject); return; }
        vel *= Mathf.Max(0f, 1f - drag * Time.deltaTime);
        vel.y -= gravity * Time.deltaTime;
        transform.position += (Vector3)vel * Time.deltaTime;
        float s = Mathf.Lerp(startScale, endScale, k);
        if (alignToVel)
        {
            float ang = Mathf.Atan2(vel.y, vel.x) * Mathf.Rad2Deg - 90f;
            transform.rotation = Quaternion.Euler(0, 0, ang);
            transform.localScale = new Vector3(s, s * (0.4f + vel.magnitude * 0.06f), 1f);
        }
        else
        {
            transform.localScale = new Vector3(s, s, 1f);
            if (spin != 0f) transform.Rotate(0, 0, spin * Time.deltaTime);
        }
        sr.color = Color.Lerp(c0, c1, k);
    }
}
