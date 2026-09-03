using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.XR.ARFoundation;
using System.Text;

public class ForensicDiagnostics
{
    [MenuItem("Tools/Run Forensic Diagnostics")]
    public static void RunDiagnostics()
    {
        StringBuilder sb = new StringBuilder();
        Scene scene = EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");

        sb.AppendLine("=== FORENSIC DIAGNOSTICS ===");

        // 1. Check for PlantDetectorPipeline
        var pdp = Object.FindAnyObjectByType<PlantDetectorPipeline>(FindObjectsInactive.Include);
        if (pdp != null)
        {
            sb.AppendLine("Found PlantDetectorPipeline on GameObject: " + pdp.gameObject.name);
            sb.AppendLine(" - GameObject active: " + pdp.gameObject.activeInHierarchy);
            sb.AppendLine(" - Component enabled: " + pdp.enabled);
        }
        else
        {
            sb.AppendLine("PlantDetectorPipeline NOT found in scene.");
        }

        // 2. Check AR_UX_System
        var arUx = GameObject.Find("AR_UX_System");
        if (arUx != null)
        {
            sb.AppendLine("Found AR_UX_System GameObject.");
            sb.AppendLine(" - GameObject active: " + arUx.activeInHierarchy);
            var demoTap = arUx.GetComponent<DemoTapSelection>();
            sb.AppendLine(" - DemoTapSelection enabled: " + (demoTap != null && demoTap.enabled));
        }
        else
        {
            sb.AppendLine("AR_UX_System NOT found.");
        }

        // 3. Check ARRaycastManager
        var arm = Object.FindAnyObjectByType<ARRaycastManager>(FindObjectsInactive.Include);
        if (arm != null)
        {
            sb.AppendLine("Found ARRaycastManager on: " + arm.gameObject.name);
            sb.AppendLine(" - GameObject active: " + arm.gameObject.activeInHierarchy);
            sb.AppendLine(" - Component enabled: " + arm.enabled);
        }
        else
        {
            sb.AppendLine("ARRaycastManager NOT found.");
        }

        // 4. Check ARPlaneManager
        var apm = Object.FindAnyObjectByType<ARPlaneManager>(FindObjectsInactive.Include);
        if (apm != null)
        {
            sb.AppendLine("Found ARPlaneManager on: " + apm.gameObject.name);
            sb.AppendLine(" - Component enabled: " + apm.enabled);
            sb.AppendLine(" - Plane prefab assigned: " + (apm.planePrefab != null));
        }
        else
        {
            sb.AppendLine("ARPlaneManager NOT found.");
        }
        
        Debug.Log(sb.ToString());
    }
}
