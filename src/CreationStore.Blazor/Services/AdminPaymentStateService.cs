using System.Net.Http.Headers;
using System.Net.Http.Json;
using CreationStore.Blazor.DTOs.Admin.Payments;
using CreationStore.Blazor.DTOs.Common;

namespace CreationStore.Blazor.Services
{
    public class AdminPaymentStateService
    {
        private readonly HttpClient _httpClient;
        private readonly UserStateService _userStateService;

        public List<AdminPaymentResponseDTO> Payments { get; private set; } = new();

        public AdminPaymentResponseDTO? SelectedPayment { get; private set; }

        public bool IsLoading { get; private set; }

        public string? ErrorMessage { get; private set; }

        public string? SuccessMessage { get; private set; }

        public Action? OnChange { get; set; }

        public AdminPaymentStateService(
            HttpClient httpClient,
            UserStateService userStateService
        )
        {
            _httpClient = httpClient;
            _userStateService = userStateService;
        }

        public async Task LoadPaymentsAsync()
        {
            IsLoading = true;
            ErrorMessage = null;
            SuccessMessage = null;
            NotifyStateChanged();

            try
            {
                if (!EnsureAdminAuthenticated())
                {
                    ErrorMessage = "You do not have permission to view payments.";
                    return;
                }

                var response = await _httpClient.GetAsync("api/admin/payments");

                var responseData = await response.Content
                    .ReadFromJsonAsync<ResponseTypeDTO<List<AdminPaymentResponseDTO>>>();

                if (!response.IsSuccessStatusCode ||
                    responseData == null ||
                    responseData.StatusCode != 200 ||
                    responseData.Content == null)
                {
                    Payments = new List<AdminPaymentResponseDTO>();
                    ErrorMessage = responseData?.Message ?? "Failed to load payments.";
                    return;
                }

                Payments = responseData.Content
                    .OrderByDescending(payment => payment.CreatedAt)
                    .ToList();
            }
            catch (Exception ex)
            {
                Payments = new List<AdminPaymentResponseDTO>();
                ErrorMessage = $"Error loading payments: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
                NotifyStateChanged();
            }
        }

        public async Task LoadPaymentDetailAsync(int paymentTransactionId)
        {
            IsLoading = true;
            ErrorMessage = null;
            SuccessMessage = null;
            SelectedPayment = null;
            NotifyStateChanged();

            try
            {
                if (!EnsureAdminAuthenticated())
                {
                    ErrorMessage = "You do not have permission to view payment detail.";
                    return;
                }

                var response = await _httpClient.GetAsync(
                    $"api/admin/payments/{paymentTransactionId}"
                );

                var responseData = await response.Content
                    .ReadFromJsonAsync<ResponseTypeDTO<AdminPaymentResponseDTO>>();

                if (!response.IsSuccessStatusCode ||
                    responseData == null ||
                    responseData.StatusCode != 200 ||
                    responseData.Content == null)
                {
                    ErrorMessage = responseData?.Message ?? "Failed to load payment detail.";
                    return;
                }

                SelectedPayment = responseData.Content;
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error loading payment detail: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
                NotifyStateChanged();
            }
        }

        public async Task LoadPaymentsByOrderIdAsync(int orderId)
        {
            IsLoading = true;
            ErrorMessage = null;
            SuccessMessage = null;
            NotifyStateChanged();

            try
            {
                if (!EnsureAdminAuthenticated())
                {
                    ErrorMessage = "You do not have permission to view order payments.";
                    return;
                }

                var response = await _httpClient.GetAsync(
                    $"api/admin/payments/order/{orderId}"
                );

                var responseData = await response.Content
                    .ReadFromJsonAsync<ResponseTypeDTO<List<AdminPaymentResponseDTO>>>();

                if (!response.IsSuccessStatusCode ||
                    responseData == null ||
                    responseData.StatusCode != 200 ||
                    responseData.Content == null)
                {
                    Payments = new List<AdminPaymentResponseDTO>();
                    ErrorMessage = responseData?.Message ?? "Failed to load order payments.";
                    return;
                }

                Payments = responseData.Content
                    .OrderByDescending(payment => payment.CreatedAt)
                    .ToList();
            }
            catch (Exception ex)
            {
                Payments = new List<AdminPaymentResponseDTO>();
                ErrorMessage = $"Error loading order payments: {ex.Message}";
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