using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace Waribashi.ScreenTransitions.Demo.CutIn
{
    /// <summary>
    /// 戦闘中のキャラクターカットイン。破れた帯が画面を斜めに横切り、顔と「!!」が飛び込んでくる。
    ///
    /// このライブラリで「画面の一部しか覆わない蓋絵」を書く例。要点は 2 つ。
    /// - BlocksRaycasts を false にしている。覆っていない領域のボタンは押せたままになる
    /// - Close / Open のルーチンで Tween を数珠つなぎにしている。Complete() で早送りしたとき、
    ///   残り全部が同じフレームで終端まで進むことを確かめられる
    ///
    /// 素材は持たず、ProceduralSprites と Image の矩形だけで組み立てる。
    /// 顔は帯からはみ出す大きさで作り、帯に付けた Mask で切り取っている。
    /// </summary>
    public class CharacterCutIn : ObjectCurtain
    {
        public string characterName = "JOKER";
        public Color accentColor = new Color(0.84f, 0.05f, 0.10f);
        public Color hairColor = new Color(0.07f, 0.07f, 0.09f);
        public Color skinColor = new Color(0.98f, 0.86f, 0.74f);

        const float BandHeight = 240f;
        const float BandWidthRatio = 1.25f;
        const float Tilt = -5f;
        const int TeethCount = 34;
        const float EnterDuration = 0.20f;
        const float MarkDuration = 0.10f;
        const float ExitDuration = 0.16f;

        RectTransform _band;
        RectTransform _clip;
        RectTransform _face;
        RectTransform _marks;
        Vector2 _bandHome;
        float _faceHomeX;
        float _faceTravel;
        float _bandWidth;

        /// <summary>画面の一部しか覆わないので、下の UI の入力は生かしておく。</summary>
        public override bool BlocksRaycasts => false;

        protected override void Build()
        {
            float w = Root.rect.width;
            float h = Root.rect.height;
            _bandWidth = w * BandWidthRatio;
            _faceTravel = BandHeight * 1.6f;

            var bandGo = new GameObject("Band", typeof(RectTransform));
            _band = (RectTransform)bandGo.transform;
            _band.SetParent(Root, false);
            _band.sizeDelta = new Vector2(_bandWidth, BandHeight);
            _bandHome = new Vector2(0f, h * 0.02f);
            _band.anchoredPosition = _bandHome;
            _band.localRotation = Quaternion.Euler(0f, 0f, Tilt);

            BuildClip();
            BuildFace();
            BuildName();

            // 破れは帯の外側にはみ出すので、Mask の外（帯の直接の子）に置く
            BuildTornEdge(1f);
            BuildTornEdge(-1f);

            BuildExclamations(w, h);

            SetProgress(0f);
            SetMarks(0f);
        }

        /// <summary>帯の下地。同時に中身（顔・名前）の切り抜き枠になる。</summary>
        void BuildClip()
        {
            var fill = CreateRect(_band, "Clip", accentColor);
            _clip = fill.rectTransform;
            StretchChild(_clip);

            var mask = _clip.gameObject.AddComponent<Mask>();
            mask.showMaskGraphic = true;
        }

        /// <summary>帯の上下に走る「破れ」。白い三角を不揃いに並べ、根元を帯に食い込ませる。</summary>
        void BuildTornEdge(float dir)
        {
            int seed = dir > 0f ? 1 : 2;
            float step = _bandWidth / TeethCount;

            for (int i = 0; i < TeethCount; i++)
            {
                var tooth = CreateSprite(_band, "Tooth" + seed + "_" + i, ProceduralSprites.Triangle, Color.white);
                float size = Mathf.Lerp(16f, 34f, Hash01(i, seed));
                var rt = tooth.rectTransform;
                rt.sizeDelta = new Vector2(step * 1.6f, size);
                // 底辺が帯の縁より 4px 内側に来るように置く（縁に隙間を作らない）
                rt.anchoredPosition = new Vector2(
                    -_bandWidth * 0.5f + step * (i + 0.5f),
                    dir * (BandHeight * 0.5f + size * 0.5f - 4f));
                rt.localRotation = Quaternion.Euler(0f, 0f, dir > 0f ? 0f : 180f);
            }
        }

        /// <summary>
        /// 顔。帯より大きく作って Mask で切り取る（帯の中に納めようとすると小さくなりすぎる）。
        /// 肌 → 眉・目 → 前髪 の順に重ね、前髪が目の上にかかる形にする。
        /// </summary>
        void BuildFace()
        {
            var faceGo = new GameObject("Face", typeof(RectTransform));
            _face = (RectTransform)faceGo.transform;
            _face.SetParent(_clip, false);
            _face.sizeDelta = new Vector2(BandHeight * 2f, BandHeight * 2f);
            _faceHomeX = -_bandWidth * 0.16f;
            _face.anchoredPosition = new Vector2(_faceHomeX, -BandHeight * 0.18f);

            var skin = CreateSprite(_face, "Skin", ProceduralSprites.Circle, skinColor);
            skin.rectTransform.sizeDelta = new Vector2(BandHeight * 1.50f, BandHeight * 2.00f);

            var brow = CreateRect(_face, "Brow", new Color(0.10f, 0.10f, 0.12f));
            brow.rectTransform.sizeDelta = new Vector2(BandHeight * 0.46f, BandHeight * 0.05f);
            brow.rectTransform.anchoredPosition = new Vector2(BandHeight * 0.16f, BandHeight * 0.42f);
            brow.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -10f);

            var eyeWhite = CreateSprite(_face, "EyeWhite", ProceduralSprites.Circle, Color.white);
            eyeWhite.rectTransform.sizeDelta = new Vector2(BandHeight * 0.54f, BandHeight * 0.32f);
            eyeWhite.rectTransform.anchoredPosition = new Vector2(BandHeight * 0.16f, BandHeight * 0.26f);

            var iris = CreateSprite(_face, "Iris", ProceduralSprites.Circle, new Color(0.12f, 0.12f, 0.15f));
            iris.rectTransform.sizeDelta = new Vector2(BandHeight * 0.22f, BandHeight * 0.22f);
            iris.rectTransform.anchoredPosition = new Vector2(BandHeight * 0.21f, BandHeight * 0.25f);

            var highlight = CreateSprite(_face, "Highlight", ProceduralSprites.Circle, Color.white);
            highlight.rectTransform.sizeDelta = new Vector2(BandHeight * 0.07f, BandHeight * 0.07f);
            highlight.rectTransform.anchoredPosition = new Vector2(BandHeight * 0.26f, BandHeight * 0.30f);

            // 前髪。帯の上端で切られる高さに置き、房の裾が目の上にかかるようにする
            for (int i = 0; i < 9; i++)
            {
                float t = i / 8f;
                var tuft = CreateSprite(_face, "Hair" + i, ProceduralSprites.Circle, hairColor);
                float width = Mathf.Lerp(BandHeight * 0.34f, BandHeight * 0.26f, Hash01(i, 7));
                tuft.rectTransform.sizeDelta = new Vector2(width, width * 1.5f);
                tuft.rectTransform.anchoredPosition = new Vector2(
                    Mathf.Lerp(-BandHeight * 0.56f, BandHeight * 0.50f, t),
                    Mathf.Lerp(BandHeight * 0.68f, BandHeight * 0.54f, t));
                tuft.rectTransform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(-26f, 22f, t));
            }
        }

        void BuildName()
        {
            var label = CreateLabel(_clip, "Name", characterName, 64, Color.white);
            var rt = label.rectTransform;
            rt.sizeDelta = new Vector2(760f, 90f);
            rt.anchoredPosition = new Vector2(BandHeight * 0.65f, -BandHeight * 0.02f);
            label.alignment = TextAnchor.MiddleLeft;
        }

        /// <summary>「!!」。フォント任せにせず、棒と点で組む。</summary>
        void BuildExclamations(float w, float h)
        {
            var marksGo = new GameObject("Marks", typeof(RectTransform));
            _marks = (RectTransform)marksGo.transform;
            _marks.SetParent(Root, false);
            _marks.sizeDelta = new Vector2(320f, 380f);
            _marks.anchoredPosition = new Vector2(w * 0.27f, h * 0.19f);
            _marks.localRotation = Quaternion.Euler(0f, 0f, 7f);

            for (int i = 0; i < 2; i++)
            {
                float scale = i == 0 ? 1f : 0.76f;
                float x = -70f + i * 140f;

                // 棒と点をくっつけると 1 本の長い棒に見えるので、間を空ける
                var bar = CreateRect(_marks, "Bar" + i, Color.white);
                bar.rectTransform.sizeDelta = new Vector2(86f * scale, 150f * scale);
                bar.rectTransform.anchoredPosition = new Vector2(x, 72f * scale);

                var dot = CreateRect(_marks, "Dot" + i, Color.white);
                dot.rectTransform.sizeDelta = new Vector2(86f * scale, 80f * scale);
                dot.rectTransform.anchoredPosition = new Vector2(x, -76f * scale);
            }
        }

        // ------------------------------------------------------------------
        // アニメーション
        // ------------------------------------------------------------------

        protected override async UniTask CloseRoutine()
        {
            // Tween を 2 本つないでいる。Complete() の早送りはこの残り全部に効く
            await Tween(EnterDuration, elapsed => SetProgress(Ease.OutQuart(elapsed / EnterDuration)));
            await Tween(MarkDuration, elapsed => SetMarks(Ease.OutBack(elapsed / MarkDuration)));
        }

        protected override async UniTask OpenRoutine()
        {
            await Tween(ExitDuration, elapsed =>
            {
                float t = Ease.InQuart(elapsed / ExitDuration);
                SetMarks(1f - t);
                SetProgress(1f - t);
            });
        }

        /// <summary>閉じ切っている間の細かい揺れ。止め絵にしない。</summary>
        protected override async UniTask HoldLoop(CancellationToken ct)
        {
            float time = 0f;
            while (true)
            {
                time += Time.unscaledDeltaTime;
                _band.anchoredPosition = _bandHome
                    + new Vector2(Mathf.Sin(time * 46f) * 3.5f, Mathf.Cos(time * 37f) * 2.5f);
                await UniTask.Yield(PlayerLoopTiming.Update, ct);
            }
        }

        /// <summary>0 = 何も無い / 1 = 帯と顔が定位置。</summary>
        void SetProgress(float t)
        {
            t = Mathf.Clamp01(t);
            _band.localScale = new Vector3(1f, t, 1f);
            _band.anchoredPosition = _bandHome;
            _face.anchoredPosition = new Vector2(
                _faceHomeX - _faceTravel * (1f - t),
                -BandHeight * 0.18f);
        }

        void SetMarks(float t)
        {
            t = Mathf.Clamp01(t);
            _marks.localScale = new Vector3(t, t, 1f);
        }
    }
}
