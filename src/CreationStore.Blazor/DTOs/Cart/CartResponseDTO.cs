namespace CreationStore.Blazor.DTOs.Cart
{
    public class CartResponseDTO
    {
        public int CartId { get; set; }

        public decimal TotalAmount { get; set; }

        public List<CartItemResponseDTO> Items { get; set; } = new();

        public decimal DisplayTotalAmount =>
            TotalAmount > 0
                ? TotalAmount
                : Items.Sum(item => item.DisplaySubTotal);
    }
}