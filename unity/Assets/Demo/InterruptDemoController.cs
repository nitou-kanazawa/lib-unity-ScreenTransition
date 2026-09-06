using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace Waribashi.ScreenTransitions.Demo
{
    /// <summary>
    /// 中断のデモ。キャンセルと早送り（Complete）は向きが逆であることを実地で確かめる。
    ///
    ///   キャンセル : 巻き戻して必ず Open へ着地する（なかったことにする）
    ///   Complete  : 早送りしてフェーズの終端へ着地する（最後まで進める）
    ///
    /// RUN で「閉じる → 3 秒保持 → 開く」を始め、その最中に COMPLETE / CANCEL を押す。
    /// State の推移とログで違いが見える。Closed（保持中）の Complete が拒否されることも確認できる。
    /// </summary>
    public class InterruptDemoController : MonoBehaviour
    {
        public ObjectCurtain[] curtains;
        public Button[] curtainButtons;
        public Button runButton;
        public Button completeButton;
        public Button cancelButton;
        public Text stateLabel;
        public Text logLabel;
        public Text targetLabel;
        public float holdSeconds = 3f;

        readonly List<string> _log = new List<string>();
        CancellationTokenSource _cts;
        ObjectCurtain _target;
        bool _running;

        void Start()
        {
            _target = curtains.Length > 0 ? curtains[0] : null;

            for (int i = 0; i < curtainButtons.Length && i < curtains.Length; i++)
            {
                int index = i;
                curtainButtons[i].onClick.AddListener(() => SelectTarget(index));
            }

            if (runButton != null)
                runButton.onClick.AddListener(() => Run().Forget());

            if (completeButton != null)
                completeButton.onClick.AddListener(RequestComplete);

            if (cancelButton != null)
                cancelButton.onClick.AddListener(RequestCancel);

            Log("RUN を押してから COMPLETE / CANCEL を試す");
            UpdateTargetLabel();
        }

        void OnDestroy()
        {
            _cts?.Cancel();
            _cts?.Dispose();
        }

        void Update()
        {
            if (stateLabel != null && _target != null)
                stateLabel.text = "State : " + _target.State;
        }

        void SelectTarget(int index)
        {
            if (_running)
                return;

            _target = curtains[index];
            UpdateTargetLabel();
            Log("対象を " + _target.DisplayName + " に変更");
        }

        void UpdateTargetLabel()
        {
            if (targetLabel != null && _target != null)
                targetLabel.text = "対象 : " + _target.DisplayName
                    + "   BlocksRaycasts = " + _target.BlocksRaycasts;
        }

        async UniTaskVoid Run()
        {
            if (_running || _target == null)
                return;
            _running = true;

            _cts?.Dispose();
            _cts = new CancellationTokenSource();

            Log("--- RunAsync 開始（閉じる → " + holdSeconds + "s 保持 → 開く）");
            try
            {
                await _target.RunAsync(holdSeconds, ct: _cts.Token);
                Log("完了。State = " + _target.State);
            }
            catch (OperationCanceledException)
            {
                // 中断は必ず Open に着地する。蓋も子要素も残らない
                Log("キャンセルで抜けた。State = " + _target.State
                    + " / 子要素 = " + _target.transform.childCount);
            }

            _running = false;
        }

        /// <summary>実行中のフェーズを終端へ早送りする。</summary>
        void RequestComplete()
        {
            if (_target == null)
                return;

            var before = _target.State;
            bool accepted = _target.Complete();

            Log(accepted
                ? "Complete() 受付。" + before + " の残りを飛ばして終端へ"
                : "Complete() 拒否。" + before + " は実行中のフェーズではない");
        }

        /// <summary>巻き戻して Open へ戻す。</summary>
        void RequestCancel()
        {
            if (_cts == null || _cts.IsCancellationRequested)
            {
                Log("Cancel : 走っていない");
                return;
            }

            Log("Cancel 要求（" + _target.State + " 中）");
            _cts.Cancel();
        }

        void Log(string line)
        {
            _log.Add(line);
            if (_log.Count > 7)
                _log.RemoveAt(0);

            if (logLabel != null)
                logLabel.text = string.Join("\n", _log);
        }
    }
}
