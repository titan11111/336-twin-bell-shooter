// TbBuild.cs
// シューター専用のシーン作成とWebビルド（ランナー用のTitanBuild.csとは別メニュー。既存の設定・シーンは触らない）
// メニュー：Titan > Tb Shooter > Create Scene / Build Web
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class TbBuild
{
    const string ScenePath = "Assets/Scenes/TbShooter.unity";

    [MenuItem("Titan/Tb Shooter/Create Scene")]
    public static void CreateScene()
    {
        Directory.CreateDirectory("Assets/Scenes");
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        new GameObject("TbShooterGame").AddComponent<TbShooterGame>();
        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
        Debug.Log("[TbBuild] Scene saved: " + ScenePath);
    }

    [MenuItem("Titan/Tb Shooter/Build Web")]
    public static void BuildWeb()
    {
        if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.WebGL, BuildTarget.WebGL))
        {
            Debug.LogError("[TbBuild] Web Build Support is not installed for this Unity version (install it in Unity Hub).");
            return;
        }
        if (!File.Exists(ScenePath)) CreateScene();
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled; // GitHub Pagesで確実に動く
        PlayerSettings.WebGL.decompressionFallback = false;
        string day = Directory.GetParent(Application.dataPath).Parent.FullName;
        string output = Path.Combine(day, "Builds", "TbWeb");
        Directory.CreateDirectory(output);
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = new[] { ScenePath },
            locationPathName = output,
            target = BuildTarget.WebGL,
            options = BuildOptions.None
        });
        if (report.summary.result == BuildResult.Succeeded) PatchIndexForMobile(Path.Combine(output, "index.html"));
        File.WriteAllText(Path.Combine(output, "build_result.txt"),
            report.summary.result + " " + report.summary.totalSize + " bytes " + report.summary.totalErrors + " errors " + System.DateTime.Now.ToString("s"));
        Debug.Log("[TbBuild] " + report.summary.result + " -> " + output);
    }

    // スマホ向け：全画面・ピンチ拡大/ダブルタップ拡大/スクロール/長押しメニューを防ぐ（ゲーム側で9:16に収める）
    static void PatchIndexForMobile(string path)
    {
        if (!File.Exists(path)) return;
        string html = File.ReadAllText(path);
        if (html.Contains("tb-mobile-guard")) return;
        const string css = "<style id=\"tb-mobile-guard\">html,body{margin:0;padding:0;width:100%;height:100%;overflow:hidden;background:#000;" +
            "touch-action:none;overscroll-behavior:none;-webkit-user-select:none;user-select:none;-webkit-touch-callout:none;-webkit-tap-highlight-color:transparent}" +
            "#unity-container{position:fixed!important;left:0!important;top:0!important;width:100%!important;height:100%!important;transform:none!important}" +
            "#unity-canvas{width:100%!important;height:100%!important;touch-action:none}#unity-footer{display:none!important}</style>";
        const string js = "<script>(function(){var p=function(e){e.preventDefault();};" +
            "document.addEventListener('gesturestart',p,{passive:false});document.addEventListener('dblclick',p,{passive:false});" +
            "document.addEventListener('touchmove',p,{passive:false});document.addEventListener('contextmenu',p);" +
            "var last=0;document.addEventListener('touchend',function(e){var n=Date.now();if(n-last<350)e.preventDefault();last=n;},{passive:false});})();</script>";
        html = html.Replace("</head>", css + "\n</head>").Replace("</body>", js + "\n</body>");
        html = System.Text.RegularExpressions.Regex.Replace(html, "<meta name=\"viewport\"[^>]*>",
            "<meta name=\"viewport\" content=\"width=device-width, height=device-height, initial-scale=1.0, maximum-scale=1.0, user-scalable=no, viewport-fit=cover\">");
        File.WriteAllText(path, html);
    }
}
