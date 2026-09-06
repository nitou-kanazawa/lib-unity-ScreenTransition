using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Waribashi.ScreenTransitions.Demo.CutIn;

namespace Waribashi.ScreenTransitions.Demo.EditorTools
{
    /// <summary>
    /// ハブと個別デモシーンを生成する。
    /// カタログ（TransitionDemo / TransitionUndersea）は TransitionDemoBuilder が作る。
    /// </summary>
    public static class DemoScenesBuilder
    {
        struct HubEntry
        {
            public string Scene;
            public string Title;
            public string Description;
        }

        static readonly HubEntry[] HubEntries =
        {
            new HubEntry
            {
                Scene = DemoScenes.Catalogue,
                Title = "TRANSITION CATALOGUE",
                Description = "同梱している蓋絵をすべて並べて再生する。シーンをまたぐ遷移もここから",
            },
            new HubEntry
            {
                Scene = DemoScenes.CutIn,
                Title = "CHARACTER CUT-IN",
                Description = "画面の一部しか覆わない蓋絵。覆っていない領域のボタンは押せたまま",
            },
            new HubEntry
            {
                Scene = DemoScenes.Interrupt,
                Title = "CANCEL / COMPLETE",
                Description = "中断の 2 種類。巻き戻して Open へ戻すのと、早送りして終端へ進めるの違い",
            },
            new HubEntry
            {
                Scene = DemoScenes.Loading,
                Title = "LOADING & TIME SCALE",
                Description = "閉じ切っている間の HoldLoop とローディング表示。timeScale = 0 でも完走する",
            },
        };

        [MenuItem("Tools/Screen Transitions/Build Demo Scenes")]
        public static void BuildAll()
        {
            // カタログ側（TransitionDemo / TransitionUndersea）を先に作る
            TransitionDemoBuilder.Build();

            BuildCutInScene();
            BuildInterruptScene();
            BuildLoadingScene();

            // 最後にハブを作り、開いた状態で終える
            BuildHubScene();

            RegisterBuildScenes();

            AssetDatabase.SaveAssets();
            Debug.Log("[ScreenTransitions] Demo scenes built: " + string.Join(", ", DemoScenes.All));
        }

        /// <summary>デモシーンをすべてビルド設定へ登録する。ハブが先頭。</summary>
        static void RegisterBuildScenes()
        {
            var scenes = new List<EditorBuildSettingsScene>();
            foreach (var name in DemoScenes.All)
            {
                var path = DemoUi.ScenePath(name);
                if (System.IO.File.Exists(path))
                    scenes.Add(new EditorBuildSettingsScene(path, true));
                else
                    Debug.LogWarning("[ScreenTransitions] シーンが見つからないので登録をとばした: " + path);
            }

            EditorBuildSettings.scenes = scenes.ToArray();
        }

        // ------------------------------------------------------------------
        // ハブ
        // ------------------------------------------------------------------

