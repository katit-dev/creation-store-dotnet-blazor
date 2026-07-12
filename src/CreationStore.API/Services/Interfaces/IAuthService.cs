using CreationStore.API.DTOs.Auth;
using CreationStore.API.DTOs.ResponseTypes;

namespace CreationStore.API.Services.Interfaces
{
    public interface IAuthService
    {
        Task<ResponseTypeDTO<RegisterResponseDTO>> RegisterAsync(RegisterDTO dto);

        Task<ResponseTypeDTO<LoginResponseDTO>> LoginAsync(LoginDTO dto);

        Task<ResponseTypeDTO<UserProfileDTO>> GetMeAsync();
    }
}