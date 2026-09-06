using System;
using System.Reflection;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Waribashi.ScreenTransitions.Demo.Submarine;

namespace Waribashi.ScreenTransitions.Demo
{
    /// <summary>
    /// 同梱している蓋絵をすべて再生できるカタログ。
    ///
    /// 「閉じる → 保持（HoldLoop / ローディング）→ 開く」を一連で再生し、
    /// 閉じ切っている間に背景を差し替える（＝遷移で隠れていることの確認）。
    /// 再生した蓋絵の素性（種別・出所・HoldLoop の有無・レイキャスト遮断）を右上に出す。
    /// </summary>
    public class TransitionDemoController : MonoBehaviour
    {
        public RuleImageCurtain player;

        public Texture2D[] patterns;
        public Button[] patternButtons;
        public ObjectCurtain[] objectCurtains;
        public Button[] curtainButtons;

        public Button sceneLoadButton;
        public Toggle loadingToggle;

        public Text statusLabel;
        public Text infoTitleLabel;
        public Text infoBodyLabel;

        public Image background;
        public Text backgroundLabel;
        public LoadingIndicator loadingIndicator;

        public float holdSeconds = 2f;

        static readonly Color SceneAColor = new Color(0.13f, 0.24f, 0.42f);
        static readonly Color SceneBColor = new Color(0.45f, 0.22f, 0.10f);
        static readonly Color ButtonIdle = new Color(0.15f, 0.15f, 0.18f, 0.92f);
        static readonly Color ButtonActive = new Color(0.20f, 0.42f, 0.60f, 0.98f);

        bool _isSceneA = true;
        bool _running;
        Image _activeButton;

        public bool IsBusy => _running;
        public int TotalPatternCount => patterns.Length + (objectCurtains?.Length ?? 0);

        bool UseLoading => loadingToggle != null && loadingToggle.isOn;

        void Start()
        {
            for (int i = 0; i < patterns.Length && i < patternButtons.Length; i++)
            {
                int index = i;
                patternButtons[i].onClick.AddListener(() => PlayPattern(index));
            }

            for (int i = 0; i < objectCurtains.Length && i < curtainButtons.Length; i++)
            {
                int index = i;
                curtainButtons[i].onClick.AddListener(() => PlayCurtain(index));
            }

            if (sceneLoadButton != null)
                sceneLoadButton.onClick.AddListener(() => LoadUndersea().Forget());

            SetStatus("Ready");
            ShowIntro();
        }

        // ------------------------------------------------------------------
        // 再生
        // ------------------------------------------------------------------

        /// <summary>DemoAutoCapture 用。ルール画像 → オブジェクト系 の通し番号で再生する。</summary>
        public void PlayPatternByIndex(int index)
        {
            if (index < 0)
                return;

            if (index < patterns.Length)
                PlayPattern(index);
            else if (index < TotalPatternCount)
                PlayCurtain(index - patterns.Length);
        }

        void PlayPattern(int index)
        {
            if (_running)
                return;

            var pattern = patterns[index];
            player.SetPattern(pattern);
            ShowPatternInfo(pattern);
            Highlight(index < patternButtons.Length ? patternButtons[index] : null);
            Run(player, DisplayName(pattern.name)).Forget();
        }

        void PlayCurtain(int index)
        {
            if (_running)
                return;

            var curtain = objectCurtains[index];
            ShowCurtainInfo(curtain);
            Highlight(index < curtainButtons.Length ? curtainButtons[index] : null);
            Run(curtain, curtain.DisplayName).Forget();
        }

        async UniTask Run(ICurtain curtain, string name)
        {
            _running = true;
            try
            {
                SetStatus("Closing : " + name);
                await curtain.CloseAsync();

                // 閉じ切っている間に背景を差し替える。開いたときに変わっていれば覆えていた証拠
                SwapScene();

                if (UseLoading)
                {
                    SetStatus("Loading : " + name);
                    loadingIndicator.Show();
                    await UniTask.Delay(TimeSpan.FromSeconds(holdSeconds), DelayType.UnscaledDeltaTime);
                    loadingIndicator.Hide();
                }
                else
                {
                    // HoldLoop が見えるよう少しだけ保持する
                    await UniTask.Delay(TimeSpan.FromSeconds(0.6), DelayType.UnscaledDeltaTime);
                }

                SetStatus("Opening : " + name);
                await curtain.OpenAsync();
                SetStatus("Ready");
            }
            finally
            {
                _running = false;
            }
        }

