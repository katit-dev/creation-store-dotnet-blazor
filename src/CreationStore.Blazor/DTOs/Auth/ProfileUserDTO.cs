namespace CreationStore.Blazor.DTOs.Auth
{
    public class ProfileUserDTO
    {
        public int UserId { get; set; }

        public string Username { get; set; } = string.Empty;

        public string? FullName { get; set; }

        public string? Email { get; set; }

        public string? Phone { get; set; }

        public List<string> Roles { get; set; } = new();
    }
}