using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace Waribashi.ScreenTransitions.Immersive
{
    /// <summary>
    /// 街の雑踏（スクランブル交差点の横視点）: 左右から人々がわっと歩き込んできて
    /// 画面を埋め、背後で暗いフェードが閉じて蓋が完成する。歩行速度は全フェーズ等速。
    /// 手前のレーンほど大きく速く（自然なパララックス）、最前列は画面高を超える特大シルエット。
    /// </summary>
    public class TownCrowdTransition : ObjectTransition
    {
        [SerializeField] Color bgColor = new Color(0.84f, 0.04f, 0.08f);
        [SerializeField] Color bgDarkColor = new Color(0.16f, 0.008f, 0.015f);
        [SerializeField] string locationLabel = "DOWNTOWN";
        [SerializeField] float enterDuration = 0.75f;
        [SerializeField] float fadeDuration = 0.5f;

        struct Lane
        {
            public SilhouetteFactory.Person[] People;
            public float[] Offsets;
            public float[] Phases;
            public float[] Speeds;
            public float[] Dirs;
            public float[] Bobs;
            public float GroundY;
            public float StepRate;
        }

        CanvasGroup _backdropGroup;
        CanvasGroup _figureGroup;
        Image _bg;
        RectTransform[] _stripes;
        RectTransform _chip;
        Lane[] _lanes;
        float _w, _h;
        float _walkTime;

        protected override void Build()
        {
            _w = Root.rect.width;
            _h = Root.rect.height;

            // 背景レイヤー（後から暗いフェードで現れる）
            var backdropGo = new GameObject("Backdrop", typeof(RectTransform), typeof(CanvasGroup));
            var backdrop = (RectTransform)backdropGo.transform;
            backdrop.SetParent(Root, false);
            StretchChild(backdrop);
            _backdropGroup = backdropGo.GetComponent<CanvasGroup>();
            _backdropGroup.alpha = 0f;

            _bg = CreateRect(backdrop, "Bg", bgDarkColor);
            StretchChild(_bg.rectTransform);

            _stripes = new RectTransform[4];
            for (int i = 0; i < _stripes.Length; i++)
            {
                var stripe = CreateRect(backdrop, $"Stripe{i}", new Color(0f, 0f, 0f, 0.10f));
                var rt = stripe.rectTransform;
                rt.sizeDelta = new Vector2(240f, _h * 2.4f);
                rt.localRotation = Quaternion.Euler(0f, 0f, 16f);
                _stripes[i] = rt;
            }

            // 路面（接地感を出す暗い帯）
            var road = CreateRect(backdrop, "Road", new Color(0.06f, 0.01f, 0.015f, 0.45f));
            var roadRt = road.rectTransform;
            roadRt.anchorMin = new Vector2(0f, 0f);
            roadRt.anchorMax = new Vector2(1f, 0f);
            roadRt.pivot = new Vector2(0.5f, 0f);
            roadRt.sizeDelta = new Vector2(0f, 175f);

            var chipGo = new GameObject("LocationChip", typeof(Image));
            chipGo.transform.SetParent(backdrop, false);
            var chip = chipGo.GetComponent<Image>();
            chip.color = new Color(0.02f, 0.02f, 0.02f);
            chip.raycastTarget = false;
            _chip = chip.rectTransform;
            _chip.anchorMin = _chip.anchorMax = new Vector2(0f, 1f);
            _chip.anchoredPosition = new Vector2(330f, -150f);
            _chip.sizeDelta = new Vector2(440f, 92f);
            _chip.localRotation = Quaternion.Euler(0f, 0f, -5f);
            _chip.localScale = Vector3.zero;
            var label = CreateLabel(_chip, "Label", locationLabel, 52, Color.white);
            label.fontStyle = FontStyle.BoldAndItalic;
            StretchChild(label.rectTransform);

            // 人物レイヤー（先に歩き込んでくる）。奥 → 手前の順に生成して前後関係を作る
            var figuresGo = new GameObject("Figures", typeof(RectTransform), typeof(CanvasGroup));
            var figures = (RectTransform)figuresGo.transform;
            figures.SetParent(Root, false);
            StretchChild(figures);
            _figureGroup = figuresGo.GetComponent<CanvasGroup>();

            // やや引いた目線（道路の向かい側から見るくらい）: 手前ほど大きく低く、全身が概ね収まる
            _lanes = new Lane[4];
            _lanes[0] = CreateLane(figures, 9, 220f, new Color(0.36f, 0.03f, 0.05f), 150f, _h * 0.08f, 6f, 10);
            _lanes[1] = CreateLane(figures, 8, 360f, new Color(0.13f, 0.01f, 0.02f), 260f, -_h * 0.14f, 7f, 20);
            _lanes[2] = CreateLane(figures, 6, 600f, new Color(0.035f, 0.02f, 0.025f), 430f, -_h * 0.40f, 7.5f, 30);
            // 最前列: 足元だけ画面下に切れる大きめシルエット
            _lanes[3] = CreateLane(figures, 4, 880f, new Color(0.015f, 0.01f, 0.012f), 640f, -_h * 0.56f, 8f, 40);
        }

        Lane CreateLane(Transform parent, int count, float personHeight, Color color, float baseSpeed, float groundY, float stepRate, int seed)
        {
            var laneGo = new GameObject($"Lane_h{(int)personHeight}", typeof(RectTransform));
            var lane = (RectTransform)laneGo.transform;
            lane.SetParent(parent, false);

            var people = new SilhouetteFactory.Person[count];
            var offsets = new float[count];
            var phases = new float[count];
            var speeds = new float[count];
            var dirs = new float[count];
            var bobs = new float[count];

            int leftQueue = 0;
            int rightQueue = 0;
            for (int i = 0; i < count; i++)
            {
                float h = personHeight * Mathf.Lerp(0.9f, 1.1f, Hash01(i, seed + 1));
                people[i] = SilhouetteFactory.CreateWalker(lane, h, color);
                dirs[i] = Hash01(i, seed + 2) > 0.5f ? 1f : -1f;
                speeds[i] = baseSpeed * Mathf.Lerp(0.85f, 1.15f, Hash01(i, seed + 3));
                phases[i] = Hash01(i, seed + 4) * Mathf.PI * 2f;
                bobs[i] = h * 0.013f;

                // 進行方向の手前側（画面外すぐ）から、左右それぞれ詰めて並んで入場する
                int queueIndex = dirs[i] > 0f ? leftQueue++ : rightQueue++;
                offsets[i] = -dirs[i] * (_w * 0.54f + queueIndex * (personHeight * 0.55f) + Hash01(i, seed + 5) * 120f);
            }

            return new Lane
            {
                People = people,
                Offsets = offsets,
                Phases = phases,
                Speeds = speeds,
                Dirs = dirs,
                Bobs = bobs,
                GroundY = groundY,
                StepRate = stepRate,
            };
        }

        void Step(float dt)
        {
            _walkTime += dt;

            for (int s = 0; s < _stripes.Length; s++)
            {
                float x = Mathf.Repeat(s * (_w * 0.55f) + _walkTime * 24f, _w * 2.2f) - _w * 1.1f;
                _stripes[s].anchoredPosition = new Vector2(x, 0f);
            }

            foreach (var lane in _lanes)
            {
                for (int i = 0; i < lane.People.Length; i++)
                {
                    var person = lane.People[i];
                    float raw = lane.Offsets[i] + lane.Dirs[i] * lane.Speeds[i] * _walkTime;
                    float x = Mathf.Repeat(raw + _w * 0.85f, _w * 1.7f) - _w * 0.85f;
                    float step = _walkTime * lane.StepRate + lane.Phases[i];
                    person.Root.anchoredPosition = new Vector2(x, lane.GroundY);
                    person.Body.anchoredPosition = new Vector2(0f, Mathf.Abs(Mathf.Sin(step)) * lane.Bobs[i]);
                    float swing = Mathf.Sin(step) * 22f;
                    person.LegL.localRotation = Quaternion.Euler(0f, 0f, swing);
                    person.LegR.localRotation = Quaternion.Euler(0f, 0f, -swing);
                }
            }
        }

        protected override async UniTask CloseRoutine()
        {
            _walkTime = 0f;
            _figureGroup.alpha = 1f;
            _backdropGroup.alpha = 0f;
            _chip.localScale = Vector3.zero;
            Step(0f);

            // 1) ゲーム画面の上へ、左右から人々がわっと歩き込んでくる（等速）
            float prev = 0f;
            await Tween(enterDuration, e =>
            {
                float dt = e - prev;
                prev = e;
                Step(dt);
            });

            // 2) 群衆の背後で暗いフェードが閉じ、赤へ染まって蓋が完成する
            prev = 0f;
            await Tween(fadeDuration, e =>
            {
                float dt = e - prev;
                prev = e;
                float t = e / fadeDuration;
                Step(dt);
                _backdropGroup.alpha = Ease.OutCubic(t);
                _bg.color = Color.Lerp(bgDarkColor, bgColor, Ease.InCubic(t));
                _chip.localScale = Vector3.one * Mathf.Max(0f, Ease.OutBack(Mathf.Clamp01((t - 0.4f) / 0.6f)));
            });
        }

        protected override async UniTask HoldLoop(CancellationToken ct)
        {
            while (true)
            {
                Step(Time.unscaledDeltaTime);
                await UniTask.Yield(PlayerLoopTiming.Update, ct);
            }
        }

        protected override async UniTask OpenRoutine()
        {
            // 背景が先に暗転して消え、群衆はそのままの歩調のまま薄れて去る
            float prev = 0f;
            await Tween(0.35f, e =>
            {
                float dt = e - prev;
                prev = e;
                float t = e / 0.35f;
                Step(dt);
                _backdropGroup.alpha = 1f - Ease.OutCubic(t);
                _bg.color = Color.Lerp(bgColor, bgDarkColor, t);
                _chip.localScale = Vector3.one * (1f - t);
            });

            prev = 0f;
            await Tween(0.4f, e =>
            {
                float dt = e - prev;
                prev = e;
                Step(dt);
                _figureGroup.alpha = 1f - Ease.InCubic(e / 0.4f);
            });
            _figureGroup.alpha = 1f;
            _backdropGroup.alpha = 0f;
        }
    }
}
