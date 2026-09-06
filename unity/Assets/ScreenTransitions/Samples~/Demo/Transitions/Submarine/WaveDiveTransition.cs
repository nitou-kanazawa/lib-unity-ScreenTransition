using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace Waribashi.ScreenTransitions.Demo.Submarine
{
    /// <summary>
    /// うねる波が下から押し寄せて画面が水没する。波頭には白い泡、水中には気泡が立ち上る。
    /// 「海に入った」ことが分かる入水演出。
    /// </summary>
    public class WaveDiveTransition : ObjectCurtain
    {
        [SerializeField] Color waterColor = new Color(0.05f, 0.15f, 0.28f);
        [SerializeField] Color foamColor = new Color(0.88f, 0.96f, 1f, 0.95f);
        [SerializeField] int crestCount = 11;
        [SerializeField] int bubbleCount = 14;
        [SerializeField] float duration = 0.75f;

        RectTransform _water;
        RectTransform[] _crests;
        RectTransform[] _foams;
        RectTransform[] _bubbles;
        Image[] _bubbleImages;
        float[] _bubbleSeeds;
        float _w, _h;

        protected override void Build()
        {
            _w = Root.rect.width;
            _h = Root.rect.height;

            _water = CreateRect(Root, "Water", waterColor).rectTransform;
            StretchChild(_water);
            _water.anchoredPosition = new Vector2(0f, -_h * 1.25f);

            // 波頭: 水面上端に沿って並べた円で「うねり」を作る
            _crests = new RectTransform[crestCount];
            _foams = new RectTransform[crestCount];
            float crestSize = _w / (crestCount - 2);
            for (int i = 0; i < crestCount; i++)
            {
                var crest = CreateSprite(_water, $"Crest{i}", ProceduralSprites.Circle, waterColor);
                var rt = crest.rectTransform;
                rt.anchorMin = rt.anchorMax = new Vector2(i / (float)(crestCount - 1), 1f);
                rt.sizeDelta = new Vector2(crestSize * 1.5f, crestSize * 1.5f);
                _crests[i] = rt;

                var foam = CreateSprite(_water, $"Foam{i}", ProceduralSprites.Circle, foamColor);
                var frt = foam.rectTransform;
                frt.anchorMin = frt.anchorMax = new Vector2(i / (float)(crestCount - 1), 1f);
                frt.sizeDelta = new Vector2(crestSize * 0.42f, crestSize * 0.42f);
                _foams[i] = frt;
            }

            // 立ち上る気泡
            _bubbles = new RectTransform[bubbleCount];
            _bubbleImages = new Image[bubbleCount];
            _bubbleSeeds = new float[bubbleCount];
            for (int i = 0; i < bubbleCount; i++)
            {
                float seed = Hash01(i, 77);
                var bubble = CreateSprite(Root, $"Bubble{i}", ProceduralSprites.Circle,
                    new Color(1f, 1f, 1f, 0f));
                var rt = bubble.rectTransform;
                float size = Mathf.Lerp(10f, 34f, Hash01(i, 31));
                rt.sizeDelta = new Vector2(size, size);
                _bubbles[i] = rt;
                _bubbleImages[i] = bubble;
                _bubbleSeeds[i] = seed;
            }
        }

        void ApplyWave(float rise, float e, float bubbleAlpha)
        {
            _water.anchoredPosition = new Vector2(0f, Mathf.Lerp(-_h * 1.25f, 0f, rise));
            for (int i = 0; i < crestCount; i++)
            {
                float bob = Mathf.Sin(e * 7f + i * 1.7f) * 22f;
                float bob2 = Mathf.Sin(e * 11f + i * 2.9f) * 9f;
                _crests[i].anchoredPosition = new Vector2(0f, bob + bob2);
                _foams[i].anchoredPosition = new Vector2(Mathf.Cos(e * 5f + i) * 10f,
                    bob + bob2 + _crests[i].sizeDelta.y * 0.42f);
            }
            for (int i = 0; i < _bubbles.Length; i++)
            {
                float seed = _bubbleSeeds[i];
                float x = (seed - 0.5f) * _w * 0.95f;
                float speed = Mathf.Lerp(0.6f, 1.5f, Hash01(i, 13));
                float y = -_h * 0.55f + Mathf.Repeat(e * speed, 1.15f) * _h * 1.1f;
                _bubbles[i].anchoredPosition = new Vector2(x + Mathf.Sin(e * 4f + i) * 14f, y);
                var c = _bubbleImages[i].color;
                c.a = bubbleAlpha * Mathf.Lerp(0.25f, 0.6f, seed);
                _bubbleImages[i].color = c;
            }
        }

        protected override async UniTask CloseRoutine()
        {
            await Tween(duration, e =>
            {
                float t = Mathf.Clamp01(e / duration);
                ApplyWave(Ease.OutCubic(t), e, Mathf.Clamp01(t * 2f));
            });
            ApplyWave(1f, duration, 0f);
        }

        protected override async UniTask HoldLoop(CancellationToken ct)
        {
            // 閉じ切り中: 波打ち際が上端で揺れ続け、気泡がゆっくり立ち上る
            float e = duration;
            while (true)
            {
                e += Time.unscaledDeltaTime;
                ApplyWave(1f, e, 0.45f);
                await UniTask.Yield(PlayerLoopTiming.Update, ct);
            }
        }

        protected override async UniTask OpenRoutine()
        {
            await Tween(duration, e =>
            {
                float t = Mathf.Clamp01(e / duration);
                ApplyWave(1f - Ease.InCubic(t), e + 3f, 1f - t);
            });
            ApplyWave(0f, 0f, 0f);
        }
    }
}
