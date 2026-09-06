using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;
using UnityEngine.Timeline;
using UnityEngine.UI;
using Waribashi.ScreenTransitions.Demo;
using Waribashi.ScreenTransitions.Demo.Submarine;
using Waribashi.ScreenTransitions.Demo.Immersive;

namespace Waribashi.ScreenTransitions.Demo.EditorTools
{
    /// <summary>
    /// デモシーンと閉じ/開き Timeline アセットを構築する。
    /// </summary>
    public static class TransitionDemoBuilder
    {
        // パスは固定せず自力で解決する。Samples~ として配布したあと利用側が
        // Assets/Samples/<pkg>/<ver>/Demo/ に取り込んでも、そのまま動くようにするため。
        static string DemoDir => ParentOf(ParentOf(AssetPathOfScript("TransitionDemoBuilder")));
        static string TimelineDir => DemoDir + "/Timelines";
        static string TextureDir => ParentOf(AssetPathOfAsset("Rule_01_CircleIn", "Texture2D"));
        static string ScenePath => DemoDir + "/TransitionDemo.unity";
        static string UnderseaScenePath => DemoDir + "/TransitionUndersea.unity";

        static string ParentOf(string path) => Path.GetDirectoryName(path).Replace('\\', '/');

        static string AssetPathOfScript(string name) => AssetPathOfAsset(name, "MonoScript");

        static string AssetPathOfAsset(string name, string type)
        {
            var guid = AssetDatabase.FindAssets($"{name} t:{type}").FirstOrDefault();
            if (string.IsNullOrEmpty(guid))
                throw new FileNotFoundException($"{name} ({type}) が見つからない。デモの再生成にはパッケージ本体が必要です。");
            return AssetDatabase.GUIDToAssetPath(guid);
        }
        const float Duration = 0.9f;

        [MenuItem("Tools/Screen Transitions/Build Catalogue Scenes Only")]
        public static void Build()
        {
            Directory.CreateDirectory(TimelineDir);

            var closeTimeline = CreateTransitionTimeline(TimelineDir + "/Transition_Close.playable", 0f, 1f);
            var openTimeline = CreateTransitionTimeline(TimelineDir + "/Transition_Open.playable", 1f, 0f);
            var loopTimeline = CreateLoopTimeline(TimelineDir + "/Transition_Loop.playable");

            var patterns = LoadPatterns();
            if (patterns.Length == 0)
            {
                Debug.LogError("[ScreenTransitions] No rule textures found. Run 'Generate Rule Textures' first.");
                return;
            }

            // 遷移先シーンを先に作っておく（最後にデモシーンを開いた状態で終える）
            BuildUnderseaScene();

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // --- Camera ---
            var camGo = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            camGo.tag = "MainCamera";
            var cam = camGo.GetComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.05f, 0.05f, 0.07f);
            camGo.transform.position = new Vector3(0f, 0f, -10f);

