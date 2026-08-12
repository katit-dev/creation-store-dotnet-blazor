namespace CreationStore.Blazor.DTOs.Payments
{
    public class VnPayPaymentResponseDTO
    {
        public int PaymentTransactionId { get; set; }

        public int OrderId { get; set; }

        public decimal Amount { get; set; }

        public string VnpTxnRef { get; set; } = string.Empty;

        public string PaymentUrl { get; set; } = string.Empty;
    }
}