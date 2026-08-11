using System.ComponentModel.DataAnnotations;

namespace CreationStore.Blazor.DTOs.Auth
{
    public class RegisterDTO
    {
        [Required(ErrorMessage = "Please enter your username")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please enter your password")]
        public string Password { get; set; } = string.Empty;

        public string? FullName { get; set; }

        public string? Email { get; set; }

        public string? Phone { get; set; }
    }
}