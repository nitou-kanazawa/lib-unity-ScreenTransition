using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Waribashi.ScreenTransitions
{
    /// <summary>
    /// ギザギザの刃先を持つパネルが右から左へ横切るワイプ。
    /// アクセント色のギザギザ層が少し先行して縁取りになる。
    /// </summary>
    public class ZigzagWipeTransition : ObjectTransition
    {
        [SerializeField] int teethCount = 9;
        [SerializeField] Color mainColor = new Color(0.05f, 0.055f, 0.08f);
        [SerializeField] Color accentColor = new Color(1f, 0.62f, 0.24f);
        [SerializeField] float duration = 0.42f;
        [SerializeField] float edgeGap = 26f;

        RectTransform _mainLayer;
        RectTransform _accentLayer;
        float _travel;

        protected override void Build()
        {
            float w = Root.rect.width;
            float h = Root.rect.height;
            float toothSize = h / teethCount * 0.8f;
            _travel = w + toothSize * 2f;

            _accentLayer = BuildLayer("AccentLayer", accentColor, toothSize);
            _mainLayer = BuildLayer("MainLayer", mainColor, toothSize);

            _accentLayer.anchoredPosition = new Vector2(_travel - edgeGap, 0f);
            _mainLayer.anchoredPosition = new Vector2(_travel, 0f);
        }

        RectTransform BuildLayer(string name, Color color, float toothSize)
        {
            var layerGo = new GameObject(name, typeof(RectTransform));
            var layer = (RectTransform)layerGo.transform;
            layer.SetParent(Root, false);
            StretchChild(layer);

            var body = CreateRect(layer, "Body", color);
            StretchChild(body.rectTransform);

            // 左端に45°回転した正方形を並べてギザギザの刃先を作る
            for (int k = 0; k < teethCount; k++)
            {
                var tooth = CreateRect(layer, $"Tooth{k}", color);
                var rt = tooth.rectTransform;
                float anchorY = (k + 0.5f) / teethCount;
                rt.anchorMin = new Vector2(0f, anchorY);
                rt.anchorMax = new Vector2(0f, anchorY);
                rt.sizeDelta = new Vector2(toothSize, toothSize);
                rt.anchoredPosition = Vector2.zero;
                rt.localRotation = Quaternion.Euler(0f, 0f, 45f);
            }
            return layer;
        }

        protected override async UniTask CloseRoutine()
        {
            float total = duration;
            await Tween(total, e =>
            {
                float t = Ease.OutQuart(Mathf.Clamp01(e / duration));
                float x = Mathf.Lerp(_travel, 0f, t);
                _mainLayer.anchoredPosition = new Vector2(x, 0f);
                _accentLayer.anchoredPosition = new Vector2(x - edgeGap, 0f);
            });
        }

        protected override async UniTask OpenRoutine()
        {
            float total = duration;
            await Tween(total, e =>
            {
                float t = Ease.OutQuart(Mathf.Clamp01(e / duration));
                float x = Mathf.Lerp(0f, -_travel, t);
                _mainLayer.anchoredPosition = new Vector2(x, 0f);
                // 開きでは後端側（右）にアクセントの縁が見えるようにする
                _accentLayer.anchoredPosition = new Vector2(x + edgeGap, 0f);
            });
        }
    }
}
