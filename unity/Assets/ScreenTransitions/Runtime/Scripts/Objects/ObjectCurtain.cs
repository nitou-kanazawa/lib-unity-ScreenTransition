using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace Waribashi.ScreenTransitions
{
    public static class Ease
    {
        public static float OutCubic(float t) => 1f - Mathf.Pow(1f - t, 3f);
        public static float InCubic(float t) => t * t * t;
        public static float OutQuart(float t) => 1f - Mathf.Pow(1f - t, 4f);
        public static float InQuart(float t) => t * t * t * t;

        public static float OutBack(float t)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
        }

        public static float InBack(float t)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            return c3 * t * t * t - c1 * t * t;
        }

        public static float OutBounce(float t)
        {
            const float n1 = 7.5625f;
            const float d1 = 2.75f;
            if (t < 1f / d1) return n1 * t * t;
            if (t < 2f / d1) { t -= 1.5f / d1; return n1 * t * t + 0.75f; }
            if (t < 2.5f / d1) { t -= 2.25f / d1; return n1 * t * t + 0.9375f; }
            t -= 2.625f / d1;
            return n1 * t * t + 0.984375f;
        }
    }

    /// <summary>
    /// オブジェクトベース（RectTransform を直接動かす）トランジションの基底。
    /// 派生クラスは CloseRoutine / OpenRoutine（任意で HoldLoop）を実装するだけでよい。
    /// - 子要素は初回再生時に手続き生成（Build）
    /// - Closed 中は HoldLoop が自動再生され、OpenAsync で自動キャンセルされる
    /// - CanvasGroup により再生中・閉じ中のみ下のUIへのレイキャストを遮断する（BlocksRaycasts で opt-out 可能）
    /// - 時間は unscaled 基準
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public abstract class ObjectCurtain : MonoBehaviour, ICurtain
    {
        public string displayName;

        public string DisplayName => string.IsNullOrEmpty(displayName) ? GetType().Name : displayName;

        /// <summary>
        /// 再生中および Closed 中に、下位 UI へのレイキャストを遮断するか。既定は true。
        ///
        /// 画面を覆い切る蓋絵では遮断が正しいが、画面の一部しか覆わない蓋絵
        /// （キャラクターのカットインなど）では、覆っていない領域の入力まで殺してしまう。
        /// そういう実装は false を返すこと。
        /// </summary>
        public virtual bool BlocksRaycasts => true;

        public CurtainState State { get; private set; } = CurtainState.Open;

        bool _built;
        bool _completeRequested;
        CanvasGroup _group;
        CancellationTokenSource _phaseCts;
        CancellationTokenSource _holdCts;

        protected RectTransform Root => (RectTransform)transform;

        /// <summary>現在の Close/Open フェーズのキャンセルトークン。Tween が暗黙に使う。</summary>
        protected CancellationToken PhaseToken { get; private set; }

        public async UniTask CloseAsync(CancellationToken ct = default)
        {
            if (State != CurtainState.Open)
                return;

            // アイドル中は非アクティブ化してあるので、再生開始時に起こす
            if (!gameObject.activeSelf)
                gameObject.SetActive(true);
            EnsureBuilt();
            _group.blocksRaycasts = BlocksRaycasts;
            State = CurtainState.Closing;
            BeginPhase(ct);
            try
            {
                await CloseRoutine();
                _completeRequested = false;
                State = CurtainState.Closed;
                StartHoldLoop();
            }
            catch
            {
                ResetToOpen();
                throw;
            }
        }

        public async UniTask OpenAsync(CancellationToken ct = default)
        {
            if (State != CurtainState.Closed)
                return;

            StopHoldLoop();
            State = CurtainState.Opening;
            BeginPhase(ct);
            try
            {
                await OpenRoutine();
                _completeRequested = false;
                State = CurtainState.Open;
                _group.blocksRaycasts = false;
                // アイドル中は Canvas に頂点を残さない
                gameObject.SetActive(false);
            }
            catch
            {
                ResetToOpen();
                throw;
            }
        }

        /// <summary>
        /// 実行中のフェーズ（Closing / Opening）を終端まで早送りする。
        ///
        /// キャンセルとは向きが逆であることに注意。
        ///   キャンセル : 巻き戻して Open へ着地する（「なかったことにする」）
        ///   Complete  : 早送りして そのフェーズの終端へ着地する（「最後まで進める」）
        /// Closing 中なら Closed、Opening 中なら Open に、待たずに到達する。
        /// 着地後の状態は通常再生と同じで、中断復帰の経路（子要素の破棄）は通らない。
        ///
        /// Closed 中（HoldLoop 再生中）は受け付けない。そこで待っているのがロードなのか
        /// ただの間なのかは呼び出し側にしか判断できないため。飛ばしたい場合は OpenAsync を呼ぶこと。
        /// </summary>
        /// <returns>受け付けたら true。Open / Closed（= 実行中のフェーズが無い）なら false。</returns>
        public bool Complete()
        {
            if (State != CurtainState.Closing && State != CurtainState.Opening)
                return false;

            _completeRequested = true;
            return true;
        }

        /// <summary>
        /// 中断（キャンセル・例外）時に「何も覆っていない Open」へ確実に戻す。
        ///
        /// 中断は Closing / Opening のどちらでも起こりうるが、着地先は常に Open にする。
        /// 蓋が残ったまま Closed を主張すると、利用側は CloseAsync も OpenAsync も
        /// 状態不一致で弾かれて画面を復帰できなくなるため。
        ///
        /// 子要素は中途姿勢のまま残っているので破棄し、_built を降ろして
        /// 次回 CloseAsync が Build() からやり直すようにする。
        /// </summary>
        void ResetToOpen()
        {
            StopHoldLoop();

            // 破棄によるキャンセルではもう触れない
            if (this == null)
                return;

            DiscardChildren();
            _built = false;

            if (_group != null)
                _group.blocksRaycasts = false;

            State = CurtainState.Open;
            gameObject.SetActive(false);
        }

        /// <summary>
        /// 子要素を切り離して破棄する。Destroy はフレーム末まで遅延するので、
        /// 先に親から外して同フレーム内の再 Build と二重に並ばないようにする。
        /// （Canvas 配下から外れた Graphic は描画されないので、破棄までの1フレームも見えない）
        /// </summary>
        void DiscardChildren()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                var child = transform.GetChild(i);
                child.SetParent(null, false);
                Destroy(child.gameObject);
            }
        }

        /// <summary>閉じアニメーション。Tween は PhaseToken で自動キャンセルされる。</summary>
        protected abstract UniTask CloseRoutine();

        /// <summary>開きアニメーション。</summary>
        protected abstract UniTask OpenRoutine();

        /// <summary>
        /// Closed 中に回るループアニメ。省略可（デフォルトは静止）。
        /// OpenAsync / 破棄で ct がキャンセルされるので、無限ループで書いてよい。
        /// </summary>
        protected virtual UniTask HoldLoop(CancellationToken ct) => UniTask.CompletedTask;

        void EnsureBuilt()
        {
            if (_built)
                return;

            _group = GetComponent<CanvasGroup>();
            if (_group == null)
                _group = gameObject.AddComponent<CanvasGroup>();
            _group.blocksRaycasts = false;

            Build();
            _built = true;
        }

        void BeginPhase(CancellationToken ct)
        {
            _completeRequested = false;
            _phaseCts?.Dispose();
            _phaseCts = CancellationTokenSource.CreateLinkedTokenSource(ct, this.GetCancellationTokenOnDestroy());
            PhaseToken = _phaseCts.Token;
        }

        void StartHoldLoop()
        {
            StopHoldLoop();
            _holdCts = CancellationTokenSource.CreateLinkedTokenSource(this.GetCancellationTokenOnDestroy());
            RunHoldLoop(_holdCts.Token).Forget();
        }

        async UniTaskVoid RunHoldLoop(CancellationToken ct)
        {
            try
            {
                await HoldLoop(ct);
            }
            catch (OperationCanceledException)
            {
                // OpenAsync / 破棄によるキャンセルは正常系
            }
        }

        void StopHoldLoop()
        {
            if (_holdCts != null)
            {
                _holdCts.Cancel();
                _holdCts.Dispose();
                _holdCts = null;
            }
        }

        void OnDestroy()
        {
            StopHoldLoop();
            _phaseCts?.Dispose();
        }

        protected abstract void Build();

        /// <summary>
        /// elapsed 秒（unscaled）を毎フレーム渡しながら duration 秒回す。最後に必ず duration ちょうどで呼ぶ。
        ///
        /// Complete() が要求されている間は待たずに apply(duration) だけ呼んで返る。
        /// Close/Open のルーチンが Tween を数珠つなぎにしている場合、残り全部が同じフレームで
        /// 終端まで進むので、派生クラス側に早送りの実装は要らない。
        /// </summary>
        protected async UniTask Tween(float duration, Action<float> apply)
        {
            var ct = PhaseToken;
            float elapsed = 0f;
            while (elapsed < duration && !_completeRequested)
            {
                ct.ThrowIfCancellationRequested();
                elapsed += Time.unscaledDeltaTime;
                apply(Mathf.Min(elapsed, duration));
                await UniTask.Yield(PlayerLoopTiming.Update, ct);
            }

            if (_completeRequested)
                apply(duration);
        }

        /// <summary>
        /// ct 駆動の Tween。HoldLoop など、フェーズの外で回すループ向け。
        /// こちらは Complete() の影響を受けない（ループの早送りには意味が無いため）。
        /// </summary>
        protected static async UniTask Tween(float duration, Action<float> apply, CancellationToken ct)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                ct.ThrowIfCancellationRequested();
                elapsed += Time.unscaledDeltaTime;
                apply(Mathf.Min(elapsed, duration));
                await UniTask.Yield(PlayerLoopTiming.Update, ct);
            }
        }

        protected static Image CreateRect(Transform parent, string name, Color color)
        {
            var go = new GameObject(name, typeof(Image));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.color = color;
            return image;
        }

        protected static Image CreateSprite(Transform parent, string name, Sprite sprite, Color color)
        {
            var image = CreateRect(parent, name, color);
            image.sprite = sprite;
            image.raycastTarget = false;
            return image;
        }

        protected static Text CreateLabel(Transform parent, string name, string content, int fontSize, Color color)
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

        protected static void StretchChild(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        protected static float Hash01(int x, int y)
        {
            uint h = (uint)(x * 374761393 + y * 668265263);
            h = (h ^ (h >> 13)) * 1274126177u;
            h ^= h >> 16;
            return (h & 0xFFFFFF) / (float)0x1000000;
        }
    }
}
