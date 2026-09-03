using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.XR.ARFoundation;
using Unity.XR.CoreUtils;

public class ConfigureSampleScene
{
    [MenuItem("Tools/Configure Sample Scene For Demo")]
    public static void ConfigureScene()
    {
        Scene scene = EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");

        var xrOrigin = Object.FindAnyObjectByType<XROrigin>();
        ARRaycastManager raycastManager = null;
        if (xrOrigin != null)
        {
            raycastManager = xrOrigin.GetComponent<ARRaycastManager>();
        }
        else
        {
            raycastManager = Object.FindAnyObjectByType<ARRaycastManager>();
        }

        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/ARSpeciesCard.prefab");

        GameObject arUxSys = GameObject.Find("AR_UX_System");
        if (arUxSys == null)
        {
            arUxSys = new GameObject("AR_UX_System");
        }

        var tapSelection = arUxSys.GetComponent<DemoTapSelection>();
        if (tapSelection == null) tapSelection = arUxSys.AddComponent<DemoTapSelection>();

        var uxManager = arUxSys.GetComponent<ARUXManager>();
        if (uxManager == null) uxManager = arUxSys.AddComponent<ARUXManager>();

        tapSelection.uxManager = uxManager;
        uxManager.raycastManager = raycastManager;
        uxManager.arSpeciesCardPrefab = prefab;

        EditorSceneManager.SaveScene(scene);
        Debug.Log("Scene configured and saved successfully.");
    }
}
