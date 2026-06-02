namespace UserPermission;

/// <summary>ライブラリ全体に関わる情報。</summary>
public static class Library
{
    /// <summary>ネイティブコア (Rust) ラッパーのバージョン文字列。</summary>
    public static string Version =>
        NativeMethods.PtrToStringAndFree(NativeMethods.up_version());
}
