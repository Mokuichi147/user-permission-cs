using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

namespace UserPermission;

/// <summary>
/// ネイティブライブラリ <c>user_permission_csharp</c> の解決を補助する。
/// </summary>
/// <remarks>
/// 既定の解決 (アプリ基準ディレクトリや NuGet パッケージの <c>runtimes/{rid}/native/</c>
/// を deps.json 経由で解決) を先に試し、見つからない場合は基準ディレクトリ配下の
/// <c>runtimes/{rid}/native/</c> を直接探索する。これにより、deps.json に頼らず
/// ネイティブ資産を同梱する .NET ツール (dnx / <c>dotnet tool</c>) でも解決できる。
///
/// 初期化は <see cref="NativeMethods"/> の静的コンストラクタから一度だけ呼ばれる。
/// </remarks>
internal static class NativeLibraryResolver
{
    private const string LibraryName = "user_permission_csharp";

    internal static void Initialize()
    {
        NativeLibrary.SetDllImportResolver(typeof(NativeLibraryResolver).Assembly, Resolve);
    }

    private static IntPtr Resolve(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
    {
        if (!string.Equals(libraryName, LibraryName, StringComparison.Ordinal))
            return IntPtr.Zero; // 対象外は既定の解決に委ねる

        // 1) 既定の解決 (アプリ基準ディレクトリ・deps.json の runtimes など)。
        if (NativeLibrary.TryLoad(libraryName, assembly, searchPath, out IntPtr handle))
            return handle;

        // 2) フォールバック: 基準ディレクトリ配下の runtimes/{rid}/native/ を探索。
        string baseDir = AppContext.BaseDirectory;
        string fileName = NativeFileName();
        foreach (string rid in CandidateRids())
        {
            string candidate = Path.Combine(baseDir, "runtimes", rid, "native", fileName);
            if (File.Exists(candidate) && NativeLibrary.TryLoad(candidate, out IntPtr h))
                return h;
        }

        return IntPtr.Zero; // 既定の失敗処理 (DllNotFoundException) に委ねる
    }

    private static string NativeFileName()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return "user_permission_csharp.dll";
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            return "libuser_permission_csharp.dylib";
        return "libuser_permission_csharp.so";
    }

    private static IEnumerable<string> CandidateRids()
    {
        string os = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "win"
            : RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? "osx"
            : "linux";
        string arch = RuntimeInformation.OSArchitecture switch
        {
            Architecture.X64 => "x64",
            Architecture.Arm64 => "arm64",
            Architecture.X86 => "x86",
            Architecture.Arm => "arm",
            _ => RuntimeInformation.OSArchitecture.ToString().ToLowerInvariant(),
        };

        // CI が配置する {os}-{arch} を最優先で探索。
        yield return $"{os}-{arch}";

        // 実行環境が返す RID も候補に含める (versioned RID 等への保険)。
        string rid = RuntimeInformation.RuntimeIdentifier;
        if (!string.IsNullOrEmpty(rid) && rid != $"{os}-{arch}")
            yield return rid;
    }
}
