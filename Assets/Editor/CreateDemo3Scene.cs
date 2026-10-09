using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public class CreateDemo3Scene
{
    [MenuItem("Tools/Create Demo 3 - Office Set 1")]
    static void Execute()
    {
        string dstScenePath = "Assets/VNB - Office Set/Scenes/Demo 3 - Office Set 1.unity";

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var floor0 = new GameObject("Floor 0").transform;
        var wsPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/VNB - Office Set/Prefabs/Workplace.prefab");

        var positions = new Vector3[] {
            new Vector3(-6f, 0, -3f),
            new Vector3(0f,  0, -3f),
            new Vector3(6f,  0, -3f),
            new Vector3(-6f, 0, 3f),
            new Vector3(0f,  0, 3f),
            new Vector3(6f,  0, 3f),
        };

        for (int i = 0; i < positions.Length; i++)
        {
            var ws = PrefabUtility.InstantiatePrefab(wsPrefab, floor0) as GameObject;
            ws.transform.localPosition = positions[i];
            ws.transform.localRotation = Quaternion.identity;
            ws.name = $"Workplace ({i + 1})";
        }

        AddPlayerToScene();

        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), dstScenePath);
        Debug.Log("Done: 6 workplaces + player");
    }

    [MenuItem("Tools/Add Player to Current Scene")]
    static void AddPlayerMenuItem()
    {
        AddPlayerToScene();
        Debug.Log("Player added to scene");
    }

    static void AddPlayerToScene()
    {
        var player = new GameObject("Player");
        player.transform.position = new Vector3(0, 1, 0);

        var cc = player.AddComponent<CharacterController>();
        cc.height = 1.8f;
        cc.radius = 0.4f;
        cc.center = new Vector3(0, 0.9f, 0);

        var camGO = new GameObject("Camera");
        camGO.transform.SetParent(player.transform);
        camGO.transform.localPosition = new Vector3(0, 0.7f, 0);

        var cam = camGO.AddComponent<Camera>();
        cam.fieldOfView = 70;
        cam.nearClipPlane = 0.1f;
        cam.farClipPlane = 100;

        var aud = camGO.AddComponent<AudioListener>();

        player.AddComponent<PlayerController>();
    }
}
