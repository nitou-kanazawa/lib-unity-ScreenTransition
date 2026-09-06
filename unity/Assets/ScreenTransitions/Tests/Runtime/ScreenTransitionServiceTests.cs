using System.Collections;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Waribashi.ScreenTransitions.Tests
{
    /// <summary>
    /// 常駐サービスの取得系 API。
    /// 蓋絵の共有 / 個別生成 / 外部インスタンスの受け入れ と、ローディング表示の遅延生成を検証する。
    /// </summary>
    [TestFixture]
    public class ScreenTransitionServiceTests
    {
        const int TimeoutMs = 30000;

        [TearDown]
        public void TearDown()
        {
            // 常駐（DontDestroyOnLoad）なのでテスト間に持ち越さないよう毎回捨てる。
            // DestroyImmediate にしておくと、次のテストの Instance が確実に作り直される
            var service = Object.FindAnyObjectByType<ScreenTransitionService>();
            if (service != null)
                Object.DestroyImmediate(service.gameObject);
        }

        [Test]
        public void Use_ReturnsTheSameInstanceForTheSameType()
        {
            var service = ScreenTransitionService.Instance;

            var first = service.Use<FadeTransition>();
            var second = service.Use<FadeTransition>();

            Assert.AreSame(first, second, "Use は型ごとに 1 つを共有すること");
        }

        [Test]
        public void Create_ReturnsANewInstanceEveryTime()
        {
            var service = ScreenTransitionService.Instance;

            var first = service.Create<FadeTransition>();
            var second = service.Create<FadeTransition>();

            Assert.AreNotSame(first, second, "Create は呼ぶたびに別インスタンスを作ること");
            Assert.AreEqual(service.Layer, first.transform.parent, "常駐レイヤーの子であること");
            Assert.AreEqual(service.Layer, second.transform.parent);
        }

        [Test]
        public void Adopt_MovesAnExternallyBuiltCurtainOntoTheLayer()
        {
            var service = ScreenTransitionService.Instance;

            // Use / Create ではコードから組み立てられない蓋絵の代わり
            var go = new GameObject("External", typeof(RectTransform), typeof(FadeTransition));
            var curtain = go.GetComponent<FadeTransition>();
            Assert.AreNotEqual(service.Layer, curtain.transform.parent, "前提: まだレイヤーの外にあること");

            service.Adopt(curtain);

            Assert.AreEqual(service.Layer, curtain.transform.parent, "レイヤーの子に移ること");
        }

        [Test]
        public void Loading_IsNotBuiltUntilItIsTouched()
        {
            var service = ScreenTransitionService.Instance;

            Assert.IsEmpty(service.GetComponentsInChildren<LoadingIndicator>(true),
                "触るまでは既定のローディング表示を作らないこと（ロードを挟まないアプリに付いてこない）");

            var loading = service.Loading;

            Assert.IsNotNull(loading);
            Assert.AreEqual(1, service.GetComponentsInChildren<LoadingIndicator>(true).Length,
                "初回アクセスで 1 つだけ作られること");
        }

        [Test]
        public void Loading_CanBeReplacedWithoutBuildingTheDefault()
        {
            var service = ScreenTransitionService.Instance;
            service.Loading = new FakeLoadingIndicator();

            Assert.IsEmpty(service.GetComponentsInChildren<LoadingIndicator>(true),
                "差し替えたら既定の実装は作られないこと");
        }

        /// <summary>RunAsync が work の前後で Show / Hide を呼ぶ。</summary>
        [UnityTest, Timeout(TimeoutMs)]
        public IEnumerator RunAsync_ShowsAndHidesTheReplacedIndicator()
            => UniTask.ToCoroutine(async () =>
        {
            var service = ScreenTransitionService.Instance;
            var fake = new FakeLoadingIndicator();
            service.Loading = fake;

            var curtain = service.Use<FadeTransition>();
            bool visibleDuringWork = false;

            await service.RunAsync(curtain, async ct =>
            {
                visibleDuringWork = fake.IsVisible;
                await UniTask.Yield(PlayerLoopTiming.Update, ct);
            });

            Assert.IsTrue(visibleDuringWork, "work の実行中は表示されていること");
            Assert.AreEqual(1, fake.ShowCount, "Show は 1 回だけ呼ばれること");
            Assert.AreEqual(1, fake.HideCount, "Hide は 1 回だけ呼ばれること");
            Assert.IsFalse(fake.IsVisible, "終わったら隠されていること");
        });

        /// <summary>showLoading: false のときは Loading に触らない（＝既定の実装も作られない）。</summary>
        [UnityTest, Timeout(TimeoutMs)]
        public IEnumerator RunAsync_WithoutLoading_DoesNotBuildTheDefault()
            => UniTask.ToCoroutine(async () =>
        {
            var service = ScreenTransitionService.Instance;
            var curtain = service.Use<FadeTransition>();

            await service.RunAsync(curtain, ct => UniTask.CompletedTask, showLoading: false);

            Assert.IsEmpty(service.GetComponentsInChildren<LoadingIndicator>(true));
        });

        /// <summary>MonoBehaviour ではない実装。IsAlive の非 UnityEngine.Object 経路も通る。</summary>
        class FakeLoadingIndicator : ILoadingIndicator
        {
            public int ShowCount { get; private set; }
            public int HideCount { get; private set; }
            public bool IsVisible { get; private set; }

            public void Show()
            {
                ShowCount++;
                IsVisible = true;
            }

            public void Hide()
            {
                HideCount++;
                IsVisible = false;
            }
        }
    }
}
