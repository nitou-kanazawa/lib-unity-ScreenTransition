using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Waribashi.ScreenTransitions
{
    /// <summary>
    /// 斜めに傾いた2枚のスラブ（アクセント色 → 本体色）が時間差で横切る二段ワイプ。
    /// </summary>
    public class DiagonalSlabTransition : ObjectTransition
    {
        [SerializeField] Color mainColor = new Color(0.04f, 0.045f, 0.06f);
        [SerializeField] Color accentColor = new Color(1f, 0.62f, 0.24f);
        [SerializeField] float tiltAngle = 12f;
        [SerializeField] float duration = 0.4f;
        [SerializeField] float lag = 0.09f;

        RectTransform _accent;
        RectTransform _main;
        float _slabWidth;

        protected override void Build()
        {
            float w = Root.rect.width;
            float h = Root.rect.height;

            // 回転しても四隅が欠けないよう大きめの傾き付きコンテナを作る
            var tiltGo = new GameObject("Tilt", typeof(RectTransform));
            var tilt = (RectTransform)tiltGo.transform;
            tilt.SetParent(Root, false);
            tilt.sizeDelta = new Vector2(w * 1.8f, h * 2.2f);
            tilt.localRotation = Quaternion.Euler(0f, 0f, tiltAngle);
            _slabWidth = w * 1.8f;

            _accent = CreateRect(tilt, "Accent", accentColor).rectTransform;
            StretchChild(_accent);
            _main = CreateRect(tilt, "Main", mainColor).rectTransform;
            StretchChild(_main);

            _accent.anchoredPosition = new Vector2(-_slabWidth, 0f);
            _main.anchoredPosition = new Vector2(-_slabWidth, 0f);
        }

        protected override async UniTask CloseRoutine()
        {
            float total = lag + duration;
            await Tween(total, e =>
            {
                float ta = Mathf.Clamp01(e / duration);
                float tm = Mathf.Clamp01((e - lag) / duration);
                _accent.anchoredPosition = new Vector2(Mathf.Lerp(-_slabWidth, 0f, Ease.OutQuart(ta)), 0f);
                _main.anchoredPosition = new Vector2(Mathf.Lerp(-_slabWidth, 0f, Ease.OutQuart(tm)), 0f);
            });
        }

        protected override async UniTask OpenRoutine()
        {
            // 本体が先に抜けてアクセント色が一瞬見え、続けてアクセントも抜ける
            float total = lag + duration;
            await Tween(total, e =>
            {
                float tm = Mathf.Clamp01(e / duration);
                float ta = Mathf.Clamp01((e - lag) / duration);
                _main.anchoredPosition = new Vector2(Mathf.Lerp(0f, _slabWidth, Ease.OutQuart(tm)), 0f);
                _accent.anchoredPosition = new Vector2(Mathf.Lerp(0f, _slabWidth, Ease.OutQuart(ta)), 0f);
            });
        }
    }
}
