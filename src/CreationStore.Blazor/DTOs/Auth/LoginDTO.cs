using System.ComponentModel.DataAnnotations;

namespace CreationStore.Blazor.DTOs.Auth
{
    public class LoginDTO
    {
        [Required(ErrorMessage = "Please enter your username, email or phone")]
        public string LoginIdentifier { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please enter your password")]
        public string Password { get; set; } = string.Empty;
    }
}