        static void BuildHubScene()
        {
            var scene = DemoUi.NewScene(new Color(0.06f, 0.06f, 0.09f), out var canvas);

            DemoUi.CreateStretchedImage(canvas, "Background", new Color(0.08f, 0.09f, 0.13f));
            DemoUi.CreateTitle(canvas, "SCREEN TRANSITIONS",
                "蓋絵（閉じる / 開ける）のデモ。見たいものを選ぶ");

            var entries = new List<DemoHubController.Entry>();

            for (int i = 0; i < HubEntries.Length; i++)
            {
                var entry = HubEntries[i];

                var button = DemoUi.CreateButton(canvas, "Entry_" + entry.Scene, string.Empty,
                    new Vector2(0f, 190f - i * 130f), new Vector2(1200f, 112f), DemoUi.Accent);

                // ボタンのラベルは 2 行（見出し + 説明）にするので、テンプレートの Label は作り直す
                var label = button.transform.Find("Label");
                if (label != null)
                    Object.DestroyImmediate(label.gameObject);

                var title = DemoUi.CreateLabel(button.transform, "Title", entry.Title, 34, Color.white,
                    new Vector2(28f, -22f), new Vector2(1120f, 40f));
                title.rectTransform.anchorMin = new Vector2(0f, 1f);
                title.rectTransform.anchorMax = new Vector2(0f, 1f);
                title.rectTransform.pivot = new Vector2(0f, 1f);

                var desc = DemoUi.CreateLabel(button.transform, "Desc", entry.Description, 24, DemoUi.Faint,
                    new Vector2(28f, -64f), new Vector2(1120f, 34f));
                desc.rectTransform.anchorMin = new Vector2(0f, 1f);
                desc.rectTransform.anchorMax = new Vector2(0f, 1f);
                desc.rectTransform.pivot = new Vector2(0f, 1f);

                entries.Add(new DemoHubController.Entry { button = button, sceneName = entry.Scene });
            }

            DemoUi.CreateLabel(canvas, "Footer",
                "Tools > Screen Transitions > Build Demo Scenes でこのシーン一式を作り直せる",
                22, DemoUi.Faint, new Vector2(0f, -420f), new Vector2(1400f, 40f), TextAnchor.MiddleCenter);

            var controllerGo = new GameObject("DemoHubController", typeof(DemoHubController));
            controllerGo.GetComponent<DemoHubController>().entries = entries.ToArray();

            EditorSceneManager.SaveScene(scene, DemoUi.ScenePath(DemoScenes.Hub));
        }

        // ------------------------------------------------------------------
        // カットイン
        // ------------------------------------------------------------------

        struct Character
        {
            public string Name;
            public Color Accent;
            public Color Hair;
        }

        static readonly Character[] Characters =
        {
            new Character { Name = "JOKER",   Accent = new Color(0.84f, 0.05f, 0.10f), Hair = new Color(0.07f, 0.07f, 0.09f) },
            new Character { Name = "SKULL",   Accent = new Color(0.95f, 0.72f, 0.10f), Hair = new Color(0.92f, 0.88f, 0.80f) },
            new Character { Name = "MONA",    Accent = new Color(0.10f, 0.42f, 0.72f), Hair = new Color(0.18f, 0.20f, 0.26f) },
            new Character { Name = "PANTHER", Accent = new Color(0.86f, 0.24f, 0.48f), Hair = new Color(0.78f, 0.62f, 0.24f) },
        };

