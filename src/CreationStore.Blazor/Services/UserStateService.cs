using System.Net.Http.Headers;
using System.Net.Http.Json;
using CreationStore.Blazor.DTOs.Auth;
using CreationStore.Blazor.DTOs.Common;
using CreationStore.Blazor.Helpers;
using Microsoft.AspNetCore.Components;

namespace CreationStore.Blazor.Services
{
    public class UserStateService
    {
        private readonly HttpClient _httpClient;
        private readonly TokenStorage _tokenStorage;
        private readonly NavigationManager _navigationManager;

        public string AccessToken { get; private set; } = string.Empty;

        public ProfileUserDTO? CurrentUser { get; private set; }

        public bool IsAuthenticated =>
            !string.IsNullOrWhiteSpace(AccessToken);

        public bool IsAdmin =>
            CurrentUser?.Roles.Any(role =>
                role.Equals("Admin", StringComparison.OrdinalIgnoreCase)
            ) == true;

        public Action? OnChange { get; set; }

        public UserStateService(
            HttpClient httpClient,
            TokenStorage tokenStorage,
            NavigationManager navigationManager
        )
        {
            _httpClient = httpClient;
            _tokenStorage = tokenStorage;
            _navigationManager = navigationManager;
        }

        public async Task LoginAsync(LoginDTO loginDto)
        {
            var response = await _httpClient.PostAsJsonAsync(
                "api/auth/login",
                loginDto
            );

            var responseData = await response.Content
                .ReadFromJsonAsync<ResponseTypeDTO<LoginResponseDTO>>();

            if (!response.IsSuccessStatusCode ||
                responseData == null ||
                responseData.StatusCode != 200 ||
                responseData.Content == null ||
                string.IsNullOrWhiteSpace(responseData.Content.Token))
            {
                throw new Exception(
                    responseData?.Message ?? "Login failed"
                );
            }

            AccessToken = responseData.Content.Token;

            await _tokenStorage.SaveTokenAsync(AccessToken);

            SetAuthorizationHeader(AccessToken);

            await GetProfileAsync();

            NotifyStateChanged();

            if (IsAdmin)
            {
                _navigationManager.NavigateTo("/admin/dashboard");
                return;
            }

            _navigationManager.NavigateTo("/");
        }

        public async Task RegisterAsync(RegisterDTO registerDto)
        {
            var response = await _httpClient.PostAsJsonAsync(
                "api/auth/register",
                registerDto
            );

            var responseData = await response.Content
                .ReadFromJsonAsync<ResponseTypeDTO<object>>();

            if (!response.IsSuccessStatusCode ||
                responseData == null ||
                responseData.StatusCode != 201)
            {
                throw new Exception(
                    responseData?.Message ?? "Registration failed"
                );
            }
        }

        public async Task GetProfileAsync()
        {
            var token = await _tokenStorage.GetTokenAsync();

            if (string.IsNullOrWhiteSpace(token))
            {
                ClearUserState();
                NotifyStateChanged();
                return;
            }

            AccessToken = token;

            SetAuthorizationHeader(token);

            var response = await _httpClient.GetAsync("api/auth/me");

            if (!response.IsSuccessStatusCode)
            {
                ClearUserState();

                await _tokenStorage.RemoveTokenAsync();

                NotifyStateChanged();

                return;
            }

            var responseData = await response.Content
                .ReadFromJsonAsync<ResponseTypeDTO<ProfileUserDTO>>();

            if (responseData == null ||
                responseData.StatusCode != 200 ||
                responseData.Content == null)
            {
                ClearUserState();
                NotifyStateChanged();
                return;
            }

            CurrentUser = responseData.Content;

            NotifyStateChanged();
        }

        public async Task UpdateProfileAsync(UpdateProfileDTO updateProfileDto)
        {
            var token = AccessToken;

            if (string.IsNullOrWhiteSpace(token))
            {
                token = await _tokenStorage.GetTokenAsync() ?? string.Empty;
            }

            if (string.IsNullOrWhiteSpace(token))
            {
                throw new Exception("Please login before updating your profile.");
            }

            AccessToken = token;

            SetAuthorizationHeader(token);

            var response = await _httpClient.PutAsJsonAsync(
                "api/auth/me",
                updateProfileDto
            );

            var responseData = await response.Content
                .ReadFromJsonAsync<ResponseTypeDTO<ProfileUserDTO>>();

            if (!response.IsSuccessStatusCode ||
                responseData == null ||
                responseData.StatusCode < 200 ||
                responseData.StatusCode >= 300 ||
                responseData.Content == null)
            {
                throw new Exception(
                    responseData?.Message ?? "Failed to update profile."
                );
            }

            CurrentUser = responseData.Content;

            NotifyStateChanged();
        }

        public async Task LogoutAsync()
        {
            ClearUserState();

            await _tokenStorage.RemoveTokenAsync();

            _httpClient.DefaultRequestHeaders.Authorization = null;

            NotifyStateChanged();

            _navigationManager.NavigateTo("/login");
        }

        private void SetAuthorizationHeader(string token)
        {
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);
        }

        private void ClearUserState()
        {
            AccessToken = string.Empty;
            CurrentUser = null;
        }

        private void NotifyStateChanged()
        {
            OnChange?.Invoke();
        }
    }
}