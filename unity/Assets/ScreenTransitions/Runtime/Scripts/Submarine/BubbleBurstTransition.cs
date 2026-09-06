using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace Waribashi.ScreenTransitions.Submarine
{
    /// <summary>
    /// 大量の泡が湧き上がって膨らみ、画面を塗り潰す。開きは泡が弾けて視界が開ける。
    /// 「水中に飛び込んだ瞬間」の演出。
    /// </summary>
    public class BubbleBurstTransition : ObjectTransition
    {
        [SerializeField] Color bubbleColor = new Color(0.06f, 0.17f, 0.30f);
        [SerializeField] Color foamColor = new Color(0.85f, 0.95f, 1f, 0.55f);
        [SerializeField] int cols = 7;
        [SerializeField] int rows = 4;
        [SerializeField] float growDuration = 0.30f;
        [SerializeField] float wave = 0.34f;

        RectTransform[] _bubbles;
        float[] _delays;
        float[] _targets;
        RectTransform[] _foamDots;
        Image[] _foamImages;
        Image _cover;
        float _h;

        protected override void Build()
        {
            float w = Root.rect.width;
            _h = Root.rect.height;
            float cellW = w / cols;
            float cellH = _h / rows;
            float baseSize = Mathf.Sqrt(cellW * cellW + cellH * cellH);

            _bubbles = new RectTransform[cols * rows];
            _delays = new float[cols * rows];
            _targets = new float[cols * rows];

            for (int y = 0; y < rows; y++)
            {
                for (int x = 0; x < cols; x++)
                {
                    int i = y * cols + x;
                    var bubble = CreateSprite(Root, $"Bubble{i}", ProceduralSprites.Circle, bubbleColor);
                    var rt = bubble.rectTransform;
                    rt.anchorMin = rt.anchorMax = new Vector2(
                        (x + 0.5f) / cols + (Hash01(x, y) - 0.5f) * 0.06f,
                        (y + 0.5f) / rows + (Hash01(y, x) - 0.5f) * 0.08f);
                    rt.sizeDelta = new Vector2(baseSize, baseSize);
                    rt.localScale = Vector3.zero;
                    _bubbles[i] = rt;
                    // 下の泡ほど先に膨らむ（湧き上がり感）
                    _delays[i] = (1f - (y + 0.5f) / rows) * wave * 0.6f + Hash01(i, 5) * wave * 0.4f;
                    _targets[i] = Mathf.Lerp(1.25f, 1.6f, Hash01(i, 9));
                }
            }

            // 手前を舞う小さな白泡
            _foamDots = new RectTransform[16];
            _foamImages = new Image[16];
            for (int i = 0; i < _foamDots.Length; i++)
            {
                var foam = CreateSprite(Root, $"Foam{i}", ProceduralSprites.Circle,
                    new Color(foamColor.r, foamColor.g, foamColor.b, 0f));
                var rt = foam.rectTransform;
                rt.anchorMin = rt.anchorMax = new Vector2(Hash01(i, 21), Hash01(i, 43) * 0.9f);
                float s = Mathf.Lerp(8f, 22f, Hash01(i, 3));
                rt.sizeDelta = new Vector2(s, s);
                _foamDots[i] = rt;
                _foamImages[i] = foam;
            }

            _cover = CreateRect(Root, "Cover", bubbleColor);
            StretchChild(_cover.rectTransform);
            _cover.gameObject.SetActive(false);
            _cover.transform.SetSiblingIndex(0); // 泡の後ろで隙間だけ塞ぐ
        }

        void ApplyFoam(float e, float alpha)
        {
            for (int i = 0; i < _foamDots.Length; i++)
            {
                _foamDots[i].anchoredPosition = new Vector2(
                    Mathf.Sin(e * 3f + i * 1.3f) * 18f,
                    Mathf.Repeat(e * Mathf.Lerp(60f, 160f, Hash01(i, 3)), _h * 0.4f));
                var c = _foamImages[i].color;
                c.a = alpha * Mathf.Lerp(0.3f, 0.7f, Hash01(i, 55));
                _foamImages[i].color = c;
            }
        }

        protected override async UniTask CloseRoutine()
        {
            float total = wave + growDuration;
            await Tween(total, e =>
            {
                for (int i = 0; i < _bubbles.Length; i++)
                {
                    float t = Mathf.Clamp01((e - _delays[i]) / growDuration);
                    float s = Mathf.Max(0f, Ease.OutBack(t)) * _targets[i];
                    _bubbles[i].localScale = new Vector3(s, s, 1f);
                }
                ApplyFoam(e, Mathf.Clamp01(e / 0.25f));
            });
            _cover.gameObject.SetActive(true);
            ApplyFoam(total, 0f);
        }

        protected override async UniTask OpenRoutine()
        {
            _cover.gameObject.SetActive(false);
            float total = wave + growDuration;
            await Tween(total, e =>
            {
                for (int i = 0; i < _bubbles.Length; i++)
                {
                    // 閉じと逆順（上から）で弾けて消える
                    float delay = wave - _delays[i];
                    float t = Mathf.Clamp01((e - delay) / growDuration);
                    float s = Mathf.Max(0f, 1f - Ease.InBack(t)) * _targets[i];
                    _bubbles[i].localScale = new Vector3(s, s, 1f);
                }
                ApplyFoam(e + 1.7f, 1f - Mathf.Clamp01(e / total));
            });
            ApplyFoam(0f, 0f);
        }
    }
}
