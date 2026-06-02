using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace UserPermission;

/// <summary>ユーザーレコード。</summary>
public sealed record User(
    [property: JsonPropertyName("id")] long Id,
    [property: JsonPropertyName("username")] string Username,
    [property: JsonPropertyName("display_name")] string DisplayName,
    [property: JsonPropertyName("is_active")] bool IsActive,
    [property: JsonPropertyName("created_at")] string CreatedAt,
    [property: JsonPropertyName("updated_at")] string UpdatedAt);

/// <summary>グループレコード。<see cref="IsAdmin"/> が立つグループのメンバーは管理者とみなされる。</summary>
public sealed record Group(
    [property: JsonPropertyName("id")] long Id,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("description")] string Description,
    [property: JsonPropertyName("is_admin")] bool IsAdmin,
    [property: JsonPropertyName("created_at")] string CreatedAt,
    [property: JsonPropertyName("updated_at")] string UpdatedAt);

/// <summary>サービス間連携 (machine-to-machine) 用のクライアント。読み取り専用スコープのみ付与できる。</summary>
public sealed record ServiceClient(
    [property: JsonPropertyName("id")] long Id,
    [property: JsonPropertyName("client_id")] string ClientId,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("scopes")] IReadOnlyList<string> Scopes,
    [property: JsonPropertyName("is_active")] bool IsActive,
    [property: JsonPropertyName("expires_at")] string? ExpiresAt,
    [property: JsonPropertyName("created_at")] string CreatedAt,
    [property: JsonPropertyName("last_used_at")] string? LastUsedAt);

/// <summary>
/// <see cref="ServiceClientManager.CreateAsync"/> の戻り値。平文 <see cref="Secret"/> は
/// 発行時にのみ取得でき、データベースには Argon2 ハッシュのみが保存される。
/// </summary>
public sealed record ServiceClientCreateResult(
    [property: JsonPropertyName("client")] ServiceClient Client,
    [property: JsonPropertyName("secret")] string Secret);
