using BeyProject.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BeyProject.EditorTools
{
    public static partial class ProjectBootstrapper
    {
        private static void BuildMainMenuScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var camGO = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            camGO.tag = "MainCamera";
            Camera cam = camGO.GetComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 5f;
            cam.backgroundColor = new Color(0.08f, 0.09f, 0.12f);
            camGO.transform.position = new Vector3(0f, 0f, -10f);

            CreatePersistentSystemsLoaderObject();

            var canvasGO = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = canvasGO.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasGO.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(960f, 540f);

            CreateText(canvasGO.transform, "Title", "Semiconductor Fab Adventure", 32, TextAnchor.MiddleCenter, Color.white,
                new Vector2(0.1f, 0.65f), new Vector2(0.9f, 0.85f), Vector2.zero, Vector2.zero);

            Button newGame = CreateButton(canvasGO.transform, "NewGameButton", "New Game", new Vector2(0.35f, 0.48f), new Vector2(0.65f, 0.58f));
            Button continueGame = CreateButton(canvasGO.transform, "ContinueButton", "Continue", new Vector2(0.35f, 0.35f), new Vector2(0.65f, 0.45f));
            Button quit = CreateButton(canvasGO.transform, "QuitButton", "Quit", new Vector2(0.35f, 0.22f), new Vector2(0.65f, 0.32f));

            var controllerGO = new GameObject("MainMenuController", typeof(MainMenuController));
            var so = new SerializedObject(controllerGO.GetComponent<MainMenuController>());
            so.FindProperty("newGameButton").objectReferenceValue = newGame;
            so.FindProperty("continueButton").objectReferenceValue = continueGame;
            so.FindProperty("quitButton").objectReferenceValue = quit;
            so.FindProperty("startingRoomScene").stringValue = "Lobby";
            so.FindProperty("startingSpawnPointId").stringValue = "lobby_start";
            so.FindProperty("startingFallbackPosition").vector2Value = new Vector2(3.5f, 3.5f);
            so.ApplyModifiedPropertiesWithoutUndo();

            string path = $"{ScenesFolder}/MainMenu.unity";
            EditorSceneManager.SaveScene(scene, path);
        }
    }
}
