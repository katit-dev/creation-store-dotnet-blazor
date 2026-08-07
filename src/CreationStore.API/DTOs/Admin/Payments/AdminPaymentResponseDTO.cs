namespace CreationStore.API.DTOs.Admin.Payments
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

        public string VnpTxnRef { get; set; } = string.Empty;

        public string? VnpTransactionNo { get; set; }

        public string? VnpResponseCode { get; set; }

        public string? VnpTransactionStatus { get; set; }

        public string? VnpBankCode { get; set; }

        public string? VnpPayDate { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? PaidAt { get; set; }
    }
}