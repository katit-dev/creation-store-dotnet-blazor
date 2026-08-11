using Microsoft.JSInterop;

namespace CreationStore.Blazor.Helpers
{
    public class TokenStorage
    {
        private const string TokenKey = "creation_store_access_token";

        private readonly IJSRuntime _jsRuntime;

        public TokenStorage(IJSRuntime jsRuntime)
        {
            _jsRuntime = jsRuntime;
        }

        public async Task SaveTokenAsync(string token)
        {
            await _jsRuntime.InvokeVoidAsync(
                "localStorage.setItem",
                TokenKey,
                token
            );
        }

        public async Task<string?> GetTokenAsync()
        {
            return await _jsRuntime.InvokeAsync<string?>(
                "localStorage.getItem",
                TokenKey
            );
        }

        public async Task RemoveTokenAsync()
        {
            await _jsRuntime.InvokeVoidAsync(
                "localStorage.removeItem",
                TokenKey
            );
        }
    }
}