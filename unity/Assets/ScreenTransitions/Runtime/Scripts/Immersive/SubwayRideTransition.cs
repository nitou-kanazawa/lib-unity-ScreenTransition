using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace Waribashi.ScreenTransitions.Immersive
{
    /// <summary>
    /// 地下鉄の車内: まずつり革や乗客のシルエットがゲーム画面の上にフェードインして揺れはじめ、
    /// その背後で車内（壁・窓・座席）が暗いフェードで閉じる。閉じ切り中は車体が揺れ、窓外を灯りが流れ続ける。
    /// </summary>
    public class SubwayRideTransition : ObjectTransition
    {
        [SerializeField] Color wallColor = new Color(0.10f, 0.11f, 0.14f);
        [SerializeField] Color ceilingColor = new Color(0.14f, 0.15f, 0.19f);
        [SerializeField] Color floorColor = new Color(0.065f, 0.07f, 0.095f);
        [SerializeField] Color outsideColor = new Color(0.012f, 0.016f, 0.03f);
        [SerializeField] Color seatColor = new Color(0.42f, 0.09f, 0.11f);
        [SerializeField] Color figureColor = new Color(0.025f, 0.025f, 0.03f);
        [SerializeField] string locationLabel = "SUBWAY";
        [SerializeField] float appearDuration = 0.5f;
        [SerializeField] float fadeDuration = 0.5f;

        RectTransform _car;
        CanvasGroup _envGroup;
        CanvasGroup _figureGroup;
        RectTransform _chip;
        RectTransform[] _straps;
        SilhouetteFactory.Person[] _hangers;
        RectTransform[] _streaks;
        Image[] _streakImages;
        float[] _streakSpeeds;
        float[] _streakSeeds;
        float _w, _h;
        float _time;

        protected override void Build()
        {
            _w = Root.rect.width;
            _h = Root.rect.height;

            // 揺れ用コンテナ（少し大きめに作って揺れても縁が見えないように）
            var carGo = new GameObject("Car", typeof(RectTransform));
            _car = (RectTransform)carGo.transform;
            _car.SetParent(Root, false);
            _car.anchorMin = Vector2.zero;
            _car.anchorMax = Vector2.one;
            _car.offsetMin = new Vector2(-40f, -40f);
            _car.offsetMax = new Vector2(40f, 40f);

            // 環境レイヤー（後から暗いフェードで現れる）
            var envGo = new GameObject("Environment", typeof(RectTransform), typeof(CanvasGroup));
            var env = (RectTransform)envGo.transform;
            env.SetParent(_car, false);
            StretchChild(env);
            _envGroup = envGo.GetComponent<CanvasGroup>();
            _envGroup.alpha = 0f;

            BuildEnvironment(env);

            // 人物・つり革レイヤー（先にフェードインして揺れはじめる）
            var figGo = new GameObject("Figures", typeof(RectTransform), typeof(CanvasGroup));
            var fig = (RectTransform)figGo.transform;
            fig.SetParent(_car, false);
            StretchChild(fig);
            _figureGroup = figGo.GetComponent<CanvasGroup>();

            BuildFigures(fig);
        }

        void BuildEnvironment(RectTransform env)
        {
            var wall = CreateRect(env, "Wall", wallColor);
            StretchChild(wall.rectTransform);

            // 窓帯（RectMask2D で流れる灯りをクリップ）
            var bandGo = new GameObject("WindowBand", typeof(RectTransform), typeof(RectMask2D));
            var band = (RectTransform)bandGo.transform;
            band.SetParent(env, false);
            band.anchorMin = new Vector2(0f, 0.5f);
            band.anchorMax = new Vector2(1f, 0.5f);
            band.sizeDelta = new Vector2(0f, 230f);
            band.anchoredPosition = new Vector2(0f, 205f);

            var outside = CreateRect(band, "Outside", outsideColor);
            StretchChild(outside.rectTransform);

            _streaks = new RectTransform[14];
            _streakImages = new Image[14];
            _streakSpeeds = new float[14];
            _streakSeeds = new float[14];
            for (int i = 0; i < _streaks.Length; i++)
            {
                var streak = CreateRect(band, $"Streak{i}", new Color(1f, 0.93f, 0.72f, Mathf.Lerp(0.25f, 0.8f, Hash01(i, 7))));
                var rt = streak.rectTransform;
                rt.sizeDelta = new Vector2(Mathf.Lerp(50f, 190f, Hash01(i, 3)), Mathf.Lerp(4f, 8f, Hash01(i, 11)));
                _streaks[i] = rt;
                _streakImages[i] = streak;
                _streakSpeeds[i] = Mathf.Lerp(1500f, 2400f, Hash01(i, 5));
                _streakSeeds[i] = Hash01(i, 23);
            }

            for (int i = 0; i < 6; i++)
            {
                var pillar = CreateRect(env, $"Pillar{i}", wallColor);
                pillar.rectTransform.sizeDelta = new Vector2(52f, 246f);
                pillar.rectTransform.anchoredPosition = new Vector2(-_w * 0.5f + i * (_w * 0.2f) + 40f, 205f);
            }

            var ceiling = CreateRect(env, "Ceiling", ceilingColor);
            ceiling.rectTransform.anchorMin = new Vector2(0f, 1f);
            ceiling.rectTransform.anchorMax = new Vector2(1f, 1f);
            ceiling.rectTransform.pivot = new Vector2(0.5f, 1f);
            ceiling.rectTransform.sizeDelta = new Vector2(0f, 190f);

            var floor = CreateRect(env, "Floor", floorColor);
            floor.rectTransform.anchorMin = new Vector2(0f, 0f);
            floor.rectTransform.anchorMax = new Vector2(1f, 0f);
            floor.rectTransform.pivot = new Vector2(0.5f, 0f);
            floor.rectTransform.sizeDelta = new Vector2(0f, 170f);

            var seatBack = CreateRect(env, "SeatBack", new Color(0.13f, 0.14f, 0.18f));
            seatBack.rectTransform.anchorMin = new Vector2(0f, 0f);
            seatBack.rectTransform.anchorMax = new Vector2(1f, 0f);
            seatBack.rectTransform.pivot = new Vector2(0.5f, 0f);
            seatBack.rectTransform.sizeDelta = new Vector2(0f, 300f);
            seatBack.rectTransform.anchoredPosition = new Vector2(0f, 140f);

            var seat = CreateRect(env, "Seat", seatColor);
            seat.rectTransform.anchorMin = new Vector2(0f, 0f);
            seat.rectTransform.anchorMax = new Vector2(1f, 0f);
            seat.rectTransform.pivot = new Vector2(0.5f, 0f);
            seat.rectTransform.sizeDelta = new Vector2(0f, 60f);
            seat.rectTransform.anchoredPosition = new Vector2(0f, 240f);

            // 行き先チップ
            var chipGo = new GameObject("LocationChip", typeof(Image));
            chipGo.transform.SetParent(env, false);
            var chip = chipGo.GetComponent<Image>();
            chip.color = Color.white;
            chip.raycastTarget = false;
            _chip = chip.rectTransform;
            _chip.anchorMin = _chip.anchorMax = new Vector2(1f, 1f);
            _chip.anchoredPosition = new Vector2(-330f, -150f);
            _chip.sizeDelta = new Vector2(400f, 86f);
            _chip.localRotation = Quaternion.Euler(0f, 0f, 4f);
            _chip.localScale = Vector3.zero;
            var label = CreateLabel(_chip, "Label", locationLabel, 48, new Color(0.03f, 0.03f, 0.03f));
            label.fontStyle = FontStyle.BoldAndItalic;
            StretchChild(label.rectTransform);
        }

        void BuildFigures(RectTransform fig)
        {
            float seatY = -_h * 0.5f + 260f;
            float[] sitterXs = { -_w * 0.30f, -_w * 0.11f, _w * 0.13f, _w * 0.31f };
            foreach (float x in sitterXs)
            {
                var sitter = SilhouetteFactory.CreateSitter(fig, 300f, figureColor);
                sitter.anchoredPosition = new Vector2(x, seatY);
            }

            var rail = CreateRect(fig, "Rail", new Color(0.55f, 0.57f, 0.62f, 0.8f));
            rail.rectTransform.anchorMin = new Vector2(0f, 0.5f);
            rail.rectTransform.anchorMax = new Vector2(1f, 0.5f);
            rail.rectTransform.sizeDelta = new Vector2(0f, 8f);
            rail.rectTransform.anchoredPosition = new Vector2(0f, 345f);

            float[] strapXs = { -_w * 0.42f, -_w * 0.26f, -_w * 0.10f, _w * 0.06f, _w * 0.22f, _w * 0.38f };
            _straps = new RectTransform[strapXs.Length];
            for (int i = 0; i < strapXs.Length; i++)
            {
                var strapGo = new GameObject($"Strap{i}", typeof(RectTransform));
                var strap = (RectTransform)strapGo.transform;
                strap.SetParent(fig, false);
                strap.pivot = new Vector2(0.5f, 1f);
                strap.sizeDelta = new Vector2(50f, 130f);
                strap.anchoredPosition = new Vector2(strapXs[i], 341f);

                var belt = CreateRect(strap, "Belt", new Color(0.8f, 0.8f, 0.82f, 0.85f));
                belt.rectTransform.pivot = new Vector2(0.5f, 1f);
                belt.rectTransform.sizeDelta = new Vector2(7f, 82f);
                belt.rectTransform.anchoredPosition = new Vector2(0f, -2f);

                var ring = CreateSprite(strap, "Ring", ProceduralSprites.Ring, new Color(0.85f, 0.85f, 0.88f, 0.9f));
                ring.rectTransform.sizeDelta = new Vector2(46f, 46f);
                ring.rectTransform.anchoredPosition = new Vector2(0f, -104f);
                _straps[i] = strap;
            }

            float floorY = -_h * 0.5f + 155f;
            int[] hangerStraps = { 1, 3, 5 };
            _hangers = new SilhouetteFactory.Person[hangerStraps.Length];
            for (int i = 0; i < hangerStraps.Length; i++)
            {
                _hangers[i] = SilhouetteFactory.CreateStrapHanger(fig, 430f, figureColor);
                _hangers[i].Root.anchoredPosition = new Vector2(strapXs[hangerStraps[i]] - 30f, floorY);
            }

            foreach (float x in new[] { -_w * 0.34f, _w * 0.30f })
            {
                var pole = CreateRect(fig, "Pole", new Color(0.62f, 0.64f, 0.7f, 0.5f));
                pole.rectTransform.sizeDelta = new Vector2(7f, _h * 0.86f);
                pole.rectTransform.anchoredPosition = new Vector2(x, 40f);
            }
        }

        void Step(float dt)
        {
            _time += dt;

            _car.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(_time * 1.5f) * 0.35f);
            _car.anchoredPosition = new Vector2(Mathf.Sin(_time * 0.9f) * 3f, Mathf.Sin(_time * 2.9f) * 4f);

            for (int i = 0; i < _straps.Length; i++)
                _straps[i].localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(_time * 2.1f + i * 0.85f) * 6.5f);

            for (int i = 0; i < _hangers.Length; i++)
                _hangers[i].Body.anchoredPosition = new Vector2(Mathf.Sin(_time * 2.1f + i) * 3f, 0f);

            for (int i = 0; i < _streaks.Length; i++)
            {
                float span = _w + 400f;
                float x = Mathf.Repeat(_streakSeeds[i] * span - _time * _streakSpeeds[i], span) - span * 0.5f;
                _streaks[i].anchoredPosition = new Vector2(x, (_streakSeeds[i] - 0.5f) * 150f);
                var c = _streakImages[i].color;
                c.a = Mathf.Lerp(0.2f, 0.75f, Hash01(i, 7)) * (0.55f + Mathf.Sin(_time * 1.1f + i * 2.4f) * 0.45f);
                _streakImages[i].color = c;
            }
        }

        protected override async UniTask CloseRoutine()
        {
            _time = 0f;
            _envGroup.alpha = 0f;
            _figureGroup.alpha = 0f;
            _chip.localScale = Vector3.zero;
            Step(0f);

            // 1) つり革と乗客のシルエットが先に現れて揺れはじめる（背後はまだゲーム画面）
            float prev = 0f;
            await Tween(appearDuration, e =>
            {
                float dt = e - prev;
                prev = e;
                Step(dt);
                _figureGroup.alpha = Ease.OutCubic(e / appearDuration);
            });

            // 2) 背後で車内が暗いフェードで閉じる
            prev = 0f;
            await Tween(fadeDuration, e =>
            {
                float dt = e - prev;
                prev = e;
                float t = e / fadeDuration;
                Step(dt);
                _envGroup.alpha = Ease.OutCubic(t);
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
            // 車内が先に暗転して消え、乗客の影が最後に残って薄れる
            float prev = 0f;
            await Tween(0.35f, e =>
            {
                float dt = e - prev;
                prev = e;
                float t = e / 0.35f;
                Step(dt);
                _envGroup.alpha = 1f - Ease.OutCubic(t);
                _chip.localScale = Vector3.one * (1f - t);
            });

            prev = 0f;
            await Tween(0.3f, e =>
            {
                float dt = e - prev;
                prev = e;
                Step(dt);
                _figureGroup.alpha = 1f - Ease.InCubic(e / 0.3f);
            });
            _figureGroup.alpha = 0f;
        }
    }
}
