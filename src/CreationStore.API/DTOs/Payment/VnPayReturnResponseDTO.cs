namespace CreationStore.API.DTOs.Payment
{
    public class VnPayReturnResponseDTO
    {
        public bool IsValidSignature { get; set; }

        public bool IsSuccess { get; set; }

        public string Message { get; set; } = string.Empty;

        public PaymentTransactionResponseDTO? Transaction { get; set; }
    }
}