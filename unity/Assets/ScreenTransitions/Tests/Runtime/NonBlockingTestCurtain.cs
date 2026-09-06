using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace Waribashi.ScreenTransitions.Tests
{
    /// <summary>
    /// 画面の一部しか覆わない蓋絵（キャラクターのカットイン相当）のテスト用実装。
    ///
    /// BlocksRaycasts を false にしても契約を満たすことを確かめるためのもの。
    /// CurtainTypes.All はロード済みアセンブリ全体を走査するので、この型も
    /// 契約テスト一式の対象になる。つまり「基底クラス単体で契約を満たしている」ことの検証も兼ねる。
    /// </summary>
    public class NonBlockingTestCurtain : ObjectCurtain
    {
        const float Duration = 0.05f;

        public override bool BlocksRaycasts => false;

        Image _band;

        protected override void Build()
        {
            _band = CreateRect(Root, "Band", Color.red);
            var rt = _band.rectTransform;
            // 画面の中央 2 割を横切るだけの帯。全画面は覆わない
            rt.anchorMin = new Vector2(0f, 0.4f);
            rt.anchorMax = new Vector2(1f, 0.6f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        protected override UniTask CloseRoutine() => Tween(Duration, elapsed => SetWidth(elapsed / Duration));

        protected override UniTask OpenRoutine() => Tween(Duration, elapsed => SetWidth(1f - elapsed / Duration));

        void SetWidth(float ratio)
            => _band.rectTransform.localScale = new Vector3(Mathf.Clamp01(ratio), 1f, 1f);
    }
}
