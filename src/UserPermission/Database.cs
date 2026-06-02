using System;
using System.Threading.Tasks;

namespace UserPermission;

/// <summary>
/// ローカル SQLite または HTTP リレーをバックエンドに持つ、非同期のユーザー / グループ管理データベース。
/// </summary>
/// <remarks>
/// <para>
/// 接続先 (<c>backend</c>) にファイルパスを渡すとローカル SQLite、<c>http(s)://</c> URL を渡すと
/// 中央サーバーへの HTTP リレーになります。認証・トークン検証・ユーザー / グループ操作は
/// どちらのバックエンドでも同一の呼び出しで動作します。
/// </para>
/// <para>
/// 使い終わったら <see cref="DisposeAsync"/> (推奨) または <see cref="Dispose"/> で解放してください。
/// </para>
/// </remarks>
public sealed class Database : IDisposable, IAsyncDisposable
{
    private IntPtr _handle;
    private bool _disposed;

    /// <summary>ユーザー管理。</summary>
    public UserManager Users { get; }

    /// <summary>グループ管理。</summary>
    public GroupManager Groups { get; }

    /// <summary>サービスクライアント管理 (管理操作はローカルバックエンド専用)。</summary>
    public ServiceClientManager ServiceClients { get; }

    /// <summary>
    /// 未接続のデータベースを生成する。実際の接続は <see cref="ConnectAsync()"/> を呼ぶこと。
    /// </summary>
    /// <param name="backend">ファイルパス、または <c>http(s)://</c> URL。</param>
    /// <param name="secret">ローカルバックエンドの JWT 署名鍵ファイルのパス。未指定だとトークン発行不可。リレーには指定不可。</param>
    public Database(string backend, string? secret = null)
    {
        _handle = NativeMethods.up_database_new(backend, secret);
        if (_handle == IntPtr.Zero)
            throw new UserPermissionException(UserPermissionErrorKind.Other, "failed to allocate database handle");

        Users = new UserManager(this);
        Groups = new GroupManager(this);
        ServiceClients = new ServiceClientManager(this);
    }

    /// <summary>新しいデータベースを生成し、接続まで済ませて返す。失敗時はハンドルを解放する。</summary>
    public static async Task<Database> ConnectAsync(string backend, string? secret = null)
    {
        var db = new Database(backend, secret);
        try
        {
            await db.ConnectAsync().ConfigureAwait(false);
        }
        catch
        {
            db.Dispose();
            throw;
        }
        return db;
    }

    internal IntPtr Handle
    {
        get
        {
            ThrowIfDisposed();
            return _handle;
        }
    }

    /// <summary>バックエンドへ接続する。</summary>
    public Task ConnectAsync()
    {
        IntPtr handle = Handle;
        return Task.Run(() => NativeMethods.DecodeVoid(NativeMethods.up_database_connect(handle)));
    }

    /// <summary>接続を閉じる (未接続でも no-op)。</summary>
    public Task CloseAsync()
    {
        IntPtr handle = Handle;
        return Task.Run(() => NativeMethods.DecodeVoid(NativeMethods.up_database_close(handle)));
    }

    /// <summary>ユーザー名 + パスワードでログインし、アクセストークンを返す (認証失敗時は <c>null</c>)。</summary>
    /// <param name="username">ユーザー名。</param>
    /// <param name="password">パスワード。</param>
    /// <param name="expires">トークン有効期間。未指定は 1 時間。リレーではサーバーが寿命を決めるため無視される。</param>
    public Task<string?> LoginAsync(string username, string password, TimeSpan? expires = null)
    {
        IntPtr handle = Handle;
        ulong secs = ToSeconds(expires);
        return Task.Run(() => NativeMethods.DecodeNullableString(
            NativeMethods.up_database_login(handle, username, password, secs)));
    }

    /// <summary>サービスクライアント (client-credentials) でログインし、スコープ付きトークンを返す。</summary>
    public Task<string?> LoginServiceAsync(string clientId, string clientSecret, TimeSpan? expires = null)
    {
        IntPtr handle = Handle;
        ulong secs = ToSeconds(expires);
        return Task.Run(() => NativeMethods.DecodeNullableString(
            NativeMethods.up_database_login_service(handle, clientId, clientSecret, secs)));
    }

    /// <summary>トークンを検証してユーザーを解決する。無効・期限切れ・サービストークン・<c>null</c> はいずれも <c>null</c> を返す。</summary>
    public Task<User?> VerifyTokenAndGetUserAsync(string? token)
    {
        IntPtr handle = Handle;
        return Task.Run(() => NativeMethods.DecodeNullable<User>(
            NativeMethods.up_database_verify_token_and_get_user(handle, token)));
    }

    /// <summary>管理者が不在なら作成して昇格し、そのユーザーを返す。既に管理者がいる場合やリレーでは <c>null</c>。</summary>
    public Task<User?> BootstrapAdminIfNeededAsync(string username, string password, string displayName = "")
    {
        IntPtr handle = Handle;
        return Task.Run(() => NativeMethods.DecodeNullable<User>(
            NativeMethods.up_database_bootstrap_admin_if_needed(handle, username, password, displayName)));
    }

    private static ulong ToSeconds(TimeSpan? expires)
    {
        if (expires is null)
            return 3600UL;
        double secs = expires.Value.TotalSeconds;
        return secs <= 0 ? 0UL : (ulong)secs;
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(Database));
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;

        IntPtr handle = _handle;
        _handle = IntPtr.Zero;
        if (handle != IntPtr.Zero)
        {
            try
            {
                NativeMethods.DecodeVoid(NativeMethods.up_database_close(handle));
            }
            catch
            {
                // Dispose では例外を握りつぶす。
            }
            NativeMethods.up_database_free(handle);
        }
        GC.SuppressFinalize(this);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;
        _disposed = true;

        IntPtr handle = _handle;
        _handle = IntPtr.Zero;
        if (handle != IntPtr.Zero)
        {
            try
            {
                await Task.Run(() => NativeMethods.DecodeVoid(NativeMethods.up_database_close(handle)))
                    .ConfigureAwait(false);
            }
            catch
            {
                // 同上。
            }
            NativeMethods.up_database_free(handle);
        }
        GC.SuppressFinalize(this);
    }

    /// <summary>解放漏れに備えてネイティブハンドルを破棄するファイナライザ。</summary>
    ~Database()
    {
        if (_handle != IntPtr.Zero)
            NativeMethods.up_database_free(_handle);
    }
}
