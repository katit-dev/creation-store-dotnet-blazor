using CreationStore.API.DTOs.Products;

namespace CreationStore.API.Services.Interfaces
{
    public interface IProductService
    {
        // PUBLIC / MEMBER
        Task<List<ProductResponseDTO>> GetAllProductsAsync();

        Task<ProductResponseDTO?> GetProductByIdAsync(int id);

        Task<List<ProductResponseDTO>?> GetProductsByCategoryAsync(int categoryId);

        Task<List<ProductResponseDTO>> SearchProductsAsync(string keyword);

        Task<List<ProductResponseDTO>> FilterProductsAsync(
            int? categoryId,
            string? keyword,
            decimal? minPrice,
            decimal? maxPrice
        );

        // ADMIN
        Task<ProductResponseDTO?> CreateProductAsync(ProductCreateDTO dto);

        Task<ProductResponseDTO?> UpdateProductAsync(int id, ProductUpdateDTO dto);

        Task<bool> DeleteProductAsync(int id);
    }
}