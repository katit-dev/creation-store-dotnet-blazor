
using CreationStore.API.DTOs.Categories;
using CreationStore.API.DTOs.ResponseTypes;

namespace CreationStore.API.Services.Interfaces
{
    public interface ICategoryService
    {
        // PUBLIC / MEMBER
        Task<ResponseTypeDTO<List<CategoryResponseDTO>>> GetAllCategoriesAsync();

        Task<ResponseTypeDTO<CategoryResponseDTO>> GetCategoryByIdAsync(int id);

        // ADMIN
        Task<ResponseTypeDTO<CategoryResponseDTO>> CreateCategoryAsync(CategoryCreateDTO dto);

        Task<ResponseTypeDTO<CategoryResponseDTO>> UpdateCategoryAsync(int id, CategoryUpdateDTO dto);

        Task<ResponseTypeDTO<bool>> DeleteCategoryAsync(int id);
    }
}