using CreationStore.API.DTOs.Products;
using CreationStore.API.DTOs.ResponseTypes;

namespace CreationStore.API.Services.Interfaces
{
    public interface IProductService
    {
        // MEMBER
        Task<ResponseTypeDTO<List<ProductResponseDTO>>> GetAllProductsAsync();

        Task<ResponseTypeDTO<ProductResponseDTO>> GetProductByIdAsync(int id);

        Task<ResponseTypeDTO<List<ProductResponseDTO>>> GetProductsByCategoryAsync(int categoryId);

        Task<ResponseTypeDTO<List<ProductResponseDTO>>> SearchProductsAsync(string? keyword);

        Task<ResponseTypeDTO<List<ProductResponseDTO>>> FilterProductsAsync(
            int? categoryId,
            string? keyword,
            decimal? minPrice,
            decimal? maxPrice
        );

        // ADMIN
        Task<ResponseTypeDTO<ProductResponseDTO>> CreateProductAsync(ProductCreateDTO dto);

        Task<ResponseTypeDTO<ProductResponseDTO>> UpdateProductAsync(int id, ProductUpdateDTO dto);

        Task<ResponseTypeDTO<bool>> DeleteProductAsync(int id);
    }
}