using UnityEngine;
using UnityEngine.UI;

namespace Waribashi.ScreenTransitions
{
    /// <summary>
    /// 蓋が閉じている間に表示するローディング演出（スピナー + NOW LOADING テキスト）。
    /// 子要素は初回 Show 時に手続き生成する。トランジションより前面に配置すること。
    /// </summary>
    public class LoadingIndicator : MonoBehaviour
    {
        [SerializeField] Color color = new Color(1f, 1f, 1f, 0.9f);
        [SerializeField] int dotCount = 10;
        [SerializeField] float radius = 34f;
        [SerializeField] float dotSize = 10f;
        [SerializeField] float cycleTime = 1.0f;

        GameObject _content;
        Image[] _dots;
        Text _label;
        float _time;

        public bool IsVisible => _content != null && _content.activeSelf;

        public void Show()
        {
            EnsureBuilt();
            _time = 0f;
            _content.SetActive(true);
        }

        public void Hide()
        {
            if (_content != null)
                _content.SetActive(false);
        }

        void EnsureBuilt()
        {
            if (_content != null)
                return;

            _content = new GameObject("Content", typeof(RectTransform));
            var content = (RectTransform)_content.transform;
            content.SetParent(transform, false);
            content.anchorMin = new Vector2(1f, 0f);
            content.anchorMax = new Vector2(1f, 0f);
            content.pivot = new Vector2(1f, 0f);
            content.anchoredPosition = new Vector2(-60f, 60f);
            content.sizeDelta = new Vector2(320f, 110f);

            var spinnerGo = new GameObject("Spinner", typeof(RectTransform));
            var spinner = (RectTransform)spinnerGo.transform;
            spinner.SetParent(content, false);
            spinner.anchorMin = spinner.anchorMax = new Vector2(1f, 0.5f);
            spinner.anchoredPosition = new Vector2(-radius - 10f, 0f);
            spinner.sizeDelta = new Vector2(radius * 2f, radius * 2f);

            _dots = new Image[dotCount];
            for (int i = 0; i < dotCount; i++)
            {
                var dotGo = new GameObject($"Dot{i}", typeof(Image));
                dotGo.transform.SetParent(spinner, false);
                var image = dotGo.GetComponent<Image>();
                image.color = color;
                image.raycastTarget = false;
                var rt = image.rectTransform;
                rt.sizeDelta = new Vector2(dotSize, dotSize);
                float angle = (float)i / dotCount * Mathf.PI * 2f;
                rt.anchoredPosition = new Vector2(Mathf.Sin(angle), Mathf.Cos(angle)) * radius;
                _dots[i] = image;
            }

            var labelGo = new GameObject("Label", typeof(Text));
            labelGo.transform.SetParent(content, false);
            _label = labelGo.GetComponent<Text>();
            _label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _label.fontSize = 30;
            _label.color = color;
            _label.alignment = TextAnchor.MiddleRight;
            _label.horizontalOverflow = HorizontalWrapMode.Overflow;
            _label.raycastTarget = false;
            var lrt = _label.rectTransform;
            lrt.anchorMin = new Vector2(0f, 0.5f);
            lrt.anchorMax = new Vector2(1f, 0.5f);
            lrt.offsetMin = new Vector2(0f, -25f);
            lrt.offsetMax = new Vector2(-radius * 2f - 30f, 25f);

            _content.SetActive(false);
        }

        void Update()
        {
            if (!IsVisible)
                return;

            _time += Time.unscaledDeltaTime;
            float phase = _time / cycleTime;
            for (int i = 0; i < _dots.Length; i++)
            {
                // 先頭が明るく、後ろへ行くほど薄くなる回転ハイライト
                float offset = Mathf.Repeat(phase - (float)i / _dots.Length, 1f);
                var c = color;
                c.a = Mathf.Lerp(0.15f, 1f, 1f - offset) * color.a;
                _dots[i].color = c;
            }

            int dots = 1 + (int)(_time * 2.5f) % 3;
            _label.text = "NOW LOADING" + new string('.', dots);
        }
    }
}
