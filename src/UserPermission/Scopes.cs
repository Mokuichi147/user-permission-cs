using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace UserPermission;

/// <summary>
/// サービスクライアントに付与できるスコープ。書き込み・管理スコープは存在せず、
/// サービストークンは状態を変更できない。
/// </summary>
public static class Scopes
{
    /// <summary>ユーザーディレクトリの読み取り。</summary>
    public const string UsersRead = "users:read";

    /// <summary>グループとメンバーシップの読み取り。</summary>
    public const string GroupsRead = "groups:read";

    /// <summary>付与可能な全スコープ。</summary>
    public static readonly IReadOnlyList<string> All = new[] { UsersRead, GroupsRead };

    /// <summary>
    /// スコープ集合を検証する。未知のスコープが含まれる場合は
    /// <see cref="UserPermissionException"/> (<see cref="UserPermissionErrorKind.InvalidArgument"/>) を送出する。
    /// </summary>
    public static void Validate(IEnumerable<string> scopes)
    {
        string json = JsonSerializer.Serialize(scopes.ToArray());
        NativeMethods.DecodeVoid(NativeMethods.up_validate_scopes(json));
    }
}
