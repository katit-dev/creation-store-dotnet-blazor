using CreationStore.API.DTOs.Categories;

namespace CreationStore.API.Services.Interfaces
{
    public interface ICategoryService
    {
        // ==========================
        // MEMBER
        // ==========================

        Task<List<CategoryResponseDTO>> GetAllCategoriesAsync();

        Task<CategoryResponseDTO?> GetCategoryByIdAsync(int id);

        // ==========================
        // ADMIN
        // ==========================

        Task<CategoryResponseDTO?> CreateCategoryAsync(CategoryCreateDTO dto);

        Task<CategoryResponseDTO?> UpdateCategoryAsync(int id, CategoryUpdateDTO dto);

        Task<bool> DeleteCategoryAsync(int id);
    }
}