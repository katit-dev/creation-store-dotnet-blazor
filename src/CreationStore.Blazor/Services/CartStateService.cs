using System.Net.Http.Headers;
using System.Net.Http.Json;
using CreationStore.Blazor.DTOs.Cart;
using CreationStore.Blazor.DTOs.Common;

namespace CreationStore.Blazor.Services
{
    public class CartStateService
    {
        private readonly HttpClient _httpClient;
        private readonly UserStateService _userStateService;

        public bool IsLoading { get; private set; }

        public string? ErrorMessage { get; private set; }

        public string? SuccessMessage { get; private set; }

        public Action? OnChange { get; set; }

        public CartStateService(
            HttpClient httpClient,
            UserStateService userStateService
        )
        {
            _httpClient = httpClient;
            _userStateService = userStateService;
        }

        public async Task<bool> AddToCartAsync(int productId, int quantity = 1)
        {
            IsLoading = true;
            ErrorMessage = null;
            SuccessMessage = null;
            NotifyStateChanged();

            try
            {
                if (!_userStateService.IsAuthenticated ||
                    string.IsNullOrWhiteSpace(_userStateService.AccessToken))
                {
                    ErrorMessage = "Please login before adding products to your cart.";
                    return false;
                }

                SetAuthorizationHeader(_userStateService.AccessToken);

                var dto = new AddCartItemDTO
                {
                    ProductId = productId,
                    Quantity = quantity
                };

                var response = await _httpClient.PostAsJsonAsync(
                    "api/cart/items",
                    dto
                );

                var responseData = await response.Content
                    .ReadFromJsonAsync<ResponseTypeDTO<object>>();

                if (!response.IsSuccessStatusCode ||
                    responseData == null ||
                    responseData.StatusCode < 200 ||
                    responseData.StatusCode >= 300)
                {
                    ErrorMessage = responseData?.Message ?? "Failed to add product to cart.";
                    return false;
                }

                SuccessMessage = responseData.Message ?? "Product added to cart.";
                return true;
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error adding product to cart: {ex.Message}";
                return false;
            }
            finally
            {
                IsLoading = false;
                NotifyStateChanged();
            }
        }

        private void SetAuthorizationHeader(string token)
        {
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);
        }

        private void NotifyStateChanged()
        {
            OnChange?.Invoke();
        }
    }
}