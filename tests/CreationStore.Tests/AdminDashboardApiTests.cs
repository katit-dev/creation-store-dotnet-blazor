using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using CreationStore.API.Data;
using CreationStore.API.DTOs.Admin.Dashboard;
using CreationStore.API.DTOs.Auth;
using CreationStore.API.DTOs.ResponseTypes;
using CreationStore.API.Helpers.Constant;
using CreationStore.API.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CreationStore.Tests
{
    public class AdminDashboardApiTests :
        IClassFixture<CustomWebApplicationFactory>,
        IAsyncLifetime
    {
        private readonly CustomWebApplicationFactory _factory;
        private readonly HttpClient _client;

        private static readonly JsonSerializerOptions JsonOptions = new(
            JsonSerializerDefaults.Web
        );

        public AdminDashboardApiTests(CustomWebApplicationFactory factory)
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
        // 1. SUMMARY - NO TOKEN
        // ============================================================
        [Fact]
        public async Task GetSummary_WithoutToken_Returns401()
        {
            SetBearerToken(null);

            var response = await _client.GetAsync(
                "/api/admin/dashboard/summary"
            );

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        // ============================================================
        // 2. SUMMARY - MEMBER TOKEN
        // ============================================================
        [Fact]
        public async Task GetSummary_WithMemberToken_Returns403()
        {
            var member = await CreateMemberUserAsync("summarymember");
            SetBearerToken(member.Token);

            var response = await _client.GetAsync(
                "/api/admin/dashboard/summary"
            );

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        // ============================================================
        // 3. SUMMARY - ADMIN TOKEN
        // ============================================================
        [Fact]
        public async Task GetSummary_WithAdminToken_Returns200()
        {
            var admin = await CreateAdminUserAsync("summaryadmin");
            var member = await CreateMemberUserAsync("summarytarget");
            await CreateSucceededOrderAsync(member.UserId, "summaryorder");

            SetBearerToken(admin.Token);

            var response = await _client.GetAsync(
                "/api/admin/dashboard/summary"
            );

            var result = await response.Content
                .ReadFromJsonAsync<
                    ResponseTypeDTO<AdminDashboardSummaryDTO>
                >(JsonOptions);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(result);
            Assert.Equal(200, result!.StatusCode);
            Assert.NotNull(result.Content);
            Assert.True(result.Content!.TotalUsers >= 2);
            Assert.True(result.Content.TotalOrders >= 1);
            Assert.True(result.Content.TotalRevenue >= 100000);
        }

        // ============================================================
        // 4. REVENUE - NO TOKEN
        // ============================================================
        [Fact]
        public async Task GetRevenue_WithoutToken_Returns401()
        {
            SetBearerToken(null);

            var response = await _client.GetAsync(
                "/api/admin/dashboard/revenue"
            );

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        // ============================================================
        // 5. REVENUE - MEMBER TOKEN
        // ============================================================
        [Fact]
        public async Task GetRevenue_WithMemberToken_Returns403()
        {
            var member = await CreateMemberUserAsync("revenuemember");
            SetBearerToken(member.Token);

            var response = await _client.GetAsync(
                "/api/admin/dashboard/revenue"
            );

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        // ============================================================
        // 6. REVENUE - DEFAULT DATE RANGE
        // ============================================================
        [Fact]
        public async Task GetRevenue_DefaultDateRange_Returns200()
        {
            var admin = await CreateAdminUserAsync("revenuedefaultadmin");
            var member = await CreateMemberUserAsync("revenuedefaultmember");
            await CreateSucceededOrderAsync(member.UserId, "revenuedefault");

            SetBearerToken(admin.Token);

            var response = await _client.GetAsync(
                "/api/admin/dashboard/revenue"
            );

            var result = await response.Content
                .ReadFromJsonAsync<
                    ResponseTypeDTO<AdminRevenueStatisticDTO>
                >(JsonOptions);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(result);
            Assert.Equal(200, result!.StatusCode);
            Assert.NotNull(result.Content);
            Assert.True(result.Content!.TotalRevenue >= 100000);
            Assert.NotNull(result.Content.Items);
        }

        // ============================================================
        // 7. REVENUE - VALID DATE RANGE
        // ============================================================
        [Fact]
        public async Task GetRevenue_ValidDateRange_Returns200()
        {
            var admin = await CreateAdminUserAsync("revenuevalidadmin");
            var member = await CreateMemberUserAsync("revenuevalidmember");
            await CreateSucceededOrderAsync(member.UserId, "revenuevalid");

            var fromDate = DateTime.Today.AddDays(-2).ToString("yyyy-MM-dd");
            var toDate = DateTime.Today.ToString("yyyy-MM-dd");

            SetBearerToken(admin.Token);

            var response = await _client.GetAsync(
                $"/api/admin/dashboard/revenue?fromDate={fromDate}&toDate={toDate}"
            );

            var result = await response.Content
                .ReadFromJsonAsync<
                    ResponseTypeDTO<AdminRevenueStatisticDTO>
                >(JsonOptions);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(result);
            Assert.Equal(200, result!.StatusCode);
            Assert.NotNull(result.Content);
            Assert.True(result.Content!.TotalRevenue >= 100000);
            Assert.NotEmpty(result.Content.Items);
        }

        // ============================================================
        // 8. REVENUE - INVALID DATE RANGE
        // ============================================================
        [Fact]
        public async Task GetRevenue_InvalidDateRange_Returns400()
        {
            var admin = await CreateAdminUserAsync("revenueinvalidadmin");

            var fromDate = DateTime.Today.ToString("yyyy-MM-dd");
            var toDate = DateTime.Today.AddDays(-5).ToString("yyyy-MM-dd");

            SetBearerToken(admin.Token);

            var response = await _client.GetAsync(
                $"/api/admin/dashboard/revenue?fromDate={fromDate}&toDate={toDate}"
            );

            var result = await response.Content
                .ReadFromJsonAsync<
                    ResponseTypeDTO<AdminRevenueStatisticDTO>
                >(JsonOptions);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.NotNull(result);
            Assert.Equal(400, result!.StatusCode);
            Assert.Null(result.Content);
        }

        // ============================================================
        // 9. TOP PRODUCTS - NO TOKEN
        // ============================================================
        [Fact]
        public async Task GetTopProducts_WithoutToken_Returns401()
        {
            SetBearerToken(null);

            var response = await _client.GetAsync(
                "/api/admin/dashboard/top-products"
            );

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        // ============================================================
        // 10. TOP PRODUCTS - MEMBER TOKEN
        // ============================================================
        [Fact]
        public async Task GetTopProducts_WithMemberToken_Returns403()
        {
            var member = await CreateMemberUserAsync("topmember");
            SetBearerToken(member.Token);

            var response = await _client.GetAsync(
                "/api/admin/dashboard/top-products"
            );

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        // ============================================================
        // 11. TOP PRODUCTS - DEFAULT TAKE
        // ============================================================
        [Fact]
        public async Task GetTopProducts_DefaultTake_Returns200()
        {
            var admin = await CreateAdminUserAsync("topdefaultadmin");
            SetBearerToken(admin.Token);

            var response = await _client.GetAsync(
                "/api/admin/dashboard/top-products"
            );

            var result = await response.Content
                .ReadFromJsonAsync<
                    ResponseTypeDTO<List<AdminTopProductDTO>>
                >(JsonOptions);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(result);
            Assert.Equal(200, result!.StatusCode);
            Assert.NotNull(result.Content);
            Assert.True(result.Content!.Count <= 5);
        }

        // ============================================================
        // 12. TOP PRODUCTS - TAKE ZERO USES DEFAULT 5
        // ============================================================
        [Fact]
        public async Task GetTopProducts_TakeZero_Returns200AndMaxFiveItems()
        {
            var admin = await CreateAdminUserAsync("topzero") ;
            SetBearerToken(admin.Token);

            var response = await _client.GetAsync(
                "/api/admin/dashboard/top-products?take=0"
            );

            var result = await response.Content
                .ReadFromJsonAsync<
                    ResponseTypeDTO<List<AdminTopProductDTO>>
                >(JsonOptions);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(result);
            Assert.NotNull(result!.Content);
            Assert.True(result.Content!.Count <= 5);
        }

        // ============================================================
        // 13. TOP PRODUCTS - TAKE TOO LARGE USES MAX 20
        // ============================================================
        [Fact]
        public async Task GetTopProducts_TakeTooLarge_Returns200AndMaxTwentyItems()
        {
            var admin = await CreateAdminUserAsync("toptoolarge");
            SetBearerToken(admin.Token);

            var response = await _client.GetAsync(
                "/api/admin/dashboard/top-products?take=100"
            );

            var result = await response.Content
                .ReadFromJsonAsync<
                    ResponseTypeDTO<List<AdminTopProductDTO>>
                >(JsonOptions);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(result);
            Assert.NotNull(result!.Content);
            Assert.True(result.Content!.Count <= 20);
        }

        // ============================================================
        // 14. RECENT ORDERS - NO TOKEN
        // ============================================================
        [Fact]
        public async Task GetRecentOrders_WithoutToken_Returns401()
        {
            SetBearerToken(null);

            var response = await _client.GetAsync(
                "/api/admin/dashboard/recent-orders"
            );

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        // ============================================================
        // 15. RECENT ORDERS - MEMBER TOKEN
        // ============================================================
        [Fact]
        public async Task GetRecentOrders_WithMemberToken_Returns403()
        {
            var member = await CreateMemberUserAsync("recentmember");
            SetBearerToken(member.Token);

            var response = await _client.GetAsync(
                "/api/admin/dashboard/recent-orders"
            );

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        // ============================================================
        // 16. RECENT ORDERS - DEFAULT TAKE
        // ============================================================
        [Fact]
        public async Task GetRecentOrders_DefaultTake_Returns200()
        {
            var admin = await CreateAdminUserAsync("recentdefaultadmin");
            var member = await CreateMemberUserAsync("recentdefaultmember");
            var order = await CreateSucceededOrderAsync(
                member.UserId,
                "recentdefault"
            );

            SetBearerToken(admin.Token);

            var response = await _client.GetAsync(
                "/api/admin/dashboard/recent-orders"
            );

            var result = await response.Content
                .ReadFromJsonAsync<
                    ResponseTypeDTO<List<AdminRecentOrderDTO>>
                >(JsonOptions);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(result);
            Assert.Equal(200, result!.StatusCode);
            Assert.NotNull(result.Content);
            Assert.True(result.Content!.Count <= 10);
            Assert.Contains(result.Content, o => o.OrderId == order.OrderId);
        }

        // ============================================================
        // 17. RECENT ORDERS - TAKE ZERO USES DEFAULT 10
        // ============================================================
        [Fact]
        public async Task GetRecentOrders_TakeZero_Returns200AndMaxTenItems()
        {
            var admin = await CreateAdminUserAsync("recentzero");
            SetBearerToken(admin.Token);

            var response = await _client.GetAsync(
                "/api/admin/dashboard/recent-orders?take=0"
            );

            var result = await response.Content
                .ReadFromJsonAsync<
                    ResponseTypeDTO<List<AdminRecentOrderDTO>>
                >(JsonOptions);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(result);
            Assert.NotNull(result!.Content);
            Assert.True(result.Content!.Count <= 10);
        }

        // ============================================================
        // 18. RECENT ORDERS - TAKE TOO LARGE USES MAX 50
        // ============================================================
        [Fact]
        public async Task GetRecentOrders_TakeTooLarge_Returns200AndMaxFiftyItems()
        {
            var admin = await CreateAdminUserAsync("recenttoolarge");
            SetBearerToken(admin.Token);

            var response = await _client.GetAsync(
                "/api/admin/dashboard/recent-orders?take=100"
            );

            var result = await response.Content
                .ReadFromJsonAsync<
                    ResponseTypeDTO<List<AdminRecentOrderDTO>>
                >(JsonOptions);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(result);
            Assert.NotNull(result!.Content);
            Assert.True(result.Content!.Count <= 50);
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

        private async Task<TestOrderInfo> CreateSucceededOrderAsync(
            int userId,
            string testName
        )
        {
            using var scope = _factory.Services.CreateScope();

            var db = scope.ServiceProvider
                .GetRequiredService<CreationStoreDbContext>();

            var order = new Order
            {
                UserId = userId,
                TotalAmount = 100000,
                Status = COrderStatus.Paid,
                PaymentStatus = CPaymentStatus.Succeeded,
                OrderDate = DateTime.Today.AddDays(-1).AddHours(10),
                Note = $"admindashboardtest-{testName}"
            };

            db.Orders.Add(order);
            await db.SaveChangesAsync();

            return new TestOrderInfo
            {
                OrderId = order.OrderId,
                UserId = userId
            };
        }

        private async Task<string> RegisterUserAsync(string testName)
        {
            var suffix = Guid.NewGuid()
                .ToString("N")
                .Substring(0, 10);

            var username = $"admindashboardtest{testName}{suffix}";
            var password = "123456";
            var phone = "07" + Random.Shared
                .Next(10000000, 99999999)
                .ToString();

            var registerDto = new RegisterDTO
            {
                Username = username,
                Password = password,
                FullName = "Admin Dashboard Test User",
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
                .Where(u => u.Username.StartsWith("admindashboardtest"))
                .Select(u => u.UserId)
                .ToListAsync();

            var orderIdsByNote = await db.Orders
                .Where(o => o.Note != null && o.Note.StartsWith("admindashboardtest-"))
                .Select(o => o.OrderId)
                .ToListAsync();

            var orderIdsByUser = userIds.Any()
                ? await db.Orders
                    .Where(o => userIds.Contains(o.UserId))
                    .Select(o => o.OrderId)
                    .ToListAsync()
                : new List<int>();

            var orderIds = orderIdsByNote
                .Concat(orderIdsByUser)
                .Distinct()
                .ToList();

            if (orderIds.Any())
            {
                var payments = await db.PaymentTransactions
                    .Where(p => orderIds.Contains(p.OrderId))
                    .ToListAsync();

                db.PaymentTransactions.RemoveRange(payments);

                var orderItems = await db.OrderItems
                    .Where(oi => orderIds.Contains(oi.OrderId))
                    .ToListAsync();

                db.OrderItems.RemoveRange(orderItems);

                var orders = await db.Orders
                    .Where(o => orderIds.Contains(o.OrderId))
                    .ToListAsync();

                db.Orders.RemoveRange(orders);
            }

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

        private class TestOrderInfo
        {
            public int OrderId { get; set; }
            public int UserId { get; set; }
        }
    }
}