// Obstacle.cs
// 役割：植木鉢／ニャン太の落下と当たり判定を担当する
//       ・植木鉢：プレイヤーにぶつかったらゲームオーバー／ベルトに落ちたら割れて右へ流れる
//       ・ニャン太：プレイヤーがキャッチしたらボーナス／キャッチできずベルトに届いたらゲームオーバー
using UnityEngine;

public class Obstacle : MonoBehaviour
{
    public bool isCat = false;      // trueならニャン太（ObstacleSpawnerが設定）
    public float speed = 6f;        // 落下速度（ObstacleSpawnerが設定）
    public float beltTopY = -4f;    // ベルトの上面のY座標（シーンのBeltに合わせる）
    public GameObject shardPrefab;  // 割れた時の破片Prefab（Shardスクリプト付き）

    Transform player;
    bool warned = false;            // 「危ない！よけて」を一度だけ出すためのフラグ
    bool finished = false;          // 割れた・キャッチされた後は二重に処理しない

    void Start()
    {
        player = FindObjectOfType<PlayerController>().transform;
    }

    void Update()
    {
        if (!GameManager.Instance.IsPlaying) return;
        if (finished) return;

        // 下方向へ移動
        transform.Translate(Vector3.down * speed * Time.deltaTime, Space.World);

        // 「危ない！よけて」：同じレーンで真上に迫ったとき（植木鉢のみ）
        if (!warned && !isCat
            && Mathf.Abs(transform.position.x - player.position.x) < 0.5f
            && transform.position.y > player.position.y && transform.position.y - player.position.y < 3f)
        {
            warned = true;
            GameManager.Instance.ui.ShowComment("危ない！よけて");
        }

        // ベルトに届いたら（オブジェクトの中心がベルトの上面＋半分に来た時）
        if (transform.position.y <= beltTopY + 0.5f)
        {
            finished = true;
            if (isCat)
            {
                // ニャン太を取りこぼした → 即ゲームオーバー
                GameManager.Instance.GameOver("うわーうっかりしてた");
                Destroy(gameObject);
            }
            else
            {
                // 植木鉢はベルトの上で割れて、破片が右へ流れる
                Shatter();
            }
        }
    }

    // プレイヤーとの当たり判定（Is Triggerのコライダー同士）
    void OnTriggerEnter2D(Collider2D other)
    {
        if (finished) return;
        if (other.GetComponent<PlayerController>() == null) return;

        finished = true;
        if (isCat)
        {
            // ニャン太をキャッチ！ボーナス
            GameManager.Instance.CatCaught();
            Destroy(gameObject);
        }
        else
        {
            // 植木鉢にぶつかった → ゲームオーバー
            GameManager.Instance.GameOver("重い、重すぎる");
            Shatter();
        }
    }

    // 植木鉢が割れる演出：コメントを出して破片を散らす
    void Shatter()
    {
        if (!isCat)
        {
            string message = Random.value < 0.6f ? "わしの大事な鉢植えが、、、" : "今日は風が強いなぁ";
            GameManager.Instance.ui.ShowComment(message);
        }

        if (shardPrefab != null)
        {
            for (int i = 0; i < 7; i++)
            {
                Vector3 p = transform.position + new Vector3(Random.Range(-0.3f, 0.3f), 0.3f, 0f);
                Instantiate(shardPrefab, p, Quaternion.identity);
            }
        }
        Destroy(gameObject);
    }
}
