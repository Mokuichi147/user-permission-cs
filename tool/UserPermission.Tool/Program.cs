using System;
using System.Threading.Tasks;
using UserPermission;

namespace UserPermission.Tool;

/// <summary>
/// `user-permission` CLI のエントリポイント。Python 版の <c>user-permission serve</c> と
/// 同じインターフェースで、同梱 HTTP サーバーを起動する。
/// </summary>
internal static class Program
{
    private static async Task<int> Main(string[] args)
    {
        if (args.Length == 0)
        {
            PrintHelp();
            return 1;
        }

        switch (args[0])
        {
            case "serve":
                return await ServeAsync(args[1..]).ConfigureAwait(false);
            case "version":
            case "--version":
            case "-v":
                Console.WriteLine(Library.Version);
                return 0;
            case "help":
            case "--help":
            case "-h":
                PrintHelp();
                return 0;
            default:
                Console.Error.WriteLine($"不明なコマンド: {args[0]}");
                PrintHelp();
                return 1;
        }
    }

    private static async Task<int> ServeAsync(string[] args)
    {
        string host = "127.0.0.1";
        int port = 8000;
        string database = "user_permission.db";
        string secret = "secret.key";
        string prefix = "";
        bool webui = false;
        string webuiPrefix = "/ui";

        for (int i = 0; i < args.Length; i++)
        {
            string arg = args[i];

            string RequireValue()
            {
                if (i + 1 >= args.Length)
                    throw new ArgumentException($"{arg} には値が必要です");
                return args[++i];
            }

            switch (arg)
            {
                case "--host":
                    host = RequireValue();
                    break;
                case "--port":
                    port = int.Parse(RequireValue());
                    break;
                case "--database":
                    database = RequireValue();
                    break;
                case "--secret":
                    secret = RequireValue();
                    break;
                case "--prefix":
                    prefix = RequireValue();
                    break;
                case "--webui":
                    webui = true;
                    break;
                case "--webui-prefix":
                    webuiPrefix = RequireValue();
                    break;
                case "-h":
                case "--help":
                    PrintServeHelp();
                    return 0;
                default:
                    Console.Error.WriteLine($"不明なオプション: {arg}");
                    PrintServeHelp();
                    return 1;
            }
        }

        string uiNote = webui ? $"  (Web UI: http://{host}:{port}{webuiPrefix})" : string.Empty;
        Console.WriteLine($"UserPermission serve → http://{host}:{port}{prefix}{uiNote}");
        Console.WriteLine("停止するには Ctrl+C を押してください。");

        await Server.ServeAsync(
            database: database,
            secret: secret,
            host: host,
            port: port,
            prefix: prefix,
            webui: webui,
            webuiPrefix: webuiPrefix).ConfigureAwait(false);
        return 0;
    }

    private static void PrintHelp()
    {
        Console.WriteLine(
            """
            user-permission – 集中型のユーザー・グループ管理

            使い方:
              user-permission serve [オプション]   HTTP サーバーを起動
              user-permission version              バージョンを表示
              user-permission help                 このヘルプを表示

            'user-permission serve --help' で serve のオプションを確認できます。
            """);
    }

    private static void PrintServeHelp()
    {
        Console.WriteLine(
            """
            使い方: user-permission serve [オプション]

            オプション:
              --host <ADDR>          バインドアドレス (既定: 127.0.0.1)
              --port <PORT>          バインドポート (既定: 8000)
              --database <PATH>      SQLite データベースのパス (既定: user_permission.db)
              --secret <PATH>        シークレットキーファイルのパス (既定: secret.key)
              --prefix <PREFIX>      API ルートプレフィックス (例: /api)
              --webui                Web 管理画面を有効化
              --webui-prefix <PREFIX> 管理画面の URL プレフィックス (既定: /ui)
              -h, --help             このヘルプを表示
            """);
    }
}
