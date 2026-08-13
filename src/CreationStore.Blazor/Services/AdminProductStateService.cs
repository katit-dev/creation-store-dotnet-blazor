using System.Net.Http.Headers;
using System.Net.Http.Json;
using CreationStore.Blazor.DTOs.Common;
using CreationStore.Blazor.DTOs.Products;

namespace CreationStore.Blazor.Services
{
    public class AdminProductStateService
    {
        private readonly HttpClient _httpClient;
        private readonly UserStateService _userStateService;

        public List<ProductResponseDTO> Products { get; private set; } = new();

        public bool IsLoading { get; private set; }

        public string? ErrorMessage { get; private set; }

        public string? SuccessMessage { get; private set; }

        public Action? OnChange { get; set; }

        public AdminProductStateService(
            HttpClient httpClient,
            UserStateService userStateService
        )
        {
            _httpClient = httpClient;
            _userStateService = userStateService;
        }

        public async Task LoadProductsAsync()
        {
            IsLoading = true;
            ErrorMessage = null;
            SuccessMessage = null;
            NotifyStateChanged();

            try
            {
                if (!EnsureAdminAuthenticated())
                {
                    ErrorMessage = "You do not have permission to manage products.";
                    return;
                }

                var response = await _httpClient.GetAsync("api/products");

                var responseData = await response.Content
                    .ReadFromJsonAsync<ResponseTypeDTO<List<ProductResponseDTO>>>();

                if (!response.IsSuccessStatusCode ||
                    responseData == null ||
                    responseData.StatusCode != 200 ||
                    responseData.Content == null)
                {
                    Products = new List<ProductResponseDTO>();
                    ErrorMessage = responseData?.Message ?? "Failed to load products.";
                    return;
                }

                Products = responseData.Content;
            }
            catch (Exception ex)
            {
                Products = new List<ProductResponseDTO>();
                ErrorMessage = $"Error loading products: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
                NotifyStateChanged();
            }
        }

        public async Task<bool> CreateProductAsync(ProductCreateDTO dto)
        {
            IsLoading = true;
            ErrorMessage = null;
            SuccessMessage = null;
            NotifyStateChanged();

            try
            {
                if (!EnsureAdminAuthenticated())
                {
                    ErrorMessage = "You do not have permission to create products.";
                    return false;
                }

                var response = await _httpClient.PostAsJsonAsync(
                    "api/admin/products",
                    dto
                );

                var responseData = await response.Content
                    .ReadFromJsonAsync<ResponseTypeDTO<ProductResponseDTO>>();

                if (!response.IsSuccessStatusCode ||
                    responseData == null ||
                    responseData.StatusCode < 200 ||
                    responseData.StatusCode >= 300 ||
                    responseData.Content == null)
                {
                    ErrorMessage = responseData?.Message ?? "Failed to create product.";
                    return false;
                }

                SuccessMessage = responseData.Message ?? "Product created successfully.";

                await LoadProductsAsync();

                return true;
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error creating product: {ex.Message}";
                return false;
            }
            finally
            {
                IsLoading = false;
                NotifyStateChanged();
            }
        }

        public async Task<bool> UpdateProductAsync(int productId, ProductUpdateDTO dto)
        {
            IsLoading = true;
            ErrorMessage = null;
            SuccessMessage = null;
            NotifyStateChanged();

            try
            {
                if (!EnsureAdminAuthenticated())
                {
                    ErrorMessage = "You do not have permission to update products.";
                    return false;
                }

                var response = await _httpClient.PutAsJsonAsync(
                    $"api/admin/products/{productId}",
                    dto
                );

                var responseData = await response.Content
                    .ReadFromJsonAsync<ResponseTypeDTO<ProductResponseDTO>>();

                if (!response.IsSuccessStatusCode ||
                    responseData == null ||
                    responseData.StatusCode < 200 ||
                    responseData.StatusCode >= 300 ||
                    responseData.Content == null)
                {
                    ErrorMessage = responseData?.Message ?? "Failed to update product.";
                    return false;
                }

                SuccessMessage = responseData.Message ?? "Product updated successfully.";

                await LoadProductsAsync();

                return true;
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error updating product: {ex.Message}";
                return false;
            }
            finally
            {
                IsLoading = false;
                NotifyStateChanged();
            }
        }

        public async Task<bool> DeleteProductAsync(int productId)
        {
            IsLoading = true;
            ErrorMessage = null;
            SuccessMessage = null;
            NotifyStateChanged();

            try
            {
                if (!EnsureAdminAuthenticated())
                {
                    ErrorMessage = "You do not have permission to delete products.";
                    return false;
                }

                var response = await _httpClient.DeleteAsync(
                    $"api/admin/products/{productId}"
                );

                var responseData = await response.Content
                    .ReadFromJsonAsync<ResponseTypeDTO<bool>>();

                if (!response.IsSuccessStatusCode ||
                    responseData == null ||
                    responseData.StatusCode < 200 ||
                    responseData.StatusCode >= 300 ||
                    responseData.Content == false)
                {
                    ErrorMessage = responseData?.Message ?? "Failed to delete product.";
                    return false;
                }

                SuccessMessage = responseData.Message ?? "Product deleted successfully.";

                await LoadProductsAsync();

                return true;
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error deleting product: {ex.Message}";
                return false;
            }
            finally
            {
                IsLoading = false;
                NotifyStateChanged();
            }
        }

        private bool EnsureAdminAuthenticated()
        {
            if (!_userStateService.IsAuthenticated ||
                !_userStateService.IsAdmin ||
                string.IsNullOrWhiteSpace(_userStateService.AccessToken))
            {
                return false;
            }

            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue(
                    "Bearer",
                    _userStateService.AccessToken
                );

            return true;
        }

        private void NotifyStateChanged()
        {
            OnChange?.Invoke();
        }
    }
}