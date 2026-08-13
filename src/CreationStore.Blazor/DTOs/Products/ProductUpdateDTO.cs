using System.ComponentModel.DataAnnotations;

namespace CreationStore.Blazor.DTOs.Products
{
    public class ProductUpdateDTO
    {
        [Required(ErrorMessage = "Product name is required")]
        public string ProductName { get; set; } = string.Empty;

        public string? Description { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Price must be greater than or equal to 0")]
        public decimal Price { get; set; }

        public string? ImageUrl { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Validity days must be greater than or equal to 0")]
        public int? ValidityDays { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Category ID is required")]
        public int CategoryId { get; set; }
    }
}