using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using CreationStore.Blazor.DTOs.Admin.Dashboard;
using CreationStore.Blazor.DTOs.Common;

namespace CreationStore.Blazor.Services
{
    public class AdminDashboardStateService
    {
        private readonly HttpClient _httpClient;
        private readonly UserStateService _userStateService;

        public AdminDashboardSummaryDTO? Summary { get; private set; }

        public List<AdminRevenueDTO> RevenueItems { get; private set; } = new();

        public List<AdminTopProductDTO> TopProducts { get; private set; } = new();

        public List<AdminRecentOrderDTO> RecentOrders { get; private set; } = new();

        public bool IsLoading { get; private set; }

        public string? ErrorMessage { get; private set; }

        public Action? OnChange { get; set; }

        public AdminDashboardStateService(
            HttpClient httpClient,
            UserStateService userStateService
        )
        {
            _httpClient = httpClient;
            _userStateService = userStateService;
        }

        public async Task LoadDashboardAsync(
            DateTime fromDate,
            DateTime toDate,
            int take = 5
        )
        {
            IsLoading = true;
            ErrorMessage = null;
            NotifyStateChanged();

            try
            {
                if (!EnsureAdminAuthenticated())
                {
                    ErrorMessage = "You do not have permission to view the admin dashboard.";
                    return;
                }

                await LoadSummaryAsync();
                await LoadRevenueAsync(fromDate, toDate);
                await LoadTopProductsAsync(take);
                await LoadRecentOrdersAsync(10);
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error loading dashboard: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
                NotifyStateChanged();
            }
        }

        private async Task LoadSummaryAsync()
        {
            var response = await _httpClient.GetAsync(
                "api/admin/dashboard/summary"
            );

            var responseData = await response.Content
                .ReadFromJsonAsync<ResponseTypeDTO<AdminDashboardSummaryDTO>>();

            if (!response.IsSuccessStatusCode ||
                responseData == null ||
                responseData.StatusCode != 200 ||
                responseData.Content == null)
            {
                throw new Exception(
                    responseData?.Message ?? "Failed to load dashboard summary."
                );
            }

            Summary = responseData.Content;
        }

        private async Task LoadRevenueAsync(DateTime fromDate, DateTime toDate)
        {
            var from = fromDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            var to = toDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

            var response = await _httpClient.GetAsync(
                $"api/admin/dashboard/revenue?fromDate={from}&toDate={to}"
            );

            var responseData = await response.Content
                .ReadFromJsonAsync<ResponseTypeDTO<AdminRevenueReportDTO>>();

            if (!response.IsSuccessStatusCode ||
                responseData == null ||
                responseData.StatusCode != 200 ||
                responseData.Content == null)
            {
                throw new Exception(
                    responseData?.Message ?? "Failed to load revenue data."
                );
            }

            RevenueItems = responseData.Content.Items;
        }

        private async Task LoadTopProductsAsync(int take)
        {
            var response = await _httpClient.GetAsync(
                $"api/admin/dashboard/top-products?take={take}"
            );

            var responseData = await response.Content
                .ReadFromJsonAsync<ResponseTypeDTO<List<AdminTopProductDTO>>>();

            if (!response.IsSuccessStatusCode ||
                responseData == null ||
                responseData.StatusCode != 200 ||
                responseData.Content == null)
            {
                throw new Exception(
                    responseData?.Message ?? "Failed to load top products."
                );
            }

            TopProducts = responseData.Content;
        }

        private async Task LoadRecentOrdersAsync(int take)
        {
            var response = await _httpClient.GetAsync(
                $"api/admin/dashboard/recent-orders?take={take}"
            );

            var responseData = await response.Content
                .ReadFromJsonAsync<ResponseTypeDTO<List<AdminRecentOrderDTO>>>();

            if (!response.IsSuccessStatusCode ||
                responseData == null ||
                responseData.StatusCode != 200 ||
                responseData.Content == null)
            {
                throw new Exception(
                    responseData?.Message ?? "Failed to load recent orders."
                );
            }

            RecentOrders = responseData.Content;
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