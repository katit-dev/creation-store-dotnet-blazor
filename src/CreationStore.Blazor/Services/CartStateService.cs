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

        public CartResponseDTO? Cart { get; private set; }

        public bool IsLoading { get; private set; }

        public string? ErrorMessage { get; private set; }

        public string? SuccessMessage { get; private set; }

        public int CartItemCount =>
            Cart?.Items.Sum(item => item.Quantity) ?? 0;

        public decimal TotalAmount =>
            Cart?.DisplayTotalAmount ?? 0;

        public Action? OnChange { get; set; }

        public CartStateService(
            HttpClient httpClient,
            UserStateService userStateService
        )
        {
            _httpClient = httpClient;
            _userStateService = userStateService;
        }

        public async Task LoadCartAsync(bool showLoading = true)
        {
            if (showLoading)
            {
                IsLoading = true;
                ErrorMessage = null;
                SuccessMessage = null;
                NotifyStateChanged();
            }

            try
            {
                if (!EnsureAuthenticated())
                {
                    Cart = null;
                    ErrorMessage = "Please login to view your cart.";
                    return;
                }

                var response = await _httpClient.GetAsync("api/cart");

                var responseData = await response.Content
                    .ReadFromJsonAsync<ResponseTypeDTO<CartResponseDTO>>();

                if (!response.IsSuccessStatusCode ||
                    responseData == null ||
                    responseData.StatusCode != 200)
                {
                    Cart = null;
                    ErrorMessage = responseData?.Message ?? "Failed to load cart.";
                    return;
                }

                Cart = responseData.Content ?? new CartResponseDTO();
            }
            catch (Exception ex)
            {
                Cart = null;
                ErrorMessage = $"Error loading cart: {ex.Message}";
            }
            finally
            {
                if (showLoading)
                {
                    IsLoading = false;
                }

                NotifyStateChanged();
            }
        }

        public async Task<bool> AddToCartAsync(int productId, int quantity = 1)
        {
            IsLoading = true;
            ErrorMessage = null;
            SuccessMessage = null;
            NotifyStateChanged();

            try
            {
                if (!EnsureAuthenticated())
                {
                    ErrorMessage = "Please login before adding products to your cart.";
                    return false;
                }

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

                if (!IsSuccess(response, responseData))
                {
                    ErrorMessage = responseData?.Message ?? "Failed to add product to cart.";
                    return false;
                }

                SuccessMessage = responseData?.Message ?? "Product added to cart.";

                await LoadCartAsync(showLoading: false);

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

        public async Task<bool> UpdateQuantityAsync(int cartItemId, int quantity)
        {
            IsLoading = true;
            ErrorMessage = null;
            SuccessMessage = null;
            NotifyStateChanged();

            try
            {
                if (!EnsureAuthenticated())
                {
                    ErrorMessage = "Please login to update your cart.";
                    return false;
                }

                if (quantity <= 0)
                {
                    ErrorMessage = "Quantity must be greater than 0.";
                    return false;
                }

                var dto = new UpdateCartItemDTO
                {
                    Quantity = quantity
                };

                var response = await _httpClient.PutAsJsonAsync(
                    $"api/cart/items/{cartItemId}",
                    dto
                );

                var responseData = await response.Content
                    .ReadFromJsonAsync<ResponseTypeDTO<object>>();

                if (!IsSuccess(response, responseData))
                {
                    ErrorMessage = responseData?.Message ?? "Failed to update cart item.";
                    return false;
                }

                SuccessMessage = responseData?.Message ?? "Cart updated.";

                await LoadCartAsync(showLoading: false);

                return true;
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error updating cart: {ex.Message}";
                return false;
            }
            finally
            {
                IsLoading = false;
                NotifyStateChanged();
            }
        }

        public async Task<bool> RemoveItemAsync(int cartItemId)
        {
            IsLoading = true;
            ErrorMessage = null;
            SuccessMessage = null;
            NotifyStateChanged();

            try
            {
                if (!EnsureAuthenticated())
                {
                    ErrorMessage = "Please login to update your cart.";
                    return false;
                }

                var response = await _httpClient.DeleteAsync(
                    $"api/cart/items/{cartItemId}"
                );

                var responseData = await response.Content
                    .ReadFromJsonAsync<ResponseTypeDTO<object>>();

                if (!IsSuccess(response, responseData))
                {
                    ErrorMessage = responseData?.Message ?? "Failed to remove cart item.";
                    return false;
                }

                SuccessMessage = responseData?.Message ?? "Cart item removed.";

                await LoadCartAsync(showLoading: false);

                return true;
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error removing cart item: {ex.Message}";
                return false;
            }
            finally
            {
                IsLoading = false;
                NotifyStateChanged();
            }
        }

        public async Task<bool> ClearCartAsync()
        {
            IsLoading = true;
            ErrorMessage = null;
            SuccessMessage = null;
            NotifyStateChanged();

            try
            {
                if (!EnsureAuthenticated())
                {
                    ErrorMessage = "Please login to clear your cart.";
                    return false;
                }

                var response = await _httpClient.DeleteAsync("api/cart/clear");

                var responseData = await response.Content
                    .ReadFromJsonAsync<ResponseTypeDTO<object>>();

                if (!IsSuccess(response, responseData))
                {
                    ErrorMessage = responseData?.Message ?? "Failed to clear cart.";
                    return false;
                }

                SuccessMessage = responseData?.Message ?? "Cart cleared.";

                await LoadCartAsync(showLoading: false);

                return true;
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error clearing cart: {ex.Message}";
                return false;
            }
            finally
            {
                IsLoading = false;
                NotifyStateChanged();
            }
        }

        private bool EnsureAuthenticated()
        {
            if (!_userStateService.IsAuthenticated ||
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

        private static bool IsSuccess(
            HttpResponseMessage response,
            ResponseTypeDTO<object>? responseData
        )
        {
            return response.IsSuccessStatusCode &&
                   responseData != null &&
                   responseData.StatusCode >= 200 &&
                   responseData.StatusCode < 300;
        }

        private void NotifyStateChanged()
        {
            OnChange?.Invoke();
        }
    }
}