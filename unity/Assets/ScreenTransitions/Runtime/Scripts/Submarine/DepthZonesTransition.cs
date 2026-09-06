using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace Waribashi.ScreenTransitions.Submarine
{
    /// <summary>
    /// 海の断面図: 水面から深海までの色帯が上から順に流れ込み、深度ラベルが点灯する。
    /// 「どこまで潜るのか」が一目で分かる演出。
    /// </summary>
    public class DepthZonesTransition : ObjectTransition
    {
        [SerializeField] float slideDuration = 0.4f;
        [SerializeField] float stagger = 0.09f;

        static readonly (Color color, string label)[] Bands =
        {
            (new Color(0.72f, 0.88f, 0.95f), "SURFACE"),
            (new Color(0.20f, 0.55f, 0.78f), "-200 m"),
            (new Color(0.10f, 0.32f, 0.55f), "-1000 m"),
            (new Color(0.05f, 0.16f, 0.33f), "-3000 m"),
            (new Color(0.01f, 0.03f, 0.08f), "ABYSS"),
        };

        RectTransform[] _bands;
        Text[] _labels;
        float[] _dirs;
        float _w;

        protected override void Build()
        {
            _w = Root.rect.width;
            int n = Bands.Length;
            _bands = new RectTransform[n];
            _labels = new Text[n];
            _dirs = new float[n];

            for (int i = 0; i < n; i++)
            {
                float dir = (i % 2 == 0) ? 1f : -1f;
                var band = CreateRect(Root, $"Band{i}", Bands[i].color);
                var rt = band.rectTransform;
                rt.anchorMin = new Vector2(0f, 1f - (i + 1) / (float)n);
                rt.anchorMax = new Vector2(1f, 1f - i / (float)n);
                rt.offsetMin = new Vector2(0f, -1f);
                rt.offsetMax = new Vector2(0f, 1f);
                rt.anchoredPosition = new Vector2(-dir * _w, 0f);

                bool bright = i == 0;
                var label = CreateLabel(rt, "Label", Bands[i].label, 34,
                    bright ? new Color(0.1f, 0.25f, 0.4f, 0f) : new Color(1f, 1f, 1f, 0f));
                label.alignment = TextAnchor.MiddleRight;
                var lrt = label.rectTransform;
                lrt.anchorMin = new Vector2(1f, 0.5f);
                lrt.anchorMax = new Vector2(1f, 0.5f);
                lrt.pivot = new Vector2(1f, 0.5f);
                lrt.anchoredPosition = new Vector2(-48f, 0f);
                lrt.sizeDelta = new Vector2(300f, 44f);

                _bands[i] = rt;
                _labels[i] = label;
                _dirs[i] = dir;
            }
        }

        protected override async UniTask CloseRoutine()
        {
            float total = stagger * (_bands.Length - 1) + slideDuration + 0.15f;
            await Tween(total, e =>
            {
                for (int i = 0; i < _bands.Length; i++)
                {
                    float t = Mathf.Clamp01((e - i * stagger) / slideDuration);
                    float x = Mathf.Lerp(-_dirs[i] * _w, 0f, Ease.OutCubic(t));
                    _bands[i].anchoredPosition = new Vector2(x, 0f);

                    var c = _labels[i].color;
                    c.a = Mathf.Clamp01((t - 0.75f) * 4f);
                    _labels[i].color = c;
                }
            });
        }

        protected override async UniTask OpenRoutine()
        {
            float total = stagger * (_bands.Length - 1) + slideDuration;
            await Tween(total, e =>
            {
                for (int i = 0; i < _bands.Length; i++)
                {
                    // 浅い層から順に抜けていく（浮上せず、より深くへ進む感じ）
                    float t = Mathf.Clamp01((e - i * stagger) / slideDuration);
                    float x = Mathf.Lerp(0f, _dirs[i] * _w, Ease.InCubic(t));
                    _bands[i].anchoredPosition = new Vector2(x, 0f);

                    var c = _labels[i].color;
                    c.a = Mathf.Clamp01(1f - t * 2f);
                    _labels[i].color = c;
                }
            });
        }
    }
}
