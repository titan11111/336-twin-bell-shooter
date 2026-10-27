using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class TitanBuild
{
    const string ScenePath = "Assets/Scenes/Main.unity";
    [MenuItem("Titan/Create Starter Scene")]
    public static void CreateScene()
    {
        if (File.Exists(ScenePath)) { EditorSceneManager.OpenScene(ScenePath); return; }
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        Directory.CreateDirectory("Assets/Scenes");
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        new GameObject("Day102 Bootstrap").AddComponent<Day102Bootstrap>();
        EditorSceneManager.SaveScene(scene, ScenePath);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        AssetDatabase.SaveAssets();
        Debug.Log("Starter scene created. Press Play to test. C# and mobile verification are still required.");
    }
    [MenuItem("Titan/Build Web")]
    public static void BuildWeb()
    {
        if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.WebGL, BuildTarget.WebGL))
            throw new BuildFailedException("Install Web Build Support for this exact Unity version in Unity Hub.");
        if (!File.Exists(ScenePath)) CreateScene();
        if (!File.Exists(ScenePath)) throw new BuildFailedException("Main scene not created.");
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        if (!EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.WebGL, BuildTarget.WebGL))
            throw new BuildFailedException("Could not switch to Web target. Select Web in Build Profiles and try again.");
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
        PlayerSettings.WebGL.decompressionFallback = false;
        PlayerSettings.defaultWebScreenWidth = 540; PlayerSettings.defaultWebScreenHeight = 960;
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
        string day = Directory.GetParent(Application.dataPath).Parent.FullName;
        string output = Path.Combine(day, "Builds", "Web");
        Directory.CreateDirectory(output);
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
            scenes = new[] { ScenePath }, locationPathName = output, target = BuildTarget.WebGL, options = BuildOptions.None
        });
        if (report.summary.result != BuildResult.Succeeded) throw new BuildFailedException("Web build failed; inspect Console.");
        Debug.Log("Web build created: " + output + ". Serve via HTTP and test on a phone; not published yet.");
    }
}
