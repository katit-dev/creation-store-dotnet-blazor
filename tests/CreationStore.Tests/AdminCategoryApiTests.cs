using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using CreationStore.API.Data;
using CreationStore.API.DTOs.Auth;
using CreationStore.API.DTOs.Categories;
using CreationStore.API.DTOs.ResponseTypes;
using CreationStore.API.Helpers.Constant;
using CreationStore.API.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CreationStore.Tests
{
    public class AdminCategoryApiTests :
        IClassFixture<CustomWebApplicationFactory>,
        IAsyncLifetime
    {
        private readonly CustomWebApplicationFactory _factory;
        private readonly HttpClient _client;

        private static readonly JsonSerializerOptions JsonOptions = new(
            JsonSerializerDefaults.Web
        );

        public AdminCategoryApiTests(CustomWebApplicationFactory factory)
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
        // 1. PUBLIC GET ALL CATEGORIES
        // ============================================================
        [Fact]
        public async Task GetAllCategories_Returns200()
        {
            var response = await _client.GetAsync("/api/categories");

            var result = await response.Content
                .ReadFromJsonAsync<
                    ResponseTypeDTO<List<CategoryResponseDTO>>
                >(JsonOptions);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(result);
            Assert.Equal(200, result!.StatusCode);
            Assert.NotNull(result.Content);
        }

        // ============================================================
        // 2. PUBLIC GET CATEGORY BY ID - NOT FOUND
        // ============================================================
        [Fact]
        public async Task GetCategoryById_NotFound_Returns404()
        {
            var response = await _client.GetAsync(
                "/api/categories/999999999"
            );

            var result = await response.Content
                .ReadFromJsonAsync<ResponseTypeDTO<CategoryResponseDTO>>(
                    JsonOptions
                );

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            Assert.NotNull(result);
            Assert.Equal(404, result!.StatusCode);
            Assert.Null(result.Content);
        }

        // ============================================================
        // 3. ADMIN CREATE CATEGORY - NO TOKEN
        // ============================================================
        [Fact]
        public async Task AdminCreateCategory_WithoutToken_Returns401()
        {
            SetBearerToken(null);

            var response = await _client.PostAsJsonAsync(
                "/api/admin/categories",
                new CategoryCreateDTO
                {
                    CategoryName = CreateUniqueName("NoToken"),
                    Description = "Create category without token"
                }
            );

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        // ============================================================
        // 4. ADMIN CREATE CATEGORY - MEMBER TOKEN
        // ============================================================
        [Fact]
        public async Task AdminCreateCategory_WithMemberToken_Returns403()
        {
            var memberToken = await CreateMemberTokenAsync("membercreate");
            SetBearerToken(memberToken);

            var response = await _client.PostAsJsonAsync(
                "/api/admin/categories",
                new CategoryCreateDTO
                {
                    CategoryName = CreateUniqueName("Member"),
                    Description = "Member cannot create category"
                }
            );

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        // ============================================================
        // 5. ADMIN CREATE CATEGORY - ADMIN TOKEN
        // ============================================================
        [Fact]
        public async Task AdminCreateCategory_WithAdminToken_Returns201()
        {
            var adminToken = await CreateAdminTokenAsync("admincreate");
            SetBearerToken(adminToken);

            var categoryName = CreateUniqueName("Create");

            var response = await _client.PostAsJsonAsync(
                "/api/admin/categories",
                new CategoryCreateDTO
                {
                    CategoryName = categoryName,
                    Description = "Admin create category"
                }
            );

            var result = await response.Content
                .ReadFromJsonAsync<ResponseTypeDTO<CategoryResponseDTO>>(
                    JsonOptions
                );

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            Assert.NotNull(result);
            Assert.Equal(201, result!.StatusCode);
            Assert.NotNull(result.Content);
            Assert.Equal(categoryName, result.Content!.CategoryName);
            Assert.True(result.Content.IsActive);
        }

        // ============================================================
        // 6. ADMIN CREATE CATEGORY - DUPLICATE NAME
        // ============================================================
        [Fact]
        public async Task AdminCreateCategory_DuplicateName_Returns400()
        {
            var adminToken = await CreateAdminTokenAsync("duplicate");
            SetBearerToken(adminToken);

            var categoryName = CreateUniqueName("Duplicate");

            var firstResponse = await _client.PostAsJsonAsync(
                "/api/admin/categories",
                new CategoryCreateDTO
                {
                    CategoryName = categoryName,
                    Description = "First category"
                }
            );

            Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);

            var secondResponse = await _client.PostAsJsonAsync(
                "/api/admin/categories",
                new CategoryCreateDTO
                {
                    CategoryName = categoryName,
                    Description = "Duplicate category"
                }
            );

            var result = await secondResponse.Content
                .ReadFromJsonAsync<ResponseTypeDTO<CategoryResponseDTO>>(
                    JsonOptions
                );

            Assert.Equal(HttpStatusCode.BadRequest, secondResponse.StatusCode);
            Assert.NotNull(result);
            Assert.Equal(400, result!.StatusCode);
            Assert.Contains(
                "already exists",
                result.Message!,
                StringComparison.OrdinalIgnoreCase
            );
        }

        // ============================================================
        // 7. ADMIN UPDATE CATEGORY - ADMIN TOKEN
        // ============================================================
        [Fact]
        public async Task AdminUpdateCategory_WithAdminToken_Returns200()
        {
            var adminToken = await CreateAdminTokenAsync("adminupdate");
            SetBearerToken(adminToken);

            var categoryId = await CreateCategoryByApiAsync(
                adminToken,
                CreateUniqueName("BeforeUpdate")
            );

            var updatedName = CreateUniqueName("AfterUpdate");

            var response = await _client.PutAsJsonAsync(
                $"/api/admin/categories/{categoryId}",
                new CategoryUpdateDTO
                {
                    CategoryName = updatedName,
                    Description = "Updated category"
                }
            );

            var result = await response.Content
                .ReadFromJsonAsync<ResponseTypeDTO<CategoryResponseDTO>>(
                    JsonOptions
                );

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(result);
            Assert.Equal(200, result!.StatusCode);
            Assert.NotNull(result.Content);
            Assert.Equal(updatedName, result.Content!.CategoryName);
        }

        // ============================================================
        // 8. ADMIN UPDATE CATEGORY - NOT FOUND
        // ============================================================
        [Fact]
        public async Task AdminUpdateCategory_NotFound_Returns404()
        {
            var adminToken = await CreateAdminTokenAsync("updatenotfound");
            SetBearerToken(adminToken);

            var response = await _client.PutAsJsonAsync(
                "/api/admin/categories/999999999",
                new CategoryUpdateDTO
                {
                    CategoryName = CreateUniqueName("NotFound"),
                    Description = "Category not found"
                }
            );

            var result = await response.Content
                .ReadFromJsonAsync<ResponseTypeDTO<CategoryResponseDTO>>(
                    JsonOptions
                );

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            Assert.NotNull(result);
            Assert.Equal(404, result!.StatusCode);
            Assert.Null(result.Content);
        }

        // ============================================================
        // 9. ADMIN DELETE CATEGORY - ADMIN TOKEN
        // ============================================================
        [Fact]
        public async Task AdminDeleteCategory_WithAdminToken_Returns200()
        {
            var adminToken = await CreateAdminTokenAsync("admindelete");
            SetBearerToken(adminToken);

            var categoryId = await CreateCategoryByApiAsync(
                adminToken,
                CreateUniqueName("Delete")
            );

            var response = await _client.DeleteAsync(
                $"/api/admin/categories/{categoryId}"
            );

            var result = await response.Content
                .ReadFromJsonAsync<ResponseTypeDTO<bool>>(JsonOptions);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(result);
            Assert.Equal(200, result!.StatusCode);
            Assert.True(result.Content);
        }

        // ============================================================
        // 10. DELETED CATEGORY NOT VISIBLE IN PUBLIC GET BY ID
        // ============================================================
        [Fact]
        public async Task DeletedCategory_NotVisibleInPublicGetById()
        {
            var adminToken = await CreateAdminTokenAsync("deletedpublic");
            SetBearerToken(adminToken);

            var categoryId = await CreateCategoryByApiAsync(
                adminToken,
                CreateUniqueName("DeletedPublic")
            );

            var deleteResponse = await _client.DeleteAsync(
                $"/api/admin/categories/{categoryId}"
            );

            Assert.Equal(HttpStatusCode.OK, deleteResponse.StatusCode);

            SetBearerToken(null);

            var getResponse = await _client.GetAsync(
                $"/api/categories/{categoryId}"
            );

            var result = await getResponse.Content
                .ReadFromJsonAsync<ResponseTypeDTO<CategoryResponseDTO>>(
                    JsonOptions
                );

            Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
            Assert.NotNull(result);
            Assert.Equal(404, result!.StatusCode);
            Assert.Null(result.Content);
        }

        // ============================================================
        // 11. ADMIN DELETE CATEGORY WITH ACTIVE PRODUCTS - RETURNS 400
        // ============================================================
        [Fact]
        public async Task AdminDeleteCategory_WithActiveProducts_Returns400()
        {
            var adminToken = await CreateAdminTokenAsync("deletewithproduct");
            SetBearerToken(adminToken);

            var categoryId = await CreateCategoryDirectlyAsync(
                CreateUniqueName("HasProduct")
            );

            await CreateActiveProductDirectlyAsync(categoryId);

            var response = await _client.DeleteAsync(
                $"/api/admin/categories/{categoryId}"
            );

            var result = await response.Content
                .ReadFromJsonAsync<ResponseTypeDTO<bool>>(JsonOptions);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.NotNull(result);
            Assert.Equal(400, result!.StatusCode);
            Assert.False(result.Content);
            Assert.Contains(
                "active products",
                result.Message!,
                StringComparison.OrdinalIgnoreCase
            );
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

            var username = $"admincategorytest{testName}{suffix}";
            var password = "123456";
            var phone = "08" + Random.Shared
                .Next(10000000, 99999999)
                .ToString();

            var registerDto = new RegisterDTO
            {
                Username = username,
                Password = password,
                FullName = "Admin Category Test User",
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

        private async Task<int> CreateCategoryByApiAsync(
            string adminToken,
            string categoryName
        )
        {
            SetBearerToken(adminToken);

            var response = await _client.PostAsJsonAsync(
                "/api/admin/categories",
                new CategoryCreateDTO
                {
                    CategoryName = categoryName,
                    Description = "Category created by helper"
                }
            );

            var result = await response.Content
                .ReadFromJsonAsync<ResponseTypeDTO<CategoryResponseDTO>>(
                    JsonOptions
                );

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            Assert.NotNull(result);
            Assert.NotNull(result!.Content);

            return result.Content!.CategoryId;
        }

        private async Task<int> CreateCategoryDirectlyAsync(string categoryName)
        {
            using var scope = _factory.Services.CreateScope();

            var db = scope.ServiceProvider
                .GetRequiredService<CreationStoreDbContext>();

            var category = new Category
            {
                CategoryName = categoryName,
                Description = "Category created directly by test",
                IsActive = true,
                CreatedAt = DateTime.Now
            };

            db.Categories.Add(category);
            await db.SaveChangesAsync();

            return category.CategoryId;
        }

        private async Task<int> CreateActiveProductDirectlyAsync(int categoryId)
        {
            using var scope = _factory.Services.CreateScope();

            var db = scope.ServiceProvider
                .GetRequiredService<CreationStoreDbContext>();

            var product = new Product
            {
                ProductName = CreateUniqueName("Product"),
                Description = "Product created directly by test",
                Price = 100000,
                ImageUrl = "https://example.com/product.jpg",
                ValidityDays = 30,
                CategoryId = categoryId,
                IsActive = true,
                CreatedAt = DateTime.Now
            };

            db.Products.Add(product);
            await db.SaveChangesAsync();

            return product.ProductId;
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

            return $"AdminCategoryTest {prefix} {suffix}";
        }

        private async Task CleanupTestDataAsync()
        {
            using var scope = _factory.Services.CreateScope();

            var db = scope.ServiceProvider
                .GetRequiredService<CreationStoreDbContext>();

            var categoryIds = await db.Categories
                .Where(c => c.CategoryName.StartsWith("AdminCategoryTest"))
                .Select(c => c.CategoryId)
                .ToListAsync();

            var productIds = await db.Products
                .Where(p =>
                    p.ProductName.StartsWith("AdminCategoryTest") ||
                    categoryIds.Contains(p.CategoryId)
                )
                .Select(p => p.ProductId)
                .ToListAsync();

            if (productIds.Any())
            {
                var products = await db.Products
                    .Where(p => productIds.Contains(p.ProductId))
                    .ToListAsync();

                db.Products.RemoveRange(products);
            }

            if (categoryIds.Any())
            {
                var categories = await db.Categories
                    .Where(c => categoryIds.Contains(c.CategoryId))
                    .ToListAsync();

                db.Categories.RemoveRange(categories);
            }

            var usernames = await db.Users
                .Where(u => u.Username.StartsWith("admincategorytest"))
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