using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace Waribashi.ScreenTransitions.Submarine
{
    /// <summary>
    /// 青みがかった水のとばりが降り、差し込む光条（ゴッドレイ）が細り、やがて漆黒になる。
    /// 「深く沈んで光が届かなくなる」ことが分かる演出。
    /// </summary>
    public class DeepFadeTransition : ObjectTransition
    {
        [SerializeField] Color waterTint = new Color(0.05f, 0.18f, 0.32f);
        [SerializeField] Color abyssColor = new Color(0.005f, 0.008f, 0.015f);
        [SerializeField] float closeDuration = 1.15f;
        [SerializeField] float openDuration = 0.7f;

        Image _tint;
        Image _darkness;
        Image _abyss;
        Image[] _rays;
        RectTransform[] _motes;
        Image[] _moteImages;
        float _h;

        protected override void Build()
        {
            float w = Root.rect.width;
            _h = Root.rect.height;

            _tint = CreateRect(Root, "WaterTint", new Color(waterTint.r, waterTint.g, waterTint.b, 0f));
            StretchChild(_tint.rectTransform);

            // 上から降りてくる暗幕（下端がグラデーション）
            _darkness = CreateSprite(Root, "Darkness", ProceduralSprites.GradientV,
                new Color(abyssColor.r, abyssColor.g, abyssColor.b, 1f));
            var drt = _darkness.rectTransform;
            drt.anchorMin = new Vector2(0f, 0f);
            drt.anchorMax = new Vector2(1f, 0f);
            drt.pivot = new Vector2(0.5f, 0f);
            drt.sizeDelta = new Vector2(0f, _h * 1.6f);
            drt.anchoredPosition = new Vector2(0f, _h * 1.7f); // 画面上の外

            // 光条
            _rays = new Image[4];
            for (int i = 0; i < _rays.Length; i++)
            {
                var ray = CreateSprite(Root, $"Ray{i}", ProceduralSprites.GradientV,
                    new Color(0.8f, 0.95f, 1f, 0f));
                var rt = ray.rectTransform;
                rt.anchorMin = rt.anchorMax = new Vector2(0.15f + i * 0.23f, 1f);
                rt.pivot = new Vector2(0.5f, 1f);
                rt.sizeDelta = new Vector2(Mathf.Lerp(90f, 190f, Hash01(i, 7)), _h * 1.5f);
                rt.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(-24f, -10f, i / 3f));
                _rays[i] = ray;
            }

            _abyss = CreateRect(Root, "Abyss", new Color(abyssColor.r, abyssColor.g, abyssColor.b, 0f));
            StretchChild(_abyss.rectTransform);

            // 漂う塵（マリンスノー）。閉じ切り中も見えるよう黒幕より前面に置く
            _motes = new RectTransform[12];
            _moteImages = new Image[12];
            for (int i = 0; i < _motes.Length; i++)
            {
                var mote = CreateSprite(Root, $"Mote{i}", ProceduralSprites.Circle,
                    new Color(0.85f, 0.92f, 1f, 0f));
                var rt = mote.rectTransform;
                rt.anchorMin = rt.anchorMax = new Vector2(Hash01(i, 91), Hash01(i, 37));
                float s = Mathf.Lerp(4f, 9f, Hash01(i, 11));
                rt.sizeDelta = new Vector2(s, s);
                _motes[i] = rt;
                _moteImages[i] = mote;
            }
        }

        void Apply(float e, float tintA, float darknessT, float rayA, float abyssA, float moteA)
        {
            var tc = waterTint;
            tc.a = tintA;
            _tint.color = tc;

            _darkness.rectTransform.anchoredPosition = new Vector2(0f, Mathf.Lerp(_h * 1.7f, 0f, darknessT));

            for (int i = 0; i < _rays.Length; i++)
            {
                var rc = _rays[i].color;
                rc.a = rayA * Mathf.Lerp(0.10f, 0.22f, Hash01(i, 7)) * (0.75f + Mathf.Sin(e * 2.2f + i * 2f) * 0.25f);
                _rays[i].color = rc;
                _rays[i].rectTransform.localScale = new Vector3(1f - darknessT * 0.4f, 1f, 1f);
            }

            for (int i = 0; i < _motes.Length; i++)
            {
                _motes[i].anchoredPosition = new Vector2(
                    Mathf.Sin(e * 1.6f + i * 2.2f) * 26f,
                    Mathf.Repeat(e * Mathf.Lerp(26f, 70f, Hash01(i, 11)) + i * 40f, _h * 0.5f) - _h * 0.25f);
                var mc = _moteImages[i].color;
                mc.a = moteA * Mathf.Lerp(0.25f, 0.6f, Hash01(i, 29));
                _moteImages[i].color = mc;
            }

            var ac = abyssColor;
            ac.a = abyssA;
            _abyss.color = ac;
        }

        protected override async UniTask CloseRoutine()
        {
            await Tween(closeDuration, e =>
            {
                float t = Mathf.Clamp01(e / closeDuration);
                Apply(e,
                    Mathf.Clamp01(t * 2.5f) * 0.85f,
                    Ease.InCubic(Mathf.Clamp01((t - 0.1f) / 0.75f)),
                    Mathf.Clamp01(t * 3f) * (1f - Mathf.Clamp01((t - 0.35f) / 0.45f)),
                    Ease.InCubic(Mathf.Clamp01((t - 0.55f) / 0.45f)),
                    Mathf.Clamp01(t * 2f) * (1f - Mathf.Clamp01((t - 0.6f) / 0.35f)));
            });
            Apply(closeDuration, 1f, 1f, 0f, 1f, 0f);
        }

        protected override async UniTask HoldLoop(CancellationToken ct)
        {
            // 閉じ切り中: 漆黒の中をマリンスノーが漂い続ける
            float e = closeDuration;
            while (true)
            {
                e += Time.unscaledDeltaTime;
                Apply(e, 1f, 1f, 0f, 1f, 0.55f);
                await UniTask.Yield(PlayerLoopTiming.Update, ct);
            }
        }

        protected override async UniTask OpenRoutine()
        {
            await Tween(openDuration, e =>
            {
                float t = Mathf.Clamp01(e / openDuration);
                Apply(e + 5f,
                    (1f - t) * 0.85f,
                    1f - Ease.OutCubic(t),
                    Mathf.Clamp01(t * 1.5f) * (1f - t) * 0.8f,
                    1f - Ease.OutCubic(Mathf.Clamp01(t * 1.8f)),
                    0f);
            });
            Apply(0f, 0f, 0f, 0f, 0f, 0f);
        }
    }
}
