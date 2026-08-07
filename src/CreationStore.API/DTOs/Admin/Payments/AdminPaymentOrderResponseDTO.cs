namespace CreationStore.API.DTOs.Admin.Payments
{
    public class AdminPaymentOrderResponseDTO
    {
        public int OrderId { get; set; }

        public decimal TotalAmount { get; set; }

        public string Status { get; set; } = string.Empty;

        public string PaymentStatus { get; set; } = string.Empty;

        public DateTime OrderDate { get; set; }
    }
}