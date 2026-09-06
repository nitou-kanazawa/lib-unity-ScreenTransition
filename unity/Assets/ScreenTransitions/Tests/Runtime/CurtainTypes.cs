using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Waribashi.ScreenTransitions.Tests
{
    /// <summary>
    /// 契約テストのパラメータ源。新しい蓋絵を追加すると自動的に全テストの対象になる。
    ///
    /// パッケージのアセンブリだけでなくロード済みアセンブリ全体を走査する。
    /// テーマ固有の蓋絵はサンプル側の別アセンブリに居るため、自アセンブリだけを見ると
    /// テストが「減るだけで赤くならない」形で対象を取りこぼすため。
    /// </summary>
    public static class CurtainTypes
    {
        /// <summary>具体（非 abstract）な ObjectCurtain 派生型すべて。</summary>
        public static IEnumerable<Type> All => AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(SafeGetTypes)
            .Where(t => typeof(ObjectCurtain).IsAssignableFrom(t) && !t.IsAbstract)
            .OrderBy(t => t.FullName, StringComparer.Ordinal);

        /// <summary>HoldLoop を override している型のみ（Closed 中のループ挙動を検証する対象）。</summary>
        public static IEnumerable<Type> WithHoldLoop => All.Where(OverridesHoldLoop);

        /// <summary>
        /// 型の解決に失敗するアセンブリ（依存が揃っていないもの）があっても走査全体を止めない。
        /// 解決できた型だけを返す。
        /// </summary>
        static IEnumerable<Type> SafeGetTypes(Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException e)
            {
                return e.Types.Where(t => t != null);
            }
        }

        static bool OverridesHoldLoop(Type type)
        {
            var method = type.GetMethod("HoldLoop",
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            return method != null && method.DeclaringType != typeof(ObjectCurtain);
        }
    }
}
