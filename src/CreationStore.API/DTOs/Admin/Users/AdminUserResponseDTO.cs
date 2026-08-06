namespace CreationStore.API.DTOs.Admin.Users
{
    public class AdminUserResponseDTO
    {
        public int UserId { get; set; }

        public string Username { get; set; } = string.Empty;

        public string? FullName { get; set; }

        public string? Email { get; set; }

        public string? Phone { get; set; }

        public List<int> RoleIds { get; set; } = new();

        public List<string> Roles { get; set; } = new();
    }
}