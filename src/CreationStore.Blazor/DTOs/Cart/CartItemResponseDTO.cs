namespace CreationStore.Blazor.DTOs.Cart
{
    public class CartItemResponseDTO
    {
        public int CartItemId { get; set; }

        public int ProductId { get; set; }

        public string ProductName { get; set; } = string.Empty;

        public int Quantity { get; set; }

        public decimal UnitPrice { get; set; }

        public decimal Price { get; set; }

        public decimal SubTotal { get; set; }

        public decimal DisplayUnitPrice =>
            UnitPrice > 0 ? UnitPrice : Price;

        public decimal DisplaySubTotal =>
            SubTotal > 0 ? SubTotal : DisplayUnitPrice * Quantity;
    }
}