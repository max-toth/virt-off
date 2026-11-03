using Game.Scripts.UI.Hud;
using Game.Scripts.UI.Menu;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;

namespace Game.Editor
{
    // Повторный запуск обновляет существующие ассеты.
    public static class GameUiSetup
    {
        private const string MenuTitle = "Setup Game UI";
        private const string UiFolder = "Assets/Game/UI";
        private const string PanelSettingsPath = UiFolder + "/GamePanelSettings.asset";
        private const string ThemePath = UiFolder + "/Theme/GameTheme.tss";
        private const string HudPath = UiFolder + "/Hud/Hud.uxml";
        private const string MenuPath = UiFolder + "/Menu/PauseMenu.uxml";
        private const string PlayerRowPath = UiFolder + "/Menu/PlayerRow.uxml";
        private const string PrefabPath = UiFolder + "/GameUI.prefab";
        private const string PlayerRowField = "playerRowTemplate";
        private const float HudSortingOrder = 0f;
        private const float MenuSortingOrder = 10f;

        private static readonly Vector2Int ReferenceResolution = new(1067, 600);

        [MenuItem("Tools/UI/Setup Game UI")]
        private static void Setup()
        {
            if (!TryLoad(ThemePath, out ThemeStyleSheet theme) ||
                !TryLoad(HudPath, out VisualTreeAsset hud) ||
                !TryLoad(MenuPath, out VisualTreeAsset menu) ||
                !TryLoad(PlayerRowPath, out VisualTreeAsset playerRow))
                return;

            var panelSettings = CreateOrUpdatePanelSettings(theme);
            var prefab = CreateOrUpdatePrefab(panelSettings, hud, menu, playerRow);
            AddToMainScene(prefab);
        }

        private static PanelSettings CreateOrUpdatePanelSettings(ThemeStyleSheet theme)
        {
            var settings = AssetDatabase.LoadAssetAtPath<PanelSettings>(PanelSettingsPath);
            var isNew = settings == null;
            if (isNew) settings = ScriptableObject.CreateInstance<PanelSettings>();

            settings.themeStyleSheet = theme;
            settings.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            settings.referenceResolution = ReferenceResolution;
            settings.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
            settings.match = 1f;

            if (isNew) AssetDatabase.CreateAsset(settings, PanelSettingsPath);
            else EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
            return settings;
        }

        private static GameObject CreateOrUpdatePrefab(PanelSettings panelSettings, VisualTreeAsset hud, VisualTreeAsset menu, VisualTreeAsset playerRow)
        {
            var root = new GameObject("GameUI");
            try
            {
                var hudObject = CreateDocument(root.transform, "Hud", panelSettings, hud, HudSortingOrder);
                hudObject.AddComponent<HudPresenter>();
                hudObject.AddComponent<VoiceDebugPresenter>();

                var menuObject = CreateDocument(root.transform, "PauseMenu", panelSettings, menu, MenuSortingOrder);
                var gameMenu = menuObject.AddComponent<GameMenu>();
                var serialized = new SerializedObject(gameMenu);
                serialized.FindProperty(PlayerRowField).objectReferenceValue = playerRow;
                serialized.ApplyModifiedPropertiesWithoutUndo();

                return PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static GameObject CreateDocument(Transform parent, string name, PanelSettings panelSettings, VisualTreeAsset tree, float sortingOrder)
        {
            var host = new GameObject(name);
            host.transform.SetParent(parent, false);

            var document = host.AddComponent<UIDocument>();
            document.panelSettings = panelSettings;
            document.visualTreeAsset = tree;
            document.sortingOrder = sortingOrder;
            return host;
        }

        private static void AddToMainScene(GameObject prefab)
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            var scene = EditorSceneManager.OpenScene(MainSceneLoader.MainScenePath, OpenSceneMode.Single);
            foreach (var rootObject in scene.GetRootGameObjects())
            {
                if (PrefabUtility.GetCorrespondingObjectFromSource(rootObject) != prefab) continue;
                Debug.Log($"[{MenuTitle}] {prefab.name} уже есть в сцене, префаб обновлён");
                return;
            }

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            Undo.RegisterCreatedObjectUndo(instance, MenuTitle);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[{MenuTitle}] {prefab.name} добавлен в {scene.path}");
        }

        private static bool TryLoad<T>(string path, out T asset) where T : Object
        {
            asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return true;

            Debug.LogError($"[{MenuTitle}] Не найден ассет: {path}");
            return false;
        }
    }
}
