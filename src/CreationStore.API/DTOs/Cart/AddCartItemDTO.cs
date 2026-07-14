using System.ComponentModel.DataAnnotations;

namespace CreationStore.API.DTOs.Cart
{
    public class AddCartItemDTO
    {
        [Required(ErrorMessage = "ProductId is required")]
        public int ProductId { get; set; }

        [Range(1, 99, ErrorMessage = "Quantity must be between 1 and 99")]
        public int Quantity { get; set; } = 1;
    }
}