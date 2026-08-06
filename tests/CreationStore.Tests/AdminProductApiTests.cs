using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using CreationStore.API.Data;
using CreationStore.API.DTOs.Auth;
using CreationStore.API.DTOs.Products;
using CreationStore.API.DTOs.ResponseTypes;
using CreationStore.API.Helpers.Constant;
using CreationStore.API.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CreationStore.Tests
{
    public class AdminProductApiTests :
        IClassFixture<CustomWebApplicationFactory>,
        IAsyncLifetime
    {
        private readonly CustomWebApplicationFactory _factory;
        private readonly HttpClient _client;

        private static readonly JsonSerializerOptions JsonOptions = new(
            JsonSerializerDefaults.Web
        );

        public AdminProductApiTests(CustomWebApplicationFactory factory)
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
        // 1. PUBLIC GET ALL PRODUCTS
        // ============================================================
        [Fact]
        public async Task GetAllProducts_Returns200()
        {
            var response = await _client.GetAsync("/api/products");

            var result = await response.Content
                .ReadFromJsonAsync<
                    ResponseTypeDTO<List<ProductResponseDTO>>
                >(JsonOptions);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(result);
            Assert.Equal(200, result!.StatusCode);
            Assert.NotNull(result.Content);
        }

        // ============================================================
        // 2. PUBLIC GET PRODUCT BY ID - NOT FOUND
        // ============================================================
        [Fact]
        public async Task GetProductById_NotFound_Returns404()
        {
            var response = await _client.GetAsync(
                "/api/products/999999999"
            );

            var result = await response.Content
                .ReadFromJsonAsync<ResponseTypeDTO<ProductResponseDTO>>(
                    JsonOptions
                );

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            Assert.NotNull(result);
            Assert.Equal(404, result!.StatusCode);
            Assert.Null(result.Content);
        }

        // ============================================================
        // 3. FILTER PRODUCTS - MIN PRICE GREATER THAN MAX PRICE
        // ============================================================
        [Fact]
        public async Task FilterProducts_MinPriceGreaterThanMaxPrice_Returns400()
        {
            var response = await _client.GetAsync(
                "/api/products/filter?minPrice=100000&maxPrice=1000"
            );

            var result = await response.Content
                .ReadFromJsonAsync<
                    ResponseTypeDTO<List<ProductResponseDTO>>
                >(JsonOptions);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.NotNull(result);
            Assert.Equal(400, result!.StatusCode);
            Assert.Null(result.Content);
        }

        // ============================================================
        // 4. FILTER PRODUCTS - INVALID CATEGORY
        // ============================================================
        [Fact]
        public async Task FilterProducts_InvalidCategory_Returns404()
        {
            var response = await _client.GetAsync(
                "/api/products/filter?categoryId=999999999"
            );

            var result = await response.Content
                .ReadFromJsonAsync<
                    ResponseTypeDTO<List<ProductResponseDTO>>
                >(JsonOptions);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            Assert.NotNull(result);
            Assert.Equal(404, result!.StatusCode);
            Assert.Null(result.Content);
        }

        // ============================================================
        // 5. ADMIN CREATE PRODUCT - NO TOKEN
        // ============================================================
        [Fact]
        public async Task AdminCreateProduct_WithoutToken_Returns401()
        {
            SetBearerToken(null);

            var categoryId = await CreateCategoryDirectlyAsync(
                CreateUniqueCategoryName("NoToken")
            );

            var response = await _client.PostAsJsonAsync(
                "/api/admin/products",
                BuildProductCreateDTO(
                    CreateUniqueProductName("NoToken"),
                    categoryId
                )
            );

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        // ============================================================
        // 6. ADMIN CREATE PRODUCT - MEMBER TOKEN
        // ============================================================
        [Fact]
        public async Task AdminCreateProduct_WithMemberToken_Returns403()
        {
            var memberToken = await CreateMemberTokenAsync("membercreate");
            SetBearerToken(memberToken);

            var categoryId = await CreateCategoryDirectlyAsync(
                CreateUniqueCategoryName("Member")
            );

            var response = await _client.PostAsJsonAsync(
                "/api/admin/products",
                BuildProductCreateDTO(
                    CreateUniqueProductName("Member"),
                    categoryId
                )
            );

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        // ============================================================
        // 7. ADMIN CREATE PRODUCT - ADMIN TOKEN
        // ============================================================
        [Fact]
        public async Task AdminCreateProduct_WithAdminToken_Returns201()
        {
            var adminToken = await CreateAdminTokenAsync("admincreate");
            SetBearerToken(adminToken);

            var categoryId = await CreateCategoryDirectlyAsync(
                CreateUniqueCategoryName("Create")
            );

            var productName = CreateUniqueProductName("Create");

            var response = await _client.PostAsJsonAsync(
                "/api/admin/products",
                BuildProductCreateDTO(productName, categoryId)
            );

            var result = await response.Content
                .ReadFromJsonAsync<ResponseTypeDTO<ProductResponseDTO>>(
                    JsonOptions
                );

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            Assert.NotNull(result);
            Assert.Equal(201, result!.StatusCode);
            Assert.NotNull(result.Content);
            Assert.Equal(productName, result.Content!.ProductName);
            Assert.Equal(categoryId, result.Content.CategoryId);
        }

        // ============================================================
        // 8. ADMIN CREATE PRODUCT - INVALID PRICE
        // ============================================================
        [Fact]
        public async Task AdminCreateProduct_InvalidPrice_Returns400()
        {
            var adminToken = await CreateAdminTokenAsync("invalidprice");
            SetBearerToken(adminToken);

            var categoryId = await CreateCategoryDirectlyAsync(
                CreateUniqueCategoryName("InvalidPrice")
            );

            var dto = BuildProductCreateDTO(
                CreateUniqueProductName("InvalidPrice"),
                categoryId
            );

            dto.Price = -1000;

            var response = await _client.PostAsJsonAsync(
                "/api/admin/products",
                dto
            );

            var result = await response.Content
                .ReadFromJsonAsync<ResponseTypeDTO<ProductResponseDTO>>(
                    JsonOptions
                );

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.NotNull(result);
            Assert.Equal(400, result!.StatusCode);
            Assert.Null(result.Content);
        }

        // ============================================================
        // 9. ADMIN CREATE PRODUCT - INVALID CATEGORY
        // ============================================================
        [Fact]
        public async Task AdminCreateProduct_InvalidCategory_Returns400()
        {
            var adminToken = await CreateAdminTokenAsync("invalidcategory");
            SetBearerToken(adminToken);

            var response = await _client.PostAsJsonAsync(
                "/api/admin/products",
                BuildProductCreateDTO(
                    CreateUniqueProductName("InvalidCategory"),
                    999999999
                )
            );

            var result = await response.Content
                .ReadFromJsonAsync<ResponseTypeDTO<ProductResponseDTO>>(
                    JsonOptions
                );

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.NotNull(result);
            Assert.Equal(400, result!.StatusCode);
            Assert.Null(result.Content);
        }

        // ============================================================
        // 10. ADMIN UPDATE PRODUCT - ADMIN TOKEN
        // ============================================================
        [Fact]
        public async Task AdminUpdateProduct_WithAdminToken_Returns200()
        {
            var adminToken = await CreateAdminTokenAsync("adminupdate");
            SetBearerToken(adminToken);

            var categoryId = await CreateCategoryDirectlyAsync(
                CreateUniqueCategoryName("Update")
            );

            var productId = await CreateProductByApiAsync(
                adminToken,
                CreateUniqueProductName("BeforeUpdate"),
                categoryId
            );

            var updatedName = CreateUniqueProductName("AfterUpdate");

            var response = await _client.PutAsJsonAsync(
                $"/api/admin/products/{productId}",
                BuildProductUpdateDTO(updatedName, categoryId)
            );

            var result = await response.Content
                .ReadFromJsonAsync<ResponseTypeDTO<ProductResponseDTO>>(
                    JsonOptions
                );

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(result);
            Assert.Equal(200, result!.StatusCode);
            Assert.NotNull(result.Content);
            Assert.Equal(updatedName, result.Content!.ProductName);
            Assert.Equal(categoryId, result.Content.CategoryId);
        }

        // ============================================================
        // 11. ADMIN UPDATE PRODUCT - NOT FOUND
        // ============================================================
        [Fact]
        public async Task AdminUpdateProduct_NotFound_Returns404()
        {
            var adminToken = await CreateAdminTokenAsync("updatenotfound");
            SetBearerToken(adminToken);

            var categoryId = await CreateCategoryDirectlyAsync(
                CreateUniqueCategoryName("UpdateNotFound")
            );

            var response = await _client.PutAsJsonAsync(
                "/api/admin/products/999999999",
                BuildProductUpdateDTO(
                    CreateUniqueProductName("NotFound"),
                    categoryId
                )
            );

            var result = await response.Content
                .ReadFromJsonAsync<ResponseTypeDTO<ProductResponseDTO>>(
                    JsonOptions
                );

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            Assert.NotNull(result);
            Assert.Equal(404, result!.StatusCode);
            Assert.Null(result.Content);
        }

        // ============================================================
        // 12. ADMIN DELETE PRODUCT - ADMIN TOKEN
        // ============================================================
        [Fact]
        public async Task AdminDeleteProduct_WithAdminToken_Returns200()
        {
            var adminToken = await CreateAdminTokenAsync("admindelete");
            SetBearerToken(adminToken);

            var categoryId = await CreateCategoryDirectlyAsync(
                CreateUniqueCategoryName("Delete")
            );

            var productId = await CreateProductByApiAsync(
                adminToken,
                CreateUniqueProductName("Delete"),
                categoryId
            );

            var response = await _client.DeleteAsync(
                $"/api/admin/products/{productId}"
            );

            var result = await response.Content
                .ReadFromJsonAsync<ResponseTypeDTO<bool>>(JsonOptions);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(result);
            Assert.Equal(200, result!.StatusCode);
            Assert.True(result.Content);
        }

        // ============================================================
        // 13. DELETED PRODUCT NOT VISIBLE IN PUBLIC GET BY ID
        // ============================================================
        [Fact]
        public async Task DeletedProduct_NotVisibleInPublicGetById()
        {
            var adminToken = await CreateAdminTokenAsync("deletedpublic");
            SetBearerToken(adminToken);

            var categoryId = await CreateCategoryDirectlyAsync(
                CreateUniqueCategoryName("DeletedPublic")
            );

            var productId = await CreateProductByApiAsync(
                adminToken,
                CreateUniqueProductName("DeletedPublic"),
                categoryId
            );

            var deleteResponse = await _client.DeleteAsync(
                $"/api/admin/products/{productId}"
            );

            Assert.Equal(HttpStatusCode.OK, deleteResponse.StatusCode);

            SetBearerToken(null);

            var getResponse = await _client.GetAsync(
                $"/api/products/{productId}"
            );

            var result = await getResponse.Content
                .ReadFromJsonAsync<ResponseTypeDTO<ProductResponseDTO>>(
                    JsonOptions
                );

            Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
            Assert.NotNull(result);
            Assert.Equal(404, result!.StatusCode);
            Assert.Null(result.Content);
        }

        // ============================================================
        // HELPERS
        // ============================================================

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

            var username = $"adminproducttest{testName}{suffix}";
            var password = "123456";
            var phone = "07" + Random.Shared
                .Next(10000000, 99999999)
                .ToString();

            var registerDto = new RegisterDTO
            {
                Username = username,
                Password = password,
                FullName = "Admin Product Test User",
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

        private async Task<int> CreateCategoryDirectlyAsync(
            string categoryName
        )
        {
            using var scope = _factory.Services.CreateScope();

            var db = scope.ServiceProvider
                .GetRequiredService<CreationStoreDbContext>();

            var category = new Category
            {
                CategoryName = categoryName,
                Description = "Category created directly by product test",
                IsActive = true,
                CreatedAt = DateTime.Now
            };

            db.Categories.Add(category);
            await db.SaveChangesAsync();

            return category.CategoryId;
        }

        private async Task<int> CreateProductByApiAsync(
            string adminToken,
            string productName,
            int categoryId
        )
        {
            SetBearerToken(adminToken);

            var response = await _client.PostAsJsonAsync(
                "/api/admin/products",
                BuildProductCreateDTO(productName, categoryId)
            );

            var result = await response.Content
                .ReadFromJsonAsync<ResponseTypeDTO<ProductResponseDTO>>(
                    JsonOptions
                );

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            Assert.NotNull(result);
            Assert.NotNull(result!.Content);

            return result.Content!.ProductId;
        }

        private static ProductCreateDTO BuildProductCreateDTO(
            string productName,
            int categoryId
        )
        {
            return new ProductCreateDTO
            {
                ProductName = productName,
                Description = "Product created by admin product test",
                Price = 450000,
                ImageUrl = "https://example.com/product.jpg",
                ValidityDays = 30,
                CategoryId = categoryId
            };
        }

        private static ProductUpdateDTO BuildProductUpdateDTO(
            string productName,
            int categoryId
        )
        {
            return new ProductUpdateDTO
            {
                ProductName = productName,
                Description = "Product updated by admin product test",
                Price = 500000,
                ImageUrl = "https://example.com/product-updated.jpg",
                ValidityDays = 60,
                CategoryId = categoryId
            };
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

        private static string CreateUniqueProductName(string prefix)
        {
            var suffix = Guid.NewGuid()
                .ToString("N")
                .Substring(0, 8);

            return $"AdminProductTest {prefix} {suffix}";
        }

        private static string CreateUniqueCategoryName(string prefix)
        {
            var suffix = Guid.NewGuid()
                .ToString("N")
                .Substring(0, 8);

            return $"AdminProductTest Category {prefix} {suffix}";
        }

        private async Task CleanupTestDataAsync()
        {
            using var scope = _factory.Services.CreateScope();

            var db = scope.ServiceProvider
                .GetRequiredService<CreationStoreDbContext>();

            var productIds = await db.Products
                .Where(p => p.ProductName.StartsWith("AdminProductTest"))
                .Select(p => p.ProductId)
                .ToListAsync();

            if (productIds.Any())
            {
                var products = await db.Products
                    .Where(p => productIds.Contains(p.ProductId))
                    .ToListAsync();

                db.Products.RemoveRange(products);
            }

            var categoryIds = await db.Categories
                .Where(c => c.CategoryName.StartsWith("AdminProductTest"))
                .Select(c => c.CategoryId)
                .ToListAsync();

            if (categoryIds.Any())
            {
                var categories = await db.Categories
                    .Where(c => categoryIds.Contains(c.CategoryId))
                    .ToListAsync();

                db.Categories.RemoveRange(categories);
            }

            var usernames = await db.Users
                .Where(u => u.Username.StartsWith("adminproducttest"))
                .Select(u => u.Username)
                .ToListAsync();

            if (usernames.Any())
            {
                var userIds = await db.Users
                    .Where(u => usernames.Contains(u.Username))
                    .Select(u => u.UserId)
                    .ToListAsync();

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