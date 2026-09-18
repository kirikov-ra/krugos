using Krugos.Bootstrap;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;

namespace Krugos.Editor
{
    public static class SudokuGameplaySceneSetup
    {
        public const string ScenePath = "Assets/Krugos/Scenes/SudokuGameplay.unity";
        private const string UiPath = "Assets/Krugos/Presentation/UI/";

        [MenuItem("Krugos/Open Sudoku Gameplay")]
        public static void OpenScene()
        {
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                EditorSceneManager.OpenScene(ScenePath);
        }

        // Explicit authoring command, never run automatically on project import.
        [MenuItem("Krugos/Rebuild Gameplay Scene")]
        public static void CreateScene()
        {
            if (!UnityEngine.Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;
            if (!AssetDatabase.IsValidFolder("Assets/Krugos/Scenes"))
                AssetDatabase.CreateFolder("Assets/Krugos", "Scenes");
            var panel = AssetDatabase.LoadAssetAtPath<PanelSettings>(UiPath + "SudokuPanelSettings.asset");
            if (panel == null)
            {
                panel = ScriptableObject.CreateInstance<PanelSettings>();
                AssetDatabase.CreateAsset(panel, UiPath + "SudokuPanelSettings.asset");
            }
            panel.themeStyleSheet = AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>(UiPath + "KrugosRuntimeTheme.tss");
            panel.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            panel.referenceResolution = new Vector2Int(720, 1280);
            panel.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
            panel.match = 0;
            EditorUtility.SetDirty(panel);
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var camera = new GameObject("UI Camera").AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color32(245, 244, 239, 255);
            camera.cullingMask = 0;
            camera.orthographic = true;
            var root = new GameObject("Sudoku Gameplay");
            var document = root.AddComponent<UIDocument>();
            document.panelSettings = panel;
            document.visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UiPath + "SudokuGameplay.uxml");
            root.AddComponent<SudokuGameplayBootstrap>();
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
        }
    }
}
