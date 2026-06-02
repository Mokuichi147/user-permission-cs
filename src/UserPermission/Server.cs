using System.Threading.Tasks;

namespace UserPermission;

/// <summary>同梱の axum HTTP サーバー。</summary>
public static class Server
{
    /// <summary>
    /// HTTP サーバーを起動する。サーバーが停止する (またはエラーになる) までブロックする
    /// <see cref="Task"/> を返す。
    /// </summary>
    /// <param name="database">SQLite データベースのパス。</param>
    /// <param name="secret">JWT 署名鍵ファイルのパス (なければ自動生成)。</param>
    /// <param name="host">バインドアドレス。</param>
    /// <param name="port">バインドポート。</param>
    /// <param name="prefix">API ルートプレフィックス (例: <c>/api</c>)。</param>
    /// <param name="webui">Web 管理画面を有効化するか。</param>
    /// <param name="webuiPrefix">管理画面の URL プレフィックス。</param>
    public static Task ServeAsync(
        string database = "user_permission.db",
        string secret = "secret.key",
        string host = "127.0.0.1",
        int port = 8000,
        string prefix = "",
        bool webui = false,
        string webuiPrefix = "/ui")
    {
        byte webuiFlag = (byte)(webui ? 1 : 0);
        return Task.Run(() => NativeMethods.DecodeVoid(
            NativeMethods.up_serve(database, secret, host, (ushort)port, prefix, webuiFlag, webuiPrefix)));
    }
}
