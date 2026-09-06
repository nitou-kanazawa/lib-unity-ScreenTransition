using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace Waribashi.ScreenTransitions
{
    /// <summary>
    /// シーンロードを跨いで使うための常駐サービス。
    /// 初回アクセス時に DontDestroyOnLoad の専用 Canvas（最前面）を自前で構築する。
    ///
    /// 典型的な利用:
    /// <code>
    /// var curtain = ScreenTransitionService.Instance.Use&lt;FadeTransition&gt;();
    /// await ScreenTransitionService.Instance.RunAsync(curtain,
    ///     async ct => await SceneManager.LoadSceneAsync("Next").ToUniTask(cancellationToken: ct));
    /// </code>
    /// </summary>
    public class ScreenTransitionService : MonoBehaviour
    {
        const int SortingOrder = 32000;

        static ScreenTransitionService _instance;

        public static ScreenTransitionService Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("ScreenTransitionService");
                    DontDestroyOnLoad(go);
                    _instance = go.AddComponent<ScreenTransitionService>();
                    _instance.BuildCanvas();
                }
                return _instance;
            }
        }

        Canvas _canvas;
        RectTransform _layer;
        ILoadingIndicator _loading;

        /// <summary>
        /// 蓋絵を載せる常駐レイヤー。
        /// 子の並び順がそのまま描画順（後ろの子ほど手前）なので、重ね順を決めたい場合は
        /// SetSiblingIndex / SetAsLastSibling をここに対して使う。
        /// </summary>
        public RectTransform Layer => _layer;

        /// <summary>
        /// 閉じ切っている間の表示。初回アクセス時に既定の実装を生成する。
        ///
        /// ロードを挟まないアプリ（カットインしか使わない等）では何も作られない。
        /// 自前の見た目を使う場合は set する。差し替えたあと既定の実装は生成されない。
        /// </summary>
        public ILoadingIndicator Loading
        {
            get => _loading.IsAlive() ? _loading : (_loading = CreateDefaultLoading());
            set => _loading = value;
        }

        void BuildCanvas()
        {
            var canvasGo = new GameObject("TransitionCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);

            _canvas = canvasGo.GetComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = SortingOrder;

            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            var layerGo = new GameObject("Curtains", typeof(RectTransform));
            _layer = (RectTransform)layerGo.transform;
            _layer.SetParent(canvasGo.transform, false);
            Stretch(_layer);
        }

        /// <summary>既定のローディング表示。蓋絵より手前に来るよう、レイヤーの後ろに置く。</summary>
        LoadingIndicator CreateDefaultLoading()
        {
            var go = new GameObject("LoadingIndicator", typeof(RectTransform), typeof(LoadingIndicator));
            var rt = (RectTransform)go.transform;
            rt.SetParent(_canvas.transform, false);
            Stretch(rt);
            rt.SetAsLastSibling();
            return go.GetComponent<LoadingIndicator>();
        }

        /// <summary>
        /// 型ごとに 1 つを共有する。画面全体を覆う蓋絵のように、同時に 1 つあれば足りるもの向け。
        /// 同じ種類を同時に複数出したい場合は <see cref="Create{T}"/> を使うこと。
        /// </summary>
        public T Use<T>() where T : ObjectCurtain
        {
            var existing = _layer.GetComponentInChildren<T>(true);
            if (existing != null)
                return existing;

            return Create<T>();
        }

        /// <summary>
        /// 常駐レイヤー上に新しい蓋絵を作る。呼ぶたびに別インスタンスになる。
        /// キャラクターごとのカットインのように、同じ型を同時に複数出す場合に使う。
        /// </summary>
        public T Create<T>() where T : ObjectCurtain
        {
            var go = new GameObject(typeof(T).Name, typeof(RectTransform), typeof(T));
            var rt = (RectTransform)go.transform;
            rt.SetParent(_layer, false);
            Stretch(rt);
            return go.GetComponent<T>();
        }

        /// <summary>
        /// 外で用意した蓋絵を常駐レイヤーへ移す。
        ///
        /// Use / Create は手続き生成できる ObjectCurtain 専用だが、RuleImageCurtain のように
        /// Timeline アセットや TransitionImage の参照が要るものはコードから組み立てられない。
        /// プレハブから生成したものをここに渡す。
        /// </summary>
        public T Adopt<T>(T curtain) where T : Component, ICurtain
        {
            if (curtain == null)
                throw new ArgumentNullException(nameof(curtain));

            curtain.transform.SetParent(_layer, false);
            return curtain;
        }

        /// <summary>閉じ → work（ローディング表示 + HoldLoop）→ 開き。</summary>
        public UniTask RunAsync(ICurtain curtain,
            Func<CancellationToken, UniTask> work,
            bool showLoading = true,
            CancellationToken ct = default)
        {
            return curtain.RunAsync(work, showLoading ? Loading : null, ct);
        }

        static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
    }
}
