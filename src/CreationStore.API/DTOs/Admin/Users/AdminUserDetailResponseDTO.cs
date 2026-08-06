namespace CreationStore.API.DTOs.Admin.Users
{
    public class AdminUserDetailResponseDTO : AdminUserResponseDTO
    {
        public int OrderCount { get; set; }

        public decimal TotalSpent { get; set; }
    }
}