using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using CreationStore.API.Data;
using CreationStore.API.DTOs.Admin.Orders;
using CreationStore.API.DTOs.Auth;
using CreationStore.API.DTOs.Order;
using CreationStore.API.DTOs.ResponseTypes;
using CreationStore.API.Helpers.Constant;
using CreationStore.API.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CreationStore.Tests
{
    public class AdminOrderApiTests :
        IClassFixture<CustomWebApplicationFactory>,
        IAsyncLifetime
    {
        private readonly CustomWebApplicationFactory _factory;
        private readonly HttpClient _client;

        private static readonly JsonSerializerOptions JsonOptions = new(
            JsonSerializerDefaults.Web
        );

        public AdminOrderApiTests(CustomWebApplicationFactory factory)
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

        [Fact]
        public async Task GetAllOrders_WithoutToken_Returns401()
        {
            SetBearerToken(null);

            var response = await _client.GetAsync("/api/admin/orders");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task GetAllOrders_WithMemberToken_Returns403()
        {
            var memberToken = await CreateMemberTokenAsync("getallmember");
            SetBearerToken(memberToken);

            var response = await _client.GetAsync("/api/admin/orders");

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task GetAllOrders_WithAdminToken_Returns200()
        {
            var adminToken = await CreateAdminTokenAsync("getalladmin");
            SetBearerToken(adminToken);

            var response = await _client.GetAsync("/api/admin/orders");

            var result = await response.Content
                .ReadFromJsonAsync<
                    ResponseTypeDTO<List<AdminOrderResponseDTO>>
                >(JsonOptions);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(result);
            Assert.Equal(200, result!.StatusCode);
            Assert.NotNull(result.Content);
        }

        [Fact]
        public async Task GetOrderById_NotFound_Returns404()
        {
            var adminToken = await CreateAdminTokenAsync("detailnotfound");
            SetBearerToken(adminToken);

            var response = await _client.GetAsync(
                "/api/admin/orders/999999999"
            );

            var result = await response.Content
                .ReadFromJsonAsync<ResponseTypeDTO<AdminOrderResponseDTO>>(
                    JsonOptions
                );

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            Assert.NotNull(result);
            Assert.Equal(404, result!.StatusCode);
            Assert.Null(result.Content);
        }

        [Fact]
        public async Task GetOrderById_WithAdminToken_Returns200()
        {
            var adminToken = await CreateAdminTokenAsync("detailsuccess");
            var memberUsername = await RegisterUserAsync("ordermemberdetail");
            var memberUserId = await GetUserIdByUsernameAsync(memberUsername);

            var orderId = await CreateOrderDirectlyAsync(
                memberUserId,
                COrderStatus.PendingPayment,
                CPaymentStatus.Pending
            );

            SetBearerToken(adminToken);

            var response = await _client.GetAsync(
                $"/api/admin/orders/{orderId}"
            );

            var result = await response.Content
                .ReadFromJsonAsync<ResponseTypeDTO<AdminOrderResponseDTO>>(
                    JsonOptions
                );

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(result);
            Assert.Equal(200, result!.StatusCode);
            Assert.NotNull(result.Content);
            Assert.Equal(orderId, result.Content!.OrderId);
            Assert.NotNull(result.Content.User);
            Assert.NotEmpty(result.Content.Items);
        }

        [Fact]
        public async Task CompleteOrder_NotFound_Returns404()
        {
            var adminToken = await CreateAdminTokenAsync("completenotfound");
            SetBearerToken(adminToken);

            var response = await _client.PutAsync(
                "/api/admin/orders/999999999/complete",
                null
            );

            var result = await response.Content
                .ReadFromJsonAsync<ResponseTypeDTO<AdminOrderResponseDTO>>(
                    JsonOptions
                );

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            Assert.NotNull(result);
            Assert.Equal(404, result!.StatusCode);
            Assert.Null(result.Content);
        }

        [Fact]
        public async Task CompleteOrder_PendingPaymentOrder_Returns400()
        {
            var adminToken = await CreateAdminTokenAsync("completepending");
            var memberUsername = await RegisterUserAsync("ordermemberpending");
            var memberUserId = await GetUserIdByUsernameAsync(memberUsername);

            var orderId = await CreateOrderDirectlyAsync(
                memberUserId,
                COrderStatus.PendingPayment,
                CPaymentStatus.Pending
            );

            SetBearerToken(adminToken);

            var response = await _client.PutAsync(
                $"/api/admin/orders/{orderId}/complete",
                null
            );

            var result = await response.Content
                .ReadFromJsonAsync<ResponseTypeDTO<AdminOrderResponseDTO>>(
                    JsonOptions
                );

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.NotNull(result);
            Assert.Equal(400, result!.StatusCode);
            Assert.Null(result.Content);
        }

        [Fact]
        public async Task CompleteOrder_PaidOrder_Returns200()
        {
            var adminToken = await CreateAdminTokenAsync("completepaid");
            var memberUsername = await RegisterUserAsync("ordermemberpaid");
            var memberUserId = await GetUserIdByUsernameAsync(memberUsername);

            var orderId = await CreateOrderDirectlyAsync(
                memberUserId,
                COrderStatus.Paid,
                CPaymentStatus.Succeeded,
                createSucceededPayment: true
            );

            SetBearerToken(adminToken);

            var response = await _client.PutAsync(
                $"/api/admin/orders/{orderId}/complete",
                null
            );

            var result = await response.Content
                .ReadFromJsonAsync<ResponseTypeDTO<AdminOrderResponseDTO>>(
                    JsonOptions
                );

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(result);
            Assert.Equal(200, result!.StatusCode);
            Assert.NotNull(result.Content);
            Assert.Equal(COrderStatus.Completed, result.Content!.Status);
            Assert.Equal(CPaymentStatus.Succeeded, result.Content.PaymentStatus);
        }

        [Fact]
        public async Task CancelOrder_PendingPaymentOrder_Returns200()
        {
            var adminToken = await CreateAdminTokenAsync("cancelpending");
            var memberUsername = await RegisterUserAsync("ordermembercancel");
            var memberUserId = await GetUserIdByUsernameAsync(memberUsername);

            var orderId = await CreateOrderDirectlyAsync(
                memberUserId,
                COrderStatus.PendingPayment,
                CPaymentStatus.Pending
            );

            SetBearerToken(adminToken);

            var response = await _client.PutAsJsonAsync(
                $"/api/admin/orders/{orderId}/cancel",
                new CancelOrderDTO
                {
                    CancelReason = "Cancelled by admin order test"
                }
            );

            var result = await response.Content
                .ReadFromJsonAsync<ResponseTypeDTO<AdminOrderResponseDTO>>(
                    JsonOptions
                );

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(result);
            Assert.Equal(200, result!.StatusCode);
            Assert.NotNull(result.Content);
            Assert.Equal(COrderStatus.Cancelled, result.Content!.Status);
            Assert.Equal(CPaymentStatus.Cancelled, result.Content.PaymentStatus);
            Assert.False(string.IsNullOrWhiteSpace(result.Content.CancelReason));
        }

        [Fact]
        public async Task CancelOrder_PaidOrder_Returns400()
        {
            var adminToken = await CreateAdminTokenAsync("cancelpaid");
            var memberUsername = await RegisterUserAsync("ordermembercancelpaid");
            var memberUserId = await GetUserIdByUsernameAsync(memberUsername);

            var orderId = await CreateOrderDirectlyAsync(
                memberUserId,
                COrderStatus.Paid,
                CPaymentStatus.Succeeded,
                createSucceededPayment: true
            );

            SetBearerToken(adminToken);

            var response = await _client.PutAsJsonAsync(
                $"/api/admin/orders/{orderId}/cancel",
                new CancelOrderDTO
                {
                    CancelReason = "Try cancel paid order"
                }
            );

            var result = await response.Content
                .ReadFromJsonAsync<ResponseTypeDTO<AdminOrderResponseDTO>>(
                    JsonOptions
                );

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.NotNull(result);
            Assert.Equal(400, result!.StatusCode);
            Assert.Null(result.Content);
        }

        private async Task<string> CreateMemberTokenAsync(string testName)
        {
            var username = await RegisterUserAsync(testName);
            return await LoginAsync(username);
        }

        private async Task<string> CreateAdminTokenAsync(string testName)
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

            return await LoginAsync(username);
        }

        private async Task<string> RegisterUserAsync(string testName)
        {
            var suffix = Guid.NewGuid()
                .ToString("N")
                .Substring(0, 10);

            var username = $"adminordertest{testName}{suffix}";
            var password = "123456";
            var phone = "06" + Random.Shared
                .Next(10000000, 99999999)
                .ToString();

            var registerDto = new RegisterDTO
            {
                Username = username,
                Password = password,
                FullName = "Admin Order Test User",
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

        private async Task<int> CreateOrderDirectlyAsync(
            int userId,
            string orderStatus,
            string paymentStatus,
            bool createSucceededPayment = false
        )
        {
            using var scope = _factory.Services.CreateScope();

            var db = scope.ServiceProvider
                .GetRequiredService<CreationStoreDbContext>();

            var category = new Category
            {
                CategoryName = CreateUniqueName("Category"),
                Description = "Category created by admin order test",
                IsActive = true,
                CreatedAt = DateTime.Now
            };

            db.Categories.Add(category);
            await db.SaveChangesAsync();

            var product = new Product
            {
                ProductName = CreateUniqueName("Product"),
                Description = "Product created by admin order test",
                Price = 250000,
                ImageUrl = "https://example.com/product.jpg",
                ValidityDays = 30,
                CategoryId = category.CategoryId,
                IsActive = true,
                CreatedAt = DateTime.Now
            };

            db.Products.Add(product);
            await db.SaveChangesAsync();

            var order = new Order
            {
                UserId = userId,
                TotalAmount = 250000,
                Status = orderStatus,
                PaymentStatus = paymentStatus,
                OrderDate = DateTime.Now,
                Note = "Order created by admin order test"
            };

            db.Orders.Add(order);
            await db.SaveChangesAsync();

            var orderItem = new OrderItem
            {
                OrderId = order.OrderId,
                ProductId = product.ProductId,
                ProductName = product.ProductName,
                Quantity = 1,
                PriceAtTime = 250000
            };

            db.OrderItems.Add(orderItem);

            if (createSucceededPayment)
            {
                var paymentTransaction = new PaymentTransaction
                {
                    OrderId = order.OrderId,
                    PaymentMethod = CPaymentMethod.VnPay,
                    Amount = 250000,
                    TransactionStatus = CPaymentTransactionStatus.Succeeded,
                    VnpTxnRef = CreateUniqueVnpTxnRef(order.OrderId),
                    VnpTransactionNo = "12345678",
                    VnpResponseCode = "00",
                    VnpTransactionStatus = "00",
                    VnpBankCode = "NCB",
                    VnpPayDate = DateTime.Now.ToString("yyyyMMddHHmmss"),
                    CreatedAt = DateTime.Now,
                    PaidAt = DateTime.Now,
                    RawResponse = "Admin order test payment"
                };

                db.PaymentTransactions.Add(paymentTransaction);
            }

            await db.SaveChangesAsync();

            return order.OrderId;
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

        private static string CreateUniqueName(string prefix)
        {
            var suffix = Guid.NewGuid()
                .ToString("N")
                .Substring(0, 8);

            return $"AdminOrderTest {prefix} {suffix}";
        }

        private static string CreateUniqueVnpTxnRef(int orderId)
        {
            var suffix = Guid.NewGuid()
                .ToString("N")
                .Substring(0, 8);

            return $"ADMORDER{orderId}{suffix}";
        }

        private async Task CleanupTestDataAsync()
        {
            using var scope = _factory.Services.CreateScope();

            var db = scope.ServiceProvider
                .GetRequiredService<CreationStoreDbContext>();

            var orderIds = await db.Orders
                .Where(o => o.Note != null &&
                            o.Note.StartsWith("Order created by admin order test"))
                .Select(o => o.OrderId)
                .ToListAsync();

            if (orderIds.Any())
            {
                var paymentTransactions = await db.PaymentTransactions
                    .Where(p => orderIds.Contains(p.OrderId))
                    .ToListAsync();

                db.PaymentTransactions.RemoveRange(paymentTransactions);

                var orderItems = await db.OrderItems
                    .Where(oi => orderIds.Contains(oi.OrderId))
                    .ToListAsync();

                db.OrderItems.RemoveRange(orderItems);

                var orders = await db.Orders
                    .Where(o => orderIds.Contains(o.OrderId))
                    .ToListAsync();

                db.Orders.RemoveRange(orders);
            }

            var products = await db.Products
                .Where(p => p.ProductName.StartsWith("AdminOrderTest"))
                .ToListAsync();

            db.Products.RemoveRange(products);

            var categories = await db.Categories
                .Where(c => c.CategoryName.StartsWith("AdminOrderTest"))
                .ToListAsync();

            db.Categories.RemoveRange(categories);

            var userIds = await db.Users
                .Where(u => u.Username.StartsWith("adminordertest"))
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
    }
}