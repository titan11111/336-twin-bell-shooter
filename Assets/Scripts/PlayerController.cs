// PlayerController.cs
// 役割：プレイヤー（頭を守って走るランナー）のレーン移動を担当する（左・中・右の3レーン）
//       キーボード（←→）と画面タップ（左半分＝左、右半分＝右）で移動する
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public float[] lanePositions = { -2f, 0f, 2f }; // 左・中・右のX座標（ObstacleSpawnerと同じ値にすること）
    public float moveSpeed = 15f;                   // レーン移動の速さ
    int currentLane = 1;                            // 0=左, 1=中, 2=右

    void Update()
    {
        // ゲーム中じゃないときは操作を受け付けない
        if (GameManager.Instance == null || !GameManager.Instance.IsPlaying) return;

        // キーボード操作（左右キー）
        if (Input.GetKeyDown(KeyCode.LeftArrow) && currentLane > 0) currentLane--;
        if (Input.GetKeyDown(KeyCode.RightArrow) && currentLane < lanePositions.Length - 1) currentLane++;

        // タップ操作（画面の左半分＝左へ、右半分＝右へ）
        if (Input.GetMouseButtonDown(0))
        {
            if (Input.mousePosition.x < Screen.width / 2f)
            {
                if (currentLane > 0) currentLane--;
            }
            else
            {
                if (currentLane < lanePositions.Length - 1) currentLane++;
            }
        }

        // 目標のレーンへ少しずつ移動させる（スーッと滑る動き）
        Vector3 target = new Vector3(lanePositions[currentLane], transform.position.y, transform.position.z);
        transform.position = Vector3.MoveTowards(transform.position, target, moveSpeed * Time.deltaTime);
    }
}
