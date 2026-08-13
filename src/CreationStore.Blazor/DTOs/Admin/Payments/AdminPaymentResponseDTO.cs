namespace CreationStore.Blazor.DTOs.Admin.Payments
{
    public class AdminPaymentResponseDTO
    {
        public int PaymentTransactionId { get; set; }

        public int OrderId { get; set; }

        public AdminPaymentOrderResponseDTO? Order { get; set; }

        public AdminPaymentUserResponseDTO? User { get; set; }

        public string PaymentMethod { get; set; } = string.Empty;

        public decimal Amount { get; set; }

        public string TransactionStatus { get; set; } = string.Empty;

        public string? VnpTxnRef { get; set; }

        public string? VnpTransactionNo { get; set; }

        public string? VnpResponseCode { get; set; }

        public string? VnpTransactionStatus { get; set; }

        public string? VnpBankCode { get; set; }

        public string? VnpPayDate { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? PaidAt { get; set; }
    }

    public class AdminPaymentOrderResponseDTO
    {
        public int OrderId { get; set; }

        public decimal TotalAmount { get; set; }

        public string Status { get; set; } = string.Empty;

        public string PaymentStatus { get; set; } = string.Empty;

        public DateTime OrderDate { get; set; }
    }

    public class AdminPaymentUserResponseDTO
    {
        public int UserId { get; set; }

        public string Username { get; set; } = string.Empty;

        public string? FullName { get; set; }

        public string? Email { get; set; }

        public string? Phone { get; set; }
    }
}