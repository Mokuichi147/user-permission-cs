using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace UserPermission;

/// <summary>
/// machine-to-machine サービスクライアントの管理。
/// </summary>
/// <remarks>
/// 管理操作 (<see cref="CreateAsync"/> / <see cref="ListAsync"/> / <see cref="GetByClientIdAsync"/> /
/// <see cref="DeleteAsync"/> / <see cref="RotateSecretAsync"/>) はローカルバックエンド専用です。
/// リレー (URL) バックエンドで呼ぶと例外になります。サービス認証
/// (<see cref="Database.LoginServiceAsync"/>) は両バックエンドで動作します。
/// </remarks>
public sealed class ServiceClientManager
{
    private readonly Database _db;

    internal ServiceClientManager(Database db) => _db = db;

    /// <summary>
    /// サービスクライアントを作成し、<c>(クライアント, secret)</c> を返す。
    /// 平文 secret はこの戻り値でのみ取得できる。
    /// </summary>
    /// <param name="name">クライアント名。</param>
    /// <param name="scopes"><see cref="Scopes.UsersRead"/> / <see cref="Scopes.GroupsRead"/> のいずれか。</param>
    /// <param name="expiresAt">失効日時 (RFC3339 文字列)。<c>null</c> で無期限。</param>
    public Task<ServiceClientCreateResult> CreateAsync(
        string name, IEnumerable<string> scopes, string? expiresAt = null)
    {
        var h = _db.Handle;
        string scopesJson = JsonSerializer.Serialize(scopes.ToArray());
        return Task.Run(() => NativeMethods.Decode<ServiceClientCreateResult>(
            NativeMethods.up_service_clients_create(h, name, scopesJson, expiresAt)));
    }

    /// <summary>全サービスクライアントを取得する。</summary>
    public Task<IReadOnlyList<ServiceClient>> ListAsync()
    {
        var h = _db.Handle;
        return Task.Run(() => (IReadOnlyList<ServiceClient>)NativeMethods.Decode<List<ServiceClient>>(
            NativeMethods.up_service_clients_list(h)));
    }

    /// <summary>client_id でサービスクライアントを取得する (存在しなければ <c>null</c>)。</summary>
    public Task<ServiceClient?> GetByClientIdAsync(string clientId)
    {
        var h = _db.Handle;
        return Task.Run(() => NativeMethods.DecodeNullable<ServiceClient>(
            NativeMethods.up_service_clients_get_by_client_id(h, clientId)));
    }

    /// <summary>サービスクライアントを削除する。</summary>
    public Task<bool> DeleteAsync(long id)
    {
        var h = _db.Handle;
        return Task.Run(() => NativeMethods.DecodeBool(
            NativeMethods.up_service_clients_delete(h, id)));
    }

    /// <summary>secret をローテートし、新しい平文 secret を返す (対象が無ければ <c>null</c>)。</summary>
    public Task<string?> RotateSecretAsync(long id)
    {
        var h = _db.Handle;
        return Task.Run(() => NativeMethods.DecodeNullableString(
            NativeMethods.up_service_clients_rotate_secret(h, id)));
    }
}
