using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.TestTools;
using UnityEngine.Timeline;
using UnityEngine.UI;

namespace Waribashi.ScreenTransitions.Tests
{
    /// <summary>
    /// RuleImageCurtain が ICurtain の契約を守っているかを検証する。
    ///
    /// ObjectCurtain 側は派生型を反射で列挙する契約テストがあるが、こちらは Timeline アセットと
    /// TransitionImage の参照が要るため対象外だった。Timeline をメモリ上に組んで埋める。
    /// </summary>
    [TestFixture]
    public class RuleImageCurtainContractTests
    {
        const int TimeoutMs = 30000;
        const float ClipDuration = 0.5f;

        GameObject _canvasGo;
        readonly List<UnityEngine.Object> _assets = new List<UnityEngine.Object>();

        [SetUp]
        public void SetUp()
        {
            _canvasGo = new GameObject("TestCanvas", typeof(Canvas), typeof(CanvasScaler));
            _canvasGo.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        }

        [TearDown]
        public void TearDown()
        {
            if (_canvasGo != null)
                UnityEngine.Object.Destroy(_canvasGo);

            foreach (var asset in _assets)
                UnityEngine.Object.Destroy(asset);
            _assets.Clear();
        }

        RuleImageCurtain CreateCurtain()
        {
            var imageGo = new GameObject("TransitionImage", typeof(RectTransform), typeof(Image), typeof(TransitionImage));
            imageGo.transform.SetParent(_canvasGo.transform, false);
            Stretch((RectTransform)imageGo.transform);

            var curtainGo = new GameObject("RuleImageCurtain", typeof(RectTransform), typeof(PlayableDirector), typeof(RuleImageCurtain));
            curtainGo.transform.SetParent(_canvasGo.transform, false);

            var curtain = curtainGo.GetComponent<RuleImageCurtain>();
            curtain.Target = imageGo.GetComponent<TransitionImage>();
            curtain.CloseTimeline = CreateTimeline(0f, 1f);
            curtain.OpenTimeline = CreateTimeline(1f, 0f);
            return curtain;
        }

        /// <summary>Cutoff を from -> to へ動かすだけの Timeline を、アセット化せずに組む。</summary>
        TimelineAsset CreateTimeline(float from, float to)
        {
            var timeline = ScriptableObject.CreateInstance<TimelineAsset>();
            _assets.Add(timeline);

            var track = timeline.CreateTrack<TransitionTrack>(null, "Transition");
            var clip = track.CreateClip<TransitionClip>();
            clip.start = 0.0;
            clip.duration = ClipDuration;

            var asset = (TransitionClip)clip.asset;
            asset.template.from = from;
            asset.template.to = to;
            asset.template.easing = AnimationCurve.Linear(0f, 0f, 1f, 1f);
            return timeline;
        }

        static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        static UniTask WaitSeconds(float seconds)
            => UniTask.Delay(TimeSpan.FromSeconds(seconds), DelayType.UnscaledDeltaTime);

        // ------------------------------------------------------------------
        // ライフサイクル
        // ------------------------------------------------------------------

        [UnityTest, Timeout(TimeoutMs)]
        public IEnumerator Lifecycle_CloseThenOpen_DrivesCutoffToBothEnds()
            => UniTask.ToCoroutine(async () =>
        {
            var curtain = CreateCurtain();
            Assert.AreEqual(CurtainState.Open, curtain.State, "生成直後は Open であること");

            await curtain.CloseAsync();

            Assert.AreEqual(CurtainState.Closed, curtain.State, "CloseAsync 完了後は Closed であること");
            Assert.AreEqual(1f, curtain.Target.Cutoff, 0.0001f, "閉じ切ったら Cutoff は 1 であること");

            await curtain.OpenAsync();

            Assert.AreEqual(CurtainState.Open, curtain.State, "OpenAsync 完了後は Open であること");
            Assert.AreEqual(0f, curtain.Target.Cutoff, 0.0001f, "開き切ったら Cutoff は 0 であること");
        });

        /// <summary>Busy 中の再入は無視される。</summary>
        [UnityTest, Timeout(TimeoutMs)]
        public IEnumerator Reentrancy_WhileBusy_IsIgnored()
            => UniTask.ToCoroutine(async () =>
        {
            var curtain = CreateCurtain();

            var closing = curtain.CloseAsync();
            await UniTask.Yield();

            Assert.AreEqual(CurtainState.Closing, curtain.State);
            await curtain.OpenAsync();   // Closed ではないので何もしない
            Assert.AreEqual(CurtainState.Closing, curtain.State, "再入で状態が壊れないこと");

            await closing;
            await curtain.OpenAsync();
        });

        // ------------------------------------------------------------------
        // 中断（キャンセル）
        // ------------------------------------------------------------------

