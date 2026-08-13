namespace CreationStore.Blazor.DTOs.Admin.Users
{
    public class AdminUserResponseDTO
    {
        public int UserId { get; set; }

        public string Username { get; set; } = string.Empty;

        public string? FullName { get; set; }

        public string? Email { get; set; }

        public string? Phone { get; set; }

        public bool IsActive { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }

        public List<int> RoleIds { get; set; } = new();

        public List<string> Roles { get; set; } = new();
    }
}