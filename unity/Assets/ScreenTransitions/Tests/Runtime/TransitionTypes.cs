using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Waribashi.ScreenTransitions.Tests
{
    /// <summary>
    /// 契約テストのパラメータ源。ScreenTransitions アセンブリを反射で走査するので、
    /// 新しい遷移を追加すると自動的に全テストの対象になる。
    /// </summary>
    public static class TransitionTypes
    {
        /// <summary>具体（非 abstract）な ObjectTransition 派生型すべて。</summary>
        public static IEnumerable<Type> All => typeof(ObjectTransition).Assembly
            .GetTypes()
            .Where(t => typeof(ObjectTransition).IsAssignableFrom(t) && !t.IsAbstract)
            .OrderBy(t => t.FullName, StringComparer.Ordinal);

        /// <summary>HoldLoop を override している型のみ（Closed 中のループ挙動を検証する対象）。</summary>
        public static IEnumerable<Type> WithHoldLoop => All.Where(OverridesHoldLoop);

        static bool OverridesHoldLoop(Type type)
        {
            var method = type.GetMethod("HoldLoop",
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            return method != null && method.DeclaringType != typeof(ObjectTransition);
        }
    }
}
