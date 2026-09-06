using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace Waribashi.ScreenTransitions.EditorTests
{
    /// <summary>
    /// Ease 関数の境界と値域。反射で列挙しているので、Ease に関数を足すと自動的に対象になる。
    /// オーバーシュート系（OutBack / OutBounce）があるため単調増加は要求しない。
    /// </summary>
    public class EaseTests
    {
        /// <summary>float f(float) 形の public static メソッド名。</summary>
        static IEnumerable<string> FunctionNames => Functions.Select(m => m.Name).OrderBy(n => n);

        static IEnumerable<MethodInfo> Functions => typeof(Ease)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(m => m.ReturnType == typeof(float))
            .Where(m => m.GetParameters().Length == 1 && m.GetParameters()[0].ParameterType == typeof(float));

        static float Invoke(string name, float t)
            => (float)Functions.First(m => m.Name == name).Invoke(null, new object[] { t });

        [Test]
        public void Ease_ExposesFunctions()
        {
            Assert.IsNotEmpty(FunctionNames.ToArray(), "Ease に float f(float) 形の関数が1つも見つからない");
        }

        [Test]
        public void Ease_StartsAtZero([ValueSource(nameof(FunctionNames))] string name)
        {
            Assert.AreEqual(0f, Invoke(name, 0f), 1e-4f, $"{name}(0) は 0 であること");
        }

        [Test]
        public void Ease_EndsAtOne([ValueSource(nameof(FunctionNames))] string name)
        {
            Assert.AreEqual(1f, Invoke(name, 1f), 1e-4f, $"{name}(1) は 1 であること");
        }

        /// <summary>
        /// 0..1 の全域で値が常識的な帯に収まる。オーバーシュートは許すが、
        /// 符号ミスや式の破壊で値が吹き飛ぶケースを検出する。
        /// </summary>
        [Test]
        public void Ease_StaysWithinReasonableBand([ValueSource(nameof(FunctionNames))] string name)
        {
            const int steps = 100;
            for (int i = 0; i <= steps; i++)
            {
                float t = i / (float)steps;
                float v = Invoke(name, t);

                Assert.IsFalse(float.IsNaN(v) || float.IsInfinity(v), $"{name}({t}) が NaN/Inf");
                Assert.That(v, Is.InRange(-0.5f, 1.5f),
                    $"{name}({t}) = {v} が想定帯（-0.5..1.5）を外れている");
            }
        }

        /// <summary>入出力が概ね進行方向を向いていること（始点側は小さく、終点側は大きい）。</summary>
        [Test]
        public void Ease_ProgressesForward([ValueSource(nameof(FunctionNames))] string name)
        {
            float early = Invoke(name, 0.25f);
            float late = Invoke(name, 0.75f);
            Assert.Less(early, late, $"{name} は t=0.25 より t=0.75 の方が大きいこと（進行方向が逆）");
        }
    }
}
