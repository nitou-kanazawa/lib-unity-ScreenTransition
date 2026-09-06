using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Waribashi.ScreenTransitions
{
    /// <summary>
    /// 縦スラットが左から順に横スケールで展開して画面を埋める。
    /// 2トーンの交互配色で奥行きを出す。
    /// </summary>
    public class SlatFlipTransition : ObjectTransition
    {
        [SerializeField] int slatCount = 8;
        [SerializeField] Color colorA = new Color(0.06f, 0.07f, 0.10f);
        [SerializeField] Color colorB = new Color(0.10f, 0.12f, 0.17f);
        [SerializeField] float duration = 0.30f;
        [SerializeField] float stagger = 0.045f;

        RectTransform[] _slats;

        protected override void Build()
        {
            _slats = new RectTransform[slatCount];
            for (int i = 0; i < slatCount; i++)
            {
                var slat = CreateRect(Root, $"Slat{i}", i % 2 == 0 ? colorA : colorB);
                var rt = slat.rectTransform;
                rt.anchorMin = new Vector2((float)i / slatCount, 0f);
                rt.anchorMax = new Vector2((float)(i + 1) / slatCount, 1f);
                rt.offsetMin = new Vector2(-1f, 0f);
                rt.offsetMax = new Vector2(1f, 0f);
                rt.pivot = new Vector2(0f, 0.5f);
                rt.localScale = new Vector3(0f, 1f, 1f);
                _slats[i] = rt;
            }
        }

        protected override async UniTask CloseRoutine()
        {
            SetPivots(0f); // 左端から広がる
            float total = stagger * (slatCount - 1) + duration;
            await Tween(total, e =>
            {
                for (int i = 0; i < slatCount; i++)
                {
                    float t = Mathf.Clamp01((e - i * stagger) / duration);
                    _slats[i].localScale = new Vector3(Ease.OutCubic(t), 1f, 1f);
                }
            });
        }

        protected override async UniTask OpenRoutine()
        {
            SetPivots(1f); // 右端に向かって畳む
            float total = stagger * (slatCount - 1) + duration;
            await Tween(total, e =>
            {
                for (int i = 0; i < slatCount; i++)
                {
                    float t = Mathf.Clamp01((e - i * stagger) / duration);
                    _slats[i].localScale = new Vector3(1f - Ease.OutCubic(t), 1f, 1f);
                }
            });
        }

        void SetPivots(float x)
        {
            foreach (var slat in _slats)
                slat.pivot = new Vector2(x, 0.5f);
        }
    }
}
