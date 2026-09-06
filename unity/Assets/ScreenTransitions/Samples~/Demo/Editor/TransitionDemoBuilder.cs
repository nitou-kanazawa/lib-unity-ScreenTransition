using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using UnityEngine.UI;
using Waribashi.ScreenTransitions.Demo;
using Waribashi.ScreenTransitions.Demo.Immersive;
using Waribashi.ScreenTransitions.Demo.Submarine;

namespace Waribashi.ScreenTransitions.Demo.EditorTools
{
    /// <summary>
    /// 蓋絵のカタログシーンと、その遷移先シーン、閉じ/開き Timeline アセットを構築する。
    /// ハブと他のデモは DemoScenesBuilder が作る。
    /// </summary>
    public static class TransitionDemoBuilder
    {
        const float Duration = 0.9f;

        // グリッドの寸法。4 列 × 最大 12 行が画面に収まるように決めてある
        const int ColumnCount = 4;
        const float ColumnWidth = 430f;
        const float ColumnGap = 22f;
        const float ButtonHeight = 34f;
        const float ButtonGap = 5f;
        const float HeaderHeight = 28f;
        const float HeaderGap = 10f;
        const float GridTop = 78f;

        static string TimelineDir => DemoUi.DemoDir + "/Timelines";
        static string TextureDir => DemoUi.ParentOf(DemoUi.AssetPathOfAsset("Rule_01_CircleIn", "Texture2D"));
        static string ScenePath => DemoUi.ScenePath(DemoScenes.Catalogue);
        static string UnderseaScenePath => DemoUi.ScenePath(DemoScenes.Undersea);

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

            // 遷移先シーンを先に作っておく（最後にカタログを開いた状態で終える）
            BuildUnderseaScene();
            BuildCatalogueScene(patterns, closeTimeline, openTimeline, loopTimeline);
        }

