namespace CreationStore.API.DTOs.Products
{
    public class ProductUpdateDTO
    {
        public string ProductName { get; set; } = string.Empty;

        public string? Description { get; set; }

        public decimal Price { get; set; }

        public string? ImageUrl { get; set; }

        public int? ValidityDays { get; set; }

        public int CategoryId { get; set; }
    }
}