using System.Net.Http.Headers;
using System.Net.Http.Json;
using CreationStore.Blazor.DTOs.Common;
using CreationStore.Blazor.DTOs.Orders;
using CreationStore.Blazor.DTOs.Payments;
using Microsoft.AspNetCore.Components;

namespace CreationStore.Blazor.Services
{
    public class OrderStateService
    {
        private readonly HttpClient _httpClient;
        private readonly UserStateService _userStateService;
        private readonly NavigationManager _navigationManager;

        public List<OrderResponseDTO> Orders { get; private set; } = new();

        public OrderResponseDTO? SelectedOrder { get; private set; }

        public bool IsLoading { get; private set; }

        public string? ErrorMessage { get; private set; }

        public string? SuccessMessage { get; private set; }

        public Action? OnChange { get; set; }

        public OrderStateService(
            HttpClient httpClient,
            UserStateService userStateService,
            NavigationManager navigationManager
        )
        {
            _httpClient = httpClient;
            _userStateService = userStateService;
            _navigationManager = navigationManager;
        }

        public async Task LoadMyOrdersAsync()
        {
            IsLoading = true;
            ErrorMessage = null;
            SuccessMessage = null;
            NotifyStateChanged();

            try
            {
                if (!EnsureAuthenticated())
                {
                    Orders = new List<OrderResponseDTO>();
                    ErrorMessage = "Please login to view your orders.";
                    return;
                }

                var response = await _httpClient.GetAsync("api/orders");

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

        public async Task<OrderResponseDTO?> GetOrderByIdAsync(int orderId)
        {
            IsLoading = true;
            ErrorMessage = null;
            SuccessMessage = null;
            NotifyStateChanged();

            try
            {
                if (!EnsureAuthenticated())
                {
                    ErrorMessage = "Please login to view this order.";
                    return null;
                }

                var response = await _httpClient.GetAsync(
                    $"api/orders/{orderId}"
                );

                var responseData = await response.Content
                    .ReadFromJsonAsync<ResponseTypeDTO<OrderResponseDTO>>();

                if (!response.IsSuccessStatusCode ||
                    responseData == null ||
                    responseData.StatusCode != 200 ||
                    responseData.Content == null)
                {
                    SelectedOrder = null;
                    ErrorMessage = responseData?.Message ?? "Failed to load order detail.";
                    return null;
                }

                SelectedOrder = responseData.Content;
                return SelectedOrder;
            }
            catch (Exception ex)
            {
                SelectedOrder = null;
                ErrorMessage = $"Error loading order detail: {ex.Message}";
                return null;
            }
            finally
            {
                IsLoading = false;
                NotifyStateChanged();
            }
        }

        public async Task<bool> CancelOrderAsync(int orderId)
        {
            IsLoading = true;
            ErrorMessage = null;
            SuccessMessage = null;
            NotifyStateChanged();

            try
            {
                if (!EnsureAuthenticated())
                {
                    ErrorMessage = "Please login to cancel this order.";
                    return false;
                }

                var dto = new CancelOrderDTO
                {
                    CancelReason = "Cancelled by customer"
                };

                var response = await _httpClient.PutAsJsonAsync(
                    $"api/orders/{orderId}/cancel",
                    dto
                );

                var responseData = await response.Content
                    .ReadFromJsonAsync<ResponseTypeDTO<object>>();

                if (!response.IsSuccessStatusCode ||
                    responseData == null ||
                    responseData.StatusCode < 200 ||
                    responseData.StatusCode >= 300)
                {
                    ErrorMessage = responseData?.Message ?? "Failed to cancel order.";
                    return false;
                }

                SuccessMessage = responseData.Message ?? "Order cancelled successfully.";

                await LoadMyOrdersAsync();

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

        public async Task<bool> PayAgainAsync(int orderId)
        {
            IsLoading = true;
            ErrorMessage = null;
            SuccessMessage = null;
            NotifyStateChanged();

            try
            {
                if (!EnsureAuthenticated())
                {
                    _navigationManager.NavigateTo("/login");
                    return false;
                }

                var response = await _httpClient.PostAsync(
                    $"api/payments/vnpay/create-payment/{orderId}",
                    content: null
                );

                var responseData = await response.Content
                    .ReadFromJsonAsync<ResponseTypeDTO<VnPayPaymentResponseDTO>>();

                if (!response.IsSuccessStatusCode ||
                    responseData == null ||
                    responseData.StatusCode < 200 ||
                    responseData.StatusCode >= 300 ||
                    responseData.Content == null ||
                    string.IsNullOrWhiteSpace(responseData.Content.PaymentUrl))
                {
                    ErrorMessage = responseData?.Message ?? "Failed to create payment URL.";
                    return false;
                }

                _navigationManager.NavigateTo(
                    responseData.Content.PaymentUrl,
                    forceLoad: true
                );

                return true;
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Payment error: {ex.Message}";
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

        private void NotifyStateChanged()
        {
            OnChange?.Invoke();
        }
    }
}