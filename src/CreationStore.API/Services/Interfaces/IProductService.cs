using CreationStore.API.DTOs.Products;

namespace CreationStore.API.Services.Interfaces
{
    public interface IProductService
    {
        // Member functions 
        Task<List<ProductResponseDTO>> GetAllProductsAsync();

        Task<ProductResponseDTO?> GetProductByIdAsync(int id);

        Task<ProductResponseDTO> GetProductsByCategoryAsync(int categoryId);

        Task<ProductResponseDTO> SearchProductAsync(string keyword);

        Task<List<ProductResponseDTO>> FilterProductsAsync(
            int? categoryId,
            string? keyword,
            decimal? minPrice,
            decimal? maxPrice
        );

        // Admin functions
        Task<ProductResponseDTO> CreateProductAsync(ProductCreateDTO dto);
        Task<ProductResponseDTO> UpdateProductAsync(int id, ProductUpdateDTO dto);
        Task<bool> DeleteProductAsync(int id);

    }
}