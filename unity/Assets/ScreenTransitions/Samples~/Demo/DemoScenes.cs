using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine.SceneManagement;

namespace Waribashi.ScreenTransitions.Demo
{
    /// <summary>
    /// デモシーンの名前と、ハブとの行き来。
    /// シーンは Tools/Screen Transitions/Build Demo Scenes で生成され、
    /// ビルド設定にもそこで登録される。
    /// </summary>
    public static class DemoScenes
    {
        public const string Hub = "DemoHub";
        public const string Catalogue = "TransitionDemo";
        public const string Undersea = "TransitionUndersea";
        public const string CutIn = "CutInDemo";
        public const string Interrupt = "InterruptDemo";
        public const string Loading = "LoadingDemo";

        /// <summary>ハブに並べる順。ビルド設定への登録もこの順で行う。</summary>
        public static readonly string[] All =
        {
            Hub, Catalogue, CutIn, Interrupt, Loading, Undersea,
        };

        /// <summary>蓋絵で覆ってからシーンを切り替える。</summary>
        public static UniTask GoAsync(ICurtain curtain, string sceneName, CancellationToken ct = default)
            => curtain.RunAsync(
                token => SceneManager.LoadSceneAsync(sceneName).ToUniTask(cancellationToken: token),
                ct: ct);
    }
}
