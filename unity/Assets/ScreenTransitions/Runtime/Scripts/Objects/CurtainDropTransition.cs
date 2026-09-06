using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Waribashi.ScreenTransitions
{
    /// <summary>
    /// 縦バーが上から落下してバウンドしながら画面を埋める。
    /// 開きはそのまま下へ落ちて抜けていく。
    /// </summary>
    public class CurtainDropTransition : ObjectTransition
    {
        [SerializeField] int barCount = 7;
        [SerializeField] Color barColor = new Color(0.06f, 0.08f, 0.13f);
        [SerializeField] Color accentColor = new Color(1f, 0.62f, 0.24f);
        [SerializeField] float closeDuration = 0.55f;
        [SerializeField] float openDuration = 0.32f;
        [SerializeField] float stagger = 0.05f;

        RectTransform[] _bars;
        float _height;

        protected override void Build()
        {
            _height = Root.rect.height;
            _bars = new RectTransform[barCount];
            for (int i = 0; i < barCount; i++)
            {
                var bar = CreateRect(Root, $"Bar{i}", barColor);
                var rt = bar.rectTransform;
                rt.anchorMin = new Vector2((float)i / barCount, 0f);
                rt.anchorMax = new Vector2((float)(i + 1) / barCount, 1f);
                rt.offsetMin = new Vector2(-1f, 0f);
                rt.offsetMax = new Vector2(1f, 0f);

                // 落下の先端（下端）にアクセントライン
                var accent = CreateRect(rt, "Accent", accentColor);
                var art = accent.rectTransform;
                art.anchorMin = new Vector2(0f, 0f);
                art.anchorMax = new Vector2(1f, 0f);
                art.pivot = new Vector2(0.5f, 0f);
                art.sizeDelta = new Vector2(0f, 10f);
                art.anchoredPosition = Vector2.zero;

                rt.anchoredPosition = new Vector2(0f, _height);
                _bars[i] = rt;
            }
        }

        protected override async UniTask CloseRoutine()
        {
            float total = stagger * (barCount - 1) + closeDuration;
            await Tween(total, e =>
            {
                for (int i = 0; i < barCount; i++)
                {
                    float t = Mathf.Clamp01((e - i * stagger) / closeDuration);
                    float y = Mathf.Lerp(_height, 0f, Ease.OutBounce(t));
                    _bars[i].anchoredPosition = new Vector2(0f, y);
                }
            });
        }

        protected override async UniTask OpenRoutine()
        {
            float total = stagger * (barCount - 1) + openDuration;
            await Tween(total, e =>
            {
                for (int i = 0; i < barCount; i++)
                {
                    float t = Mathf.Clamp01((e - i * stagger) / openDuration);
                    float y = Mathf.Lerp(0f, -_height, Ease.InCubic(t));
                    _bars[i].anchoredPosition = new Vector2(0f, y);
                }
            });
        }
    }
}