        static void BuildCutInScene()
        {
            var scene = DemoUi.NewScene(new Color(0.05f, 0.05f, 0.08f), out var canvas);

            var background = DemoUi.CreateStretchedImage(canvas, "Background", new Color(0.10f, 0.08f, 0.14f));
            DemoUi.CreateDeco(background.transform, new Vector2(-620f, 120f), new Vector2(260f, 260f), new Color(0.20f, 0.13f, 0.24f));
            DemoUi.CreateDeco(background.transform, new Vector2(620f, -160f), new Vector2(200f, 200f), new Color(0.17f, 0.11f, 0.20f));
            DemoUi.CreateDeco(background.transform, new Vector2(120f, 300f), new Vector2(140f, 140f), new Color(0.22f, 0.15f, 0.26f));

            DemoUi.CreateTitle(canvas, "CHARACTER CUT-IN",
                "カットインは画面の一部しか覆わない（BlocksRaycasts = false）。再生中も右のボタンを押せる");

            var status = DemoUi.CreateLabel(canvas, "Status", "Ready", 30, Color.white,
                new Vector2(40f, -170f), new Vector2(1400f, 40f));
            status.rectTransform.anchorMin = new Vector2(0f, 1f);
            status.rectTransform.anchorMax = new Vector2(0f, 1f);
            status.rectTransform.pivot = new Vector2(0f, 1f);

            // 「押せること」を確かめるためのカウンタ
            // 帯にかからない位置に置く。カットイン中でも見えていて押せることが分かるように
            var counter = DemoUi.CreateButton(canvas, "CounterButton", string.Empty,
                new Vector2(620f, -300f), new Vector2(300f, 180f), new Color(0.16f, 0.34f, 0.28f, 0.95f));
            var counterLabel = counter.transform.Find("Label").GetComponent<Text>();
            counterLabel.fontSize = 40;

            DemoUi.CreateLabel(canvas, "CounterHint", "カットイン中も押せる / FADE 中は押せない",
                20, DemoUi.Faint, new Vector2(620f, -410f), new Vector2(420f, 34f), TextAnchor.MiddleCenter);

            // 操作ボタン
            var cutInButtons = new Button[Characters.Length];
            for (int i = 0; i < Characters.Length; i++)
            {
                cutInButtons[i] = DemoUi.CreateButton(canvas, "CutIn_" + Characters[i].Name, Characters[i].Name,
                    new Vector2(-700f + i * 250f, -380f), new Vector2(230f, 64f), DemoUi.Panel);
            }

            var allOut = DemoUi.CreateButton(canvas, "AllOutButton", "ALL-OUT ATTACK",
                new Vector2(-420f, -470f), new Vector2(440f, 64f), DemoUi.Accent);

            var fadeButton = DemoUi.CreateButton(canvas, "FadeButton", "FADE（全画面・対比用）",
                new Vector2(60f, -470f), new Vector2(440f, 64f), DemoUi.Warn);

            // 蓋絵は UI より手前に置く（生成順 = 描画順）
            var cutIns = new CharacterCutIn[Characters.Length];
            for (int i = 0; i < Characters.Length; i++)
            {
                var cutIn = CreateCurtain<CharacterCutIn>(canvas, "CutIn_" + Characters[i].Name, Characters[i].Name);
                cutIn.characterName = Characters[i].Name;
                cutIn.accentColor = Characters[i].Accent;
                cutIn.hairColor = Characters[i].Hair;
                cutIns[i] = cutIn;
            }

            var fade = CreateCurtain<FadeTransition>(canvas, "Fade", "Fade");

            DemoUi.AddBackToHub(canvas);

            var controllerGo = new GameObject("CutInDemoController", typeof(CutInDemoController));
            var controller = controllerGo.GetComponent<CutInDemoController>();
            controller.cutIns = cutIns;
            controller.cutInButtons = cutInButtons;
            controller.allOutButton = allOut;
            controller.fadeButton = fadeButton;
            controller.fade = fade;
            controller.counterButton = counter;
            controller.counterLabel = counterLabel;
            controller.statusLabel = status;

            EditorSceneManager.SaveScene(scene, DemoUi.ScenePath(DemoScenes.CutIn));
        }

        // ------------------------------------------------------------------
        // キャンセル / 早送り
        // ------------------------------------------------------------------

