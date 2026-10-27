// TbControls.cs
// 画面下30%のオリジナル・コントローラー＋全入力のまとめ役。
// ・十字キー：8方向移動　・A：連射（長押しでため）　・B：ため専用　・SELECT：画面揺れ 強/弱/OFF　・START：一時停止/リトライ
// ・ゲーム画面（上70%）をなぞる：指の移動量だけ自機が動き、触れている間は自動連射
// 入力は毎フレーム「今触れている指」だけから作り直すので、指を離す・画面外へ出る・アプリ切替で入力が残らない。
using System.Collections.Generic;
using UnityEngine;

public sealed class TbControls : MonoBehaviour
{
    public static TbControls I { get; private set; }
    public static readonly Vector3 Origin = new Vector3(100f, 0f, 0f); // コントローラーはゲーム世界の外に置き、専用カメラで映す

    public Camera gameCam, ctrlCam;

    // 毎フレームの入力結果
    public Vector2 Move { get; private set; }
    public Vector3 DragDelta { get; private set; }
    public bool AHeld { get; private set; }
    public bool BHeld { get; private set; }
    public bool GameTouchHeld { get; private set; }
    public bool ADown { get; private set; }
    public bool SelectDown { get; private set; }
    public bool StartDown { get; private set; }
    public int ResetCount { get; private set; } // アプリ切替などで入力を強制リセットした回数

    // 9 x 4.8 の操作エリア。十字キー・A・Bを大きく、SELECT/STARTは上段に離して配置（重なりなし）
    static readonly Vector2 DpadC = new Vector2(-2.55f, -0.35f);
    static readonly Vector2 BC = new Vector2(1.5f, -1.0f), AC = new Vector2(3.3f, -0.1f);
    static readonly Vector2 SelC = new Vector2(-0.85f, 1.75f), StaC = new Vector2(0.85f, 1.75f);
    const float DpadR = 2.0f, BtnReach = 1.3f, SmallHalfW = 0.75f, SmallHalfH = 0.4f;
    const float BtnScale = 1.25f;
    const float MenuCooldown = 0.4f;   // SELECT/STARTの連打・ダブルタップ防止
    

    readonly Dictionary<int, Vector3> lastDrag = new Dictionary<int, Vector3>();
    readonly HashSet<int> seen = new HashSet<int>();
    readonly List<int> stale = new List<int>();
    Vector2 dpadVec; bool aT, bT, selT, staT, prevSel, prevSta, prevA, prevB;
    float menuLock;

    Transform dpadT, aT_, bT_, selT_, staT_;
    SpriteRenderer dpadHi, gaugeFill, aSr, bSr;
    TextMesh shakeText, chargeText;
    static readonly string[] ShakeNames = { "SHAKE: STRONG", "SHAKE: WEAK", "SHAKE: OFF" };

    void Awake() { I = this; Input.simulateMouseWithTouches = false; Input.multiTouchEnabled = true; }

    public void Build(Transform root)
    {
        var o = Origin;
        TbShooterGame.MakeSprite("PadBody", TbArt.ControllerBody(), o, 200, root);

        var dbase = TbShooterGame.MakeSprite("DPadBase", TbArt.DPadBase(), o + (Vector3)DpadC, 202, root);
        dbase.transform.localScale = Vector3.one * 0.92f;
        dpadT = TbShooterGame.MakeSprite("DPad", TbArt.DPad(), o + (Vector3)DpadC, 205, root).transform;
        dpadHi = TbShooterGame.MakeSprite("DPadHi", TbArt.Soft(), o + (Vector3)DpadC, 206, root);
        dpadHi.color = new Color(0.4f, 1f, 1f, 0.6f); dpadHi.transform.localScale = Vector3.one * 1.1f; dpadHi.enabled = false;

        bSr = TbShooterGame.MakeSprite("BtnB", TbArt.ButtonB(), o + (Vector3)BC, 205, root); bT_ = bSr.transform;
        aSr = TbShooterGame.MakeSprite("BtnA", TbArt.ButtonA(), o + (Vector3)AC, 205, root); aT_ = aSr.transform;
        bT_.localScale = aT_.localScale = Vector3.one * BtnScale;
        selT_ = TbShooterGame.MakeSprite("BtnSelect", TbArt.SmallButton(), o + (Vector3)SelC, 205, root).transform;
        staT_ = TbShooterGame.MakeSprite("BtnStart", TbArt.SmallButton(), o + (Vector3)StaC, 205, root).transform;

        var light = new Color(0.92f, 0.95f, 1f);
        var dim = new Color(0.6f, 0.75f, 0.85f);
        TbShooterGame.MakeText("B", o + (Vector3)BC, 0.5f, light, TextAnchor.MiddleCenter, root, 210);
        TbShooterGame.MakeText("A", o + (Vector3)AC, 0.5f, light, TextAnchor.MiddleCenter, root, 210);
        TbShooterGame.MakeText("SELECT", o + (Vector3)SelC, 0.2f, light, TextAnchor.MiddleCenter, root, 210);
        TbShooterGame.MakeText("START", o + (Vector3)StaC, 0.2f, light, TextAnchor.MiddleCenter, root, 210);
        shakeText = TbShooterGame.MakeText(ShakeNames[0], o + new Vector3(-3.4f, 2.0f, 0), 0.17f, dim, TextAnchor.MiddleLeft, root, 210);

        // ためゲージ（中央の空きスペース）
        var gbg = TbShooterGame.MakeSprite("GaugeBg", TbArt.Bar(), o + new Vector3(0, 0.95f, 0), 203, root);
        gbg.color = new Color(0.08f, 0.1f, 0.16f); gbg.transform.localScale = new Vector3(3.2f, 1.1f, 1f);
        gaugeFill = TbShooterGame.MakeSprite("GaugeFill", TbArt.Bar(), o + new Vector3(-1.5f, 0.95f, 0), 204, root);
        chargeText = TbShooterGame.MakeText("HOLD A/B: CHARGE", o + new Vector3(0, 0.55f, 0), 0.16f, dim, TextAnchor.MiddleCenter, root, 210);
        SetCharge(0f, false);
    }

