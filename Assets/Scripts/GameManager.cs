// GameManager.cs
// 役割：ゲーム状態（プレイ中か）・経過時間・スコア・ニャン太キャッチのボーナス・ゲームオーバー・リトライを管理する
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance; // どこからでも呼べるように自分自身を保持

    public UIManager ui;                // Inspectorで割り当てる（Canvas）
    public float catBonus = 100f;       // ニャン太をキャッチした時のボーナス点

    public bool IsPlaying { get; private set; }
    public float Elapsed { get; private set; }  // 経過時間（秒）
    public float Score { get; private set; }    // 距離（スコア）

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        StartGame();
    }

    void Update()
    {
        if (!IsPlaying) return;

        Elapsed += Time.deltaTime;
        Score += Time.deltaTime * 10f; // 時間に比例して距離が増える
        ui.SetScore(Mathf.FloorToInt(Score));
    }

    public void StartGame()
    {
        IsPlaying = true;
        Elapsed = 0f;
        Score = 0f;
    }

    // ニャン太をキャッチした時に呼ばれる
    public void CatCaught()
    {
        if (!IsPlaying) return;
        Score += catBonus;
        ui.SetScore(Mathf.FloorToInt(Score));
        ui.ShowComment("こら！ニャン太！");
    }

    // ゲームオーバー（comment には画面に流すセリフを渡す）
    public void GameOver(string comment)
    {
        if (!IsPlaying) return; // 二重に呼ばれても1回だけ処理
        IsPlaying = false;
        ui.ShowComment(comment);
        ui.ShowGameOver(Mathf.FloorToInt(Score));
    }

    // リトライボタンから呼ばれる：シーンを読み直して最初からやり直す
    public void Retry()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
