using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Editor
{
    // Один раз за сессию редактора открывает основную сцену.
    [InitializeOnLoad]
    public static class MainSceneLoader
    {
        public const string MainScenePath = "Assets/Game/Scenes/Demo 3 - Office Set 1.unity";

        private const string SessionKey = "virt-off.MainSceneOpened";

        static MainSceneLoader()
        {
            if (Application.isBatchMode || SessionState.GetBool(SessionKey, false)) return;

            SessionState.SetBool(SessionKey, true);
            // В статическом конструкторе сцены открывать нельзя — ждём загрузки редактора.
            EditorApplication.delayCall += OpenMainScene;
        }

        [MenuItem("Tools/Open Main Scene %#m")]
        private static void OpenMainSceneMenu()
        {
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Single);
        }

        private static void OpenMainScene()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (SceneManager.GetActiveScene().path == MainScenePath) return;

            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(MainScenePath) == null)
            {
                Debug.LogWarning($"[MainSceneLoader] Основная сцена не найдена: {MainScenePath}");
                return;
            }

            EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Single);
        }
    }
}