    public void SetCharge(float k01, bool full)
    {
        if (gaugeFill == null) return;
        float w = 3f * Mathf.Clamp01(k01);
        gaugeFill.transform.localScale = new Vector3(Mathf.Max(0.001f, w), 0.8f, 1f);
        gaugeFill.transform.position = Origin + new Vector3(-1.5f + w * 0.5f, 0.95f, 0);
        bool blink = Mathf.Repeat(Time.unscaledTime * 8f, 1f) > 0.5f;
        gaugeFill.color = full ? (blink ? Color.white : new Color(0.3f, 0.95f, 1f)) : Color.Lerp(new Color(1f, 0.8f, 0.2f), new Color(1f, 0.4f, 0.1f), k01);
        chargeText.text = full ? "RELEASE: BAZOOKA!" : "HOLD A/B: CHARGE";
    }

    public void SetShakeLabel(int level) { if (shakeText != null) shakeText.text = ShakeNames[Mathf.Clamp(level, 0, 2)]; }

    // アプリ切替・タブ非表示・フォーカス喪失：入力を全部捨てる
    void OnApplicationFocus(bool focus) { if (!focus) ForceRelease(); }
    void OnApplicationPause(bool pause) { if (pause) ForceRelease(); }
    void ForceRelease()
    {
        lastDrag.Clear();
        Move = Vector2.zero; DragDelta = Vector3.zero;
        AHeld = BHeld = GameTouchHeld = ADown = SelectDown = StartDown = false;
        ResetCount++;
    }

    void Update()
    {
        dpadVec = Vector2.zero; aT = bT = selT = staT = false;
        bool gameHeld = false;
        Vector3 drag = Vector3.zero;
        seen.Clear();

        if (Input.touchCount > 0)
        {
            for (int i = 0; i < Input.touchCount; i++)
            {
                var t = Input.GetTouch(i);
                if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled) continue;
                Handle(t.fingerId, t.position, t.phase == TouchPhase.Began, ref gameHeld, ref drag);
            }
        }
        else if (Input.GetMouseButton(0))
        {
            Handle(-1, Input.mousePosition, Input.GetMouseButtonDown(0), ref gameHeld, ref drag);
        }

        // もう触れていない指のドラッグ記録を消す
        stale.Clear();
        foreach (var k in lastDrag.Keys) if (!seen.Contains(k)) stale.Add(k);
        foreach (var k in stale) lastDrag.Remove(k);

