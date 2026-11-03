using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// При запуске редактора открывает основную сцену проекта.
/// Срабатывает один раз за сессию редактора, дальше можно свободно переключаться между сценами.
/// </summary>
[InitializeOnLoad]
public static class MainSceneLoader
{
    public const string MainScenePath = "Assets/Game/Scenes/Demo 3 - Office Set 1.unity";

    const string SessionKey = "virt-off.MainSceneOpened";

    static MainSceneLoader()
    {
        if (Application.isBatchMode || SessionState.GetBool(SessionKey, false))
            return;

        SessionState.SetBool(SessionKey, true);
        // Открывать сцены прямо в статическом конструкторе нельзя — ждём, пока редактор загрузится.
        EditorApplication.delayCall += OpenMainScene;
    }

    static void OpenMainScene()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        if (EditorSceneManager.GetActiveScene().path == MainScenePath)
            return;

        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(MainScenePath) == null)
        {
            Debug.LogWarning($"[MainSceneLoader] Основная сцена не найдена: {MainScenePath}");
            return;
        }

        EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Single);
    }

    [MenuItem("Tools/Open Main Scene %#m")]
    static void OpenMainSceneMenu()
    {
        if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Single);
    }
}
