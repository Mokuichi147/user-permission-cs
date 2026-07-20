using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UserPermission;
using Xunit;

namespace UserPermission.Tests;

public class DatabaseTests
{
    private static string TempDbPath() =>
        Path.Combine(Path.GetTempPath(), $"up_cs_test_{Guid.NewGuid():N}.db");

    private static void Cleanup(string dbPath, string secretPath)
    {
        foreach (var f in new[] { dbPath, dbPath + "-wal", dbPath + "-shm", secretPath })
        {
            if (File.Exists(f))
                File.Delete(f);
        }
    }

    [Fact]
    public async Task FullFlow_LocalSqlite()
    {
        string dbPath = TempDbPath();
        string secretPath = dbPath + ".key";
        try
        {
            await using var db = await Database.ConnectAsync(dbPath, secretPath);

            // --- ユーザー作成 / 取得 ---
            User alice = await db.Users.CreateAsync("alice", "alice-secret-1", "Alice");
            Assert.NotEqual(Guid.Empty, alice.Id);
            Assert.Equal("alice", alice.Username);
            Assert.Equal("Alice", alice.DisplayName);
            Assert.True(alice.IsActive);

            User? fetched = await db.Users.GetByUsernameAsync("alice");
            Assert.NotNull(fetched);
            Assert.Equal(alice.Id, fetched!.Id);

            Assert.Null(await db.Users.GetByIdAsync(Guid.NewGuid()));

            // 重複作成は Conflict
            UserPermissionException conflict = await Assert.ThrowsAsync<UserPermissionException>(
                () => db.Users.CreateAsync("alice", "another-pass1"));
            Assert.Equal(UserPermissionErrorKind.Conflict, conflict.Kind);

            // --- 認証 / トークン ---
            string? token = await db.LoginAsync("alice", "alice-secret-1");
            Assert.NotNull(token);

            User? resolved = await db.VerifyTokenAndGetUserAsync(token);
            Assert.NotNull(resolved);
            Assert.Equal(alice.Id, resolved!.Id);

            Assert.Null(await db.VerifyTokenAndGetUserAsync(null));
            Assert.Null(await db.LoginAsync("alice", "wrong-password"));

            // --- 管理者ロール (最初に作成されたユーザーは自動的に admin になる) ---
            Assert.True(await db.Users.IsAdminAsync(alice.Id));

            var adminGroups = await db.Groups.ListAdminGroupsAsync();
            Assert.Single(adminGroups);
            Assert.Equal("admin", adminGroups[0].Name);
            Assert.True(adminGroups[0].IsAdmin);

            // 2 人目以降は管理者ではない。SetAdmin で昇格・降格できる。
            User bob = await db.Users.CreateAsync("bob", "bob-secret-1", "Bob");
            Assert.False(await db.Users.IsAdminAsync(bob.Id));
            Assert.True(await db.Users.SetAdminAsync(bob.Id, true));
            Assert.True(await db.Users.IsAdminAsync(bob.Id));
            Assert.True(await db.Users.SetAdminAsync(bob.Id, false));
            Assert.False(await db.Users.IsAdminAsync(bob.Id));

            // --- グループ / メンバーシップ ---
            Group group = await db.Groups.CreateAsync("editors", "Editors");
            Assert.Equal("editors", group.Name);
            Assert.False(group.IsAdmin);

            Assert.True(await db.Groups.AddUserAsync(group.Id, bob.Id));

            var members = await db.Groups.GetMembersAsync(group.Id);
            Assert.Single(members);
            Assert.Equal(bob.Id, members[0].Id);

            var bobGroups = await db.Groups.GetUserGroupsAsync(bob.Id);
            Assert.Contains(bobGroups, g => g.Id == group.Id);

            // --- 更新 ---
            User? updated = await db.Users.UpdateAsync(alice.Id, displayName: "Alice Smith");
            Assert.NotNull(updated);
            Assert.Equal("Alice Smith", updated!.DisplayName);

            Group? updatedGroup = await db.Groups.UpdateAsync(group.Id, description: "All editors");
            Assert.NotNull(updatedGroup);
            Assert.Equal("All editors", updatedGroup!.Description);

            // --- サービスクライアント ---
            var (client, secret) = await db.ServiceClients.CreateAsync("reader", new[] { Scopes.UsersRead });
            Assert.StartsWith("svc_", client.ClientId);
            Assert.False(string.IsNullOrEmpty(secret));
            Assert.Contains(Scopes.UsersRead, client.Scopes);

            string? svcToken = await db.LoginServiceAsync(client.ClientId, secret);
            Assert.NotNull(svcToken);

            var clients = await db.ServiceClients.ListAsync();
            Assert.Single(clients);
            Assert.Equal(client.ClientId, clients[0].ClientId);

            // --- 削除 ---
            Assert.True(await db.Groups.RemoveUserAsync(group.Id, bob.Id));
            Assert.Empty(await db.Groups.GetMembersAsync(group.Id));

            Assert.True(await db.Users.DeleteAsync(bob.Id));
            Assert.Null(await db.Users.GetByIdAsync(bob.Id));

            var remaining = await db.Users.ListAllAsync();
            Assert.Single(remaining);
            Assert.Equal(alice.Id, remaining[0].Id);
        }
        finally
        {
            Cleanup(dbPath, secretPath);
        }
    }

    [Fact]
    public async Task Operations_BeforeConnect_ThrowNotConnected()
    {
        using var db = new Database(TempDbPath());
        UserPermissionException ex = await Assert.ThrowsAsync<UserPermissionException>(
            () => db.Users.ListAllAsync());
        Assert.Equal(UserPermissionErrorKind.NotConnected, ex.Kind);
    }

    [Fact]
    public void ValidateScopes_RejectsUnknownScope()
    {
        // 既知スコープは通る
        Scopes.Validate(new[] { Scopes.UsersRead, Scopes.GroupsRead });

        UserPermissionException ex = Assert.Throws<UserPermissionException>(
            () => Scopes.Validate(new[] { "admin:write" }));
        Assert.Equal(UserPermissionErrorKind.InvalidArgument, ex.Kind);
    }

    [Fact]
    public void Version_IsNotEmpty()
    {
        Assert.False(string.IsNullOrEmpty(Library.Version));
    }
}
