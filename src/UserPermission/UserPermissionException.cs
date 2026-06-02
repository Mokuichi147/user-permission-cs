using System;
using System.Text.Json;

namespace UserPermission;

/// <summary>コア (Rust) が返したエラー種別。<see cref="UserPermissionException.Kind"/> で参照する。</summary>
public enum UserPermissionErrorKind
{
    /// <summary>対象が見つからない。</summary>
    NotFound,
    /// <summary>一意制約違反など、既存データとの競合。</summary>
    Conflict,
    /// <summary>認証情報が不正。</summary>
    InvalidCredentials,
    /// <summary>secret 未指定でトークン操作を行おうとした。</summary>
    MissingTokenManager,
    /// <summary>未接続のまま操作を行おうとした。</summary>
    NotConnected,
    /// <summary>データベースエラー。</summary>
    Database,
    /// <summary>マイグレーションエラー。</summary>
    Migrate,
    /// <summary>パスワードハッシュ処理のエラー。</summary>
    Password,
    /// <summary>JWT 処理のエラー。</summary>
    Jwt,
    /// <summary>リレー通信時の HTTP エラー。</summary>
    Http,
    /// <summary>I/O エラー。</summary>
    Io,
    /// <summary>URL 解析エラー。</summary>
    Url,
    /// <summary>リレーサーバーがエラーステータスを返した。</summary>
    Relay,
    /// <summary>引数が不正 (未知のスコープ・誤った URL スキームなど)。</summary>
    InvalidArgument,
    /// <summary>上記いずれにも分類されないエラー (FFI 層の異常を含む)。</summary>
    Other,
}

/// <summary>user-permission の操作が失敗したときに送出される例外。</summary>
public sealed class UserPermissionException : Exception
{
    /// <summary>エラー種別。</summary>
    public UserPermissionErrorKind Kind { get; }

    /// <summary>エラー種別とメッセージを指定して例外を生成する。</summary>
    public UserPermissionException(UserPermissionErrorKind kind, string message)
        : base(message)
    {
        Kind = kind;
    }

    internal static UserPermissionException FromJson(JsonElement err)
    {
        string? kindStr = err.TryGetProperty("kind", out JsonElement k) ? k.GetString() : null;
        string? message = err.TryGetProperty("message", out JsonElement m) ? m.GetString() : null;

        UserPermissionErrorKind kind = kindStr switch
        {
            "NotFound" => UserPermissionErrorKind.NotFound,
            "Conflict" => UserPermissionErrorKind.Conflict,
            "InvalidCredentials" => UserPermissionErrorKind.InvalidCredentials,
            "MissingTokenManager" => UserPermissionErrorKind.MissingTokenManager,
            "NotConnected" => UserPermissionErrorKind.NotConnected,
            "Database" => UserPermissionErrorKind.Database,
            "Migrate" => UserPermissionErrorKind.Migrate,
            "Password" => UserPermissionErrorKind.Password,
            "Jwt" => UserPermissionErrorKind.Jwt,
            "Http" => UserPermissionErrorKind.Http,
            "Io" => UserPermissionErrorKind.Io,
            "Url" => UserPermissionErrorKind.Url,
            "Relay" => UserPermissionErrorKind.Relay,
            "InvalidArgument" => UserPermissionErrorKind.InvalidArgument,
            _ => UserPermissionErrorKind.Other,
        };

        return new UserPermissionException(kind, message ?? "unknown error");
    }
}
