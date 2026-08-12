namespace CreationStore.Blazor.DTOs.Admin.Dashboard
{
    public class AdminRecentOrderDTO
    {
        public int OrderId { get; set; }

        public string Username { get; set; } = string.Empty;

        public string CustomerName { get; set; } = string.Empty;

        public decimal TotalAmount { get; set; }

        public string Status { get; set; } = string.Empty;

        public string PaymentStatus { get; set; } = string.Empty;

        public DateTime OrderDate { get; set; }

        public string DisplayCustomerName =>
            !string.IsNullOrWhiteSpace(CustomerName)
                ? CustomerName
                : Username;
    }
}