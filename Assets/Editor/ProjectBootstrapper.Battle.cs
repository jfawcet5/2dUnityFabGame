using BeyProject.Battle;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BeyProject.EditorTools
{
    public static partial class ProjectBootstrapper
    {
        private static void BuildBattleScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var camGO = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            camGO.tag = "MainCamera";
            Camera cam = camGO.GetComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 5f;
            cam.backgroundColor = new Color(0.12f, 0.12f, 0.14f);
            camGO.transform.position = new Vector3(0f, 0f, -10f);

            // Persistent EventSystem/UI is shared across all scenes via this loader - do not
            // add a second standalone EventSystem here, it would conflict with the one on
            // the persistent prefab.
            CreatePersistentSystemsLoaderObject();

            var canvasGO = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = canvasGO.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasGO.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(960f, 540f);

            Text label = CreateText(canvasGO.transform, "OpponentLabel", "Battle", 30, TextAnchor.MiddleCenter, Color.white,
                new Vector2(0.1f, 0.5f), new Vector2(0.9f, 0.85f), Vector2.zero, Vector2.zero);

            Button returnButton = CreateButton(canvasGO.transform, "ReturnButton", "Return to Exploration",
                new Vector2(0.3f, 0.2f), new Vector2(0.7f, 0.3f));

            // Fade overlay - rendered last (on top), starts fully opaque and fades to
            // transparent, revealing the message underneath rather than fading it out.
            var fadeGO = new GameObject("FadeOverlay", typeof(Image));
            fadeGO.transform.SetParent(canvasGO.transform, false);
            Image fadeImage = fadeGO.GetComponent<Image>();
            fadeImage.color = Color.black;
            fadeImage.raycastTarget = false;
            RectTransform fadeRect = fadeGO.GetComponent<RectTransform>();
            fadeRect.anchorMin = Vector2.zero;
            fadeRect.anchorMax = Vector2.one;
            fadeRect.offsetMin = Vector2.zero;
            fadeRect.offsetMax = Vector2.zero;
            fadeGO.transform.SetAsLastSibling();

            var controllerGO = new GameObject("BattleController", typeof(BattleSceneController));
            var so = new SerializedObject(controllerGO.GetComponent<BattleSceneController>());
            so.FindProperty("opponentLabel").objectReferenceValue = label;
            so.FindProperty("returnButton").objectReferenceValue = returnButton;
            so.FindProperty("fadeOverlay").objectReferenceValue = fadeImage;
            so.ApplyModifiedPropertiesWithoutUndo();

            string path = $"{ScenesFolder}/Battle.unity";
            EditorSceneManager.SaveScene(scene, path);
        }
    }
}
