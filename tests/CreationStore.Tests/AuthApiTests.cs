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

        public AuthApiTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        public async Task InitializeAsync()
        {
            await CleanupTestUsersAsync();
        }

        public async Task DisposeAsync()
        {
            await CleanupTestUsersAsync();
        }

        [Fact]
        public async Task Register_EmailInvalid_Returns400()
        {
            var dto = new RegisterDTO
            {
                Username = "authtest01",
                Password = "123456",
                FullName = "Auth Test User",
                Email = "authtest01gmail.com",
                Phone = "0900000001"
            };

            var response = await _client.PostAsJsonAsync(
                "/api/auth/register",
                dto
            );

            var result = await response.Content
                .ReadFromJsonAsync<ResponseTypeDTO<object>>();

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.NotNull(result);
            Assert.Equal(400, result!.StatusCode);
            Assert.Equal("Email format is invalid", result.Message);
        }

        [Fact]
        public async Task Register_Success_Returns201()
        {
            var dto = new RegisterDTO
            {
                Username = "authtest01",
                Password = "123456",
                FullName = "Auth Test User",
                Email = "authtest01@gmail.com",
                Phone = "0900000001"
            };

            var response = await _client.PostAsJsonAsync(
                "/api/auth/register",
                dto
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
            Assert.Equal("authtest01", result.Content!.Username);
            Assert.True(result.Content.UserId > 0);
        }

        [Fact]
        public async Task Register_DuplicateUsername_Returns409()
        {
            var firstRegister = new RegisterDTO
            {
                Username = "authtest01",
                Password = "123456",
                FullName = "Auth Test User",
                Email = "authtest01@gmail.com",
                Phone = "0900000001"
            };

            await _client.PostAsJsonAsync(
                "/api/auth/register",
                firstRegister
            );

            var duplicateRegister = new RegisterDTO
            {
                Username = "authtest01",
                Password = "123456",
                FullName = "Auth Test User 2",
                Email = "authtest02@gmail.com",
                Phone = "0900000002"
            };

            var response = await _client.PostAsJsonAsync(
                "/api/auth/register",
                duplicateRegister
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
            await RegisterDefaultTestUserAsync();

            var dto = new LoginDTO
            {
                LoginIdentifier = "authtest01",
                Password = "123456"
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
        }

        [Fact]
        public async Task Login_WrongPassword_Returns401()
        {
            await RegisterDefaultTestUserAsync();

            var dto = new LoginDTO
            {
                LoginIdentifier = "authtest01",
                Password = "wrongpassword"
            };

            var response = await _client.PostAsJsonAsync(
                "/api/auth/login",
                dto
            );

            var result = await response.Content
                .ReadFromJsonAsync<ResponseTypeDTO<object>>();

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            Assert.NotNull(result);
            Assert.Equal(401, result!.StatusCode);
            Assert.Equal("Invalid username or password", result.Message);
        }

        [Fact]
        public async Task GetMe_WithValidToken_ReturnsProfile()
        {
            await RegisterDefaultTestUserAsync();

            var loginDto = new LoginDTO
            {
                LoginIdentifier = "authtest01",
                Password = "123456"
            };

            var loginResponse = await _client.PostAsJsonAsync(
                "/api/auth/login",
                loginDto
            );

            var loginResult = await loginResponse.Content
                .ReadFromJsonAsync<ResponseTypeDTO<LoginResponseDTO>>();

            Assert.NotNull(loginResult);
            Assert.NotNull(loginResult!.Content);

            var token = loginResult.Content!.Token;

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
            Assert.Equal("authtest01", result.Content!.Username);
            Assert.Contains("Member", result.Content.Roles);
        }

        private async Task RegisterDefaultTestUserAsync()
        {
            var dto = new RegisterDTO
            {
                Username = "authtest01",
                Password = "123456",
                FullName = "Auth Test User",
                Email = "authtest01@gmail.com",
                Phone = "0900000001"
            };

            var response = await _client.PostAsJsonAsync(
                "/api/auth/register",
                dto
            );

            Assert.True(
                response.StatusCode == HttpStatusCode.Created ||
                response.StatusCode == HttpStatusCode.Conflict
            );
        }

        private async Task CleanupTestUsersAsync()
        {
            using var scope = _factory.Services.CreateScope();

            var db = scope.ServiceProvider
                .GetRequiredService<CreationStoreDbContext>();

            var users = await db.Users
                .Where(u => u.Username.StartsWith("authtest"))
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
    }
}