using CreationStore.API.DTOs.Admin.Payments;
using CreationStore.API.DTOs.ResponseTypes;

namespace CreationStore.API.Services.Interfaces
{
    public interface IAdminPaymentService
    {
        Task<ResponseTypeDTO<List<AdminPaymentResponseDTO>>>
            GetAllPaymentsAsync();

        Task<ResponseTypeDTO<AdminPaymentResponseDTO>>
            GetPaymentByIdAsync(int paymentTransactionId);

        Task<ResponseTypeDTO<List<AdminPaymentResponseDTO>>>
            GetPaymentsByOrderIdAsync(int orderId);
    }
}