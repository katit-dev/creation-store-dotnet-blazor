using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using CreationStore.API.Data;
using CreationStore.API.DTOs.Auth;
using CreationStore.API.DTOs.ResponseTypes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CreationStore.Tests
{
    public class AuthApiTests :
        IClassFixture<CustomWebApplicationFactory>,
        IAsyncLifetime
    {
        private readonly CustomWebApplicationFactory _factory;
        private readonly HttpClient _client;

        private readonly List<string> _createdUsernames = new();

        public AuthApiTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        public async Task InitializeAsync()
        {
            await CleanupUsersByPrefixAsync("autht_");
        }

        public async Task DisposeAsync()
        {
            await CleanupCreatedUsersAsync();
        }

        [Fact]
        public async Task Register_EmailInvalid_Returns400()
        {
            var username = CreateUsername("bademail");

            var dto = new RegisterDTO
            {
                Username = username,
                Password = "123456",
                FullName = "Auth Test User",
                Email = "invalid-email-format",
                Phone = null
            };

            var response = await _client.PostAsJsonAsync(
                "/api/auth/register",
                dto
            );

            var responseText = await response.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Contains("Email format is invalid", responseText);
        }

        [Fact]
        public async Task Register_Success_Returns201()
        {
            var username = CreateUsername("regok");
            var email = CreateEmail(username);

            var response = await RegisterUserAsync(
                username: username,
                email: email,
                phone: null
            );

            var responseText = await response.Content.ReadAsStringAsync();

            Assert.True(
                response.StatusCode == HttpStatusCode.Created,
                $"Expected 201 Created but got {(int)response.StatusCode} {response.StatusCode}. Body: {responseText}"
            );

            var result = await response.Content
                .ReadFromJsonAsync<ResponseTypeDTO<RegisterResponseDTO>>();

            Assert.NotNull(result);
            Assert.Equal(201, result!.StatusCode);
            Assert.Equal(
                "Register successfully. Please login to continue.",
                result.Message
            );

            Assert.NotNull(result.Content);
            Assert.True(result.Content!.UserId > 0);
            Assert.Equal(username, result.Content.Username);
        }

        [Fact]
        public async Task Register_DuplicateUsername_Returns409()
        {
            var username = CreateUsername("dup");
            var email = CreateEmail(username);

            var firstResponse = await RegisterUserAsync(
                username: username,
                email: email,
                phone: null
            );

            var firstResponseText = await firstResponse.Content.ReadAsStringAsync();

            Assert.True(
                firstResponse.StatusCode == HttpStatusCode.Created,
                $"Expected first register 201 Created but got {(int)firstResponse.StatusCode} {firstResponse.StatusCode}. Body: {firstResponseText}"
            );

            var duplicateDto = new RegisterDTO
            {
                Username = username,
                Password = "123456",
                FullName = "Auth Test User Duplicate",
                Email = CreateEmail(CreateUsername("dup2")),
                Phone = null
            };

            var response = await _client.PostAsJsonAsync(
                "/api/auth/register",
                duplicateDto
            );

            var responseText = await response.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
            Assert.Contains("Username already exists", responseText);
        }

        [Fact]
        public async Task Login_Success_ReturnsToken()
        {
            var testUser = await CreateRegisteredUserAsync("loginok");

            var dto = new LoginDTO
            {
                LoginIdentifier = testUser.Username,
                Password = testUser.Password
            };

            var response = await _client.PostAsJsonAsync(
                "/api/auth/login",
                dto
            );

            var responseText = await response.Content.ReadAsStringAsync();

            Assert.True(
                response.StatusCode == HttpStatusCode.OK,
                $"Expected 200 OK but got {(int)response.StatusCode} {response.StatusCode}. Body: {responseText}"
            );

            var result = await response.Content
                .ReadFromJsonAsync<ResponseTypeDTO<LoginResponseDTO>>();

            Assert.NotNull(result);
            Assert.Equal(200, result!.StatusCode);
            Assert.Equal("Login successfully", result.Message);

            Assert.NotNull(result.Content);
            Assert.False(string.IsNullOrWhiteSpace(result.Content!.Token));
            Assert.Equal("Bearer", result.Content.TokenType);
            Assert.True(result.Content.ExpiresAt > DateTime.UtcNow);
        }

        [Fact]
        public async Task Login_WrongPassword_Returns401()
        {
            var testUser = await CreateRegisteredUserAsync("wrongpass");

            var dto = new LoginDTO
            {
                LoginIdentifier = testUser.Username,
                Password = "wrongpassword"
            };

            var response = await _client.PostAsJsonAsync(
                "/api/auth/login",
                dto
            );

            var responseText = await response.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            Assert.Contains("Invalid", responseText);
        }

        [Fact]
        public async Task GetMe_WithValidToken_ReturnsProfile()
        {
            var testUser = await CreateRegisteredUserAsync("getme");

            var loginDto = new LoginDTO
            {
                LoginIdentifier = testUser.Username,
                Password = testUser.Password
            };

            var loginResponse = await _client.PostAsJsonAsync(
                "/api/auth/login",
                loginDto
            );

            var loginResponseText = await loginResponse.Content.ReadAsStringAsync();

            Assert.True(
                loginResponse.StatusCode == HttpStatusCode.OK,
                $"Expected login 200 OK but got {(int)loginResponse.StatusCode} {loginResponse.StatusCode}. Body: {loginResponseText}"
            );

            var loginResult = await loginResponse.Content
                .ReadFromJsonAsync<ResponseTypeDTO<LoginResponseDTO>>();

            Assert.NotNull(loginResult);
            Assert.NotNull(loginResult!.Content);
            Assert.False(string.IsNullOrWhiteSpace(loginResult.Content!.Token));

            var token = loginResult.Content.Token;

            _client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            var response = await _client.GetAsync("/api/auth/me");

            var responseText = await response.Content.ReadAsStringAsync();

            Assert.True(
                response.StatusCode == HttpStatusCode.OK,
                $"Expected /me 200 OK but got {(int)response.StatusCode} {response.StatusCode}. Body: {responseText}"
            );

            var result = await response.Content
                .ReadFromJsonAsync<ResponseTypeDTO<UserProfileDTO>>();

            Assert.NotNull(result);
            Assert.Equal(200, result!.StatusCode);
            Assert.Equal("Get profile successfully", result.Message);

            Assert.NotNull(result.Content);
            Assert.Equal(testUser.Username, result.Content!.Username);
            Assert.Equal(testUser.Email, result.Content.Email);
            Assert.Null(result.Content.Phone);
            Assert.Contains("Member", result.Content.Roles);
        }

        private async Task<HttpResponseMessage> RegisterUserAsync(
            string username,
            string email,
            string? phone,
            string password = "123456",
            string fullName = "Auth Test User"
        )
        {
            _createdUsernames.Add(username);

            var dto = new RegisterDTO
            {
                Username = username,
                Password = password,
                FullName = fullName,
                Email = email,
                Phone = phone
            };

            return await _client.PostAsJsonAsync(
                "/api/auth/register",
                dto
            );
        }

        private async Task<TestUserData> CreateRegisteredUserAsync(
            string scenario
        )
        {
            var username = CreateUsername(scenario);
            var email = CreateEmail(username);
            var password = "123456";

            var response = await RegisterUserAsync(
                username: username,
                email: email,
                phone: null,
                password: password
            );

            var responseText = await response.Content.ReadAsStringAsync();

            Assert.True(
                response.StatusCode == HttpStatusCode.Created,
                $"Expected register 201 Created but got {(int)response.StatusCode} {response.StatusCode}. Body: {responseText}"
            );

            return new TestUserData
            {
                Username = username,
                Password = password,
                Email = email,
                Phone = null
            };
        }

        private async Task CleanupCreatedUsersAsync()
        {
            var usernames = _createdUsernames
                .Distinct()
                .ToList();

            if (!usernames.Any())
            {
                return;
            }

            using var scope = _factory.Services.CreateScope();

            var db = scope.ServiceProvider
                .GetRequiredService<CreationStoreDbContext>();

            var users = await db.Users
                .Where(u => usernames.Contains(u.Username))
                .ToListAsync();

            if (!users.Any())
            {
                return;
            }

            var userIds = users
                .Select(u => u.UserId)
                .ToList();

            var userRoles = await db.UserRoles
                .Where(ur => userIds.Contains(ur.UserId))
                .ToListAsync();

            db.UserRoles.RemoveRange(userRoles);
            db.Users.RemoveRange(users);

            await db.SaveChangesAsync();
        }

        private async Task CleanupUsersByPrefixAsync(string prefix)
        {
            using var scope = _factory.Services.CreateScope();

            var db = scope.ServiceProvider
                .GetRequiredService<CreationStoreDbContext>();

            var users = await db.Users
                .Where(u => u.Username.StartsWith(prefix))
                .ToListAsync();

            if (!users.Any())
            {
                return;
            }

            var userIds = users
                .Select(u => u.UserId)
                .ToList();

            var userRoles = await db.UserRoles
                .Where(ur => userIds.Contains(ur.UserId))
                .ToListAsync();

            db.UserRoles.RemoveRange(userRoles);
            db.Users.RemoveRange(users);

            await db.SaveChangesAsync();
        }

        private static string CreateUsername(string scenario)
        {
            var shortScenario = scenario.Length > 10
                ? scenario.Substring(0, 10)
                : scenario;

            var suffix = Guid.NewGuid()
                .ToString("N")
                .Substring(0, 8);

            return $"autht_{shortScenario}_{suffix}";
        }

        private static string CreateEmail(string username)
        {
            return $"{username}@gmail.com";
        }

        private class TestUserData
        {
            public string Username { get; set; } = string.Empty;

            public string Password { get; set; } = string.Empty;

            public string Email { get; set; } = string.Empty;

            public string? Phone { get; set; }
        }
    }
}