        /// <summary>
        /// 閉じ中のキャンセルは Open に着地し、画面に何も残さない。
        /// 以前は State だけ Open に戻して Cutoff を中途の値のまま残していた。
        /// </summary>
        [UnityTest, Timeout(TimeoutMs)]
        public IEnumerator Cancellation_DuringClose_LandsInOpenWithNothingCovering()
            => UniTask.ToCoroutine(async () =>
        {
            var curtain = CreateCurtain();
            using var cts = new CancellationTokenSource();

            var closing = curtain.CloseAsync(cts.Token);
            await WaitSeconds(ClipDuration * 0.3f);

            Assert.AreEqual(CurtainState.Closing, curtain.State, "前提: まだ閉じている途中であること");
            Assert.Greater(curtain.Target.Cutoff, 0f, "前提: 途中まで覆っていること");

            cts.Cancel();
            await AssertCanceled(closing);

            Assert.AreEqual(CurtainState.Open, curtain.State, "Open に着地すること");
            Assert.AreEqual(0f, curtain.Target.Cutoff, 0.0001f, "画面に何も残っていないこと");
        });

        /// <summary>
        /// 開き中のキャンセルも Open に着地する。
        /// 以前は Closed へ戻してループを再開しており、ObjectCurtain と着地先が食い違っていた。
        /// </summary>
        [UnityTest, Timeout(TimeoutMs)]
        public IEnumerator Cancellation_DuringOpen_LandsInOpenWithNothingCovering()
            => UniTask.ToCoroutine(async () =>
        {
            var curtain = CreateCurtain();
            await curtain.CloseAsync();

            using var cts = new CancellationTokenSource();
            var opening = curtain.OpenAsync(cts.Token);
            await WaitSeconds(ClipDuration * 0.3f);

            Assert.AreEqual(CurtainState.Opening, curtain.State, "前提: まだ開いている途中であること");

            cts.Cancel();
            await AssertCanceled(opening);

            Assert.AreEqual(CurtainState.Open, curtain.State, "Closed ではなく Open に着地すること");
            Assert.AreEqual(0f, curtain.Target.Cutoff, 0.0001f, "画面に何も残っていないこと");
        });

        /// <summary>キャンセル後も再利用できる。</summary>
        [UnityTest, Timeout(TimeoutMs)]
        public IEnumerator Cancellation_ThenReuse_CompletesNormally()
            => UniTask.ToCoroutine(async () =>
        {
            var curtain = CreateCurtain();
            using (var cts = new CancellationTokenSource())
            {
                var closing = curtain.CloseAsync(cts.Token);
                await WaitSeconds(ClipDuration * 0.3f);
                cts.Cancel();
                await AssertCanceled(closing);
            }

            await curtain.CloseAsync();

            Assert.AreEqual(CurtainState.Closed, curtain.State, "キャンセル後も普通に閉じられること");
            Assert.AreEqual(1f, curtain.Target.Cutoff, 0.0001f);

            await curtain.OpenAsync();
            Assert.AreEqual(CurtainState.Open, curtain.State);
        });

        // ------------------------------------------------------------------
        // 早送り（Complete）
        // ------------------------------------------------------------------

        [UnityTest, Timeout(TimeoutMs)]
        public IEnumerator Complete_DuringClose_FastForwardsToClosed()
            => UniTask.ToCoroutine(async () =>
        {
            var curtain = CreateCurtain();

            var closing = curtain.CloseAsync();
            await UniTask.Yield();

            Assert.AreEqual(CurtainState.Closing, curtain.State, "前提: Closing に入っていること");
            Assert.IsTrue(curtain.Complete(), "Busy 中の Complete は受け付けられること");

            int frame = Time.frameCount;
            await closing;

            Assert.AreEqual(CurtainState.Closed, curtain.State, "Closed へ着地すること");
            Assert.AreEqual(1f, curtain.Target.Cutoff, 0.0001f, "Cutoff が終端値になっていること");
            Assert.LessOrEqual(Time.frameCount - frame, 3, "Timeline の残りを待たずに終わること");

            await curtain.OpenAsync();
        });

        [UnityTest, Timeout(TimeoutMs)]
        public IEnumerator Complete_WhenNoPhaseIsRunning_IsRejected()
            => UniTask.ToCoroutine(async () =>
        {
            var curtain = CreateCurtain();

            Assert.IsFalse(curtain.Complete(), "Open 中は受け付けないこと");

            await curtain.CloseAsync();
            Assert.IsFalse(curtain.Complete(), "Closed 中は受け付けないこと");

            await curtain.OpenAsync();
        });

        /// <summary>
        /// UniTask のタスクがキャンセルされたことを確かめる。
        /// Assert.ThrowsAsync / CatchAsync はスレッドをブロックして Task の完了を待つが、
        /// UniTask の継続は Unity のプレイヤーループで進むため自己デッドロックする。使わないこと。
        /// </summary>
        static async UniTask AssertCanceled(UniTask task)
        {
            try
            {
                await task;
            }
            catch (OperationCanceledException)
            {
                return;
            }

            Assert.Fail("OperationCanceledException が飛ぶはず");
        }
    }
}