            // --- EventSystem (Input System) ---
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));

            // --- Canvas ---
            var canvasGo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            // --- 疑似シーン背景 ---
            var background = CreateStretchedImage(canvasGo.transform, "Background", new Color(0.13f, 0.24f, 0.42f));
            var bgLabel = CreateText(background.transform, "SceneLabel", "SCENE A", 120, new Color(1f, 1f, 1f, 0.85f));
            Stretch(bgLabel.rectTransform);

            // 画面に動きの基準となる装飾を置く
            CreateDecoRect(background.transform, new Vector2(-620f, 180f), new Vector2(240f, 240f), new Color(0.95f, 0.75f, 0.25f, 0.9f));
            CreateDecoRect(background.transform, new Vector2(640f, -120f), new Vector2(180f, 180f), new Color(0.35f, 0.8f, 0.55f, 0.9f));
            CreateDecoRect(background.transform, new Vector2(430f, 300f), new Vector2(120f, 120f), new Color(0.85f, 0.4f, 0.55f, 0.9f));

            // --- ステータス表示 ---
            var status = CreateText(canvasGo.transform, "StatusLabel", "Ready", 40, Color.white);
            var statusRt = status.rectTransform;
            statusRt.anchorMin = new Vector2(0f, 1f);
            statusRt.anchorMax = new Vector2(0f, 1f);
            statusRt.pivot = new Vector2(0f, 1f);
            statusRt.anchoredPosition = new Vector2(24f, -20f);
            statusRt.sizeDelta = new Vector2(900f, 60f);
            status.alignment = TextAnchor.MiddleLeft;

            // --- ボタングリッド（下部） ---
            var gridGo = new GameObject("ButtonGrid", typeof(RectTransform), typeof(GridLayoutGroup));
            gridGo.transform.SetParent(canvasGo.transform, false);
            var gridRt = gridGo.GetComponent<RectTransform>();
            gridRt.anchorMin = new Vector2(0.5f, 0f);
            gridRt.anchorMax = new Vector2(0.5f, 0f);
            gridRt.pivot = new Vector2(0.5f, 0f);
            gridRt.anchoredPosition = new Vector2(0f, 24f);
            gridRt.sizeDelta = new Vector2(1720f, 350f);
            var grid = gridGo.GetComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(276f, 48f);
            grid.spacing = new Vector2(12f, 8f);
            grid.childAlignment = TextAnchor.MiddleCenter;
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 6;

            var buttonTemplate = CreateButtonTemplate(gridGo.transform);

            // --- オブジェクト系トランジション ---
            var fade = CreateObjectCurtain<FadeTransition>(canvasGo.transform, "Obj_Fade", "Fade");
            var stripe = CreateObjectCurtain<StripeSlideTransition>(canvasGo.transform, "Obj_StripeSlide", "Stripe Slide");
            var slab = CreateObjectCurtain<DiagonalSlabTransition>(canvasGo.transform, "Obj_DiagonalSlab", "Diagonal Slab");
            var tile = CreateObjectCurtain<TilePopTransition>(canvasGo.transform, "Obj_TilePop", "Tile Pop");
            var slam = CreateObjectCurtain<SplitSlamTransition>(canvasGo.transform, "Obj_SplitSlam", "Split Slam");
            var slat = CreateObjectCurtain<SlatFlipTransition>(canvasGo.transform, "Obj_SlatFlip", "Slat Flip");
            var spin = CreateObjectCurtain<SpinSquareTransition>(canvasGo.transform, "Obj_SpinSquare", "Spin Square");
            var curtain = CreateObjectCurtain<CurtainDropTransition>(canvasGo.transform, "Obj_CurtainDrop", "Curtain Drop");
            var zigzag = CreateObjectCurtain<ZigzagWipeTransition>(canvasGo.transform, "Obj_ZigzagWipe", "Zigzag Wipe");

            // --- 潜水艦テーマ ---
            var waveDive = CreateObjectCurtain<WaveDiveTransition>(canvasGo.transform, "Sub_WaveDive", "Wave Dive");
            var boarding = CreateObjectCurtain<SubBoardingTransition>(canvasGo.transform, "Sub_Boarding", "Sub Boarding");
            var depthGauge = CreateObjectCurtain<DepthGaugeTransition>(canvasGo.transform, "Sub_DepthGauge", "Depth Gauge");
            var bubbleBurst = CreateObjectCurtain<BubbleBurstTransition>(canvasGo.transform, "Sub_BubbleBurst", "Bubble Burst");
            var sonar = CreateObjectCurtain<SonarPingTransition>(canvasGo.transform, "Sub_SonarPing", "Sonar Ping");
            var periscope = CreateObjectCurtain<PeriscopeIrisTransition>(canvasGo.transform, "Sub_Periscope", "Periscope");
            var deepFade = CreateObjectCurtain<DeepFadeTransition>(canvasGo.transform, "Sub_DeepFade", "Deep Fade");
            var fishSchool = CreateObjectCurtain<FishSchoolTransition>(canvasGo.transform, "Sub_FishSchool", "Fish School");
            var hatchSlam = CreateObjectCurtain<HatchSlamTransition>(canvasGo.transform, "Sub_HatchSlam", "Hatch Slam");
            var depthZones = CreateObjectCurtain<DepthZonesTransition>(canvasGo.transform, "Sub_DepthZones", "Depth Zones");

            // --- 没入系（群衆・車内スナップショット + HoldLoop）---
            var townCrowd = CreateObjectCurtain<TownCrowdTransition>(canvasGo.transform, "Imm_TownCrowd", "Town Crowd");
            var subwayRide = CreateObjectCurtain<SubwayRideTransition>(canvasGo.transform, "Imm_SubwayRide", "Subway Ride");

            // --- トランジション本体（最前面） ---
            var transitionGo = new GameObject("Transition", typeof(Image), typeof(TransitionImage));
            transitionGo.transform.SetParent(canvasGo.transform, false);
            Stretch(transitionGo.GetComponent<RectTransform>());
            var transitionImage = transitionGo.GetComponent<Image>();
            transitionImage.color = Color.black;
            transitionImage.raycastTarget = true;
            var transition = transitionGo.GetComponent<TransitionImage>();
            transition.RuleTexture = patterns[0];
            transition.Cutoff = 0f;
            transitionGo.transform.SetAsLastSibling();

            // --- プレイヤーとコントローラ ---
            var systemGo = new GameObject("TransitionSystem", typeof(PlayableDirector), typeof(RuleImageCurtain), typeof(TransitionDemoController));
            var player = systemGo.GetComponent<RuleImageCurtain>();
            player.Target = transition;
            player.CloseTimeline = closeTimeline;
            player.OpenTimeline = openTimeline;
            player.LoopTimeline = loopTimeline;

            var controller = systemGo.GetComponent<TransitionDemoController>();
            controller.player = player;
            controller.buttonRoot = gridRt;
            controller.buttonTemplate = buttonTemplate;
            controller.statusLabel = status;
            controller.background = background;
            controller.backgroundLabel = bgLabel;
            controller.patterns = patterns;
            controller.objectTransitions = new ObjectCurtain[]
            {
                fade, stripe, slab, tile, slam, slat, spin, curtain, zigzag,
                waveDive, boarding, depthGauge, bubbleBurst, sonar, periscope, deepFade, fishSchool, hatchSlam, depthZones,
                townCrowd, subwayRide
            };

            // --- ローディングインジケーター（最前面） ---
            var loadingGo = new GameObject("LoadingIndicator", typeof(RectTransform), typeof(LoadingIndicator));
            loadingGo.transform.SetParent(canvasGo.transform, false);
            Stretch((RectTransform)loadingGo.transform);
            loadingGo.transform.SetAsLastSibling();
            controller.loadingIndicator = loadingGo.GetComponent<LoadingIndicator>();

            // --- ローディング保持トグル（右上、蓋より背面に置く） ---
            controller.loadingToggle = CreateLoadingToggle(canvasGo.transform);
            controller.loadingToggle.transform.SetSiblingIndex(stripe.transform.GetSiblingIndex());

            // ハブへ戻る導線（蓋絵より背面に置く）
            DemoUi.AddBackToHub(canvasGo.transform);

            EditorSceneManager.SaveScene(scene, ScenePath);

            // ビルド設定への登録は DemoScenesBuilder がまとめて行う
            Debug.Log($"[ScreenTransitions] Catalogue scenes built: {ScenePath}, {UnderseaScenePath} (patterns: {patterns.Length})");
        }

        static TimelineAsset CreateTransitionTimeline(string path, float from, float to)
        {
            var existing = AssetDatabase.LoadAssetAtPath<TimelineAsset>(path);
            if (existing != null)
                AssetDatabase.DeleteAsset(path);

            var timeline = ScriptableObject.CreateInstance<TimelineAsset>();
            AssetDatabase.CreateAsset(timeline, path);

            var track = timeline.CreateTrack<TransitionTrack>(null, "Transition");
            var clip = track.CreateClip<TransitionClip>();
            clip.start = 0.0;
            clip.duration = Duration;
            clip.displayName = from < to ? "Close" : "Open";

            var asset = (TransitionClip)clip.asset;
            asset.template.from = from;
            asset.template.to = to;
            asset.template.easing = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

            EditorUtility.SetDirty(timeline);
            AssetDatabase.SaveAssets();
            return timeline;
        }

        static TimelineAsset CreateLoopTimeline(string path)
        {
            var existing = AssetDatabase.LoadAssetAtPath<TimelineAsset>(path);
            if (existing != null)
                AssetDatabase.DeleteAsset(path);

            var timeline = ScriptableObject.CreateInstance<TimelineAsset>();
            AssetDatabase.CreateAsset(timeline, path);

            var track = timeline.CreateTrack<TransitionTrack>(null, "Transition Loop");
            var clip = track.CreateClip<TransitionClip>();
            clip.start = 0.0;
            clip.duration = 1.4;
            clip.displayName = "Breath";

            var asset = (TransitionClip)clip.asset;
            asset.template.from = 1f;
            asset.template.to = 0.965f;
            // 0→1→0 カーブで from→to→from（Cutoff の呼吸ループ）
            asset.template.easing = new AnimationCurve(
                new Keyframe(0f, 0f, 0f, 0f),
                new Keyframe(0.5f, 1f, 0f, 0f),
                new Keyframe(1f, 0f, 0f, 0f));

            EditorUtility.SetDirty(timeline);
            AssetDatabase.SaveAssets();
            return timeline;
        }

        static void BuildUnderseaScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var camGo = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            camGo.tag = "MainCamera";
            var cam = camGo.GetComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.02f, 0.05f, 0.09f);
            camGo.transform.position = new Vector3(0f, 0f, -10f);

            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));

            var canvasGo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            var background = CreateStretchedImage(canvasGo.transform, "Background", new Color(0.03f, 0.09f, 0.17f));
            var label = CreateText(background.transform, "Label", "UNDERSEA", 120, new Color(0.55f, 0.8f, 0.95f, 0.9f));
            Stretch(label.rectTransform);

            CreateDecoRect(background.transform, new Vector2(-560f, -220f), new Vector2(300f, 90f), new Color(0.05f, 0.14f, 0.24f));
            CreateDecoRect(background.transform, new Vector2(520f, 260f), new Vector2(200f, 60f), new Color(0.06f, 0.16f, 0.28f));

            // Back ボタン
            var buttonGo = new GameObject("BackButton", typeof(Image), typeof(Button));
            buttonGo.transform.SetParent(canvasGo.transform, false);
            var brt = (RectTransform)buttonGo.transform;
            brt.anchorMin = new Vector2(0.5f, 0f);
            brt.anchorMax = new Vector2(0.5f, 0f);
            brt.pivot = new Vector2(0.5f, 0f);
            brt.anchoredPosition = new Vector2(0f, 60f);
            brt.sizeDelta = new Vector2(360f, 64f);
            buttonGo.GetComponent<Image>().color = new Color(0.12f, 0.2f, 0.3f, 0.95f);
            var backLabel = CreateText(buttonGo.transform, "Label", "<< BACK TO MAP", 30, Color.white);
            Stretch(backLabel.rectTransform);

            var controllerGo = new GameObject("UnderseaController", typeof(UnderseaDemoController));
            controllerGo.GetComponent<UnderseaDemoController>().backButton = buttonGo.GetComponent<Button>();

            DemoUi.AddBackToHub(canvasGo.transform);

            EditorSceneManager.SaveScene(scene, UnderseaScenePath);
        }

        static Texture2D[] LoadPatterns()
        {
            return AssetDatabase.FindAssets("t:Texture2D Rule_", new[] { TextureDir })
                .Select(AssetDatabase.GUIDToAssetPath)
                .OrderBy(p => p)
                .Select(AssetDatabase.LoadAssetAtPath<Texture2D>)
                .Where(t => t != null)
                .ToArray();
        }

        static Toggle CreateLoadingToggle(Transform parent)
        {
            var go = new GameObject("LoadingToggle", typeof(RectTransform), typeof(Toggle));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = new Vector2(1f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(1f, 1f);
            rt.anchoredPosition = new Vector2(-24f, -20f);
            rt.sizeDelta = new Vector2(330f, 44f);

            var bg = CreateStretchedImage(go.transform, "Background", new Color(0.15f, 0.15f, 0.18f, 0.92f));
            var bgRt = bg.rectTransform;
            bgRt.anchorMin = new Vector2(0f, 0.5f);
            bgRt.anchorMax = new Vector2(0f, 0.5f);
            bgRt.pivot = new Vector2(0f, 0.5f);
            bgRt.offsetMin = Vector2.zero;
            bgRt.offsetMax = Vector2.zero;
            bgRt.sizeDelta = new Vector2(36f, 36f);
            bgRt.anchoredPosition = Vector2.zero;

            var check = CreateStretchedImage(bg.transform, "Checkmark", new Color(1f, 0.62f, 0.24f));
            var checkRt = check.rectTransform;
            checkRt.anchorMin = new Vector2(0.5f, 0.5f);
            checkRt.anchorMax = new Vector2(0.5f, 0.5f);
            checkRt.pivot = new Vector2(0.5f, 0.5f);
            checkRt.offsetMin = Vector2.zero;
            checkRt.offsetMax = Vector2.zero;
            checkRt.sizeDelta = new Vector2(22f, 22f);
            checkRt.anchoredPosition = Vector2.zero;

            var label = CreateText(go.transform, "Label", "Loading Hold (2s)", 26, Color.white);
            var lrt = label.rectTransform;
            lrt.anchorMin = Vector2.zero;
            lrt.anchorMax = Vector2.one;
            lrt.offsetMin = new Vector2(46f, 0f);
            lrt.offsetMax = Vector2.zero;
            label.alignment = TextAnchor.MiddleLeft;

            var toggle = go.GetComponent<Toggle>();
            toggle.targetGraphic = bg;
            toggle.graphic = check;
            toggle.isOn = false;
            return toggle;
        }

        static T CreateObjectCurtain<T>(Transform parent, string name, string displayName) where T : ObjectCurtain
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(T));
            go.transform.SetParent(parent, false);
            Stretch((RectTransform)go.transform);
            var transition = go.GetComponent<T>();
            transition.displayName = displayName;
            return transition;
        }

        static Image CreateStretchedImage(Transform parent, string name, Color color)
        {
            var go = new GameObject(name, typeof(Image));
            go.transform.SetParent(parent, false);
            Stretch(go.GetComponent<RectTransform>());
            var image = go.GetComponent<Image>();
            image.color = color;
            return image;
        }

        static void CreateDecoRect(Transform parent, Vector2 pos, Vector2 size, Color color)
        {
            var go = new GameObject("Deco", typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            go.GetComponent<Image>().color = color;
        }

        static Text CreateText(Transform parent, string name, string content, int fontSize, Color color)
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
            return text;
        }

        static Button CreateButtonTemplate(Transform parent)
        {
            var go = new GameObject("ButtonTemplate", typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.color = new Color(0.15f, 0.15f, 0.18f, 0.92f);

            var label = CreateText(go.transform, "Label", "Pattern", 26, Color.white);
            Stretch(label.rectTransform);

            go.SetActive(false);
            return go.GetComponent<Button>();
        }

        static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
    }
}
