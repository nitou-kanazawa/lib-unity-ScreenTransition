using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace Waribashi.ScreenTransitions.Demo.Submarine
{
    /// <summary>
    /// 魚群のシルエットが横切り、その後ろから深い青の水塊が画面を満たす。
    /// 「水中に生物がいる」ことが分かる演出。
    /// </summary>
    public class FishSchoolTransition : ObjectCurtain
    {
        [SerializeField] Color waterColor = new Color(0.04f, 0.11f, 0.20f);
        [SerializeField] Color fishColor = new Color(0.02f, 0.05f, 0.09f);
        [SerializeField] int fishCount = 26;
        [SerializeField] float duration = 1.1f;

        RectTransform _water;
        RectTransform[] _fishes;
        float[] _seeds;
        float _w, _h;

        protected override void Build()
        {
            _w = Root.rect.width;
            _h = Root.rect.height;

            _water = CreateRect(Root, "Water", waterColor).rectTransform;
            StretchChild(_water);
            _water.anchoredPosition = new Vector2(_w * 1.05f, 0f);

            _fishes = new RectTransform[fishCount];
            _seeds = new float[fishCount];
            for (int i = 0; i < fishCount; i++)
            {
                float seed = Hash01(i, 101);
                var fishGo = new GameObject($"Fish{i}", typeof(RectTransform));
                var fish = (RectTransform)fishGo.transform;
                fish.SetParent(Root, false);
                float scale = Mathf.Lerp(0.65f, 1.5f, Hash01(i, 41));
                fish.sizeDelta = new Vector2(56f, 22f);
                fish.localScale = new Vector3(scale, scale, 1f);

                // 左向きの魚: 楕円の体 + 右側の尾びれ
                var body = CreateSprite(fish, "Body", ProceduralSprites.Circle, fishColor);
                body.rectTransform.sizeDelta = new Vector2(44f, 18f);
                body.rectTransform.anchoredPosition = new Vector2(-6f, 0f);

                var tail = CreateSprite(fish, "Tail", ProceduralSprites.Triangle, fishColor);
                tail.rectTransform.sizeDelta = new Vector2(20f, 20f);
                tail.rectTransform.anchoredPosition = new Vector2(20f, 0f);

                fish.anchoredPosition = new Vector2(_w, 0f);
                _fishes[i] = fish;
                _seeds[i] = seed;
            }
        }

        void ApplyFish(float sweep, float e)
        {
            for (int i = 0; i < _fishes.Length; i++)
            {
                float seed = _seeds[i];
                float lag = Hash01(i, 3) * 0.35f;
                float t = Mathf.Clamp01(sweep * (1.35f + lag) - lag);
                float x = Mathf.Lerp(_w * 0.75f, -_w * 0.85f, t);
                float y = (seed - 0.5f) * _h * 0.9f + Mathf.Sin(e * 6f + i * 1.9f) * 26f;
                _fishes[i].anchoredPosition = new Vector2(x, y);
            }
        }

        protected override async UniTask CloseRoutine()
        {
            await Tween(duration, e =>
            {
                float t = Mathf.Clamp01(e / duration);
                // 魚は等速寄りに泳がせて姿を見せ、水塊が少し遅れて追いつく
                ApplyFish(t, e);
                float wt = Ease.OutCubic(Mathf.Clamp01((t - 0.3f) / 0.7f));
                _water.anchoredPosition = new Vector2(Mathf.Lerp(_w * 1.05f, 0f, wt), 0f);
            });
            _water.anchoredPosition = Vector2.zero;
        }

        protected override async UniTask OpenRoutine()
        {
            await Tween(duration, e =>
            {
                float t = Mathf.Clamp01(e / duration);
                ApplyFish(t, e + 2.4f);
                float wt = Ease.OutCubic(Mathf.Clamp01((t - 0.25f) / 0.75f));
                _water.anchoredPosition = new Vector2(Mathf.Lerp(0f, -_w * 1.05f, wt), 0f);
            });
            _water.anchoredPosition = new Vector2(_w * 1.05f, 0f);
        }
    }
}
