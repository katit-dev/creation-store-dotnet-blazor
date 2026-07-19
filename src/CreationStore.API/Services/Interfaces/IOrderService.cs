using CreationStore.API.DTOs.Order;
using CreationStore.API.DTOs.ResponseTypes;

namespace CreationStore.API.Services.Interfaces
{
    public interface IOrderService
    {
        Task<ResponseTypeDTO<OrderResponseDTO>> CheckoutAsync(
            CheckoutOrderDTO dto
        );

        Task<ResponseTypeDTO<List<OrderResponseDTO>>> GetMyOrdersAsync();

        Task<ResponseTypeDTO<OrderResponseDTO>> GetMyOrderByIdAsync(
            int orderId
        );

        Task<ResponseTypeDTO<OrderResponseDTO>> CancelOrderAsync(
            int orderId,
            CancelOrderDTO dto
        );
    }
}