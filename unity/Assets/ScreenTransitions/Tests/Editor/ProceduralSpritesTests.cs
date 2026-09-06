using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace Waribashi.ScreenTransitions.EditorTests
{
    /// <summary>
    /// 手続き生成スプライトの健全性とキャッシュ挙動。
    /// キャッシュキーの取り違え（別プロパティが同じスプライトを返す）は実際に起きやすいので明示的に検証する。
    /// </summary>
    public class ProceduralSpritesTests
    {
        static IEnumerable<string> SpriteNames => Properties.Select(p => p.Name).OrderBy(n => n);

        static IEnumerable<PropertyInfo> Properties => typeof(ProceduralSprites)
            .GetProperties(BindingFlags.Public | BindingFlags.Static)
            .Where(p => p.PropertyType == typeof(Sprite));

        static Sprite Get(string name) => (Sprite)Properties.First(p => p.Name == name).GetValue(null);

        [Test]
        public void ProceduralSprites_ExposesSprites()
        {
            Assert.IsNotEmpty(SpriteNames.ToArray(), "public static Sprite プロパティが1つも見つからない");
        }

        [Test]
        public void Sprite_IsUsable([ValueSource(nameof(SpriteNames))] string name)
        {
            var sprite = Get(name);
            Assert.IsNotNull(sprite, $"{name} が null");
            Assert.IsNotNull(sprite.texture, $"{name}.texture が null");
            Assert.Greater(sprite.texture.width, 0, $"{name} の幅が 0");
            Assert.Greater(sprite.texture.height, 0, $"{name} の高さが 0");
        }

        [Test]
        public void Sprite_IsCached([ValueSource(nameof(SpriteNames))] string name)
        {
            var first = Get(name);
            var second = Get(name);
            Assert.AreSame(first, second, $"{name} は2回目のアクセスでキャッシュを返すこと（毎回生成していると GC を焼く）");
        }

        /// <summary>
        /// 白（RGB=1）で生成し、アルファで形を作る方式。アルファが全面 0 や全面 1 だと
        /// 形が出ていない＝生成器が壊れているので、変化があることを確認する。
        /// </summary>
        [Test]
        public void Sprite_HasVaryingAlpha([ValueSource(nameof(SpriteNames))] string name)
        {
            var pixels = Get(name).texture.GetPixels();
            float min = pixels.Min(p => p.a);
            float max = pixels.Max(p => p.a);

            Assert.Less(min, 0.1f, $"{name} に透明な領域が無い（形が出ていない）");
            Assert.Greater(max, 0.9f, $"{name} に不透明な領域が無い（形が出ていない）");
        }

        /// <summary>キャッシュキーの取り違えで別プロパティが同じインスタンスを返していないこと。</summary>
        [Test]
        public void Sprites_AreDistinctInstances()
        {
            var all = SpriteNames.ToDictionary(n => n, Get);
            var names = all.Keys.ToArray();

            for (int i = 0; i < names.Length; i++)
            {
                for (int j = i + 1; j < names.Length; j++)
                {
                    Assert.AreNotSame(all[names[i]], all[names[j]],
                        $"{names[i]} と {names[j]} が同一インスタンス（キャッシュキーの取り違え）");
                }
            }
        }
    }
}
