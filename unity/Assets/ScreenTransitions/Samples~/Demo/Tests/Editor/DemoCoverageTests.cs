using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace Waribashi.ScreenTransitions.Demo.EditorTests
{
    /// <summary>
    /// デモシーンが全蓋絵を網羅しているかを検証する。
    /// 「新しい蓋絵を実装したがデモに追加し忘れた」「ビルダーは直したがシーンを再生成していない」を
    /// 機械的に検出するのが目的。種類が増えるほど価値が上がる。
    /// </summary>
    public class DemoCoverageTests
    {
        const string DemoSceneName = "TransitionDemo";

        /// <summary>
        /// 具体的な蓋絵すべて。パッケージ本体とサンプルで別アセンブリに分かれているため、
        /// ロード済みアセンブリ全体を走査する。
        ///
        /// ただしテストアセンブリは除く。テスト用の蓋絵（基底クラスの検証のために置いてある）まで
        /// デモシーンへの配置を要求してしまうため。
        /// </summary>
        static IEnumerable<Type> ConcreteCurtainTypes => AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => !IsTestAssembly(a))
            .SelectMany(SafeGetTypes)
            .Where(t => typeof(ObjectCurtain).IsAssignableFrom(t) && !t.IsAbstract)
            .OrderBy(t => t.FullName, StringComparer.Ordinal);

        /// <summary>nunit を参照しているアセンブリはテスト用とみなす。</summary>
        static bool IsTestAssembly(Assembly assembly)
            => assembly.GetReferencedAssemblies().Any(n => n.Name == "nunit.framework");

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

        /// <summary>
        /// シーンのパスは固定しない。Samples~ として配布したあと利用側が
        /// Assets/Samples/&lt;pkg&gt;/&lt;ver&gt;/Demo/ に取り込んでも、そのまま動くようにするため。
        /// </summary>
        static string ResolveDemoScenePath()
        {
            var guid = AssetDatabase.FindAssets($"{DemoSceneName} t:SceneAsset")
                .FirstOrDefault(g => System.IO.Path.GetFileNameWithoutExtension(
                    AssetDatabase.GUIDToAssetPath(g)) == DemoSceneName);
            return string.IsNullOrEmpty(guid) ? null : AssetDatabase.GUIDToAssetPath(guid);
        }

        [Test]
        public void DemoScene_ContainsEveryObjectCurtain()
        {
            var scenePath = ResolveDemoScenePath();
            Assert.IsNotNull(scenePath,
                $"{DemoSceneName}.unity が見つからない。Tools/Screen Transitions/Build Demo Scene で生成すること");

            var found = new HashSet<Type>();
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
            try
            {
                foreach (var root in scene.GetRootGameObjects())
                {
                    foreach (var curtain in root.GetComponentsInChildren<ObjectCurtain>(true))
                        found.Add(curtain.GetType());
                }
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }

            var missing = ConcreteCurtainTypes.Where(t => !found.Contains(t)).Select(t => t.Name).ToArray();

            Assert.IsEmpty(missing,
                "デモシーンに配置されていない蓋絵がある（TransitionDemoBuilder への追加とシーン再生成が必要）: "
                + string.Join(", ", missing));
        }
    }
}
