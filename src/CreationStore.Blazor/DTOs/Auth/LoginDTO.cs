using System.ComponentModel.DataAnnotations;

namespace CreationStore.Blazor.DTOs.Auth
{
    public class LoginDTO
    {
        [Required(ErrorMessage = "Vui lòng nhập username, email hoặc số điện thoại")]
        public string LoginIdentifier { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu")]
        public string Password { get; set; } = string.Empty;
    }
}