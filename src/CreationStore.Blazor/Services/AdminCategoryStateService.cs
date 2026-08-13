using System.Net.Http.Headers;
using System.Net.Http.Json;
using CreationStore.Blazor.DTOs.Categories;
using CreationStore.Blazor.DTOs.Common;

namespace CreationStore.Blazor.Services
{
    public class AdminCategoryStateService
    {
        private readonly HttpClient _httpClient;
        private readonly UserStateService _userStateService;

        public List<CategoryResponseDTO> Categories { get; private set; } = new();

        public bool IsLoading { get; private set; }

        public string? ErrorMessage { get; private set; }

        public string? SuccessMessage { get; private set; }

        public Action? OnChange { get; set; }

        public AdminCategoryStateService(
            HttpClient httpClient,
            UserStateService userStateService
        )
        {
            _httpClient = httpClient;
            _userStateService = userStateService;
        }

        public async Task LoadCategoriesAsync()
        {
            IsLoading = true;
            ErrorMessage = null;
            SuccessMessage = null;
            NotifyStateChanged();

            try
            {
                if (!EnsureAdminAuthenticated())
                {
                    ErrorMessage = "You do not have permission to manage categories.";
                    return;
                }

                var response = await _httpClient.GetAsync("api/categories");

                var responseData = await response.Content
                    .ReadFromJsonAsync<ResponseTypeDTO<List<CategoryResponseDTO>>>();

                if (!response.IsSuccessStatusCode ||
                    responseData == null ||
                    responseData.StatusCode != 200 ||
                    responseData.Content == null)
                {
                    Categories = new List<CategoryResponseDTO>();
                    ErrorMessage = responseData?.Message ?? "Failed to load categories.";
                    return;
                }

                Categories = responseData.Content;
            }
            catch (Exception ex)
            {
                Categories = new List<CategoryResponseDTO>();
                ErrorMessage = $"Error loading categories: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
                NotifyStateChanged();
            }
        }

        public async Task<bool> CreateCategoryAsync(CategoryCreateDTO dto)
        {
            IsLoading = true;
            ErrorMessage = null;
            SuccessMessage = null;
            NotifyStateChanged();

            try
            {
                if (!EnsureAdminAuthenticated())
                {
                    ErrorMessage = "You do not have permission to create categories.";
                    return false;
                }

                var response = await _httpClient.PostAsJsonAsync(
                    "api/admin/categories",
                    dto
                );

                var responseData = await response.Content
                    .ReadFromJsonAsync<ResponseTypeDTO<CategoryResponseDTO>>();

                if (!response.IsSuccessStatusCode ||
                    responseData == null ||
                    responseData.StatusCode < 200 ||
                    responseData.StatusCode >= 300 ||
                    responseData.Content == null)
                {
                    ErrorMessage = responseData?.Message ?? "Failed to create category.";
                    return false;
                }

                SuccessMessage = responseData.Message ?? "Category created successfully.";

                await LoadCategoriesAsync();

                return true;
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error creating category: {ex.Message}";
                return false;
            }
            finally
            {
                IsLoading = false;
                NotifyStateChanged();
            }
        }

        public async Task<bool> UpdateCategoryAsync(
            int categoryId,
            CategoryUpdateDTO dto
        )
        {
            IsLoading = true;
            ErrorMessage = null;
            SuccessMessage = null;
            NotifyStateChanged();

            try
            {
                if (!EnsureAdminAuthenticated())
                {
                    ErrorMessage = "You do not have permission to update categories.";
                    return false;
                }

                var response = await _httpClient.PutAsJsonAsync(
                    $"api/admin/categories/{categoryId}",
                    dto
                );

                var responseData = await response.Content
                    .ReadFromJsonAsync<ResponseTypeDTO<CategoryResponseDTO>>();

                if (!response.IsSuccessStatusCode ||
                    responseData == null ||
                    responseData.StatusCode < 200 ||
                    responseData.StatusCode >= 300 ||
                    responseData.Content == null)
                {
                    ErrorMessage = responseData?.Message ?? "Failed to update category.";
                    return false;
                }

                SuccessMessage = responseData.Message ?? "Category updated successfully.";

                await LoadCategoriesAsync();

                return true;
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error updating category: {ex.Message}";
                return false;
            }
            finally
            {
                IsLoading = false;
                NotifyStateChanged();
            }
        }

        public async Task<bool> DeleteCategoryAsync(int categoryId)
        {
            IsLoading = true;
            ErrorMessage = null;
            SuccessMessage = null;
            NotifyStateChanged();

            try
            {
                if (!EnsureAdminAuthenticated())
                {
                    ErrorMessage = "You do not have permission to delete categories.";
                    return false;
                }

                var response = await _httpClient.DeleteAsync(
                    $"api/admin/categories/{categoryId}"
                );

                var responseData = await response.Content
                    .ReadFromJsonAsync<ResponseTypeDTO<bool>>();

                if (!response.IsSuccessStatusCode ||
                    responseData == null ||
                    responseData.StatusCode < 200 ||
                    responseData.StatusCode >= 300 ||
                    responseData.Content == false)
                {
                    ErrorMessage = responseData?.Message ?? "Failed to delete category.";
                    return false;
                }

                SuccessMessage = responseData.Message ?? "Category deleted successfully.";

                await LoadCategoriesAsync();

                return true;
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error deleting category: {ex.Message}";
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