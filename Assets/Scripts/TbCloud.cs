// TbCloud.cs
// 雲を下へ流して、上下ループさせる（奥の雲は遅く、手前は速い＝奥行き）
using UnityEngine;

public sealed class TbCloud : MonoBehaviour
{
    public float speed = 2f;

    void Update()
    {
        var p = transform.position;
        p.y -= speed * Time.deltaTime;
        if (p.y < -TbShooterGame.HalfH - 2.5f)
        {
            p.y = TbShooterGame.HalfH + 2.5f;
            p.x = Random.Range(-TbShooterGame.HalfW, TbShooterGame.HalfW);
        }
        transform.position = p;
    }
}