        static void BuildCatalogueScene(Texture2D[] patterns,
            TimelineAsset closeTimeline, TimelineAsset openTimeline, TimelineAsset loopTimeline)
        {
            var scene = DemoUi.NewScene(new Color(0.05f, 0.05f, 0.07f), out var canvas);

            // --- 疑似的なゲーム画面（蓋の下に隠れる側）---
            var background = DemoUi.CreateStretchedImage(canvas, "Background", new Color(0.13f, 0.24f, 0.42f));
            DemoUi.CreateDeco(background.transform, new Vector2(-700f, 250f), new Vector2(140f, 140f), new Color(0.95f, 0.75f, 0.25f, 0.85f));
            DemoUi.CreateDeco(background.transform, new Vector2(760f, 180f), new Vector2(110f, 110f), new Color(0.35f, 0.8f, 0.55f, 0.85f));
            DemoUi.CreateDeco(background.transform, new Vector2(-500f, 145f), new Vector2(80f, 80f), new Color(0.85f, 0.4f, 0.55f, 0.85f));

            var sceneLabel = DemoUi.CreateLabel(background.transform, "SceneLabel", "SCENE A", 96,
                new Color(1f, 1f, 1f, 0.85f), new Vector2(0f, 190f), new Vector2(1000f, 120f), TextAnchor.MiddleCenter);
            DemoUi.CreateLabel(background.transform, "SceneNote",
                "閉じ切っている間に背景を差し替えている（＝蓋で覆えていることの確認）", 20,
                new Color(1f, 1f, 1f, 0.5f), new Vector2(0f, 122f), new Vector2(1200f, 30f), TextAnchor.MiddleCenter);

            // --- 見出しとステータス ---
            DemoUi.CreateTitle(canvas, "TRANSITION CATALOGUE",
                "押すと 閉じる → 保持 → 開く を一連で流す");

            var status = DemoUi.CreateLabel(canvas, "Status", "Ready", 30, Color.white,
                new Vector2(40f, -170f), new Vector2(900f, 40f));
            status.rectTransform.anchorMin = new Vector2(0f, 1f);
            status.rectTransform.anchorMax = new Vector2(0f, 1f);
            status.rectTransform.pivot = new Vector2(0f, 1f);

            BuildInfoPanel(canvas, out var infoTitle, out var infoBody);

            // --- 蓋絵の実体（グリッドより先に作り、あとで最前面へ回す）---
            var objectCurtains = new List<ObjectCurtain>();
            var generic = CreateGenericCurtains(canvas, objectCurtains);
            var submarine = CreateSubmarineCurtains(canvas, objectCurtains);
            var immersive = CreateImmersiveCurtains(canvas, objectCurtains);

            // --- カテゴリごとのボタン列 ---
            var patternButtons = BuildColumn(canvas, 0, "ルール画像系 " + patterns.Length, "パッケージ本体",
                patterns.Select(p => DisplayName(p.name)).ToArray());
            var genericButtons = BuildColumn(canvas, 1, "オブジェクト系・汎用 " + generic.Length, "パッケージ本体", generic);
            var submarineButtons = BuildColumn(canvas, 2, "潜水艦テーマ " + submarine.Length, "サンプル", submarine);
            var immersiveButtons = BuildColumn(canvas, 3, "没入系 " + immersive.Length, "サンプル", immersive);

            // --- 操作列 ---
            var sceneLoad = DemoUi.CreateButton(canvas, "SceneLoadButton", ">> SCENE LOAD（実シーンロード）",
                new Vector2(-330f, -470f), new Vector2(460f, 52f), DemoUi.Accent, 24);
            var loadingToggle = CreateLoadingToggle(canvas, new Vector2(340f, -470f));

            DemoUi.AddBackToHub(canvas);

            // --- 蓋絵を最前面へ。生成順 = 描画順なので、UI を作り終えてから前に出す ---
            foreach (var curtain in objectCurtains)
                curtain.transform.SetAsLastSibling();

            var transitionGo = new GameObject("RuleImage", typeof(Image), typeof(TransitionImage));
            transitionGo.transform.SetParent(canvas, false);
            DemoUi.Stretch((RectTransform)transitionGo.transform);
            var transitionImage = transitionGo.GetComponent<Image>();
            transitionImage.color = Color.black;
            transitionImage.raycastTarget = true;
            var target = transitionGo.GetComponent<TransitionImage>();
            target.RuleTexture = patterns[0];
            target.Cutoff = 0f;

            var loadingGo = new GameObject("LoadingIndicator", typeof(RectTransform), typeof(LoadingIndicator));
            loadingGo.transform.SetParent(canvas, false);
            DemoUi.Stretch((RectTransform)loadingGo.transform);

            // --- プレイヤーとコントローラ ---
            var systemGo = new GameObject("TransitionSystem",
                typeof(PlayableDirector), typeof(RuleImageCurtain), typeof(TransitionDemoController));

            var player = systemGo.GetComponent<RuleImageCurtain>();
            player.Target = target;
            player.CloseTimeline = closeTimeline;
            player.OpenTimeline = openTimeline;
            player.LoopTimeline = loopTimeline;

            var controller = systemGo.GetComponent<TransitionDemoController>();
            controller.player = player;
            controller.patterns = patterns;
            controller.patternButtons = patternButtons;
            controller.objectCurtains = objectCurtains.ToArray();
            controller.curtainButtons = genericButtons.Concat(submarineButtons).Concat(immersiveButtons).ToArray();
            controller.sceneLoadButton = sceneLoad;
            controller.loadingToggle = loadingToggle;
            controller.statusLabel = status;
            controller.infoTitleLabel = infoTitle;
            controller.infoBodyLabel = infoBody;
            controller.background = background;
            controller.backgroundLabel = sceneLabel;
            controller.loadingIndicator = loadingGo.GetComponent<LoadingIndicator>();

            EditorSceneManager.SaveScene(scene, ScenePath);

            // ビルド設定への登録は DemoScenesBuilder がまとめて行う
            Debug.Log($"[ScreenTransitions] Catalogue scenes built: {ScenePath}, {UnderseaScenePath} (patterns: {patterns.Length})");
        }

        // ------------------------------------------------------------------
        // 部品
        // ------------------------------------------------------------------

