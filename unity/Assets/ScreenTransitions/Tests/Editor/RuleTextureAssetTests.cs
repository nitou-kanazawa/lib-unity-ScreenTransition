using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Waribashi.ScreenTransitions.EditorTests
{
    /// <summary>
    /// ルール画像アセットのインポート設定と画素分布。
    /// RuleTextureGenerator を弄って「全面スイープしないルール画像」を出してしまう回帰を検出する。
    /// </summary>
    public class RuleTextureAssetTests
    {
        const string TextureDirectory = "Packages/com.waribashi.screen-transitions/Runtime/Textures";

        static IEnumerable<string> RuleTexturePaths => AssetDatabase
            .FindAssets("t:Texture2D", new[] { TextureDirectory })
            .Select(AssetDatabase.GUIDToAssetPath)
            .Where(p => Path.GetFileName(p).StartsWith("Rule_"))
            .OrderBy(p => p, System.StringComparer.Ordinal);

        static TextureImporter ImporterAt(string path) => (TextureImporter)AssetImporter.GetAtPath(path);

        [Test]
        public void RuleTextures_Exist()
        {
            Assert.IsNotEmpty(RuleTexturePaths.ToArray(), $"{TextureDirectory} に Rule_*.png が無い");
        }

        /// <summary>
        /// シェーダーはルール画像の値をそのまま Cutoff と比較するので、sRGB 変換が入ると
        /// スイープの進み方が歪む。ミップと圧縮も同様にエッジを壊す。
        /// </summary>
        [Test]
        public void RuleTexture_IsLinearUncompressedAndUnmipped(
            [ValueSource(nameof(RuleTexturePaths))] string path)
        {
            var importer = ImporterAt(path);
            Assert.IsNotNull(importer, $"{path} の TextureImporter が取得できない");

            Assert.IsFalse(importer.sRGBTexture, $"{path}: sRGB はオフであること（ルール値をそのまま使う）");
            Assert.IsFalse(importer.mipmapEnabled, $"{path}: ミップマップはオフであること");
            Assert.AreEqual(TextureImporterType.Default, importer.textureType, $"{path}: textureType は Default であること");
            Assert.AreEqual(TextureWrapMode.Clamp, importer.wrapMode, $"{path}: wrapMode は Clamp であること");
            Assert.AreEqual(TextureImporterCompression.Uncompressed,
                importer.GetDefaultPlatformTextureSettings().textureCompression,
                $"{path}: 非圧縮であること（圧縮ノイズがスイープの境界を汚す）");
        }

        /// <summary>ルール画像は同一シェーダーで差し替えて使うので、全て同じ解像度で揃っていること。</summary>
        [Test]
        public void RuleTextures_ShareSameDimensions()
        {
            var sizes = RuleTexturePaths
                .Select(p => AssetDatabase.LoadAssetAtPath<Texture2D>(p))
                .Select(t => (t.width, t.height))
                .Distinct()
                .ToArray();

            Assert.AreEqual(1, sizes.Length,
                "ルール画像の解像度が揃っていない: " + string.Join(", ", sizes.Select(s => $"{s.width}x{s.height}")));
        }

        /// <summary>
        /// min-max 正規化により、Cutoff 0→1 で画面全体が必ず覆われる（＝画素値が 0..1 の全域を使う）。
        /// 正規化が抜けると「Cutoff=1 でも隅が残る」という致命的な症状になる。
        /// </summary>
        [Test]
        public void RuleTexture_UsesFullValueRange([ValueSource(nameof(RuleTexturePaths))] string path)
        {
            // インポート済みテクスチャは isReadable=0 なので、元ファイルをデコードして読む
            var bytes = File.ReadAllBytes(path);
            var decoded = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            try
            {
                Assert.IsTrue(ImageConversion.LoadImage(decoded, bytes, markNonReadable: false),
                    $"{path} を PNG としてデコードできない");

                var pixels = decoded.GetPixels();
                float min = pixels.Min(p => p.r);
                float max = pixels.Max(p => p.r);

                Assert.Less(min, 0.02f, $"{path}: 最小値 {min:F3} が 0 に届いていない（開始時点で覆われている領域がある）");
                Assert.Greater(max, 0.98f, $"{path}: 最大値 {max:F3} が 1 に届いていない（Cutoff=1 でも覆われない領域が残る）");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(decoded);
            }
        }
    }
}
