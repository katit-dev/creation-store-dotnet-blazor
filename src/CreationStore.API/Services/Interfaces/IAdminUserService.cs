using CreationStore.API.DTOs.Admin.Users;
using CreationStore.API.DTOs.ResponseTypes;

namespace CreationStore.API.Services.Interfaces
{
    public interface IAdminUserService
    {
        Task<ResponseTypeDTO<List<AdminUserResponseDTO>>> GetAllUsersAsync();

        Task<ResponseTypeDTO<AdminUserDetailResponseDTO>> GetUserByIdAsync(
            int userId
        );

        Task<ResponseTypeDTO<AdminUserResponseDTO>> ChangeUserRoleAsync(
            int userId,
            AdminChangeUserRoleDTO dto
        );
    }
}