using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using Waribashi.ScreenTransitions.Demo.CutIn;

namespace Waribashi.ScreenTransitions.Demo
{
    /// <summary>
    /// キャラクターカットインのデモ。
    ///
    /// 見どころは 2 つ。
    /// - カットイン中でも右のカウンタボタンが押せる（CharacterCutIn.BlocksRaycasts = false）。
    ///   同じ位置で FADE（全画面の蓋絵）を出すと押せなくなるので、対比で確かめられる
    /// - 連続発動は await を並べるだけ。合成のための API はライブラリに要らない
    /// </summary>
    public class CutInDemoController : MonoBehaviour
    {
        public CharacterCutIn[] cutIns;
        public Button[] cutInButtons;
        public Button chainButton;
        public Button fadeButton;
        public FadeTransition fade;
        public Button counterButton;
        public Text counterLabel;
        public Text statusLabel;
        public float holdSeconds = 0.35f;

        int _count;
        bool _running;

        void Start()
        {
            for (int i = 0; i < cutInButtons.Length && i < cutIns.Length; i++)
            {
                int index = i;
                cutInButtons[i].onClick.AddListener(() => PlayOne(index).Forget());
            }

            if (chainButton != null)
                chainButton.onClick.AddListener(() => PlayChain().Forget());

            if (fadeButton != null)
                fadeButton.onClick.AddListener(() => PlayFade().Forget());

            if (counterButton != null)
                counterButton.onClick.AddListener(Count);

            UpdateCounter();
            SetStatus("Ready");
        }

        void Count()
        {
            _count++;
            UpdateCounter();
        }

        void UpdateCounter()
        {
            if (counterLabel != null)
                counterLabel.text = "TAP ME\n" + _count;
        }

        async UniTaskVoid PlayOne(int index)
        {
            if (_running)
                return;
            _running = true;

            var cutIn = cutIns[index];
            SetStatus(cutIn.characterName + " — カットイン中もタップできる");
            await cutIn.RunAsync(holdSeconds);
            SetStatus("Ready");

            _running = false;
        }

        /// <summary>連続発動。連鎖は await を並べるだけで足りる。</summary>
        async UniTaskVoid PlayChain()
        {
            if (_running)
                return;
            _running = true;

            for (int i = 0; i < cutIns.Length; i++)
            {
                SetStatus("CHAIN CUT-IN  " + (i + 1) + " / " + cutIns.Length);
                await cutIns[i].RunAsync(0.18f);
            }

            SetStatus("Ready");
            _running = false;
        }

        /// <summary>対比用。全画面の蓋絵はレイキャストを遮断するので、閉じている間はタップできない。</summary>
        async UniTaskVoid PlayFade()
        {
            if (_running)
                return;
            _running = true;

            SetStatus("FADE（全画面）— 閉じている間はタップできない");
            await fade.RunAsync(1.2f);
            SetStatus("Ready");

            _running = false;
        }

        void SetStatus(string text)
        {
            if (statusLabel != null)
                statusLabel.text = text;
        }
    }
}
