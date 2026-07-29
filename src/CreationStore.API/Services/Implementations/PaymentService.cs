using System.Security.Claims;
using CreationStore.API.Data;
using CreationStore.API.DTOs.Payment;
using CreationStore.API.DTOs.ResponseTypes;
using CreationStore.API.Helpers.Constant;
using CreationStore.API.Models;
using CreationStore.API.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace CreationStore.API.Services.Implementations
{
    public class PaymentService : IPaymentService
    {
        private readonly CreationStoreDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IVnPayService _vnPayService;

        public PaymentService(
            CreationStoreDbContext context,
            IHttpContextAccessor httpContextAccessor,
            IVnPayService vnPayService
        )
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
            _vnPayService = vnPayService;
        }

        // ============================================================
        // CREATE VNPAY PAYMENT
        // Mục đích:
        // - User bấm thanh toán cho 1 order
        // - Backend kiểm tra order có hợp lệ không
        // - Tạo PaymentTransaction trạng thái Pending
        // - Gọi VnPayService để tạo paymentUrl
        // - Trả paymentUrl cho frontend
        // ============================================================
        public async Task<ResponseTypeDTO<CreateVnPayPaymentResponseDTO>>
            CreateVnPayPaymentAsync(int orderId)
        {
            var userId = GetCurrentUserId();

            if (userId == null)
            {
                return new ResponseTypeDTO<CreateVnPayPaymentResponseDTO>
                {
                    StatusCode = 401,
                    Message = "Invalid token",
                    Content = null
                };
            }

            // Chỉ lấy order thuộc user đang đăng nhập.
            // Không cho user A thanh toán order của user B.
            var order = await _context.Orders
                .FirstOrDefaultAsync(o =>
                    o.OrderId == orderId &&
                    o.UserId == userId.Value
                );

            if (order == null)
            {
                return new ResponseTypeDTO<CreateVnPayPaymentResponseDTO>
                {
                    StatusCode = 404,
                    Message = "Order not found",
                    Content = null
                };
            }

            if (order.Status == COrderStatus.Cancelled)
            {
                return new ResponseTypeDTO<CreateVnPayPaymentResponseDTO>
                {
                    StatusCode = 400,
                    Message = "Cancelled order cannot be paid",
                    Content = null
                };
            }

            if (order.Status != COrderStatus.PendingPayment)
            {
                return new ResponseTypeDTO<CreateVnPayPaymentResponseDTO>
                {
                    StatusCode = 400,
                    Message = "Only pending payment orders can be paid",
                    Content = null
                };
            }

            if (order.PaymentStatus == CPaymentStatus.Succeeded)
            {
                return new ResponseTypeDTO<CreateVnPayPaymentResponseDTO>
                {
                    StatusCode = 400,
                    Message = "Order has already been paid",
                    Content = null
                };
            }

            if (order.TotalAmount <= 0)
            {
                return new ResponseTypeDTO<CreateVnPayPaymentResponseDTO>
                {
                    StatusCode = 400,
                    Message = "Invalid order amount",
                    Content = null
                };
            }

            // Tạo mã giao dịch gửi sang VNPAY.
            // Khi VNPAY redirect về, mình dùng mã này để tìm lại transaction.
            var vnpTxnRef = GenerateVnpTxnRef(order.OrderId);

            var orderInfo = $"Thanh toan don hang {order.OrderId}";

            var ipAddress = _vnPayService.GetIpAddress(
                _httpContextAccessor.HttpContext
            );

            // Gọi VnPayService để tạo URL thanh toán.
            // PaymentService không tự ký hash, không tự build URL VNPAY.
            var paymentUrl = _vnPayService.CreatePaymentUrl(
                vnpTxnRef,
                order.TotalAmount,
                orderInfo,
                ipAddress
            );

            var paymentTransaction = new PaymentTransaction
            {
                OrderId = order.OrderId,
                PaymentMethod = CPaymentMethod.VnPay,
                Amount = order.TotalAmount,
                TransactionStatus = CPaymentTransactionStatus.Pending,
                VnpTxnRef = vnpTxnRef,
                CreatedAt = DateTime.Now,
                PaidAt = null
            };

            _context.PaymentTransactions.Add(paymentTransaction);

            // Nếu order trước đó từng thanh toán fail,
            // khi tạo payment mới thì đưa PaymentStatus về Pending.
            order.PaymentStatus = CPaymentStatus.Pending;

            await _context.SaveChangesAsync();

            return new ResponseTypeDTO<CreateVnPayPaymentResponseDTO>
            {
                StatusCode = 200,
                Message = "Create VNPAY payment successfully",
                Content = new CreateVnPayPaymentResponseDTO
                {
                    PaymentTransactionId =
                        paymentTransaction.PaymentTransactionId,
                    OrderId = order.OrderId,
                    Amount = paymentTransaction.Amount,
                    VnpTxnRef = paymentTransaction.VnpTxnRef,
                    PaymentUrl = paymentUrl
                }
            };
        }

        // ============================================================
        // HANDLE VNPAY RETURN
        // Mục đích:
        // - VNPAY redirect về API /vnpay-return
        // - Backend verify chữ ký
        // - Tìm PaymentTransaction bằng vnp_TxnRef
        // - Nếu thành công thì update:
        //      Order.Status = Paid
        //      Order.PaymentStatus = Succeeded
        //      PaymentTransaction.TransactionStatus = Succeeded
        // - Nếu thất bại thì update Failed
        // ============================================================
        public async Task<ResponseTypeDTO<VnPayReturnResponseDTO>>
            HandleVnPayReturnAsync(IQueryCollection query)
        {
            var isValidSignature = _vnPayService.ValidateSignature(query);

            if (!isValidSignature)
            {
                return new ResponseTypeDTO<VnPayReturnResponseDTO>
                {
                    StatusCode = 400,
                    Message = "Invalid VNPAY signature",
                    Content = new VnPayReturnResponseDTO
                    {
                        IsValidSignature = false,
                        IsSuccess = false,
                        Message = "Invalid VNPAY signature",
                        Transaction = null
                    }
                };
            }

            var vnpTxnRef = GetQueryValue(query, "vnp_TxnRef");

            if (string.IsNullOrWhiteSpace(vnpTxnRef))
            {
                return new ResponseTypeDTO<VnPayReturnResponseDTO>
                {
                    StatusCode = 400,
                    Message = "Missing vnp_TxnRef",
                    Content = new VnPayReturnResponseDTO
                    {
                        IsValidSignature = true,
                        IsSuccess = false,
                        Message = "Missing vnp_TxnRef",
                        Transaction = null
                    }
                };
            }

            var paymentTransaction = await _context.PaymentTransactions
                .Include(pt => pt.Order)
                .FirstOrDefaultAsync(pt => pt.VnpTxnRef == vnpTxnRef);

            if (paymentTransaction == null)
            {
                return new ResponseTypeDTO<VnPayReturnResponseDTO>
                {
                    StatusCode = 404,
                    Message = "Payment transaction not found",
                    Content = new VnPayReturnResponseDTO
                    {
                        IsValidSignature = true,
                        IsSuccess = false,
                        Message = "Payment transaction not found",
                        Transaction = null
                    }
                };
            }

            // Kiểm tra số tiền VNPAY trả về có khớp với transaction không.
            var vnpAmountText = GetQueryValue(query, "vnp_Amount");

            if (!long.TryParse(vnpAmountText, out long vnpAmount))
            {
                return new ResponseTypeDTO<VnPayReturnResponseDTO>
                {
                    StatusCode = 400,
                    Message = "Invalid VNPAY amount",
                    Content = new VnPayReturnResponseDTO
                    {
                        IsValidSignature = true,
                        IsSuccess = false,
                        Message = "Invalid VNPAY amount",
                        Transaction = MapPaymentTransactionToResponse(
                            paymentTransaction
                        )
                    }
                };
            }

            var expectedAmount = Convert.ToInt64(
                paymentTransaction.Amount * 100
            );

            if (vnpAmount != expectedAmount)
            {
                return new ResponseTypeDTO<VnPayReturnResponseDTO>
                {
                    StatusCode = 400,
                    Message = "VNPAY amount does not match order amount",
                    Content = new VnPayReturnResponseDTO
                    {
                        IsValidSignature = true,
                        IsSuccess = false,
                        Message = "VNPAY amount does not match order amount",
                        Transaction = MapPaymentTransactionToResponse(
                            paymentTransaction
                        )
                    }
                };
            }

            // Nếu callback/return bị gọi lại nhiều lần,
            // không xử lý lại transaction đã thành công.
            if (
                paymentTransaction.TransactionStatus ==
                CPaymentTransactionStatus.Succeeded
            )
            {
                return new ResponseTypeDTO<VnPayReturnResponseDTO>
                {
                    StatusCode = 200,
                    Message = "Payment already processed",
                    Content = new VnPayReturnResponseDTO
                    {
                        IsValidSignature = true,
                        IsSuccess = true,
                        Message = "Payment already processed",
                        Transaction = MapPaymentTransactionToResponse(
                            paymentTransaction
                        )
                    }
                };
            }

            await using var dbTransaction =
                await _context.Database.BeginTransactionAsync();

            try
            {
                var responseCode = GetQueryValue(query, "vnp_ResponseCode");
                var transactionStatus = GetQueryValue(
                    query,
                    "vnp_TransactionStatus"
                );

                // Nếu responseCode = 00 và transactionStatus = 00
                // ==> thanh toán thành công
                var isSuccess =
                    responseCode == "00" &&
                    transactionStatus == "00";

                paymentTransaction.VnpTransactionNo =
                    GetQueryValue(query, "vnp_TransactionNo");

                paymentTransaction.VnpResponseCode = responseCode;

                paymentTransaction.VnpTransactionStatus =
                    transactionStatus;

                paymentTransaction.VnpBankCode =
                    GetQueryValue(query, "vnp_BankCode");

                paymentTransaction.VnpPayDate =
                    GetQueryValue(query, "vnp_PayDate");

                // Lưu toàn bộ query vnpay trả về
                paymentTransaction.RawResponse = BuildRawResponse(query);

                if (isSuccess)
                {
                    paymentTransaction.TransactionStatus =
                        CPaymentTransactionStatus.Succeeded;

                    paymentTransaction.PaidAt = DateTime.Now;

                    paymentTransaction.Order.Status = COrderStatus.Paid;

                    paymentTransaction.Order.PaymentStatus =
                        CPaymentStatus.Succeeded;
                }
                else
                {
                    paymentTransaction.TransactionStatus =
                        CPaymentTransactionStatus.Failed;

                    paymentTransaction.PaidAt = null;

                    // Nếu order chưa từng paid thì mới set Failed.
                    // Tránh trường hợp order đã Paid rồi bị một return fail cũ ghi đè.
                    if (
                        paymentTransaction.Order.PaymentStatus !=
                        CPaymentStatus.Succeeded
                    )
                    {
                        paymentTransaction.Order.Status =
                            COrderStatus.PendingPayment;

                        paymentTransaction.Order.PaymentStatus =
                            CPaymentStatus.Failed;
                    }
                }

                await _context.SaveChangesAsync();

                await dbTransaction.CommitAsync();

                var message = isSuccess
                    ? "Payment succeeded"
                    : "Payment failed";

                return new ResponseTypeDTO<VnPayReturnResponseDTO>
                {
                    StatusCode = 200,
                    Message = message,
                    Content = new VnPayReturnResponseDTO
                    {
                        IsValidSignature = true,
                        IsSuccess = isSuccess,
                        Message = message,
                        Transaction = MapPaymentTransactionToResponse(
                            paymentTransaction
                        )
                    }
                };
            }
            catch
            {
                await dbTransaction.RollbackAsync();

                return new ResponseTypeDTO<VnPayReturnResponseDTO>
                {
                    StatusCode = 500,
                    Message = "Handle VNPAY return failed",
                    Content = new VnPayReturnResponseDTO
                    {
                        IsValidSignature = true,
                        IsSuccess = false,
                        Message = "Handle VNPAY return failed",
                        Transaction = null
                    }
                };
            }
        }

        // ============================================================
        // GET MY TRANSACTIONS
        // Mục đích:
        // - Lấy danh sách giao dịch thanh toán của user đang đăng nhập
        // - Chỉ lấy transaction thuộc order của user đó
        // ============================================================
        public async Task<ResponseTypeDTO<List<PaymentTransactionResponseDTO>>>
            GetMyTransactionsAsync()
        {
            var userId = GetCurrentUserId();

            if (userId == null)
            {
                return new ResponseTypeDTO<List<PaymentTransactionResponseDTO>>
                {
                    StatusCode = 401,
                    Message = "Invalid token",
                    Content = null
                };
            }

            var transactions = await _context.PaymentTransactions
                .AsNoTracking()
                .Include(pt => pt.Order)
                .Where(pt => pt.Order.UserId == userId.Value)
                .OrderByDescending(pt => pt.CreatedAt)
                .ToListAsync();

            var result = transactions
                .Select(MapPaymentTransactionToResponse)
                .ToList();

            return new ResponseTypeDTO<List<PaymentTransactionResponseDTO>>
            {
                StatusCode = 200,
                Message = "Get payment transactions successfully",
                Content = result
            };
        }

        // ============================================================
        // GET MY TRANSACTION BY ID
        // Mục đích:
        // - Lấy chi tiết 1 payment transaction
        // - User chỉ được xem transaction thuộc order của mình
        // ============================================================
        public async Task<ResponseTypeDTO<PaymentTransactionResponseDTO>>
            GetMyTransactionByIdAsync(int paymentTransactionId)
        {
            var userId = GetCurrentUserId();

            if (userId == null)
            {
                return new ResponseTypeDTO<PaymentTransactionResponseDTO>
                {
                    StatusCode = 401,
                    Message = "Invalid token",
                    Content = null
                };
            }

            var transaction = await _context.PaymentTransactions
                .AsNoTracking()
                .Include(pt => pt.Order)
                .FirstOrDefaultAsync(pt =>
                    pt.PaymentTransactionId == paymentTransactionId &&
                    pt.Order.UserId == userId.Value
                );

            if (transaction == null)
            {
                return new ResponseTypeDTO<PaymentTransactionResponseDTO>
                {
                    StatusCode = 404,
                    Message = "Payment transaction not found",
                    Content = null
                };
            }

            return new ResponseTypeDTO<PaymentTransactionResponseDTO>
            {
                StatusCode = 200,
                Message = "Get payment transaction successfully",
                Content = MapPaymentTransactionToResponse(transaction)
            };
        }

        // ============================================================
        // GET CURRENT USER ID
        // Mục đích:
        // - Lấy UserId từ JWT token
        // - UserId nằm trong ClaimTypes.NameIdentifier
        // ============================================================
        private int? GetCurrentUserId()
        {
            var userIdValue = _httpContextAccessor
                .HttpContext?
                .User
                .FindFirst(ClaimTypes.NameIdentifier)?
                .Value;

            if (!int.TryParse(userIdValue, out int userId))
            {
                return null;
            }

            return userId;
        }

        // ============================================================
        // GENERATE VNP TXN REF
        // Mục đích:
        // - Tạo mã giao dịch duy nhất gửi sang VNPAY
        // - VNPAY sẽ trả lại mã này khi redirect về
        // ============================================================
        private static string GenerateVnpTxnRef(int orderId)
        {
            var timePart = DateTime.UtcNow
                .AddHours(7)
                .ToString("yyyyMMddHHmmss");

            var randomPart = Guid.NewGuid()
                .ToString("N")
                .Substring(0, 8);

            return $"PAY{orderId}{timePart}{randomPart}";
        }

        // ============================================================
        // GET QUERY VALUE
        // Mục đích:
        // - Lấy value từ query string VNPAY trả về
        // - Nếu không có thì trả null
        // ============================================================
        private static string? GetQueryValue(
            IQueryCollection query,
            string key
        )
        {
            var value = query[key].ToString();

            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            return value;
        }

        // ============================================================
        // BUILD RAW RESPONSE
        // Mục đích:
        // - Lưu lại toàn bộ query VNPAY trả về
        // - Dùng để debug hoặc đối soát sau này
        // ============================================================
        private static string BuildRawResponse(IQueryCollection query)
        {
            return string.Join(
                "&",
                query.Select(q => $"{q.Key}={q.Value}")
            );
        }

        // ============================================================
        // MAP PAYMENT TRANSACTION TO RESPONSE DTO
        // Mục đích:
        // - Convert từ PaymentTransaction model sang DTO
        // - Không trả trực tiếp entity ra ngoài API
        // ============================================================
        private static PaymentTransactionResponseDTO
            MapPaymentTransactionToResponse(
                PaymentTransaction transaction
            )
        {
            return new PaymentTransactionResponseDTO
            {
                PaymentTransactionId =
                    transaction.PaymentTransactionId,

                OrderId = transaction.OrderId,

                PaymentMethod = transaction.PaymentMethod,

                Amount = transaction.Amount,

                TransactionStatus = transaction.TransactionStatus,

                VnpTxnRef = transaction.VnpTxnRef,

                VnpTransactionNo = transaction.VnpTransactionNo,

                VnpResponseCode = transaction.VnpResponseCode,

                VnpTransactionStatus =
                    transaction.VnpTransactionStatus,

                VnpBankCode = transaction.VnpBankCode,

                VnpPayDate = transaction.VnpPayDate,

                CreatedAt = transaction.CreatedAt,

                PaidAt = transaction.PaidAt
            };
        }
    }
}