        /// <summary>常駐サービス経由の実シーンロード。蓋絵を跨いでシーンが入れ替わる。</summary>
        async UniTask LoadUndersea()
        {
            if (_running)
                return;
            _running = true;

            SetStatus("ScreenTransitionService でシーンをロード中…");
            SetInfo("SCENE LOAD",
                "常駐サービス（DontDestroyOnLoad の最前面 Canvas）経由\n"
                + "蓋が閉じている間に SceneManager.LoadSceneAsync を挟む\n"
                + "蓋絵はシーンをまたいで生き残る");

            var service = ScreenTransitionService.Instance;
            var curtain = service.Use<WaveDiveTransition>();

            await service.RunAsync(curtain, async ct =>
            {
                await SceneManager.LoadSceneAsync("TransitionUndersea").ToUniTask(cancellationToken: ct);
                // 疑似ロード時間（HoldLoop + ローディング表示の確認用）
                await UniTask.Delay(TimeSpan.FromSeconds(1.5), DelayType.UnscaledDeltaTime, PlayerLoopTiming.Update, ct);
            });
        }

        // ------------------------------------------------------------------
        // 表示
        // ------------------------------------------------------------------

        void ShowIntro()
        {
            SetInfo("ボタンを押すと再生します",
                "左から ルール画像 / 汎用 / 潜水艦 / 没入系 の順\n"
                + "潜水艦と没入系はパッケージ本体に含まれません\n"
                + "閉じ切っている間に背景が入れ替わります");
        }

        void ShowPatternInfo(Texture2D pattern)
        {
            SetInfo(DisplayName(pattern.name),
                "ルール画像系 / パッケージ本体\n"
                + "TransitionImage をシェーダーで塗り、Cutoff を Timeline で駆動する\n"
                + "パターン追加はルール画像を 1 枚足すだけ");
        }

        void ShowCurtainInfo(ObjectCurtain curtain)
        {
            var type = curtain.GetType();
            bool inPackage = type.Assembly == typeof(ObjectCurtain).Assembly;

            SetInfo(type.Name,
                "オブジェクト系 / " + (inPackage ? "パッケージ本体" : "サンプル（テーマ固有）") + "\n"
                + "HoldLoop : " + (OverridesHoldLoop(type) ? "あり（閉じ切っている間も動く）" : "なし（閉じ切ったら静止）") + "\n"
                + "BlocksRaycasts : " + (curtain.BlocksRaycasts ? "true" : "false"));
        }

        /// <summary>HoldLoop を override しているか。契約テストと同じ判定。</summary>
        static bool OverridesHoldLoop(Type type)
        {
            var method = type.GetMethod("HoldLoop",
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            return method != null && method.DeclaringType != typeof(ObjectCurtain);
        }

        void SetInfo(string title, string body)
        {
            if (infoTitleLabel != null)
                infoTitleLabel.text = title;
            if (infoBodyLabel != null)
                infoBodyLabel.text = body;
        }

        /// <summary>直前に押したボタンだけ色を変える。どれを見ているのか分かるように。</summary>
        void Highlight(Button button)
        {
            if (_activeButton != null)
                _activeButton.color = ButtonIdle;

            _activeButton = button != null ? button.GetComponent<Image>() : null;

            if (_activeButton != null)
                _activeButton.color = ButtonActive;
        }

        void SwapScene()
        {
            _isSceneA = !_isSceneA;

            if (background != null)
                background.color = _isSceneA ? SceneAColor : SceneBColor;

            if (backgroundLabel != null)
                backgroundLabel.text = _isSceneA ? "SCENE A" : "SCENE B";
        }

        void SetStatus(string message)
        {
            if (statusLabel != null)
                statusLabel.text = message;
        }

        static string DisplayName(string textureName)
        {
            if (textureName.StartsWith("Rule_"))
                textureName = textureName.Substring(5);
            return textureName.Replace('_', ' ');
        }
    }
}
