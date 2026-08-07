using CreationStore.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CreationStore.API.Controllers.Admin
{
    [Authorize(Roles = "Admin")]
    [Route("api/admin/payments")]
    [ApiController]
    public class AdminPaymentController : ControllerBase
    {
        private readonly IAdminPaymentService _adminPaymentService;

        public AdminPaymentController(
            IAdminPaymentService adminPaymentService
        )
        {
            _adminPaymentService = adminPaymentService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllPayments()
        {
            var result = await _adminPaymentService.GetAllPaymentsAsync();
            return StatusCode(result.StatusCode, result);
        }

        [HttpGet("{paymentTransactionId:int}")]
        public async Task<IActionResult> GetPaymentById(
            int paymentTransactionId
        )
        {
            var result = await _adminPaymentService
                .GetPaymentByIdAsync(paymentTransactionId);

            return StatusCode(result.StatusCode, result);
        }

        [HttpGet("order/{orderId:int}")]
        public async Task<IActionResult> GetPaymentsByOrderId(int orderId)
        {
            var result = await _adminPaymentService
                .GetPaymentsByOrderIdAsync(orderId);

            return StatusCode(result.StatusCode, result);
        }
    }
}