// TbShooterGame.cs
// ツインビー風シューター本体：起動・画面レイアウト（9:16、上70%ゲーム／下30%コントローラー）・背景・敵の出現・
// 60秒でボス・残機/スコア/HUD・画面揺れ（強/弱/OFF）・ヒットストップ・一時停止・リトライ。
// シーン配線不要：Playを押すと自動で起動する（Day102Bootstrapがあるシーンでは起動しない）。
using UnityEngine;

public sealed class TbShooterGame : MonoBehaviour
{
    public const float HalfW = 4.5f;            // ゲーム画面の左右端（ワールド座標）
    public const float HalfH = 5.6f;            // ゲーム画面の上下端（9:11.2 = 上70%）
    const float FrameAspect = 9f / 16f;
    const float GameShare = 0.7f;               // 画面の上70%がゲーム、下30%がコントローラー
    public const float BossTime = 60f;

    public enum State { Playing, Paused, GameOver, Clear }

    public static TbShooterGame Instance { get; private set; }
    public static TbPlayer Player;
    public static Transform World;              // ゲーム中の物は全部ここにぶら下げる（リトライで丸ごと消す）
    public static int Hits;
    public static int ShakeLevel;               // 0=強 1=弱 2=OFF
    static readonly float[] ShakeMul = { 1f, 0.4f, 0f };

    public State state { get; private set; }
    public Vector3 ShakeOffset { get; private set; }
    public int Score;
    public int Lives;
    public float Elapsed { get; private set; }

    Camera cam, ctrlCam;
    TbControls controls;
    float shake, stopTimer, spawnTimer, waveTimer, endTimer, warnTimer;
    int spawnCount;
    bool bossSpawned;
    TextMesh scoreText, livesText, timerText, centerText, patternText;
    SpriteRenderer bossBarBg, bossBarFill;
    static Sprite square;
    static Font font;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AutoStart()
    {
        if (FindAnyObjectByType<Day102Bootstrap>() != null) return;
        if (FindAnyObjectByType<TbShooterGame>() != null) return;
        new GameObject("TbShooterGame").AddComponent<TbShooterGame>();
    }

    // ---------- 共通の部品づくり ----------
    public static Sprite Square()
    {
        if (square != null) return square;
        var tex = new Texture2D(4, 4);
        var px = new Color[16];
        for (int i = 0; i < 16; i++) px[i] = Color.white;
        tex.SetPixels(px); tex.Apply();
        square = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
        return square;
    }

    public static SpriteRenderer MakeSprite(string name, Sprite sprite, Vector3 pos, int order, Transform parent = null)
    {
        var g = new GameObject(name);
        g.transform.SetParent(parent != null ? parent : World, false);
        g.transform.position = pos;
        var sr = g.AddComponent<SpriteRenderer>();
        sr.sprite = sprite; sr.sortingOrder = order;
        return sr;
    }

    public static TextMesh MakeText(string text, Vector3 pos, float size, Color color, TextAnchor anchor, Transform parent, int order)
    {
        if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        var g = new GameObject("Text");
        g.transform.SetParent(parent != null ? parent : World, false);
        g.transform.position = pos;
        var tm = g.AddComponent<TextMesh>();
        tm.font = font; tm.fontSize = 64; tm.characterSize = size * 0.16f;
        tm.anchor = anchor; tm.alignment = TextAlignment.Center; tm.color = color; tm.text = text;
        var mr = g.GetComponent<MeshRenderer>();
        mr.sharedMaterial = font.material; mr.sortingOrder = order;
        return tm;
    }

    // 飛んでいる物の「影」：右下にずらした暗いシルエット（高さがあるように見える）
    public static void AddShadow(Transform owner, Sprite sprite, float scale)
    {
        var sr = MakeSprite("Shadow", sprite, owner.position, 1, owner);
        sr.transform.localPosition = new Vector3(0.45f, -0.65f, 0f);
        sr.transform.localScale = new Vector3(scale, scale, 1f);
        sr.color = new Color(0.05f, 0.1f, 0.3f, 0.28f);
    }

