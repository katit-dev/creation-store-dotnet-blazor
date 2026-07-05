using CreationStore.API.Data;
using CreationStore.API.DTOs.Products;
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
    }
}