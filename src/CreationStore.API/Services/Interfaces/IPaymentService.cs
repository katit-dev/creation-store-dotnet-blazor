using CreationStore.API.DTOs.Payment;
using CreationStore.API.DTOs.ResponseTypes;
using Microsoft.AspNetCore.Http;

namespace CreationStore.API.Services.Interfaces
{
    public interface IPaymentService
    {
        Task<ResponseTypeDTO<CreateVnPayPaymentResponseDTO>>
            CreateVnPayPaymentAsync(int orderId);

        Task<ResponseTypeDTO<VnPayReturnResponseDTO>>
            HandleVnPayReturnAsync(IQueryCollection query);

        Task<ResponseTypeDTO<List<PaymentTransactionResponseDTO>>>
            GetMyTransactionsAsync();

        Task<ResponseTypeDTO<PaymentTransactionResponseDTO>>
            GetMyTransactionByIdAsync(int paymentTransactionId);
    }
}