using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace UserPermission;

/// <summary>
/// グループレコードとメンバーシップの管理。<c>token</c> の扱いは <see cref="UserManager"/> と同じ。
/// </summary>
public sealed class GroupManager
{
    private readonly Database _db;

    internal GroupManager(Database db) => _db = db;

    /// <summary>グループを作成する。</summary>
    public Task<Group> CreateAsync(string name, string description = "", bool isAdmin = false, string? token = null)
    {
        var h = _db.Handle;
        byte flag = (byte)(isAdmin ? 1 : 0);
        return Task.Run(() => NativeMethods.Decode<Group>(
            NativeMethods.up_groups_create(h, name, description, flag, token)));
    }

    /// <summary>ID でグループを取得する (存在しなければ <c>null</c>)。</summary>
    public Task<Group?> GetByIdAsync(long groupId, string? token = null)
    {
        var h = _db.Handle;
        return Task.Run(() => NativeMethods.DecodeNullable<Group>(
            NativeMethods.up_groups_get_by_id(h, groupId, token)));
    }

    /// <summary>名前でグループを取得する (存在しなければ <c>null</c>)。</summary>
    public Task<Group?> GetByNameAsync(string name, string? token = null)
    {
        var h = _db.Handle;
        return Task.Run(() => NativeMethods.DecodeNullable<Group>(
            NativeMethods.up_groups_get_by_name(h, name, token)));
    }

    /// <summary>全グループを取得する。</summary>
    public Task<IReadOnlyList<Group>> ListAllAsync(string? token = null)
    {
        var h = _db.Handle;
        return Task.Run(() => (IReadOnlyList<Group>)NativeMethods.Decode<List<Group>>(
            NativeMethods.up_groups_list_all(h, token)));
    }

    /// <summary>管理者グループのみ取得する。</summary>
    public Task<IReadOnlyList<Group>> ListAdminGroupsAsync(string? token = null)
    {
        var h = _db.Handle;
        return Task.Run(() => (IReadOnlyList<Group>)NativeMethods.Decode<List<Group>>(
            NativeMethods.up_groups_list_admin_groups(h, token)));
    }

    /// <summary>グループを更新する。<c>null</c> の引数は「変更なし」。対象が存在しなければ <c>null</c>。</summary>
    public Task<Group?> UpdateAsync(
        long groupId,
        string? name = null,
        string? description = null,
        bool? isAdmin = null,
        string? token = null)
    {
        var h = _db.Handle;
        int triAdmin = NativeMethods.Tri(isAdmin);
        return Task.Run(() => NativeMethods.DecodeNullable<Group>(
            NativeMethods.up_groups_update(h, groupId, name, description, triAdmin, token)));
    }

    /// <summary>グループを削除する。</summary>
    public Task<bool> DeleteAsync(long groupId, string? token = null)
    {
        var h = _db.Handle;
        return Task.Run(() => NativeMethods.DecodeBool(
            NativeMethods.up_groups_delete(h, groupId, token)));
    }

    /// <summary>グループにユーザーを追加する。</summary>
    public Task<bool> AddUserAsync(long groupId, Guid userId, string? token = null)
    {
        var h = _db.Handle;
        return Task.Run(() => NativeMethods.DecodeBool(
            NativeMethods.up_groups_add_user(h, groupId, userId.ToString(), token)));
    }

    /// <summary>グループからユーザーを除外する。</summary>
    public Task<bool> RemoveUserAsync(long groupId, Guid userId, string? token = null)
    {
        var h = _db.Handle;
        return Task.Run(() => NativeMethods.DecodeBool(
            NativeMethods.up_groups_remove_user(h, groupId, userId.ToString(), token)));
    }

    /// <summary>グループのメンバー一覧を取得する。</summary>
    public Task<IReadOnlyList<User>> GetMembersAsync(long groupId, string? token = null)
    {
        var h = _db.Handle;
        return Task.Run(() => (IReadOnlyList<User>)NativeMethods.Decode<List<User>>(
            NativeMethods.up_groups_get_members(h, groupId, token)));
    }

    /// <summary>ユーザーが所属するグループ一覧を取得する。</summary>
    public Task<IReadOnlyList<Group>> GetUserGroupsAsync(Guid userId, string? token = null)
    {
        var h = _db.Handle;
        return Task.Run(() => (IReadOnlyList<Group>)NativeMethods.Decode<List<Group>>(
            NativeMethods.up_groups_get_user_groups(h, userId.ToString(), token)));
    }
}
