namespace CreationStore.API.DTOs.Admin.Payments
{
    public class AdminPaymentUserResponseDTO
    {
        public int UserId { get; set; }

        public string Username { get; set; } = string.Empty;

        public string? FullName { get; set; }

        public string? Email { get; set; }

        public string? Phone { get; set; }
    }
}