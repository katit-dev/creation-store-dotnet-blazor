using System.Net.Http.Headers;
using System.Net.Http.Json;
using CreationStore.Blazor.DTOs.Admin.Users;
using CreationStore.Blazor.DTOs.Common;

namespace CreationStore.Blazor.Services
{
    public class AdminUserStateService
    {
        private readonly HttpClient _httpClient;
        private readonly UserStateService _userStateService;

        public List<AdminUserResponseDTO> Users { get; private set; } = new();

        public bool IsLoading { get; private set; }

        public string? ErrorMessage { get; private set; }

        public string? SuccessMessage { get; private set; }

        public Action? OnChange { get; set; }

        public AdminUserStateService(
            HttpClient httpClient,
            UserStateService userStateService
        )
        {
            _httpClient = httpClient;
            _userStateService = userStateService;
        }

        public async Task LoadUsersAsync()
        {
            IsLoading = true;
            ErrorMessage = null;
            SuccessMessage = null;
            NotifyStateChanged();

            try
            {
                if (!EnsureAdminAuthenticated())
                {
                    ErrorMessage = "You do not have permission to manage users.";
                    return;
                }

                var response = await _httpClient.GetAsync("api/admin/users");

                var responseData = await response.Content
                    .ReadFromJsonAsync<ResponseTypeDTO<List<AdminUserResponseDTO>>>();

                if (!response.IsSuccessStatusCode ||
                    responseData == null ||
                    responseData.StatusCode != 200 ||
                    responseData.Content == null)
                {
                    Users = new List<AdminUserResponseDTO>();
                    ErrorMessage = responseData?.Message ?? "Failed to load users.";
                    return;
                }

                Users = responseData.Content;
            }
            catch (Exception ex)
            {
                Users = new List<AdminUserResponseDTO>();
                ErrorMessage = $"Error loading users: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
                NotifyStateChanged();
            }
        }

        public async Task<bool> ActivateUserAsync(int userId)
        {
            IsLoading = true;
            ErrorMessage = null;
            SuccessMessage = null;
            NotifyStateChanged();

            try
            {
                if (!EnsureAdminAuthenticated())
                {
                    ErrorMessage = "You do not have permission to activate users.";
                    return false;
                }

                var response = await _httpClient.PutAsync(
                    $"api/admin/users/{userId}/activate",
                    null
                );

                var responseData = await response.Content
                    .ReadFromJsonAsync<ResponseTypeDTO<AdminUserResponseDTO>>();

                if (!response.IsSuccessStatusCode ||
                    responseData == null ||
                    responseData.StatusCode < 200 ||
                    responseData.StatusCode >= 300 ||
                    responseData.Content == null)
                {
                    ErrorMessage = responseData?.Message ?? "Failed to activate user.";
                    return false;
                }

                SuccessMessage = responseData.Message ?? "User activated successfully.";

                await LoadUsersAsync();

                return true;
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error activating user: {ex.Message}";
                return false;
            }
            finally
            {
                IsLoading = false;
                NotifyStateChanged();
            }
        }

        public async Task<bool> DeactivateUserAsync(int userId)
        {
            IsLoading = true;
            ErrorMessage = null;
            SuccessMessage = null;
            NotifyStateChanged();

            try
            {
                if (!EnsureAdminAuthenticated())
                {
                    ErrorMessage = "You do not have permission to deactivate users.";
                    return false;
                }

                var response = await _httpClient.PutAsync(
                    $"api/admin/users/{userId}/deactivate",
                    null
                );

                var responseData = await response.Content
                    .ReadFromJsonAsync<ResponseTypeDTO<AdminUserResponseDTO>>();

                if (!response.IsSuccessStatusCode ||
                    responseData == null ||
                    responseData.StatusCode < 200 ||
                    responseData.StatusCode >= 300 ||
                    responseData.Content == null)
                {
                    ErrorMessage = responseData?.Message ?? "Failed to deactivate user.";
                    return false;
                }

                SuccessMessage = responseData.Message ?? "User deactivated successfully.";

                await LoadUsersAsync();

                return true;
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error deactivating user: {ex.Message}";
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