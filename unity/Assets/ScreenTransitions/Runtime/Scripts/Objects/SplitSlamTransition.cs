using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace Waribashi.ScreenTransitions
{
    /// <summary>
    /// 上下のパネルが勢いよく閉じ、衝突時にアクセントラインのフラッシュと画面シェイクが入る。
    /// </summary>
    public class SplitSlamTransition : ObjectCurtain
    {
        [SerializeField] Color panelColor = new Color(0.045f, 0.05f, 0.07f);
        [SerializeField] Color accentColor = new Color(1f, 0.62f, 0.24f);
        [SerializeField] float closeDuration = 0.26f;
        [SerializeField] float openDuration = 0.30f;
        [SerializeField] float shakeDuration = 0.14f;
        [SerializeField] float shakeAmplitude = 5f;

        RectTransform _top;
        RectTransform _bottom;
        Image _flash;
        float _half;

        protected override void Build()
        {
            _half = Root.rect.height * 0.5f + 4f;

            _top = CreatePanel("TopPanel", true);
            _bottom = CreatePanel("BottomPanel", false);

            _flash = CreateRect(Root, "Flash", new Color(accentColor.r, accentColor.g, accentColor.b, 0f));
            var frt = _flash.rectTransform;
            frt.anchorMin = new Vector2(0f, 0.5f);
            frt.anchorMax = new Vector2(1f, 0.5f);
            frt.sizeDelta = new Vector2(0f, 14f);
            _flash.raycastTarget = false;
        }

        RectTransform CreatePanel(string name, bool isTop)
        {
            var panel = CreateRect(Root, name, panelColor);
            var rt = panel.rectTransform;
            if (isTop)
            {
                rt.anchorMin = new Vector2(0f, 0.5f);
                rt.anchorMax = new Vector2(1f, 1f);
                rt.offsetMin = new Vector2(0f, -2f); // 中央の継ぎ目を重ねる
                rt.offsetMax = new Vector2(0f, 0f);
            }
            else
            {
                rt.anchorMin = new Vector2(0f, 0f);
                rt.anchorMax = new Vector2(1f, 0.5f);
                rt.offsetMin = new Vector2(0f, 0f);
                rt.offsetMax = new Vector2(0f, 2f);
            }

            // 閉じたときの合わせ目にアクセントの細いラインを仕込む
            var edge = CreateRect(rt, "Edge", accentColor);
            var ert = edge.rectTransform;
            if (isTop)
            {
                ert.anchorMin = new Vector2(0f, 0f);
                ert.anchorMax = new Vector2(1f, 0f);
                ert.pivot = new Vector2(0.5f, 0f);
            }
            else
            {
                ert.anchorMin = new Vector2(0f, 1f);
                ert.anchorMax = new Vector2(1f, 1f);
                ert.pivot = new Vector2(0.5f, 1f);
            }
            ert.sizeDelta = new Vector2(0f, 5f);
            ert.anchoredPosition = Vector2.zero;

            rt.anchoredPosition = new Vector2(0f, isTop ? _half : -_half);
            return rt;
        }

        protected override async UniTask CloseRoutine()
        {
            // 加速しながらスラム
            await Tween(closeDuration, e =>
            {
                float t = Ease.InQuart(Mathf.Clamp01(e / closeDuration));
                _top.anchoredPosition = new Vector2(0f, Mathf.Lerp(_half, 0f, t));
                _bottom.anchoredPosition = new Vector2(0f, Mathf.Lerp(-_half, 0f, t));
            });

            // 衝突: フラッシュ + シェイク
            await Tween(shakeDuration, e =>
            {
                float n = Mathf.Clamp01(e / shakeDuration);
                float shake = Mathf.Sin(e * 70f) * shakeAmplitude * (1f - n);
                _top.anchoredPosition = new Vector2(0f, shake);
                _bottom.anchoredPosition = new Vector2(0f, shake);
                var c = _flash.color;
                c.a = 1f - n;
                _flash.color = c;
            });

            _top.anchoredPosition = Vector2.zero;
            _bottom.anchoredPosition = Vector2.zero;
            _flash.color = new Color(accentColor.r, accentColor.g, accentColor.b, 0f);
        }

        protected override async UniTask HoldLoop(CancellationToken ct)
        {
            // 閉じ切り中: 合わせ目のアクセントラインが静かに明滅する
            float e = 0f;
            while (true)
            {
                e += Time.unscaledDeltaTime;
                var c = _flash.color;
                c.a = 0.15f + (Mathf.Sin(e * 2.4f) * 0.5f + 0.5f) * 0.25f;
                _flash.color = c;
                await UniTask.Yield(PlayerLoopTiming.Update, ct);
            }
        }

        protected override async UniTask OpenRoutine()
        {
            _flash.color = new Color(accentColor.r, accentColor.g, accentColor.b, 0f);
            await Tween(openDuration, e =>
            {
                float t = Ease.OutQuart(Mathf.Clamp01(e / openDuration));
                _top.anchoredPosition = new Vector2(0f, Mathf.Lerp(0f, _half, t));
                _bottom.anchoredPosition = new Vector2(0f, Mathf.Lerp(0f, -_half, t));
            });
        }
    }
}