        static void BuildInterruptScene()
        {
            var scene = DemoUi.NewScene(new Color(0.05f, 0.06f, 0.08f), out var canvas);

            var background = DemoUi.CreateStretchedImage(canvas, "Background", new Color(0.11f, 0.13f, 0.17f));
            DemoUi.CreateDeco(background.transform, new Vector2(-680f, 300f), new Vector2(200f, 200f), new Color(0.18f, 0.22f, 0.28f));
            DemoUi.CreateDeco(background.transform, new Vector2(700f, 300f), new Vector2(160f, 160f), new Color(0.16f, 0.20f, 0.26f));

            DemoUi.CreateTitle(canvas, "CANCEL / COMPLETE",
                "RUN で「閉じる → 3s 保持 → 開く」を始め、その最中に COMPLETE / CANCEL を押す");

            var target = DemoUi.CreateLabel(canvas, "TargetLabel", "対象 :", 26, DemoUi.Faint,
                new Vector2(40f, -150f), new Vector2(1400f, 36f));
            target.rectTransform.anchorMin = new Vector2(0f, 1f);
            target.rectTransform.anchorMax = new Vector2(0f, 1f);
            target.rectTransform.pivot = new Vector2(0f, 1f);

            var state = DemoUi.CreateLabel(canvas, "StateLabel", "State : Open", 46, Color.white,
                new Vector2(0f, 210f), new Vector2(900f, 60f), TextAnchor.MiddleCenter);

            var log = DemoUi.CreateLabel(canvas, "LogLabel", string.Empty, 24, new Color(0.75f, 0.85f, 0.95f),
                new Vector2(0f, 30f), new Vector2(1200f, 240f), TextAnchor.UpperCenter);

            // 対象の切り替え
            var curtainButtons = new[]
            {
                DemoUi.CreateButton(canvas, "Target0", "SplitSlam（全画面）", new Vector2(-420f, -230f), new Vector2(380f, 56f), DemoUi.Panel, 24),
                DemoUi.CreateButton(canvas, "Target1", "ZigzagWipe（全画面）", new Vector2(0f, -230f), new Vector2(380f, 56f), DemoUi.Panel, 24),
                DemoUi.CreateButton(canvas, "Target2", "CharacterCutIn（部分）", new Vector2(420f, -230f), new Vector2(380f, 56f), DemoUi.Panel, 24),
            };

            var run = DemoUi.CreateButton(canvas, "RunButton", "RUN", new Vector2(-420f, -350f), new Vector2(380f, 78f), DemoUi.Accent, 32);
            var complete = DemoUi.CreateButton(canvas, "CompleteButton", "COMPLETE（早送り）", new Vector2(0f, -350f), new Vector2(380f, 78f), new Color(0.18f, 0.40f, 0.28f, 0.95f), 28);
            var cancel = DemoUi.CreateButton(canvas, "CancelButton", "CANCEL（巻き戻し）", new Vector2(420f, -350f), new Vector2(380f, 78f), DemoUi.Warn, 28);

            DemoUi.CreateLabel(canvas, "Hint",
                "Closed（保持中）の COMPLETE は拒否される。そこがロードかどうかは呼び出し側にしか分からないため",
                22, DemoUi.Faint, new Vector2(0f, -440f), new Vector2(1500f, 36f), TextAnchor.MiddleCenter);

            var curtains = new ObjectCurtain[]
            {
                CreateCurtain<SplitSlamTransition>(canvas, "SplitSlam", "SplitSlam"),
                CreateCurtain<ZigzagWipeTransition>(canvas, "ZigzagWipe", "ZigzagWipe"),
                CreateCurtain<CharacterCutIn>(canvas, "CutIn", "CharacterCutIn"),
            };

            DemoUi.AddBackToHub(canvas);

            var controllerGo = new GameObject("InterruptDemoController", typeof(InterruptDemoController));
            var controller = controllerGo.GetComponent<InterruptDemoController>();
            controller.curtains = curtains;
            controller.curtainButtons = curtainButtons;
            controller.runButton = run;
            controller.completeButton = complete;
            controller.cancelButton = cancel;
            controller.stateLabel = state;
            controller.logLabel = log;
            controller.targetLabel = target;

            EditorSceneManager.SaveScene(scene, DemoUi.ScenePath(DemoScenes.Interrupt));
        }

        // ------------------------------------------------------------------
        // ロード / timeScale
        // ------------------------------------------------------------------

