using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace Waribashi.ScreenTransitions.Demo
{
    /// <summary>
    /// デモの入口。各デモシーンへ蓋絵を挟んで飛ぶ。
    /// シーンをまたぐので、蓋絵は常駐サービス（DontDestroyOnLoad の最前面 Canvas）から取る。
    /// </summary>
    public class DemoHubController : MonoBehaviour
    {
        [System.Serializable]
        public class Entry
        {
            public Button button;
            public string sceneName;
        }

        public Entry[] entries;

        bool _running;

        void Start()
        {
            foreach (var entry in entries)
            {
                if (entry.button == null || string.IsNullOrEmpty(entry.sceneName))
                    continue;

                var scene = entry.sceneName;
                entry.button.onClick.AddListener(() => Go(scene).Forget());
            }
        }

        async UniTaskVoid Go(string sceneName)
        {
            if (_running)
                return;
            _running = true;

            var curtain = ScreenTransitionService.Instance.Use<FadeTransition>();
            await DemoScenes.GoAsync(curtain, sceneName);

            // シーンが入れ替わるのでここには戻ってこない
            _running = false;
        }
    }
}
