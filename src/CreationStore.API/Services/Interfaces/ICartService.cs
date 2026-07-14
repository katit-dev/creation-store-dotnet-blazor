using CreationStore.API.DTOs.Cart;
using CreationStore.API.DTOs.ResponseTypes;

namespace CreationStore.API.Services.Interfaces
{
    public interface ICartService
    {
        Task<ResponseTypeDTO<CartResponseDTO>> GetMyCartAsync();

        Task<ResponseTypeDTO<CartResponseDTO>> AddItemAsync(AddCartItemDTO dto);

        Task<ResponseTypeDTO<CartResponseDTO>> UpdateItemAsync(
            int cartItemId,
            UpdateCartItemDTO dto
        );

        Task<ResponseTypeDTO<CartResponseDTO>> RemoveItemAsync(int cartItemId);

        Task<ResponseTypeDTO<CartResponseDTO>> ClearCartAsync();
    }
}