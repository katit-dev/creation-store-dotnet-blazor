using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using CreationStore.API.Data;
using CreationStore.API.DTOs.Admin.Users;
using CreationStore.API.DTOs.Auth;
using CreationStore.API.DTOs.ResponseTypes;
using CreationStore.API.Helpers.Constant;
using CreationStore.API.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CreationStore.Tests
{
    public class AdminUserApiTests :
        IClassFixture<CustomWebApplicationFactory>,
        IAsyncLifetime
    {
        private readonly CustomWebApplicationFactory _factory;
        private readonly HttpClient _client;

        private static readonly JsonSerializerOptions JsonOptions = new(
            JsonSerializerDefaults.Web
        );

        public AdminUserApiTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        public async Task InitializeAsync()
        {
            await CleanupTestDataAsync();
        }

        public async Task DisposeAsync()
        {
            _client.DefaultRequestHeaders.Authorization = null;
            await CleanupTestDataAsync();
        }

        // ============================================================
        // 1. GET ALL USERS - NO TOKEN
        // ============================================================
        [Fact]
        public async Task GetAllUsers_WithoutToken_Returns401()
        {
            SetBearerToken(null);

            var response = await _client.GetAsync("/api/admin/users");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        // ============================================================
        // 2. GET ALL USERS - MEMBER TOKEN
        // ============================================================
        [Fact]
        public async Task GetAllUsers_WithMemberToken_Returns403()
        {
            var member = await CreateMemberUserAsync("getallmember");
            SetBearerToken(member.Token);

            var response = await _client.GetAsync("/api/admin/users");

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        // ============================================================
        // 3. GET ALL USERS - ADMIN TOKEN
        // ============================================================
        [Fact]
        public async Task GetAllUsers_WithAdminToken_Returns200()
        {
            var admin = await CreateAdminUserAsync("getalladmin");
            SetBearerToken(admin.Token);

            var response = await _client.GetAsync("/api/admin/users");

            var result = await response.Content
                .ReadFromJsonAsync<
                    ResponseTypeDTO<List<AdminUserResponseDTO>>
                >(JsonOptions);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(result);
            Assert.Equal(200, result!.StatusCode);
            Assert.NotNull(result.Content);
        }

        // ============================================================
        // 4. GET USER BY ID - NOT FOUND
        // ============================================================
        [Fact]
        public async Task GetUserById_NotFound_Returns404()
        {
            var admin = await CreateAdminUserAsync("detailnotfound");
            SetBearerToken(admin.Token);

            var response = await _client.GetAsync(
                "/api/admin/users/999999999"
            );

            var result = await response.Content
                .ReadFromJsonAsync<
                    ResponseTypeDTO<AdminUserDetailResponseDTO>
                >(JsonOptions);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            Assert.NotNull(result);
            Assert.Equal(404, result!.StatusCode);
            Assert.Null(result.Content);
        }

        // ============================================================
        // 5. GET USER BY ID - SUCCESS
        // ============================================================
        [Fact]
        public async Task GetUserById_WithAdminToken_Returns200()
        {
            var admin = await CreateAdminUserAsync("detailsuccess");
            var member = await CreateMemberUserAsync("targetdetail");

            SetBearerToken(admin.Token);

            var response = await _client.GetAsync(
                $"/api/admin/users/{member.UserId}"
            );

            var result = await response.Content
                .ReadFromJsonAsync<
                    ResponseTypeDTO<AdminUserDetailResponseDTO>
                >(JsonOptions);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(result);
            Assert.Equal(200, result!.StatusCode);
            Assert.NotNull(result.Content);
            Assert.Equal(member.UserId, result.Content!.UserId);
            Assert.Equal(member.Username, result.Content.Username);
            Assert.Contains(CRole.Member, result.Content.RoleIds);
            Assert.True(result.Content.OrderCount >= 0);
            Assert.True(result.Content.TotalSpent >= 0);
        }

        // ============================================================
        // 6. CHANGE USER ROLE - NO TOKEN
        // ============================================================
        [Fact]
        public async Task ChangeUserRole_WithoutToken_Returns401()
        {
            var member = await CreateMemberUserAsync("changewithouttoken");

            SetBearerToken(null);

            var response = await _client.PutAsJsonAsync(
                $"/api/admin/users/{member.UserId}/role",
                new AdminChangeUserRoleDTO
                {
                    RoleId = CRole.Admin
                }
            );

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        // ============================================================
        // 7. CHANGE USER ROLE - MEMBER TOKEN
        // ============================================================
        [Fact]
        public async Task ChangeUserRole_WithMemberToken_Returns403()
        {
            var memberTokenUser = await CreateMemberUserAsync("membertoken");
            var targetUser = await CreateMemberUserAsync("targetmember");

            SetBearerToken(memberTokenUser.Token);

            var response = await _client.PutAsJsonAsync(
                $"/api/admin/users/{targetUser.UserId}/role",
                new AdminChangeUserRoleDTO
                {
                    RoleId = CRole.Admin
                }
            );

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        // ============================================================
        // 8. CHANGE USER ROLE - USER NOT FOUND
        // ============================================================
        [Fact]
        public async Task ChangeUserRole_UserNotFound_Returns404()
        {
            var admin = await CreateAdminUserAsync("changenotfound");

            SetBearerToken(admin.Token);

            var response = await _client.PutAsJsonAsync(
                "/api/admin/users/999999999/role",
                new AdminChangeUserRoleDTO
                {
                    RoleId = CRole.Admin
                }
            );

            var result = await response.Content
                .ReadFromJsonAsync<ResponseTypeDTO<AdminUserResponseDTO>>(
                    JsonOptions
                );

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            Assert.NotNull(result);
            Assert.Equal(404, result!.StatusCode);
            Assert.Null(result.Content);
        }

        // ============================================================
        // 9. CHANGE USER ROLE - ROLE ID ZERO
        // ============================================================
        [Fact]
        public async Task ChangeUserRole_RoleIdZero_Returns400()
        {
            var admin = await CreateAdminUserAsync("roleidzero");
            var member = await CreateMemberUserAsync("targetrolezero");

            SetBearerToken(admin.Token);

            var response = await _client.PutAsJsonAsync(
                $"/api/admin/users/{member.UserId}/role",
                new AdminChangeUserRoleDTO
                {
                    RoleId = 0
                }
            );

            var result = await response.Content
                .ReadFromJsonAsync<ResponseTypeDTO<AdminUserResponseDTO>>(
                    JsonOptions
                );

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.NotNull(result);
            Assert.Equal(400, result!.StatusCode);
            Assert.Null(result.Content);
        }

        // ============================================================
        // 10. CHANGE USER ROLE - INVALID ROLE
        // ============================================================
        [Fact]
        public async Task ChangeUserRole_InvalidRole_Returns400()
        {
            var admin = await CreateAdminUserAsync("invalidrole");
            var member = await CreateMemberUserAsync("targetinvalidrole");

            SetBearerToken(admin.Token);

            var response = await _client.PutAsJsonAsync(
                $"/api/admin/users/{member.UserId}/role",
                new AdminChangeUserRoleDTO
                {
                    RoleId = 999999
                }
            );

            var result = await response.Content
                .ReadFromJsonAsync<ResponseTypeDTO<AdminUserResponseDTO>>(
                    JsonOptions
                );

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.NotNull(result);
            Assert.Equal(400, result!.StatusCode);
            Assert.Null(result.Content);
        }

        // ============================================================
        // 11. CHANGE MEMBER TO ADMIN - SUCCESS
        // ============================================================
        [Fact]
        public async Task ChangeUserRole_MemberToAdmin_Returns200()
        {
            var admin = await CreateAdminUserAsync("membertoadmin");
            var member = await CreateMemberUserAsync("targettoadmin");

            SetBearerToken(admin.Token);

            var response = await _client.PutAsJsonAsync(
                $"/api/admin/users/{member.UserId}/role",
                new AdminChangeUserRoleDTO
                {
                    RoleId = CRole.Admin
                }
            );

            var result = await response.Content
                .ReadFromJsonAsync<ResponseTypeDTO<AdminUserResponseDTO>>(
                    JsonOptions
                );

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(result);
            Assert.Equal(200, result!.StatusCode);
            Assert.NotNull(result.Content);
            Assert.Equal(member.UserId, result.Content!.UserId);
            Assert.Contains(CRole.Admin, result.Content.RoleIds);
            Assert.Contains(
                "Admin",
                result.Content.Roles,
                StringComparer.OrdinalIgnoreCase
            );
        }

        // ============================================================
        // 12. ADMIN CANNOT CHANGE OWN ROLE
        // ============================================================
        [Fact]
        public async Task ChangeUserRole_AdminChangeOwnRole_Returns400()
        {
            var admin = await CreateAdminUserAsync("ownrole");

            SetBearerToken(admin.Token);

            var response = await _client.PutAsJsonAsync(
                $"/api/admin/users/{admin.UserId}/role",
                new AdminChangeUserRoleDTO
                {
                    RoleId = CRole.Member
                }
            );

            var result = await response.Content
                .ReadFromJsonAsync<ResponseTypeDTO<AdminUserResponseDTO>>(
                    JsonOptions
                );

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.NotNull(result);
            Assert.Equal(400, result!.StatusCode);
            Assert.Null(result.Content);
        }

        // ============================================================
        // HELPERS
        // ============================================================

        private async Task<TestUserInfo> CreateMemberUserAsync(string testName)
        {
            var username = await RegisterUserAsync(testName);
            var userId = await GetUserIdByUsernameAsync(username);
            var token = await LoginAsync(username);

            return new TestUserInfo
            {
                UserId = userId,
                Username = username,
                Token = token
            };
        }

        private async Task<TestUserInfo> CreateAdminUserAsync(string testName)
        {
            var username = await RegisterUserAsync(testName);

            using var scope = _factory.Services.CreateScope();

            var db = scope.ServiceProvider
                .GetRequiredService<CreationStoreDbContext>();

            var user = await db.Users
                .FirstAsync(u => u.Username == username);

            var oldRoles = await db.UserRoles
                .Where(ur => ur.UserId == user.UserId)
                .ToListAsync();

            db.UserRoles.RemoveRange(oldRoles);

            db.UserRoles.Add(new UserRole
            {
                UserId = user.UserId,
                RoleId = CRole.Admin
            });

            await db.SaveChangesAsync();

            var token = await LoginAsync(username);

            return new TestUserInfo
            {
                UserId = user.UserId,
                Username = username,
                Token = token
            };
        }

        private async Task<string> RegisterUserAsync(string testName)
        {
            var suffix = Guid.NewGuid()
                .ToString("N")
                .Substring(0, 10);

            var username = $"adminusertest{testName}{suffix}";
            var password = "123456";
            var phone = "05" + Random.Shared
                .Next(10000000, 99999999)
                .ToString();

            var registerDto = new RegisterDTO
            {
                Username = username,
                Password = password,
                FullName = "Admin User Test User",
                Email = $"{username}@gmail.com",
                Phone = phone
            };

            var response = await _client.PostAsJsonAsync(
                "/api/auth/register",
                registerDto
            );

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);

            return username;
        }

        private async Task<string> LoginAsync(string username)
        {
            var loginDto = new LoginDTO
            {
                LoginIdentifier = username,
                Password = "123456"
            };

            var response = await _client.PostAsJsonAsync(
                "/api/auth/login",
                loginDto
            );

            var result = await response.Content
                .ReadFromJsonAsync<ResponseTypeDTO<LoginResponseDTO>>(
                    JsonOptions
                );

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(result);
            Assert.NotNull(result!.Content);
            Assert.False(
                string.IsNullOrWhiteSpace(result.Content!.Token)
            );

            return result.Content.Token;
        }

        private async Task<int> GetUserIdByUsernameAsync(string username)
        {
            using var scope = _factory.Services.CreateScope();

            var db = scope.ServiceProvider
                .GetRequiredService<CreationStoreDbContext>();

            return await db.Users
                .Where(u => u.Username == username)
                .Select(u => u.UserId)
                .FirstAsync();
        }

        private void SetBearerToken(string? token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                _client.DefaultRequestHeaders.Authorization = null;
                return;
            }

            _client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);
        }

        private async Task CleanupTestDataAsync()
        {
            using var scope = _factory.Services.CreateScope();

            var db = scope.ServiceProvider
                .GetRequiredService<CreationStoreDbContext>();

            var userIds = await db.Users
                .Where(u => u.Username.StartsWith("adminusertest"))
                .Select(u => u.UserId)
                .ToListAsync();

            if (userIds.Any())
            {
                var userRoles = await db.UserRoles
                    .Where(ur => userIds.Contains(ur.UserId))
                    .ToListAsync();

                db.UserRoles.RemoveRange(userRoles);

                var users = await db.Users
                    .Where(u => userIds.Contains(u.UserId))
                    .ToListAsync();

                db.Users.RemoveRange(users);
            }

            await db.SaveChangesAsync();
        }

        private class TestUserInfo
        {
            public int UserId { get; set; }
            public string Username { get; set; } = string.Empty;
            public string Token { get; set; } = string.Empty;
        }
    }
}