using UnityEngine;

namespace Waribashi.ScreenTransitions
{
    /// <summary>
    /// アートアセット不要でオブジェクト系演出を組むための手続き生成スプライト集。
    /// すべて白色で生成するので Image.color でティントして使う。
    /// </summary>
    public static class ProceduralSprites
    {
        static Sprite _circle;
        static Sprite _ring;
        static Sprite _triangle;
        static Sprite _gradientV;
        static Sprite _irisHole;

        /// <summary>アンチエイリアス付きの塗り潰し円。</summary>
        public static Sprite Circle
        {
            get { if (_circle == null) _circle = MakeCircle(128); return _circle; }
        }

        /// <summary>細いリング（ソナー・舷窓の縁など）。</summary>
        public static Sprite Ring
        {
            get { if (_ring == null) _ring = MakeRing(128, 0.80f); return _ring; }
        }

        /// <summary>右向きの三角形（魚の尾びれなど）。</summary>
        public static Sprite Triangle
        {
            get { if (_triangle == null) _triangle = MakeTriangle(64); return _triangle; }
        }

        /// <summary>上端で不透明、下端で透明になる縦グラデーション。</summary>
        public static Sprite GradientV
        {
            get { if (_gradientV == null) _gradientV = MakeGradientV(4, 128); return _gradientV; }
        }

        /// <summary>中央に透明な円孔が空いた板（潜望鏡アイリス用）。孔の半径はテクスチャの 120/512。</summary>
        public static Sprite IrisHole
        {
            get { if (_irisHole == null) _irisHole = MakeIrisHole(512, 120f); return _irisHole; }
        }

        /// <summary>IrisHole の孔半径（scale 1・sizeDelta 512 のときのピクセル値）。</summary>
        public const float IrisHoleRadius = 120f;
        public const float IrisHoleSize = 512f;

        static Sprite FromTexture(Texture2D tex)
        {
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            tex.hideFlags = HideFlags.DontSave;
            tex.Apply();
            var sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
            sprite.hideFlags = HideFlags.DontSave;
            return sprite;
        }

        static Sprite MakeCircle(int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float c = size * 0.5f;
            float r = size * 0.5f - 1.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float d = Mathf.Sqrt((x + 0.5f - c) * (x + 0.5f - c) + (y + 0.5f - c) * (y + 0.5f - c));
                    float a = Mathf.Clamp01((r - d) / 1.5f);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
            }
            return FromTexture(tex);
        }

        static Sprite MakeRing(int size, float innerRatio)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float c = size * 0.5f;
            float rOut = size * 0.5f - 1.5f;
            float rIn = rOut * innerRatio;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float d = Mathf.Sqrt((x + 0.5f - c) * (x + 0.5f - c) + (y + 0.5f - c) * (y + 0.5f - c));
                    float a = Mathf.Min(Mathf.Clamp01((rOut - d) / 1.5f), Mathf.Clamp01((d - rIn) / 1.5f));
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
            }
            return FromTexture(tex);
        }

        static Sprite MakeTriangle(int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float u = (x + 0.5f) / size;                 // 0=左(底辺) 1=右(頂点)
                    float v = Mathf.Abs((y + 0.5f) / size - 0.5f) * 2f;
                    float a = Mathf.Clamp01(((1f - u) - v) * size * 0.5f);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
            }
            return FromTexture(tex);
        }

        static Sprite MakeGradientV(int width, int height)
        {
            var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            for (int y = 0; y < height; y++)
            {
                float a = y / (float)(height - 1); // 上端(最終行)で 1
                for (int x = 0; x < width; x++)
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
            return FromTexture(tex);
        }

        static Sprite MakeIrisHole(int size, float holeRadius)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float c = size * 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float d = Mathf.Sqrt((x + 0.5f - c) * (x + 0.5f - c) + (y + 0.5f - c) * (y + 0.5f - c));
                    float a = Mathf.Clamp01((d - holeRadius) / 2f);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
            }
            return FromTexture(tex);
        }
    }
}
