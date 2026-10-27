// ObstacleSpawner.cs
// 役割：植木鉢とニャン太を一定間隔・ランダムなレーンで生成する
//       ニャン太は1/28の確率で出る（キャッチ必須）
//       時間が経つほど生成間隔が短くなり、落下速度も上がる
using UnityEngine;

public class ObstacleSpawner : MonoBehaviour
{
    public GameObject obstaclePrefab;               // 植木鉢・ニャン太の共通Prefab（Obstacleスクリプト付き）
    public float[] lanePositions = { -2f, 0f, 2f }; // PlayerControllerと同じ値にすること
    public float spawnY = 6f;                       // 生成する高さ（画面の上の外）
    public float spawnInterval = 1.5f;              // 最初の生成間隔（秒）
    public float minInterval = 0.5f;                // 一番短い生成間隔（秒）
    public float baseSpeed = 6f;                    // 最初の落下速度
    public float maxSpeed = 14f;                    // 落下速度の上限
    public float catChance = 1f / 28f;              // ニャン太が出る確率（1/28）
    public Color potColor = new Color(0.75f, 0.34f, 0.18f); // 植木鉢の色（素材がない時の代用）
    public Color catColor = new Color(0.95f, 0.6f, 0.1f);   // ニャン太の色（素材がない時の代用）

    float timer = 0f;

    void Update()
    {
        if (!GameManager.Instance.IsPlaying) return;

        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            Spawn();

            // 経過時間に応じて次の生成間隔を短くする
            float interval = spawnInterval - GameManager.Instance.Elapsed * 0.02f;
            timer = Mathf.Max(minInterval, interval);
        }
    }

    void Spawn()
    {
        // ランダムなレーンを選んで生成
        int lane = Random.Range(0, lanePositions.Length);
        Vector3 pos = new Vector3(lanePositions[lane], spawnY, 0f);
        GameObject obj = Instantiate(obstaclePrefab, pos, Quaternion.identity);

        Obstacle ob = obj.GetComponent<Obstacle>();

        // 1/28の確率でニャン太にする
        ob.isCat = Random.value < catChance;

        // 経過時間に応じて落下速度を上げる（上限あり）
        ob.speed = Mathf.Min(maxSpeed, baseSpeed + GameManager.Instance.Elapsed * 0.15f);

        // 見た目の色を変える（Prefabの SpriteRenderer を上書き）
        SpriteRenderer sr = obj.GetComponent<SpriteRenderer>();
        sr.color = ob.isCat ? catColor : potColor;
    }
}
