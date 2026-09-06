using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Waribashi.ScreenTransitions
{
    public enum CurtainState
    {
        /// <summary>蓋が開いている（画面が見えている・アイドル）。</summary>
        Open,
        /// <summary>閉じアニメーション再生中。</summary>
        Closing,
        /// <summary>蓋が閉じ切っている（HoldLoop 再生中）。</summary>
        Closed,
        /// <summary>開きアニメーション再生中。</summary>
        Opening,
    }

    /// <summary>
    /// 蓋絵トランジションの共通契約。
    /// - CloseAsync 完了後〜OpenAsync 開始まで、画面は完全に覆われていることを保証する
    /// - Closed 中は実装側の HoldLoop（ループアニメ）が自動で回る
    /// - Busy（Closing / Opening）中の再入は無視される
    /// - 時間は unscaled 基準（ロード中の timeScale 操作に影響されない）
    /// </summary>
    public interface ICurtain
    {
        CurtainState State { get; }
        UniTask CloseAsync(CancellationToken ct = default);
        UniTask OpenAsync(CancellationToken ct = default);
    }

    public static class CurtainExtensions
    {
        public static bool IsBusy(this ICurtain curtain)
            => curtain.State == CurtainState.Closing || curtain.State == CurtainState.Opening;

        /// <summary>
        /// 閉じ → work 実行（ローディング表示可・その間 HoldLoop が回る）→ 開き。
        /// </summary>
        public static async UniTask RunAsync(this ICurtain curtain,
            Func<CancellationToken, UniTask> work = null,
            LoadingIndicator loading = null,
            CancellationToken ct = default)
        {
            await curtain.CloseAsync(ct);

            if (loading != null)
                loading.Show();
            try
            {
                if (work != null)
                    await work(ct);
            }
            finally
            {
                if (loading != null)
                    loading.Hide();
            }

            await curtain.OpenAsync(ct);
        }

        /// <summary>閉じ → 固定秒保持 → 開き の糖衣。</summary>
        public static UniTask RunAsync(this ICurtain curtain,
            float holdSeconds,
            LoadingIndicator loading = null,
            CancellationToken ct = default)
        {
            return curtain.RunAsync(
                c => UniTask.Delay(TimeSpan.FromSeconds(holdSeconds), DelayType.UnscaledDeltaTime, PlayerLoopTiming.Update, c),
                loading, ct);
        }
    }
}
