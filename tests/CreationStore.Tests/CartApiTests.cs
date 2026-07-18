using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using CreationStore.API.Data;
using CreationStore.API.DTOs.Auth;
using CreationStore.API.DTOs.Cart;
using CreationStore.API.DTOs.ResponseTypes;
using CreationStore.API.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CreationStore.Tests
{
    public class CartApiTests :
        IClassFixture<CustomWebApplicationFactory>,
        IAsyncLifetime
    {
        private readonly CustomWebApplicationFactory _factory;
        private readonly HttpClient _client;

        private const string Password = "123456";

        public CartApiTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        public async Task InitializeAsync()
        {
            await CleanupCartTestDataAsync();
        }

        public async Task DisposeAsync()
        {
            await CleanupCartTestDataAsync();
        }

        [Fact]
        public async Task GetCart_WithoutToken_Returns401()
        {
            var response = await _client.GetAsync("/api/cart");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task GetCart_WithValidToken_Returns200()
        {
            var token = await RegisterAndLoginAsync("getcart_ok");

            SetBearerToken(token);

            var response = await _client.GetAsync("/api/cart");

            var responseText = await response.Content.ReadAsStringAsync();

            Assert.True(
                response.StatusCode == HttpStatusCode.OK,
                $"Expected 200 OK but got {(int)response.StatusCode} {response.StatusCode}. Body: {responseText}"
            );

            var result = await response.Content
                .ReadFromJsonAsync<ResponseTypeDTO<CartResponseDTO>>();

            Assert.NotNull(result);
            Assert.Equal(200, result!.StatusCode);
            Assert.Equal("Get cart successfully", result.Message);
            Assert.NotNull(result.Content);
            Assert.Equal("Active", result.Content!.Status);
        }

        [Fact]
        public async Task GetCart_FirstTime_CreatesEmptyCart()
        {
            var token = await RegisterAndLoginAsync("getcart_empty");

            SetBearerToken(token);

            var response = await _client.GetAsync("/api/cart");

            var result = await response.Content
                .ReadFromJsonAsync<ResponseTypeDTO<CartResponseDTO>>();

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            Assert.NotNull(result);
            Assert.NotNull(result!.Content);

            Assert.True(result.Content!.CartId > 0);
            Assert.Equal("Active", result.Content.Status);
            Assert.Empty(result.Content.Items);
            Assert.Equal(0, result.Content.TotalItems);
            Assert.Equal(0, result.Content.TotalAmount);
        }

        [Fact]
        public async Task AddItem_ValidProduct_ReturnsCartWithItem()
        {
            var token = await RegisterAndLoginAsync("additem_ok");
            var product = await CreateActiveProductAsync("additem_ok", 100000);

            SetBearerToken(token);

            var response = await _client.PostAsJsonAsync(
                "/api/cart/items",
                new AddCartItemDTO
                {
                    ProductId = product.ProductId,
                    Quantity = 2
                }
            );

            var responseText = await response.Content.ReadAsStringAsync();

            Assert.True(
                response.StatusCode == HttpStatusCode.OK,
                $"Expected 200 OK but got {(int)response.StatusCode} {response.StatusCode}. Body: {responseText}"
            );

            var result = await response.Content
                .ReadFromJsonAsync<ResponseTypeDTO<CartResponseDTO>>();

            Assert.NotNull(result);
            Assert.Equal(200, result!.StatusCode);
            Assert.Equal("Add item to cart successfully", result.Message);

            Assert.NotNull(result.Content);
            Assert.Single(result.Content!.Items);

            var item = result.Content.Items.First();

            Assert.Equal(product.ProductId, item.ProductId);
            Assert.Equal(product.ProductName, item.ProductName);
            Assert.Equal(100000, item.UnitPrice);
            Assert.Equal(2, item.Quantity);
            Assert.Equal(200000, item.SubTotal);

            Assert.Equal(2, result.Content.TotalItems);
            Assert.Equal(200000, result.Content.TotalAmount);
        }

        [Fact]
        public async Task AddSameProduct_IncreasesQuantity()
        {
            var token = await RegisterAndLoginAsync("addsame");
            var product = await CreateActiveProductAsync("addsame", 50000);

            SetBearerToken(token);

            var firstResponse = await _client.PostAsJsonAsync(
                "/api/cart/items",
                new AddCartItemDTO
                {
                    ProductId = product.ProductId,
                    Quantity = 2
                }
            );

            Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);

            var secondResponse = await _client.PostAsJsonAsync(
                "/api/cart/items",
                new AddCartItemDTO
                {
                    ProductId = product.ProductId,
                    Quantity = 3
                }
            );

            var responseText = await secondResponse.Content.ReadAsStringAsync();

            Assert.True(
                secondResponse.StatusCode == HttpStatusCode.OK,
                $"Expected 200 OK but got {(int)secondResponse.StatusCode} {secondResponse.StatusCode}. Body: {responseText}"
            );

            var result = await secondResponse.Content
                .ReadFromJsonAsync<ResponseTypeDTO<CartResponseDTO>>();

            Assert.NotNull(result);
            Assert.NotNull(result!.Content);

            Assert.Single(result.Content!.Items);

            var item = result.Content.Items.First();

            Assert.Equal(product.ProductId, item.ProductId);
            Assert.Equal(5, item.Quantity);
            Assert.Equal(250000, item.SubTotal);

            Assert.Equal(5, result.Content.TotalItems);
            Assert.Equal(250000, result.Content.TotalAmount);
        }

        [Fact]
        public async Task AddItem_ProductNotFound_Returns404()
        {
            var token = await RegisterAndLoginAsync("product_not_found");

            SetBearerToken(token);

            var response = await _client.PostAsJsonAsync(
                "/api/cart/items",
                new AddCartItemDTO
                {
                    ProductId = 999999999,
                    Quantity = 1
                }
            );

            var responseText = await response.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            Assert.Contains("Product not found", responseText);
        }

        [Fact]
        public async Task UpdateQuantity_Valid_Returns200()
        {
            var token = await RegisterAndLoginAsync("update_ok");
            var product = await CreateActiveProductAsync("update_ok", 100000);

            SetBearerToken(token);

            var addResponse = await _client.PostAsJsonAsync(
                "/api/cart/items",
                new AddCartItemDTO
                {
                    ProductId = product.ProductId,
                    Quantity = 2
                }
            );

            var addResult = await addResponse.Content
                .ReadFromJsonAsync<ResponseTypeDTO<CartResponseDTO>>();

            Assert.Equal(HttpStatusCode.OK, addResponse.StatusCode);
            Assert.NotNull(addResult?.Content);

            var cartItemId = addResult!.Content!.Items.First().CartItemId;

            var updateResponse = await _client.PutAsJsonAsync(
                $"/api/cart/items/{cartItemId}",
                new UpdateCartItemDTO
                {
                    Quantity = 3
                }
            );

            var responseText = await updateResponse.Content.ReadAsStringAsync();

            Assert.True(
                updateResponse.StatusCode == HttpStatusCode.OK,
                $"Expected 200 OK but got {(int)updateResponse.StatusCode} {updateResponse.StatusCode}. Body: {responseText}"
            );

            var result = await updateResponse.Content
                .ReadFromJsonAsync<ResponseTypeDTO<CartResponseDTO>>();

            Assert.NotNull(result);
            Assert.NotNull(result!.Content);

            var item = result.Content!.Items.First();

            Assert.Equal(cartItemId, item.CartItemId);
            Assert.Equal(3, item.Quantity);
            Assert.Equal(300000, item.SubTotal);

            Assert.Equal(3, result.Content.TotalItems);
            Assert.Equal(300000, result.Content.TotalAmount);
        }

        [Fact]
        public async Task UpdateQuantityZero_Returns400()
        {
            var token = await RegisterAndLoginAsync("update_zero");
            var product = await CreateActiveProductAsync("update_zero", 100000);

            SetBearerToken(token);

            var addResponse = await _client.PostAsJsonAsync(
                "/api/cart/items",
                new AddCartItemDTO
                {
                    ProductId = product.ProductId,
                    Quantity = 2
                }
            );

            var addResult = await addResponse.Content
                .ReadFromJsonAsync<ResponseTypeDTO<CartResponseDTO>>();

            Assert.Equal(HttpStatusCode.OK, addResponse.StatusCode);
            Assert.NotNull(addResult?.Content);

            var cartItemId = addResult!.Content!.Items.First().CartItemId;

            var updateResponse = await _client.PutAsJsonAsync(
                $"/api/cart/items/{cartItemId}",
                new UpdateCartItemDTO
                {
                    Quantity = 0
                }
            );

            var responseText = await updateResponse.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.BadRequest, updateResponse.StatusCode);
            Assert.Contains("Quantity must be between 1 and 99", responseText);
        }

        [Fact]
        public async Task RemoveItem_Valid_Returns200()
        {
            var token = await RegisterAndLoginAsync("remove_ok");
            var product = await CreateActiveProductAsync("remove_ok", 100000);

            SetBearerToken(token);

            var addResponse = await _client.PostAsJsonAsync(
                "/api/cart/items",
                new AddCartItemDTO
                {
                    ProductId = product.ProductId,
                    Quantity = 2
                }
            );

            var addResult = await addResponse.Content
                .ReadFromJsonAsync<ResponseTypeDTO<CartResponseDTO>>();

            Assert.Equal(HttpStatusCode.OK, addResponse.StatusCode);
            Assert.NotNull(addResult?.Content);

            var cartItemId = addResult!.Content!.Items.First().CartItemId;

            var removeResponse = await _client.DeleteAsync(
                $"/api/cart/items/{cartItemId}"
            );

            var responseText = await removeResponse.Content.ReadAsStringAsync();

            Assert.True(
                removeResponse.StatusCode == HttpStatusCode.OK,
                $"Expected 200 OK but got {(int)removeResponse.StatusCode} {removeResponse.StatusCode}. Body: {responseText}"
            );

            var result = await removeResponse.Content
                .ReadFromJsonAsync<ResponseTypeDTO<CartResponseDTO>>();

            Assert.NotNull(result);
            Assert.NotNull(result!.Content);

            Assert.Empty(result.Content!.Items);
            Assert.Equal(0, result.Content.TotalItems);
            Assert.Equal(0, result.Content.TotalAmount);
        }

        [Fact]
        public async Task ClearCart_ReturnsEmptyCart()
        {
            var token = await RegisterAndLoginAsync("clear_ok");

            var product1 = await CreateActiveProductAsync("clear_1", 100000);
            var product2 = await CreateActiveProductAsync("clear_2", 50000);

            SetBearerToken(token);

            var addFirstResponse = await _client.PostAsJsonAsync(
                "/api/cart/items",
                new AddCartItemDTO
                {
                    ProductId = product1.ProductId,
                    Quantity = 2
                }
            );

            Assert.Equal(HttpStatusCode.OK, addFirstResponse.StatusCode);

            var addSecondResponse = await _client.PostAsJsonAsync(
                "/api/cart/items",
                new AddCartItemDTO
                {
                    ProductId = product2.ProductId,
                    Quantity = 3
                }
            );

            Assert.Equal(HttpStatusCode.OK, addSecondResponse.StatusCode);

            var clearResponse = await _client.DeleteAsync("/api/cart/clear");

            var responseText = await clearResponse.Content.ReadAsStringAsync();

            Assert.True(
                clearResponse.StatusCode == HttpStatusCode.OK,
                $"Expected 200 OK but got {(int)clearResponse.StatusCode} {clearResponse.StatusCode}. Body: {responseText}"
            );

            var result = await clearResponse.Content
                .ReadFromJsonAsync<ResponseTypeDTO<CartResponseDTO>>();

            Assert.NotNull(result);
            Assert.NotNull(result!.Content);

            Assert.Empty(result.Content!.Items);
            Assert.Equal(0, result.Content.TotalItems);
            Assert.Equal(0, result.Content.TotalAmount);
        }

        private async Task<string> RegisterAndLoginAsync(string scenario)
        {
            var username = CreateUsername(scenario);

            var registerDto = new RegisterDTO
            {
                Username = username,
                Password = Password,
                FullName = "Cart Test User",
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

            var suffix = Guid.NewGuid().ToString("N").Substring(0, 8);

            var category = new Category
            {
                CategoryName = $"carttest_category_{scenario}_{suffix}",
                Description = "Category for cart api test",
                IsActive = true,
                CreatedAt = DateTime.Now
            };

            db.Categories.Add(category);

            await db.SaveChangesAsync();

            var product = new Product
            {
                ProductName = $"carttest_product_{scenario}_{suffix}",
                Description = "Product for cart api test",
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

            return $"carttest_{shortScenario}_{suffix}";
        }

        private async Task CleanupCartTestDataAsync()
        {
            using var scope = _factory.Services.CreateScope();

            var db = scope.ServiceProvider
                .GetRequiredService<CreationStoreDbContext>();

            var users = await db.Users
                .Where(u => u.Username.StartsWith("carttest_"))
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

            if (cartItems.Any())
            {
                db.CartItems.RemoveRange(cartItems);
                await db.SaveChangesAsync();
            }

            if (carts.Any())
            {
                db.Carts.RemoveRange(carts);
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
                .Where(p => p.ProductName.StartsWith("carttest_product_"))
                .ToListAsync();

            if (products.Any())
            {
                db.Products.RemoveRange(products);
                await db.SaveChangesAsync();
            }

            var categories = await db.Categories
                .Where(c => c.CategoryName.StartsWith("carttest_category_"))
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