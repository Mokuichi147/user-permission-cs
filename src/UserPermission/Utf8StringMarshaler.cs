using System;
using System.Runtime.InteropServices;
using System.Text;

namespace UserPermission;

/// <summary>
/// マネージド <see cref="string"/> を null 終端 UTF-8 としてネイティブへ渡すカスタムマーシャラ。
/// </summary>
/// <remarks>
/// <c>UnmanagedType.LPUTF8Str</c> は netstandard2.0 に存在しないため、両ターゲットで動作する
/// <see cref="ICustomMarshaler"/> で UTF-8 変換を行う。入力 (managed → native) 専用。
/// </remarks>
internal sealed class Utf8StringMarshaler : ICustomMarshaler
{
    private static readonly Utf8StringMarshaler Instance = new();

    public static ICustomMarshaler GetInstance(string cookie) => Instance;

    public IntPtr MarshalManagedToNative(object ManagedObj)
    {
        if (ManagedObj is not string s)
            return IntPtr.Zero; // null → NULL ポインタ (ネイティブ側で None 扱い)

        int count = Encoding.UTF8.GetByteCount(s);
        var bytes = new byte[count + 1];
        Encoding.UTF8.GetBytes(s, 0, s.Length, bytes, 0);
        bytes[count] = 0; // null 終端

        IntPtr ptr = Marshal.AllocHGlobal(bytes.Length);
        Marshal.Copy(bytes, 0, ptr, bytes.Length);
        return ptr;
    }

    public void CleanUpNativeData(IntPtr pNativeData)
    {
        if (pNativeData != IntPtr.Zero)
            Marshal.FreeHGlobal(pNativeData);
    }

    // 入力専用のため呼ばれない。
    public object MarshalNativeToManaged(IntPtr pNativeData) => null!;

    public void CleanUpManagedData(object ManagedObj)
    {
    }

    public int GetNativeDataSize() => -1;
}
