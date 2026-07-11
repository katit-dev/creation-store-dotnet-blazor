namespace CreationStore.API.DTOs.Auth
{
    public class RegisterDTO
    {
        public string Username { get; set; } = string.Empty;

        public string Password { get; set; } = string.Empty;

        public string FullName { get; set; } = string.Empty;

        public string? Email { get; set; }

        public string? Phone { get; set; }
    }
}