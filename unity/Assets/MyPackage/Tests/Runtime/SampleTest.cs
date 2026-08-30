using NUnit.Framework;

namespace Nitou.MyPackage.Tests
{
    /// <summary>
    /// テスト構成の疎通確認用サンプル．
    /// 実際のテストを追加したら削除して良い．
    /// </summary>
    public sealed class SampleTest
    {
        [Test]
        public void Sample_Passes()
        {
            Assert.That(true, Is.True);
        }
    }
}
