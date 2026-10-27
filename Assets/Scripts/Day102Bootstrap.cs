// Day102Bootstrap.cs
// 役割：GameManager/UIManager/PlayerController/ObstacleSpawner/Obstacle/Shard（既存ロジック）を
//       コード生成シーンに接続する。Inspectorでの手動ドラッグ配線は不要（.unityシーンを手編集しない鉄則、CoinGarden.cs/SkySweeper.csと同じ方式）。
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

public sealed class Day102Bootstrap : MonoBehaviour
{
    // RECIPE-002 (Knowledge/RECIPES.md): CreatePrimitiveのColliderはWebGLビルドでIL2CPPに
    // 除去されることがある。2D版でも同じ手口で BoxCollider2D への参照を明示的に保持する。
    static readonly System.Type[] KeepColliders = { typeof(BoxCollider2D) };

    const float BeltTopY = -4f;
    const float SpawnY = 6f;
    const float PlayerY = -2f;
    static readonly float[] Lanes = { -2f, 0f, 2f };

    Font font;

    void Start()
    {
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        MakeWorld();
        var ui = MakeUI();

        var gm = new GameObject("GameManager").AddComponent<GameManager>();
        gm.ui = ui;

        var shardTemplate = MakeShardTemplate();
        var obstacleTemplate = MakeObstacleTemplate(shardTemplate);

        var spawner = new GameObject("ObstacleSpawner").AddComponent<ObstacleSpawner>();
        spawner.obstaclePrefab = obstacleTemplate;
        spawner.lanePositions = Lanes;
    }

    void MakeWorld()
    {
        var cam = new GameObject("Game Camera").AddComponent<Camera>();
        cam.tag = "MainCamera";
        cam.orthographic = true; cam.orthographicSize = 6f;
        cam.transform.position = new Vector3(0, 1f, -10f);
        cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(0.05f, 0.08f, 0.12f);
        cam.gameObject.AddComponent<AudioListener>();

        // ベルトコンベア（視覚のみ。見切れない帯）
        var belt = Sprite2D("Belt", new Vector3(0, BeltTopY - 0.3f, 0), new Vector2(10f, 0.6f), new Color(0.3f, 0.25f, 0.2f));

        // プレイヤー（頭を守って走る）
        var player = new GameObject("Player");
        player.transform.position = new Vector3(0, PlayerY, 0);
        var psr = player.AddComponent<SpriteRenderer>(); psr.sprite = SquareSprite(); psr.color = new Color(0.3f, 0.85f, 1f);
        player.transform.localScale = new Vector3(0.8f, 0.8f, 1f);
        var pcol = player.AddComponent<BoxCollider2D>(); pcol.size = Vector2.one;
        var prb = player.AddComponent<Rigidbody2D>(); prb.bodyType = RigidbodyType2D.Kinematic; prb.gravityScale = 0;
        player.AddComponent<PlayerController>().lanePositions = Lanes;
    }

    GameObject MakeShardTemplate()
    {
        var g = new GameObject("ShardTemplate");
        g.SetActive(false);
        var sr = g.AddComponent<SpriteRenderer>(); sr.sprite = SquareSprite(); sr.color = new Color(0.6f, 0.3f, 0.15f);
        g.transform.localScale = new Vector3(0.25f, 0.25f, 1f);
        var shard = g.AddComponent<Shard>(); shard.beltTopY = BeltTopY;
        return g;
    }

    GameObject MakeObstacleTemplate(GameObject shardTemplate)
    {
        var g = new GameObject("ObstacleTemplate");
        g.SetActive(false);
        g.AddComponent<SpriteRenderer>().sprite = SquareSprite();
        var col = g.AddComponent<BoxCollider2D>(); col.isTrigger = true; col.size = Vector2.one * 0.9f;
        g.transform.localScale = new Vector3(0.9f, 0.9f, 1f);
        var ob = g.AddComponent<Obstacle>();
        ob.beltTopY = BeltTopY;
        ob.shardPrefab = shardTemplate;
        return g;
    }

