using CreationStore.API.DTOs.Admin.Orders;
using CreationStore.API.DTOs.Order;
using CreationStore.API.DTOs.ResponseTypes;

namespace CreationStore.API.Services.Interfaces
{
    public interface IAdminOrderService
    {
        Task<ResponseTypeDTO<List<AdminOrderResponseDTO>>> GetAllOrdersAsync();

        Task<ResponseTypeDTO<AdminOrderResponseDTO>> GetOrderByIdAsync(
            int orderId
        );

        Task<ResponseTypeDTO<AdminOrderResponseDTO>> CompleteOrderAsync(
            int orderId
        );

        Task<ResponseTypeDTO<AdminOrderResponseDTO>> CancelOrderAsync(
            int orderId,
            CancelOrderDTO dto
        );
    }
}