namespace Waribashi.ScreenTransitions
{
    /// <summary>
    /// 蓋が閉じ切っている間に出す表示。<see cref="CurtainExtensions.RunAsync"/> が Show / Hide を呼ぶ。
    ///
    /// 同梱の <see cref="LoadingIndicator"/> は既定の実装にすぎない。
    /// 見た目はゲームごとに違うので、コア側はこの契約だけに依存する。
    /// </summary>
    public interface ILoadingIndicator
    {
        void Show();

        void Hide();
    }

    public static class LoadingIndicatorExtensions
    {
        /// <summary>
        /// 破棄済みの MonoBehaviour を掴んでいないか確かめる。
        ///
        /// インターフェース型の変数に入れた MonoBehaviour は、GameObject が破棄されていても
        /// `!= null` が true になる（Unity の偽 null 判定は UnityEngine.Object の演算子で、
        /// インターフェース越しでは効かない）。素直に呼ぶと MissingReferenceException になる。
        /// </summary>
        public static bool IsAlive(this ILoadingIndicator indicator)
        {
            if (indicator == null)
                return false;

            return indicator is UnityEngine.Object obj ? obj != null : true;
        }
    }
}
