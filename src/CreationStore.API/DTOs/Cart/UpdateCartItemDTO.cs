using System.ComponentModel.DataAnnotations;

namespace CreationStore.API.DTOs.Cart
{
    public class UpdateCartItemDTO
    {
        [Range(1, 99, ErrorMessage = "Quantity must be between 1 and 99")]
        public int Quantity { get; set; }
    }
}