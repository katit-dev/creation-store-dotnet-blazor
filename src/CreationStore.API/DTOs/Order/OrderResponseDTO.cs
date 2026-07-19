namespace CreationStore.API.DTOs.Order
{
    public class OrderResponseDTO
    {
        public int OrderId { get; set; }

        public decimal TotalAmount { get; set; }

        public string Status { get; set; } = string.Empty;

        public string PaymentStatus { get; set; } = string.Empty;

        public DateTime OrderDate { get; set; }

        public string? Note { get; set; }

        public DateTime? CancelledAt { get; set; }

        public string? CancelReason { get; set; }

        public List<OrderItemResponseDTO> Items { get; set; } = new();
    }
}