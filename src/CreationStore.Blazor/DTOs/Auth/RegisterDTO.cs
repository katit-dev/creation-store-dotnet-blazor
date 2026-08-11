using System.ComponentModel.DataAnnotations;

namespace CreationStore.Blazor.DTOs.Auth
{
    public class RegisterDTO
    {
        [Required(ErrorMessage = "Vui lòng nhập username")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu")]
        public string Password { get; set; } = string.Empty;

        public string? FullName { get; set; }

        public string? Email { get; set; }

        public string? Phone { get; set; }
    }
}