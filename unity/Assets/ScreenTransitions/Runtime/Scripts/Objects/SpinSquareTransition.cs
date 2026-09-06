using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Waribashi.ScreenTransitions
{
    /// <summary>
    /// 回転しながら拡大する巨大な正方形が画面を飲み込む。
    /// アクセント色の正方形が一歩先行し、縁取りとして見える二層構成。
    /// </summary>
    public class SpinSquareTransition : ObjectCurtain
    {
        [SerializeField] Color mainColor = new Color(0.05f, 0.055f, 0.08f);
        [SerializeField] Color accentColor = new Color(1f, 0.62f, 0.24f);
        [SerializeField] float closeDuration = 0.45f;
        [SerializeField] float openDuration = 0.38f;
        [SerializeField] float lag = 0.07f;
        [SerializeField] float spinAngle = 135f;

        RectTransform _accent;
        RectTransform _main;

        protected override void Build()
        {
            float w = Root.rect.width;
            float h = Root.rect.height;
            float cover = Mathf.Sqrt(w * w + h * h) * 1.05f;

            _accent = CreateRect(Root, "Accent", accentColor).rectTransform;
            _accent.sizeDelta = new Vector2(cover, cover);
            _accent.localScale = Vector3.zero;

            _main = CreateRect(Root, "Main", mainColor).rectTransform;
            _main.sizeDelta = new Vector2(cover, cover);
            _main.localScale = Vector3.zero;
        }

        protected override async UniTask CloseRoutine()
        {
            // アクセントが先行して育ち、本体が追いかけて塗り潰す
            float total = lag + closeDuration;
            await Tween(total, e =>
            {
                float ta = Mathf.Clamp01(e / closeDuration);
                float tm = Mathf.Clamp01((e - lag) / closeDuration);
                Apply(_accent, Ease.OutCubic(ta));
                Apply(_main, Ease.OutCubic(tm));
            });
        }

        protected override async UniTask OpenRoutine()
        {
            // 本体が先に縮み、一瞬アクセントの縁が見えてから消える
            float total = lag + openDuration;
            await Tween(total, e =>
            {
                float tm = Mathf.Clamp01(e / openDuration);
                float ta = Mathf.Clamp01((e - lag) / openDuration);
                Apply(_main, 1f - Ease.InCubic(tm));
                Apply(_accent, 1f - Ease.InCubic(ta));
            });
        }

        void Apply(RectTransform rt, float progress)
        {
            float s = Mathf.Max(0f, progress);
            rt.localScale = new Vector3(s, s, 1f);
            rt.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(spinAngle, 0f, progress));
        }
    }
}
