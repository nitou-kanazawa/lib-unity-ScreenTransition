using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Waribashi.ScreenTransitions
{
    /// <summary>
    /// タイルが斜めの波＋ランダムゆらぎで OutBack ポップしながら画面を埋める。
    /// </summary>
    public class TilePopTransition : ObjectTransition
    {
        [SerializeField] int cols = 10;
        [SerializeField] int rows = 6;
        [SerializeField] Color colorA = new Color(0.07f, 0.08f, 0.11f);
        [SerializeField] Color colorB = new Color(0.10f, 0.12f, 0.16f);
        [SerializeField] float closeDuration = 0.26f;
        [SerializeField] float openDuration = 0.22f;
        [SerializeField] float wave = 0.32f;
        [SerializeField] float jitter = 0.05f;

        RectTransform[] _tiles;
        float[] _closeDelays;
        float[] _openDelays;

        protected override void Build()
        {
            _tiles = new RectTransform[cols * rows];
            _closeDelays = new float[cols * rows];
            _openDelays = new float[cols * rows];

            for (int y = 0; y < rows; y++)
            {
                for (int x = 0; x < cols; x++)
                {
                    int index = y * cols + x;
                    var tile = CreateRect(Root, $"Tile{index}", (x + y) % 2 == 0 ? colorA : colorB);
                    var rt = tile.rectTransform;
                    rt.anchorMin = new Vector2((float)x / cols, (float)y / rows);
                    rt.anchorMax = new Vector2((float)(x + 1) / cols, (float)(y + 1) / rows);
                    // スケール中の隙間を減らすため少し大きめに
                    rt.offsetMin = new Vector2(-1f, -1f);
                    rt.offsetMax = new Vector2(1f, 1f);
                    rt.localScale = Vector3.zero;

                    float diag = (x + y) / (float)(cols + rows - 2);
                    float rev = ((cols - 1 - x) + (rows - 1 - y)) / (float)(cols + rows - 2);
                    float noise = Hash01(x, y) * jitter;
                    _closeDelays[index] = diag * wave + noise;
                    _openDelays[index] = rev * wave + noise;
                    _tiles[index] = rt;
                }
            }
        }

        protected override async UniTask CloseRoutine()
        {
            float total = wave + jitter + closeDuration;
            await Tween(total, e =>
            {
                for (int i = 0; i < _tiles.Length; i++)
                {
                    float t = Mathf.Clamp01((e - _closeDelays[i]) / closeDuration);
                    float s = t <= 0f ? 0f : Mathf.Max(0f, Ease.OutBack(t));
                    _tiles[i].localScale = new Vector3(s, s, 1f);
                }
            });
        }

        protected override async UniTask OpenRoutine()
        {
            float total = wave + jitter + openDuration;
            await Tween(total, e =>
            {
                for (int i = 0; i < _tiles.Length; i++)
                {
                    float t = Mathf.Clamp01((e - _openDelays[i]) / openDuration);
                    // InBack で一瞬膨らんでから消える（アンチシペーション）
                    float s = Mathf.Max(0f, 1f - Ease.InBack(t));
                    _tiles[i].localScale = new Vector3(s, s, 1f);
                }
            });
        }
    }
}