    // ---------- 起動 ----------
    void Awake()
    {
        Instance = this;
        ShakeLevel = PlayerPrefs.GetInt("TbShake", 0);
        controls = gameObject.AddComponent<TbControls>();
    }

    void Start()
    {
        cam = Camera.main;
        if (cam == null) { cam = new GameObject("Main Camera").AddComponent<Camera>(); cam.tag = "MainCamera"; }
        cam.orthographic = true; cam.orthographicSize = HalfH;
        cam.transform.position = new Vector3(0, 0, -10); cam.transform.rotation = Quaternion.identity;
        cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(0.35f, 0.65f, 0.95f);
        cam.depth = 0;

        var bg = new GameObject("TbLetterboxCamera").AddComponent<Camera>();
        bg.transform.SetParent(transform, false);
        bg.depth = -10; bg.cullingMask = 0; bg.clearFlags = CameraClearFlags.SolidColor; bg.backgroundColor = Color.black;

        ctrlCam = new GameObject("TbControllerCamera").AddComponent<Camera>();
        ctrlCam.transform.SetParent(transform, false);
        ctrlCam.orthographic = true; ctrlCam.orthographicSize = 16f * (1f - GameShare) * 0.5f; // = 2.4
        ctrlCam.transform.position = TbControls.Origin + new Vector3(0, 0, -10);
        ctrlCam.clearFlags = CameraClearFlags.SolidColor; ctrlCam.backgroundColor = new Color(0.1f, 0.05f, 0.06f);
        ctrlCam.depth = 1;
        FitLayout();

        var padRoot = new GameObject("TbControllerRoot").transform;
        padRoot.SetParent(transform, false);
        controls.gameCam = cam; controls.ctrlCam = ctrlCam;
        controls.Build(padRoot);
        controls.SetShakeLabel(ShakeLevel);

        // HUD：カメラの子にして画面揺れの影響を受けない
        var hud = new GameObject("TbHUD").transform;
        hud.SetParent(cam.transform, false);
        hud.localPosition = new Vector3(0, 0, 10);
        scoreText = MakeText("", new Vector3(-HalfW + 0.2f, HalfH - 0.15f, 0), 0.32f, Color.white, TextAnchor.UpperLeft, hud, 100);
        livesText = MakeText("", new Vector3(HalfW - 0.2f, HalfH - 0.15f, 0), 0.32f, Color.white, TextAnchor.UpperRight, hud, 100);
        timerText = MakeText("", new Vector3(0, HalfH - 0.15f, 0), 0.3f, new Color(1f, 1f, 0.7f), TextAnchor.UpperCenter, hud, 100);
        centerText = MakeText("", new Vector3(0, 0.6f, 0), 0.55f, Color.white, TextAnchor.MiddleCenter, hud, 101);
        patternText = MakeText("", new Vector3(0, HalfH - 1.15f, 0), 0.22f, new Color(1f, 0.7f, 0.9f), TextAnchor.UpperCenter, hud, 100);
        foreach (var t in new[] { scoreText, livesText, timerText, centerText, patternText }) t.transform.localPosition = new Vector3(t.transform.position.x, t.transform.position.y, 0);
        bossBarBg = MakeSprite("BossBarBg", TbArt.Bar(), Vector3.zero, 99, hud);
        bossBarBg.transform.localPosition = new Vector3(0, HalfH - 0.85f, 0); bossBarBg.transform.localScale = new Vector3(7f, 1.2f, 1f);
        bossBarBg.color = new Color(0.15f, 0.05f, 0.1f, 0.85f);
        bossBarFill = MakeSprite("BossBarFill", TbArt.Bar(), Vector3.zero, 100, hud);
        bossBarFill.color = new Color(1f, 0.25f, 0.45f);

        NewGame();
    }

