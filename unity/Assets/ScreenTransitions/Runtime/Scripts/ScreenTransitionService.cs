using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace Waribashi.ScreenTransitions
{
    /// <summary>
    /// シーンロードを跨いで使うための常駐トランジションサービス。
    /// 初回アクセス時に DontDestroyOnLoad の専用 Canvas（最前面）を自前で構築する。
    ///
    /// 典型的な利用:
    /// <code>
    /// var t = ScreenTransitionService.Instance.Use&lt;WaveDiveTransition&gt;();
    /// await ScreenTransitionService.Instance.RunAsync(t,
    ///     async ct => await SceneManager.LoadSceneAsync("Undersea").ToUniTask(cancellationToken: ct));
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

        RectTransform _layer;
        LoadingIndicator _loading;

        public LoadingIndicator Loading => _loading;

        void BuildCanvas()
        {
            var canvasGo = new GameObject("TransitionCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = SortingOrder;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            var layerGo = new GameObject("Transitions", typeof(RectTransform));
            _layer = (RectTransform)layerGo.transform;
            _layer.SetParent(canvasGo.transform, false);
            Stretch(_layer);

            var loadingGo = new GameObject("LoadingIndicator", typeof(RectTransform), typeof(LoadingIndicator));
            var loadingRt = (RectTransform)loadingGo.transform;
            loadingRt.SetParent(canvasGo.transform, false);
            Stretch(loadingRt);
            _loading = loadingGo.GetComponent<LoadingIndicator>();
        }

        /// <summary>指定型のトランジションを常駐 Canvas 上に取得（無ければ生成）する。</summary>
        public T Use<T>() where T : ObjectTransition
        {
            var existing = _layer.GetComponentInChildren<T>(true);
            if (existing != null)
                return existing;

            var go = new GameObject(typeof(T).Name, typeof(RectTransform), typeof(T));
            var rt = (RectTransform)go.transform;
            rt.SetParent(_layer, false);
            Stretch(rt);
            return go.GetComponent<T>();
        }

        /// <summary>閉じ → work（ローディング表示 + HoldLoop）→ 開き。</summary>
        public UniTask RunAsync(IScreenTransition transition,
            Func<CancellationToken, UniTask> work,
            bool showLoading = true,
            CancellationToken ct = default)
        {
            return transition.RunAsync(work, showLoading ? _loading : null, ct);
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