        /// <summary>再生した蓋絵の素性を出すパネル（右上）。</summary>
        static void BuildInfoPanel(Transform canvas, out Text title, out Text body)
        {
            var panel = DemoUi.CreateRect(canvas, "InfoPanel", new Color(0f, 0f, 0f, 0.45f),
                new Vector2(600f, 378f), new Vector2(640f, 200f));

            title = DemoUi.CreateLabel(panel.transform, "InfoTitle", "-", 30, Color.white,
                new Vector2(18f, -14f), new Vector2(604f, 36f));
            Anchor(title.rectTransform);

            body = DemoUi.CreateLabel(panel.transform, "InfoBody", "-", 21, new Color(1f, 1f, 1f, 0.7f),
                new Vector2(18f, -58f), new Vector2(604f, 130f));
            body.lineSpacing = 1.25f;
            Anchor(body.rectTransform);
        }

        static void Anchor(RectTransform rt)
        {
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
        }

        /// <summary>カテゴリ 1 列ぶんの見出しとボタンを作る。</summary>
        static Button[] BuildColumn(Transform canvas, int columnIndex, string title, string note, string[] labels)
        {
            float totalWidth = ColumnCount * ColumnWidth + (ColumnCount - 1) * ColumnGap;
            float x = -totalWidth * 0.5f + ColumnWidth * 0.5f + columnIndex * (ColumnWidth + ColumnGap);

            var header = DemoUi.CreateLabel(canvas, "Header_" + columnIndex,
                title + "   ·   " + note, 22, new Color(1f, 1f, 1f, 0.72f),
                new Vector2(x, GridTop - HeaderHeight * 0.5f), new Vector2(ColumnWidth, HeaderHeight),
                TextAnchor.MiddleCenter);
            header.fontStyle = FontStyle.Bold;

            float firstY = GridTop - HeaderHeight - HeaderGap - ButtonHeight * 0.5f;
            var buttons = new Button[labels.Length];

            for (int i = 0; i < labels.Length; i++)
            {
                buttons[i] = DemoUi.CreateButton(canvas, "Btn_" + columnIndex + "_" + i, labels[i],
                    new Vector2(x, firstY - i * (ButtonHeight + ButtonGap)),
                    new Vector2(ColumnWidth, ButtonHeight), DemoUi.Panel, 22);
            }

            return buttons;
        }

        static string[] CreateGenericCurtains(Transform canvas, List<ObjectCurtain> sink)
        {
            return Add(canvas, sink,
                Curtain<FadeTransition>("Fade"),
                Curtain<StripeSlideTransition>("Stripe Slide"),
                Curtain<DiagonalSlabTransition>("Diagonal Slab"),
                Curtain<TilePopTransition>("Tile Pop"),
                Curtain<SplitSlamTransition>("Split Slam"),
                Curtain<SlatFlipTransition>("Slat Flip"),
                Curtain<SpinSquareTransition>("Spin Square"),
                Curtain<CurtainDropTransition>("Curtain Drop"),
                Curtain<ZigzagWipeTransition>("Zigzag Wipe"));
        }

        static string[] CreateSubmarineCurtains(Transform canvas, List<ObjectCurtain> sink)
        {
            return Add(canvas, sink,
                Curtain<WaveDiveTransition>("Wave Dive"),
                Curtain<SubBoardingTransition>("Sub Boarding"),
                Curtain<DepthGaugeTransition>("Depth Gauge"),
                Curtain<BubbleBurstTransition>("Bubble Burst"),
                Curtain<SonarPingTransition>("Sonar Ping"),
                Curtain<PeriscopeIrisTransition>("Periscope Iris"),
                Curtain<DeepFadeTransition>("Deep Fade"),
                Curtain<FishSchoolTransition>("Fish School"),
                Curtain<HatchSlamTransition>("Hatch Slam"),
                Curtain<DepthZonesTransition>("Depth Zones"));
        }

        static string[] CreateImmersiveCurtains(Transform canvas, List<ObjectCurtain> sink)
        {
            return Add(canvas, sink,
                Curtain<TownCrowdTransition>("Town Crowd"),
                Curtain<SubwayRideTransition>("Subway Ride"));
        }

        delegate ObjectCurtain CurtainFactory(Transform parent, string displayName);

        struct CurtainSpec
        {
            public CurtainFactory Factory;
            public string DisplayName;
        }

