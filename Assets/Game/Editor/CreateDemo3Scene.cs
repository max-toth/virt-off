using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Game.Editor
{
    // Генерирует основную сцену: этаж с шестью рабочими местами и игрок из префаба.
    public static class CreateDemo3Scene
    {
        private const string MenuTitle = "Create Demo 3";
        private const string FloorName = "Floor 0";
        private const string WorkplacePrefabPath = "Assets/Game/Prefabs/Workplace.prefab";
        private const string PlayerPrefabPath = "Assets/Game/Prefabs/Player.prefab";

        private static readonly Vector3 PlayerSpawn = new(0f, 1f, 0f);

        private static readonly Vector3[] WorkplacePositions =
        {
            new(-6f, 0f, -3f),
            new(0f, 0f, -3f),
            new(6f, 0f, -3f),
            new(-6f, 0f, 3f),
            new(0f, 0f, 3f),
            new(6f, 0f, 3f)
        };

        [MenuItem("Tools/Create Demo 3 - Office Set 1")]
        private static void Execute()
        {
            if (!TryLoadPrefab(WorkplacePrefabPath, out var workplace) || !TryLoadPrefab(PlayerPrefabPath, out var player)) return;
            if (!ConfirmOverwrite() || !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var floor = new GameObject(FloorName).transform;
            for (var i = 0; i < WorkplacePositions.Length; i++)
            {
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(workplace, floor);
                instance.transform.SetLocalPositionAndRotation(WorkplacePositions[i], Quaternion.identity);
                instance.name = $"Workplace ({i + 1})";
            }

            InstantiatePlayer(player);

            EditorSceneManager.SaveScene(scene, MainSceneLoader.MainScenePath);
            Debug.Log($"[{MenuTitle}] {WorkplacePositions.Length} workplaces + player → {MainSceneLoader.MainScenePath}");
        }

        [MenuItem("Tools/Add Player to Current Scene")]
        private static void AddPlayerToCurrentScene()
        {
            if (!TryLoadPrefab(PlayerPrefabPath, out var player)) return;

            var instance = InstantiatePlayer(player);
            Undo.RegisterCreatedObjectUndo(instance, "Add Player");
            Selection.activeGameObject = instance;
            EditorSceneManager.MarkSceneDirty(instance.scene);
        }

        private static GameObject InstantiatePlayer(GameObject prefab)
        {
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.transform.position = PlayerSpawn;
            return instance;
        }

        private static bool ConfirmOverwrite()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(MainSceneLoader.MainScenePath) == null) return true;

            return EditorUtility.DisplayDialog(MenuTitle,
                $"Сцена {MainSceneLoader.MainScenePath} уже существует и будет перезаписана заново сгенерированной. Продолжить?",
                "Перезаписать", "Отмена");
        }

        private static bool TryLoadPrefab(string path, out GameObject prefab)
        {
            prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab != null) return true;

            Debug.LogError($"[{MenuTitle}] Префаб не найден: {path}");
            return false;
        }
    }
}
