using UnityEngine;
using UnityEditor;

public class SetupPostExplore
{
    [MenuItem("Team Cero/Setup Post-Explore UI")]
    public static void Setup()
    {
        GameObject arUxSystem = GameObject.Find("AR_UX_System");
        if (arUxSystem == null)
        {
            Debug.LogError("Could not find AR_UX_System in the scene. Please open SampleScene.");
            return;
        }

        if (arUxSystem.GetComponent<PostExploreController>() == null)
        {
            arUxSystem.AddComponent<PostExploreController>();
            Debug.Log("Added PostExploreController to AR_UX_System.");
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(arUxSystem.scene);
        }
        else
        {
            Debug.Log("PostExploreController already exists on AR_UX_System.");
        }
    }
}
