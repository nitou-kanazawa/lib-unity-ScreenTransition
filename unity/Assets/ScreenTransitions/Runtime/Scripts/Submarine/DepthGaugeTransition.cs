using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace Waribashi.ScreenTransitions.Submarine
{
    /// <summary>
    /// 画面が深い青に沈み込みながら、深度計の数字が回っていく。
    /// 「水深が深くなっていく」ことが数字とゲージで分かる演出。
    /// </summary>
    public class DepthGaugeTransition : ObjectTransition
    {
        [SerializeField] Color coverColor = new Color(0.02f, 0.05f, 0.10f);
        [SerializeField] Color gaugeColor = new Color(0.35f, 0.85f, 1f);
        [SerializeField] int maxDepth = 3000;
        [SerializeField] float closeDuration = 1.05f;
        [SerializeField] float openDuration = 0.6f;

        Image _cover;
        RectTransform _panel;
        Image _barFill;
        Text _depthText;
        Text _zoneText;
        float _w;

        static readonly (float ratio, string label)[] Zones =
        {
            (0f, "SURFACE"),
            (0.15f, "SUNLIGHT ZONE"),
            (0.45f, "TWILIGHT ZONE"),
            (0.8f, "MIDNIGHT ZONE"),
        };

        protected override void Build()
        {
            _w = Root.rect.width;

            _cover = CreateRect(Root, "Cover", new Color(coverColor.r, coverColor.g, coverColor.b, 0f));
            StretchChild(_cover.rectTransform);

            // 右側の深度パネル
            var panelGo = new GameObject("GaugePanel", typeof(RectTransform));
            _panel = (RectTransform)panelGo.transform;
            _panel.SetParent(Root, false);
            _panel.anchorMin = _panel.anchorMax = new Vector2(1f, 0.5f);
            _panel.pivot = new Vector2(1f, 0.5f);
            _panel.sizeDelta = new Vector2(360f, 640f);
            _panel.anchoredPosition = new Vector2(380f, 0f); // 画面外から入る

            var track = CreateRect(_panel, "Track", new Color(1f, 1f, 1f, 0.14f));
            track.rectTransform.anchorMin = new Vector2(0f, 0f);
            track.rectTransform.anchorMax = new Vector2(0f, 1f);
            track.rectTransform.pivot = new Vector2(0f, 0.5f);
            track.rectTransform.sizeDelta = new Vector2(10f, 0f);
            track.rectTransform.anchoredPosition = new Vector2(40f, 0f);

            _barFill = CreateRect(track.transform, "Fill", gaugeColor);
            var fillRt = _barFill.rectTransform;
            fillRt.anchorMin = new Vector2(0f, 1f);
            fillRt.anchorMax = new Vector2(1f, 1f);
            fillRt.pivot = new Vector2(0.5f, 1f);
            fillRt.offsetMin = Vector2.zero;
            fillRt.offsetMax = Vector2.zero;
            fillRt.sizeDelta = new Vector2(0f, 0f);

            // 目盛り
            for (int i = 0; i <= 6; i++)
            {
                var tick = CreateRect(_panel, $"Tick{i}", new Color(1f, 1f, 1f, 0.35f));
                tick.rectTransform.anchorMin = new Vector2(0f, 1f - i / 6f);
                tick.rectTransform.anchorMax = new Vector2(0f, 1f - i / 6f);
                tick.rectTransform.sizeDelta = new Vector2(26f, 3f);
                tick.rectTransform.anchoredPosition = new Vector2(68f, 0f);
            }

            _depthText = CreateLabel(_panel, "Depth", "0 m", 84, Color.white);
            _depthText.alignment = TextAnchor.MiddleRight;
            var drt = _depthText.rectTransform;
            drt.anchorMin = drt.anchorMax = new Vector2(1f, 0.5f);
            drt.pivot = new Vector2(1f, 0.5f);
            drt.anchoredPosition = new Vector2(-40f, 30f);
            drt.sizeDelta = new Vector2(280f, 100f);

            _zoneText = CreateLabel(_panel, "Zone", "SURFACE", 30, gaugeColor);
            _zoneText.alignment = TextAnchor.MiddleRight;
            var zrt = _zoneText.rectTransform;
            zrt.anchorMin = zrt.anchorMax = new Vector2(1f, 0.5f);
            zrt.pivot = new Vector2(1f, 0.5f);
            zrt.anchoredPosition = new Vector2(-40f, -45f);
            zrt.sizeDelta = new Vector2(340f, 44f);
        }

        void ApplyDepth(float t)
        {
            int depth = Mathf.RoundToInt(Mathf.Lerp(0f, maxDepth, t));
            _depthText.text = depth + " m";
            _barFill.rectTransform.sizeDelta = new Vector2(0f, 0f);
            var fillRt = _barFill.rectTransform;
            fillRt.anchorMin = new Vector2(0f, 1f - t);
            string zone = Zones[0].label;
            foreach (var z in Zones)
                if (t >= z.ratio)
                    zone = z.label;
            _zoneText.text = zone;
        }

        protected override async UniTask CloseRoutine()
        {
            await Tween(closeDuration, e =>
            {
                float t = Mathf.Clamp01(e / closeDuration);
                var c = coverColor;
                c.a = Mathf.Clamp01(t * 1.6f);
                _cover.color = c;
                _panel.anchoredPosition = new Vector2(Mathf.Lerp(380f, -60f, Ease.OutCubic(Mathf.Clamp01(t * 2.2f))), 0f);
                ApplyDepth(Ease.OutQuart(t));
            });
        }

        protected override async UniTask OpenRoutine()
        {
            await Tween(openDuration, e =>
            {
                float t = Mathf.Clamp01(e / openDuration);
                var c = coverColor;
                c.a = 1f - Ease.OutCubic(t);
                _cover.color = c;
                _panel.anchoredPosition = new Vector2(Mathf.Lerp(-60f, 380f, Ease.InCubic(t)), 0f);
            });
        }
    }
}
