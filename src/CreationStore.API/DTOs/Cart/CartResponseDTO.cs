namespace CreationStore.API.DTOs.Cart
{
    public class CartResponseDTO
    {
        public int CartId { get; set; }

        public string Status { get; set; } = string.Empty;

        public List<CartItemResponseDTO> Items { get; set; } = new();

        public int TotalItems { get; set; }

        public decimal TotalAmount { get; set; }
    }
}