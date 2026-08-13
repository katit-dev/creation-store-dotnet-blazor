using System.Net.Http.Headers;
using System.Net.Http.Json;
using CreationStore.Blazor.DTOs.Common;
using CreationStore.Blazor.DTOs.Orders;

namespace CreationStore.Blazor.Services
{
    public class AdminOrderStateService
    {
        private readonly HttpClient _httpClient;
        private readonly UserStateService _userStateService;

        public List<OrderResponseDTO> Orders { get; private set; } = new();

        public OrderResponseDTO? SelectedOrder { get; private set; }

        public bool IsLoading { get; private set; }

        public string? ErrorMessage { get; private set; }

        public string? SuccessMessage { get; private set; }

        public Action? OnChange { get; set; }

        public AdminOrderStateService(
            HttpClient httpClient,
            UserStateService userStateService
        )
        {
            _httpClient = httpClient;
            _userStateService = userStateService;
        }

        public async Task LoadOrdersAsync()
        {
            IsLoading = true;
            ErrorMessage = null;
            SuccessMessage = null;
            NotifyStateChanged();

            try
            {
                if (!EnsureAdminAuthenticated())
                {
                    ErrorMessage = "You do not have permission to manage orders.";
                    return;
                }

                var response = await _httpClient.GetAsync("api/admin/orders");

                var responseData = await response.Content
                    .ReadFromJsonAsync<ResponseTypeDTO<List<OrderResponseDTO>>>();

                if (!response.IsSuccessStatusCode ||
                    responseData == null ||
                    responseData.StatusCode != 200 ||
                    responseData.Content == null)
                {
                    Orders = new List<OrderResponseDTO>();
                    ErrorMessage = responseData?.Message ?? "Failed to load orders.";
                    return;
                }

                Orders = responseData.Content;
            }
            catch (Exception ex)
            {
                Orders = new List<OrderResponseDTO>();
                ErrorMessage = $"Error loading orders: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
                NotifyStateChanged();
            }
        }

        public async Task LoadOrderDetailAsync(int orderId)
        {
            IsLoading = true;
            ErrorMessage = null;
            SuccessMessage = null;
            SelectedOrder = null;
            NotifyStateChanged();

            try
            {
                if (!EnsureAdminAuthenticated())
                {
                    ErrorMessage = "You do not have permission to view this order.";
                    return;
                }

                var response = await _httpClient.GetAsync(
                    $"api/admin/orders/{orderId}"
                );

                var responseData = await response.Content
                    .ReadFromJsonAsync<ResponseTypeDTO<OrderResponseDTO>>();

                if (!response.IsSuccessStatusCode ||
                    responseData == null ||
                    responseData.StatusCode != 200 ||
                    responseData.Content == null)
                {
                    ErrorMessage = responseData?.Message ?? "Failed to load order detail.";
                    return;
                }

                SelectedOrder = responseData.Content;
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error loading order detail: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
                NotifyStateChanged();
            }
        }

        public async Task<bool> CompleteOrderAsync(int orderId)
        {
            IsLoading = true;
            ErrorMessage = null;
            SuccessMessage = null;
            NotifyStateChanged();

            try
            {
                if (!EnsureAdminAuthenticated())
                {
                    ErrorMessage = "You do not have permission to complete orders.";
                    return false;
                }

                var response = await _httpClient.PutAsync(
                    $"api/admin/orders/{orderId}/complete",
                    null
                );

                var responseData = await response.Content
                    .ReadFromJsonAsync<ResponseTypeDTO<OrderResponseDTO>>();

                if (!response.IsSuccessStatusCode ||
                    responseData == null ||
                    responseData.StatusCode < 200 ||
                    responseData.StatusCode >= 300 ||
                    responseData.Content == null)
                {
                    ErrorMessage = responseData?.Message ?? "Failed to complete order.";
                    return false;
                }

                SuccessMessage = responseData.Message ?? "Order completed successfully.";
                SelectedOrder = responseData.Content;

                await LoadOrdersAsync();

                return true;
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error completing order: {ex.Message}";
                return false;
            }
            finally
            {
                IsLoading = false;
                NotifyStateChanged();
            }
        }

        public async Task<bool> CancelOrderAsync(int orderId, string reason)
        {
            IsLoading = true;
            ErrorMessage = null;
            SuccessMessage = null;
            NotifyStateChanged();

            try
            {
                if (!EnsureAdminAuthenticated())
                {
                    ErrorMessage = "You do not have permission to cancel orders.";
                    return false;
                }

                var requestBody = new
                {
                    reason = reason,
                    cancelReason = reason
                };

                var response = await _httpClient.PutAsJsonAsync(
                    $"api/admin/orders/{orderId}/cancel",
                    requestBody
                );

                var responseData = await response.Content
                    .ReadFromJsonAsync<ResponseTypeDTO<OrderResponseDTO>>();

                if (!response.IsSuccessStatusCode ||
                    responseData == null ||
                    responseData.StatusCode < 200 ||
                    responseData.StatusCode >= 300 ||
                    responseData.Content == null)
                {
                    ErrorMessage = responseData?.Message ?? "Failed to cancel order.";
                    return false;
                }

                SuccessMessage = responseData.Message ?? "Order cancelled successfully.";
                SelectedOrder = responseData.Content;

                await LoadOrdersAsync();

                return true;
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error cancelling order: {ex.Message}";
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