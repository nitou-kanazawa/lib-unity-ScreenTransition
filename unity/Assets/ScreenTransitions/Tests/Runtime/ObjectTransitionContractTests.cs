using System;
using System.Collections;
using System.Threading;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Waribashi.ScreenTransitions.Tests
{
    /// <summary>
    /// ObjectTransition 派生型すべてが IScreenTransition の契約を守っているかを検証する。
    ///
    /// 検証するのは「見た目」ではなく契約（状態遷移・レイキャスト・時間軸・キャンセル）。
    /// アニメーションの曲線や外観は演出調整のたびに変わるので、意図的にアサートしない。
    /// </summary>
    [TestFixture]
    public class ObjectTransitionContractTests
    {
        /// <summary>1サイクルが 10 秒を超える遷移は無いという前提。ハングをタイムアウト失敗に変える。</summary>
        const int TimeoutMs = 30000;

        GameObject _canvasGo;

        [SetUp]
        public void SetUp()
        {
            _canvasGo = new GameObject("TestCanvas", typeof(Canvas), typeof(CanvasScaler));
            var canvas = _canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = _canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
        }

        [TearDown]
        public void TearDown()
        {
            Time.timeScale = 1f;
            if (_canvasGo != null)
                UnityEngine.Object.Destroy(_canvasGo);
        }

        /// <summary>全画面ストレッチした子として遷移を1つ生成する（ScreenTransitionService.Use&lt;T&gt; と同じ配置）。</summary>
        ObjectTransition Create(Type type)
        {
            var go = new GameObject(type.Name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(_canvasGo.transform, false);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            return (ObjectTransition)go.AddComponent(type);
        }

        // ------------------------------------------------------------------
        // ライフサイクル
        // ------------------------------------------------------------------

        /// <summary>
        /// Open → Closing → Closed → Opening → Open を2周する。
        /// 2周目を回すのは「再生ごとの状態リセット漏れ」（子要素の再生成前提が崩れるパターン）を検出するため。
        /// </summary>
        [UnityTest, Timeout(TimeoutMs)]
        public IEnumerator Lifecycle_CloseThenOpen_SurvivesTwoCycles(
            [ValueSource(typeof(TransitionTypes), nameof(TransitionTypes.All))] Type type)
            => UniTask.ToCoroutine(async () =>
        {
            var transition = Create(type);
            Assert.AreEqual(TransitionState.Open, transition.State, "生成直後は Open であること");

            for (int cycle = 1; cycle <= 2; cycle++)
            {
                await transition.CloseAsync();

                Assert.AreEqual(TransitionState.Closed, transition.State,
                    $"{cycle}周目: CloseAsync 完了後は Closed であること");
                Assert.IsTrue(transition.gameObject.activeSelf,
                    $"{cycle}周目: Closed 中は蓋がアクティブであること（画面を覆っている）");

                var group = transition.GetComponent<CanvasGroup>();
                Assert.IsNotNull(group, "基底が CanvasGroup を自動付与すること");
                Assert.IsTrue(group.blocksRaycasts,
                    $"{cycle}周目: Closed 中は下のUIへの入力を遮断すること");

                await transition.OpenAsync();

                Assert.AreEqual(TransitionState.Open, transition.State,
                    $"{cycle}周目: OpenAsync 完了後は Open に戻ること");
                Assert.IsFalse(group.blocksRaycasts,
                    $"{cycle}周目: Open 完了後は入力を塞がないこと（アルファ0の全画面 Image が残っても通す）");
            }

            LogAssert.NoUnexpectedReceived();
        });

        /// <summary>Busy（Closing / Opening）中の再入は無視され、状態を壊さない。</summary>
        [UnityTest, Timeout(TimeoutMs)]
        public IEnumerator Reentrancy_WhileBusy_IsIgnored(
            [ValueSource(typeof(TransitionTypes), nameof(TransitionTypes.All))] Type type)
            => UniTask.ToCoroutine(async () =>
        {
            var transition = Create(type);

            // await せずに走らせる → State は同期的に Closing になっている
            var closing = transition.CloseAsync();
            Assert.AreEqual(TransitionState.Closing, transition.State,
                "CloseAsync 呼び出し直後は Closing であること");

            await transition.CloseAsync();  // 再入（Open でないので即 return されるべき）
            await transition.OpenAsync();   // Closed でないので無視されるべき

            Assert.AreEqual(TransitionState.Closing, transition.State,
                "Closing 中の再入で状態が壊れないこと");

            await closing;
            Assert.AreEqual(TransitionState.Closed, transition.State,
                "再入があっても本来の Close は完走すること");

            var opening = transition.OpenAsync();
            Assert.AreEqual(TransitionState.Opening, transition.State,
                "OpenAsync 呼び出し直後は Opening であること");

            await transition.OpenAsync();   // 再入
            await transition.CloseAsync();  // Open でないので無視されるべき

            Assert.AreEqual(TransitionState.Opening, transition.State,
                "Opening 中の再入で状態が壊れないこと");

            await opening;
            Assert.AreEqual(TransitionState.Open, transition.State);

            LogAssert.NoUnexpectedReceived();
        });

        /// <summary>
        /// timeScale = 0 でも完走する。DESIGN.md の「時間は unscaled 基準」という仕様の実装テスト。
        /// Time.deltaTime を使っている実装があるとここで永久に止まり、Timeout で失敗する。
        /// </summary>
        [UnityTest, Timeout(TimeoutMs)]
        public IEnumerator Lifecycle_CompletesWhileTimeScaleIsZero(
            [ValueSource(typeof(TransitionTypes), nameof(TransitionTypes.All))] Type type)
            => UniTask.ToCoroutine(async () =>
        {
            Time.timeScale = 0f;
            var transition = Create(type);

            await transition.CloseAsync();
            Assert.AreEqual(TransitionState.Closed, transition.State,
                "timeScale=0 でも CloseAsync が完走すること");

            await transition.OpenAsync();
            Assert.AreEqual(TransitionState.Open, transition.State,
                "timeScale=0 でも OpenAsync が完走すること");

            LogAssert.NoUnexpectedReceived();
        });

        // ------------------------------------------------------------------
        // キャンセル
        // ------------------------------------------------------------------

        /// <summary>
        /// 中断後の着地状態。キャンセルは Closing / Opening のどちらで起きても
        /// 「何も覆っていない Open」に着地しなければならない。
        /// 蓋が中途姿勢のまま画面に残ると、見えているのにクリックが透ける状態になり、
        /// 利用側は状態不一致で CloseAsync も OpenAsync も弾かれて復帰できなくなる。
        /// </summary>
        /// <summary>
        /// キャンセルされたかどうかを await で判定する。
        /// NUnit の Assert.ThrowsAsync は内部でスレッドをブロックするので使えない
        /// （UniTask の継続はプレイヤーループで進むため、ブロックすると自己デッドロックする）。
        /// </summary>
        static async UniTask<bool> WasCanceled(UniTask task)
        {
            try
            {
                await task;
                return false;
            }
            catch (OperationCanceledException)
            {
                return true;
            }
        }

        static void AssertLandedInOpen(ObjectTransition transition, string context)
        {
            Assert.AreEqual(TransitionState.Open, transition.State,
                $"{context}: State は Open であること");
            Assert.IsFalse(transition.gameObject.activeSelf,
                $"{context}: 蓋のルートが非アクティブであること（中途姿勢が画面に残らない）");
            Assert.AreEqual(0, transition.transform.childCount,
                $"{context}: 中途姿勢の子要素が破棄されていること");
            Assert.IsFalse(transition.GetComponent<CanvasGroup>().blocksRaycasts,
                $"{context}: 入力を塞ぎっぱなしにしないこと");
        }

        /// <summary>閉じ中のキャンセルは Open に着地し、画面に何も残さない。</summary>
        [UnityTest, Timeout(TimeoutMs)]
        public IEnumerator Cancellation_DuringClose_LandsInOpenWithNothingVisible(
            [ValueSource(typeof(TransitionTypes), nameof(TransitionTypes.All))] Type type)
            => UniTask.ToCoroutine(async () =>
        {
            var transition = Create(type);
            using var cts = new CancellationTokenSource();

            var closing = transition.CloseAsync(cts.Token);
            await UniTask.DelayFrame(2);
            Assume.That(transition.State, Is.EqualTo(TransitionState.Closing),
                "2フレームで閉じ切る遷移は想定外（このテストは中断を作れない）");
            cts.Cancel();

            Assert.IsTrue(await WasCanceled(closing),
                "キャンセルは OperationCanceledException として呼び出し側に伝わること");

            AssertLandedInOpen(transition, "閉じ中のキャンセル");
            LogAssert.NoUnexpectedReceived();
        });

        /// <summary>
        /// 開き中のキャンセルも Open に着地する（Closed には戻さない）。
        /// 半分開いた蓋で Closed を主張すると「画面は完全に覆われている」契約を破るため。
        /// </summary>
        [UnityTest, Timeout(TimeoutMs)]
        public IEnumerator Cancellation_DuringOpen_LandsInOpenWithNothingVisible(
            [ValueSource(typeof(TransitionTypes), nameof(TransitionTypes.All))] Type type)
            => UniTask.ToCoroutine(async () =>
        {
            var transition = Create(type);
            await transition.CloseAsync();

            using var cts = new CancellationTokenSource();
            var opening = transition.OpenAsync(cts.Token);
            await UniTask.DelayFrame(2);
            Assume.That(transition.State, Is.EqualTo(TransitionState.Opening),
                "2フレームで開き切る遷移は想定外（このテストは中断を作れない）");
            cts.Cancel();

            Assert.IsTrue(await WasCanceled(opening),
                "キャンセルは OperationCanceledException として呼び出し側に伝わること");

            AssertLandedInOpen(transition, "開き中のキャンセル");
            LogAssert.NoUnexpectedReceived();
        });

        /// <summary>
        /// 中断した蓋を再利用できる。子要素を破棄して _built を降ろしているので、
        /// 次の CloseAsync は Build() からやり直して正常な姿勢で閉じ切る。
        /// </summary>
        [UnityTest, Timeout(TimeoutMs)]
        public IEnumerator Cancellation_ThenReuse_RebuildsAndCompletes(
            [ValueSource(typeof(TransitionTypes), nameof(TransitionTypes.All))] Type type)
            => UniTask.ToCoroutine(async () =>
        {
            var transition = Create(type);
            using var cts = new CancellationTokenSource();

            var closing = transition.CloseAsync(cts.Token);
            await UniTask.DelayFrame(2);
            cts.Cancel();
            try { await closing; } catch (OperationCanceledException) { }

            Assert.AreEqual(0, transition.transform.childCount, "中断直後は子要素が空であること");

            // 中断を挟んだあとの通常サイクル
            await transition.CloseAsync();
            Assert.AreEqual(TransitionState.Closed, transition.State,
                "中断後も CloseAsync が完走すること");
            Assert.Greater(transition.transform.childCount, 0,
                "Build() がやり直されて子要素が再生成されていること");

            await transition.OpenAsync();
            Assert.AreEqual(TransitionState.Open, transition.State);

            LogAssert.NoUnexpectedReceived();
        });

        // ------------------------------------------------------------------
        // HoldLoop
        // ------------------------------------------------------------------

        /// <summary>
        /// HoldLoop を override している型が、Closed 中に回り続け、OpenAsync でキャンセルされ、
        /// そのキャンセル例外を外に漏らさないこと。UniTask 化で最も壊れやすい箇所。
        /// </summary>
        [UnityTest, Timeout(TimeoutMs)]
        public IEnumerator HoldLoop_RunsWhileClosed_AndCancelsOnOpen(
            [ValueSource(typeof(TransitionTypes), nameof(TransitionTypes.WithHoldLoop))] Type type)
            => UniTask.ToCoroutine(async () =>
        {
            var transition = Create(type);

            await transition.CloseAsync();
            await UniTask.DelayFrame(60);

            Assert.AreEqual(TransitionState.Closed, transition.State,
                "HoldLoop が回っている間も Closed を維持すること（HoldLoop が状態を書き換えないこと）");

            await transition.OpenAsync();
            await UniTask.DelayFrame(5);

            Assert.AreEqual(TransitionState.Open, transition.State);
            LogAssert.NoUnexpectedReceived();
        });

        /// <summary>
        /// Closed（HoldLoop 稼働中）のまま破棄しても例外を漏らさない。
        /// シーン遷移中に蓋ごと破棄されるケースの再現。
        /// </summary>
        [UnityTest, Timeout(TimeoutMs)]
        public IEnumerator Destroy_WhileClosed_DoesNotLeakException(
            [ValueSource(typeof(TransitionTypes), nameof(TransitionTypes.All))] Type type)
            => UniTask.ToCoroutine(async () =>
        {
            var transition = Create(type);
            await transition.CloseAsync();

            UnityEngine.Object.Destroy(transition.gameObject);
            await UniTask.DelayFrame(10);

            LogAssert.NoUnexpectedReceived();
        });

        /// <summary>
        /// 閉じアニメーション再生中に破棄しても例外を漏らさない。
        /// 破棄は GetCancellationTokenOnDestroy 経由のキャンセルとして中断復帰処理に入るので、
        /// 復帰処理が破棄済みオブジェクトを触っていないことの確認も兼ねる。
        /// </summary>
        [UnityTest, Timeout(TimeoutMs)]
        public IEnumerator Destroy_WhileClosing_DoesNotLeakException(
            [ValueSource(typeof(TransitionTypes), nameof(TransitionTypes.All))] Type type)
            => UniTask.ToCoroutine(async () =>
        {
            var transition = Create(type);
            transition.CloseAsync().Forget();
            await UniTask.DelayFrame(2);

            UnityEngine.Object.Destroy(transition.gameObject);
            await UniTask.DelayFrame(10);

            LogAssert.NoUnexpectedReceived();
        });
    }
}