    void NewGame()
    {
        if (World != null) Destroy(World.gameObject);
        World = new GameObject("TbWorld").transform;
        Time.timeScale = 1f;
        state = State.Playing;
        Hits = 0; Score = 0; Lives = 3; Elapsed = 0f;
        spawnTimer = 1.2f; waveTimer = 6f; spawnCount = 0; bossSpawned = false; warnTimer = 0f; endTimer = 0f;
        TbBoss.Current = null;

        var sky = MakeSprite("Sky", TbArt.Sky(), Vector3.zero, -20);
        sky.transform.localScale = new Vector3(20f, (HalfH * 2f + 1.5f) / 16f, 1f);
        for (int i = 0; i < 8; i++)
        {
            bool front = i >= 5;
            var c = MakeSprite("Cloud", TbArt.Cloud(), new Vector3(Random.Range(-HalfW, HalfW), Random.Range(-HalfH, HalfH), 0f), front ? -5 : -10);
            float s = front ? Random.Range(0.8f, 1.1f) : Random.Range(0.4f, 0.65f);
            c.transform.localScale = new Vector3(s, s, 1f);
            c.color = new Color(1f, 1f, 1f, front ? 0.75f : 0.5f);
            c.gameObject.AddComponent<TbCloud>().speed = front ? 2.6f : 1.1f;
        }

        var p = MakeSprite("Player", TbArt.Ship().body, new Vector3(0, -HalfH + 1.6f, 0), 35);
        Player = p.gameObject.AddComponent<TbPlayer>();
        Flash("READY!", 1.2f);
    }

    float flashMsgTimer;
    void Flash(string msg, float seconds) { centerText.text = msg; flashMsgTimer = seconds; }

    // ---------- レイアウト ----------
    void FitLayout()
    {
        float sa = (float)Screen.width / Mathf.Max(1, Screen.height);
        Rect frame;
        if (sa > FrameAspect) { float w = FrameAspect / sa; frame = new Rect((1f - w) * 0.5f, 0f, w, 1f); }
        else { float h = sa / FrameAspect; frame = new Rect(0f, (1f - h) * 0.5f, 1f, h); }
        cam.rect = new Rect(frame.x, frame.y + frame.height * (1f - GameShare), frame.width, frame.height * GameShare);
        ctrlCam.rect = new Rect(frame.x, frame.y, frame.width, frame.height * (1f - GameShare));
    }

    public void AddShake(float amount)
    {
        amount *= ShakeMul[ShakeLevel];
        if (amount > 0f) shake = Mathf.Min(0.6f, Mathf.Max(shake, amount));
    }

    // ヒットストップ：一瞬だけ時間をほぼ止めて「重さ」を出す
    public void HitStop(float seconds)
    {
        if (state != State.Playing) return;
        stopTimer = Mathf.Max(stopTimer, seconds); Time.timeScale = 0.05f;
    }

    public void AddScore(int v) { Score += v; }

    public void OnPlayerDead() { state = State.GameOver; endTimer = 0f; centerText.text = "GAME OVER\n<size=36>PRESS START or A</size>"; }

    public void OnBossDefeated() { Score += 10000; state = State.Clear; endTimer = 0f; centerText.text = "STAGE CLEAR!\n<size=36>PRESS START or A</size>"; }

