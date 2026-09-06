using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Waribashi.ScreenTransitions.EditorTools
{
    /// <summary>
    /// 12種のルール画像（白→黒グラデーション）を手続き生成する。
    /// 値が小さいピクセルほど先に塗られる（= 先に隠れる）。
    /// 生成後に各パターンを min-max 正規化するので、Cutoff 0→1 で必ず全面をスイープする。
    /// </summary>
    public static class RuleTextureGenerator
    {
        const int Width = 960;
        const int Height = 540;
        const float Aspect = 16f / 9f;
        const string OutputDir = "Packages/com.waribashi.screen-transitions/Runtime/Textures";

        [MenuItem("Tools/Screen Transitions/Generate Rule Textures")]
        public static void GenerateAll()
        {
            Directory.CreateDirectory(OutputDir);

            Generate("Rule_01_CircleIn", CircleIn);
            Generate("Rule_02_CircleOut", CircleOut);
            Generate("Rule_03_Star", Star);
            Generate("Rule_04_Heart", Heart);
            Generate("Rule_05_WipeRight", WipeRight);
            Generate("Rule_06_WipeDiagonal", WipeDiagonal);
            Generate("Rule_07_Staircase", Staircase);
            Generate("Rule_08_Blinds", Blinds);
            Generate("Rule_09_Checkerboard", Checkerboard);
            Generate("Rule_10_DiamondGrid", DiamondGrid);
            Generate("Rule_11_RadialSweep", RadialSweep);
            Generate("Rule_12_RandomBlocks", RandomBlocks);

            AssetDatabase.Refresh();
            Debug.Log("[ScreenTransitions] 12 rule textures generated at " + OutputDir);
        }

        static void Generate(string name, Func<float, float, float> pattern)
        {
            var values = new float[Width * Height];
            float min = float.MaxValue, max = float.MinValue;

            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    float u = (x + 0.5f) / Width;
                    float v = (y + 0.5f) / Height;
                    float val = pattern(u, v);
                    values[y * Width + x] = val;
                    if (val < min) min = val;
                    if (val > max) max = val;
                }
            }

            float range = Mathf.Max(1e-5f, max - min);
            var pixels = new Color32[Width * Height];
            for (int i = 0; i < values.Length; i++)
            {
                byte b = (byte)Mathf.RoundToInt(Mathf.Clamp01((values[i] - min) / range) * 255f);
                pixels[i] = new Color32(b, b, b, 255);
            }

            var tex = new Texture2D(Width, Height, TextureFormat.RGB24, false);
            tex.SetPixels32(pixels);
            tex.Apply();

            string path = $"{OutputDir}/{name}.png";
            File.WriteAllBytes(path, tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);

            AssetDatabase.ImportAsset(path);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Default;
            importer.sRGBTexture = false;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }

        // アスペクト補正済み座標（画面中央原点、y は上方向が正）
        static float AX(float u) => (u - 0.5f) * Aspect;
        static float AY(float v) => v - 0.5f;

        static float Hash(int x, int y)
        {
            uint h = (uint)(x * 374761393 + y * 668265263);
            h = (h ^ (h >> 13)) * 1274126177u;
            h ^= h >> 16;
            return (h & 0xFFFFFF) / (float)0x1000000;
        }

        // 01: 外周から穴が縮んでいく（キャラを丸で囲むアイリスイン）
        static float CircleIn(float u, float v)
        {
            float x = AX(u), y = AY(v);
            return -Mathf.Sqrt(x * x + y * y);
        }

        // 02: 中央から円が広がる
        static float CircleOut(float u, float v)
        {
            float x = AX(u), y = AY(v);
            return Mathf.Sqrt(x * x + y * y);
        }

        // 03: 中央から星形（5芒星）が広がる
        static float Star(float u, float v)
        {
            float x = AX(u), y = AY(v);
            float d = Mathf.Sqrt(x * x + y * y);
            if (d < 1e-5f)
                return 0f;

            const int points = 5;
            float seg = Mathf.PI / points;
            float theta = Mathf.Atan2(x, y); // 上方向を 0 とする角度
            float a = Mathf.Repeat(theta, 2f * seg);
            float t = Mathf.Abs(a - seg) / seg;   // 1 = 外側頂点方向, 0 = 内側頂点方向
            float phi = (1f - t) * seg;           // 最寄りの外側頂点からの角度

            // 外側頂点 (R, 0) と内側頂点 (r*cos(seg), r*sin(seg)) を結ぶ辺と、
            // 角度 phi 方向のレイとの交点までの距離が星形境界の半径
            const float R = 1f;
            const float r = 0.5f;
            float x1 = R, y1 = 0f;
            float x2 = r * Mathf.Cos(seg), y2 = r * Mathf.Sin(seg);
            float denom = (y2 - y1) * Mathf.Cos(phi) - (x2 - x1) * Mathf.Sin(phi);
            if (Mathf.Abs(denom) < 1e-5f)
                denom = 1e-5f;
            float boundary = Mathf.Abs((x1 * y2 - y1 * x2) / denom);
            return d / boundary;
        }

        static bool InsideHeart(float x, float y)
        {
            float a = x * x + y * y - 1f;
            return a * a * a - x * x * y * y * y <= 0f;
        }

        // 04: 中央からハートが広がる
        static float Heart(float u, float v)
        {
            float x = AX(u) * 2.6f;
            float y = AY(v) * 2.6f + 0.2f;

            // 点を含む最小のハートスケールを二分探索で求める
            float lo = 0.02f, hi = 8f;
            for (int i = 0; i < 18; i++)
            {
                float mid = (lo + hi) * 0.5f;
                if (InsideHeart(x / mid, y / mid))
                    hi = mid;
                else
                    lo = mid;
            }
            return hi;
        }

        // 05: 左から右への単純ワイプ
        static float WipeRight(float u, float v) => u;

        // 06: 左上から右下への斜めワイプ
        static float WipeDiagonal(float u, float v) => (u + (1f - v)) * 0.5f;

        // 07: 段々の長方形が横から流れ込む（行ごとに位相をずらす）
        static float Staircase(float u, float v)
        {
            const int rows = 5;
            int row = Mathf.Min(rows - 1, (int)((1f - v) * rows)); // 上の行から先行
            float phase = row / (float)(rows - 1);
            return u * 0.55f + phase * 0.45f;
        }

        // 08: ブラインド（横スラットが順に閉じる）
        static float Blinds(float u, float v)
        {
            const int slats = 8;
            return Mathf.Repeat((1f - v) * slats, 1f);
        }

        // 09: 市松模様のタイルが2波で埋まる
        static float Checkerboard(float u, float v)
        {
            const int nx = 10, ny = 6;
            int ix = Mathf.Min(nx - 1, (int)(u * nx));
            int iy = Mathf.Min(ny - 1, (int)(v * ny));
            float fx = u * nx - ix - 0.5f;
            float fy = v * ny - iy - 0.5f;
            float local = Mathf.Sqrt(fx * fx + fy * fy) / 0.7071f;
            float group = (ix + iy) % 2;
            return group * 0.45f + local * 0.55f;
        }

        // 10: ひし形グリッドが斜めに埋まっていく
        static float DiamondGrid(float u, float v)
        {
            const int nx = 8, ny = 5;
            int ix = Mathf.Min(nx - 1, (int)(u * nx));
            int iy = Mathf.Min(ny - 1, (int)(v * ny));
            float fx = u * nx - ix - 0.5f;
            float fy = v * ny - iy - 0.5f;
            float local = Mathf.Abs(fx) + Mathf.Abs(fy);
            float sweep = (u + (1f - v)) * 0.5f;
            return local * 0.6f + sweep * 0.4f;
        }

        // 11: 時計回りのラジアルスイープ（12時始まり）
        static float RadialSweep(float u, float v)
        {
            float x = AX(u), y = AY(v);
            float ang = Mathf.Atan2(x, y); // 12時方向 = 0、時計回りが正
            return Mathf.Repeat(ang / (2f * Mathf.PI), 1f);
        }

        // 12: ランダムなブロックが順に埋まる
        static float RandomBlocks(float u, float v)
        {
            const int nx = 24, ny = 14;
            int ix = Mathf.Min(nx - 1, (int)(u * nx));
            int iy = Mathf.Min(ny - 1, (int)(v * ny));
            float fx = u * nx - ix - 0.5f;
            float fy = v * ny - iy - 0.5f;
            float local = Mathf.Max(Mathf.Abs(fx), Mathf.Abs(fy)) * 2f;
            return Hash(ix, iy) * 0.85f + local * 0.15f;
        }
    }
}
