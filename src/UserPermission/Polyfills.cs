#if NETSTANDARD2_0
namespace System.Runtime.CompilerServices
{
    /// <summary>
    /// netstandard2.0 で <c>init</c> アクセサ (record の位置パラメータ等) を使うための polyfill。
    /// net5.0 以降ではフレームワークに含まれるため不要。
    /// </summary>
    internal static class IsExternalInit
    {
    }
}
#endif
