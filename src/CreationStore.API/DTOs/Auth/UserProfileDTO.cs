namespace CreationStore.API.DTOs.Auth
{
    public class UserProfileDTO
    {
        public int UserId { get; set; }

        public string Username { get; set; } = string.Empty;

        public string FullName { get; set; } = string.Empty;

        public string? Email { get; set; }

        public string? Phone { get; set; }

        public List<string> Roles { get; set; } = new();
    }
}