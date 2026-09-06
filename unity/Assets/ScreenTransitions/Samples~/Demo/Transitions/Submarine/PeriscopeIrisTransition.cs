using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace Waribashi.ScreenTransitions.Demo.Submarine
{
    /// <summary>
    /// 潜望鏡の円形視界に絞り込まれ、クロスヘアが浮かんでから真っ暗になる。
    /// 「潜望鏡を覗いている」ことが分かる演出。
    /// </summary>
    public class PeriscopeIrisTransition : ObjectCurtain
    {
        [SerializeField] Color maskColor = new Color(0.02f, 0.02f, 0.03f);
        [SerializeField] Color reticleColor = new Color(1f, 1f, 1f, 0.4f);
        [SerializeField] float scopeRadius = 200f;
        [SerializeField] float shrinkDuration = 0.55f;
        [SerializeField] float holdDuration = 0.28f;
        [SerializeField] float snapDuration = 0.18f;

        RectTransform _hole;
        RectTransform _left, _right, _top, _bottom;
        Image _hLine, _vLine;
        Image _ring;
        float _w, _h, _coverRadius;

        protected override void Build()
        {
            _w = Root.rect.width;
            _h = Root.rect.height;
            _coverRadius = Mathf.Sqrt(_w * _w + _h * _h) * 0.5f + 12f;

            _left = CreateSidePanel("Left", new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f));
            _right = CreateSidePanel("Right", new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(1f, 0.5f));
            _top = CreateSidePanel("Top", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f));
            _bottom = CreateSidePanel("Bottom", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f));

            var hole = CreateSprite(Root, "IrisHole", ProceduralSprites.IrisHole, maskColor);
            _hole = hole.rectTransform;
            _hole.sizeDelta = new Vector2(ProceduralSprites.IrisHoleSize, ProceduralSprites.IrisHoleSize);

            _ring = CreateSprite(Root, "ScopeRing", ProceduralSprites.Ring,
                new Color(reticleColor.r, reticleColor.g, reticleColor.b, 0f));

            _hLine = CreateRect(Root, "ReticleH", new Color(reticleColor.r, reticleColor.g, reticleColor.b, 0f));
            _hLine.rectTransform.sizeDelta = new Vector2(_w, 2f);
            _hLine.raycastTarget = false;
            _vLine = CreateRect(Root, "ReticleV", new Color(reticleColor.r, reticleColor.g, reticleColor.b, 0f));
            _vLine.rectTransform.sizeDelta = new Vector2(2f, _h);
            _vLine.raycastTarget = false;

            SetIris(_coverRadius, 0f);
        }

        RectTransform CreateSidePanel(string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot)
        {
            var panel = CreateRect(Root, name, maskColor);
            var rt = panel.rectTransform;
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.sizeDelta = Vector2.zero;
            rt.anchoredPosition = Vector2.zero;
            return rt;
        }

        void SetIris(float radius, float reticleAlpha)
        {
            float scale = Mathf.Max(0.0001f, radius / ProceduralSprites.IrisHoleRadius);
            _hole.localScale = new Vector3(scale, scale, 1f);
            float outerHalf = ProceduralSprites.IrisHoleSize * 0.5f * scale;

            _left.sizeDelta = new Vector2(Mathf.Max(0f, _w * 0.5f - outerHalf + 2f), 0f);
            _right.sizeDelta = new Vector2(Mathf.Max(0f, _w * 0.5f - outerHalf + 2f), 0f);
            _top.sizeDelta = new Vector2(0f, Mathf.Max(0f, _h * 0.5f - outerHalf + 2f));
            _bottom.sizeDelta = new Vector2(0f, Mathf.Max(0f, _h * 0.5f - outerHalf + 2f));

            var c = reticleColor;
            c.a = reticleColor.a * reticleAlpha;
            _hLine.color = c;
            _vLine.color = c;
            var rc = c;
            rc.a *= 0.8f;
            _ring.color = rc;
            _ring.rectTransform.sizeDelta = Vector2.one * (radius * 1.72f);
        }

        protected override async UniTask CloseRoutine()
        {
            // 視界がスコープ径まで絞られる
            await Tween(shrinkDuration, e =>
            {
                float t = Ease.OutCubic(Mathf.Clamp01(e / shrinkDuration));
                SetIris(Mathf.Lerp(_coverRadius, scopeRadius, t), Mathf.Clamp01((t - 0.5f) * 2f));
            });

            // クロスヘアを見せながら一拍置く（微かな呼吸）
            await Tween(holdDuration, e =>
            {
                SetIris(scopeRadius + Mathf.Sin(e * 18f) * 4f, 1f);
            });

            // 一気に閉じ切る
            await Tween(snapDuration, e =>
            {
                float t = Ease.InCubic(Mathf.Clamp01(e / snapDuration));
                SetIris(Mathf.Lerp(scopeRadius, 0f, t), 1f - t);
            });
            SetIris(0f, 0f);
        }

        protected override async UniTask OpenRoutine()
        {
            // スコープ径までポップして開く
            await Tween(0.25f, e =>
            {
                float t = Mathf.Max(0f, Ease.OutBack(Mathf.Clamp01(e / 0.25f)));
                SetIris(scopeRadius * t, t);
            });

            await Tween(0.4f, e =>
            {
                float t = Ease.InCubic(Mathf.Clamp01(e / 0.4f));
                SetIris(Mathf.Lerp(scopeRadius, _coverRadius, t), 1f - t);
            });
            SetIris(_coverRadius, 0f);
        }
    }
}
