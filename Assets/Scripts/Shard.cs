// Shard.cs
// 役割：割れた植木鉢の破片。少しはねてからベルトの上を右へ流れ、画面の右端で消える
// ※ObstacleSpawner・Obstacleとは別の、破片専用の小さなスクリプト（追加ファイル）
using UnityEngine;

public class Shard : MonoBehaviour
{
    public float destroyX = 6f;     // この右端を超えたら消す
    public float beltTopY = -4f;    // ベルトの上面のY座標

    float vx;   // 右向きの速さ（ベルトの流れ＋ランダム）
    float vy;   // 上下の速さ

    void Start()
    {
        vx = 1.5f + Random.Range(0f, 1f);
        vy = Random.Range(2f, 4f);
    }

    void Update()
    {
        // 重力で落ちる
        vy -= 12f * Time.deltaTime;
        transform.position += new Vector3(vx, vy, 0f) * Time.deltaTime;

        // ベルトの上に着地したら止める（右へは流れ続ける）
        if (transform.position.y < beltTopY + 0.15f)
        {
            transform.position = new Vector3(transform.position.x, beltTopY + 0.15f, 0f);
            vy = 0f;
        }

        // 回転させて欠片っぽく見せる
        transform.Rotate(0f, 0f, 200f * Time.deltaTime);

        // 右端を越えたら削除
        if (transform.position.x > destroyX) Destroy(gameObject);
    }
}
