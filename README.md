# user-permission-cs

![NuGet License](https://img.shields.io/nuget/l/UserPermission?cacheSeconds=0)
![NuGet Version](https://img.shields.io/nuget/v/UserPermission?cacheSeconds=0)
![NuGet Downloads](https://img.shields.io/nuget/dt/UserPermission?cacheSeconds=0)

ユーザーとグループを管理するための非同期ライブラリ `user-permission` の **.NET (C#) バインディング** です。

Rust 実装本体は別リポジトリにあります: **[mokuichi147/user-permission](https://github.com/mokuichi147/user-permission)**

このリポジトリは Rust の C ABI (cdylib) を **P/Invoke** で呼び出す薄いラッパーのみを含み、`async/await` でそのまま使える API を提供します。2 つの NuGet パッケージを公開します。

- **`UserPermission`** — ライブラリ本体
- **`UserPermission.Tool`** — `serve` コマンドを持つ .NET ツール (dnx / `dotnet tool`)

## クイックスタート (サーバーを試す)

.NET 10 SDK があれば、インストール不要で `dnx`（ワンショット実行）から同梱サーバーを直接起動できます。

```bash
dnx UserPermission.Tool serve --webui
```

従来の SDK では、グローバルツールとしてインストールして使えます。

```bash
dotnet tool install -g UserPermission.Tool
user-permission serve --webui
```

ブラウザで <http://127.0.0.1:8000/ui> を開くと Web 管理画面が利用できます。
オプション一覧は [サーバー起動](#サーバー起動) を参照してください。

## インストール

```bash
dotnet add package UserPermission
```

ネイティブライブラリ (`user-permission-core` の Rust コア) は各プラットフォーム向けに同梱されています。対応 RID:

| OS | アーキテクチャ | RID |
|---|---|---|
| Linux | x64 / arm64 | `linux-x64` / `linux-arm64` |
| macOS | Intel / Apple Silicon | `osx-x64` / `osx-arm64` |
| Windows | x64 | `win-x64` |

### 対応ターゲットフレームワーク

`netstandard2.0` と `net8.0` のマルチターゲットです。

- **.NET 8+ / .NET Core 3.1+ / .NET 5・6・7**: ネイティブライブラリは `runtimes/{rid}/native/` から自動解決されます（追加設定不要）。
- **.NET Framework 4.6.1+ など (netstandard2.0 経由)**: 参照・コンパイルは可能ですが、`runtimes/` による RID 別ネイティブの自動配置をランタイムがサポートしないため、対象プラットフォームのネイティブライブラリ（例: `user_permission_csharp.dll`）を実行ファイルと同じディレクトリに手動配置するか PATH を通す必要があります。

## 使い方

### 初期化

```csharp
using UserPermission;

// 初回実行時にシークレットキーを自動生成（以降はファイルから読み込み）
await using var db = await Database.ConnectAsync("app.db", secret: "secret.key");

User user = await db.Users.CreateAsync("alice", "password123");
Group group = await db.Groups.CreateAsync("admins");
```

`Database.ConnectAsync(...)` は生成と接続をまとめて行います。生成と接続を分けたい場合は次のようにします。

```csharp
var db = new Database("app.db", secret: "secret.key");
await db.ConnectAsync();
// ...
await db.DisposeAsync();   // または using/await using で自動解放
```

> 最初に作成したユーザーは自動的に管理者グループ (`admin`) に所属し、管理者になります。

### ユーザー管理

```csharp
User user = await db.Users.CreateAsync("alice", "password123", displayName: "Alice");
User? byId = await db.Users.GetByIdAsync(1);
User? byName = await db.Users.GetByUsernameAsync("alice");
IReadOnlyList<User> users = await db.Users.ListAllAsync();

await db.Users.UpdateAsync(user.Id, password: "new_password");
await db.Users.UpdateAsync(user.Id, displayName: "Alice Smith");
await db.Users.UpdateAsync(user.Id, isActive: false);
await db.Users.DeleteAsync(user.Id);
```

### 認証・トークン

```csharp
string? token = await db.LoginAsync("alice", "password123");
string? token2 = await db.LoginAsync("alice", "password123", expires: TimeSpan.FromHours(24));

// トークンを検証してユーザーを解決（無効・期限切れは null）
User? resolved = await db.VerifyTokenAndGetUserAsync(token);
Console.WriteLine(resolved!.Id);                       // ユーザーID
Console.WriteLine(resolved.Username);                  // ユーザー名
Console.WriteLine(await db.Users.IsAdminAsync(resolved.Id));  // bool
```

### グループ管理

```csharp
Group group = await db.Groups.CreateAsync("editors", description: "Editor group");
Group? byId = await db.Groups.GetByIdAsync(1);
Group? byName = await db.Groups.GetByNameAsync("editors");
IReadOnlyList<Group> groups = await db.Groups.ListAllAsync();
IReadOnlyList<Group> adminGroups = await db.Groups.ListAdminGroupsAsync();

await db.Groups.UpdateAsync(group.Id, description: "Updated description");
await db.Groups.DeleteAsync(group.Id);
```

### グループメンバー管理

```csharp
await db.Groups.AddUserAsync(group.Id, user.Id);
await db.Groups.RemoveUserAsync(group.Id, user.Id);
IReadOnlyList<User> members = await db.Groups.GetMembersAsync(group.Id);
IReadOnlyList<Group> userGroups = await db.Groups.GetUserGroupsAsync(user.Id);
```

### サーバー起動

同梱の axum HTTP サーバーを起動できます。

```csharp
using UserPermission;

await Server.ServeAsync(host: "0.0.0.0", port: 8001, prefix: "/api", webui: true);
```

`ServeAsync` はサーバーが停止するまで完了しない `Task` を返します。

| 引数 | デフォルト | 説明 |
|---|---|---|
| `database` | `user_permission.db` | SQLiteデータベースのパス |
| `secret` | `secret.key` | シークレットキーファイルのパス |
| `host` | `127.0.0.1` | バインドアドレス |
| `port` | `8000` | バインドポート |
| `prefix` | (なし) | APIルートプレフィックス（例: `/api`） |
| `webui` | `false` | Web管理画面を有効化 |
| `webuiPrefix` | `/ui` | 管理画面のURLプレフィックス |

CLI からも起動できます（`UserPermission.Tool` パッケージ）。

```bash
# .NET 10: インストール不要のワンショット実行
dnx UserPermission.Tool serve --host 0.0.0.0 --port 8001 --prefix /api --webui

# もしくはグローバルツールとして
dotnet tool install -g UserPermission.Tool
user-permission serve --host 0.0.0.0 --port 8001 --prefix /api --webui
```

| オプション | デフォルト | 説明 |
|---|---|---|
| `--host` | `127.0.0.1` | バインドアドレス |
| `--port` | `8000` | バインドポート |
| `--database` | `user_permission.db` | SQLiteデータベースのパス |
| `--secret` | `secret.key` | シークレットキーファイルのパス |
| `--prefix` | (なし) | APIルートプレフィックス（例: `/api`） |
| `--webui` | 無効 | Web管理画面を有効化 |
| `--webui-prefix` | `/ui` | 管理画面のURLプレフィックス |

> ユーザー管理などフル機能の CLI サーバーは Rust リポジトリ側にあります (`cargo install user-permission`)。この .NET ツールは `serve` のみを提供します。

### リレー（中継）

`Database` に URL を渡すと、ローカル SQLite と中央サーバーを同じインターフェースで切り替えられます。

```csharp
using UserPermission;

// ファイルパス → ローカル SQLite
var local = new Database("app.db", secret: "secret.key");

// URL → リモートサーバーへ HTTP 中継
await using var db = await Database.ConnectAsync("http://localhost:8001");
string? token = await db.LoginAsync("alice", "password123");
IReadOnlyList<User> users = await db.Users.ListAllAsync();
```

`db.LoginAsync(...)` で取得したトークンは `Database` が内部に保持し、以降のリクエストの `Authorization: Bearer` に自動付与されます。

**推奨: backend を意識しない実装。** 認証 (`LoginAsync` / `LoginServiceAsync`)、トークン検証 (`VerifyTokenAndGetUserAsync`)、ユーザー・グループ操作はすべてローカル / リレーで同一の呼び出しで動作します。接続先 URL（またはファイルパス）を切り替えるだけで、アプリ側のコードを変えずにローカル運用と中央サーバー運用を行き来できます。

```csharp
// どちらの backend でも同じコードが動く
async Task<User?> AuthenticateAsync(Database db, string username, string password)
{
    // login 失敗時は null、VerifyTokenAndGetUserAsync は null を渡すと null を返す
    string? token = await db.LoginAsync(username, password);
    return await db.VerifyTokenAndGetUserAsync(token);
}
```

> サービスクライアントの**管理操作**だけは例外でローカル専用です（後述）。

### サービスクライアント認証（client-credentials）

サービス間連携向けに、平文パスワードを持たずに**読み取り専用**のスコープ付きトークンを発行できます。
クライアントには `users:read` / `groups:read` のスコープのみ付与でき、書き込みや管理操作はできません。

```csharp
using UserPermission;

// 管理側（ローカル）でサービスクライアントを発行。secret は発行時のみ取得可能。
await using (var admin = await Database.ConnectAsync("app.db", secret: "secret.key"))
{
    var (client, secret) = await admin.ServiceClients.CreateAsync("reader", new[] { Scopes.UsersRead });
    Console.WriteLine($"{client.ClientId} / {secret}");
}

// リレー側はサービストークンでログインし、スコープ内のみ読み取れる。
await using (var relay = await Database.ConnectAsync("http://localhost:8001"))
{
    await relay.LoginServiceAsync(clientId, secret);
    IReadOnlyList<User> users = await relay.Users.ListAllAsync();  // users:read があるので OK
}
```

> **注意: 管理操作はローカル backend 専用です。** `ServiceClients.CreateAsync` / `ListAsync` / `GetByClientIdAsync` / `DeleteAsync` / `RotateSecretAsync` はリレー（URL）backend で呼ぶと例外になります（サービスクライアントの発行・管理は中央サーバー側で行う設計のため）。一方、サービス認証 (`db.LoginServiceAsync(...)`) はローカル / リレーのどちらでも動作します。

スコープの検証は `Scopes.Validate(...)` で行えます（未知のスコープがあれば `UserPermissionException` を送出）。

```csharp
Scopes.Validate(new[] { Scopes.UsersRead, Scopes.GroupsRead });
```

### バックエンド非依存のユーティリティ

ローカル / リレーのどちらでも同じ呼び出しで動作します。

```csharp
// トークンを検証してユーザーを解決（無効・期限切れ・サービストークンは null）
User? user = await db.VerifyTokenAndGetUserAsync(token);

// 管理者が不在なら作成して昇格（リレーでは no-op で null）
await db.BootstrapAdminIfNeededAsync("admin", "password123");
```

### エラーハンドリング

操作が失敗すると `UserPermissionException` が送出されます。`Kind` プロパティで種別を判別できます。

```csharp
try
{
    await db.Users.CreateAsync("alice", "password123");
}
catch (UserPermissionException ex) when (ex.Kind == UserPermissionErrorKind.Conflict)
{
    // ユーザー名が既に存在する
}
```

## REST API・管理者ロール・スキーマ

REST エンドポイント仕様、`groups.is_admin` による管理者ロールの扱い、データベーススキーマは
[Rust リポジトリの README](https://github.com/mokuichi147/user-permission#readme) を参照してください。

## アーキテクチャ

```
┌─────────────────────────┐
│ C# (UserPermission.dll) │  Database / UserManager / GroupManager / ...
│  P/Invoke ([DllImport])  │  ← async Task<T>（内部で Task.Run）
└────────────┬────────────┘
             │ C ABI（UTF-8文字列 + JSON エンベロープ）
┌────────────▼────────────────────────┐
│ Rust cdylib                          │  user_permission_csharp
│  (crates/user-permission-csharp)     │  ← tokio ランタイムで block_on
└────────────┬─────────────────────────┘
             │
┌────────────▼─────────────────────────┐
│ user-permission-core / user-permission│  crates.io（SQLite/Argon2/JWT/axum）
└──────────────────────────────────────┘
```

- Rust 側は各関数が JSON エンベロープ (`{"ok": …}` / `{"err": {"kind","message"}}`) を返し、C# 側でデシリアライズ・例外変換します。
- 非同期処理は Rust 側の共有 tokio ランタイムで `block_on` し、C# 側は `Task.Run` でラップして `async` API にしています。

## 開発

前提: [.NET SDK 8.0+](https://dotnet.microsoft.com/) と [Rust toolchain](https://rustup.rs/)。

```bash
# 1. ネイティブライブラリ (cdylib) をビルド
cargo build --release            # もしくは開発中は cargo build（debug）

# 2. C# ライブラリのビルドとテスト
#    Directory.Build.targets が target/{profile}/ のネイティブ lib を
#    各プロジェクトの出力へ自動コピーするため、別途配置は不要。
dotnet test
```

## リリース

`v` で始まる Git タグを push すると GitHub Actions が全プラットフォームのネイティブライブラリをビルドし、
NuGet パッケージ (`UserPermission` と `UserPermission.Tool`) を生成して nuget.org に公開します（詳細は `.github/workflows/release.yml`）。

## ライセンス

MIT OR Apache-2.0
