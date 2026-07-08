using CreationStore.API.Data;
using CreationStore.API.DTOs;
using CreationStore.API.DTOs.Products;
using CreationStore.API.DTOs.ResponseTypes;
using CreationStore.API.Models;
using CreationStore.API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CreationStore.API.Services.Implementations
{
    public class ProductService : IProductService
    {
        private readonly CreationStoreDbContext _context;

        public ProductService(CreationStoreDbContext context)
        {
            _context = context;
        }

        private IQueryable<ProductResponseDTO> MapToProductResponse(IQueryable<Product> query)
        {
            return query.Select(p => new ProductResponseDTO
            {
                ProductId = p.ProductId,
                ProductName = p.ProductName,
                Description = p.Description,
                Price = p.Price,
                ImageUrl = p.ImageUrl,
                ValidityDays = p.ValidityDays,
                CategoryId = p.CategoryId,
                CategoryName = p.Category.CategoryName
            });
        }

        // ==========================
        // MEMBER
        // ==========================

        public async Task<ResponseTypeDTO<List<ProductResponseDTO>>> GetAllProductsAsync()
        {
            var products = await MapToProductResponse(
                    _context.Products
                        .AsNoTracking()
                        .Where(p => p.IsActive)
                )
                .ToListAsync();

            return new ResponseTypeDTO<List<ProductResponseDTO>>
            {
                StatusCode = 200,
                Message = "Get products successfully",
                Content = products
            };
        }

        public async Task<ResponseTypeDTO<ProductResponseDTO>> GetProductByIdAsync(int id)
        {
            var product = await MapToProductResponse(
                    _context.Products
                        .AsNoTracking()
                        .Where(p => p.ProductId == id && p.IsActive)
                )
                .FirstOrDefaultAsync();

            if (product == null)
            {
                return new ResponseTypeDTO<ProductResponseDTO>
                {
                    StatusCode = 404,
                    Message = "Product not found",
                    Content = null
                };
            }

            return new ResponseTypeDTO<ProductResponseDTO>
            {
                StatusCode = 200,
                Message = "Get product successfully",
                Content = product
            };
        }

        public async Task<ResponseTypeDTO<List<ProductResponseDTO>>> GetProductsByCategoryAsync(int categoryId)
        {
            var categoryExists = await _context.Categories
                .AsNoTracking()
                .AnyAsync(c => c.CategoryId == categoryId && c.IsActive);

            if (!categoryExists)
            {
                return new ResponseTypeDTO<List<ProductResponseDTO>>
                {
                    StatusCode = 404,
                    Message = "Category not found",
                    Content = null
                };
            }

            var products = await MapToProductResponse(
                    _context.Products
                        .AsNoTracking()
                        .Where(p => p.CategoryId == categoryId && p.IsActive)
                )
                .ToListAsync();

            return new ResponseTypeDTO<List<ProductResponseDTO>>
            {
                StatusCode = 200,
                Message = "Get products by category successfully",
                Content = products
            };
        }

        public async Task<ResponseTypeDTO<List<ProductResponseDTO>>> SearchProductsAsync(string? keyword)
        {
            var query = _context.Products
                .AsNoTracking()
                .Where(p => p.IsActive);

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                keyword = keyword.Trim();
                query = query.Where(p => p.ProductName.Contains(keyword));
            }

            var products = await MapToProductResponse(query).ToListAsync();

            return new ResponseTypeDTO<List<ProductResponseDTO>>
            {
                StatusCode = 200,
                Message = "Search products successfully",
                Content = products
            };
        }

        public async Task<ResponseTypeDTO<List<ProductResponseDTO>>> FilterProductsAsync(
            int? categoryId,
            string? keyword,
            decimal? minPrice,
            decimal? maxPrice
        )
        {
            if (minPrice.HasValue && maxPrice.HasValue && minPrice > maxPrice)
            {
                return new ResponseTypeDTO<List<ProductResponseDTO>>
                {
                    StatusCode = 400,
                    Message = "Min price cannot be greater than max price",
                    Content = null
                };
            }

            var query = _context.Products
                .AsNoTracking()
                .Where(p => p.IsActive);

            if (categoryId.HasValue)
            {
                var categoryExists = await _context.Categories
                    .AsNoTracking()
                    .AnyAsync(c => c.CategoryId == categoryId.Value && c.IsActive);

                if (!categoryExists)
                {
                    return new ResponseTypeDTO<List<ProductResponseDTO>>
                    {
                        StatusCode = 404,
                        Message = "Category not found",
                        Content = null
                    };
                }

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

            var products = await MapToProductResponse(query).ToListAsync();

            return new ResponseTypeDTO<List<ProductResponseDTO>>
            {
                StatusCode = 200,
                Message = "Filter products successfully",
                Content = products
            };
        }

        // ==========================
        // ADMIN
        // ==========================
        public async Task<ResponseTypeDTO<ProductResponseDTO>> CreateProductAsync(ProductCreateDTO dto)
        {
            if (string.IsNullOrWhiteSpace(dto.ProductName))
            {
                return new ResponseTypeDTO<ProductResponseDTO>
                {
                    StatusCode = 400,
                    Message = "Product name is required",
                    Content = null
                };
            }

            if (dto.Price < 0)
            {
                return new ResponseTypeDTO<ProductResponseDTO>
                {
                    StatusCode = 400,
                    Message = "Price must be greater than or equal to 0",
                    Content = null
                };
            }

            if (dto.ValidityDays.HasValue && dto.ValidityDays < 0)
            {
                return new ResponseTypeDTO<ProductResponseDTO>
                {
                    StatusCode = 400,
                    Message = "Validity days must be greater than or equal to 0",
                    Content = null
                };
            }

            if (dto.CategoryId <= 0)
            {
                return new ResponseTypeDTO<ProductResponseDTO>
                {
                    StatusCode = 400,
                    Message = "CategoryId is invalid",
                    Content = null
                };
            }

            var categoryExists = await _context.Categories
                .AnyAsync(c => c.CategoryId == dto.CategoryId && c.IsActive);

            if (!categoryExists)
            {
                return new ResponseTypeDTO<ProductResponseDTO>
                {
                    StatusCode = 400,
                    Message = "Invalid category",
                    Content = null
                };
            }

            var product = new Product
            {
                ProductName = dto.ProductName.Trim(),
                Description = dto.Description,
                Price = dto.Price,
                ImageUrl = dto.ImageUrl,
                ValidityDays = dto.ValidityDays,
                CategoryId = dto.CategoryId,
                IsActive = true,
                CreatedAt = System.DateTime.Now
            };

            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            var response = await GetProductByIdAsync(product.ProductId);

            return new ResponseTypeDTO<ProductResponseDTO>
            {
                StatusCode = 201,
                Message = "Product created successfully",
                Content = response.Content
            };
        }

        public async Task<ResponseTypeDTO<ProductResponseDTO>> UpdateProductAsync(int id, ProductUpdateDTO dto)
        {
            if (string.IsNullOrWhiteSpace(dto.ProductName))
            {
                return new ResponseTypeDTO<ProductResponseDTO>
                {
                    StatusCode = 400,
                    Message = "Product name is required",
                    Content = null
                };
            }

            if (dto.Price < 0)
            {
                return new ResponseTypeDTO<ProductResponseDTO>
                {
                    StatusCode = 400,
                    Message = "Price must be greater than or equal to 0",
                    Content = null
                };
            }

            if (dto.ValidityDays.HasValue && dto.ValidityDays < 0)
            {
                return new ResponseTypeDTO<ProductResponseDTO>
                {
                    StatusCode = 400,
                    Message = "Validity days must be greater than or equal to 0",
                    Content = null
                };
            }

            if (dto.CategoryId <= 0)
            {
                return new ResponseTypeDTO<ProductResponseDTO>
                {
                    StatusCode = 400,
                    Message = "CategoryId is invalid",
                    Content = null
                };
            }

            var product = await _context.Products
                .FirstOrDefaultAsync(p => p.ProductId == id && p.IsActive);

            if (product == null)
            {
                return new ResponseTypeDTO<ProductResponseDTO>
                {
                    StatusCode = 404,
                    Message = "Product not found",
                    Content = null
                };
            }

            var categoryExists = await _context.Categories
                .AnyAsync(c => c.CategoryId == dto.CategoryId && c.IsActive);

            if (!categoryExists)
            {
                return new ResponseTypeDTO<ProductResponseDTO>
                {
                    StatusCode = 400,
                    Message = "Invalid category",
                    Content = null
                };
            }

            product.ProductName = dto.ProductName.Trim();
            product.Description = dto.Description;
            product.Price = dto.Price;
            product.ImageUrl = dto.ImageUrl;
            product.ValidityDays = dto.ValidityDays;
            product.CategoryId = dto.CategoryId;
            product.UpdatedAt = System.DateTime.Now;

            await _context.SaveChangesAsync();

            var response = await GetProductByIdAsync(product.ProductId);

            return new ResponseTypeDTO<ProductResponseDTO>
            {
                StatusCode = 200,
                Message = "Product updated successfully",
                Content = response.Content
            };
        }

        public async Task<ResponseTypeDTO<bool>> DeleteProductAsync(int id)
        {
            var product = await _context.Products
                .FirstOrDefaultAsync(p => p.ProductId == id && p.IsActive);

            if (product == null)
            {
                return new ResponseTypeDTO<bool>
                {
                    StatusCode = 404,
                    Message = "Product not found",
                    Content = false
                };
            }

            product.IsActive = false;
            product.UpdatedAt = System.DateTime.Now;

            await _context.SaveChangesAsync();

            return new ResponseTypeDTO<bool>
            {
                StatusCode = 200,
                Message = "Product deleted successfully",
                Content = true
            };
        }
    }
}