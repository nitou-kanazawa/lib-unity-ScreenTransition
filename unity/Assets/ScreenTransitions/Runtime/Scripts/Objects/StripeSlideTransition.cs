using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Waribashi.ScreenTransitions
{
    /// <summary>
    /// 段々の横バーが左右交互にスライドして蓋を閉じる。
    /// 各バーの先頭にアクセントカラーのエッジが付く。
    /// </summary>
    public class StripeSlideTransition : ObjectTransition
    {
        [SerializeField] int barCount = 6;
        [SerializeField] Color barColor = new Color(0.06f, 0.08f, 0.13f);
        [SerializeField] Color accentColor = new Color(1f, 0.62f, 0.24f);
        [SerializeField] float slideDuration = 0.38f;
        [SerializeField] float stagger = 0.055f;
        [SerializeField] float accentWidth = 18f;

        RectTransform[] _bars;
        float[] _dirs;
        float _width;

        protected override void Build()
        {
            _width = Root.rect.width;
            _bars = new RectTransform[barCount];
            _dirs = new float[barCount];

            for (int i = 0; i < barCount; i++)
            {
                float dir = (i % 2 == 0) ? 1f : -1f; // 偶数行は左から、奇数行は右から

                var bar = CreateRect(Root, $"Bar{i}", barColor);
                var rt = bar.rectTransform;
                rt.anchorMin = new Vector2(0f, (float)i / barCount);
                rt.anchorMax = new Vector2(1f, (float)(i + 1) / barCount);
                rt.offsetMin = new Vector2(0f, -1f); // 行間の継ぎ目を消すため 1px 重ねる
                rt.offsetMax = new Vector2(0f, 1f);

                var accent = CreateRect(rt, "Accent", accentColor);
                var art = accent.rectTransform;
                if (dir > 0f)
                {
                    art.anchorMin = new Vector2(1f, 0f);
                    art.anchorMax = new Vector2(1f, 1f);
                    art.pivot = new Vector2(0f, 0.5f);
                }
                else
                {
                    art.anchorMin = new Vector2(0f, 0f);
                    art.anchorMax = new Vector2(0f, 1f);
                    art.pivot = new Vector2(1f, 0.5f);
                }
                art.sizeDelta = new Vector2(accentWidth, 0f);
                art.anchoredPosition = Vector2.zero;

                rt.anchoredPosition = new Vector2(-dir * _width, 0f);
                _bars[i] = rt;
                _dirs[i] = dir;
            }
        }

        protected override async UniTask CloseRoutine()
        {
            float total = stagger * (barCount - 1) + slideDuration;
            await Tween(total, e =>
            {
                for (int i = 0; i < barCount; i++)
                {
                    float t = Mathf.Clamp01((e - i * stagger) / slideDuration);
                    float x = Mathf.Lerp(-_dirs[i] * _width, 0f, Ease.OutCubic(t));
                    _bars[i].anchoredPosition = new Vector2(x, 0f);
                }
            });
        }

        protected override async UniTask OpenRoutine()
        {
            // 閉じと同じ方向へ抜けていく（パネルが通過していく見え方）
            float total = stagger * (barCount - 1) + slideDuration;
            await Tween(total, e =>
            {
                for (int i = 0; i < barCount; i++)
                {
                    float t = Mathf.Clamp01((e - i * stagger) / slideDuration);
                    float x = Mathf.Lerp(0f, _dirs[i] * _width, Ease.InCubic(t));
                    _bars[i].anchoredPosition = new Vector2(x, 0f);
                }
            });
        }
    }
}
