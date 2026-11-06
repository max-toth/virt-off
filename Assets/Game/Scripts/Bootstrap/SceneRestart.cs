using UnityEngine.SceneManagement;

namespace Game.Scripts.Bootstrap
{
    /// <summary>Перезапуск текущей сцены. Контейнер сцены освобождается: NetManager отправляет DISCONNECT,
    /// и игрок попадает в стартовое меню с чистым состоянием (лобби, чужие игроки, владение предметами).</summary>
    public static class SceneRestart
    {
        public static void Run()
        {
            var scene = SceneManager.GetActiveScene();
#if UNITY_EDITOR
            // В редакторе сцена может быть не добавлена в сборку: грузим её по пути.
            UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode(scene.path,
                new LoadSceneParameters(LoadSceneMode.Single));
#else
            SceneManager.LoadScene(scene.buildIndex);
#endif
        }
    }
}
