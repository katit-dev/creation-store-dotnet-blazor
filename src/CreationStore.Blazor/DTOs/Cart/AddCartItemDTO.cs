namespace CreationStore.Blazor.DTOs.Cart
{
    public class AddCartItemDTO
    {
        public int ProductId { get; set; }

        public int Quantity { get; set; } = 1;
    }
}