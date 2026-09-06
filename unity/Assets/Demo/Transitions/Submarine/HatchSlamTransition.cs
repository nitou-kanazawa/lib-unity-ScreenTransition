using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace Waribashi.ScreenTransitions.Demo.Submarine
{
    /// <summary>
    /// リベット付きの鋼鉄隔壁が左右からガシャンと閉じ、残った丸い舷窓も暗くなる。
    /// 「艦内に入ってハッチが閉まった」ことが分かる演出。
    /// </summary>
    public class HatchSlamTransition : ObjectCurtain
    {
        [SerializeField] Color steelColor = new Color(0.16f, 0.18f, 0.21f);
        [SerializeField] Color steelEdgeColor = new Color(0.10f, 0.11f, 0.13f);
        [SerializeField] Color rivetColor = new Color(0.28f, 0.31f, 0.35f);
        [SerializeField] Color glassColor = new Color(0.16f, 0.42f, 0.6f);
        [SerializeField] float slamDuration = 0.3f;
        [SerializeField] float portholeDuration = 0.45f;

        RectTransform _leftPanel, _rightPanel;
        Image _flash;
        Image _portholeRing;
        Image _glass;
        float _half;

        protected override void Build()
        {
            _half = Root.rect.width * 0.5f + 4f;

            _leftPanel = CreatePanel("LeftPanel", true);
            _rightPanel = CreatePanel("RightPanel", false);

            _flash = CreateRect(Root, "Flash", new Color(1f, 1f, 1f, 0f));
            var frt = _flash.rectTransform;
            frt.anchorMin = new Vector2(0.5f, 0f);
            frt.anchorMax = new Vector2(0.5f, 1f);
            frt.sizeDelta = new Vector2(14f, 0f);
            _flash.raycastTarget = false;

            // 舷窓（リング + ガラス）
            _portholeRing = CreateSprite(Root, "PortholeRing", ProceduralSprites.Ring,
                new Color(rivetColor.r, rivetColor.g, rivetColor.b, 0f));
            _portholeRing.rectTransform.sizeDelta = new Vector2(340f, 340f);

            _glass = CreateSprite(Root, "Glass", ProceduralSprites.Circle,
                new Color(glassColor.r, glassColor.g, glassColor.b, 0f));
            _glass.rectTransform.sizeDelta = new Vector2(300f, 300f);
        }

        RectTransform CreatePanel(string name, bool isLeft)
        {
            var panel = CreateRect(Root, name, steelColor);
            var rt = panel.rectTransform;
            if (isLeft)
            {
                rt.anchorMin = new Vector2(0f, 0f);
                rt.anchorMax = new Vector2(0.5f, 1f);
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = new Vector2(2f, 0f);
            }
            else
            {
                rt.anchorMin = new Vector2(0.5f, 0f);
                rt.anchorMax = new Vector2(1f, 1f);
                rt.offsetMin = new Vector2(-2f, 0f);
                rt.offsetMax = Vector2.zero;
            }

            // 合わせ目の縁
            var edge = CreateRect(rt, "Edge", steelEdgeColor);
            var ert = edge.rectTransform;
            if (isLeft)
            {
                ert.anchorMin = new Vector2(1f, 0f);
                ert.anchorMax = new Vector2(1f, 1f);
                ert.pivot = new Vector2(1f, 0.5f);
            }
            else
            {
                ert.anchorMin = new Vector2(0f, 0f);
                ert.anchorMax = new Vector2(0f, 1f);
                ert.pivot = new Vector2(0f, 0.5f);
            }
            ert.sizeDelta = new Vector2(30f, 0f);
            ert.anchoredPosition = Vector2.zero;

            // リベット2列
            for (int col = 0; col < 2; col++)
            {
                for (int i = 0; i < 9; i++)
                {
                    var rivet = CreateSprite(rt, $"Rivet{col}_{i}", ProceduralSprites.Circle, rivetColor);
                    var rrt = rivet.rectTransform;
                    float x = isLeft ? 1f : 0f;
                    rrt.anchorMin = rrt.anchorMax = new Vector2(x, (i + 0.5f) / 9f);
                    rrt.sizeDelta = new Vector2(14f, 14f);
                    rrt.anchoredPosition = new Vector2(isLeft ? -52f - col * 34f : 52f + col * 34f, 0f);
                }
            }

            rt.anchoredPosition = new Vector2(isLeft ? -_half : _half, 0f);
            return rt;
        }

        protected override async UniTask CloseRoutine()
        {
            // 隔壁スラム
            await Tween(slamDuration, e =>
            {
                float t = Ease.InQuart(Mathf.Clamp01(e / slamDuration));
                _leftPanel.anchoredPosition = new Vector2(Mathf.Lerp(-_half, 0f, t), 0f);
                _rightPanel.anchoredPosition = new Vector2(Mathf.Lerp(_half, 0f, t), 0f);
            });

            // 衝撃フラッシュ + シェイク + 舷窓が現れて暗くなる
            await Tween(portholeDuration, e =>
            {
                float n = Mathf.Clamp01(e / portholeDuration);
                float shake = Mathf.Sin(e * 65f) * 6f * Mathf.Max(0f, 1f - n * 2.5f);
                _leftPanel.anchoredPosition = new Vector2(shake, 0f);
                _rightPanel.anchoredPosition = new Vector2(shake, 0f);

                var fc = _flash.color;
                fc.a = Mathf.Max(0f, 0.85f - n * 3f);
                _flash.color = fc;

                float ringA = Mathf.Clamp01(n * 3f);
                var rc = rivetColor; rc.a = ringA;
                _portholeRing.color = rc;

                // ガラス窓の外の光が段々暗くなる
                var gc = Color.Lerp(glassColor, new Color(0.01f, 0.02f, 0.04f), Ease.InCubic(n));
                gc.a = ringA;
                _glass.color = gc;
            });
        }

        protected override async UniTask OpenRoutine()
        {
            // 窓の外が明るくなり、隔壁が開く
            await Tween(0.25f, e =>
            {
                float t = Mathf.Clamp01(e / 0.25f);
                var gc = Color.Lerp(new Color(0.01f, 0.02f, 0.04f), glassColor, Ease.OutCubic(t));
                gc.a = 1f;
                _glass.color = gc;
            });

            await Tween(0.35f, e =>
            {
                float t = Ease.OutQuart(Mathf.Clamp01(e / 0.35f));
                _leftPanel.anchoredPosition = new Vector2(Mathf.Lerp(0f, -_half, t), 0f);
                _rightPanel.anchoredPosition = new Vector2(Mathf.Lerp(0f, _half, t), 0f);
                float fade = 1f - t;
                var rc = rivetColor; rc.a = fade;
                _portholeRing.color = rc;
                var gc = _glass.color; gc.a = fade;
                _glass.color = gc;
            });
        }
    }
}
