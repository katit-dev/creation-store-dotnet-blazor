using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using CreationStore.API.Data;
using CreationStore.API.DTOs.Admin.Payments;
using CreationStore.API.DTOs.Auth;
using CreationStore.API.DTOs.ResponseTypes;
using CreationStore.API.Helpers.Constant;
using CreationStore.API.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CreationStore.Tests
{
    public class AdminPaymentApiTests :
        IClassFixture<CustomWebApplicationFactory>,
        IAsyncLifetime
    {
        private readonly CustomWebApplicationFactory _factory;
        private readonly HttpClient _client;

        private static readonly JsonSerializerOptions JsonOptions = new(
            JsonSerializerDefaults.Web
        );

        public AdminPaymentApiTests(CustomWebApplicationFactory factory)
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
        // 1. GET ALL PAYMENTS - NO TOKEN
        // ============================================================
        [Fact]
        public async Task GetAllPayments_WithoutToken_Returns401()
        {
            SetBearerToken(null);

            var response = await _client.GetAsync("/api/admin/payments");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        // ============================================================
        // 2. GET ALL PAYMENTS - MEMBER TOKEN
        // ============================================================
        [Fact]
        public async Task GetAllPayments_WithMemberToken_Returns403()
        {
            var member = await CreateMemberUserAsync("getallmember");
            SetBearerToken(member.Token);

            var response = await _client.GetAsync("/api/admin/payments");

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        // ============================================================
        // 3. GET ALL PAYMENTS - ADMIN TOKEN
        // ============================================================
        [Fact]
        public async Task GetAllPayments_WithAdminToken_Returns200()
        {
            var admin = await CreateAdminUserAsync("getalladmin");
            var member = await CreateMemberUserAsync("getalltarget");
            await CreateOrderWithPaymentAsync(member.UserId, "getallpayment");

            SetBearerToken(admin.Token);

            var response = await _client.GetAsync("/api/admin/payments");

            var result = await response.Content
                .ReadFromJsonAsync<
                    ResponseTypeDTO<List<AdminPaymentResponseDTO>>
                >(JsonOptions);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(result);
            Assert.Equal(200, result!.StatusCode);
            Assert.NotNull(result.Content);
            Assert.NotEmpty(result.Content);
        }

        // ============================================================
        // 4. GET PAYMENT BY ID - NO TOKEN
        // ============================================================
        [Fact]
        public async Task GetPaymentById_WithoutToken_Returns401()
        {
            var member = await CreateMemberUserAsync("detailnotoken");
            var payment = await CreateOrderWithPaymentAsync(
                member.UserId,
                "detailnotokenpayment"
            );

            SetBearerToken(null);

            var response = await _client.GetAsync(
                $"/api/admin/payments/{payment.PaymentTransactionId}"
            );

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        // ============================================================
        // 5. GET PAYMENT BY ID - MEMBER TOKEN
        // ============================================================
        [Fact]
        public async Task GetPaymentById_WithMemberToken_Returns403()
        {
            var memberTokenUser = await CreateMemberUserAsync(
                "detailmembertoken"
            );
            var targetUser = await CreateMemberUserAsync("detailtarget");
            var payment = await CreateOrderWithPaymentAsync(
                targetUser.UserId,
                "detailmembertokenpayment"
            );

            SetBearerToken(memberTokenUser.Token);

            var response = await _client.GetAsync(
                $"/api/admin/payments/{payment.PaymentTransactionId}"
            );

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        // ============================================================
        // 6. GET PAYMENT BY ID - NOT FOUND
        // ============================================================
        [Fact]
        public async Task GetPaymentById_NotFound_Returns404()
        {
            var admin = await CreateAdminUserAsync("detailnotfound");
            SetBearerToken(admin.Token);

            var response = await _client.GetAsync(
                "/api/admin/payments/999999999"
            );

            var result = await response.Content
                .ReadFromJsonAsync<
                    ResponseTypeDTO<AdminPaymentResponseDTO>
                >(JsonOptions);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            Assert.NotNull(result);
            Assert.Equal(404, result!.StatusCode);
            Assert.Null(result.Content);
        }

        // ============================================================
        // 7. GET PAYMENT BY ID - SUCCESS
        // ============================================================
        [Fact]
        public async Task GetPaymentById_WithAdminToken_Returns200()
        {
            var admin = await CreateAdminUserAsync("detailsuccess");
            var member = await CreateMemberUserAsync("detailsuccesstarget");
            var payment = await CreateOrderWithPaymentAsync(
                member.UserId,
                "detailsuccesspayment"
            );

            SetBearerToken(admin.Token);

            var response = await _client.GetAsync(
                $"/api/admin/payments/{payment.PaymentTransactionId}"
            );

            var result = await response.Content
                .ReadFromJsonAsync<
                    ResponseTypeDTO<AdminPaymentResponseDTO>
                >(JsonOptions);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(result);
            Assert.Equal(200, result!.StatusCode);
            Assert.NotNull(result.Content);
            Assert.Equal(
                payment.PaymentTransactionId,
                result.Content!.PaymentTransactionId
            );
            Assert.Equal(payment.OrderId, result.Content.OrderId);
            Assert.Equal(CPaymentMethod.VnPay, result.Content.PaymentMethod);
            Assert.NotNull(result.Content.Order);
            Assert.NotNull(result.Content.User);
            Assert.Equal(member.UserId, result.Content.User!.UserId);
        }

        // ============================================================
        // 8. GET PAYMENTS BY ORDER ID - NO TOKEN
        // ============================================================
        [Fact]
        public async Task GetPaymentsByOrderId_WithoutToken_Returns401()
        {
            var member = await CreateMemberUserAsync("orderwithouttoken");
            var order = await CreateOrderWithoutPaymentAsync(
                member.UserId,
                "orderwithouttokenpayment"
            );

            SetBearerToken(null);

            var response = await _client.GetAsync(
                $"/api/admin/payments/order/{order.OrderId}"
            );

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        // ============================================================
        // 9. GET PAYMENTS BY ORDER ID - MEMBER TOKEN
        // ============================================================
        [Fact]
        public async Task GetPaymentsByOrderId_WithMemberToken_Returns403()
        {
            var memberTokenUser = await CreateMemberUserAsync(
                "ordermembertoken"
            );
            var targetUser = await CreateMemberUserAsync("ordertarget");
            var order = await CreateOrderWithoutPaymentAsync(
                targetUser.UserId,
                "ordermembertokenpayment"
            );

            SetBearerToken(memberTokenUser.Token);

            var response = await _client.GetAsync(
                $"/api/admin/payments/order/{order.OrderId}"
            );

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        // ============================================================
        // 10. GET PAYMENTS BY ORDER ID - ORDER NOT FOUND
        // ============================================================
        [Fact]
        public async Task GetPaymentsByOrderId_OrderNotFound_Returns404()
        {
            var admin = await CreateAdminUserAsync("ordernotfound");
            SetBearerToken(admin.Token);

            var response = await _client.GetAsync(
                "/api/admin/payments/order/999999999"
            );

            var result = await response.Content
                .ReadFromJsonAsync<
                    ResponseTypeDTO<List<AdminPaymentResponseDTO>>
                >(JsonOptions);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            Assert.NotNull(result);
            Assert.Equal(404, result!.StatusCode);
            Assert.Null(result.Content);
        }

        // ============================================================
        // 11. GET PAYMENTS BY ORDER ID - ORDER HAS PAYMENT
        // ============================================================
        [Fact]
        public async Task GetPaymentsByOrderId_WithPayment_Returns200()
        {
            var admin = await CreateAdminUserAsync("orderhaspaymentadmin");
            var member = await CreateMemberUserAsync("orderhaspaymentmember");
            var payment = await CreateOrderWithPaymentAsync(
                member.UserId,
                "orderhaspayment"
            );

            SetBearerToken(admin.Token);

            var response = await _client.GetAsync(
                $"/api/admin/payments/order/{payment.OrderId}"
            );

            var result = await response.Content
                .ReadFromJsonAsync<
                    ResponseTypeDTO<List<AdminPaymentResponseDTO>>
                >(JsonOptions);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(result);
            Assert.Equal(200, result!.StatusCode);
            Assert.NotNull(result.Content);
            Assert.NotEmpty(result.Content);
            Assert.Contains(
                result.Content,
                p => p.PaymentTransactionId == payment.PaymentTransactionId
            );
        }

        // ============================================================
        // 12. GET PAYMENTS BY ORDER ID - ORDER HAS NO PAYMENT
        // ============================================================
        [Fact]
        public async Task GetPaymentsByOrderId_WithoutPayment_Returns200EmptyContent()
        {
            var admin = await CreateAdminUserAsync("ordernopaymentadmin");
            var member = await CreateMemberUserAsync("ordernopaymentmember");
            var order = await CreateOrderWithoutPaymentAsync(
                member.UserId,
                "ordernopayment"
            );

            SetBearerToken(admin.Token);

            var response = await _client.GetAsync(
                $"/api/admin/payments/order/{order.OrderId}"
            );

            var result = await response.Content
                .ReadFromJsonAsync<
                    ResponseTypeDTO<List<AdminPaymentResponseDTO>>
                >(JsonOptions);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(result);
            Assert.Equal(200, result!.StatusCode);
            Assert.NotNull(result.Content);
            Assert.Empty(result.Content);
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

        private async Task<TestOrderInfo> CreateOrderWithoutPaymentAsync(
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
                Status = COrderStatus.PendingPayment,
                PaymentStatus = CPaymentStatus.Pending,
                OrderDate = DateTime.Now,
                Note = $"adminpaymenttest-{testName}"
            };

            db.Orders.Add(order);
            await db.SaveChangesAsync();

            return new TestOrderInfo
            {
                OrderId = order.OrderId,
                UserId = userId
            };
        }

        private async Task<TestPaymentInfo> CreateOrderWithPaymentAsync(
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
                Status = COrderStatus.PendingPayment,
                PaymentStatus = CPaymentStatus.Pending,
                OrderDate = DateTime.Now,
                Note = $"adminpaymenttest-{testName}"
            };

            db.Orders.Add(order);
            await db.SaveChangesAsync();

            var payment = new PaymentTransaction
            {
                OrderId = order.OrderId,
                PaymentMethod = CPaymentMethod.VnPay,
                Amount = order.TotalAmount,
                TransactionStatus = CPaymentTransactionStatus.Pending,
                VnpTxnRef = $"ADMINPAYTEST{Guid.NewGuid():N}",
                CreatedAt = DateTime.Now
            };

            db.PaymentTransactions.Add(payment);
            await db.SaveChangesAsync();

            return new TestPaymentInfo
            {
                OrderId = order.OrderId,
                PaymentTransactionId = payment.PaymentTransactionId
            };
        }

        private async Task<string> RegisterUserAsync(string testName)
        {
            var suffix = Guid.NewGuid()
                .ToString("N")
                .Substring(0, 10);

            var username = $"adminpaymenttest{testName}{suffix}";
            var password = "123456";
            var phone = "06" + Random.Shared
                .Next(10000000, 99999999)
                .ToString();

            var registerDto = new RegisterDTO
            {
                Username = username,
                Password = password,
                FullName = "Admin Payment Test User",
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
                .Where(u => u.Username.StartsWith("adminpaymenttest"))
                .Select(u => u.UserId)
                .ToListAsync();

            var orderIdsByNote = await db.Orders
                .Where(o => o.Note != null && o.Note.StartsWith("adminpaymenttest-"))
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

        private class TestPaymentInfo
        {
            public int OrderId { get; set; }
            public int PaymentTransactionId { get; set; }
        }
    }
}