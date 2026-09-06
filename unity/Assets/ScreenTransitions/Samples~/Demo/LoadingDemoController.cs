using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using Waribashi.ScreenTransitions.Demo.Submarine;

namespace Waribashi.ScreenTransitions.Demo
{
    /// <summary>
    /// 実運用に近い使い方のデモ。蓋絵の裏で重い処理を回す。
    ///
    /// 確かめられること。
    /// - 閉じ切っている間 HoldLoop が回るので、ロード中に画面が静止しない
    /// - LoadingIndicator は蓋絵より前面に出る
    /// - timeScale = 0 にしても蓋絵は完走する（すべて unscaledDeltaTime 基準）。
    ///   左の四角は Time.deltaTime、右は Time.unscaledDeltaTime で回しているので違いが見える
    /// </summary>
    public class LoadingDemoController : MonoBehaviour
    {
        public Button loadButton;
        public Button pauseLoadButton;
        public Toggle loadingToggle;
        public Text statusLabel;
        public Text progressLabel;
        public RectTransform scaledSpinner;
        public RectTransform unscaledSpinner;
        public float workSeconds = 3f;

        bool _running;

        void Start()
        {
            if (loadButton != null)
                loadButton.onClick.AddListener(() => Run(false).Forget());

            if (pauseLoadButton != null)
                pauseLoadButton.onClick.AddListener(() => Run(true).Forget());

            SetStatus("Ready");
            SetProgress(0f);
        }

        void OnDestroy()
        {
            // デモ途中で抜けても timeScale を戻しておく
            Time.timeScale = 1f;
        }

        void Update()
        {
            if (scaledSpinner != null)
                scaledSpinner.Rotate(0f, 0f, -140f * Time.deltaTime);

            if (unscaledSpinner != null)
                unscaledSpinner.Rotate(0f, 0f, -140f * Time.unscaledDeltaTime);
        }

        async UniTaskVoid Run(bool freezeTimeScale)
        {
            if (_running)
                return;
            _running = true;

            // HoldLoop を実装している蓋絵を選ぶ。閉じ切っている間も動き続けることがこのデモの主題
            // （FadeTransition は HoldLoop を持たないので、閉じている間は本当に静止画になる）
            var curtain = ScreenTransitionService.Instance.Use<SonarPingTransition>();
            bool showLoading = loadingToggle == null || loadingToggle.isOn;

            if (freezeTimeScale)
            {
                Time.timeScale = 0f;
                SetStatus("timeScale = 0 で実行中（左の四角が止まる）");
            }
            else
            {
                SetStatus("ロード中（HoldLoop が回り続ける）");
            }

            try
            {
                await ScreenTransitionService.Instance.RunAsync(curtain, SimulateHeavyWork, showLoading);
            }
            finally
            {
                Time.timeScale = 1f;
            }

            SetStatus("Ready");
            SetProgress(0f);
            _running = false;
        }

        /// <summary>重いロードの代わり。進捗を出しながら workSeconds かけて進む。</summary>
        async UniTask SimulateHeavyWork(CancellationToken ct)
        {
            float elapsed = 0f;
            while (elapsed < workSeconds)
            {
                elapsed += Time.unscaledDeltaTime;
                SetProgress(Mathf.Clamp01(elapsed / workSeconds));
                await UniTask.Yield(PlayerLoopTiming.Update, ct);
            }

            SetProgress(1f);
        }

        void SetStatus(string text)
        {
            if (statusLabel != null)
                statusLabel.text = text;
        }

        void SetProgress(float ratio)
        {
            if (progressLabel != null)
                progressLabel.text = "Progress : " + Mathf.RoundToInt(ratio * 100f) + " %";
        }
    }
}
