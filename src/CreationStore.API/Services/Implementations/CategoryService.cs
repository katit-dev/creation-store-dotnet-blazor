using CreationStore.API.Data;
using CreationStore.API.DTOs.Categories;
using CreationStore.API.Models;
using CreationStore.API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CreationStore.API.Services.Implementations
{
    public class CategoryService : ICategoryService
    {
        private readonly CreationStoreDbContext _context;

        public CategoryService(CreationStoreDbContext context)
        {
            _context = context;
        }

        // ==========================
        // MEMBER
        // ==========================

        // Lấy tất cả category đang active
        public async Task<List<CategoryResponseDTO>> GetAllCategoriesAsync()
        {
            var categories = await _context.Categories
                    .AsNoTracking()
                    .Where(p => p.IsActive)
                    .Select(p => new CategoryResponseDTO
                    {
                        CategoryId = p.CategoryId,
                        CategoryName = p.CategoryName,
                        Description = p.Description,
                        IsActive = p.IsActive,
                        CreatedAt = p.CreatedAt
                    })
                    .ToListAsync();
            return categories;
        }

        // Lấy chi tiết category theo id
         public async Task<CategoryResponseDTO?> GetCategoryByIdAsync(int id)
        {
            var category = await _context.Categories
                .AsNoTracking()
                .Where(c => c.CategoryId == id && c.IsActive)
                .Select(c => new CategoryResponseDTO
                {
                    CategoryId = c.CategoryId,
                    CategoryName = c.CategoryName,
                    Description = c.Description,
                    IsActive = c.IsActive,
                    CreatedAt = c.CreatedAt
                })
                .FirstOrDefaultAsync();

            return category;
        }

        // ==========================
        // ADMIN
        // ==========================
        // Thêm category mới
        public async Task<CategoryResponseDTO?> CreateCategoryAsync(CategoryCreateDTO dto)
        {
            // trim name
            var categoryName = dto.CategoryName.Trim();

            // kiem tra name trong db
            var categoryExist = await _context.Categories
                .AnyAsync(c => c.CategoryName == categoryName && c.IsActive);

            // neu khong null-> ton tai
            if(categoryExist)
            {
                return null;
            }

            // neu khong ton tai -> tao moi
            var category = new Category
            {
                CategoryName = categoryName,
                Description = dto.Description,
                IsActive = true,
                CreatedAt = DateTime.Now
            };

            // them vao db va save changes
            _context.Categories.Add(category);
            await _context.SaveChangesAsync();

            // get by Id va return
            var result = await GetCategoryByIdAsync(category.CategoryId);

            return result;
        }

        // Cập nhật category
        public async Task<CategoryResponseDTO?> UpdateCategoryAsync(int id, CategoryUpdateDTO dto)
        {
            var category = await _context.Categories
                .FirstOrDefaultAsync(c => c.CategoryId == id && c.IsActive);

            if (category == null)
            {
                return null;
            }

            var categoryName = dto.CategoryName.Trim();

            var categoryNameExists = await _context.Categories
                .AnyAsync(c =>
                    c.CategoryName == categoryName &&
                    c.CategoryId != id &&
                    c.IsActive
                );

            if (categoryNameExists)
            {
                return null;
            }

            category.CategoryName = categoryName;
            category.Description = dto.Description;

            await _context.SaveChangesAsync();

            var result = await GetCategoryByIdAsync(category.CategoryId);

            return result;
        }

        // Xóa mềm category
        // Không xóa khỏi database, chỉ set IsActive = false
        public async Task<bool> DeleteCategoryAsync(int id)
        {
            var category = await _context.Categories
                .FirstOrDefaultAsync(c => c.CategoryId == id && c.IsActive);

            if (category == null)
            {
                return false;
            }

            var hasActiveProducts = await _context.Products
                .AnyAsync(p => p.CategoryId == id && p.IsActive);

            if (hasActiveProducts)
            {
                return false;
            }

            category.IsActive = false;

            await _context.SaveChangesAsync();

            return true;
        }
    }
}