        // キーボード
        var mv = dpadVec;
        if (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A)) mv.x -= 1f;
        if (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D)) mv.x += 1f;
        if (Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.S)) mv.y -= 1f;
        if (Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.W)) mv.y += 1f;
        Move = Vector2.ClampMagnitude(mv, 1f);

        bool a = aT || Input.GetKey(KeyCode.Z) || Input.GetKey(KeyCode.Space) || Input.GetKey(KeyCode.J);
        AHeld = a;
        BHeld = bT || Input.GetKey(KeyCode.X) || Input.GetKey(KeyCode.K);
        GameTouchHeld = gameHeld;
        DragDelta = drag;
        ADown = a && !prevA; prevA = a;
        menuLock -= Time.unscaledDeltaTime;
        bool sel = (selT && !prevSel) || Input.GetKeyDown(KeyCode.C); prevSel = selT;
        bool sta = (staT && !prevSta) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Escape); prevSta = staT;
        SelectDown = StartDown = false;
        if ((sel || sta) && menuLock <= 0f) { SelectDown = sel; StartDown = sta && !sel; menuLock = MenuCooldown; }
        bool bNow = BHeld;
        if (ADown) Ping(AC);
        if (bNow && !prevB) Ping(BC);
        prevB = bNow;

        UpdateVisuals();
    }

    void Handle(int id, Vector2 sp, bool began, ref bool gameHeld, ref Vector3 drag)
    {
        seen.Add(id);
        if (ctrlCam != null && ctrlCam.pixelRect.Contains(sp))
        {
            lastDrag.Remove(id);
            var w = (Vector2)(ctrlCam.ScreenToWorldPoint(sp) - Origin);
            var v = w - DpadC;
            if (v.magnitude < DpadR)
            {
                if (v.magnitude > 0.22f)
                {
                    float q = Mathf.Round(Mathf.Atan2(v.y, v.x) / (Mathf.PI / 4f)) * (Mathf.PI / 4f);
                    dpadVec = new Vector2(Mathf.Cos(q), Mathf.Sin(q));
                }
            }
            else if (Mathf.Abs(w.x - SelC.x) < SmallHalfW && Mathf.Abs(w.y - SelC.y) < SmallHalfH) selT = true;
            else if (Mathf.Abs(w.x - StaC.x) < SmallHalfW && Mathf.Abs(w.y - StaC.y) < SmallHalfH) staT = true;
            else
            {
                // A/Bは「近いほう」を押したことにする（大きく押しやすい。2つを同時に押すことはない）
                float da = (w - AC).magnitude, db = (w - BC).magnitude;
                if (Mathf.Min(da, db) < BtnReach) { if (da <= db) aT = true; else bT = true; }
            }
        }
        else if (gameCam != null && gameCam.pixelRect.Contains(sp))
        {
            gameHeld = true;
            var shake = TbShooterGame.Instance != null ? TbShooterGame.Instance.ShakeOffset : Vector3.zero;
            var w = gameCam.ScreenToWorldPoint(sp) - shake; w.z = 0f;
            if (!began && lastDrag.TryGetValue(id, out var last)) drag += w - last;
            lastDrag[id] = w;
        }
        else lastDrag.Remove(id);
    }

    void UpdateVisuals()
    {
        if (dpadT == null) return;
        dpadHi.enabled = dpadVec != Vector2.zero;
        if (dpadHi.enabled) dpadHi.transform.position = Origin + (Vector3)(DpadC + dpadVec * 0.78f);
        dpadT.localScale = dpadVec != Vector2.zero ? new Vector3(0.96f, 0.96f, 1f) : Vector3.one;
        dpadT.localRotation = Quaternion.Euler(dpadVec.y * -6f, dpadVec.x * 6f, 0f); // 押した方向に少し傾く
        Press(aT_, aSr, AHeld); Press(bT_, bSr, BHeld);
        selT_.localScale = selT ? new Vector3(0.92f, 0.85f, 1f) : Vector3.one;
        staT_.localScale = staT ? new Vector3(0.92f, 0.85f, 1f) : Vector3.one;
    }

    static void Press(Transform t, SpriteRenderer sr, bool on)
    {
        float target = on ? BtnScale * 0.88f : BtnScale;
        float s = Mathf.MoveTowards(t.localScale.x, target, Time.unscaledDeltaTime * 6f); // すっと沈んで戻る
        t.localScale = new Vector3(s, s, 1f);
        sr.color = on ? new Color(0.72f, 0.72f, 0.72f) : Color.white;
    }

    // 押した瞬間の波紋（打感）
    void Ping(Vector2 at)
    {
        var g = new GameObject("Ping");
        g.transform.SetParent(transform, false);
        g.transform.position = Origin + (Vector3)at;
        var sr = g.AddComponent<SpriteRenderer>(); sr.sprite = TbArt.Ring(); sr.sortingOrder = 207;
        var p = g.AddComponent<TbParticle>();
        p.life = 0.18f; p.startScale = 1.4f; p.endScale = 2.3f; p.drag = 0f;
        p.c0 = new Color(1f, 1f, 1f, 0.8f); p.c1 = new Color(1f, 1f, 1f, 0f);
    }
}
