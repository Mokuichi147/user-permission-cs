using System.Collections.Generic;
using System.Threading.Tasks;

namespace UserPermission;

/// <summary>
/// ユーザーレコードの管理。各メソッドの <c>token</c> はリレーバックエンドで使う
/// <c>Authorization: Bearer</c> の上書き用 (省略時は <see cref="Database.LoginAsync"/> で保持したトークン)。
/// ローカルバックエンドでは <c>token</c> を渡すと操作前に JWT が検証される。
/// </summary>
public sealed class UserManager
{
    private readonly Database _db;

    internal UserManager(Database db) => _db = db;

    /// <summary>ユーザーを作成する。</summary>
    public Task<User> CreateAsync(string username, string password, string displayName = "", string? token = null)
    {
        var h = _db.Handle;
        return Task.Run(() => NativeMethods.Decode<User>(
            NativeMethods.up_users_create(h, username, password, displayName, token)));
    }

    /// <summary>ID でユーザーを取得する (存在しなければ <c>null</c>)。</summary>
    public Task<User?> GetByIdAsync(long userId, string? token = null)
    {
        var h = _db.Handle;
        return Task.Run(() => NativeMethods.DecodeNullable<User>(
            NativeMethods.up_users_get_by_id(h, userId, token)));
    }

    /// <summary>ユーザー名でユーザーを取得する (存在しなければ <c>null</c>)。</summary>
    public Task<User?> GetByUsernameAsync(string username, string? token = null)
    {
        var h = _db.Handle;
        return Task.Run(() => NativeMethods.DecodeNullable<User>(
            NativeMethods.up_users_get_by_username(h, username, token)));
    }

    /// <summary>全ユーザーを取得する。</summary>
    public Task<IReadOnlyList<User>> ListAllAsync(string? token = null)
    {
        var h = _db.Handle;
        return Task.Run(() => (IReadOnlyList<User>)NativeMethods.Decode<List<User>>(
            NativeMethods.up_users_list_all(h, token)));
    }

    /// <summary>ユーザーを更新する。<c>null</c> の引数は「変更なし」。対象が存在しなければ <c>null</c>。</summary>
    public Task<User?> UpdateAsync(
        long userId,
        string? username = null,
        string? password = null,
        string? displayName = null,
        bool? isActive = null,
        string? token = null)
    {
        var h = _db.Handle;
        int triActive = NativeMethods.Tri(isActive);
        return Task.Run(() => NativeMethods.DecodeNullable<User>(
            NativeMethods.up_users_update(h, userId, username, password, displayName, triActive, token)));
    }

    /// <summary>ユーザーを削除する。</summary>
    public Task<bool> DeleteAsync(long userId, string? token = null)
    {
        var h = _db.Handle;
        return Task.Run(() => NativeMethods.DecodeBool(
            NativeMethods.up_users_delete(h, userId, token)));
    }

    /// <summary>ユーザーが管理者か判定する。</summary>
    public Task<bool> IsAdminAsync(long userId, string? token = null)
    {
        var h = _db.Handle;
        return Task.Run(() => NativeMethods.DecodeBool(
            NativeMethods.up_users_is_admin(h, userId, token)));
    }

    /// <summary>ユーザーの管理者フラグを設定する。</summary>
    public Task<bool> SetAdminAsync(long userId, bool isAdmin, string? token = null)
    {
        var h = _db.Handle;
        byte flag = (byte)(isAdmin ? 1 : 0);
        return Task.Run(() => NativeMethods.DecodeBool(
            NativeMethods.up_users_set_admin(h, userId, flag, token)));
    }
}
