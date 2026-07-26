using Microsoft.AspNetCore.Http;

namespace CreationStore.API.Services.Interfaces
{
    public interface IVnPayService
    {
        string CreatePaymentUrl(
            string vnpTxnRef,
            decimal amount,
            string orderInfo,
            string ipAddress
        );

        bool ValidateSignature(IQueryCollection query);

        string GetIpAddress(HttpContext? httpContext);
    }
}