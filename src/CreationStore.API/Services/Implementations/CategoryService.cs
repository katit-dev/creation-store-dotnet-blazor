using CreationStore.API.Data;
using CreationStore.API.DTOs.Categories;
using CreationStore.API.DTOs.ResponseTypes;
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
        public async Task<ResponseTypeDTO<List<CategoryResponseDTO>>> GetAllCategoriesAsync()
        {
            var categories = await _context.Categories
                .AsNoTracking()
                .Where(c => c.IsActive)
                .Select(c => new CategoryResponseDTO
                {
                    CategoryId = c.CategoryId,
                    CategoryName = c.CategoryName,
                    Description = c.Description,
                    IsActive = c.IsActive,
                    CreatedAt = c.CreatedAt
                })
                .ToListAsync();

            return new ResponseTypeDTO<List<CategoryResponseDTO>>
            {
                StatusCode = 200,
                Message = "Get categories successfully",
                Content = categories,
                DateTime = System.DateTime.Now
            };
        }

        public async Task<ResponseTypeDTO<CategoryResponseDTO>> GetCategoryByIdAsync(int id)
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

            if (category == null)
            {
                return new ResponseTypeDTO<CategoryResponseDTO>
                {
                    StatusCode = 404,
                    Message = "Category not found",
                    Content = null,
                    DateTime = System.DateTime.Now
                };
            }

            return new ResponseTypeDTO<CategoryResponseDTO>
            {
                StatusCode = 200,
                Message = "Get category successfully",
                Content = category,
                DateTime = System.DateTime.Now
            };
        }

        // ==========================
        // ADMIN
        // ==========================
        public async Task<ResponseTypeDTO<CategoryResponseDTO>> CreateCategoryAsync(CategoryCreateDTO dto)
        {
            // trim name
            var categoryName = dto.CategoryName.Trim();

            // check if name exists
            var categoryExists = await _context.Categories
                .AnyAsync(c => c.CategoryName == categoryName && c.IsActive);

            if (categoryExists)
            {
                return new ResponseTypeDTO<CategoryResponseDTO>
                {
                    StatusCode = 400,
                    Message = "Category name already exists",
                    Content = null,
                    DateTime = System.DateTime.Now
                };
            }

            // create new category
            var category = new Category
            {
                CategoryName = categoryName,
                Description = dto.Description,
                IsActive = true,
                CreatedAt = DateTime.Now
            };

            // save to database
            _context.Categories.Add(category);
            await _context.SaveChangesAsync();

            // get by id and rerturn response
            var response = await GetCategoryByIdAsync(category.CategoryId);

            return new ResponseTypeDTO<CategoryResponseDTO>
            {
                StatusCode = 200,
                Message = "Category created successfully",
                Content = response.Content,
                DateTime = System.DateTime.Now
            };
        }

        public async Task<ResponseTypeDTO<CategoryResponseDTO>> UpdateCategoryAsync(int id, CategoryUpdateDTO dto)
        {
            var category = await _context.Categories
                .FirstOrDefaultAsync(c => c.CategoryId == id && c.IsActive);

            if (category == null)
            {
                return new ResponseTypeDTO<CategoryResponseDTO>
                {
                    StatusCode = 404,
                    Message = "Category not found",
                    Content = null,
                    DateTime = System.DateTime.Now
                };
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
                return new ResponseTypeDTO<CategoryResponseDTO>
                {
                    StatusCode = 400,
                    Message = "Category name already exists",
                    Content = null,
                    DateTime = System.DateTime.Now
                };
            }

            category.CategoryName = categoryName;
            category.Description = dto.Description;

            await _context.SaveChangesAsync();

            var response = await GetCategoryByIdAsync(category.CategoryId);

            return new ResponseTypeDTO<CategoryResponseDTO>
            {
                StatusCode = 200,
                Message = "Category updated successfully",
                Content = response.Content,
                DateTime = System.DateTime.Now
            };
        }

        public async Task<ResponseTypeDTO<bool>> DeleteCategoryAsync(int id)
        {
            var category = await _context.Categories
                .FirstOrDefaultAsync(c => c.CategoryId == id && c.IsActive);

            if (category == null)
            {
                return new ResponseTypeDTO<bool>
                {
                    StatusCode = 404,
                    Message = "Category not found",
                    Content = false,
                    DateTime = System.DateTime.Now
                };
            }

            var hasActiveProducts = await _context.Products
                .AnyAsync(p => p.CategoryId == id && p.IsActive);

            if (hasActiveProducts)
            {
                return new ResponseTypeDTO<bool>
                {
                    StatusCode = 400,
                    Message = "Cannot delete category because it has active products",
                    Content = false,
                    DateTime = System.DateTime.Now
                };
            }

            category.IsActive = false;

            await _context.SaveChangesAsync();

            return new ResponseTypeDTO<bool>
            {
                StatusCode = 200,
                Message = "Category deleted successfully",
                Content = true,
                DateTime = System.DateTime.Now
            };
        }
    }
}