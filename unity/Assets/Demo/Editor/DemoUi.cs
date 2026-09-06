using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Waribashi.ScreenTransitions.Demo.EditorTools
{
    /// <summary>
    /// デモシーンを手続き生成するための共通部品。
    /// シーンはアセットとして手で組まず、すべてここからコードで作る。
    /// 差分がレビューできること、蓋絵を追加したときに作り直せることを優先している。
    /// </summary>
    public static class DemoUi
    {
        public static readonly Color Panel = new Color(0.15f, 0.15f, 0.18f, 0.92f);
        public static readonly Color Accent = new Color(0.18f, 0.30f, 0.46f, 0.95f);
        public static readonly Color Warn = new Color(0.42f, 0.20f, 0.16f, 0.95f);
        public static readonly Color Faint = new Color(1f, 1f, 1f, 0.55f);

        /// <summary>デモフォルダ。Samples~ として取り込まれた先でも自力で解決する。</summary>
        public static string DemoDir => ParentOf(ParentOf(AssetPathOfAsset(nameof(DemoUi), "MonoScript")));

        public static string ScenePath(string sceneName) => DemoDir + "/" + sceneName + ".unity";

        public static string ParentOf(string path) => Path.GetDirectoryName(path).Replace('\\', '/');

        public static string AssetPathOfAsset(string name, string type)
        {
            var guid = AssetDatabase.FindAssets(name + " t:" + type)
                .FirstOrDefault(g => Path.GetFileNameWithoutExtension(AssetDatabase.GUIDToAssetPath(g)) == name);

            if (string.IsNullOrEmpty(guid))
                throw new FileNotFoundException(name + " (" + type + ") が見つからない。デモの再生成にはパッケージ本体が必要です。");

            return AssetDatabase.GUIDToAssetPath(guid);
        }

        /// <summary>カメラ / EventSystem / Canvas だけを持つ空のシーンを作る。</summary>
        public static Scene NewScene(Color background, out RectTransform canvas)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var camGo = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            camGo.tag = "MainCamera";
            var cam = camGo.GetComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = background;
            camGo.transform.position = new Vector3(0f, 0f, -10f);

            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));

            var canvasGo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            canvas = (RectTransform)canvasGo.transform;
            return scene;
        }

        public static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        public static Text CreateText(Transform parent, string name, string content, int fontSize, Color color)
        {
            var go = new GameObject(name, typeof(Text));
            go.transform.SetParent(parent, false);

            var text = go.GetComponent<Text>();
            text.text = content;
            text.fontSize = fontSize;
            text.color = color;
            text.alignment = TextAnchor.MiddleCenter;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            return text;
        }

        /// <summary>左上に置く見出し。</summary>
        public static Text CreateTitle(Transform canvas, string title, string subtitle)
        {
            var text = CreateText(canvas, "Title", title + "\n" + subtitle, 40, Color.white);
            text.alignment = TextAnchor.UpperLeft;
            text.lineSpacing = 1.25f;

            var rt = text.rectTransform;
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(40f, -32f);
            rt.sizeDelta = new Vector2(1400f, 120f);
            return text;
        }

        /// <summary>任意の位置に置くラベル。</summary>
        public static Text CreateLabel(Transform parent, string name, string content, int fontSize,
            Color color, Vector2 position, Vector2 size, TextAnchor alignment = TextAnchor.UpperLeft)
        {
            var text = CreateText(parent, name, content, fontSize, color);
            text.alignment = alignment;

            var rt = text.rectTransform;
            rt.anchoredPosition = position;
            rt.sizeDelta = size;
            return text;
        }

        public static Image CreateStretchedImage(Transform parent, string name, Color color)
        {
            var go = new GameObject(name, typeof(Image));
            go.transform.SetParent(parent, false);
            Stretch((RectTransform)go.transform);
            go.GetComponent<Image>().color = color;
            return go.GetComponent<Image>();
        }

        public static Image CreateRect(Transform parent, string name, Color color, Vector2 position, Vector2 size)
        {
            var go = new GameObject(name, typeof(Image));
            go.transform.SetParent(parent, false);

            var rt = (RectTransform)go.transform;
            rt.anchoredPosition = position;
            rt.sizeDelta = size;

            var image = go.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        public static Button CreateButton(Transform parent, string name, string label,
            Vector2 position, Vector2 size, Color color, int fontSize = 28)
        {
            var go = new GameObject(name, typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);

            var rt = (RectTransform)go.transform;
            rt.anchoredPosition = position;
            rt.sizeDelta = size;
            go.GetComponent<Image>().color = color;

            var text = CreateText(go.transform, "Label", label, fontSize, Color.white);
            Stretch(text.rectTransform);

            return go.GetComponent<Button>();
        }

        /// <summary>左下に「ハブへ戻る」を置く。ハブ以外のデモシーンすべてに付ける。</summary>
        public static void AddBackToHub(Transform canvas)
        {
            var button = CreateButton(canvas, "BackToHubButton", "<< HUB",
                Vector2.zero, new Vector2(220f, 60f), Panel);

            var rt = button.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(0f, 0f);
            rt.pivot = new Vector2(0f, 0f);
            rt.anchoredPosition = new Vector2(40f, 32f);

            var go = new GameObject("BackToHub", typeof(BackToHubButton));
            go.GetComponent<BackToHubButton>().button = button;
        }

        /// <summary>デモらしさを出すための背景の飾り。動きの基準にもなる。</summary>
        public static void CreateDeco(Transform parent, Vector2 position, Vector2 size, Color color)
        {
            CreateRect(parent, "Deco", color, position, size);
        }
    }
}