        static void BuildLoadingScene()
        {
            var scene = DemoUi.NewScene(new Color(0.05f, 0.07f, 0.06f), out var canvas);

            var background = DemoUi.CreateStretchedImage(canvas, "Background", new Color(0.10f, 0.14f, 0.13f));
            DemoUi.CreateTitle(canvas, "LOADING & TIME SCALE",
                "蓋絵の裏で重い処理を回す。閉じ切っている間も HoldLoop が回るので画面が静止しない");

            // timeScale の違いを見るための 2 つの回転
            var scaled = DemoUi.CreateRect(canvas, "ScaledSpinner", new Color(0.85f, 0.45f, 0.25f),
                new Vector2(-260f, 90f), new Vector2(180f, 180f)).rectTransform;
            DemoUi.CreateLabel(canvas, "ScaledLabel", "Time.deltaTime\n（timeScale の影響を受ける）", 24, DemoUi.Faint,
                new Vector2(-260f, -60f), new Vector2(420f, 70f), TextAnchor.UpperCenter);

            var unscaled = DemoUi.CreateRect(canvas, "UnscaledSpinner", new Color(0.35f, 0.75f, 0.55f),
                new Vector2(260f, 90f), new Vector2(180f, 180f)).rectTransform;
            DemoUi.CreateLabel(canvas, "UnscaledLabel", "Time.unscaledDeltaTime\n（蓋絵はこちら基準）", 24, DemoUi.Faint,
                new Vector2(260f, -60f), new Vector2(420f, 70f), TextAnchor.UpperCenter);

            var status = DemoUi.CreateLabel(canvas, "Status", "Ready", 30, Color.white,
                new Vector2(0f, -170f), new Vector2(1400f, 40f), TextAnchor.MiddleCenter);
            var progress = DemoUi.CreateLabel(canvas, "Progress", "Progress : 0 %", 26, DemoUi.Faint,
                new Vector2(0f, -220f), new Vector2(1400f, 40f), TextAnchor.MiddleCenter);

            var load = DemoUi.CreateButton(canvas, "LoadButton", "LOAD（3 秒の重い処理）",
                new Vector2(-260f, -330f), new Vector2(460f, 74f), DemoUi.Accent);
            var pauseLoad = DemoUi.CreateButton(canvas, "PauseLoadButton", "LOAD（timeScale = 0）",
                new Vector2(260f, -330f), new Vector2(460f, 74f), DemoUi.Warn);

            var toggle = CreateLoadingToggle(canvas);

            DemoUi.AddBackToHub(canvas);

            var controllerGo = new GameObject("LoadingDemoController", typeof(LoadingDemoController));
            var controller = controllerGo.GetComponent<LoadingDemoController>();
            controller.loadButton = load;
            controller.pauseLoadButton = pauseLoad;
            controller.loadingToggle = toggle;
            controller.statusLabel = status;
            controller.progressLabel = progress;
            controller.scaledSpinner = scaled;
            controller.unscaledSpinner = unscaled;

            EditorSceneManager.SaveScene(scene, DemoUi.ScenePath(DemoScenes.Loading));
        }

        static Toggle CreateLoadingToggle(Transform canvas)
        {
            var go = new GameObject("LoadingToggle", typeof(RectTransform), typeof(Toggle));
            go.transform.SetParent(canvas, false);

            var rt = (RectTransform)go.transform;
            rt.anchoredPosition = new Vector2(0f, -420f);
            rt.sizeDelta = new Vector2(460f, 48f);

            var box = DemoUi.CreateRect(go.transform, "Box", new Color(0.9f, 0.9f, 0.9f),
                new Vector2(-200f, 0f), new Vector2(32f, 32f));
            box.raycastTarget = true;

            var check = DemoUi.CreateRect(box.transform, "Checkmark", new Color(0.2f, 0.7f, 0.45f),
                Vector2.zero, new Vector2(20f, 20f));

            DemoUi.CreateLabel(go.transform, "Label", "LoadingIndicator を表示する", 24, Color.white,
                new Vector2(30f, 0f), new Vector2(400f, 40f), TextAnchor.MiddleLeft);

            var toggle = go.GetComponent<Toggle>();
            toggle.targetGraphic = box;
            toggle.graphic = check;
            toggle.isOn = true;
            return toggle;
        }

        static T CreateCurtain<T>(Transform parent, string name, string displayName) where T : ObjectCurtain
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(T));
            go.transform.SetParent(parent, false);
            DemoUi.Stretch((RectTransform)go.transform);

            var curtain = go.GetComponent<T>();
            curtain.displayName = displayName;
            return curtain;
        }
    }
}
