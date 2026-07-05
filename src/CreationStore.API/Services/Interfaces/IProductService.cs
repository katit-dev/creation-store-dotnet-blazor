using CreationStore.API.DTOs.Products;

namespace CreationStore.API.Services.Interfaces
{
    public interface IProductService
    {
        Task<List<ProductResponseDTO>> GetAllProductsAsync();

        Task<ProductResponseDTO?> GetProductByIdAsync(int id);
    }
}