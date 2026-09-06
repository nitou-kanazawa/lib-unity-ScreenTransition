using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Waribashi.ScreenTransitions.Demo.Submarine;

namespace Waribashi.ScreenTransitions.Demo
{
    /// <summary>
    /// シーン跨ぎデモの遷移先。Back ボタンで常駐サービス経由でデモシーンへ戻る。
    /// </summary>
    public class UnderseaDemoController : MonoBehaviour
    {
        public Button backButton;

        bool _running;

        void Start()
        {
            if (backButton != null)
                backButton.onClick.AddListener(() => Back().Forget());
        }

        async UniTask Back()
        {
            if (_running)
                return;
            _running = true;

            var service = ScreenTransitionService.Instance;
            var transition = service.Use<DepthZonesTransition>();
            await service.RunAsync(transition, async ct =>
            {
                await SceneManager.LoadSceneAsync("TransitionDemo").ToUniTask(cancellationToken: ct);
                await UniTask.Delay(TimeSpan.FromSeconds(1.0), DelayType.UnscaledDeltaTime, PlayerLoopTiming.Update, ct);
            });
        }
    }
}
