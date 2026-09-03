using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.XR.ARFoundation;
using System.Text;

public class PresentationDiagnostics
{
    [MenuItem("Tools/Run Presentation Diagnostics")]
    public static void RunDiagnostics()
    {
        StringBuilder sb = new StringBuilder();
        Scene scene = EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");

        sb.AppendLine("=== PRESENTATION DIAGNOSTICS ===");

        // 1. AR Visualizations
        var pcManager = Object.FindAnyObjectByType<ARPointCloudManager>(FindObjectsInactive.Include);
        if (pcManager != null)
        {
            sb.AppendLine("ARPointCloudManager found on: " + pcManager.gameObject.name);
            sb.AppendLine("- Enabled: " + pcManager.enabled);
            sb.AppendLine("- Prefab: " + (pcManager.pointCloudPrefab != null ? pcManager.pointCloudPrefab.name : "None"));
        }

        var planeManager = Object.FindAnyObjectByType<ARPlaneManager>(FindObjectsInactive.Include);
        if (planeManager != null)
        {
            sb.AppendLine("ARPlaneManager found on: " + planeManager.gameObject.name);
            sb.AppendLine("- Enabled: " + planeManager.enabled);
            sb.AppendLine("- Prefab: " + (planeManager.planePrefab != null ? planeManager.planePrefab.name : "None"));
        }

        // 2. Perception Observers / State Machine
        var pso = Object.FindAnyObjectByType<PerceptionStateObserver>(FindObjectsInactive.Include);
        if (pso != null)
        {
            sb.AppendLine("PerceptionStateObserver found on: " + pso.gameObject.name);
            sb.AppendLine("- Enabled: " + pso.enabled);
        }

        var fakeDetector = Object.FindAnyObjectByType<FakeDetector>(FindObjectsInactive.Include);
        if (fakeDetector != null)
        {
            sb.AppendLine("FakeDetector found on: " + fakeDetector.gameObject.name);
            sb.AppendLine("- Enabled: " + fakeDetector.enabled);
        }
        
        Debug.Log(sb.ToString());
    }
}
