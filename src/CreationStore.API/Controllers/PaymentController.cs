using CreationStore.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;

namespace CreationStore.API.Controllers
{
    [Route("api/payments")]
    [ApiController]
    public class PaymentController : ControllerBase
    {
        private readonly IPaymentService _paymentService;
        private readonly IConfiguration _configuration;

        public PaymentController(
            IPaymentService paymentService,
            IConfiguration configuration
        )
        {
            _paymentService = paymentService;
            _configuration = configuration;
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
        [HttpGet("vnpay-return")]
        [AllowAnonymous]
        public async Task<IActionResult> VnPayReturn()
        {
            var response = await _paymentService.HandleVnPayReturnAsync(Request.Query);

            if (response.Content == null)
            {
                var failedUrl = BuildPaymentResultUrl(
                    status: "failed",
                    orderId: null,
                    paymentTransactionId: null,
                    amount: null,
                    message: response.Message ?? "Payment failed"
                );

                return Redirect(failedUrl);
            }

            var result = response.Content;

            var status = result.IsSuccess ? "success" : "failed";

            var transaction = result.Transaction;

            var redirectUrl = BuildPaymentResultUrl(
                status: status,
                orderId: transaction?.OrderId,
                paymentTransactionId: transaction?.PaymentTransactionId,
                amount: transaction?.Amount,
                message: result.Message ?? response.Message
            );

            return Redirect(redirectUrl);
        }

        private string BuildPaymentResultUrl(string status, int? orderId, int? paymentTransactionId, decimal? amount, string? message)
        {
            var frontendBaseUrl =
                _configuration["Frontend:BaseUrl"]?.TrimEnd('/');

            if (string.IsNullOrWhiteSpace(frontendBaseUrl))
            {
                frontendBaseUrl = "http://localhost:5000";
            }

            var queryParams = new Dictionary<string, string?>
            {
                ["status"] = status,
                ["orderId"] = orderId?.ToString(),
                ["paymentTransactionId"] = paymentTransactionId?.ToString(),
                ["amount"] = amount?.ToString(CultureInfo.InvariantCulture),
                ["message"] = message
            };

            var queryString = string.Join(
                "&",
                queryParams
                    .Where(item => !string.IsNullOrWhiteSpace(item.Value))
                    .Select(item =>
                        $"{Uri.EscapeDataString(item.Key)}={Uri.EscapeDataString(item.Value!)}"
                    )
            );

            return $"{frontendBaseUrl}/payment-result?{queryString}";
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