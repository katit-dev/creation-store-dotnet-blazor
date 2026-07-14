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

        private static int _phoneCounter = 1;

        public AuthApiTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        public Task InitializeAsync()
        {
            return Task.CompletedTask;
        }

        public async Task DisposeAsync()
        {
            await CleanupCreatedUsersAsync();
        }

        [Fact]
        public async Task Register_EmailInvalid_Returns400()
        {
            var username = CreateUsername("bademail");

            _createdUsernames.Add(username);

            var dto = new RegisterDTO
            {
                Username = username,
                Password = "123456",
                FullName = "Auth Test User",
                Email = "invalid-email-format",
                Phone = CreatePhone()
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
            var phone = CreatePhone();

            var response = await RegisterUserAsync(
                username,
                email,
                phone
            );

            var result = await response.Content
                .ReadFromJsonAsync<ResponseTypeDTO<RegisterResponseDTO>>();

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);

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
            var phone = CreatePhone();

            var firstResponse = await RegisterUserAsync(
                username,
                email,
                phone
            );

            Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);

            var duplicateDto = new RegisterDTO
            {
                Username = username,
                Password = "123456",
                FullName = "Auth Test User Duplicate",
                Email = CreateEmail(CreateUsername("dupemail")),
                Phone = CreatePhone()
            };

            var response = await _client.PostAsJsonAsync(
                "/api/auth/register",
                duplicateDto
            );

            var result = await response.Content
                .ReadFromJsonAsync<ResponseTypeDTO<object>>();

            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

            Assert.NotNull(result);
            Assert.Equal(409, result!.StatusCode);
            Assert.Equal("Username already exists", result.Message);
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

            var result = await response.Content
                .ReadFromJsonAsync<ResponseTypeDTO<LoginResponseDTO>>();

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

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

            var loginResult = await loginResponse.Content
                .ReadFromJsonAsync<ResponseTypeDTO<LoginResponseDTO>>();

            Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
            Assert.NotNull(loginResult);
            Assert.NotNull(loginResult!.Content);
            Assert.False(string.IsNullOrWhiteSpace(loginResult.Content!.Token));

            var token = loginResult.Content.Token;

            _client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            var response = await _client.GetAsync("/api/auth/me");

            var result = await response.Content
                .ReadFromJsonAsync<ResponseTypeDTO<UserProfileDTO>>();

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            Assert.NotNull(result);
            Assert.Equal(200, result!.StatusCode);
            Assert.Equal("Get profile successfully", result.Message);

            Assert.NotNull(result.Content);
            Assert.Equal(testUser.Username, result.Content!.Username);
            Assert.Equal(testUser.Email, result.Content.Email);
            Assert.Equal(testUser.Phone, result.Content.Phone);
            Assert.Contains("Member", result.Content.Roles);
        }

        private async Task<HttpResponseMessage> RegisterUserAsync(
            string username,
            string email,
            string phone,
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
            var phone = CreatePhone();
            var password = "123456";

            var response = await RegisterUserAsync(
                username,
                email,
                phone,
                password
            );

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);

            return new TestUserData
            {
                Username = username,
                Password = password,
                Email = email,
                Phone = phone
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

        private static string CreateUsername(string scenario)
        {
            return $"authtest_{scenario}_{Guid.NewGuid():N}";
        }

        private static string CreateEmail(string username)
        {
            return $"{username}@gmail.com";
        }

        private static string CreatePhone()
        {
            var number = Interlocked.Increment(ref _phoneCounter);

            return $"09{number:D8}";
        }

        private class TestUserData
        {
            public string Username { get; set; } = string.Empty;

            public string Password { get; set; } = string.Empty;

            public string Email { get; set; } = string.Empty;

            public string Phone { get; set; } = string.Empty;
        }
    }
}