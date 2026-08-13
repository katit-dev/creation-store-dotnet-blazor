using CreationStore.API.DTOs.Admin.Users;
using CreationStore.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CreationStore.API.Controllers.Admin
{
    [Authorize(Roles = "Admin")]
    [Route("api/admin/users")]
    [ApiController]
    public class AdminUserController : ControllerBase
    {
        private readonly IAdminUserService _adminUserService;

        public AdminUserController(IAdminUserService adminUserService)
        {
            _adminUserService = adminUserService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllUsers()
        {
            var result = await _adminUserService.GetAllUsersAsync();

            return StatusCode(result.StatusCode, result);
        }

        [HttpGet("{userId:int}")]
        public async Task<IActionResult> GetUserById(int userId)
        {
            var result = await _adminUserService.GetUserByIdAsync(userId);

            return StatusCode(result.StatusCode, result);
        }

        [HttpPut("{userId:int}/role")]
        public async Task<IActionResult> ChangeUserRole(
            int userId,
            [FromBody] AdminChangeUserRoleDTO dto
        )
        {
            var result = await _adminUserService.ChangeUserRoleAsync(
                userId,
                dto
            );

            return StatusCode(result.StatusCode, result);
        }

        [HttpPut("{userId:int}/activate")]
        public async Task<IActionResult> ActivateUser(int userId)
        {
            var result = await _adminUserService.ActivateUserAsync(userId);

            return StatusCode(result.StatusCode, result);
        }

        [HttpPut("{userId:int}/deactivate")]
        public async Task<IActionResult> DeactivateUser(int userId)
        {
            var result = await _adminUserService.DeactivateUserAsync(userId);

            return StatusCode(result.StatusCode, result);
        }
    }
}