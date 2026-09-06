using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace Waribashi.ScreenTransitions.Demo.Submarine
{
    /// <summary>
    /// 画面が消灯してソナースコープに切り替わる。リングが広がり、スイープが回り、輝点が明滅する。
    /// 「艦内の計器で潜航している」ことが分かる演出。
    /// </summary>
    public class SonarPingTransition : ObjectCurtain
    {
        [SerializeField] Color screenColor = new Color(0.01f, 0.05f, 0.045f);
        [SerializeField] Color sonarColor = new Color(0.25f, 1f, 0.6f);
        [SerializeField] float closeDuration = 1.0f;
        [SerializeField] float openDuration = 0.6f;
        [SerializeField] int ringCount = 3;
        [SerializeField] int blipCount = 5;

        Image _cover;
        Image[] _rings;
        RectTransform _sweep;
        Image _sweepImage;
        Image[] _blips;
        Image _grid;
        float _diag;

        protected override void Build()
        {
            float w = Root.rect.width;
            float h = Root.rect.height;
            _diag = Mathf.Sqrt(w * w + h * h);

            _cover = CreateRect(Root, "Screen", new Color(screenColor.r, screenColor.g, screenColor.b, 0f));
            StretchChild(_cover.rectTransform);

            // 距離環（固定の薄いリング）
            _grid = CreateSprite(Root, "RangeRing", ProceduralSprites.Ring,
                new Color(sonarColor.r, sonarColor.g, sonarColor.b, 0f));
            _grid.rectTransform.sizeDelta = new Vector2(h * 0.72f, h * 0.72f);

            // 広がるピングリング
            _rings = new Image[ringCount];
            for (int i = 0; i < ringCount; i++)
            {
                var ring = CreateSprite(Root, $"Ping{i}", ProceduralSprites.Ring,
                    new Color(sonarColor.r, sonarColor.g, sonarColor.b, 0f));
                ring.rectTransform.sizeDelta = new Vector2(220f, 220f);
                ring.rectTransform.localScale = Vector3.zero;
                _rings[i] = ring;
            }

            // 回転スイープ（中心から伸びる線）
            var sweepGo = new GameObject("Sweep", typeof(RectTransform));
            _sweep = (RectTransform)sweepGo.transform;
            _sweep.SetParent(Root, false);
            _sweep.sizeDelta = Vector2.zero;
            _sweepImage = CreateRect(_sweep.transform, "Line", new Color(sonarColor.r, sonarColor.g, sonarColor.b, 0f));
            var line = _sweepImage.rectTransform;
            line.pivot = new Vector2(0f, 0.5f);
            line.sizeDelta = new Vector2(_diag * 0.5f, 4f);
            line.anchoredPosition = Vector2.zero;
            _sweepImage.raycastTarget = false;

            // 輝点
            _blips = new Image[blipCount];
            for (int i = 0; i < blipCount; i++)
            {
                var blip = CreateSprite(Root, $"Blip{i}", ProceduralSprites.Circle,
                    new Color(sonarColor.r, sonarColor.g, sonarColor.b, 0f));
                float ang = Hash01(i, 61) * Mathf.PI * 2f;
                float dist = Mathf.Lerp(h * 0.12f, h * 0.34f, Hash01(i, 17));
                blip.rectTransform.anchoredPosition = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * dist;
                blip.rectTransform.sizeDelta = new Vector2(16f, 16f);
                _blips[i] = blip;
            }
        }

        void SetAlpha(Image image, Color baseColor, float a)
        {
            var c = baseColor;
            c.a = a;
            image.color = c;
        }

        void ApplySonar(float e, float master)
        {
            SetAlpha(_grid, sonarColor, 0.22f * master);
            SetAlpha(_sweepImage, sonarColor, 0.75f * master);
            _sweep.localRotation = Quaternion.Euler(0f, 0f, -e * 420f);

            for (int i = 0; i < _rings.Length; i++)
            {
                float cycle = Mathf.Repeat(e * 0.9f - i * 0.33f, 1f);
                float s = cycle * _diag / 220f;
                _rings[i].rectTransform.localScale = new Vector3(s, s, 1f);
                SetAlpha(_rings[i], sonarColor, (1f - cycle) * 0.8f * master);
            }

            for (int i = 0; i < _blips.Length; i++)
            {
                // スイープが通過した直後に光って減衰する
                float blink = Mathf.Repeat(e * 1.4f - Hash01(i, 61), 1f);
                SetAlpha(_blips[i], sonarColor, Mathf.Pow(1f - blink, 2.5f) * master);
            }
        }

        protected override async UniTask CloseRoutine()
        {
            await Tween(closeDuration, e =>
            {
                float t = Mathf.Clamp01(e / closeDuration);
                SetAlpha(_cover, screenColor, Mathf.Clamp01(t * 2.4f));
                ApplySonar(e, Mathf.Clamp01((t - 0.18f) / 0.3f));
            });
        }

        protected override async UniTask HoldLoop(CancellationToken ct)
        {
            // 閉じ切り中: スイープが回り続け、輝点が明滅し続ける
            float e = closeDuration;
            while (true)
            {
                e += Time.unscaledDeltaTime;
                ApplySonar(e, 1f);
                await UniTask.Yield(PlayerLoopTiming.Update, ct);
            }
        }

        protected override async UniTask OpenRoutine()
        {
            await Tween(openDuration, e =>
            {
                float t = Mathf.Clamp01(e / openDuration);
                SetAlpha(_cover, screenColor, 1f - Ease.OutCubic(t));
                ApplySonar(e + closeDuration, 1f - t);
            });
            ApplySonar(0f, 0f);
        }
    }
}
