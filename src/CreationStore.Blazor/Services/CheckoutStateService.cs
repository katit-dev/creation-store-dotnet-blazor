using System.Net.Http.Headers;
using System.Net.Http.Json;
using CreationStore.Blazor.DTOs.Common;
using CreationStore.Blazor.DTOs.Orders;
using CreationStore.Blazor.DTOs.Payments;
using Microsoft.AspNetCore.Components;

namespace CreationStore.Blazor.Services
{
    public class CheckoutStateService
    {
        private readonly HttpClient _httpClient;
        private readonly UserStateService _userStateService;
        private readonly NavigationManager _navigationManager;

        public bool IsLoading { get; private set; }

        public string? ErrorMessage { get; private set; }

        public Action? OnChange { get; set; }

        public CheckoutStateService(
            HttpClient httpClient,
            UserStateService userStateService,
            NavigationManager navigationManager
        )
        {
            _httpClient = httpClient;
            _userStateService = userStateService;
            _navigationManager = navigationManager;
        }

        public async Task CheckoutAndPayAsync(string? note = null)
        {
            IsLoading = true;
            ErrorMessage = null;
            NotifyStateChanged();

            try
            {
                if (!EnsureAuthenticated())
                {
                    _navigationManager.NavigateTo("/login");
                    return;
                }

                var order = await CreateOrderAsync(note);

                if (order == null)
                {
                    return;
                }

                var payment = await CreateVnPayPaymentAsync(order.OrderId);

                if (payment == null ||
                    string.IsNullOrWhiteSpace(payment.PaymentUrl))
                {
                    ErrorMessage = "Failed to create payment URL.";
                    return;
                }

                _navigationManager.NavigateTo(payment.PaymentUrl, forceLoad: true);
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Checkout error: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
                NotifyStateChanged();
            }
        }

        private async Task<OrderResponseDTO?> CreateOrderAsync(string? note)
        {
            var dto = new CheckoutOrderDTO
            {
                Note = note
            };

            var response = await _httpClient.PostAsJsonAsync(
                "api/orders/checkout",
                dto
            );

            var responseData = await response.Content
                .ReadFromJsonAsync<ResponseTypeDTO<OrderResponseDTO>>();

            if (!response.IsSuccessStatusCode ||
                responseData == null ||
                responseData.StatusCode < 200 ||
                responseData.StatusCode >= 300 ||
                responseData.Content == null)
            {
                ErrorMessage = responseData?.Message ?? "Failed to create order.";
                return null;
            }

            return responseData.Content;
        }

        private async Task<VnPayPaymentResponseDTO?> CreateVnPayPaymentAsync(int orderId)
        {
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
                responseData.Content == null)
            {
                ErrorMessage = responseData?.Message ?? "Failed to create VNPAY payment.";
                return null;
            }

            return responseData.Content;
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