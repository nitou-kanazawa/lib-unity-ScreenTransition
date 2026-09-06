using System.Collections;
using System.IO;
using UnityEngine;

namespace Waribashi.ScreenTransitions.Demo
{
    /// <summary>
    /// セルフレビュー用: 全パターンを順に再生しながらスクリーンショットを保存する。
    /// Play モード中にエディタコードから StartTour で起動する。
    /// </summary>
    public class DemoAutoCapture : MonoBehaviour
    {
        public static void StartTour(string outputDir, int[] indices = null)
        {
            var controller = FindAnyObjectByType<TransitionDemoController>();
            if (controller == null)
            {
                Debug.LogError("[ScreenTransitions] TransitionDemoController not found.");
                return;
            }

            var go = new GameObject("DemoAutoCapture");
            var capture = go.AddComponent<DemoAutoCapture>();
            capture.StartCoroutine(capture.Tour(controller, outputDir, indices));
        }

        IEnumerator Tour(TransitionDemoController controller, string outputDir, int[] indices)
        {
            Directory.CreateDirectory(outputDir);

            if (indices == null)
            {
                indices = new int[controller.TotalPatternCount];
                for (int i = 0; i < indices.Length; i++)
                    indices[i] = i;
            }

            foreach (int i in indices)
            {
                while (controller.IsBusy)
                    yield return null;
                yield return new WaitForSeconds(0.2f);

                controller.PlayPatternByIndex(i);

                // 閉じ途中を2枚、閉じ切り(HoldLoop中)1枚、開き途中1枚
                yield return new WaitForSecondsRealtime(0.30f);
                ScreenCapture.CaptureScreenshot(Path.Combine(outputDir, $"p{i:00}_close_a.png"));
                yield return new WaitForSecondsRealtime(0.30f);
                ScreenCapture.CaptureScreenshot(Path.Combine(outputDir, $"p{i:00}_close_b.png"));
                yield return new WaitForSecondsRealtime(0.45f);
                ScreenCapture.CaptureScreenshot(Path.Combine(outputDir, $"p{i:00}_closed.png"));
                yield return new WaitForSecondsRealtime(0.65f);
                ScreenCapture.CaptureScreenshot(Path.Combine(outputDir, $"p{i:00}_open.png"));

                while (controller.IsBusy)
                    yield return null;
            }

            Debug.Log("[ScreenTransitions] Capture tour finished: " + outputDir);
            Destroy(gameObject);
        }
    }
}
