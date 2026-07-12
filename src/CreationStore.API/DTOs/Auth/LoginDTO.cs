using System.ComponentModel.DataAnnotations;

namespace CreationStore.API.DTOs.Auth
{
    public class LoginDTO
    {
        [Required(ErrorMessage = "Username, email or phone is required")]
        public string LoginIdentifier { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required")]
        public string Password { get; set; } = string.Empty;
    }
}