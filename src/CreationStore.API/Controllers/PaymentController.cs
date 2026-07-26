using CreationStore.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CreationStore.API.Controllers
{
    [Route("api/payments")]
    [ApiController]
    public class PaymentController : ControllerBase
    {
        private readonly IPaymentService _paymentService;

        public PaymentController(IPaymentService paymentService)
        {
            _paymentService = paymentService;
        }

        // ============================================================
        // CREATE VNPAY PAYMENT
        // API:
        // POST /api/payments/vnpay/create-payment/{orderId}
        //
        // Mục đích:
        // - User bấm thanh toán cho một order
        // - Controller nhận orderId
        // - Gọi PaymentService tạo PaymentTransaction + paymentUrl
        // - Trả paymentUrl về cho frontend
        // ============================================================
        [Authorize]
        [HttpPost("vnpay/create-payment/{orderId}")]
        public async Task<IActionResult> CreateVnPayPayment(int orderId)
        {
            var result = await _paymentService
                .CreateVnPayPaymentAsync(orderId);

            return StatusCode(result.StatusCode, result);
        }

        // ============================================================
        // VNPAY RETURN
        // API:
        // GET /api/payments/vnpay-return
        //
        // Mục đích:
        // - Sau khi user thanh toán xong bên VNPAY
        // - VNPAY redirect về URL này kèm query string
        // - Backend verify chữ ký
        // - Backend update Order + PaymentTransaction
        //
        // Không dùng [Authorize]
        // Vì request này đến từ VNPAY/browser redirect,
        // không có JWT token của user.
        // ============================================================
        [AllowAnonymous]
        [HttpGet("vnpay-return")]
        public async Task<IActionResult> VnPayReturn()
        {
            var result = await _paymentService
                .HandleVnPayReturnAsync(Request.Query);

            return StatusCode(result.StatusCode, result);
        }

        // ============================================================
        // GET MY PAYMENT TRANSACTIONS
        // API:
        // GET /api/payments/my-transactions
        //
        // Mục đích:
        // - User xem lịch sử giao dịch thanh toán của mình
        // ============================================================
        [Authorize]
        [HttpGet("my-transactions")]
        public async Task<IActionResult> GetMyTransactions()
        {
            var result = await _paymentService
                .GetMyTransactionsAsync();

            return StatusCode(result.StatusCode, result);
        }

        // ============================================================
        // GET MY PAYMENT TRANSACTION BY ID
        // API:
        // GET /api/payments/{paymentTransactionId}
        //
        // Mục đích:
        // - User xem chi tiết một giao dịch thanh toán
        // - Chỉ xem được transaction thuộc order của chính user đó
        // ============================================================
        [Authorize]
        [HttpGet("{paymentTransactionId:int}")]
        public async Task<IActionResult> GetMyTransactionById(
            int paymentTransactionId
        )
        {
            var result = await _paymentService
                .GetMyTransactionByIdAsync(paymentTransactionId);

            return StatusCode(result.StatusCode, result);
        }
    }
}