        static CurtainSpec Curtain<T>(string displayName) where T : ObjectCurtain
        {
            return new CurtainSpec
            {
                Factory = (parent, name) => CreateObjectCurtain<T>(parent, typeof(T).Name, name),
                DisplayName = displayName,
            };
        }

        /// <summary>蓋絵を生成して sink に積み、ボタン用の表示名を返す。</summary>
        static string[] Add(Transform canvas, List<ObjectCurtain> sink, params CurtainSpec[] specs)
        {
            foreach (var spec in specs)
                sink.Add(spec.Factory(canvas, spec.DisplayName));

            return specs.Select(s => s.DisplayName).ToArray();
        }

        static T CreateObjectCurtain<T>(Transform parent, string name, string displayName) where T : ObjectCurtain
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(T));
            go.transform.SetParent(parent, false);
            DemoUi.Stretch((RectTransform)go.transform);

            var curtain = go.GetComponent<T>();
            curtain.displayName = displayName;
            return curtain;
        }

        static Toggle CreateLoadingToggle(Transform canvas, Vector2 position)
        {
            var go = new GameObject("LoadingToggle", typeof(RectTransform), typeof(Toggle));
            go.transform.SetParent(canvas, false);

            var rt = (RectTransform)go.transform;
            rt.anchoredPosition = position;
            rt.sizeDelta = new Vector2(520f, 52f);

            var box = DemoUi.CreateRect(go.transform, "Box", new Color(0.9f, 0.9f, 0.9f),
                new Vector2(-230f, 0f), new Vector2(30f, 30f));
            box.raycastTarget = true;

            var check = DemoUi.CreateRect(box.transform, "Checkmark", new Color(0.2f, 0.7f, 0.45f),
                Vector2.zero, new Vector2(18f, 18f));

            DemoUi.CreateLabel(go.transform, "Label", "閉じ切ったら 2 秒保持して LoadingIndicator を出す", 22,
                Color.white, new Vector2(30f, 0f), new Vector2(440f, 40f), TextAnchor.MiddleLeft);

            var toggle = go.GetComponent<Toggle>();
            toggle.targetGraphic = box;
            toggle.graphic = check;
            toggle.isOn = false;
            return toggle;
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

        static string DisplayName(string textureName)
        {
            if (textureName.StartsWith("Rule_"))
                textureName = textureName.Substring(5);
            return textureName.Replace('_', ' ');
        }

        // ------------------------------------------------------------------
        // Timeline アセット
        // ------------------------------------------------------------------

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

        // ------------------------------------------------------------------
        // 遷移先シーン
        // ------------------------------------------------------------------

        static void BuildUnderseaScene()
        {
            var scene = DemoUi.NewScene(new Color(0.02f, 0.05f, 0.09f), out var canvas);

            var background = DemoUi.CreateStretchedImage(canvas, "Background", new Color(0.03f, 0.09f, 0.17f));
            DemoUi.CreateDeco(background.transform, new Vector2(-560f, -220f), new Vector2(300f, 90f), new Color(0.05f, 0.14f, 0.24f));
            DemoUi.CreateDeco(background.transform, new Vector2(520f, 260f), new Vector2(200f, 60f), new Color(0.06f, 0.16f, 0.28f));

            DemoUi.CreateLabel(background.transform, "Label", "UNDERSEA", 120,
                new Color(0.55f, 0.8f, 0.95f, 0.9f), new Vector2(0f, 60f), new Vector2(1200f, 160f), TextAnchor.MiddleCenter);

            DemoUi.CreateTitle(canvas, "SCENE LOAD",
                "常駐サービス経由で遷移してきた先。蓋絵はシーンをまたいで生き残る");

            var back = DemoUi.CreateButton(canvas, "BackButton", "<< BACK TO CATALOGUE",
                new Vector2(0f, -330f), new Vector2(460f, 64f), DemoUi.Accent);

            var controllerGo = new GameObject("UnderseaController", typeof(UnderseaDemoController));
            controllerGo.GetComponent<UnderseaDemoController>().backButton = back;

            DemoUi.AddBackToHub(canvas);

            EditorSceneManager.SaveScene(scene, UnderseaScenePath);
        }
    }
}