    // ---------- 毎フレーム ----------
    void Update()
    {
        FitLayout();

        if (controls.SelectDown)
        {
            ShakeLevel = (ShakeLevel + 1) % 3;
            PlayerPrefs.SetInt("TbShake", ShakeLevel);
            controls.SetShakeLabel(ShakeLevel);
            Debug.Log("[Tb] SELECT -> shake level " + ShakeLevel); // 連打防止の確認用（1回押し＝1行）
            if (ShakeLevel == 0) AddShake(0.3f);
        }

        if (state == State.Paused)
        {
            Time.timeScale = 0f;
            if (controls.StartDown) { state = State.Playing; Time.timeScale = 1f; centerText.text = ""; }
            return;
        }
        if (state == State.GameOver || state == State.Clear)
        {
            Time.timeScale = 1f;
            endTimer += Time.unscaledDeltaTime;
            if (endTimer > 1f && (controls.StartDown || controls.ADown)) NewGame();
            UpdateHud();
            return;
        }

        if (controls.StartDown) { state = State.Paused; centerText.text = "PAUSE"; return; }

        if (stopTimer > 0f)
        {
            stopTimer -= Time.unscaledDeltaTime;
            if (stopTimer <= 0f) Time.timeScale = 1f;
        }

        Elapsed += Time.deltaTime;
#if UNITY_EDITOR
        if (Input.GetKeyDown(KeyCode.Alpha9) && !bossSpawned && Elapsed < BossTime - 3f) // 確認用：ボス直前へワープ＋無敵
        {
            Elapsed = BossTime - 3f; TbPlayer.God = true;
            Debug.Log("[Tb] Boss test mode: warp + invincible");
        }
#endif
        if (flashMsgTimer > 0f) { flashMsgTimer -= Time.deltaTime; if (flashMsgTimer <= 0f) centerText.text = ""; }

        if (!bossSpawned)
        {
            if (Elapsed < BossTime - 3f) SpawnGrunts();
            else if (warnTimer <= 0f) { warnTimer = 3f; }
            if (warnTimer > 0f)
            {
                warnTimer -= Time.deltaTime;
                centerText.text = Mathf.Repeat(Elapsed * 3f, 1f) > 0.35f ? "WARNING!!\n<size=34>BOSS APPROACHING</size>" : "";
                centerText.color = new Color(1f, 0.3f, 0.35f);
                if (warnTimer <= 0f) { SpawnBoss(); centerText.text = ""; centerText.color = Color.white; }
            }
        }
        UpdateHud();
    }

    void SpawnGrunts()
    {
        spawnTimer -= Time.deltaTime;
        if (spawnTimer <= 0f)
        {
            spawnTimer = 0.8f;
            spawnCount++;
            float x = Random.Range(-HalfW + 0.7f, HalfW - 0.7f);
            SpawnEnemy(spawnCount % 4 == 0 ? TbEnemy.Kind.Dart : TbEnemy.Kind.Drone, new Vector3(x, HalfH + 0.8f, 0));
        }
        waveTimer -= Time.deltaTime;
        if (waveTimer <= 0f)
        {
            waveTimer = 7f;
            for (int i = 0; i < 5; i++)
                SpawnEnemy(TbEnemy.Kind.Drone, new Vector3(-3f + i * 1.5f, HalfH + 0.8f + Mathf.Abs(i - 2) * 0.6f, 0));
        }
    }

    void SpawnEnemy(TbEnemy.Kind kind, Vector3 pos)
    {
        var art = kind == TbEnemy.Kind.Dart ? TbArt.Dart() : TbArt.Drone();
        var sr = MakeSprite("Enemy", art.body, pos, 5);
        sr.gameObject.AddComponent<TbEnemy>().Setup(kind, art);
    }

    void SpawnBoss()
    {
        bossSpawned = true;
        var art = TbArt.Boss();
        var sr = MakeSprite("Boss", art.body, new Vector3(0, HalfH + 2.5f, 0), 4);
        sr.gameObject.AddComponent<TbBoss>().Setup(art);
    }

    void UpdateHud()
    {
        scoreText.text = "SCORE " + Score;
        livesText.text = "SHIP x" + Mathf.Max(0, Lives);
        var boss = TbBoss.Current;
        if (!bossSpawned) timerText.text = "BOSS IN " + Mathf.CeilToInt(Mathf.Max(0f, BossTime - Elapsed));
        else timerText.text = boss != null ? "BOSS" : "";
        bool showBar = boss != null && !boss.Dying;
        bossBarBg.enabled = bossBarFill.enabled = showBar;
        patternText.text = showBar ? boss.PatternName : "";
        if (showBar)
        {
            float w = 6.8f * boss.Hp01;
            bossBarFill.transform.localScale = new Vector3(Mathf.Max(0.001f, w), 0.8f, 1f);
            bossBarFill.transform.localPosition = new Vector3(-3.4f + w * 0.5f, HalfH - 0.85f, 0);
        }
    }

    void LateUpdate()
    {
        shake = Mathf.MoveTowards(shake, 0f, Time.unscaledDeltaTime * 1.6f);
        ShakeOffset = (Vector3)(Random.insideUnitCircle * shake);
        cam.transform.position = new Vector3(0, 0, -10) + ShakeOffset;
    }

    void OnDestroy() { Time.timeScale = 1f; }
}
