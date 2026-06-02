using System;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace UserPermission;

/// <summary>
/// ネイティブライブラリ (<c>user_permission_csharp</c>) への P/Invoke 宣言と、
/// JSON エンベロープ (<c>{"ok": …}</c> / <c>{"err": …}</c>) のデコード補助。
/// </summary>
internal static class NativeMethods
{
    private const string Lib = "user_permission_csharp";

    internal static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    // 最初の P/Invoke (= NativeMethods のメンバーアクセス) より前に一度だけ実行され、
    // ネイティブライブラリ解決のフォールバックを登録する。
    static NativeMethods()
    {
        NativeLibraryResolver.Initialize();
    }

    // --- 文字列・エンベロープ補助 -------------------------------------------------

    /// <summary>ネイティブが返した UTF-8 文字列をマネージド文字列へ変換し、必ず解放する。</summary>
    internal static string PtrToStringAndFree(IntPtr ptr)
    {
        if (ptr == IntPtr.Zero)
            throw new UserPermissionException(UserPermissionErrorKind.Other, "native call returned a null pointer");
        try
        {
            return Marshal.PtrToStringUTF8(ptr) ?? string.Empty;
        }
        finally
        {
            up_string_free(ptr);
        }
    }

    /// <summary>エンベロープを解析し、<c>ok</c> ペイロードの生 JSON を返す (null の場合は <c>null</c>)。<c>err</c> なら例外を送出。</summary>
    private static string? DecodeRawOk(IntPtr ptr)
    {
        string json = PtrToStringAndFree(ptr);
        using var doc = JsonDocument.Parse(json);
        JsonElement root = doc.RootElement;
        if (root.TryGetProperty("err", out JsonElement err))
            throw UserPermissionException.FromJson(err);
        if (!root.TryGetProperty("ok", out JsonElement ok))
            throw new UserPermissionException(UserPermissionErrorKind.Other, $"malformed response envelope: {json}");
        return ok.ValueKind == JsonValueKind.Null ? null : ok.GetRawText();
    }

    /// <summary>非 null が保証されるペイロードを <typeparamref name="T"/> へデシリアライズする。</summary>
    internal static T Decode<T>(IntPtr ptr)
    {
        string? raw = DecodeRawOk(ptr);
        if (raw is null)
            throw new UserPermissionException(UserPermissionErrorKind.Other, "expected a value but received null");
        return JsonSerializer.Deserialize<T>(raw, JsonOptions)!;
    }

    /// <summary>null になりうる参照型ペイロードをデシリアライズする。</summary>
    internal static T? DecodeNullable<T>(IntPtr ptr) where T : class
    {
        string? raw = DecodeRawOk(ptr);
        return raw is null ? null : JsonSerializer.Deserialize<T>(raw, JsonOptions);
    }

    internal static bool DecodeBool(IntPtr ptr)
    {
        string? raw = DecodeRawOk(ptr);
        return raw is not null && JsonSerializer.Deserialize<bool>(raw, JsonOptions);
    }

    internal static string? DecodeNullableString(IntPtr ptr)
    {
        string? raw = DecodeRawOk(ptr);
        return raw is null ? null : JsonSerializer.Deserialize<string>(raw, JsonOptions);
    }

    /// <summary>ペイロードを無視し、エラーのみ検査する (失敗時に例外送出)。</summary>
    internal static void DecodeVoid(IntPtr ptr) => _ = DecodeRawOk(ptr);

    /// <summary><see cref="bool"/>? を 3 値表現に変換する (null=-1 / false=0 / true=1)。</summary>
    internal static int Tri(bool? value) => value switch
    {
        true => 1,
        false => 0,
        null => -1,
    };

    // --- 共通 --------------------------------------------------------------------

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void up_string_free(IntPtr s);

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    internal static extern IntPtr up_version();

    // --- Database ----------------------------------------------------------------

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    internal static extern IntPtr up_database_new(
        [MarshalAs(UnmanagedType.LPUTF8Str)] string target,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string? secret);

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void up_database_free(IntPtr handle);

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    internal static extern IntPtr up_database_connect(IntPtr handle);

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    internal static extern IntPtr up_database_close(IntPtr handle);

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    internal static extern IntPtr up_database_login(
        IntPtr handle,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string username,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string password,
        ulong expiresSecs);

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    internal static extern IntPtr up_database_login_service(
        IntPtr handle,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string clientId,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string clientSecret,
        ulong expiresSecs);

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    internal static extern IntPtr up_database_verify_token_and_get_user(
        IntPtr handle,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string? token);

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    internal static extern IntPtr up_database_bootstrap_admin_if_needed(
        IntPtr handle,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string username,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string password,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string displayName);

