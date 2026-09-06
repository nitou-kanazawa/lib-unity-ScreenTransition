using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Waribashi.ScreenTransitions.Demo.Submarine;

namespace Waribashi.ScreenTransitions.Demo
{
    /// <summary>
    /// 全パターンをボタンで再生確認するデモ。
    /// 閉じ → 保持（HoldLoop / ローディング）→ 開き、を UniTask で一連再生する。
    /// 最後のボタンは ScreenTransitionService による実シーンロードのデモ。
    /// </summary>
    public class TransitionDemoController : MonoBehaviour
    {
        public RuleImageCurtain player;
        public RectTransform buttonRoot;
        public Button buttonTemplate;
        public Text statusLabel;
        public Image background;
        public Text backgroundLabel;
        public Texture2D[] patterns;
        public ObjectCurtain[] objectTransitions;
        public LoadingIndicator loadingIndicator;
        public Toggle loadingToggle;
        public float holdSeconds = 2f;

        static readonly Color SceneAColor = new Color(0.13f, 0.24f, 0.42f);
        static readonly Color SceneBColor = new Color(0.45f, 0.22f, 0.10f);

        bool _isSceneA = true;
        bool _running;

        public bool IsBusy => _running;
        public int TotalPatternCount => patterns.Length + (objectTransitions?.Length ?? 0);
        bool UseLoading => loadingToggle != null && loadingToggle.isOn;

        void Start()
        {
            foreach (var tex in patterns)
            {
                var captured = tex;
                CreateButton(DisplayName(tex.name), () => PlayPattern(captured));
            }

            if (objectTransitions != null)
            {
                for (int i = 0; i < objectTransitions.Length; i++)
                {
                    var captured = objectTransitions[i];
                    CreateButton($"{patterns.Length + i + 1:00} {captured.DisplayName}", () => PlayObject(captured));
                }
            }

            // 常駐サービス経由の実シーンロードデモ
            CreateButton(">> SCENE LOAD", () => LoadUndersea().Forget());

            SetStatus("Ready");
        }

        void CreateButton(string label, UnityEngine.Events.UnityAction onClick)
        {
            var button = Instantiate(buttonTemplate, buttonRoot);
            button.gameObject.SetActive(true);
            button.GetComponentInChildren<Text>().text = label;
            button.onClick.AddListener(onClick);
        }

        public void PlayPatternByIndex(int index)
        {
            if (index < 0)
                return;
            if (index < patterns.Length)
                PlayPattern(patterns[index]);
            else if (index < TotalPatternCount)
                PlayObject(objectTransitions[index - patterns.Length]);
        }

        public void PlayPattern(Texture2D tex)
        {
            if (_running)
                return;
            player.SetPattern(tex);
            Run(player, DisplayName(tex.name)).Forget();
        }

        public void PlayObject(ObjectCurtain transition)
        {
            if (_running)
                return;
            Run(transition, transition.DisplayName).Forget();
        }

        async UniTask Run(ICurtain transition, string name)
        {
            _running = true;
            try
            {
                SetStatus("Closing: " + name);
                await transition.CloseAsync();
                SwapScene();

                if (UseLoading)
                {
                    SetStatus("Loading: " + name);
                    loadingIndicator.Show();
                    await UniTask.Delay(TimeSpan.FromSeconds(holdSeconds), DelayType.UnscaledDeltaTime);
                    loadingIndicator.Hide();
                }
                else
                {
                    // HoldLoop が見えるよう少しだけ保持する
                    await UniTask.Delay(TimeSpan.FromSeconds(0.6), DelayType.UnscaledDeltaTime);
                }

                SetStatus("Opening: " + name);
                await transition.OpenAsync();
                SetStatus("Ready");
            }
            finally
            {
                _running = false;
            }
        }

        async UniTask LoadUndersea()
        {
            if (_running)
                return;
            _running = true;
            SetStatus("Loading scene via ScreenTransitionService...");

            var service = ScreenTransitionService.Instance;
            var transition = service.Use<WaveDiveTransition>();
            await service.RunAsync(transition, async ct =>
            {
                await SceneManager.LoadSceneAsync("TransitionUndersea").ToUniTask(cancellationToken: ct);
                // 疑似ロード時間（HoldLoop + ローディング表示の確認用）
                await UniTask.Delay(TimeSpan.FromSeconds(1.5), DelayType.UnscaledDeltaTime, PlayerLoopTiming.Update, ct);
            });
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

        static string DisplayName(string texName)
        {
            if (texName.StartsWith("Rule_"))
                texName = texName.Substring(5);
            return texName.Replace('_', ' ');
        }
    }
}
