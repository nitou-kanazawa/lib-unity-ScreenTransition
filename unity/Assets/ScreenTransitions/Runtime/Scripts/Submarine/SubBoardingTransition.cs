using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace Waribashi.ScreenTransitions.Submarine
{
    /// <summary>
    /// 潜水艦のシルエットが滑り込み、ハッチ（円）がズームして画面を飲み込む。
    /// 「潜水艦に乗り込んだ」ことが分かる乗艦演出。
    /// </summary>
    public class SubBoardingTransition : ObjectTransition
    {
        [SerializeField] Color hullColor = new Color(0.13f, 0.15f, 0.19f);
        [SerializeField] Color portholeColor = new Color(0.55f, 0.8f, 0.95f, 0.9f);
        [SerializeField] Color hatchColor = new Color(0.03f, 0.035f, 0.05f);
        [SerializeField] float slideDuration = 0.5f;
        [SerializeField] float zoomDuration = 0.38f;

        RectTransform _sub;
        RectTransform _hatch;
        Image _cover;
        float _w, _hatchCoverScale;

        protected override void Build()
        {
            _w = Root.rect.width;
            float h = Root.rect.height;

            // 潜水艦シルエット（カプセル艦体 + セイル + 潜望鏡 + 舷窓）
            var subGo = new GameObject("Submarine", typeof(RectTransform));
            _sub = (RectTransform)subGo.transform;
            _sub.SetParent(Root, false);
            _sub.sizeDelta = new Vector2(560f, 220f);

            var body = CreateSprite(_sub, "Body", ProceduralSprites.Circle, hullColor);
            body.rectTransform.sizeDelta = new Vector2(560f, 150f);

            var sail = CreateRect(_sub, "Sail", hullColor);
            sail.rectTransform.sizeDelta = new Vector2(130f, 78f);
            sail.rectTransform.anchoredPosition = new Vector2(10f, 96f);
            sail.raycastTarget = false;

            var scope = CreateRect(_sub, "Periscope", hullColor);
            scope.rectTransform.sizeDelta = new Vector2(14f, 60f);
            scope.rectTransform.anchoredPosition = new Vector2(40f, 158f);
            scope.raycastTarget = false;

            for (int i = 0; i < 3; i++)
            {
                var port = CreateSprite(_sub, $"Porthole{i}", ProceduralSprites.Circle, portholeColor);
                port.rectTransform.sizeDelta = new Vector2(30f, 30f);
                port.rectTransform.anchoredPosition = new Vector2(-120f + i * 90f, -8f);
            }

            _sub.anchoredPosition = new Vector2(-_w * 0.85f, 0f);

            // ハッチ円（セイル位置から拡大して全画面を覆う）
            var hatch = CreateSprite(Root, "Hatch", ProceduralSprites.Circle, hatchColor);
            _hatch = hatch.rectTransform;
            _hatch.sizeDelta = new Vector2(200f, 200f);
            _hatch.anchoredPosition = new Vector2(10f, 96f);
            _hatch.localScale = Vector3.zero;
            _hatchCoverScale = Mathf.Sqrt(_w * _w + h * h) * 1.05f / 200f;

            // 円の縁のにじみ対策として最終的に全面を塗る保険パネル
            _cover = CreateRect(Root, "Cover", hatchColor);
            StretchChild(_cover.rectTransform);
            _cover.gameObject.SetActive(false);
        }

        protected override async UniTask CloseRoutine()
        {
            // 艦が滑り込む
            await Tween(slideDuration, e =>
            {
                float t = Ease.OutCubic(Mathf.Clamp01(e / slideDuration));
                _sub.anchoredPosition = new Vector2(
                    Mathf.Lerp(-_w * 0.85f, 0f, t),
                    Mathf.Sin(e * 5f) * 10f);
            });

            // ハッチがズームして飲み込む
            await Tween(zoomDuration, e =>
            {
                float t = Ease.InCubic(Mathf.Clamp01(e / zoomDuration));
                float s = Mathf.Lerp(0.001f, _hatchCoverScale, t);
                _hatch.localScale = new Vector3(s, s, 1f);
                float subScale = 1f + t * 0.18f;
                _sub.localScale = new Vector3(subScale, subScale, 1f);
            });
            _cover.gameObject.SetActive(true);
            _sub.anchoredPosition = new Vector2(-_w * 0.85f, 0f);
            _sub.localScale = Vector3.one;
        }

        protected override async UniTask OpenRoutine()
        {
            _cover.gameObject.SetActive(false);
            // ハッチが縮んで視界が開け、艦は先へ進んでいく
            await Tween(zoomDuration + 0.15f, e =>
            {
                float t = Ease.OutCubic(Mathf.Clamp01(e / (zoomDuration + 0.15f)));
                float s = Mathf.Lerp(_hatchCoverScale, 0.001f, t);
                _hatch.localScale = new Vector3(s, s, 1f);
                _sub.anchoredPosition = new Vector2(Mathf.Lerp(0f, _w * 0.9f, Ease.InCubic(t)), Mathf.Sin(e * 5f) * 8f);
            });
            _hatch.localScale = Vector3.zero;
        }
    }
}
