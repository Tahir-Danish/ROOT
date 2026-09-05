using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class BuildPlayerCLI
{
    public static void BuildAndroid()
    {
        string[] scenes = new string[] { "Assets/Scenes/SampleScene.unity" };
        string outputPath = "D:/Unity/Projects/Team_Cero/Builds/TeamCero_Demo_Lock.apk";

        BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
        buildPlayerOptions.scenes = scenes;
        buildPlayerOptions.locationPathName = outputPath;
        buildPlayerOptions.target = BuildTarget.Android;
        buildPlayerOptions.options = BuildOptions.None;

        Debug.Log($"[BuildPlayerCLI] Starting Android build to {outputPath}...");
        BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
        BuildSummary summary = report.summary;

        if (summary.result == BuildResult.Succeeded)
        {
            Debug.Log($"[BuildPlayerCLI] Build SUCCEEDED: {summary.totalSize} bytes in {summary.totalTime.TotalSeconds:F1}s");
        }
        else if (summary.result == BuildResult.Failed)
        {
            Debug.LogError($"[BuildPlayerCLI] Build FAILED with {summary.totalErrors} errors.");
            EditorApplication.Exit(1);
        }
    }
}
