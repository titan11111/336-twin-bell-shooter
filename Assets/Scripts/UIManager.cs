// UIManager.cs
// 役割：スコア表示、ゲームオーバー画面、上から流れるコメント（吹き出し）を担当する
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public Text scoreText;              // 画面左上のスコア表示
    public GameObject gameOverPanel;    // ゲームオーバー時に出すパネル
    public Text finalScoreText;         // パネル内の最終スコア表示
    public RectTransform commentArea;   // コメントを流す場所（画面全体に広がる空のRectTransform）
    public Text commentTemplate;        // コメントの元になるText（非表示にしておく）
    public float randomCommentInterval = 3f; // ランダムコメントの間隔（秒）

    static readonly string[] randomComments =
    {
        "危ない！よけて", "おっとっと", "重い、重すぎる", "こら！ニャン太！",
        "今日は風が強いなぁ", "わしの大事な鉢植えが、、、"
    };

    float commentTimer;

    void Start()
    {
        gameOverPanel.SetActive(false);         // 最初は非表示
        commentTemplate.gameObject.SetActive(false);
        commentTimer = randomCommentInterval;
    }

    void Update()
    {
        if (!GameManager.Instance.IsPlaying) return;

        // プレイ中はランダムにコメントを流す
        commentTimer -= Time.deltaTime;
        if (commentTimer <= 0f)
        {
            ShowComment(randomComments[Random.Range(0, randomComments.Length)]);
            commentTimer = randomCommentInterval + Random.Range(0f, 2f);
        }
    }

    public void SetScore(int score)
    {
        scoreText.text = "Score: " + score;
    }

    public void ShowGameOver(int score)
    {
        gameOverPanel.SetActive(true);
        finalScoreText.text = "Score: " + score;
    }

    // 上からコメントを流す（テンプレートを複製して使う）
    public void ShowComment(string message)
    {
        Text t = Instantiate(commentTemplate, commentArea);
        t.gameObject.SetActive(true);
        t.text = message;
        // 横位置はランダム、縦は画面の上端付近から
        t.rectTransform.anchoredPosition = new Vector2(Random.Range(-120f, 120f), -40f);
        StartCoroutine(FloatAndFade(t));
    }

    // コメントを下へゆっくり流し、だんだん消す
    IEnumerator FloatAndFade(Text t)
    {
        RectTransform rt = t.rectTransform;
        Vector2 start = rt.anchoredPosition;
        Color c = t.color;
        float elapsed = 0f;
        const float life = 3f;

        while (elapsed < life)
        {
            elapsed += Time.deltaTime;
            rt.anchoredPosition = start + Vector2.down * (elapsed * 40f);
            c.a = 1f - Mathf.Max(0f, (elapsed - 2f));
            t.color = c;
            yield return null;
        }
        Destroy(t.gameObject);
    }

    // リトライボタンのOnClickから呼ぶ
    public void OnRetryButton()
    {
        GameManager.Instance.Retry();
    }
}
