using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace Waribashi.ScreenTransitions.Demo
{
    /// <summary>各デモシーンからハブへ戻る。</summary>
    public class BackToHubButton : MonoBehaviour
    {
        public Button button;

        bool _running;

        void Start()
        {
            if (button != null)
                button.onClick.AddListener(() => Back().Forget());
        }

        async UniTaskVoid Back()
        {
            if (_running)
                return;
            _running = true;

            var curtain = ScreenTransitionService.Instance.Use<FadeTransition>();
            await DemoScenes.GoAsync(curtain, DemoScenes.Hub);
        }
    }
}