    UIManager MakeUI()
    {
        var canvasObject = new GameObject("Game UI", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = canvasObject.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasObject.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(540, 960); scaler.matchWidthOrHeight = 0.5f;

        var ui = canvasObject.AddComponent<UIManager>();

        ui.scoreText = Label("ScoreText", canvasObject.transform, "Score: 0", 32);
        Position(ui.scoreText.rectTransform, new Vector2(0, 1), new Vector2(110, -40), new Vector2(200, 60));

        ui.commentArea = new GameObject("CommentArea", typeof(RectTransform)).GetComponent<RectTransform>();
        ui.commentArea.SetParent(canvasObject.transform, false);
        ui.commentArea.anchorMin = Vector2.zero; ui.commentArea.anchorMax = Vector2.one;
        ui.commentArea.offsetMin = ui.commentArea.offsetMax = Vector2.zero;

        ui.commentTemplate = Label("CommentTemplate", ui.commentArea, "", 28);

        ui.gameOverPanel = new GameObject("GameOverPanel", typeof(RectTransform), typeof(Image));
        ui.gameOverPanel.transform.SetParent(canvasObject.transform, false);
        var panelRect = ui.gameOverPanel.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero; panelRect.anchorMax = Vector2.one; panelRect.offsetMin = panelRect.offsetMax = Vector2.zero;
        ui.gameOverPanel.GetComponent<Image>().color = new Color(0.02f, 0.05f, 0.09f, 0.92f);

        ui.finalScoreText = Label("FinalScoreText", ui.gameOverPanel.transform, "Score: 0", 36);
        Position(ui.finalScoreText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, 60), new Vector2(400, 80));

        var retry = Button("もう一度", ui.gameOverPanel.transform);
        Position(retry.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0, -60), new Vector2(240, 80));
        retry.onClick.AddListener(ui.OnRetryButton);

        var events = new GameObject("EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM
        events.AddComponent<InputSystemUIInputModule>();
#else
        events.AddComponent<StandaloneInputModule>();
#endif
        return ui;
    }

    GameObject Sprite2D(string label, Vector3 position, Vector2 scale, Color color)
    {
        var g = new GameObject(label);
        g.transform.position = position; g.transform.localScale = new Vector3(scale.x, scale.y, 1f);
        var sr = g.AddComponent<SpriteRenderer>(); sr.sprite = SquareSprite(); sr.color = color;
        return g;
    }

    static Sprite cachedSquare;
    static Sprite SquareSprite()
    {
        if (cachedSquare != null) return cachedSquare;
        var tex = new Texture2D(2, 2);
        tex.SetPixels(new[] { Color.white, Color.white, Color.white, Color.white });
        tex.Apply();
        cachedSquare = Sprite.Create(tex, new Rect(0, 0, 2, 2), new Vector2(0.5f, 0.5f), 2f);
        return cachedSquare;
    }

    Button Button(string label, Transform parent)
    {
        var g = new GameObject(label, typeof(RectTransform), typeof(Image), typeof(Button));
        g.transform.SetParent(parent, false);
        g.GetComponent<Image>().color = new Color(0.1f, 0.4f, 0.5f, 0.95f);
        var b = g.GetComponent<Button>(); b.targetGraphic = g.GetComponent<Image>();
        var t = Label("Label", g.transform, label, 24); t.raycastTarget = false;
        t.rectTransform.anchorMin = Vector2.zero; t.rectTransform.anchorMax = Vector2.one;
        t.rectTransform.offsetMin = t.rectTransform.offsetMax = Vector2.zero;
        return b;
    }

    Text Label(string name, Transform parent, string value, int size)
    {
        var g = new GameObject(name, typeof(RectTransform), typeof(Text));
        g.transform.SetParent(parent, false);
        var t = g.GetComponent<Text>();
        t.font = font; t.text = value; t.fontSize = size; t.alignment = TextAnchor.MiddleCenter;
        t.color = Color.white; t.raycastTarget = false;
        return t;
    }

    static void Position(RectTransform r, Vector2 anchor, Vector2 pos, Vector2 size)
    {
        r.anchorMin = r.anchorMax = anchor; r.pivot = new Vector2(0.5f, 0.5f);
        r.anchoredPosition = pos; r.sizeDelta = size;
    }
}
