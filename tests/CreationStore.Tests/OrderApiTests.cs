using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using CreationStore.API.Data;
using CreationStore.API.DTOs.Auth;
using CreationStore.API.DTOs.Cart;
using CreationStore.API.DTOs.Order;
using CreationStore.API.DTOs.ResponseTypes;
using CreationStore.API.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CreationStore.Tests
{
    public class OrderApiTests :
        IClassFixture<CustomWebApplicationFactory>,
        IAsyncLifetime
    {
        private readonly CustomWebApplicationFactory _factory;
        private readonly HttpClient _client;

        private const string Password = "123456";

        public OrderApiTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        public async Task InitializeAsync()
        {
            await CleanupOrderTestDataAsync();
        }

        public async Task DisposeAsync()
        {
            await CleanupOrderTestDataAsync();
        }

        [Fact]
        public async Task Checkout_WithoutToken_Returns401()
        {
            _client.DefaultRequestHeaders.Authorization = null;

            var response = await _client.PostAsJsonAsync(
                "/api/orders/checkout",
                new CheckoutOrderDTO
                {
                    Note = "Checkout without token"
                }
            );

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task Checkout_EmptyCart_Returns400()
        {
            var token = await RegisterAndLoginAsync("empty_cart");

            SetBearerToken(token);

            var response = await _client.PostAsJsonAsync(
                "/api/orders/checkout",
                new CheckoutOrderDTO
                {
                    Note = "Checkout empty cart"
                }
            );

            var responseText = await response.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Contains("Cart is empty", responseText);
        }

        [Fact]
        public async Task Checkout_WithCartItems_Returns201()
        {
            var token = await RegisterAndLoginAsync("checkout_ok");
            var product = await CreateActiveProductAsync("checkout_ok", 100000m);

            SetBearerToken(token);

            await AddProductToCartAsync(product.ProductId, 2);

            var response = await _client.PostAsJsonAsync(
                "/api/orders/checkout",
                new CheckoutOrderDTO
                {
                    Note = "Test checkout order"
                }
            );

            var responseText = await response.Content.ReadAsStringAsync();

            Assert.True(
                response.StatusCode == HttpStatusCode.Created,
                $"Expected 201 Created but got {(int)response.StatusCode} {response.StatusCode}. Body: {responseText}"
            );

            var result = await response.Content
                .ReadFromJsonAsync<ResponseTypeDTO<OrderResponseDTO>>();

            Assert.NotNull(result);
            Assert.Equal(201, result!.StatusCode);
            Assert.Equal("Checkout successfully", result.Message);

            Assert.NotNull(result.Content);
            Assert.True(result.Content!.OrderId > 0);
            Assert.Equal("PendingPayment", result.Content.Status);
            Assert.Equal("Pending", result.Content.PaymentStatus);
            Assert.Equal(200000m, result.Content.TotalAmount);
            Assert.Equal("Test checkout order", result.Content.Note);

            Assert.Single(result.Content.Items);

            var item = result.Content.Items.First();

            Assert.Equal(product.ProductId, item.ProductId);
            Assert.Equal(product.ProductName, item.ProductName);
            Assert.Equal(2, item.Quantity);
            Assert.Equal(100000m, item.UnitPrice);
            Assert.Equal(200000m, item.SubTotal);
        }

        [Fact]
        public async Task Checkout_CreatesOrderItemsFromCartItems()
        {
            var token = await RegisterAndLoginAsync("order_items");
            var product = await CreateActiveProductAsync("order_items", 50000m);

            SetBearerToken(token);

            await AddProductToCartAsync(product.ProductId, 3);

            var checkoutResponse = await _client.PostAsJsonAsync(
                "/api/orders/checkout",
                new CheckoutOrderDTO
                {
                    Note = "Check order items"
                }
            );

            var checkoutResult = await checkoutResponse.Content
                .ReadFromJsonAsync<ResponseTypeDTO<OrderResponseDTO>>();

            Assert.Equal(HttpStatusCode.Created, checkoutResponse.StatusCode);
            Assert.NotNull(checkoutResult?.Content);

            var orderId = checkoutResult!.Content!.OrderId;

            using var scope = _factory.Services.CreateScope();

            var db = scope.ServiceProvider
                .GetRequiredService<CreationStoreDbContext>();

            var order = await db.Orders
                .Include(o => o.OrderItems)
                .FirstAsync(o => o.OrderId == orderId);

            Assert.Single(order.OrderItems);

            var orderItem = order.OrderItems.First();

            Assert.Equal(product.ProductId, orderItem.ProductId);
            Assert.Equal(product.ProductName, orderItem.ProductName);
            Assert.Equal(3, orderItem.Quantity);
            Assert.Equal(50000m, orderItem.PriceAtTime);
        }

        [Fact]
        public async Task Checkout_ChangesCartStatusToCheckedOut()
        {
            var token = await RegisterAndLoginAsync("cart_checked");
            var product = await CreateActiveProductAsync("cart_checked", 100000m);

            SetBearerToken(token);

            var addCartResult = await AddProductToCartAsync(product.ProductId, 2);

            var cartId = addCartResult.CartId;

            var checkoutResponse = await _client.PostAsJsonAsync(
                "/api/orders/checkout",
                new CheckoutOrderDTO
                {
                    Note = "Check cart status"
                }
            );

            Assert.Equal(HttpStatusCode.Created, checkoutResponse.StatusCode);

            using var scope = _factory.Services.CreateScope();

            var db = scope.ServiceProvider
                .GetRequiredService<CreationStoreDbContext>();

            var cart = await db.Carts
                .FirstAsync(c => c.CartId == cartId);

            Assert.Equal("CheckedOut", cart.Status);
        }

        [Fact]
        public async Task GetMyOrders_Returns200()
        {
            var token = await RegisterAndLoginAsync("get_orders");

            SetBearerToken(token);

            var response = await _client.GetAsync("/api/orders");

            var responseText = await response.Content.ReadAsStringAsync();

            Assert.True(
                response.StatusCode == HttpStatusCode.OK,
                $"Expected 200 OK but got {(int)response.StatusCode} {response.StatusCode}. Body: {responseText}"
            );

            var result = await response.Content
                .ReadFromJsonAsync<ResponseTypeDTO<List<OrderResponseDTO>>>();

            Assert.NotNull(result);
            Assert.Equal(200, result!.StatusCode);
            Assert.Equal("Get orders successfully", result.Message);
            Assert.NotNull(result.Content);
        }

        [Fact]
        public async Task GetMyOrderById_NotFound_Returns404()
        {
            var token = await RegisterAndLoginAsync("order_notfound");

            SetBearerToken(token);

            var response = await _client.GetAsync("/api/orders/999999999");

            var responseText = await response.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            Assert.Contains("Order not found", responseText);
        }

        [Fact]
        public async Task CancelOrder_PendingPayment_ReturnsCancelled()
        {
            var token = await RegisterAndLoginAsync("cancel_ok");
            var product = await CreateActiveProductAsync("cancel_ok", 100000m);

            SetBearerToken(token);

            await AddProductToCartAsync(product.ProductId, 2);

            var checkoutResponse = await _client.PostAsJsonAsync(
                "/api/orders/checkout",
                new CheckoutOrderDTO
                {
                    Note = "Order to cancel"
                }
            );

            var checkoutResult = await checkoutResponse.Content
                .ReadFromJsonAsync<ResponseTypeDTO<OrderResponseDTO>>();

            Assert.Equal(HttpStatusCode.Created, checkoutResponse.StatusCode);
            Assert.NotNull(checkoutResult?.Content);

            var orderId = checkoutResult!.Content!.OrderId;

            var cancelResponse = await _client.PutAsJsonAsync(
                $"/api/orders/{orderId}/cancel",
                new CancelOrderDTO
                {
                    CancelReason = "User changed mind"
                }
            );

            var responseText = await cancelResponse.Content.ReadAsStringAsync();

            Assert.True(
                cancelResponse.StatusCode == HttpStatusCode.OK,
                $"Expected 200 OK but got {(int)cancelResponse.StatusCode} {cancelResponse.StatusCode}. Body: {responseText}"
            );

            var result = await cancelResponse.Content
                .ReadFromJsonAsync<ResponseTypeDTO<OrderResponseDTO>>();

            Assert.NotNull(result);
            Assert.Equal(200, result!.StatusCode);
            Assert.Equal("Cancel order successfully", result.Message);

            Assert.NotNull(result.Content);
            Assert.Equal(orderId, result.Content!.OrderId);
            Assert.Equal("Cancelled", result.Content.Status);
            Assert.Equal("Cancelled", result.Content.PaymentStatus);
            Assert.Equal("User changed mind", result.Content.CancelReason);
            Assert.NotNull(result.Content.CancelledAt);
        }

        private async Task<string> RegisterAndLoginAsync(string scenario)
        {
            var username = CreateUsername(scenario);

            var registerDto = new RegisterDTO
            {
                Username = username,
                Password = Password,
                FullName = "Order Test User",
                Email = $"{username}@gmail.com",
                Phone = null
            };

            var registerResponse = await _client.PostAsJsonAsync(
                "/api/auth/register",
                registerDto
            );

            var registerBody = await registerResponse.Content.ReadAsStringAsync();

            Assert.True(
                registerResponse.StatusCode == HttpStatusCode.Created,
                $"Expected register 201 Created but got {(int)registerResponse.StatusCode} {registerResponse.StatusCode}. Body: {registerBody}"
            );

            var loginDto = new LoginDTO
            {
                LoginIdentifier = username,
                Password = Password
            };

            var loginResponse = await _client.PostAsJsonAsync(
                "/api/auth/login",
                loginDto
            );

            var loginBody = await loginResponse.Content.ReadAsStringAsync();

            Assert.True(
                loginResponse.StatusCode == HttpStatusCode.OK,
                $"Expected login 200 OK but got {(int)loginResponse.StatusCode} {loginResponse.StatusCode}. Body: {loginBody}"
            );

            var loginResult = await loginResponse.Content
                .ReadFromJsonAsync<ResponseTypeDTO<LoginResponseDTO>>();

            Assert.NotNull(loginResult);
            Assert.NotNull(loginResult!.Content);
            Assert.False(string.IsNullOrWhiteSpace(loginResult.Content!.Token));

            return loginResult.Content.Token;
        }

        private async Task<TestProductData> CreateActiveProductAsync(
            string scenario,
            decimal price
        )
        {
            using var scope = _factory.Services.CreateScope();

            var db = scope.ServiceProvider
                .GetRequiredService<CreationStoreDbContext>();

            var shortScenario = scenario.Length > 12
                ? scenario.Substring(0, 12)
                : scenario;

            var suffix = Guid.NewGuid()
                .ToString("N")
                .Substring(0, 8);

            var category = new Category
            {
                CategoryName = $"ordertest_category_{shortScenario}_{suffix}",
                Description = "Category for order api test",
                IsActive = true,
                CreatedAt = DateTime.Now
            };

            db.Categories.Add(category);

            await db.SaveChangesAsync();

            var product = new Product
            {
                ProductName = $"ordertest_product_{shortScenario}_{suffix}",
                Description = "Product for order api test",
                Price = price,
                IsActive = true,
                ImageUrl = null,
                ValidityDays = 30,
                CategoryId = category.CategoryId,
                CreatedAt = DateTime.Now,
                UpdatedAt = null
            };

            db.Products.Add(product);

            await db.SaveChangesAsync();

            return new TestProductData
            {
                ProductId = product.ProductId,
                ProductName = product.ProductName,
                Price = product.Price,
                CategoryId = category.CategoryId
            };
        }

        private async Task<CartResponseDTO> AddProductToCartAsync(
            int productId,
            int quantity
        )
        {
            var response = await _client.PostAsJsonAsync(
                "/api/cart/items",
                new AddCartItemDTO
                {
                    ProductId = productId,
                    Quantity = quantity
                }
            );

            var responseText = await response.Content.ReadAsStringAsync();

            Assert.True(
                response.StatusCode == HttpStatusCode.OK,
                $"Expected add cart 200 OK but got {(int)response.StatusCode} {response.StatusCode}. Body: {responseText}"
            );

            var result = await response.Content
                .ReadFromJsonAsync<ResponseTypeDTO<CartResponseDTO>>();

            Assert.NotNull(result);
            Assert.NotNull(result!.Content);

            return result.Content!;
        }

        private void SetBearerToken(string token)
        {
            _client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);
        }

        private static string CreateUsername(string scenario)
        {
            var shortScenario = scenario.Length > 12
                ? scenario.Substring(0, 12)
                : scenario;

            var suffix = Guid.NewGuid()
                .ToString("N")
                .Substring(0, 8);

            return $"ordertest_{shortScenario}_{suffix}";
        }

        private async Task CleanupOrderTestDataAsync()
        {
            using var scope = _factory.Services.CreateScope();

            var db = scope.ServiceProvider
                .GetRequiredService<CreationStoreDbContext>();

            var users = await db.Users
                .Where(u => u.Username.StartsWith("ordertest_"))
                .ToListAsync();

            var userIds = users
                .Select(u => u.UserId)
                .ToList();

            var carts = await db.Carts
                .Where(c => userIds.Contains(c.UserId))
                .ToListAsync();

            var cartIds = carts
                .Select(c => c.CartId)
                .ToList();

            var cartItems = await db.CartItems
                .Where(ci => cartIds.Contains(ci.CartId))
                .ToListAsync();

            var orders = await db.Orders
                .Where(o => userIds.Contains(o.UserId))
                .ToListAsync();

            var orderIds = orders
                .Select(o => o.OrderId)
                .ToList();

            var orderItems = await db.OrderItems
                .Where(oi => orderIds.Contains(oi.OrderId))
                .ToListAsync();

            if (cartItems.Any())
            {
                db.CartItems.RemoveRange(cartItems);
                await db.SaveChangesAsync();
            }

            if (orderItems.Any())
            {
                db.OrderItems.RemoveRange(orderItems);
                await db.SaveChangesAsync();
            }

            if (carts.Any())
            {
                db.Carts.RemoveRange(carts);
                await db.SaveChangesAsync();
            }

            if (orders.Any())
            {
                db.Orders.RemoveRange(orders);
                await db.SaveChangesAsync();
            }

            var userRoles = await db.UserRoles
                .Where(ur => userIds.Contains(ur.UserId))
                .ToListAsync();

            if (userRoles.Any())
            {
                db.UserRoles.RemoveRange(userRoles);
                await db.SaveChangesAsync();
            }

            if (users.Any())
            {
                db.Users.RemoveRange(users);
                await db.SaveChangesAsync();
            }

            var products = await db.Products
                .Where(p => p.ProductName.StartsWith("ordertest_product_"))
                .ToListAsync();

            var productIds = products
                .Select(p => p.ProductId)
                .ToList();

            var productCartItems = await db.CartItems
                .Where(ci => productIds.Contains(ci.ProductId))
                .ToListAsync();

            var productOrderItems = await db.OrderItems
                .Where(oi => productIds.Contains(oi.ProductId))
                .ToListAsync();

            if (productCartItems.Any())
            {
                db.CartItems.RemoveRange(productCartItems);
                await db.SaveChangesAsync();
            }

            if (productOrderItems.Any())
            {
                db.OrderItems.RemoveRange(productOrderItems);
                await db.SaveChangesAsync();
            }

            if (products.Any())
            {
                db.Products.RemoveRange(products);
                await db.SaveChangesAsync();
            }

            var categories = await db.Categories
                .Where(c => c.CategoryName.StartsWith("ordertest_category_"))
                .ToListAsync();

            if (categories.Any())
            {
                db.Categories.RemoveRange(categories);
                await db.SaveChangesAsync();
            }
        }

        private class TestProductData
        {
            public int ProductId { get; set; }

            public string ProductName { get; set; } = string.Empty;

            public decimal Price { get; set; }

            public int CategoryId { get; set; }
        }
    }
}