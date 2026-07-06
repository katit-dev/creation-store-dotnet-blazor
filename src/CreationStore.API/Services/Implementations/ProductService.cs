using CreationStore.API.Data;
using CreationStore.API.DTOs.Products;
using CreationStore.API.Models;
using CreationStore.API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace CreationStore.API.Services.Implementations
{
    public class ProductService : IProductService
    {
        private readonly CreationStoreDbContext _context;

        public ProductService(CreationStoreDbContext context)
        {
            _context = context;
        }

        // ==========================
        // PUBLIC / MEMBER
        // ==========================


        // Lấy tất cả sản phẩm đang active
        public async Task<List<ProductResponseDTO>> GetAllProductsAsync()
        {
            var products = await _context.Products
            .AsNoTracking()
            .Where(p => p.IsActive)
            .Select(p => new ProductResponseDTO
            {
                ProductId = p.ProductId,
                ProductName = p.ProductName,
                Description = p.Description,
                Price = p.Price,
                ImageUrl = p.ImageUrl,
                ValidityDays = p.ValidityDays,
                CategoryId = p.CategoryId,
                CategoryName = p.Category.CategoryName
            })
            .ToListAsync();

            return products;
        }

        // Lấy ra chi tiết 1 sản phẩm theo id
        public async Task<ProductResponseDTO?> GetProductByIdAsync(int id)
        {
            var product = await _context.Products
                .AsNoTracking()
                .Where(p => p.ProductId == id && p.IsActive)
                .Select(p => new ProductResponseDTO
                {
                    ProductId = p.ProductId,
                    ProductName = p.ProductName,
                    Description = p.Description,
                    Price = p.Price,
                    ImageUrl = p.ImageUrl,
                    ValidityDays = p.ValidityDays,
                    CategoryId = p.CategoryId,
                    CategoryName = p.Category.CategoryName
                })
                .FirstOrDefaultAsync();

            return product;
        }

        // Lọc sản phẩm theo category
        public async Task<List<ProductResponseDTO>?> GetProductsByCategoryAsync(int categoryId)
        {
            // kiem tra xem categoryId co ton tai va active hay khong
            var categoryExists = await _context.Categories
        .AsNoTracking()
        .AnyAsync(c => c.CategoryId == categoryId && c.IsActive);

            if (!categoryExists)
            {
                return null;
            }

            var products = await _context.Products
                    .AsNoTracking()
                    .Where(p => p.CategoryId == categoryId && p.IsActive)
                    .Select(p => new ProductResponseDTO
                    {
                        ProductId = p.ProductId,
                        ProductName = p.ProductName,
                        Description = p.Description,
                        Price = p.Price,
                        ImageUrl = p.ImageUrl,
                        ValidityDays = p.ValidityDays,
                        CategoryId = p.CategoryId,
                        CategoryName = p.Category.CategoryName
                    })
                    .ToListAsync();
            return products;
        }

        // Tìm kiếm sản phẩm theo tên
        public async Task<List<ProductResponseDTO>> SearchProductsAsync(string keyword)
        {
            keyword = keyword.Trim();
            var products = await _context.Products
                    .AsNoTracking()
                    .Where(p => p.IsActive && p.ProductName.Contains(keyword))
                    .Select(p => new ProductResponseDTO
                    {
                        ProductId = p.ProductId,
                        ProductName = p.ProductName,
                        Description = p.Description,
                        Price = p.Price,
                        ImageUrl = p.ImageUrl,
                        ValidityDays = p.ValidityDays,
                        CategoryId = p.CategoryId,
                        CategoryName = p.Category.CategoryName
                    })
                    .ToListAsync();
            return products;
        }

        // Lọc nâng cao: category, keyword, minPrice, maxPrice
        public async Task<List<ProductResponseDTO>> FilterProductsAsync(
            int? categoryId,
            string? keyword,
            decimal? minPrice,
            decimal? maxPrice
        )
        {
            var query = _context.Products
                .AsNoTracking()
                .Where(p => p.IsActive);

            if (categoryId.HasValue)
            {
                query = query.Where(p => p.CategoryId == categoryId.Value);
            }

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                keyword = keyword.Trim();
                query = query.Where(p => p.ProductName.Contains(keyword));
            }

            if (minPrice.HasValue)
            {
                query = query.Where(p => p.Price >= minPrice.Value);
            }

            if (maxPrice.HasValue)
            {
                query = query.Where(p => p.Price <= maxPrice.Value);
            }

            var products = await query
                .Select(p => new ProductResponseDTO
                {

                    ProductId = p.ProductId,
                    ProductName = p.ProductName,
                    Description = p.Description,
                    Price = p.Price,
                    ImageUrl = p.ImageUrl,
                    ValidityDays = p.ValidityDays,
                    CategoryId = p.CategoryId,
                    CategoryName = p.Category.CategoryName
                })
                .ToListAsync();
            return products;
        }

        // ==========================
        // ADMIN
        // Sau này thêm [Authorize(Roles = "Admin")] ở Controller
        // ==========================
        // Thêm sản phẩm mới
        public async Task<ProductResponseDTO?> CreateProductAsync(ProductCreateDTO dto)
        {
            var categoryExists = await _context.Categories
                .AnyAsync(c => c.CategoryId == dto.CategoryId && c.IsActive);

            if (!categoryExists)
            {
                return null;
            }

            var product = new Product
            {
                ProductName = dto.ProductName,
                Description = dto.Description,
                Price = dto.Price,
                ImageUrl = dto.ImageUrl,
                ValidityDays = dto.ValidityDays,
                CategoryId = dto.CategoryId,
                IsActive = true,
                CreatedAt = DateTime.Now
            };

            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            var result = await GetProductByIdAsync(product.ProductId);

            return result;
        }
        // Cập nhật sản phẩm
        public async Task<ProductResponseDTO?> UpdateProductAsync(int id, ProductUpdateDTO dto)
        {
            var product = await _context.Products
                .FirstOrDefaultAsync(p => p.ProductId == id && p.IsActive);

            if (product == null)
            {
                return null;
            }

            var categoryExists = await _context.Categories
                .AnyAsync(c => c.CategoryId == dto.CategoryId && c.IsActive);

            if (!categoryExists)
            {
                return null;
            }

            product.ProductName = dto.ProductName;
            product.Description = dto.Description;
            product.Price = dto.Price;
            product.ImageUrl = dto.ImageUrl;
            product.ValidityDays = dto.ValidityDays;
            product.CategoryId = dto.CategoryId;
            product.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            var result = await GetProductByIdAsync(product.ProductId);

            return result;
        }

        // Xóa mềm sản phẩm
        // Không xóa khỏi database, chỉ set IsActive = false
        public async Task<bool> DeleteProductAsync(int id)
        {
            var product = await _context.Products
                .FirstOrDefaultAsync(p => p.ProductId == id && p.IsActive);

            if (product == null)
            {
                return false;
            }

            product.IsActive = false;
            product.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            return true;
        }

    }
}