namespace CreationStore.API.DTOs.Payment
{
    public class CreateVnPayPaymentResponseDTO
    {
        public int PaymentTransactionId { get; set; }

        public int OrderId { get; set; }

        public decimal Amount { get; set; }

        public string VnpTxnRef { get; set; } = string.Empty;

        public string PaymentUrl { get; set; } = string.Empty;
    }
}