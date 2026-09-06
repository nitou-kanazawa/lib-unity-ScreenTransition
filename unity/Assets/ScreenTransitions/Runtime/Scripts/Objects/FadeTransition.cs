using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace Waribashi.ScreenTransitions
{
    /// <summary>
    /// 最もシンプルな単色フェード。シーン遷移の既定の蓋絵として使う想定。
    /// </summary>
    public class FadeTransition : ObjectCurtain
    {
        [SerializeField] Color color = Color.black;
        [SerializeField] float closeDuration = 0.35f;
        [SerializeField] float openDuration = 0.45f;

        Image _cover;

        protected override void Build()
        {
            _cover = CreateRect(Root, "Cover", new Color(color.r, color.g, color.b, 0f));
            StretchChild(_cover.rectTransform);
        }

        void SetAlpha(float a)
        {
            var c = color;
            c.a = a;
            _cover.color = c;
        }

        protected override async UniTask CloseRoutine()
        {
            await Tween(closeDuration, e => SetAlpha(Ease.OutCubic(Mathf.Clamp01(e / closeDuration))));
            SetAlpha(1f);
        }

        protected override async UniTask OpenRoutine()
        {
            await Tween(openDuration, e => SetAlpha(1f - Ease.OutCubic(Mathf.Clamp01(e / openDuration))));
            SetAlpha(0f);
        }
    }
}
