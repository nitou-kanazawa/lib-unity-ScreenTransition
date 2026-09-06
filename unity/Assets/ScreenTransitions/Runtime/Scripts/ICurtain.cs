using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Waribashi.ScreenTransitions
{
    public enum CurtainState
    {
        /// <summary>蓋が開いている（何も覆っていない・アイドル）。</summary>
        Open,
        /// <summary>閉じアニメーション再生中。</summary>
        Closing,
        /// <summary>蓋が閉じ切っている（HoldLoop 再生中）。何をどこまで覆うかは実装による。</summary>
        Closed,
        /// <summary>開きアニメーション再生中。</summary>
        Opening,
    }

    /// <summary>
    /// 蓋絵（画面を覆う物）の共通契約。閉じる / 開ける、以上の意味は持たせていない。
    ///
    /// 保証すること:
    /// - CloseAsync 完了後〜OpenAsync 開始まで、蓋は閉じ切った状態で維持される
    /// - Closed 中は実装側の HoldLoop（ループアニメ）が自動で回る
    /// - Busy（Closing / Opening）中の再入は無視される
    /// - 時間は unscaled 基準（ロード中の timeScale 操作に影響されない）
    ///
    /// 保証しないこと:
    /// - <b>どこをどこまで覆うか。</b> 全画面を塗り潰す蓋絵も、画面の一部を横切るカットインも
    ///   同じ ICurtain である。「Closed 中は画面が完全に覆われている」は実装ごとの性質であって
    ///   契約ではない。シーンロードを隠す用途には全画面を覆う実装を選ぶこと
    ///   （同梱の汎用 9 種とルール画像系はいずれも全画面を覆う）
    /// - 中断時の着地先。ObjectCurtain 派生は必ず Open へ戻すが、RuleImageCurtain は Closed へ戻る
    /// </summary>
    public interface ICurtain
    {
        CurtainState State { get; }

        UniTask CloseAsync(CancellationToken ct = default);

        UniTask OpenAsync(CancellationToken ct = default);

        /// <summary>
        /// 実行中のフェーズ（Closing / Opening）を終端まで早送りする。キャンセルとは向きが逆で、
        /// 巻き戻さずに「最後まで進める」。Closing 中なら Closed、Opening 中なら Open へ待たずに着地する。
        ///
        /// Closed 中（HoldLoop 再生中）は受け付けない。そこで待っているのがロードなのか
        /// ただの間なのかは呼び出し側にしか判断できないため。
        /// </summary>
        /// <returns>受け付けたら true。実行中のフェーズが無ければ false。</returns>
        bool Complete();
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