    // --- Users -------------------------------------------------------------------

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    internal static extern IntPtr up_users_create(
        IntPtr handle,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string username,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string password,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string displayName,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string? token);

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    internal static extern IntPtr up_users_get_by_id(
        IntPtr handle, long userId, [MarshalAs(UnmanagedType.LPUTF8Str)] string? token);

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    internal static extern IntPtr up_users_get_by_username(
        IntPtr handle, [MarshalAs(UnmanagedType.LPUTF8Str)] string username,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string? token);

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    internal static extern IntPtr up_users_list_all(
        IntPtr handle, [MarshalAs(UnmanagedType.LPUTF8Str)] string? token);

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    internal static extern IntPtr up_users_update(
        IntPtr handle, long userId,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string? username,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string? password,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string? displayName,
        int isActive,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string? token);

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    internal static extern IntPtr up_users_delete(
        IntPtr handle, long userId, [MarshalAs(UnmanagedType.LPUTF8Str)] string? token);

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    internal static extern IntPtr up_users_is_admin(
        IntPtr handle, long userId, [MarshalAs(UnmanagedType.LPUTF8Str)] string? token);

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    internal static extern IntPtr up_users_set_admin(
        IntPtr handle, long userId, byte isAdmin, [MarshalAs(UnmanagedType.LPUTF8Str)] string? token);

    // --- Groups ------------------------------------------------------------------

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    internal static extern IntPtr up_groups_create(
        IntPtr handle,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string name,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string description,
        byte isAdmin,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string? token);

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    internal static extern IntPtr up_groups_get_by_id(
        IntPtr handle, long groupId, [MarshalAs(UnmanagedType.LPUTF8Str)] string? token);

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    internal static extern IntPtr up_groups_get_by_name(
        IntPtr handle, [MarshalAs(UnmanagedType.LPUTF8Str)] string name,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string? token);

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    internal static extern IntPtr up_groups_list_all(
        IntPtr handle, [MarshalAs(UnmanagedType.LPUTF8Str)] string? token);

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    internal static extern IntPtr up_groups_list_admin_groups(
        IntPtr handle, [MarshalAs(UnmanagedType.LPUTF8Str)] string? token);

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    internal static extern IntPtr up_groups_update(
        IntPtr handle, long groupId,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string? name,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string? description,
        int isAdmin,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string? token);

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    internal static extern IntPtr up_groups_delete(
        IntPtr handle, long groupId, [MarshalAs(UnmanagedType.LPUTF8Str)] string? token);

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    internal static extern IntPtr up_groups_add_user(
        IntPtr handle, long groupId, long userId, [MarshalAs(UnmanagedType.LPUTF8Str)] string? token);

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    internal static extern IntPtr up_groups_remove_user(
        IntPtr handle, long groupId, long userId, [MarshalAs(UnmanagedType.LPUTF8Str)] string? token);

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    internal static extern IntPtr up_groups_get_members(
        IntPtr handle, long groupId, [MarshalAs(UnmanagedType.LPUTF8Str)] string? token);

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    internal static extern IntPtr up_groups_get_user_groups(
        IntPtr handle, long userId, [MarshalAs(UnmanagedType.LPUTF8Str)] string? token);

    // --- ServiceClients ----------------------------------------------------------

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    internal static extern IntPtr up_service_clients_create(
        IntPtr handle,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string name,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string scopesJson,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string? expiresAt);

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    internal static extern IntPtr up_service_clients_list(IntPtr handle);

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    internal static extern IntPtr up_service_clients_get_by_client_id(
        IntPtr handle, [MarshalAs(UnmanagedType.LPUTF8Str)] string clientId);

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    internal static extern IntPtr up_service_clients_delete(IntPtr handle, long id);

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    internal static extern IntPtr up_service_clients_rotate_secret(IntPtr handle, long id);

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    internal static extern IntPtr up_validate_scopes(
        [MarshalAs(UnmanagedType.LPUTF8Str)] string scopesJson);

    // --- Server ------------------------------------------------------------------

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    internal static extern IntPtr up_serve(
        [MarshalAs(UnmanagedType.LPUTF8Str)] string database,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string secret,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string host,
        ushort port,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string prefix,
        byte webui,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string webuiPrefix);
}
