namespace CreationStore.API.DTOs.Payment
{
    public class PaymentTransactionResponseDTO
    {
        public int PaymentTransactionId { get; set; }

        public int OrderId { get; set; }

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