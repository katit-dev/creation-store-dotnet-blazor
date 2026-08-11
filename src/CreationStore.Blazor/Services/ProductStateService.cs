using System.Net.Http.Json;
using CreationStore.Blazor.DTOs.Common;
using CreationStore.Blazor.DTOs.Products;

namespace CreationStore.Blazor.Services
{
    public class ProductStateService
    {
        private readonly HttpClient _httpClient;

        public List<ProductResponseDTO> Products { get; private set; } = new();

        public bool IsLoading { get; private set; }

        public string? ErrorMessage { get; private set; }

        public Action? OnChange { get; set; }

        public ProductStateService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task LoadProductsAsync(string? keyword = null)
        {
            IsLoading = true;
            ErrorMessage = null;
            NotifyStateChanged();

            try
            {
                string url;

                if (string.IsNullOrWhiteSpace(keyword))
                {
                    url = "api/products";
                }
                else
                {
                    var encodedKeyword = Uri.EscapeDataString(keyword.Trim());
                    url = $"api/products/search?keyword={encodedKeyword}";
                }

                var response = await _httpClient.GetAsync(url);

                var responseData = await response.Content
                    .ReadFromJsonAsync<ResponseTypeDTO<List<ProductResponseDTO>>>();

                if (!response.IsSuccessStatusCode ||
                    responseData == null ||
                    responseData.StatusCode != 200 ||
                    responseData.Content == null)
                {
                    Products = new List<ProductResponseDTO>();
                    ErrorMessage = responseData?.Message ?? "Không tải được sản phẩm.";
                    return;
                }

                Products = responseData.Content;
            }
            catch (Exception ex)
            {
                Products = new List<ProductResponseDTO>();
                ErrorMessage = $"Lỗi khi tải sản phẩm: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
                NotifyStateChanged();
            }
        }

        public async Task<ProductResponseDTO?> GetProductByIdAsync(int productId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"api/products/{productId}");

                var responseData = await response.Content
                    .ReadFromJsonAsync<ResponseTypeDTO<ProductResponseDTO>>();

                if (!response.IsSuccessStatusCode ||
                    responseData == null ||
                    responseData.StatusCode != 200 ||
                    responseData.Content == null)
                {
                    return null;
                }

                return responseData.Content;
            }
            catch
            {
                return null;
            }
        }

        private void NotifyStateChanged()
        {
            OnChange?.Invoke();
        }
    }
}